using FourE.Players;

namespace FourE.Core
{
    /// <summary>
    /// Decide quale dei due giocatori apre (inizio partita e apertura di ogni round).
    /// Astratto per consentire implementazioni mock e deterministiche nei test.
    /// </summary>
    public interface IStartingPlayerDecider
    {
        /// <summary>
        /// Sceglie il giocatore che muove per primo tra i due candidati.
        /// </summary>
        /// <param name="first">Primo candidato.</param>
        /// <param name="second">Secondo candidato.</param>
        /// <returns>Il giocatore che inizia.</returns>
        PlayerState DecideStartingPlayer(PlayerState first, PlayerState second);
    }
}
