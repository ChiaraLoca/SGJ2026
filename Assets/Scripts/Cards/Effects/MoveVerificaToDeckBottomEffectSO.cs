using UnityEngine;
using FourE.Core;
using FourE.Players;

namespace FourE.Cards.Effects
{
    /// <summary>
    /// Sposta la carta Verifica del giocatore bersaglio in fondo al suo mazzo.
    /// </summary>
    [CreateAssetMenu(
        fileName = "MoveVerificaToDeckBottomEffect",
        menuName = "4E/Effects/Move Verifica To Deck Bottom",
        order = 13)]
    public sealed class MoveVerificaToDeckBottomEffectSO : CardEffectSO
    {
        /// <inheritdoc/>
        public override void Apply(GameContext context)
        {
            PlayerState player = context.ResolvePlayer(Target) ?? context.InactivePlayer;
            context.RegisterChange(new MoveVerificaToDeckBottomChange(player));
        }
    }
}
