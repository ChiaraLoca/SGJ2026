using System;
using System.Collections.Generic;
using FourE.Cards;
using FourE.Commanders;
using FourE.Config;
using FourE.Players;
using FourE.Shop;

namespace FourE.Core
{
    /// <summary>
    /// Costruisce lo stato iniziale di un giocatore a partire dall'archivio contenuti.
    /// </summary>
    public static class MatchSetup
    {
        /// <summary>
        /// Crea lo stato di un giocatore: comandanti, mazzo mischiato, mano iniziale,
        /// carta Verifica e pool shop.
        /// </summary>
        /// <param name="actorNumber">Identificativo attore Photon del giocatore.</param>
        /// <param name="commanderData">Definizioni dei due comandanti del giocatore.</param>
        /// <param name="verificaCard">Carta Verifica da assegnare allo slot dedicato.</param>
        /// <param name="shopCatalog">Catalogo per generare il pool shop iniziale.</param>
        /// <param name="config">Configurazione di gioco.</param>
        /// <param name="rng">Generatore casuale host-authoritative.</param>
        /// <returns>Stato del giocatore pronto per la partita.</returns>
        public static PlayerState BuildPlayer(
            int actorNumber,
            IReadOnlyList<CommanderDataSO> commanderData,
            CardDataSO verificaCard,
            IReadOnlyList<CardDataSO> shopCatalog,
            GameConfigSO config,
            Random rng)
        {
            CommanderState[] commanders = new CommanderState[GameConstants.CommandersPerPlayer];
            PlayerState player = new(actorNumber, commanders);

            for (int i = 0; i < GameConstants.CommandersPerPlayer; i++)
            {
                CommanderDataSO data = commanderData[i];
                commanders[i] = new CommanderState(data);
                foreach (CardDataSO card in data.LinkedCards)
                {
                    player.Deck.Add(card);
                }
            }

            // La Verifica è una carta normale del mazzo (5 + 5 + 1 Verifica).
            if (verificaCard != null)
            {
                player.Deck.Add(verificaCard);
            }

            CollectionUtils.Shuffle(player.Deck, rng);
            DrawInitialHand(player, config.StartingHandSize);

            player.ShopPool.AddRange(ShopPool.GeneratePool(shopCatalog, player.Credits, config, rng));

            return player;
        }

        /// <summary>
        /// Pesca la mano iniziale dal mazzo già mischiato.
        /// </summary>
        /// <param name="player">Giocatore destinatario.</param>
        /// <param name="handSize">Numero di carte da pescare.</param>
        private static void DrawInitialHand(PlayerState player, int handSize)
        {
            int drawable = Math.Min(handSize, player.Deck.Count);
            for (int i = 0; i < drawable; i++)
            {
                int topIndex = player.Deck.Count - 1;
                CardDataSO card = player.Deck[topIndex];
                player.Deck.RemoveAt(topIndex);
                player.Hand.Add(card);
            }
        }
    }
}
