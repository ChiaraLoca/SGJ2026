namespace FourE.Cards
{
    /// <summary>
    /// Categoria della carta che ne determina il comportamento nel flusso di gioco.
    /// </summary>
    public enum CardType
    {
        /// <summary>Carta standard giocabile con i propri effetti.</summary>
        Standard,

        /// <summary>Carta Verifica: chiude la Fase PLAY e avvia Shop e conversione.</summary>
        Verifica
    }

    /// <summary>
    /// Affinità di una carta con un comandante del giocatore.
    /// Le carte di partenza sono legate a un comandante; quelle acquistate sono neutre.
    /// </summary>
    public enum CardAffinity
    {
        /// <summary>Legata al primo comandante (slot 0).</summary>
        Commander0,

        /// <summary>Legata al secondo comandante (slot 1).</summary>
        Commander1,

        /// <summary>Carta neutra, tipicamente acquistata nello shop.</summary>
        Neutral
    }

    /// <summary>
    /// Bersaglio su cui un effetto carta agisce.
    /// </summary>
    public enum EffectTarget
    {
        /// <summary>Primo comandante del giocatore attivo.</summary>
        OwnCommander0,

        /// <summary>Secondo comandante del giocatore attivo.</summary>
        OwnCommander1,

        /// <summary>Primo comandante dell'avversario.</summary>
        EnemyCommander0,

        /// <summary>Secondo comandante dell'avversario.</summary>
        EnemyCommander1,

        /// <summary>Entrambi i comandanti del giocatore attivo.</summary>
        AllOwnCommanders,

        /// <summary>Entrambi i comandanti dell'avversario.</summary>
        AllEnemyCommanders,

        /// <summary>Tutti e quattro i comandanti in gioco.</summary>
        AllCommanders,

        /// <summary>
        /// Uno o più comandanti scelti a runtime dal giocatore al momento del gioco.
        /// Risolti dai bersagli passati al <c>GameContext</c> tramite l'intent di gioco.
        /// </summary>
        SelectedCommanders,

        /// <summary>Il giocatore che sta giocando la carta.</summary>
        ActivePlayer,

        /// <summary>Il giocatore avversario.</summary>
        InactivePlayer
    }

    /// <summary>
    /// Durata temporale di un effetto applicato a un comandante.
    /// </summary>
    public enum EffectDuration
    {
        /// <summary>Applicato una volta, senza tracciamento successivo.</summary>
        Instant,

        /// <summary>Attivo per un numero di turni, poi rimosso automaticamente.</summary>
        Turns,

        /// <summary>Attivo fino al reset post-Verifica del round.</summary>
        UntilVerifica
    }
}
