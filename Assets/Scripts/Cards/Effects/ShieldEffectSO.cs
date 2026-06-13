using UnityEngine;
using FourE.Commanders;
using FourE.Core;

namespace FourE.Cards.Effects
{
    /// <summary>
    /// Aggiunge uno scudo anti-debuff ai comandanti bersaglio: ogni scudo annulla
    /// il prossimo debuff ricevuto (Dialogo).
    /// </summary>
    [CreateAssetMenu(fileName = "ShieldEffect", menuName = "4E/Effects/Shield", order = 10)]
    public sealed class ShieldEffectSO : CardEffectSO
    {
        /// <inheritdoc/>
        public override void Apply(GameContext context)
        {
            foreach (CommanderState commander in context.ResolveCommanders(Target))
            {
                context.RegisterChange(new AddShieldChange(commander));
            }
        }
    }
}
