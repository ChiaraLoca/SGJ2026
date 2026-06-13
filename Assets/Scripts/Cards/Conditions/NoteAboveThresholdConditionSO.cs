using UnityEngine;
using FourE.Commanders;
using FourE.Core;

namespace FourE.Cards.Conditions
{
    /// <summary>
    /// Soddisfatta se almeno un comandante bersaglio ha la Note corrente sopra la soglia.
    /// </summary>
    [CreateAssetMenu(fileName = "NoteAboveThreshold", menuName = "4E/Conditions/Note Above Threshold", order = 0)]
    public sealed class NoteAboveThresholdConditionSO : CardConditionSO
    {
        [SerializeField] private EffectTarget _target;
        [SerializeField] private int _threshold;

        /// <inheritdoc/>
        public override bool IsMet(GameContext context)
        {
            foreach (CommanderState commander in context.ResolveCommanders(_target))
            {
                if (commander.CurrentNote > _threshold)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
