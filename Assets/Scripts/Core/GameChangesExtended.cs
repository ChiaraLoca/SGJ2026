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
    /// Concede o sottrae azioni giocabili nel turno corrente.
    /// </summary>
    public sealed class GrantActionsChange : IGameChange, IPositiveActionCardChange
    {
        private readonly TurnManager _turns;
        private readonly int _amount;

        /// <inheritdoc/>
        bool IPositiveActionCardChange.IsPositiveActionBenefit => _amount > 0;

        /// <summary>Crea la modifica di azioni extra.</summary>
        /// <param name="turns">Gestore dei turni su cui agire.</param>
        /// <param name="amount">Azioni da aggiungere (negativo per ridurle).</param>
        public GrantActionsChange(TurnManager turns, int amount)
        {
            _turns = turns;
            _amount = amount;
        }

        /// <inheritdoc/>
        public void Apply()
        {
            _turns?.GrantExtraActions(_amount);
        }
    }

    /// <summary>
    /// Fa pescare a un giocatore fino a raggiungere una dimensione di mano (Biblioteca).
    /// </summary>
    public sealed class DrawToHandSizeChange : IGameChange, IPositivePlayerCardChange
    {
        private readonly PlayerState _player;
        private readonly int _targetHandSize;

        /// <inheritdoc/>
        PlayerState IPositivePlayerCardChange.BeneficiaryPlayer => _player;

        /// <summary>Crea la modifica di pesca fino a dimensione mano.</summary>
        /// <param name="player">Giocatore che pesca.</param>
        /// <param name="targetHandSize">Dimensione di mano da raggiungere.</param>
        public DrawToHandSizeChange(PlayerState player, int targetHandSize)
        {
            _player = player;
            _targetHandSize = targetHandSize;
        }

        /// <inheritdoc/>
        public void Apply()
        {
            int missing = _targetHandSize - _player.Hand.Count;
            DeckOps.DrawTopToHand(_player, missing);
        }
    }

    /// <summary>
    /// Fa pescare a un giocatore l'intero mazzo (Approfondimento).
    /// </summary>
    public sealed class DrawAllChange : IGameChange, IPositivePlayerCardChange
    {
        private readonly PlayerState _player;

        /// <inheritdoc/>
        PlayerState IPositivePlayerCardChange.BeneficiaryPlayer => _player;

        /// <summary>Crea la modifica di pesca totale.</summary>
        /// <param name="player">Giocatore che pesca tutto il mazzo.</param>
        public DrawAllChange(PlayerState player)
        {
            _player = player;
        }

        /// <inheritdoc/>
        public void Apply()
        {
            DeckOps.DrawTopToHand(_player, _player.Deck.Count);
        }
    }

    /// <summary>
    /// Fa scartare a un giocatore N carte casuali dalla mano (Gossip), escludendo la Verifica.
    /// </summary>
    public sealed class ForceDiscardRandomChange : IGameChange
    {
        private readonly PlayerState _player;
        private readonly int _count;

        /// <summary>Crea la modifica di scarto casuale.</summary>
        /// <param name="player">Giocatore che scarta.</param>
        /// <param name="count">Numero di carte da scartare.</param>
        public ForceDiscardRandomChange(PlayerState player, int count)
        {
            _player = player;
            _count = count;
        }

        /// <inheritdoc/>
        public void Apply()
        {
            // La Verifica è immune allo scarto casuale per regola.
            var pool = new List<CardDataSO>();
            foreach (CardDataSO c in _player.Hand)
            {
                if (c != null && !c.IsVerifica) pool.Add(c);
            }

            int toDiscard = Math.Min(_count, pool.Count);
            for (int i = 0; i < toDiscard; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                CardDataSO card = pool[index];
                pool.RemoveAt(index);
                _player.Hand.Remove(card);
                _player.DiscardPile.Add(card);
            }
        }
    }

    /// <summary>
    /// Fa scartare a un giocatore tutte le carte con un tag dalla mano (Politica, Bullismo).
    /// </summary>
    public sealed class ForceDiscardByTagChange : IGameChange
    {
        private readonly PlayerState _player;
        private readonly CardTag _tag;

        /// <summary>Crea la modifica di scarto per tag.</summary>
        /// <param name="player">Giocatore che scarta.</param>
        /// <param name="tag">Tag da scartare interamente.</param>
        public ForceDiscardByTagChange(PlayerState player, CardTag tag)
        {
            _player = player;
            _tag = tag;
        }

        /// <inheritdoc/>
        public void Apply()
        {
            for (int i = _player.Hand.Count - 1; i >= 0; i--)
            {
                CardDataSO card = _player.Hand[i];
                if (card != null && card.HasTag(_tag))
                {
                    _player.Hand.RemoveAt(i);
                    _player.DiscardPile.Add(card);
                }
            }
        }
    }

    /// <summary>
    /// Alza la Note del comandante più basso del giocatore verso quella del più alto (Tutor).
    /// </summary>
    public sealed class EqualizeNotesChange : IGameChange, IPositivePlayerCardChange
    {
        private readonly PlayerState _player;
        private readonly int _maxAmount;

        /// <inheritdoc/>
        PlayerState IPositivePlayerCardChange.BeneficiaryPlayer => _player;

        /// <summary>Crea la modifica di pareggiamento note.</summary>
        /// <param name="player">Giocatore proprietario dei comandanti.</param>
        /// <param name="maxAmount">Aumento massimo applicabile al comandante più basso.</param>
        public EqualizeNotesChange(PlayerState player, int maxAmount)
        {
            _player = player;
            _maxAmount = maxAmount;
        }

        /// <inheritdoc/>
        public void Apply()
        {
            CommanderState[] cs = _player.Commanders;
            if (cs == null || cs.Length < 2)
            {
                return;
            }

            CommanderState lowest = cs[0].CurrentNote <= cs[1].CurrentNote ? cs[0] : cs[1];
            CommanderState highest = lowest == cs[0] ? cs[1] : cs[0];

            int gap = highest.CurrentNote - lowest.CurrentNote;
            int amount = Math.Min(_maxAmount, gap);
            if (amount > 0)
            {
                lowest.ApplyInstantDelta(amount);
                EventBus.Publish(new NoteChangedEvent(lowest));
                EventBus.Publish(new NoteIncreasedEvent(lowest, amount));
            }
        }
    }

    /// <summary>
    /// Scambia le Note correnti tra due comandanti (Rappresentante di Classe).
    /// </summary>
    public sealed class SwapNotesChange : IGameChange
    {
        private readonly CommanderState _a;
        private readonly CommanderState _b;

        /// <summary>Crea la modifica di scambio note.</summary>
        /// <param name="a">Primo comandante.</param>
        /// <param name="b">Secondo comandante.</param>
        public SwapNotesChange(CommanderState a, CommanderState b)
        {
            _a = a;
            _b = b;
        }

        /// <inheritdoc/>
        public void Apply()
        {
            if (_a == null || _b == null)
            {
                return;
            }

            int noteA = _a.CurrentNote;
            int noteB = _b.CurrentNote;
            _a.ApplyInstantDelta(noteB - noteA);
            _b.ApplyInstantDelta(noteA - noteB);
            EventBus.Publish(new NoteChangedEvent(_a));
            EventBus.Publish(new NoteChangedEvent(_b));
            // Pubblica NoteIncreasedEvent per chi ha guadagnato Note (trigger passive Inglese/Storia).
            if (noteB > noteA) EventBus.Publish(new NoteIncreasedEvent(_a, noteB - noteA));
            if (noteA > noteB) EventBus.Publish(new NoteIncreasedEvent(_b, noteA - noteB));
        }
    }

    /// <summary>
    /// Destinazione di una carta recuperata dal cimitero.
    /// </summary>
    public enum ReturnDestination
    {
        /// <summary>In mano al giocatore.</summary>
        Hand,

        /// <summary>In cima al mazzo del giocatore.</summary>
        DeckTop
    }

    /// <summary>
    /// Recupera N carte casuali dal cimitero del giocatore in mano o in cima al mazzo (Schema, Compito a Casa).
    /// </summary>
    public sealed class ReturnFromDiscardChange : IGameChange, IPositivePlayerCardChange
    {
        private readonly PlayerState _player;
        private readonly int _count;
        private readonly ReturnDestination _destination;

        /// <inheritdoc/>
        PlayerState IPositivePlayerCardChange.BeneficiaryPlayer => _player;

        /// <summary>Crea la modifica di recupero dal cimitero.</summary>
        /// <param name="player">Giocatore proprietario del cimitero.</param>
        /// <param name="count">Numero di carte da recuperare.</param>
        /// <param name="destination">Dove collocare le carte recuperate.</param>
        public ReturnFromDiscardChange(PlayerState player, int count, ReturnDestination destination)
        {
            _player = player;
            _count = count;
            _destination = destination;
        }

        /// <inheritdoc/>
        public void Apply()
        {
            int toReturn = Math.Min(_count, _player.DiscardPile.Count);
            for (int i = 0; i < toReturn; i++)
            {
                // La carta recuperata è estratta a caso dal cimitero (non sempre l'ultima).
                int randomIndex = UnityEngine.Random.Range(0, _player.DiscardPile.Count);
                CardDataSO card = _player.DiscardPile[randomIndex];
                _player.DiscardPile.RemoveAt(randomIndex);

                if (_destination == ReturnDestination.Hand)
                    _player.Hand.Add(card);
                else
                    _player.Deck.Add(card); // la cima del mazzo è l'ultimo elemento
            }
        }
    }

    /// <summary>
    /// Aggiunge uno scudo anti-debuff a uno o più comandanti (Dialogo).
    /// </summary>
    public sealed class AddShieldChange : IGameChange
    {
        private readonly CommanderState _target;

        /// <summary>Crea la modifica di aggiunta scudo.</summary>
        /// <param name="target">Comandante a cui aggiungere lo scudo.</param>
        public AddShieldChange(CommanderState target)
        {
            _target = target;
        }

        /// <inheritdoc/>
        public void Apply()
        {
            _target.AddDebuffShield();
        }
    }

    /// <summary>
    /// Blocca il calo di Note di un comandante fino al prossimo turno del proprietario (Fidanzata).
    /// </summary>
    public sealed class SetImmunityChange : IGameChange
    {
        private readonly CommanderState _target;

        /// <summary>Crea la modifica di immunità.</summary>
        /// <param name="target">Comandante reso immune al calo di Note.</param>
        public SetImmunityChange(CommanderState target)
        {
            _target = target;
        }

        /// <inheritdoc/>
        public void Apply()
        {
            _target.SetNoteFloorLocked(true);
        }
    }

    /// <summary>
    /// Attiva il flag "copia prossima carta": il prossimo effetto giocato in questo turno
    /// viene riapplicato una seconda volta (Copiare).
    /// </summary>
    public sealed class SetCopyNextCardChange : IGameChange
    {
        private readonly TurnManager _turns;

        /// <summary>Crea la modifica di attivazione copia.</summary>
        /// <param name="turns">Gestore dei turni su cui attivare il flag.</param>
        public SetCopyNextCardChange(TurnManager turns)
        {
            _turns = turns;
        }

        /// <inheritdoc/>
        public void Apply()
        {
            _turns?.ActivateCopyNextCard();
        }
    }

    /// <summary>
    /// Impedisce a un giocatore di giocare la Verifica nel suo prossimo turno (Sciopero).
    /// </summary>
    public sealed class BlockVerificaChange : IGameChange
    {
        private readonly PlayerState _player;

        /// <summary>Crea la modifica di blocco Verifica.</summary>
        /// <param name="player">Giocatore a cui bloccare la Verifica.</param>
        public BlockVerificaChange(PlayerState player)
        {
            _player = player;
        }

        /// <inheritdoc/>
        public void Apply()
        {
            _player.VerificaBlocked = true;
        }
    }

    /// <summary>
    /// Sposta la carta Verifica di un giocatore in fondo al suo mazzo (Occupazione).
    /// </summary>
    public sealed class MoveVerificaToDeckBottomChange : IGameChange
    {
        private readonly PlayerState _player;

        /// <summary>Crea la modifica che sposta la Verifica in fondo al mazzo.</summary>
        /// <param name="player">Giocatore proprietario della Verifica.</param>
        public MoveVerificaToDeckBottomChange(PlayerState player)
        {
            _player = player;
        }

        /// <inheritdoc/>
        public void Apply()
        {
            if (_player == null)
            {
                return;
            }

            CardDataSO verifica = RemoveVerifica(_player.Hand)
                ?? RemoveVerifica(_player.Deck)
                ?? RemoveVerifica(_player.DiscardPile);

            if (verifica != null)
            {
                _player.Deck.Insert(GameConstants.DeckBottomIndex, verifica);
            }
        }

        /// <summary>Rimuove e restituisce la Verifica dalla raccolta indicata.</summary>
        /// <param name="cards">Raccolta in cui cercare.</param>
        /// <returns>La Verifica rimossa, oppure null se assente.</returns>
        private static CardDataSO RemoveVerifica(List<CardDataSO> cards)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                CardDataSO card = cards[i];
                if (card != null && card.IsVerifica)
                {
                    cards.RemoveAt(i);
                    return card;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Attiva lo scudo di intercettazione Wikipedia: la prossima carta giocata dall'avversario
    /// (esclusa la Verifica) verrà copiata nella mano del proprietario dello scudo.
    /// </summary>
    public sealed class ActivateWikipediaInterceptChange : IGameChange
    {
        private readonly PlayerState _targetPlayer;

        /// <summary>Crea la modifica di attivazione dello scudo Wikipedia.</summary>
        /// <param name="targetPlayer">Giocatore su cui attivare lo scudo (chi ha giocato Wikipedia, non l'avversario).</param>
        public ActivateWikipediaInterceptChange(PlayerState targetPlayer)
        {
            _targetPlayer = targetPlayer;
        }

        /// <inheritdoc/>
        public void Apply()
        {
            if (_targetPlayer != null)
            {
                _targetPlayer.WikipediaInterceptActive = true;
            }
        }
    }

    /// <summary>
    /// Attiva la protezione di Costituzione sul giocatore fino all'inizio del suo prossimo turno.
    /// </summary>
    public sealed class SetConstitutionProtectionChange : IGameChange
    {
        private readonly PlayerState _player;

        /// <summary>Crea la modifica di attivazione della protezione.</summary>
        /// <param name="player">Giocatore protetto.</param>
        public SetConstitutionProtectionChange(PlayerState player)
        {
            _player = player;
        }

        /// <inheritdoc/>
        public void Apply()
        {
            if (_player != null)
            {
                _player.ConstitutionProtectionActive = true;
            }
        }
    }

    /// <summary>
    /// Operazioni condivise sul mazzo, usate da più modifiche di pesca.
    /// </summary>
    internal static class DeckOps
    {
        /// <summary>
        /// Pesca fino a <paramref name="count"/> carte dalla cima del mazzo alla mano.
        /// </summary>
        /// <param name="player">Giocatore che pesca.</param>
        /// <param name="count">Numero di carte da pescare (clampato al mazzo, minimo 0).</param>
        /// <param name="publishEvent">Se false, non pubblica <see cref="CardsDrawnEvent"/> (carte non contano come pescate).</param>
        internal static void DrawTopToHand(PlayerState player, int count, bool publishEvent = true)
        {
            int drawable = Math.Min(Math.Max(count, 0), player.Deck.Count);
            for (int i = 0; i < drawable; i++)
            {
                int topIndex = player.Deck.Count - 1;
                CardDataSO card = player.Deck[topIndex];
                player.Deck.RemoveAt(topIndex);
                player.Hand.Add(card);
            }

            if (drawable > 0 && publishEvent)
            {
                EventBus.Publish(new CardsDrawnEvent(player, drawable));
            }
        }
    }
}
