using UnityEngine;
using FourE.Commanders;
using FourE.Core;

namespace FourE.Cards.Effects
{
    /// <summary>
    /// Aumenta la Note di uno o più comandanti, in modo istantaneo o a durata.
    /// </summary>
    [CreateAssetMenu(fileName = "BuffEffect", menuName = "4E/Effects/Buff", order = 0)]
    public sealed class BuffEffectSO : CardEffectSO
    {
        [SerializeField] private int _magnitude = 1;
        [SerializeField] private EffectDuration _duration = EffectDuration.Instant;
        [SerializeField] private int _durationTurns = 1;

        /// <inheritdoc/>
        public override void Apply(GameContext context)
        {
            foreach (CommanderState commander in context.ResolveCommanders(Target))
            {
                if (_duration == EffectDuration.Instant)
                {
                    context.RegisterChange(new InstantNoteChange(commander, _magnitude));
                }
                else
                {
                    ActiveEffect effect = new(name, _magnitude, _duration, _durationTurns);
                    context.RegisterChange(new AddActiveEffectChange(commander, effect, isBuff: true));
                }
            }
        }
    }
}
