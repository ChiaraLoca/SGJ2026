#if PHOTON_UNITY_NETWORKING
using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using FourE.Config;

namespace FourE.Network
{
    /// <summary>
    /// Implementazione Photon (PUN2) di <see cref="INetworkTransport"/> per il 1v1 online,
    /// host-authoritative. Gli intent viaggiano Client→MasterClient; i DTO di stato
    /// MasterClient→Others. Il MasterClient (chi crea la stanza) è l'host autoritativo.
    /// L'intero file è compilato solo quando PUN2 è importato (<c>PHOTON_UNITY_NETWORKING</c>).
    /// </summary>
    public sealed class PhotonTransport : INetworkTransport, IOnEventCallback, IDisposable
    {
        private static readonly SendOptions Reliable = SendOptions.SendReliable;

        /// <inheritdoc />
        public bool IsHost => PhotonNetwork.IsMasterClient;

        /// <summary>
        /// Attore di gioco locale: il MasterClient è sempre il primo giocatore (slot 0),
        /// l'ospite il secondo (slot 1). Mappa i numeri attore Photon sugli indici di gioco.
        /// </summary>
        public int LocalActorNumber => PhotonNetwork.IsMasterClient
            ? GameConstants.FirstCommanderIndex
            : GameConstants.SecondCommanderIndex;

        /// <inheritdoc />
        public event Action<GameIntent> IntentReceived;

        /// <inheritdoc />
        public event Action<GameStateDTO> StateReceived;

        /// <inheritdoc />
        public event Action ClientJoined;

        /// <summary>
        /// Registra il trasporto come destinatario delle callback evento di Photon.
        /// </summary>
        public PhotonTransport()
        {
            PhotonNetwork.AddCallbackTarget(this);
        }

        /// <inheritdoc />
        public void SendIntent(GameIntent intent)
        {
            // L'host esegue i propri intent in locale, senza passare dalla rete.
            if (IsHost)
            {
                IntentReceived?.Invoke(intent);
                return;
            }

            byte[] payload = NetworkSerializer.SerializeIntent(intent);
            RaiseEventOptions options = new() { Receivers = ReceiverGroup.MasterClient };
            PhotonNetwork.RaiseEvent((byte)intent.Type, payload, options, Reliable);
        }

        /// <inheritdoc />
        public void BroadcastState(GameStateDTO state)
        {
            // L'host aggiorna subito la propria view in locale...
            StateReceived?.Invoke(state);

            // ...e invia lo snapshot agli altri (solo l'host broadcasta).
            byte[] payload = NetworkSerializer.SerializeState(state);
            RaiseEventOptions options = new() { Receivers = ReceiverGroup.Others };
            PhotonNetwork.RaiseEvent(PhotonEventCodes.StateSync, payload, options, Reliable);
        }

        /// <summary>
        /// Chiede all'host lo stato corrente. Invocato dal client appena pronto nella scena di
        /// gioco (resync di late-join). Ignorato sull'host, che lo stato lo possiede già.
        /// </summary>
        public void RequestInitialState()
        {
            if (IsHost)
            {
                return;
            }

            RaiseEventOptions options = new() { Receivers = ReceiverGroup.MasterClient };
            PhotonNetwork.RaiseEvent(PhotonEventCodes.RequestState, null, options, Reliable);
        }

        /// <summary>
        /// Smista gli eventi Photon in arrivo verso gli abbonati appropriati.
        /// </summary>
        /// <param name="photonEvent">Evento ricevuto dalla rete.</param>
        public void OnEvent(EventData photonEvent)
        {
            byte code = photonEvent.Code;

            if (code == PhotonEventCodes.StateSync)
            {
                // L'host ignora la propria sincronizzazione: la applica già in locale.
                if (IsHost)
                {
                    return;
                }

                GameStateDTO state = NetworkSerializer.DeserializeState((byte[])photonEvent.CustomData);
                StateReceived?.Invoke(state);
                return;
            }

            if (code == PhotonEventCodes.RequestState)
            {
                if (IsHost)
                {
                    ClientJoined?.Invoke();
                }

                return;
            }

            // Codici intent (PlayCard..FinishShop): solo l'host li esegue.
            if (code >= PhotonEventCodes.PlayCard && code <= PhotonEventCodes.FinishShop)
            {
                if (!IsHost)
                {
                    return;
                }

                GameIntent intent = NetworkSerializer.DeserializeIntent((byte[])photonEvent.CustomData);
                IntentReceived?.Invoke(intent);
            }
        }

        /// <summary>
        /// Disiscrive il trasporto dalle callback Photon. Da chiamare al teardown.
        /// </summary>
        public void Dispose()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }
    }
}
#endif
