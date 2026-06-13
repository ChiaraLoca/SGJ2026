using UnityEngine;
using FourE.Core;

namespace FourE.Cards.Effects
{
    /// <summary>
    /// Attiva uno scudo di intercettazione: la prossima carta giocata dall'avversario
    /// (esclusa la Verifica) viene copiata nella mano del giocatore attivo.
    /// </summary>
    [CreateAssetMenu(fileName = "WikipediaEffect", menuName = "4E/Effects/Wikipedia", order = 6)]
    public sealed class WikipediaEffectSO : CardEffectSO
    {
        /// <inheritdoc/>
        public override void Apply(GameContext context)
        {
            if (context.InactivePlayer == null)
            {
                return;
            }

            // Attiva lo scudo di intercettazione sull'avversario.
            context.RegisterChange(new ActivateWikipediaInterceptChange(context.InactivePlayer));
        }
    }
}
