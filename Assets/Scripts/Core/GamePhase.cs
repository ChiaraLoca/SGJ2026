namespace FourE.Core
{
    /// <summary>
    /// Fasi della state machine di una partita, scandite dal PhaseManager.
    /// </summary>
    public enum GamePhase
    {
        /// <summary>Allestimento iniziale: comandanti, mani e pool shop.</summary>
        Setup,

        /// <summary>Turni alternati in cui i giocatori giocano carte.</summary>
        Play,

        /// <summary>Chiusura del round innescata dalla carta Verifica.</summary>
        Verifica,

        /// <summary>Acquisti dallo shop prima della conversione in Credits.</summary>
        Shop,

        /// <summary>Scarto, rimescolo e pesca per il round successivo.</summary>
        Draw,

        /// <summary>Esame Finale: confronto dei Credits e vittoria.</summary>
        FinalExam
    }
}
