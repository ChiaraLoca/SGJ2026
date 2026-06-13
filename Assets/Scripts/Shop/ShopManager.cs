using System;
using System.Collections.Generic;
using FourE.Cards;
using FourE.Core;
using FourE.Events;
using FourE.Players;

namespace FourE.Shop
{
    /// <summary>
    /// Gestisce gli acquisti dallo shop e il refresh del pool durante la Fase SHOP.
    /// Il costo è pagato in Crediti (valuta permanente); le carte acquistate sono neutre.
    /// </summary>
    [System.Serializable]
    public sealed class ShopManager
    {
        private readonly GameStateManager _state;
        private readonly Random _rng;
        private readonly Dictionary<int, int> _purchasesByActor = new();

        /// <summary>
        /// Crea il gestore dello shop.
        /// </summary>
        /// <param name="state">Riferimento allo stato di gioco.</param>
        /// <param name="rng">Generatore casuale host-authoritative.</param>
        public ShopManager(GameStateManager state, Random rng)
        {
            _state = state;
            _rng = rng;
        }

        /// <summary>
        /// Tenta l'acquisto di una carta dal pool del giocatore.
        /// </summary>
        /// <param name="player">Giocatore acquirente.</param>
        /// <param name="card">Carta da acquistare.</param>
        /// <returns>True se l'acquisto è andato a buon fine.</returns>
        public bool TryPurchase(PlayerState player, CardDataSO card)
        {
            if (card == null || !player.ShopPool.Contains(card))
            {
                return false;
            }

            int purchasesMade = GetPurchases(player.ActorNumber);
            if (purchasesMade >= _state.GameConfig.ShopPurchasesPerRound)
            {
                return false;
            }

            if (player.Credits < card.ShopCost)
            {
                return false;
            }

            player.SpendCredits(card.ShopCost);
            player.ShopPool.Remove(card);
            // La carta acquistata entra negli scarti: verrà rimischiata nel mazzo alla Fase DRAW.
            player.DiscardPile.Add(card);
            _purchasesByActor[player.ActorNumber] = purchasesMade + 1;

            EventBus.Publish(new CardBoughtEvent(card, player));
            return true;
        }

        /// <summary>
        /// Rinfresca il pool shop del giocatore in base ai Credits aggiornati.
        /// </summary>
        /// <param name="player">Giocatore di cui rinfrescare il pool.</param>
        public void RefreshPool(PlayerState player)
        {
            ShopPool.RefreshSlots(
                player.ShopPool,
                _state.Content.ShopCatalog,
                player.Credits,
                _state.GameConfig.ShopRefreshSlots,
                _state.GameConfig.ShopPoolSize,
                _rng);
        }

        /// <summary>
        /// Azzera il conteggio acquisti, da chiamare all'ingresso di una nuova Fase SHOP.
        /// </summary>
        public void ResetPurchases()
        {
            _purchasesByActor.Clear();
        }

        /// <summary>
        /// Restituisce gli acquisti effettuati dal giocatore nel round corrente.
        /// </summary>
        /// <param name="actorNumber">Attore del giocatore.</param>
        /// <returns>Numero di acquisti già effettuati.</returns>
        private int GetPurchases(int actorNumber)
        {
            return _purchasesByActor.TryGetValue(actorNumber, out int count) ? count : 0;
        }
    }
}
