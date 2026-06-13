using System;
using System.Collections.Generic;
using UnityEngine;
using FourE.Cards;
using FourE.Commanders;
using FourE.Events;
using FourE.Players;

namespace FourE.Core
{
    /// <summary>
    /// Concede o sottrae azioni giocabili nel turno corrente.
    /// </summary>
    public sealed class GrantActionsChange : IGameChange
    {
        private readonly TurnManager _turns;
        private readonly int _amount;

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
    /// Raddoppia le azioni ancora disponibili nel turno corrente (Copiare).
    /// </summary>
    public sealed class DoubleActionsChange : IGameChange
    {
        private readonly TurnManager _turns;

        /// <summary>Crea la modifica di raddoppio azioni.</summary>
        /// <param name="turns">Gestore dei turni su cui agire.</param>
        public DoubleActionsChange(TurnManager turns)
        {
            _turns = turns;
        }

        /// <inheritdoc/>
        public void Apply()
        {
            _turns?.DoubleRemainingActions();
        }
    }

    /// <summary>
    /// Fa pescare a un giocatore fino a raggiungere una dimensione di mano (Biblioteca).
    /// </summary>
    public sealed class DrawToHandSizeChange : IGameChange
    {
        private readonly PlayerState _player;
        private readonly int _targetHandSize;

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
    public sealed class DrawAllChange : IGameChange
    {
        private readonly PlayerState _player;

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
    /// Fa scartare a un giocatore N carte casuali dalla mano (Gossip). La Verifica non è in mano.
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
            int toDiscard = Math.Min(_count, _player.Hand.Count);
            for (int i = 0; i < toDiscard; i++)
            {
                int index = UnityEngine.Random.Range(0, _player.Hand.Count);
                CardDataSO card = _player.Hand[index];
                _player.Hand.RemoveAt(index);
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
    public sealed class EqualizeNotesChange : IGameChange
    {
        private readonly PlayerState _player;
        private readonly int _maxAmount;

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
    /// Recupera N carte dal cimitero del giocatore (Schema, Compito a Casa).
    /// </summary>
    public sealed class ReturnFromDiscardChange : IGameChange
    {
        private readonly PlayerState _player;
        private readonly int _count;
        private readonly ReturnDestination _destination;

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
                int topIndex = _player.DiscardPile.Count - 1;
                CardDataSO card = _player.DiscardPile[topIndex];
                _player.DiscardPile.RemoveAt(topIndex);

                if (_destination == ReturnDestination.Hand)
                {
                    _player.Hand.Add(card);
                }
                else
                {
                    _player.Deck.Add(card); // la cima del mazzo è l'ultimo elemento
                }
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
    /// Ritorna una carta dalla risoluzione in mano al giocatore (Test di Cooper).
    /// Rimuove la carta dall'ultimo posto nel cimitero e la aggiunge alla mano.
    /// </summary>
    public sealed class ReturnCardToHandChange : IGameChange
    {
        private readonly PlayerState _player;
        private readonly CardDataSO _card;

        /// <summary>Crea la modifica di ritorno della carta in mano.</summary>
        /// <param name="player">Giocatore a cui ritornare la carta.</param>
        /// <param name="card">Carta da ritornare in mano.</param>
        public ReturnCardToHandChange(PlayerState player, CardDataSO card)
        {
            _player = player;
            _card = card;
        }

        /// <inheritdoc/>
        public void Apply()
        {
            if (_player?.DiscardPile != null && _player.DiscardPile.Count > 0)
            {
                int lastIndex = _player.DiscardPile.Count - 1;
                CardDataSO lastCard = _player.DiscardPile[lastIndex];
                if (lastCard == _card)
                {
                    _player.DiscardPile.RemoveAt(lastIndex);
                    _player.Hand.Add(_card);
                }
            }
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
        /// <param name="targetPlayer">Giocatore su cui attivare lo scudo (avversario di chi ha giocato Wikipedia).</param>
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
    /// Operazioni condivise sul mazzo, usate da più modifiche di pesca.
    /// </summary>
    internal static class DeckOps
    {
        /// <summary>
        /// Pesca fino a <paramref name="count"/> carte dalla cima del mazzo alla mano.
        /// </summary>
        /// <param name="player">Giocatore che pesca.</param>
        /// <param name="count">Numero di carte da pescare (clampato al mazzo, minimo 0).</param>
        internal static void DrawTopToHand(PlayerState player, int count)
        {
            int drawable = Math.Min(Math.Max(count, 0), player.Deck.Count);
            for (int i = 0; i < drawable; i++)
            {
                int topIndex = player.Deck.Count - 1;
                CardDataSO card = player.Deck[topIndex];
                player.Deck.RemoveAt(topIndex);
                player.Hand.Add(card);
            }

            if (drawable > 0)
            {
                EventBus.Publish(new CardsDrawnEvent(player, drawable));
            }
        }
    }
}
