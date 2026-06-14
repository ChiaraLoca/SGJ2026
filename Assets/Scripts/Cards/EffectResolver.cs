using FourE.Core;
using FourE.Events;

namespace FourE.Cards
{
    /// <summary>
    /// Risolve gli effetti di una carta sul <see cref="GameContext"/>.
    /// Applica e committa le modifiche dopo ogni effetto, così che gli effetti
    /// successivi (e le condizioni) leggano sempre stato aggiornato.
    /// </summary>
    public sealed class EffectResolver
    {
        /// <summary>
        /// Risolve in sequenza tutti gli effetti di una carta standard.
        /// </summary>
        /// <param name="card">Carta giocata di cui applicare gli effetti.</param>
        /// <param name="context">Contesto con lo stato completo del gioco.</param>
        public void Resolve(CardDataSO card, GameContext context)
        {
            if (card == null || card.Effects == null)
            {
                return;
            }

            // Espone la carta e la sua affinità agli effetti.
            context.Card = card;
            context.SetSourceAffinity(card.Affinity);

            // Notifica le passive reattive prima di applicare gli effetti (es. Storia/Inglese secondaria).
            EventBus.Publish(new CardResolvingEvent(card, context.ActivePlayer, context));

            foreach (CardEffectSO effect in card.Effects)
            {
                if (effect == null)
                {
                    continue;
                }

                effect.Apply(context);
                context.CommitChanges();
            }

            EventBus.Publish(new CardPlayedEvent(card, context.ActivePlayer));
        }
    }
}
