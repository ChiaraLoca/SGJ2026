using UnityEngine;
using FourE.Commanders;
using FourE.Core;

namespace FourE.Cards.Effects
{
    /// <summary>
    /// Scambia le Note correnti tra due comandanti scelti a runtime (Rappresentante di Classe).
    /// Richiede due bersagli in <see cref="GameContext.SelectedTargets"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "SwapNotesEffect", menuName = "4E/Effects/Swap Notes", order = 8)]
    public sealed class SwapNotesEffectSO : CardEffectSO
    {
        /// <inheritdoc/>
        public override void Apply(GameContext context)
        {
            if (context.SelectedTargets.Count < 2)
            {
                return;
            }

            CommanderState a = context.SelectedTargets[0];
            CommanderState b = context.SelectedTargets[1];
            context.RegisterChange(new SwapNotesChange(a, b));
        }
    }
}
