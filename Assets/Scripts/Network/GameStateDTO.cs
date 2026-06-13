using System;

namespace FourE.Network
{
    /// <summary>
    /// Snapshot serializzabile dello stato di un comandante per la rete e la UI.
    /// Solo tipi primitivi, nessun riferimento a UnityEngine.Object.
    /// </summary>
    [Serializable]
    public struct CommanderDTO
    {
        /// <summary>Note di base del comandante.</summary>
        public int BaseNote;

        /// <summary>Note corrente effettiva (base + modifiche + buff - debuff).</summary>
        public int CurrentNote;

        /// <summary>True se il comandante ha almeno un debuff attivo.</summary>
        public bool HasDebuff;

        /// <summary>Numero di buff a durata attivi.</summary>
        public int ActiveBuffCount;

        /// <summary>Numero di debuff a durata attivi.</summary>
        public int ActiveDebuffCount;
    }

    /// <summary>
    /// Snapshot serializzabile dello stato di un giocatore.
    /// Le carte sono referenziate per id tramite <see cref="CardRegistry"/>.
    /// </summary>
    [Serializable]
    public struct PlayerDTO
    {
        /// <summary>Attore Photon del giocatore.</summary>
        public int ActorNumber;

        /// <summary>Crediti permanenti: valuta dello shop e criterio di vittoria.</summary>
        public int Credits;

        /// <summary>Note correnti (punteggio temporaneo del round = somma dei comandanti).</summary>
        public int Notes;

        /// <summary>Id delle carte in mano (inclusa la Verifica, se pescata).</summary>
        public int[] HandCardIds;

        /// <summary>Numero di carte rimaste nel mazzo.</summary>
        public int DeckCount;

        /// <summary>Id delle carte disponibili nel pool shop.</summary>
        public int[] ShopPoolCardIds;

        /// <summary>Stato dei due comandanti.</summary>
        public CommanderDTO[] Commanders;
    }

    /// <summary>
    /// Snapshot completo dello stato di gioco, broadcastato dall'host dopo ogni azione.
    /// Il client ricostruisce interamente la propria view a partire da questo DTO.
    /// </summary>
    [Serializable]
    public struct GameStateDTO
    {
        /// <summary>Fase corrente, come valore di <c>GamePhase</c>.</summary>
        public int Phase;

        /// <summary>Indice del round corrente (0-based).</summary>
        public int RoundIndex;

        /// <summary>Attore del giocatore di turno.</summary>
        public int ActiveActorNumber;

        /// <summary>Stato dei due giocatori.</summary>
        public PlayerDTO[] Players;

        /// <summary>True se la partita è conclusa (Esame Finale risolto).</summary>
        public bool IsGameOver;

        /// <summary>Attore vincitore, o <c>GameOverEvent.NoWinner</c> in pareggio o partita in corso.</summary>
        public int WinnerActorNumber;

        /// <summary>True se l'esito è un pareggio.</summary>
        public bool IsDraw;

        /// <summary>Numero progressivo dell'ultima carta giocata, usato per attivare la UI una sola volta.</summary>
        public int PlayedCardSequence;

        /// <summary>Id registry dell'ultima carta giocata, o <see cref="CardRegistry.NoCard"/>.</summary>
        public int LastPlayedCardId;

        /// <summary>Attore che ha giocato l'ultima carta.</summary>
        public int LastPlayedActorNumber;
    }
}
