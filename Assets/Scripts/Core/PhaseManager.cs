using System;
using System.Collections.Generic;
using UnityEngine;
using FourE.Cards;
using FourE.Commanders;
using FourE.Config;
using FourE.Events;
using FourE.Players;
using FourE.Shop;

namespace FourE.Core
{
    /// <summary>
    /// State machine della partita: orchestra le transizioni PLAY → VERIFICA → SHOP → DRAW
    /// e il passaggio al round successivo o all'Esame Finale.
    /// </summary>
    [Serializable]
    public sealed class PhaseManager
    {
        private readonly GameStateManager _state;
        private readonly TurnManager _turns;
        private readonly ShopManager _shop;
        private readonly RoundManager _rounds;
        private readonly IStartingPlayerDecider _startingPlayerDecider;
        private readonly System.Random _rng;
        private readonly HashSet<int> _shopFinished = new();

        [SerializeField] private GamePhase _currentPhase = GamePhase.Setup;

        /// <summary>Fase corrente della partita.</summary>
        public GamePhase CurrentPhase => _currentPhase;

        /// <summary>
        /// Crea il gestore delle fasi.
        /// </summary>
        /// <param name="state">Riferimento allo stato di gioco.</param>
        /// <param name="turns">Gestore dei turni.</param>
        /// <param name="shop">Gestore dello shop.</param>
        /// <param name="rounds">Gestore dei round.</param>
        /// <param name="startingPlayerDecider">Decider del giocatore che apre ogni round.</param>
        /// <param name="rng">Generatore casuale host-authoritative.</param>
        public PhaseManager(
            GameStateManager state,
            TurnManager turns,
            ShopManager shop,
            RoundManager rounds,
            IStartingPlayerDecider startingPlayerDecider,
            System.Random rng)
        {
            _state = state;
            _turns = turns;
            _shop = shop;
            _rounds = rounds;
            _startingPlayerDecider = startingPlayerDecider;
            _rng = rng;
        }

        /// <summary>
        /// Avvia la partita entrando nella prima Fase PLAY.
        /// </summary>
        /// <param name="firstPlayer">Giocatore che muove per primo.</param>
        public void BeginMatch(PlayerState firstPlayer)
        {
            EnterPhase(GamePhase.Play);
            _turns.BeginRound();
            ApplyRoundStartPassives();
            _turns.StartTurn(firstPlayer);
        }

        /// <summary>
        /// Gestisce il gioco della Verifica: converte subito le Note in Crediti per entrambi.
        /// Entra nello Shop nei round intermedi; nell'ultimo procede direttamente al Draw.
        /// </summary>
        /// <param name="closer">Giocatore che ha chiuso il round.</param>
        public void HandleVerifica(PlayerState closer)
        {
            EnterPhase(GamePhase.Verifica);

            // La Verifica trasforma le Note (temporanee) in Crediti (permanenti) per entrambi.
            ConvertNotesToCredits(_state.Player0);
            ConvertNotesToCredits(_state.Player1);

            if (_rounds.IsLastPlayableRound)
            {
                ConvertAndAdvance();
            }
            else
            {
                EnterShop();
            }
        }

        /// <summary>
        /// Segnala che un giocatore ha terminato i propri acquisti. Quando entrambi
        /// hanno finito, procede alla Fase DRAW.
        /// </summary>
        /// <param name="player">Giocatore che ha concluso lo shop.</param>
        public void FinishShop(PlayerState player)
        {
            if (_currentPhase != GamePhase.Shop)
            {
                return;
            }

            _shop.RefreshPool(player);
            _shopFinished.Add(player.ActorNumber);

            if (_shopFinished.Count >= GameConstants.PlayersPerMatch)
            {
                ConvertAndAdvance();
            }
        }

        /// <summary>
        /// Entra nella Fase SHOP azzerando acquisti e marcatori di completamento.
        /// </summary>
        private void EnterShop()
        {
            _shop.ResetPurchases();
            _shopFinished.Clear();
            EnterPhase(GamePhase.Shop);
        }

        /// <summary>
        /// Conclude l'intervallo: esegue la Fase DRAW per entrambi (rimischia, pesca, azzera le Note a 5)
        /// e avanza al round successivo o all'Esame Finale. La conversione Note→Crediti è già avvenuta
        /// al gioco della Verifica.
        /// </summary>
        private void ConvertAndAdvance()
        {
            EnterPhase(GamePhase.Draw);

            // Fase DRAW: scarta, rimischia, pesca, reset Note (a base) ed effetti.
            RunDrawPhase(_state.Player0);
            RunDrawPhase(_state.Player1);

            // Avanzamento round.
            _rounds.Advance();

            if (_rounds.IsFinalExamReached)
            {
                EnterFinalExam();
            }
            else
            {
                BeginNextRound();
            }
        }

        /// <summary>
        /// Converte le Note correnti di un giocatore in Crediti permanenti.
        /// </summary>
        /// <param name="player">Giocatore di cui convertire le Note.</param>
        private void ConvertNotesToCredits(PlayerState player)
        {
            int credits = (int)Math.Round(player.TotalNotes * _state.GameConfig.NoteToCreditsMultiplier);
            player.AddCredits(credits);
        }

        /// <summary>
        /// Esegue la Fase DRAW per un giocatore: scarta la mano, rimischia tutte le carte
        /// possedute, pesca la nuova mano e resetta Note ed effetti dei comandanti.
        /// </summary>
        /// <param name="player">Giocatore su cui eseguire la fase.</param>
        private void RunDrawPhase(PlayerState player)
        {
            // Scarta tutta la mano e unisci ogni carta posseduta nel mazzo.
            player.DiscardPile.AddRange(player.Hand);
            player.Hand.Clear();
            player.Deck.AddRange(player.DiscardPile);
            player.DiscardPile.Clear();

            CollectionUtils.Shuffle(player.Deck, _rng);

            int draw = Math.Min(_state.GameConfig.StartingHandSize, player.Deck.Count);
            for (int i = 0; i < draw; i++)
            {
                int topIndex = player.Deck.Count - 1;
                CardDataSO card = player.Deck[topIndex];
                player.Deck.RemoveAt(topIndex);
                player.Hand.Add(card);
            }

            foreach (CommanderState commander in player.Commanders)
            {
                commander.ResetForNewRound();
            }
        }

        /// <summary>
        /// Avvia la Fase PLAY del round successivo.
        /// </summary>
        private void BeginNextRound()
        {
            EnterPhase(GamePhase.Play);
            _turns.BeginRound();
            ApplyRoundStartPassives();
            // L'apertura del round è decisa dal decider (lancio di moneta o mock nei test).
            PlayerState opener = _startingPlayerDecider.DecideStartingPlayer(_state.Player0, _state.Player1);
            _turns.StartTurn(opener);
        }

        /// <summary>
        /// Applica le passive di inizio round a entrambi i giocatori (Note di Storia, pesca di Matematica).
        /// </summary>
        private void ApplyRoundStartPassives()
        {
            _state.Passives?.ApplyRoundStartPassives(_state.Player0);
            _state.Passives?.ApplyRoundStartPassives(_state.Player1);
        }

        /// <summary>
        /// Entra nell'Esame Finale e pubblica l'esito della partita.
        /// </summary>
        private void EnterFinalExam()
        {
            EnterPhase(GamePhase.FinalExam);
            EventBus.Publish(ResolveOutcome());
        }

        /// <summary>
        /// Determina l'esito della partita: vince chi ha più Credits; a parità di Credits
        /// vince chi ha più carte nel mazzo; se anche queste sono pari, è pareggio.
        /// </summary>
        /// <returns>Evento di fine partita con vincitore o pareggio.</returns>
        private GameOverEvent ResolveOutcome()
        {
            PlayerState p0 = _state.Player0;
            PlayerState p1 = _state.Player1;

            if (p0.Credits != p1.Credits)
            {
                int winner = p0.Credits > p1.Credits ? p0.ActorNumber : p1.ActorNumber;
                return new GameOverEvent(winner, isDraw: false);
            }

            // Spareggio: vince chi possiede più carte nel mazzo.
            if (p0.Deck.Count != p1.Deck.Count)
            {
                int winner = p0.Deck.Count > p1.Deck.Count ? p0.ActorNumber : p1.ActorNumber;
                return new GameOverEvent(winner, isDraw: false);
            }

            // Parità totale: pareggio.
            return new GameOverEvent(GameOverEvent.NoWinner, isDraw: true);
        }

        /// <summary>
        /// Imposta la fase corrente e pubblica <see cref="PhaseChangedEvent"/>.
        /// </summary>
        /// <param name="phase">Nuova fase.</param>
        private void EnterPhase(GamePhase phase)
        {
            _currentPhase = phase;
            EventBus.Publish(new PhaseChangedEvent(phase));
        }
    }
}
