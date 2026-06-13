namespace FourE.Network
{
    /// <summary>
    /// Codici evento Photon usati dal livello di rete. Sono <c>byte</c> costanti:
    /// gli intent (Client→Host) usano i codici bassi, i broadcast (Host→All) quelli alti.
    /// Riferiti dall'implementazione Photon di <see cref="INetworkTransport"/>.
    /// </summary>
    public static class PhotonEventCodes
    {
        /// <summary>Intent di gioco carta standard (Client→Host).</summary>
        public const byte PlayCard = 1;

        /// <summary>Intent di acquisto dallo shop (Client→Host).</summary>
        public const byte BuyCard = 2;

        /// <summary>Intent di gioco della Verifica (Client→Host).</summary>
        public const byte PlayVerifica = 3;

        /// <summary>Intent di fine turno (Client→Host).</summary>
        public const byte EndTurn = 4;

        /// <summary>Intent di conclusione acquisti shop (Client→Host).</summary>
        public const byte FinishShop = 5;

        /// <summary>Richiesta di resync: un client pronto chiede all'host lo stato corrente (Client→Host).</summary>
        public const byte RequestState = 6;

        /// <summary>Sincronizzazione completa dello stato (Host→All).</summary>
        public const byte StateSync = 10;

        /// <summary>Stato iniziale della partita (Host→All).</summary>
        public const byte GameStart = 11;

        /// <summary>Esito di fine partita (Host→All).</summary>
        public const byte GameOver = 12;
    }
}
