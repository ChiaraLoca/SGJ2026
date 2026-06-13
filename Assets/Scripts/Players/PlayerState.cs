using System.Collections.Generic;
using FourE.Cards;
using FourE.Commanders;
using FourE.Config;

namespace FourE.Players
{
    /// <summary>
    /// Stato runtime di un giocatore: Credits, carte, comandanti e carta Verifica.
    /// </summary>
    public sealed class PlayerState
    {
        /// <summary>Numero attore Photon che identifica il giocatore.</summary>
        public int ActorNumber { get; }

        /// <summary>Punteggio permanente. Aumenta solo alla conversione post-Verifica.</summary>
        public int Credits { get; private set; }

        /// <summary>Carte attualmente in mano.</summary>
        public List<CardDataSO> Hand { get; } = new();

        /// <summary>Mazzo personale da cui si pesca.</summary>
        public List<CardDataSO> Deck { get; } = new();

        /// <summary>Carte scartate e acquistate, rimischiate nel mazzo alla Fase DRAW.</summary>
        public List<CardDataSO> DiscardPile { get; } = new();

        /// <summary>Pool shop personale, filtrato per Credits.</summary>
        public List<CardDataSO> ShopPool { get; } = new();

        /// <summary>I due comandanti del giocatore.</summary>
        public CommanderState[] Commanders { get; }

        /// <summary>Carta Verifica in slot dedicato, fuori dal mazzo.</summary>
        public CardDataSO VerificaCard { get; set; }

        /// <summary>Somma delle Note correnti dei due comandanti.</summary>
        public int TotalNotes => Commanders[GameConstants.FirstCommanderIndex].CurrentNote
                                 + Commanders[GameConstants.SecondCommanderIndex].CurrentNote;

        /// <summary>Note già spese nello shop durante il round corrente.</summary>
        public int SpentNotes { get; private set; }

        /// <summary>
        /// Note ancora disponibili come valuta (totale meno speso), mai negativa.
        /// Usata sia per gli acquisti shop sia per la conversione in Credits.
        /// </summary>
        public int AvailableNotes
        {
            get
            {
                int available = TotalNotes - SpentNotes;
                return available < 0 ? 0 : available;
            }
        }

        /// <summary>
        /// Crea lo stato del giocatore con i suoi comandanti.
        /// </summary>
        /// <param name="actorNumber">Identificativo attore Photon.</param>
        /// <param name="commanders">Array dei comandanti, di lunghezza <see cref="GameConstants.CommandersPerPlayer"/>.</param>
        public PlayerState(int actorNumber, CommanderState[] commanders)
        {
            ActorNumber = actorNumber;
            Commanders = commanders;
        }

        /// <summary>
        /// Aggiunge Credits al giocatore (es. conversione post-Verifica).
        /// </summary>
        /// <param name="amount">Credits da aggiungere.</param>
        public void AddCredits(int amount)
        {
            Credits += amount;
        }

        /// <summary>
        /// Registra Note spese per un acquisto shop nel round corrente.
        /// </summary>
        /// <param name="amount">Note spese.</param>
        public void SpendNotes(int amount)
        {
            SpentNotes += amount;
        }

        /// <summary>
        /// Azzera le Note spese, da chiamare al reset di fine round.
        /// </summary>
        public void ResetSpentNotes()
        {
            SpentNotes = 0;
        }
    }
}
