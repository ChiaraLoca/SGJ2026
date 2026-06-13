using System;

namespace FourE.Network
{
    /// <summary>
    /// Tipo di intent inviato dal client all'host. Codificato come <c>byte</c> per la rete.
    /// </summary>
    public enum IntentType : byte
    {
        /// <summary>Gioca una carta standard dalla mano.</summary>
        PlayCard = PhotonEventCodes.PlayCard,

        /// <summary>Acquista una carta dallo shop.</summary>
        BuyCard = PhotonEventCodes.BuyCard,

        /// <summary>Gioca la carta Verifica, chiudendo la Fase PLAY.</summary>
        PlayVerifica = PhotonEventCodes.PlayVerifica,

        /// <summary>Termina il turno corrente.</summary>
        EndTurn = PhotonEventCodes.EndTurn,

        /// <summary>Conclude gli acquisti nello shop.</summary>
        FinishShop = PhotonEventCodes.FinishShop
    }

    /// <summary>
    /// Comando serializzabile che esprime l'intenzione di un giocatore (pattern Command).
    /// Solo l'host lo valida ed esegue; non contiene riferimenti a UnityEngine.Object.
    /// I bersagli selezionabili sono coppie parallele (attore proprietario, indice comandante).
    /// </summary>
    public readonly struct GameIntent
    {
        private static readonly int[] EmptyTargets = Array.Empty<int>();

        /// <summary>Tipo di azione richiesta.</summary>
        public IntentType Type { get; }

        /// <summary>Attore Photon che emette l'intent.</summary>
        public int ActorNumber { get; }

        /// <summary>Id della carta coinvolta, o <see cref="CardRegistry.NoCard"/> se non pertinente.</summary>
        public int CardId { get; }

        /// <summary>Attori proprietari dei comandanti bersaglio selezionati.</summary>
        public int[] TargetActorNumbers { get; }

        /// <summary>Indici dei comandanti bersaglio, paralleli a <see cref="TargetActorNumbers"/>.</summary>
        public int[] TargetCommanderIndices { get; }

        /// <summary>
        /// Crea un intent. Usare di preferenza le factory tipizzate.
        /// </summary>
        /// <param name="type">Tipo di intent.</param>
        /// <param name="actorNumber">Attore emittente.</param>
        /// <param name="cardId">Id carta o <see cref="CardRegistry.NoCard"/>.</param>
        /// <param name="targetActorNumbers">Attori proprietari dei bersagli, o null.</param>
        /// <param name="targetCommanderIndices">Indici dei comandanti bersaglio, o null.</param>
        public GameIntent(
            IntentType type,
            int actorNumber,
            int cardId,
            int[] targetActorNumbers,
            int[] targetCommanderIndices)
        {
            Type = type;
            ActorNumber = actorNumber;
            CardId = cardId;
            TargetActorNumbers = targetActorNumbers ?? EmptyTargets;
            TargetCommanderIndices = targetCommanderIndices ?? EmptyTargets;
        }

        /// <summary>Crea un intent di gioco carta con eventuali bersagli selezionati.</summary>
        /// <param name="actorNumber">Attore emittente.</param>
        /// <param name="cardId">Id della carta da giocare.</param>
        /// <param name="targetActorNumbers">Attori proprietari dei bersagli, o null.</param>
        /// <param name="targetCommanderIndices">Indici dei comandanti bersaglio, o null.</param>
        /// <returns>Intent di tipo PlayCard.</returns>
        public static GameIntent PlayCard(
            int actorNumber,
            int cardId,
            int[] targetActorNumbers = null,
            int[] targetCommanderIndices = null)
        {
            return new GameIntent(IntentType.PlayCard, actorNumber, cardId, targetActorNumbers, targetCommanderIndices);
        }

        /// <summary>Crea un intent di acquisto carta dallo shop.</summary>
        /// <param name="actorNumber">Attore emittente.</param>
        /// <param name="cardId">Id della carta da acquistare.</param>
        /// <returns>Intent di tipo BuyCard.</returns>
        public static GameIntent BuyCard(int actorNumber, int cardId)
        {
            return new GameIntent(IntentType.BuyCard, actorNumber, cardId, null, null);
        }

        /// <summary>Crea un intent di gioco della Verifica.</summary>
        /// <param name="actorNumber">Attore emittente.</param>
        /// <returns>Intent di tipo PlayVerifica.</returns>
        public static GameIntent PlayVerifica(int actorNumber)
        {
            return new GameIntent(IntentType.PlayVerifica, actorNumber, CardRegistry.NoCard, null, null);
        }

        /// <summary>Crea un intent di fine turno.</summary>
        /// <param name="actorNumber">Attore emittente.</param>
        /// <returns>Intent di tipo EndTurn.</returns>
        public static GameIntent EndTurn(int actorNumber)
        {
            return new GameIntent(IntentType.EndTurn, actorNumber, CardRegistry.NoCard, null, null);
        }

        /// <summary>Crea un intent di conclusione acquisti shop.</summary>
        /// <param name="actorNumber">Attore emittente.</param>
        /// <returns>Intent di tipo FinishShop.</returns>
        public static GameIntent FinishShop(int actorNumber)
        {
            return new GameIntent(IntentType.FinishShop, actorNumber, CardRegistry.NoCard, null, null);
        }
    }
}
