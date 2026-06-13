namespace FourE.Network
{
    /// <summary>
    /// Pubblicato sull'EventBus a ogni nuovo stato ricevuto dal trasporto.
    /// La UI vi si iscrive per ridisegnarsi a partire dal DTO, senza accedere alla logica.
    /// </summary>
    public readonly struct GameStateSyncedEvent
    {
        /// <summary>Snapshot di stato appena ricevuto.</summary>
        public GameStateDTO State { get; }

        /// <summary>Attore dell'istanza locale che osserva lo stato.</summary>
        public int LocalActorNumber { get; }

        /// <summary>
        /// Crea l'evento di sincronizzazione.
        /// </summary>
        /// <param name="state">Stato ricevuto.</param>
        /// <param name="localActorNumber">Attore locale che osserva.</param>
        public GameStateSyncedEvent(GameStateDTO state, int localActorNumber)
        {
            State = state;
            LocalActorNumber = localActorNumber;
        }
    }
}
