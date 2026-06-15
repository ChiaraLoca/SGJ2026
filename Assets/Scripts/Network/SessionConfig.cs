using FourE.Commanders;

namespace FourE.Network
{
    /// <summary>
    /// Modalità di rete scelta nel menu iniziale, letta dal <see cref="NetworkGameManager"/>
    /// per selezionare l'implementazione di <see cref="INetworkTransport"/>.
    /// </summary>
    public enum NetworkMode
    {
        /// <summary>Modalita legacy con due giocatori sullo stesso dispositivo.</summary>
        Hotseat,

        /// <summary>Partita locale contro un avversario controllato dal computer.</summary>
        Pve,

        /// <summary>Partita 1v1 online via Photon, accoppiamento per codice stanza.</summary>
        Online
    }

    /// <summary>
    /// Scelte del menu iniziale che sopravvivono al cambio di scena (stato statico di sessione).
    /// Impostate dal menu prima di caricare la scena di gioco; lette dal livello di rete.
    /// </summary>
    public static class SessionConfig
    {
        /// <summary>Modalità di rete attiva per la prossima partita. Default: PvE locale.</summary>
        public static NetworkMode Mode { get; set; } = NetworkMode.Pve;

        /// <summary>
        /// Codice stanza condiviso per la modalità online: l'host lo crea, l'ospite lo digita.
        /// Ignorato nelle modalità locali.
        /// </summary>
        public static string RoomCode { get; set; } = string.Empty;

        /// <summary>
        /// Comandanti scelti dal primo giocatore (Player0) nella schermata di selezione.
        /// Lunghezza <see cref="GameConstants.CommandersPerPlayer"/>; null = usa i comandanti di default del contenuto.
        /// I duplicati sono ammessi: i due slot possono avere lo stesso <see cref="CommanderKind"/>.
        /// </summary>
        public static CommanderKind[] Player0Commanders { get; set; }

        /// <summary>
        /// Comandanti scelti dal secondo giocatore (Player1). null = usa i default del contenuto.
        /// </summary>
        public static CommanderKind[] Player1Commanders { get; set; }

        /// <summary>
        /// Riporta la sessione ai valori di default (PvE, nessun codice, selezioni azzerate).
        /// Utile tra una partita e l'altra.
        /// </summary>
        public static void Reset()
        {
            Mode = NetworkMode.Pve;
            RoomCode = string.Empty;
            Player0Commanders = null;
            Player1Commanders = null;
        }
    }
}
