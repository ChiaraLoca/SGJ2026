using System.Collections.Generic;
using UnityEngine;
using FourE.Cards;
using FourE.Commanders;
using FourE.Config;
using FourE.Core;
using FourE.Events;
using FourE.Players;

namespace FourE.Network
{
    /// <summary>
    /// Livello di rete host-authoritative. Riceve gli intent dal trasporto, li valida ed
    /// esegue (solo l'host), quindi broadcasta lo stato aggiornato come <see cref="GameStateDTO"/>.
    /// La UI invia intent tramite i metodi <c>Submit*</c> e si ridisegna sull'evento di sync.
    /// Gira offline col <see cref="LocalLoopbackTransport"/>; Photon si innesta sostituendo solo
    /// l'implementazione di <see cref="INetworkTransport"/>.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class NetworkGameManager : MonoBehaviour
    {
        [Tooltip("Stato di gioco autoritativo da cui leggere ed eseguire gli intent.")]
        [SerializeField] private GameStateManager _gameState;
        [Tooltip("Tempo lasciato alla presentazione completa di una carta prima della prossima decisione PvE.")]
        [SerializeField] private float _computerActionDelaySeconds = 3f;

        private INetworkTransport _transport;
        private CardRegistry _registry;
        private PveOpponentController _pveOpponent;
        private Coroutine _computerActionRoutine;
        private bool _gameOver;
        private int _winnerActorNumber = GameOverEvent.NoWinner;
        private bool _isDraw;
        private bool _stateReceived;
        private int _playedCardSequence;
        private int _lastPlayedCardId = CardRegistry.NoCard;
        private int _lastPlayedActorNumber = -1;
        private int[] _lastPlayedTargetActorNumbers = System.Array.Empty<int>();
        private int[] _lastPlayedTargetCommanderIndices = System.Array.Empty<int>();
        private int[] _pendingTargetActorNumbers = System.Array.Empty<int>();
        private int[] _pendingTargetCommanderIndices = System.Array.Empty<int>();
        private int _drawSequence;
        private int _lastDrawActorNumber = -1;
        private int[] _lastDrawnCardIds = System.Array.Empty<int>();

#if PHOTON_UNITY_NETWORKING
        /// <summary>Intervallo tra i tentativi di resync del client online (secondi).</summary>
        private const float ResyncIntervalSeconds = 1f;

        /// <summary>Numero massimo di richieste di stato prima di rinunciare.</summary>
        private const int MaxResyncAttempts = 15;
#endif

        /// <summary>Trasporto di rete attivo (loopback offline o Photon).</summary>
        public INetworkTransport Transport => _transport;

        /// <summary>Registry id↔carta condiviso con la UI per risolvere i DTO.</summary>
        public CardRegistry Registry => _registry;

        /// <summary>Attore dell'istanza locale.</summary>
        public int LocalActorNumber => _transport.LocalActorNumber;

        /// <summary>
        /// Costruisce registry e trasporto, registra le callback di rete e l'osservatore di esito.
        /// </summary>
        private void Awake()
        {
            _registry = CardRegistry.Build(_gameState.Content);
            _transport = CreateTransport();
            _transport.IntentReceived += OnIntentReceived;
            _transport.StateReceived += OnStateReceived;
            _transport.ClientJoined += OnClientJoined;

            // Il boot è guidato qui (host avvia, client attende): niente auto-start dal GameStateManager.
            _gameState.AutoStartOffline = false;
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Subscribe<VerificaPlayedEvent>(OnVerificaPlayed);
            EventBus.Subscribe<CardsDrawnEvent>(OnCardsDrawn);
        }

        /// <summary>
        /// Seleziona l'implementazione di trasporto in base alla modalità scelta nel menu.
        /// Online usa Photon se installato, altrimenti ripiega sull'hotseat locale.
        /// </summary>
        /// <returns>Il trasporto da usare per questa partita.</returns>
        private INetworkTransport CreateTransport()
        {
            if (SessionConfig.Mode == NetworkMode.Online)
            {
#if PHOTON_UNITY_NETWORKING
                return new PhotonTransport();
#else
                Debug.LogWarning("Online mode requested but PUN2 is not installed; falling back to local mode.");
#endif
            }

            return SessionConfig.Mode == NetworkMode.Pve
                ? new LocalLoopbackTransport(GameConstants.FirstCommanderIndex)
                : new HotseatTransport(GameConstants.FirstCommanderIndex);
        }

        /// <summary>
        /// Broadcasta lo stato iniziale dopo che il GameStateManager ha avviato la partita
        /// (garantito dall'ordine di esecuzione posticipato).
        /// </summary>
        private void Start()
        {
            // L'host (hotseat o MasterClient online) avvia la partita e ne broadcasta lo stato.
            if (_transport.IsHost)
            {
                _gameState.SetCommanderSelections(
                    SessionConfig.Player0Commanders,
                    SessionConfig.Player1Commanders);
                _gameState.StartMatch();
                if (SessionConfig.Mode == NetworkMode.Pve)
                {
                    _pveOpponent = new PveOpponentController(
                        _gameState,
                        _registry,
                        new System.Random());
                }

                BroadcastCurrentState();
                return;
            }

#if PHOTON_UNITY_NETWORKING
            // Il client online non costruisce la partita: chiede lo stato all'host e attende.
            StartCoroutine(RequestStateUntilSynced());
#endif
        }

#if PHOTON_UNITY_NETWORKING
        /// <summary>
        /// Chiede ripetutamente lo stato all'host finché il primo snapshot non arriva,
        /// coprendo eventuali differenze di tempo nel caricamento della scena tra i due device.
        /// </summary>
        /// <returns>Enumeratore della coroutine.</returns>
        private System.Collections.IEnumerator RequestStateUntilSynced()
        {
            int attempts = 0;
            while (!_stateReceived && attempts < MaxResyncAttempts)
            {
                (_transport as PhotonTransport)?.RequestInitialState();
                attempts++;
                yield return new WaitForSeconds(ResyncIntervalSeconds);
            }
        }
#endif

        /// <summary>
        /// Sull'host, ri-broadcasta lo stato corrente quando un client chiede il resync.
        /// </summary>
        private void OnClientJoined()
        {
            BroadcastCurrentState();
        }

        /// <summary>
        /// Disiscrive callback di rete ed EventBus al teardown.
        /// </summary>
        private void OnDestroy()
        {
            if (_transport != null)
            {
                _transport.IntentReceived -= OnIntentReceived;
                _transport.StateReceived -= OnStateReceived;
                _transport.ClientJoined -= OnClientJoined;
                (_transport as System.IDisposable)?.Dispose();
            }

            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Unsubscribe<VerificaPlayedEvent>(OnVerificaPlayed);
            EventBus.Unsubscribe<CardsDrawnEvent>(OnCardsDrawn);

            if (_computerActionRoutine != null)
            {
                StopCoroutine(_computerActionRoutine);
                _computerActionRoutine = null;
            }
        }

        /// <summary>
        /// Invia un intent di gioco carta, con eventuali comandanti bersaglio (attore, indice).
        /// </summary>
        /// <param name="card">Carta da giocare.</param>
        /// <param name="targetActorNumbers">Attori proprietari dei bersagli, o null.</param>
        /// <param name="targetCommanderIndices">Indici dei comandanti bersaglio, o null.</param>
        public void SubmitPlayCard(CardDataSO card, int[] targetActorNumbers = null, int[] targetCommanderIndices = null)
        {
            _transport.SendIntent(GameIntent.PlayCard(
                LocalActorNumber, _registry.GetId(card), targetActorNumbers, targetCommanderIndices));
        }

        /// <summary>Invia un intent di acquisto carta dallo shop.</summary>
        /// <param name="card">Carta da acquistare.</param>
        public void SubmitBuyCard(CardDataSO card)
        {
            _transport.SendIntent(GameIntent.BuyCard(LocalActorNumber, _registry.GetId(card)));
        }

        /// <summary>Invia un intent di gioco della Verifica.</summary>
        public void SubmitPlayVerifica()
        {
            _transport.SendIntent(GameIntent.PlayVerifica(LocalActorNumber));
        }

        /// <summary>Invia un intent di fine turno.</summary>
        public void SubmitEndTurn()
        {
            _transport.SendIntent(GameIntent.EndTurn(LocalActorNumber));
        }

        /// <summary>Invia un intent di conclusione acquisti shop.</summary>
        public void SubmitFinishShop()
        {
            _transport.SendIntent(GameIntent.FinishShop(LocalActorNumber));
        }

        /// <summary>
        /// Memorizza l'esito della partita per includerlo nei broadcast successivi.
        /// </summary>
        /// <param name="outcome">Evento di fine partita.</param>
        private void OnGameOver(GameOverEvent outcome)
        {
            _gameOver = true;
            _winnerActorNumber = outcome.WinnerActorNumber;
            _isDraw = outcome.IsDraw;
        }

        /// <summary>
        /// Memorizza una carta standard appena risolta per il prossimo snapshot.
        /// </summary>
        /// <param name="evt">Evento carta giocata dall'host.</param>
        private void OnCardPlayed(CardPlayedEvent evt)
        {
            RegisterPlayedCard(evt.Card, evt.Player.ActorNumber);
        }

        /// <summary>
        /// Memorizza la Verifica appena giocata per il prossimo snapshot.
        /// </summary>
        /// <param name="evt">Evento Verifica giocata dall'host.</param>
        private void OnVerificaPlayed(VerificaPlayedEvent evt)
        {
            ClearPendingTargets();
            RegisterPlayedCard(_gameState.Content.VerificaCard, evt.Player.ActorNumber);
        }

        /// <summary>
        /// Memorizza le carte effettivamente pescate per animarle nel prossimo snapshot.
        /// </summary>
        /// <param name="evt">Evento di pesca pubblicato dalla logica autoritativa.</param>
        private void OnCardsDrawn(CardsDrawnEvent evt)
        {
            int count = Mathf.Min(evt.Count, evt.Player.Hand.Count);
            int startIndex = evt.Player.Hand.Count - count;
            int[] cardIds = new int[count];
            for (int i = 0; i < count; i++)
            {
                cardIds[i] = _registry.GetId(evt.Player.Hand[startIndex + i]);
            }

            _drawSequence++;
            _lastDrawActorNumber = evt.Player.ActorNumber;
            _lastDrawnCardIds = cardIds;
        }

        /// <summary>
        /// Aggiorna i metadati di presentazione dell'ultima carta giocata.
        /// </summary>
        /// <param name="card">Carta risolta.</param>
        /// <param name="actorNumber">Attore che ha giocato la carta.</param>
        private void RegisterPlayedCard(CardDataSO card, int actorNumber)
        {
            int cardId = _registry.GetId(card);
            if (cardId == CardRegistry.NoCard)
            {
                return;
            }

            _playedCardSequence++;
            _lastPlayedCardId = cardId;
            _lastPlayedActorNumber = actorNumber;
            ResolvePresentationTargets(card, actorNumber);
        }

        /// <summary>
        /// Riceve un intent e, se l'istanza è host, lo esegue e broadcasta il nuovo stato.
        /// </summary>
        /// <param name="intent">Intent ricevuto dal trasporto.</param>
        private void OnIntentReceived(GameIntent intent)
        {
            // Host-authoritative: solo l'host valida ed esegue.
            if (!_transport.IsHost)
            {
                return;
            }

            ProcessIntent(intent);
            BroadcastCurrentState();
        }

        /// <summary>
        /// Inoltra lo stato ricevuto alla UI tramite l'EventBus.
        /// </summary>
        /// <param name="state">Stato ricevuto dal trasporto.</param>
        private void OnStateReceived(GameStateDTO state)
        {
            _stateReceived = true;
            EventBus.Publish(new GameStateSyncedEvent(state, _transport.LocalActorNumber));
        }

        /// <summary>
        /// Costruisce e broadcasta lo snapshot corrente, completo dell'esito se la partita è finita.
        /// </summary>
        private void BroadcastCurrentState()
        {
            if (!_transport.IsHost)
            {
                return;
            }

            GameStateDTO dto = GameStateDtoBuilder.Build(_gameState, _registry);
            dto.IsGameOver = _gameOver;
            dto.WinnerActorNumber = _winnerActorNumber;
            dto.IsDraw = _isDraw;
            dto.PlayedCardSequence = _playedCardSequence;
            dto.LastPlayedCardId = _lastPlayedCardId;
            dto.LastPlayedActorNumber = _lastPlayedActorNumber;
            dto.LastPlayedTargetActorNumbers = _lastPlayedTargetActorNumbers;
            dto.LastPlayedTargetCommanderIndices = _lastPlayedTargetCommanderIndices;
            dto.DrawSequence = _drawSequence;
            dto.LastDrawActorNumber = _lastDrawActorNumber;
            dto.LastDrawnCardIds = _lastDrawnCardIds;
            _transport.BroadcastState(dto);
            ScheduleComputerAction();
        }

        /// <summary>
        /// Accoda una decisione PvE quando il computer deve giocare o completare lo shop.
        /// </summary>
        private void ScheduleComputerAction()
        {
            if (_pveOpponent == null
                || _gameOver
                || !_pveOpponent.CanAct
                || _computerActionRoutine != null)
            {
                return;
            }

            _computerActionRoutine = StartCoroutine(ComputerActionRoutine());
        }

        /// <summary>
        /// Attende il tempo di lettura UI, esegue un intent del computer e sincronizza lo stato.
        /// </summary>
        /// <returns>Enumeratore della coroutine.</returns>
        private System.Collections.IEnumerator ComputerActionRoutine()
        {
            yield return new WaitForSecondsRealtime(_computerActionDelaySeconds);
            _computerActionRoutine = null;

            if (_pveOpponent != null && _pveOpponent.TryCreateIntent(out GameIntent intent))
            {
                ProcessIntent(intent);
                BroadcastCurrentState();
            }
        }

        /// <summary>
        /// Valida ed esegue un intent instradandolo al manager competente.
        /// </summary>
        /// <param name="intent">Intent da eseguire.</param>
        private void ProcessIntent(GameIntent intent)
        {
            PlayerState player = _gameState.GetPlayerByActor(intent.ActorNumber);
            if (player == null)
            {
                return;
            }

            switch (intent.Type)
            {
                case IntentType.PlayCard:
                    _pendingTargetActorNumbers = CloneArray(intent.TargetActorNumbers);
                    _pendingTargetCommanderIndices = CloneArray(intent.TargetCommanderIndices);
                    bool deferComputerTurnEnd = SessionConfig.Mode == NetworkMode.Pve
                        && player == _gameState.Player1;
                    _gameState.Turns.TryPlayCard(
                        player,
                        _registry.GetCard(intent.CardId),
                        ResolveTargets(intent),
                        endTurnWhenActionsExhausted: !deferComputerTurnEnd);
                    ClearPendingTargets();
                    break;
                case IntentType.BuyCard:
                    _gameState.Shop.TryPurchase(player, _registry.GetCard(intent.CardId));
                    break;
                case IntentType.PlayVerifica:
                    ClearPendingTargets();
                    _gameState.Turns.TryPlayVerifica(player);
                    break;
                case IntentType.EndTurn:
                    _gameState.Turns.EndTurn(player);
                    break;
                case IntentType.FinishShop:
                    _gameState.Phases.FinishShop(player);
                    break;
            }
        }

        /// <summary>
        /// Costruisce l'elenco dei comandanti interessati dalla carta per il feedback UI.
        /// </summary>
        /// <param name="card">Carta appena risolta.</param>
        /// <param name="actorNumber">Attore che ha giocato la carta.</param>
        private void ResolvePresentationTargets(CardDataSO card, int actorNumber)
        {
            List<int> actors = new();
            List<int> indices = new();

            int selectedCount = Mathf.Min(
                _pendingTargetActorNumbers.Length,
                _pendingTargetCommanderIndices.Length);
            for (int i = 0; i < selectedCount; i++)
            {
                AddPresentationTarget(
                    actors,
                    indices,
                    _pendingTargetActorNumbers[i],
                    _pendingTargetCommanderIndices[i]);
            }

            PlayerState sourcePlayer = _gameState.GetPlayerByActor(actorNumber);
            PlayerState opponent = sourcePlayer == _gameState.Player0
                ? _gameState.Player1
                : sourcePlayer == _gameState.Player1
                    ? _gameState.Player0
                    : null;
            if (opponent != null && card?.Effects != null)
            {
                foreach (CardEffectSO effect in card.Effects)
                {
                    if (effect == null)
                    {
                        continue;
                    }

                    switch (effect.Target)
                    {
                        case EffectTarget.EnemyCommander0:
                            AddPresentationTarget(actors, indices, opponent.ActorNumber, GameConstants.FirstCommanderIndex);
                            break;
                        case EffectTarget.EnemyCommander1:
                            AddPresentationTarget(actors, indices, opponent.ActorNumber, GameConstants.SecondCommanderIndex);
                            break;
                        case EffectTarget.AllEnemyCommanders:
                        case EffectTarget.AllCommanders:
                            AddPresentationTarget(actors, indices, opponent.ActorNumber, GameConstants.FirstCommanderIndex);
                            AddPresentationTarget(actors, indices, opponent.ActorNumber, GameConstants.SecondCommanderIndex);
                            break;
                    }
                }
            }

            _lastPlayedTargetActorNumbers = actors.ToArray();
            _lastPlayedTargetCommanderIndices = indices.ToArray();
        }

        /// <summary>
        /// Aggiunge una coppia bersaglio evitando duplicati.
        /// </summary>
        private static void AddPresentationTarget(
            List<int> actors,
            List<int> indices,
            int actorNumber,
            int commanderIndex)
        {
            for (int i = 0; i < actors.Count; i++)
            {
                if (actors[i] == actorNumber && indices[i] == commanderIndex)
                {
                    return;
                }
            }

            actors.Add(actorNumber);
            indices.Add(commanderIndex);
        }

        /// <summary>
        /// Azzera i bersagli temporanei dell'intent appena processato.
        /// </summary>
        private void ClearPendingTargets()
        {
            _pendingTargetActorNumbers = System.Array.Empty<int>();
            _pendingTargetCommanderIndices = System.Array.Empty<int>();
        }

        /// <summary>
        /// Copia un array ricevuto dal trasporto per non conservarne il riferimento mutabile.
        /// </summary>
        /// <param name="source">Array sorgente, eventualmente null.</param>
        /// <returns>Copia indipendente o array vuoto.</returns>
        private static int[] CloneArray(int[] source)
        {
            return source == null || source.Length == 0
                ? System.Array.Empty<int>()
                : (int[])source.Clone();
        }

        /// <summary>
        /// Risolve i bersagli selezionabili di un intent in comandanti concreti.
        /// </summary>
        /// <param name="intent">Intent contenente le coppie (attore, indice).</param>
        /// <returns>Lista dei comandanti bersaglio, o null se assenti.</returns>
        private IReadOnlyList<CommanderState> ResolveTargets(GameIntent intent)
        {
            int count = Mathf.Min(intent.TargetActorNumbers.Length, intent.TargetCommanderIndices.Length);
            if (count == 0)
            {
                return null;
            }

            List<CommanderState> targets = new(count);
            for (int i = 0; i < count; i++)
            {
                PlayerState owner = _gameState.GetPlayerByActor(intent.TargetActorNumbers[i]);
                int index = intent.TargetCommanderIndices[i];
                if (owner != null && index >= 0 && index < owner.Commanders.Length)
                {
                    targets.Add(owner.Commanders[index]);
                }
            }

            return targets;
        }
    }
}
