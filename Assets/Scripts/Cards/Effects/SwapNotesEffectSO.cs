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
            // Richiede un comandante avversario e uno proprio (SelectedOwnAndEnemy).
            if (context.SelectedEnemyTargets.Count < 1 || context.SelectedOwnTargets.Count < 1)
            {
                return;
            }

            CommanderState enemy = context.SelectedEnemyTargets[0];
            CommanderState own = context.SelectedOwnTargets[0];
            context.RegisterChange(new SwapNotesChange(enemy, own));
        }
    }
}
