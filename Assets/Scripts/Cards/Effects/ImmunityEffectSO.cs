using UnityEngine;
using FourE.Commanders;
using FourE.Core;

namespace FourE.Cards.Effects
{
    /// <summary>
    /// Blocca il calo di Note dei comandanti bersaglio fino al prossimo turno del proprietario
    /// (Fidanzata). L'immunità decade in <c>TurnManager.StartTurn</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "ImmunityEffect", menuName = "4E/Effects/Immunity", order = 11)]
    public sealed class ImmunityEffectSO : CardEffectSO
    {
        /// <inheritdoc/>
        public override void Apply(GameContext context)
        {
            foreach (CommanderState commander in context.ResolveCommanders(Target))
            {
                context.RegisterChange(new SetImmunityChange(commander));
            }
        }
    }
}
