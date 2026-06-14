namespace FourE.Commanders
{
    /// <summary>
    /// Identità di un comandante. Determina quali passive (base e secondaria) gli si applicano.
    /// </summary>
    public enum CommanderKind
    {
        /// <summary>Storia (S): passive legate alla Verifica e al raddoppio delle carte Studio.</summary>
        Storia = 0,

        /// <summary>Matematica (M): passive legate alla pesca di carte.</summary>
        Matematica = 1,

        /// <summary>Inglese (I): passive che propagano gli aumenti di Nota all'altro comandante.</summary>
        Inglese = 2,

        /// <summary>Educazione Fisica (E): passive legate al numero di carte in mano e alle azioni giocate.</summary>
        EducazioneFisica = 3,

        /// <summary>Diritto (D): passive legate alle azioni lasciate inutilizzate a fine turno.</summary>
        Diritto = 4,

        /// <summary>Arte (A): passive legate ai tag giocati e alla propagazione dei debuff.</summary>
        Arte = 5
    }
}
