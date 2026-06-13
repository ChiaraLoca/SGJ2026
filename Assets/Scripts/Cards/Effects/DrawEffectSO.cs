using UnityEngine;
using FourE.Core;
using FourE.Players;

namespace FourE.Cards.Effects
{
    /// <summary>
    /// Fa pescare N carte al giocatore bersaglio (di norma il giocatore attivo).
    /// </summary>
    [CreateAssetMenu(fileName = "DrawEffect", menuName = "4E/Effects/Draw", order = 2)]
    public sealed class DrawEffectSO : CardEffectSO
    {
        [SerializeField] private int _cardCount = 1;

        /// <inheritdoc/>
        public override void Apply(GameContext context)
        {
            // Se il bersaglio non è un giocatore, la pesca ricade sul giocatore attivo.
            PlayerState player = context.ResolvePlayer(Target) ?? context.ActivePlayer;
            context.RegisterChange(new DrawCardsChange(player, _cardCount));
        }
    }
}
