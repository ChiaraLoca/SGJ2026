using UnityEngine;
using FourE.Core;

namespace FourE.Cards.Effects
{
    /// <summary>
    /// Recupera carte dal cimitero del giocatore attivo, mettendole in mano o in cima al mazzo
    /// (Schema, Compito a Casa).
    /// </summary>
    [CreateAssetMenu(fileName = "ReturnFromDiscardEffect", menuName = "4E/Effects/Return From Discard", order = 9)]
    public sealed class ReturnFromDiscardEffectSO : CardEffectSO
    {
        [SerializeField] private int _count = 1;
        [SerializeField] private ReturnDestination _destination = ReturnDestination.Hand;

        /// <inheritdoc/>
        public override void Apply(GameContext context)
        {
            context.RegisterChange(new ReturnFromDiscardChange(context.ActivePlayer, _count, _destination));
        }
    }
}
