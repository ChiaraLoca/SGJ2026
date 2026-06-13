using UnityEngine;
using FourE.Commanders;
using FourE.Core;

namespace FourE.Cards.Conditions
{
    /// <summary>
    /// Soddisfatta se almeno un comandante bersaglio ha un debuff attivo.
    /// </summary>
    [CreateAssetMenu(fileName = "CommanderHasDebuff", menuName = "4E/Conditions/Commander Has Debuff", order = 2)]
    public sealed class CommanderHasDebuffConditionSO : CardConditionSO
    {
        [SerializeField] private EffectTarget _target;

        /// <inheritdoc/>
        public override bool IsMet(GameContext context)
        {
            foreach (CommanderState commander in context.ResolveCommanders(_target))
            {
                if (commander.HasActiveDebuff)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
