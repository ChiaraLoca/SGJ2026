using System;

namespace FourE.Network
{
    /// <summary>
    /// Trasporto offline a giro chiuso: l'istanza locale è sempre host e si parla da sola.
    /// Gli intent inviati tornano subito a <see cref="IntentReceived"/>, i broadcast a
    /// <see cref="StateReceived"/>. Permette di girare e testare tutto il flusso senza Photon.
    /// </summary>
    public sealed class LocalLoopbackTransport : INetworkTransport
    {
        /// <inheritdoc />
        public bool IsHost => true;

        /// <inheritdoc />
        public int LocalActorNumber { get; }

        /// <inheritdoc />
        public event Action<GameIntent> IntentReceived;

        /// <inheritdoc />
        public event Action<GameStateDTO> StateReceived;

        /// <summary>
        /// Crea il trasporto loopback.
        /// </summary>
        /// <param name="localActorNumber">Attore associato all'istanza locale.</param>
        public LocalLoopbackTransport(int localActorNumber)
        {
            LocalActorNumber = localActorNumber;
        }

        /// <inheritdoc />
        public void SendIntent(GameIntent intent)
        {
            // In loopback l'host è locale: l'intent è consegnato immediatamente.
            IntentReceived?.Invoke(intent);
        }

        /// <inheritdoc />
        public void BroadcastState(GameStateDTO state)
        {
            // Il broadcast torna direttamente alla view locale.
            StateReceived?.Invoke(state);
        }
    }
}
