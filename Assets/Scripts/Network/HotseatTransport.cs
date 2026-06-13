using System;

namespace FourE.Network
{
    /// <summary>
    /// Trasporto hotseat a giro chiuso: un solo schermo, due giocatori che si alternano.
    /// Aggiorna <see cref="LocalActorNumber"/> dopo ogni broadcast in modo che la UI
    /// mostri sempre la prospettiva del giocatore attivo.
    /// </summary>
    public sealed class HotseatTransport : INetworkTransport
    {
        /// <inheritdoc />
        public bool IsHost => true;

        /// <inheritdoc />
        public int LocalActorNumber { get; private set; }

        /// <inheritdoc />
        public event Action<GameIntent> IntentReceived;

        /// <inheritdoc />
        public event Action<GameStateDTO> StateReceived;

        /// <summary>
        /// Crea il trasporto hotseat.
        /// </summary>
        /// <param name="initialActorNumber">Attore del primo giocatore che inizia.</param>
        public HotseatTransport(int initialActorNumber)
        {
            LocalActorNumber = initialActorNumber;
        }

        /// <inheritdoc />
        public void SendIntent(GameIntent intent)
        {
            // In hotseat l'host è locale: l'intent viene consegnato immediatamente.
            IntentReceived?.Invoke(intent);
        }

        /// <inheritdoc />
        public void BroadcastState(GameStateDTO state)
        {
            // Aggiorna l'attore locale prima di notificare la UI: così la view
            // si ridisegna dalla prospettiva del nuovo giocatore attivo.
            if (state.ActiveActorNumber >= 0)
            {
                LocalActorNumber = state.ActiveActorNumber;
            }

            StateReceived?.Invoke(state);
        }
    }
}
