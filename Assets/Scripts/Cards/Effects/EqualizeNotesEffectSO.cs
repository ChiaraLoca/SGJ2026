using UnityEngine;
using FourE.Core;

namespace FourE.Cards.Effects
{
    /// <summary>
    /// Alza la Note del comandante più basso del giocatore attivo verso quella del più alto,
    /// fino a un massimo configurato (Tutor).
    /// </summary>
    [CreateAssetMenu(fileName = "EqualizeNotesEffect", menuName = "4E/Effects/Equalize Notes", order = 7)]
    public sealed class EqualizeNotesEffectSO : CardEffectSO
    {
        [Tooltip("Aumento massimo applicabile al comandante con Note più bassa.")]
        [SerializeField] private int _maxAmount = 1;

        /// <inheritdoc/>
        public override void Apply(GameContext context)
        {
            if (context.ActivePlayer == null)
            {
                return;
            }

            // Tutor opera sempre e soltanto sulla coppia del giocatore che lo ha giocato.
            context.RegisterChange(new EqualizeNotesChange(context.ActivePlayer, _maxAmount));
        }
    }
}
