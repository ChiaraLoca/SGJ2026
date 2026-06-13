using System;
using System.Collections.Generic;
using FourE.Cards;
using FourE.Core;

namespace FourE.Shop
{
    /// <summary>
    /// Logica di generazione e refresh del pool shop personale, filtrato per Credits.
    /// </summary>
    public static class ShopPool
    {
        /// <summary>
        /// Genera un pool iniziale di carte casuali idonee ai Credits del giocatore.
        /// </summary>
        /// <param name="catalog">Catalogo completo delle carte acquistabili.</param>
        /// <param name="credits">Credits attuali del giocatore.</param>
        /// <param name="size">Dimensione massima del pool.</param>
        /// <param name="rng">Generatore casuale host-authoritative.</param>
        /// <returns>Lista di carte selezionate per il pool.</returns>
        public static List<CardDataSO> GeneratePool(IReadOnlyList<CardDataSO> catalog, int credits, int size, Random rng)
        {
            List<CardDataSO> eligible = FilterByCredits(catalog, credits);
            CollectionUtils.Shuffle(eligible, rng);

            int take = Math.Min(size, eligible.Count);
            return eligible.GetRange(0, take);
        }

        /// <summary>
        /// Sostituisce alcune carte del pool con nuove carte idonee, fino alla dimensione target.
        /// Tiene conto dei Credits aggiornati, così le carte sbloccate possono comparire.
        /// </summary>
        /// <param name="pool">Pool corrente da modificare in place.</param>
        /// <param name="catalog">Catalogo completo delle carte.</param>
        /// <param name="credits">Credits aggiornati del giocatore.</param>
        /// <param name="refreshCount">Numero di slot da sostituire.</param>
        /// <param name="poolSize">Dimensione target del pool.</param>
        /// <param name="rng">Generatore casuale host-authoritative.</param>
        public static void RefreshSlots(List<CardDataSO> pool, IReadOnlyList<CardDataSO> catalog, int credits, int refreshCount, int poolSize, Random rng)
        {
            int toRemove = Math.Min(refreshCount, pool.Count);
            for (int i = 0; i < toRemove; i++)
            {
                pool.RemoveAt(rng.Next(pool.Count));
            }

            List<CardDataSO> candidates = new();
            foreach (CardDataSO card in catalog)
            {
                if (card != null && card.MinCreditsRequired <= credits && !pool.Contains(card))
                {
                    candidates.Add(card);
                }
            }

            CollectionUtils.Shuffle(candidates, rng);

            int freeSlots = poolSize - pool.Count;
            int toAdd = Math.Min(freeSlots, candidates.Count);
            for (int i = 0; i < toAdd; i++)
            {
                pool.Add(candidates[i]);
            }
        }

        /// <summary>
        /// Restituisce le carte del catalogo idonee alla soglia di Credits.
        /// </summary>
        /// <param name="catalog">Catalogo completo.</param>
        /// <param name="credits">Credits attuali.</param>
        /// <returns>Lista delle carte con soglia soddisfatta.</returns>
        private static List<CardDataSO> FilterByCredits(IReadOnlyList<CardDataSO> catalog, int credits)
        {
            List<CardDataSO> result = new();
            foreach (CardDataSO card in catalog)
            {
                if (card != null && card.MinCreditsRequired <= credits)
                {
                    result.Add(card);
                }
            }

            return result;
        }
    }
}
