namespace FourE.Config
{
    /// <summary>
    /// Costanti di gioco strutturali, non configurabili a runtime.
    /// I valori di bilanciamento vivono invece in <see cref="GameConfigSO"/>.
    /// </summary>
    public static class GameConstants
    {
        /// <summary>Numero di giocatori in una partita 1v1.</summary>
        public const int PlayersPerMatch = 2;

        /// <summary>Numero di comandanti controllati da ogni giocatore.</summary>
        public const int CommandersPerPlayer = 2;

        /// <summary>Carte di partenza legate a ciascun comandante.</summary>
        public const int StartingCardsPerCommander = 5;

        /// <summary>Carte totali nella mano iniziale di setup (2 comandanti × 5).</summary>
        public const int CardsInStartingHand = CommandersPerPlayer * StartingCardsPerCommander;

        /// <summary>Round di Verifica giocati prima dell'Esame Finale.</summary>
        public const int RoundsBeforeFinalExam = 3;

        /// <summary>Indice dello slot del primo comandante.</summary>
        public const int FirstCommanderIndex = 0;

        /// <summary>Indice dello slot del secondo comandante.</summary>
        public const int SecondCommanderIndex = 1;

        /// <summary>Numero del primo turno di un round (la Verifica non è giocabile in questo turno).</summary>
        public const int FirstRoundTurnNumber = 1;

        /// <summary>Valore normalizzato usato per centrare gli elementi UI negli anchor del Canvas.</summary>
        public const float UiCenterAnchor = 0.5f;
    }
}
