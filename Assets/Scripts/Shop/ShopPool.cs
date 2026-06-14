using System;
using System.Collections.Generic;
using FourE.Cards;
using FourE.Config;
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
        /// <param name="config">Configurazione della dimensione e delle quote per tier.</param>
        /// <param name="rng">Generatore casuale host-authoritative.</param>
        /// <returns>Lista di carte selezionate per il pool.</returns>
        public static List<CardDataSO> GeneratePool(
            IReadOnlyList<CardDataSO> catalog,
            int credits,
            GameConfigSO config,
            Random rng)
        {
            List<CardDataSO> eligible = FilterByCredits(catalog, credits);
            List<CardDataSO> pool = new();

            FillTierQuota(pool, eligible, CardTier.C, config.ShopTierCSlots, config.ShopPoolSize, rng);
            FillTierQuota(pool, eligible, CardTier.B, config.ShopTierBSlots, config.ShopPoolSize, rng);
            FillTierQuota(pool, eligible, CardTier.A, config.ShopTierASlots, config.ShopPoolSize, rng);
            FillRemainingSlots(pool, eligible, config.ShopPoolSize, rng);

            return pool;
        }

        /// <summary>
        /// Sostituisce alcune carte del pool con nuove carte idonee, fino alla dimensione target.
        /// Tiene conto dei Credits aggiornati, così le carte sbloccate possono comparire.
        /// </summary>
        /// <param name="pool">Pool corrente da modificare in place.</param>
        /// <param name="catalog">Catalogo completo delle carte.</param>
        /// <param name="credits">Credits aggiornati del giocatore.</param>
        /// <param name="config">Configurazione della dimensione, del refresh e delle quote per tier.</param>
        /// <param name="rng">Generatore casuale host-authoritative.</param>
        public static void RefreshSlots(
            List<CardDataSO> pool,
            IReadOnlyList<CardDataSO> catalog,
            int credits,
            GameConfigSO config,
            Random rng)
        {
            int toRemove = Math.Min(config.ShopRefreshSlots, pool.Count);
            for (int i = 0; i < toRemove; i++)
            {
                pool.RemoveAt(rng.Next(pool.Count));
            }

            List<CardDataSO> eligible = FilterByCredits(catalog, credits);
            FillTierQuota(pool, eligible, CardTier.C, config.ShopTierCSlots, config.ShopPoolSize, rng);
            FillTierQuota(pool, eligible, CardTier.B, config.ShopTierBSlots, config.ShopPoolSize, rng);
            FillTierQuota(pool, eligible, CardTier.A, config.ShopTierASlots, config.ShopPoolSize, rng);
            FillRemainingSlots(pool, eligible, config.ShopPoolSize, rng);
        }

        /// <summary>
        /// Completa la quota minima del tier indicato usando carte non ancora presenti.
        /// </summary>
        private static void FillTierQuota(
            List<CardDataSO> pool,
            IReadOnlyList<CardDataSO> eligible,
            CardTier tier,
            int targetCount,
            int poolSize,
            Random rng)
        {
            int currentCount = 0;
            foreach (CardDataSO card in pool)
            {
                if (card.Tier == tier)
                {
                    currentCount++;
                }
            }

            List<CardDataSO> candidates = new();
            foreach (CardDataSO card in eligible)
            {
                if (card.Tier == tier && !pool.Contains(card))
                {
                    candidates.Add(card);
                }
            }

            CollectionUtils.Shuffle(candidates, rng);
            int required = Math.Max(0, targetCount - currentCount);
            int toAdd = Math.Min(required, Math.Min(candidates.Count, poolSize - pool.Count));
            for (int i = 0; i < toAdd; i++)
            {
                pool.Add(candidates[i]);
            }
        }

        /// <summary>
        /// Riempie gli slot liberi con carte casuali idonee non ancora presenti.
        /// </summary>
        private static void FillRemainingSlots(
            List<CardDataSO> pool,
            IReadOnlyList<CardDataSO> eligible,
            int poolSize,
            Random rng)
        {
            List<CardDataSO> candidates = new();
            foreach (CardDataSO card in eligible)
            {
                if (!pool.Contains(card))
                {
                    candidates.Add(card);
                }
            }

            CollectionUtils.Shuffle(candidates, rng);
            int toAdd = Math.Min(poolSize - pool.Count, candidates.Count);
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
