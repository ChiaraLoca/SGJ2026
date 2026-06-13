using UnityEngine;
using FourE.Commanders;
using FourE.Core;

namespace FourE.Cards.Effects
{
    /// <summary>
    /// Riduce la Note di uno o più comandanti, in modo istantaneo o a durata.
    /// </summary>
    [CreateAssetMenu(fileName = "DebuffEffect", menuName = "4E/Effects/Debuff", order = 1)]
    public sealed class DebuffEffectSO : CardEffectSO
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
                    // Debuff istantaneo: delta negativo sulla Note.
                    context.RegisterChange(new InstantNoteChange(commander, -_magnitude));
                }
                else
                {
                    ActiveEffect effect = new(name, _magnitude, _duration, _durationTurns);
                    context.RegisterChange(new AddActiveEffectChange(commander, effect, isBuff: false));
                }
            }
        }
    }
}
