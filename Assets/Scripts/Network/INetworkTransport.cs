using System;

namespace FourE.Network
{
    /// <summary>
    /// Astrazione del trasporto di rete che disaccoppia la logica di gioco da Photon.
    /// Gli intent viaggiano Client→Host; i DTO di stato viaggiano Host→All.
    /// Implementazioni: <see cref="LocalLoopbackTransport"/> (offline) e una futura PhotonTransport.
    /// </summary>
    public interface INetworkTransport
    {
        /// <summary>True se l'istanza locale è l'host autoritativo (MasterClient).</summary>
        bool IsHost { get; }

        /// <summary>Attore Photon dell'istanza locale.</summary>
        int LocalActorNumber { get; }

        /// <summary>Sollevato sull'host alla ricezione di un intent da validare ed eseguire.</summary>
        event Action<GameIntent> IntentReceived;

        /// <summary>Sollevato su ogni client alla ricezione di un nuovo stato da applicare.</summary>
        event Action<GameStateDTO> StateReceived;

        /// <summary>
        /// Invia un intent all'host. Sull'host viene instradato direttamente alla validazione.
        /// </summary>
        /// <param name="intent">Intent da inviare.</param>
        void SendIntent(GameIntent intent);

        /// <summary>
        /// Broadcasta lo stato corrente a tutti i client. Invocato solo dall'host.
        /// </summary>
        /// <param name="state">Snapshot completo dello stato.</param>
        void BroadcastState(GameStateDTO state);
    }
}
