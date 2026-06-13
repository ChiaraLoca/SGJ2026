using System;
using FourE.Config;
using FourE.Players;

namespace FourE.Core
{
    /// <summary>
    /// Decide il primo giocatore con un lancio di moneta basato sul generatore host-authoritative.
    /// </summary>
    public sealed class CoinFlipStartingPlayerDecider : IStartingPlayerDecider
    {
        private readonly Random _rng;

        /// <summary>
        /// Crea il decider a lancio di moneta.
        /// </summary>
        /// <param name="rng">Generatore casuale host-authoritative.</param>
        public CoinFlipStartingPlayerDecider(Random rng)
        {
            _rng = rng;
        }

        /// <inheritdoc/>
        public PlayerState DecideStartingPlayer(PlayerState first, PlayerState second)
        {
            return _rng.Next(GameConstants.PlayersPerMatch) == 0 ? first : second;
        }
    }
}
