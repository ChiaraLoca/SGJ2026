using UnityEngine;
using FourE.Core;

namespace FourE.Cards.Effects
{
    /// <summary>
    /// Attiva la protezione di Costituzione fino all'inizio del prossimo turno:
    /// le carte avversarie non possono concedere Note positive o carte al proprietario.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ConstitutionProtectionEffect",
        menuName = "4E/Effects/Constitution Protection",
        order = 15)]
    public sealed class ConstitutionProtectionEffectSO : CardEffectSO
    {
        /// <inheritdoc/>
        public override void Apply(GameContext context)
        {
            context.RegisterChange(new SetConstitutionProtectionChange(context.ActivePlayer));
        }
    }
}
