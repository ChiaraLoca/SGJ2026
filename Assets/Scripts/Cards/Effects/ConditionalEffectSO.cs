using UnityEngine;
using FourE.Core;

namespace FourE.Cards.Effects
{
    /// <summary>
    /// Applica un effetto interno solo se la condizione associata è soddisfatta.
    /// Il bersaglio e la durata sono delegati all'effetto interno.
    /// </summary>
    [CreateAssetMenu(fileName = "ConditionalEffect", menuName = "4E/Effects/Conditional", order = 3)]
    public sealed class ConditionalEffectSO : CardEffectSO
    {
        [SerializeField] private CardConditionSO _condition;
        [SerializeField] private CardEffectSO _innerEffect;

        /// <inheritdoc/>
        public override void Apply(GameContext context)
        {
            if (_condition == null || _innerEffect == null)
            {
                return;
            }

            if (_condition.IsMet(context))
            {
                _innerEffect.Apply(context);
            }
        }
    }
}
