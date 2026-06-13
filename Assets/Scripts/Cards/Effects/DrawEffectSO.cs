using UnityEngine;
using FourE.Core;
using FourE.Players;

namespace FourE.Cards.Effects
{
    /// <summary>
    /// Modalità di pesca di un <see cref="DrawEffectSO"/>.
    /// </summary>
    public enum DrawMode
    {
        /// <summary>Pesca un numero fisso di carte.</summary>
        FixedCount,

        /// <summary>Pesca fino a raggiungere una dimensione di mano (Biblioteca).</summary>
        ToHandSize,

        /// <summary>Pesca l'intero mazzo (Approfondimento).</summary>
        EntireDeck
    }

    /// <summary>
    /// Fa pescare carte al giocatore bersaglio (di norma il giocatore attivo),
    /// con conteggio fisso, fino a una dimensione di mano, o l'intero mazzo.
    /// </summary>
    [CreateAssetMenu(fileName = "DrawEffect", menuName = "4E/Effects/Draw", order = 2)]
    public sealed class DrawEffectSO : CardEffectSO
    {
        [SerializeField] private DrawMode _mode = DrawMode.FixedCount;
        [SerializeField] private int _cardCount = 1;
        [SerializeField] private int _targetHandSize = 5;

        /// <inheritdoc/>
        public override void Apply(GameContext context)
        {
            // Se il bersaglio non è un giocatore, la pesca ricade sul giocatore attivo.
            PlayerState player = context.ResolvePlayer(Target) ?? context.ActivePlayer;

            switch (_mode)
            {
                case DrawMode.ToHandSize:
                    context.RegisterChange(new DrawToHandSizeChange(player, _targetHandSize));
                    break;
                case DrawMode.EntireDeck:
                    context.RegisterChange(new DrawAllChange(player));
                    break;
                default:
                    context.RegisterChange(new DrawCardsChange(player, _cardCount));
                    break;
            }
        }
    }
}
