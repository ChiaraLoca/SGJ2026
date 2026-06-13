using System.Collections.Generic;
using FourE.Cards;
using FourE.Commanders;
using FourE.Events;
using FourE.Players;

namespace FourE.Core
{
    /// <summary>
    /// Gestisce i turni alternati della Fase PLAY: gioco carte, limite per turno,
    /// gioco della Verifica e passaggio del turno.
    /// </summary>
    public sealed class TurnManager
    {
        private readonly GameStateManager _state;
        private readonly EffectResolver _resolver;
        private PhaseManager _phases;

        /// <summary>Carte giocate nel turno corrente.</summary>
        public int CardsPlayedThisTurn { get; private set; }

        /// <summary>Carte giocabili nel turno corrente, dal config in base al round.</summary>
        public int CardsAllowedThisTurn { get; private set; }

        /// <summary>
        /// Crea il gestore dei turni.
        /// </summary>
        /// <param name="state">Riferimento allo stato di gioco.</param>
        /// <param name="resolver">Risolutore degli effetti carta.</param>
        public TurnManager(GameStateManager state, EffectResolver resolver)
        {
            _state = state;
            _resolver = resolver;
        }

        /// <summary>
        /// Collega il PhaseManager dopo la costruzione (rompe la dipendenza circolare).
        /// </summary>
        /// <param name="phases">Gestore delle fasi.</param>
        public void SetPhaseManager(PhaseManager phases)
        {
            _phases = phases;
        }

        /// <summary>
        /// Avvia il turno di un giocatore, impostandolo come attivo e azzerando i contatori.
        /// </summary>
        /// <param name="player">Giocatore di turno.</param>
        public void StartTurn(PlayerState player)
        {
            _state.SetActivePlayer(player);
            CardsPlayedThisTurn = 0;
            CardsAllowedThisTurn = _state.GameConfig.GetCardsPlayablePerTurn(_state.CurrentRoundIndex);
        }

        /// <summary>
        /// Tenta di giocare una carta standard dalla mano del giocatore attivo.
        /// Raggiunto il limite di carte, il turno termina automaticamente.
        /// </summary>
        /// <param name="player">Giocatore che gioca la carta.</param>
        /// <param name="card">Carta da giocare.</param>
        /// <param name="selectedTargets">Comandanti scelti a runtime per i bersagli selezionabili.</param>
        /// <returns>True se la carta è stata giocata.</returns>
        public bool TryPlayCard(PlayerState player, CardDataSO card, IReadOnlyList<CommanderState> selectedTargets = null)
        {
            if (!IsActivePlayer(player) || _state.CurrentPhase != GamePhase.Play)
            {
                return false;
            }

            if (card == null || card.IsVerifica || !player.Hand.Contains(card))
            {
                return false;
            }

            if (CardsPlayedThisTurn >= CardsAllowedThisTurn)
            {
                return false;
            }

            GameContext context = _state.BuildContext(selectedTargets);
            _resolver.Resolve(card, context);

            player.Hand.Remove(card);
            player.DiscardPile.Add(card);
            CardsPlayedThisTurn++;

            if (CardsPlayedThisTurn >= CardsAllowedThisTurn)
            {
                EndTurn(player);
            }

            return true;
        }

        /// <summary>
        /// Tenta di giocare la carta Verifica, chiudendo la Fase PLAY.
        /// </summary>
        /// <param name="player">Giocatore che gioca la Verifica.</param>
        /// <returns>True se la Verifica è stata giocata.</returns>
        public bool TryPlayVerifica(PlayerState player)
        {
            if (!IsActivePlayer(player) || _state.CurrentPhase != GamePhase.Play)
            {
                return false;
            }

            if (player.VerificaCard == null)
            {
                return false;
            }

            player.VerificaCard = null;
            EventBus.Publish(new VerificaPlayedEvent(player));
            _phases.HandleVerifica(player);
            return true;
        }

        /// <summary>
        /// Termina il turno del giocatore: scala gli effetti a durata sui suoi comandanti
        /// e passa il turno all'avversario se la Fase PLAY è ancora attiva.
        /// </summary>
        /// <param name="player">Giocatore di cui terminare il turno.</param>
        public void EndTurn(PlayerState player)
        {
            if (!IsActivePlayer(player))
            {
                return;
            }

            // Gli effetti a durata su chi termina il turno scalano (è chi li subisce).
            foreach (CommanderState commander in player.Commanders)
            {
                commander.TickActiveEffects();
            }

            EventBus.Publish(new TurnEndedEvent(player));

            if (_state.CurrentPhase == GamePhase.Play)
            {
                StartTurn(_state.OpponentOf(player));
            }
        }

        /// <summary>
        /// Verifica che il giocatore indicato sia quello attivo.
        /// </summary>
        /// <param name="player">Giocatore da controllare.</param>
        /// <returns>True se è il giocatore attivo.</returns>
        private bool IsActivePlayer(PlayerState player)
        {
            return _state.ActivePlayer == player;
        }
    }
}
