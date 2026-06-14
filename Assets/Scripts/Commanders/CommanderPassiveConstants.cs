namespace FourE.Commanders
{
    /// <summary>
    /// Costanti di gioco delle passive dei comandanti (valori di design fissi, non runtime-tunable).
    /// Definite in CARDS.md.
    /// </summary>
    public static class CommanderPassiveConstants
    {
        // --- Storia ---

        /// <summary>Note aggiunte al comandante Storia, a inizio round, per ogni Verifica giocata nella partita.</summary>
        public const int StoriaNotePerVerifica = 3;

        /// <summary>Crediti necessari a sbloccare la secondaria di Storia.</summary>
        public const int StoriaSecondaryUnlockCredits = 20;

        // --- Matematica ---

        /// <summary>Carte aggiuntive con cui Matematica inizia ogni round (non contano come pescate).</summary>
        public const int MateExtraCardsPerRound = 1;

        /// <summary>Note guadagnate dal comandante Matematica (secondaria) per ogni carta pescata.</summary>
        public const int MateNotePerDraw = 1;

        /// <summary>Indice di round (0-based) che sblocca la secondaria di Matematica (3° round).</summary>
        public const int MateSecondaryUnlockRoundIndex = 2;

        // --- Inglese ---

        /// <summary>Note aggiunte all'altro comandante quando aumenta la Nota di Inglese (passiva base).</summary>
        public const int IngleseBaseBonusToOther = 1;

        /// <summary>Carte nel mazzo necessarie a sbloccare la secondaria di Inglese.</summary>
        public const int IngleseSecondaryUnlockDeckSize = 15;

        // --- Educazione Fisica ---

        /// <summary>Note aggiunte all'altro comandante a fine turno (passiva base), se condizione soddisfatta.</summary>
        public const int EduFisicaTurnEndBonus = 1;

        /// <summary>Note sottratte al comandante avversario più alto per ogni azione giocata (secondaria).</summary>
        public const int EduFisicaActionPenalty = 1;

        /// <summary>Soglia di Note che sblocca la secondaria di Educazione Fisica (arrivare a 0).</summary>
        public const int EduFisicaSecondaryUnlockNote = 0;
    }
}
