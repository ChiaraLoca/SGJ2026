using System;

namespace FourE.Cards
{
    /// <summary>
    /// Tag tematici di una carta. Combinabili (una carta può avere più tag).
    /// Usati da effetti che contano o filtrano per tag (Sabotaggio, Politica, Iperka…).
    /// </summary>
    [Flags]
    public enum CardTag
    {
        /// <summary>Nessun tag.</summary>
        None = 0,

        /// <summary>Pratica (E, I).</summary>
        Pratica = 1 << 0,

        /// <summary>Studio (S, M).</summary>
        Studio = 1 << 1,

        /// <summary>Estetica (S, E).</summary>
        Estetica = 1 << 2,

        /// <summary>Letteratura (S, I).</summary>
        Letteratura = 1 << 3,

        /// <summary>Pianificazione (M, E).</summary>
        Pianificazione = 1 << 4,

        /// <summary>Ricerca (M, I).</summary>
        Ricerca = 1 << 5
    }

    /// <summary>
    /// Fascia di costo di una carta nello shop. Il costo concreto è risolto dal
    /// <see cref="FourE.Config.GameConfigSO"/>, così resta configurabile a runtime.
    /// </summary>
    public enum CardTier
    {
        /// <summary>Fascia economica (default 1 Nota).</summary>
        C,

        /// <summary>Fascia media (default 3 Note).</summary>
        B,

        /// <summary>Fascia alta (default 10 Note).</summary>
        A
    }

    /// <summary>
    /// Sorgente di conteggio per gli effetti che scalano in base a un insieme di carte.
    /// </summary>
    public enum CountSource
    {
        /// <summary>Carte nel cimitero (scarti) del giocatore attivo.</summary>
        OwnDiscardPile,

        /// <summary>Carte in mano al giocatore attivo.</summary>
        OwnHand,

        /// <summary>Tag distinti presenti nel cimitero del giocatore attivo.</summary>
        OwnDiscardDistinctTags,

        /// <summary>Carte con un tag specifico nel cimitero del giocatore attivo.</summary>
        OwnDiscardWithTag,

        /// <summary>Carte con un tag specifico nel cimitero dell'avversario.</summary>
        EnemyDiscardWithTag
    }

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

        /// <summary>
        /// Il comandante del giocatore attivo legato all'affinità della carta giocata
        /// (Commander0 → slot 0, Commander1 → slot 1). Per le carte di mazzo auto-bersaglianti.
        /// </summary>
        AffinityCommander,

        /// <summary>
        /// L'altro comandante del giocatore attivo rispetto all'affinità della carta.
        /// </summary>
        AffinityOtherCommander,

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
