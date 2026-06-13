using System;
using System.Collections.Generic;

namespace FourE.Core
{
    /// <summary>
    /// Utility statiche per la manipolazione di collezioni runtime.
    /// </summary>
    public static class CollectionUtils
    {
        /// <summary>
        /// Mischia la lista in place con l'algoritmo di Fisher-Yates.
        /// </summary>
        /// <typeparam name="T">Tipo degli elementi.</typeparam>
        /// <param name="list">Lista da mischiare.</param>
        /// <param name="rng">Generatore casuale host-authoritative.</param>
        public static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
