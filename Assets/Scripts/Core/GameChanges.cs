using System;
using FourE.Cards;
using FourE.Commanders;
using FourE.Events;
using FourE.Players;

namespace FourE.Core
{
    /// <summary>
    /// Modifica istantanea alla Note di un comandante. Pubblica <see cref="NoteChangedEvent"/>.
    /// </summary>
    public sealed class InstantNoteChange : IGameChange
    {
        private readonly CommanderState _target;
        private readonly int _delta;

        /// <summary>
        /// Crea la modifica istantanea.
        /// </summary>
        /// <param name="target">Comandante bersaglio.</param>
        /// <param name="delta">Variazione con segno alla Note.</param>
        public InstantNoteChange(CommanderState target, int delta)
        {
            _target = target;
            _delta = delta;
        }

        /// <inheritdoc/>
        public void Apply()
        {
            _target.ApplyInstantDelta(_delta);
            EventBus.Publish(new NoteChangedEvent(_target));

            // Aumento istantaneo: notifica le passive reattive (Inglese).
            if (_delta > 0)
            {
                EventBus.Publish(new NoteIncreasedEvent(_target, _delta));
            }
        }
    }

    /// <summary>
    /// Aggiunge un effetto a durata (buff o debuff) a un comandante.
    /// </summary>
    public sealed class AddActiveEffectChange : IGameChange
    {
        private readonly CommanderState _target;
        private readonly ActiveEffect _effect;
        private readonly bool _isBuff;

        /// <summary>
        /// Crea la modifica di aggiunta effetto a durata.
        /// </summary>
        /// <param name="target">Comandante bersaglio.</param>
        /// <param name="effect">Effetto attivo da registrare.</param>
        /// <param name="isBuff">True se è un buff, false se è un debuff.</param>
        public AddActiveEffectChange(CommanderState target, ActiveEffect effect, bool isBuff)
        {
            _target = target;
            _effect = effect;
            _isBuff = isBuff;
        }

        /// <inheritdoc/>
        public void Apply()
        {
            if (_isBuff)
            {
                _target.AddBuff(_effect);
            }
            else
            {
                _target.AddDebuff(_effect);
            }

            EventBus.Publish(new NoteChangedEvent(_target));
        }
    }

    /// <summary>
    /// Pesca un numero di carte dal mazzo di un giocatore alla sua mano.
    /// </summary>
    public sealed class DrawCardsChange : IGameChange
    {
        private readonly PlayerState _player;
        private readonly int _count;

        /// <summary>
        /// Crea la modifica di pesca.
        /// </summary>
        /// <param name="player">Giocatore che pesca.</param>
        /// <param name="count">Numero di carte da pescare.</param>
        public DrawCardsChange(PlayerState player, int count)
        {
            _player = player;
            _count = count;
        }

        /// <inheritdoc/>
        public void Apply()
        {
            int drawable = Math.Min(_count, _player.Deck.Count);
            for (int i = 0; i < drawable; i++)
            {
                int topIndex = _player.Deck.Count - 1;
                CardDataSO card = _player.Deck[topIndex];
                _player.Deck.RemoveAt(topIndex);
                _player.Hand.Add(card);
            }

            if (drawable > 0)
            {
                EventBus.Publish(new CardsDrawnEvent(_player, drawable));
            }
        }
    }
}
