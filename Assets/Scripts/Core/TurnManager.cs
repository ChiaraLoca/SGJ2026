using System;
using System.Collections.Generic;
using UnityEngine;
using FourE.Cards;
using FourE.Commanders;
using FourE.Config;
using FourE.Events;
using FourE.Players;

namespace FourE.Core
{
    /// <summary>
    /// Gestisce i turni alternati della Fase PLAY: gioco carte, limite per turno,
    /// gioco della Verifica e passaggio del turno.
    /// </summary>
    [Serializable]
    public sealed class TurnManager
    {
        private readonly GameStateManager _state;
        private readonly EffectResolver _resolver;
        private PhaseManager _phases;

        [SerializeField] private int _cardsPlayedThisTurn;
        [SerializeField] private int _cardsAllowedThisTurn;
        [SerializeField] private int _turnInRound;

        /// <summary>Carte giocate nel turno corrente.</summary>
        public int CardsPlayedThisTurn => _cardsPlayedThisTurn;

        /// <summary>Carte giocabili nel turno corrente, dal config in base al round.</summary>
        public int CardsAllowedThisTurn => _cardsAllowedThisTurn;

        /// <summary>Azioni carta ancora disponibili nel turno corrente.</summary>
        public int RemainingActions => Math.Max(0, _cardsAllowedThisTurn - _cardsPlayedThisTurn);

        /// <summary>Indice del turno all'interno del round corrente (1 = primo turno).</summary>
        public int TurnInRound => _turnInRound;

        /// <summary>True se la Verifica può essere giocata ora: non nel 1° turno del round.</summary>
        public bool CanPlayVerificaThisTurn => _turnInRound > GameConstants.FirstRoundTurnNumber;

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
        /// Reimposta il contatore dei turni all'inizio di un nuovo round.
        /// Da chiamare prima del primo <see cref="StartTurn"/> del round.
        /// </summary>
        public void BeginRound()
        {
            _turnInRound = 0;
        }

        /// <summary>
        /// Avvia il turno di un giocatore, impostandolo come attivo e azzerando i contatori.
        /// Pesca le carte di inizio turno dal mazzo.
        /// </summary>
        /// <param name="player">Giocatore di turno.</param>
        public void StartTurn(PlayerState player)
        {
            _state.SetActivePlayer(player);
            _cardsPlayedThisTurn = 0;
            _cardsAllowedThisTurn = _state.GameConfig.GetCardsPlayablePerTurn(_state.CurrentRoundIndex);
            _turnInRound++;

            // L'immunità "fino al tuo prossimo turno" (Fidanzata) decade qui.
            foreach (CommanderState commander in player.Commanders)
            {
                commander.SetNoteFloorLocked(false);
            }

            DrawTurnStartCards(player);
        }

        /// <summary>
        /// Pesca le carte di inizio turno dal mazzo del giocatore, fino al limite configurato.
        /// </summary>
        /// <param name="player">Giocatore che pesca.</param>
        private void DrawTurnStartCards(PlayerState player)
        {
            int count = _state.GameConfig.TurnStartDrawCount;
            int drawable = Math.Min(count, player.Deck.Count);
            for (int i = 0; i < drawable; i++)
            {
                int topIndex = player.Deck.Count - 1;
                player.Hand.Add(player.Deck[topIndex]);
                player.Deck.RemoveAt(topIndex);
            }
        }

        /// <summary>
        /// Concede (o sottrae, se negativo) azioni giocabili nel turno corrente.
        /// </summary>
        /// <param name="amount">Azioni da aggiungere; negativo per ridurle.</param>
        public void GrantExtraActions(int amount)
        {
            _cardsAllowedThisTurn += amount;
        }

        /// <summary>
        /// Raddoppia le azioni ancora disponibili nel turno corrente (Copiare).
        /// </summary>
        public void DoubleRemainingActions()
        {
            int remaining = _cardsAllowedThisTurn - _cardsPlayedThisTurn;
            if (remaining > 0)
            {
                _cardsAllowedThisTurn += remaining;
            }
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

            if (_cardsPlayedThisTurn >= _cardsAllowedThisTurn)
            {
                return false;
            }

            GameContext context = _state.BuildContext(selectedTargets);
            _resolver.Resolve(card, context);

            player.Hand.Remove(card);
            player.DiscardPile.Add(card);
            _cardsPlayedThisTurn++;

            if (_cardsPlayedThisTurn >= _cardsAllowedThisTurn)
            {
                EndTurn(player);
            }

            return true;
        }

        /// <summary>
        /// Tenta di giocare la carta Verifica dalla mano, chiudendo la Fase PLAY.
        /// Vietata nel 1° turno del round, se bloccata (Sciopero) o se non in mano.
        /// </summary>
        /// <param name="player">Giocatore che gioca la Verifica.</param>
        /// <returns>True se la Verifica è stata giocata.</returns>
        public bool TryPlayVerifica(PlayerState player)
        {
            if (!IsActivePlayer(player) || _state.CurrentPhase != GamePhase.Play)
            {
                return false;
            }

            if (player.VerificaBlocked || !CanPlayVerificaThisTurn)
            {
                return false;
            }

            CardDataSO verifica = FindVerificaInHand(player);
            if (verifica == null)
            {
                return false;
            }

            // La Verifica giocata va nel cimitero: rientra nel mazzo alla Fase DRAW.
            player.Hand.Remove(verifica);
            player.DiscardPile.Add(verifica);

            EventBus.Publish(new VerificaPlayedEvent(player));
            _phases.HandleVerifica(player);
            return true;
        }

        /// <summary>
        /// Cerca la carta Verifica nella mano del giocatore.
        /// </summary>
        /// <param name="player">Giocatore di cui ispezionare la mano.</param>
        /// <returns>La carta Verifica, o null se assente.</returns>
        private static CardDataSO FindVerificaInHand(PlayerState player)
        {
            foreach (CardDataSO card in player.Hand)
            {
                if (card != null && card.IsVerifica)
                {
                    return card;
                }
            }

            return null;
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

            // Il blocco Verifica (Sciopero) dura un solo turno: si libera a fine turno.
            player.VerificaBlocked = false;

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
