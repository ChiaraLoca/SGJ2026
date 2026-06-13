using UnityEngine;
using FourE.Core;

namespace FourE.Cards.Conditions
{
    /// <summary>
    /// Soddisfatta se il giocatore attivo ha almeno N carte in mano.
    /// </summary>
    [CreateAssetMenu(fileName = "CardsInHand", menuName = "4E/Conditions/Cards In Hand", order = 1)]
    public sealed class CardsInHandConditionSO : CardConditionSO
    {
        [SerializeField] private int _minCards = 1;

        /// <inheritdoc/>
        public override bool IsMet(GameContext context)
        {
            return context.ActivePlayer.Hand.Count >= _minCards;
        }
    }
}
