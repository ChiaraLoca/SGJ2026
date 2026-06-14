using UnityEngine;
using FourE.Core;

namespace FourE.Cards.Effects
{
    /// <summary>
    /// Attiva il flag "copia prossima carta": la prossima carta giocata in questo turno
    /// viene riapplicata una seconda volta con gli stessi bersagli (Copiare).
    /// Se il turno termina prima che venga giocata un'altra carta, il flag decade.
    /// </summary>
    [CreateAssetMenu(fileName = "CopyNextCardEffect", menuName = "4E/Effects/Copy Next Card", order = 17)]
    public sealed class CopyNextCardEffectSO : CardEffectSO
    {
        /// <inheritdoc/>
        public override void Apply(GameContext context)
        {
            TurnManager turns = context.State?.Turns;
            if (turns == null)
            {
                return;
            }

            context.RegisterChange(new SetCopyNextCardChange(turns));
        }
    }
}
