using UnityEngine;
using FourE.Core;

namespace FourE.Cards
{
    /// <summary>
    /// Base astratta delle condizioni valutabili da un effetto condizionale.
    /// Ogni condizione concreta è uno ScriptableObject separato.
    /// </summary>
    public abstract class CardConditionSO : ScriptableObject
    {
        /// <summary>
        /// Valuta la condizione rispetto al contesto di gioco corrente.
        /// </summary>
        /// <param name="context">Contesto contenente lo stato completo del gioco.</param>
        /// <returns>True se la condizione è soddisfatta.</returns>
        public abstract bool IsMet(GameContext context);
    }
}
