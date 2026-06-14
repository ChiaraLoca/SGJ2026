using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FourE.Cards;
using FourE.Commanders;
using FourE.Config;
using FourE.Core;
using FourE.Events;
using FourE.Network;

namespace FourE.UI
{
    /// <summary>
    /// HUD principale: si ridisegna a ogni <see cref="GameStateSyncedEvent"/> a partire dal DTO
    /// e inoltra le azioni del giocatore al <see cref="NetworkGameManager"/>. Bridge puro:
    /// nessuna logica di gioco, solo presentazione e raccolta intent.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class GameView : MonoBehaviour
    {
        [Header("Rete e contenuti")]
        [SerializeField] private NetworkGameManager _network;
        [SerializeField] private GameContentSO _content;

        [Header("Prefab")]
        [SerializeField] private CardView _cardPrefab;
        [SerializeField] private CommanderAbilityPopup _commanderAbilityPopupPrefab;
        [SerializeField] private float _cardPreviewScale = 2.4f;

        [Header("Animazione carta giocata")]
        [SerializeField] private float _playedCardScale = 2.4f;
        [SerializeField] private float _playedCardMoveDuration = 0.4f;
        [SerializeField] private float _playedCardHoldDuration = 2f;
        [SerializeField] private float _playedCardFadeDuration = 0.35f;

        [Header("Contenitori")]
        [SerializeField] private Transform _handContainer;
        [SerializeField] private Transform _shopContainer;
        [SerializeField] private int _handCardsBeforeOverlap = 4;
        [SerializeField] private float _handDefaultSpacing = 8f;

        [Header("Etichette")]
        [SerializeField] private Text _phaseLabel;
        [SerializeField] private Text _turnLabel;
        [SerializeField] private Text _remainingActionsLabel;
        [SerializeField] private Text _creditsLabel;
        [SerializeField] private Text _enemyCreditsLabel;
        [SerializeField] private Text _notesLabel;
        [SerializeField] private Text _deckCountLabel;
        [SerializeField] private Text _discardCountLabel;
        [SerializeField] private Text _outcomeLabel;

        [Header("Comandanti locali")]
        [SerializeField] private CommanderView _localCommander0;
        [SerializeField] private CommanderView _localCommander1;

        [Header("Comandanti avversario")]
        [SerializeField] private CommanderView _enemyCommander0;
        [SerializeField] private CommanderView _enemyCommander1;

        [Header("Azioni")]
        [SerializeField] private Button _endTurnButton;
        [SerializeField] private Button _verificaButton;
        [SerializeField] private Button _finishShopButton;

        private readonly List<CardView> _spawnedHand = new();
        private readonly List<CardView> _spawnedShop = new();
        private Canvas _canvas;
        private CardView _cardPreview;
        private CommanderAbilityPopup _commanderAbilityPopup;
        private CardPlayAnimationController _cardPlayAnimator;
        private readonly Dictionary<(int ActorNumber, int CommanderIndex), bool> _secondaryUnlockStates = new();
        private bool _hasRenderedCommanderUnlockStates;
        private bool _hasObservedPlayedCardSequence;
        private int _lastObservedPlayedCardSequence;
        private int _previousLocalActorNumber = -1;
        private bool _hasPendingLocalPlayStart;
        private Vector2 _pendingLocalPlayStart;
        private int _displayedEnemyCredits;
        private int _enemyCreditsRoundIndex = -1;
        private int _enemyCreditsLocalActorNumber = -1;

        // Stato della selezione bersaglio multi-step.
        private CardDataSO _pendingTargetCard;
        private int _pendingEnemyActorNumber = -1;
        private int _pendingEnemyCommanderIndex = -1;

        /// <summary>
        /// Si iscrive agli aggiornamenti di stato.
        /// </summary>
        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            _shopContainer?.gameObject.SetActive(false);
            _cardPlayAnimator = new CardPlayAnimationController(
                this,
                _canvas,
                _cardPrefab,
                _playedCardScale,
                _playedCardMoveDuration,
                _playedCardHoldDuration,
                _playedCardFadeDuration);
            EventBus.Subscribe<GameStateSyncedEvent>(OnStateSynced);
        }

        /// <summary>
        /// Collega i pulsanti d'azione agli intent di rete.
        /// </summary>
        private void Start()
        {
            if (_endTurnButton != null)
            {
                _endTurnButton.onClick.AddListener(_network.SubmitEndTurn);
            }

            // Il bottone Verifica separato è disattivato: la carta Verifica compare in mano.
            if (_verificaButton != null)
            {
                _verificaButton.gameObject.SetActive(false);
            }

            if (_finishShopButton != null)
            {
                _finishShopButton.onClick.AddListener(_network.SubmitFinishShop);
            }
        }

        /// <summary>
        /// Disiscrive dall'EventBus al teardown.
        /// </summary>
        private void OnDestroy()
        {
            HideCardPreview();
            HideCommanderAbilityPopup();
            _cardPlayAnimator?.Dispose();
            EventBus.Unsubscribe<GameStateSyncedEvent>(OnStateSynced);
        }

        /// <summary>
        /// Ridisegna l'intera HUD a partire dallo stato ricevuto.
        /// </summary>
        /// <param name="sync">Evento di sincronizzazione con DTO e attore locale.</param>
        private void OnStateSynced(GameStateSyncedEvent sync)
        {
            GameStateDTO state = sync.State;
            GamePhase phase = (GamePhase)state.Phase;

            int localIndex = state.Players[0].ActorNumber == sync.LocalActorNumber ? 0 : 1;
            PlayerDTO local = state.Players[localIndex];
            PlayerDTO enemy = state.Players[1 - localIndex];
            bool isLocalTurn = state.ActiveActorNumber == sync.LocalActorNumber;

            bool verificaPlayable = isLocalTurn
                && phase == GamePhase.Play
                && state.CanPlayVerificaThisTurn
                && !local.VerificaBlocked;

            RenderLabels(state, phase, local, enemy, isLocalTurn, sync.LocalActorNumber);
            RenderCommanders(local, enemy, localIndex);
            RenderHand(local, phase, isLocalTurn, verificaPlayable);
            RenderShop(local, phase);
            RenderButtons(phase, isLocalTurn, local);
            AnimatePlayedCard(state);
            _previousLocalActorNumber = sync.LocalActorNumber;
        }

        /// <summary>
        /// Aggiorna le etichette testuali di fase, turno, credits, note ed esito.
        /// </summary>
        private void RenderLabels(
            GameStateDTO state,
            GamePhase phase,
            PlayerDTO local,
            PlayerDTO enemy,
            bool isLocalTurn,
            int localActorNumber)
        {
            if (_phaseLabel != null)
            {
                _phaseLabel.text = phase.ToString();
            }

            if (_turnLabel != null)
            {
                _turnLabel.text = isLocalTurn ? "Il tuo turno" : "Turno avversario";
            }

            if (_remainingActionsLabel != null)
            {
                bool showRemainingActions = phase == GamePhase.Play && isLocalTurn;
                _remainingActionsLabel.gameObject.SetActive(showRemainingActions);
                if (showRemainingActions)
                {
                    _remainingActionsLabel.text = state.RemainingActions.ToString();
                }
            }

            if (_creditsLabel != null)
            {
                _creditsLabel.text = local.Credits.ToString();
            }

            if (_enemyCreditsLabel != null)
            {
                bool shouldRefreshEnemyCredits =
                    _enemyCreditsRoundIndex != state.RoundIndex ||
                    _enemyCreditsLocalActorNumber != localActorNumber ||
                    phase == GamePhase.FinalExam;

                if (shouldRefreshEnemyCredits)
                {
                    _displayedEnemyCredits = enemy.Credits;
                    _enemyCreditsRoundIndex = state.RoundIndex;
                    _enemyCreditsLocalActorNumber = localActorNumber;
                }

                _enemyCreditsLabel.text = _displayedEnemyCredits.ToString();
            }

            if (_notesLabel != null)
            {
                _notesLabel.text = local.Notes.ToString();
            }

            if (_deckCountLabel != null)
            {
                _deckCountLabel.text = local.DeckCount.ToString();
            }

            if (_discardCountLabel != null)
            {
                _discardCountLabel.text = local.DiscardCount.ToString();
            }

            if (_outcomeLabel != null)
            {
                _outcomeLabel.gameObject.SetActive(state.IsGameOver);
                if (state.IsGameOver)
                {
                    _outcomeLabel.text = ResolveOutcomeText(state, local.ActorNumber);
                }
            }
        }

        /// <summary>
        /// Aggiorna le quattro view comandante con snapshot e dati statici.
        /// </summary>
        private void RenderCommanders(PlayerDTO local, PlayerDTO enemy, int localIndex)
        {
            BindCommander(_localCommander0, local, GameConstants.FirstCommanderIndex);
            BindCommander(_localCommander1, local, GameConstants.SecondCommanderIndex);
            BindCommander(_enemyCommander0, enemy, GameConstants.FirstCommanderIndex);
            BindCommander(_enemyCommander1, enemy, GameConstants.SecondCommanderIndex);
            _hasRenderedCommanderUnlockStates = true;
        }

        /// <summary>
        /// Lega una view comandante allo snapshot, risolvendo la definizione dal <see cref="CommanderKind"/>
        /// presente nel DTO (così riflette i comandanti scelti nella schermata di selezione).
        /// </summary>
        private void BindCommander(CommanderView view, PlayerDTO player, int index)
        {
            if (view == null || player.Commanders == null || index >= player.Commanders.Length)
            {
                return;
            }

            CommanderDTO snapshot = player.Commanders[index];
            CommanderDataSO definition = _content.GetCommanderByKind((CommanderKind)snapshot.Kind);
            (int ActorNumber, int CommanderIndex) key = (player.ActorNumber, index);
            bool shouldPlayUnlockEffect =
                _hasRenderedCommanderUnlockStates &&
                _secondaryUnlockStates.TryGetValue(key, out bool wasUnlocked) &&
                !wasUnlocked &&
                snapshot.SecondaryUnlocked;

            _secondaryUnlockStates[key] = snapshot.SecondaryUnlocked;
            view.Bind(
                snapshot,
                definition,
                player.ActorNumber,
                index,
                ShowCommanderAbilityPopup,
                HideCommanderAbilityPopup);

            if (shouldPlayUnlockEffect)
            {
                view.PlaySecondaryUnlockEffect();
            }
        }

        /// <summary>
        /// Mostra al centro del Canvas il riquadro con le abilita del comandante premuto.
        /// </summary>
        /// <param name="data">Definizione del comandante.</param>
        /// <param name="secondaryUnlocked">Stato corrente della passiva secondaria.</param>
        private void ShowCommanderAbilityPopup(CommanderDataSO data, bool secondaryUnlocked)
        {
            HideCommanderAbilityPopup();
            if (_commanderAbilityPopupPrefab == null || _canvas == null || data == null)
            {
                return;
            }

            _commanderAbilityPopup = Instantiate(_commanderAbilityPopupPrefab, _canvas.transform);
            _commanderAbilityPopup.transform.SetAsLastSibling();
            _commanderAbilityPopup.Bind(data, secondaryUnlocked);
            _commanderAbilityPopup.gameObject.SetActive(true);
        }

        /// <summary>
        /// Chiude il riquadro delle abilita del comandante.
        /// </summary>
        private void HideCommanderAbilityPopup()
        {
            if (_commanderAbilityPopup == null)
            {
                return;
            }

            Destroy(_commanderAbilityPopup.gameObject);
            _commanderAbilityPopup = null;
        }

        /// <summary>
        /// Rigenera le carte in mano del giocatore locale come pulsanti giocabili.
        /// </summary>
        private void RenderHand(PlayerDTO local, GamePhase phase, bool isLocalTurn, bool verificaPlayable)
        {
            ClearSpawned(_spawnedHand);
            if (_cardPrefab == null || _handContainer == null)
            {
                return;
            }

            bool turnPlayable = phase == GamePhase.Play && isLocalTurn;
            foreach (int cardId in local.HandCardIds)
            {
                CardDataSO card = _network.Registry.GetCard(cardId);
                if (card == null)
                {
                    continue;
                }

                // La Verifica ha una condizione aggiuntiva: non può essere giocata al 1° turno del round.
                bool playable = card.IsVerifica ? verificaPlayable : turnPlayable;
                CardView view = Instantiate(_cardPrefab, _handContainer);
                view.Bind(card, OnPlayCardClicked, playable, ShowCardPreview, HideCardPreview);
                _spawnedHand.Add(view);
            }

            UpdateHandSpacing();
        }

        /// <summary>
        /// Riduce progressivamente lo spazio tra le carte quando la mano supera la capienza visiva.
        /// </summary>
        private void UpdateHandSpacing()
        {
            if (_handContainer is not RectTransform handRect ||
                !_handContainer.TryGetComponent(out HorizontalLayoutGroup layout))
            {
                return;
            }

            int cardCount = _spawnedHand.Count;
            if (cardCount <= _handCardsBeforeOverlap)
            {
                layout.spacing = _handDefaultSpacing;
                return;
            }

            CardView firstCard = _spawnedHand[0];
            if (firstCard == null || firstCard.transform is not RectTransform cardRect)
            {
                return;
            }

            float availableWidth = handRect.rect.width - layout.padding.horizontal;
            float cardsWidth = cardRect.rect.width * cardCount;
            int spacesCount = cardCount - GameConstants.IndexToCountOffset;
            layout.spacing = Mathf.Min(
                _handDefaultSpacing,
                (availableWidth - cardsWidth) / spacesCount);
            LayoutRebuilder.MarkLayoutForRebuild(handRect);
        }

        /// <summary>
        /// Rigenera il pool shop del giocatore locale come pulsanti acquistabili.
        /// </summary>
        private void RenderShop(PlayerDTO local, GamePhase phase)
        {
            ClearSpawned(_spawnedShop);
            if (_cardPrefab == null || _shopContainer == null)
            {
                return;
            }

            bool isShopVisible = phase == GamePhase.Shop;
            _shopContainer.gameObject.SetActive(isShopVisible);
            if (!isShopVisible)
            {
                return;
            }

            foreach (int cardId in local.ShopPoolCardIds)
            {
                CardDataSO card = _network.Registry.GetCard(cardId);
                if (card == null)
                {
                    continue;
                }

                bool affordable = local.Credits >= card.ShopCost;
                CardView view = Instantiate(_cardPrefab, _shopContainer);
                view.Bind(card, OnBuyCardClicked, affordable, ShowCardPreview, HideCardPreview);
                _spawnedShop.Add(view);
            }
        }

        /// <summary>
        /// Mostra una copia ingrandita e non interattiva della carta al centro del Canvas.
        /// </summary>
        /// <param name="card">Carta da mostrare in anteprima.</param>
        private void ShowCardPreview(CardDataSO card)
        {
            HideCardPreview();
            if (_cardPrefab == null || _canvas == null || card == null)
            {
                return;
            }

            _cardPreview = Instantiate(_cardPrefab, _canvas.transform);
            _cardPreview.BindPreview(card);
            _cardPreview.transform.SetAsLastSibling();

            if (_cardPreview.transform is RectTransform previewTransform)
            {
                Vector2 centerAnchor = Vector2.one * GameConstants.UiCenterAnchor;
                previewTransform.anchorMin = centerAnchor;
                previewTransform.anchorMax = centerAnchor;
                previewTransform.pivot = centerAnchor;
                previewTransform.anchoredPosition = new Vector2(0f, 80f);
                previewTransform.localScale = Vector3.one * _cardPreviewScale;
            }

            CanvasGroup canvasGroup = _cardPreview.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// Chiude l'anteprima carta attualmente visibile.
        /// </summary>
        private void HideCardPreview()
        {
            if (_cardPreview == null)
            {
                return;
            }

            Destroy(_cardPreview.gameObject);
            _cardPreview = null;
        }

        /// <summary>
        /// Abilita i pulsanti d'azione in base alla fase e al turno.
        /// </summary>
        private void RenderButtons(GamePhase phase, bool isLocalTurn, PlayerDTO local)
        {
            bool inPlay = phase == GamePhase.Play && isLocalTurn;

            if (_endTurnButton != null)
            {
                _endTurnButton.interactable = inPlay;
            }

            if (_finishShopButton != null)
            {
                _finishShopButton.interactable = phase == GamePhase.Shop;
            }
        }

        /// <summary>
        /// Inoltra il gioco di una carta standard come intent di rete.
        /// Se la carta richiede selezione bersaglio, avvia il flusso multi-step.
        /// </summary>
        /// <param name="card">Carta cliccata in mano.</param>
        private void OnPlayCardClicked(CardDataSO card)
        {
            if (_pendingTargetCard != null)
            {
                return;
            }

            CaptureLocalPlayStart(card);

            if (card.IsVerifica)
                _network.SubmitPlayVerifica();
            else if (card.RequiresTargetSelection)
                EnterTargetSelectionMode(card);
            else
                _network.SubmitPlayCard(card);
        }

        /// <summary>
        /// Inoltra l'acquisto di una carta come intent di rete.
        /// </summary>
        /// <param name="card">Carta cliccata nello shop.</param>
        private void OnBuyCardClicked(CardDataSO card)
        {
            _network.SubmitBuyCard(card);
        }

        /// <summary>
        /// Rileva una nuova carta giocata nello snapshot e ne avvia l'animazione.
        /// </summary>
        /// <param name="state">Snapshot sincronizzato.</param>
        private void AnimatePlayedCard(GameStateDTO state)
        {
            if (!_hasObservedPlayedCardSequence)
            {
                _hasObservedPlayedCardSequence = true;
                _lastObservedPlayedCardSequence = state.PlayedCardSequence;
                return;
            }

            if (state.PlayedCardSequence <= _lastObservedPlayedCardSequence)
            {
                return;
            }

            _lastObservedPlayedCardSequence = state.PlayedCardSequence;
            CardDataSO card = _network.Registry.GetCard(state.LastPlayedCardId);
            if (card == null)
            {
                return;
            }

            bool wasPlayedLocally = state.LastPlayedActorNumber == _previousLocalActorNumber;
            Vector2 startPosition = wasPlayedLocally && _hasPendingLocalPlayStart
                ? _pendingLocalPlayStart
                : ResolveOpponentPlayStart();

            _hasPendingLocalPlayStart = false;
            _cardPlayAnimator?.Enqueue(card, startPosition);
        }

        /// <summary>
        /// Memorizza la posizione della carta in mano prima dell'invio dell'intent.
        /// </summary>
        /// <param name="card">Carta che il giocatore sta tentando di giocare.</param>
        private void CaptureLocalPlayStart(CardDataSO card)
        {
            foreach (CardView view in _spawnedHand)
            {
                if (view != null && view.Card == card)
                {
                    _pendingLocalPlayStart = WorldToCanvasPosition(view.transform.position);
                    _hasPendingLocalPlayStart = true;
                    return;
                }
            }

            if (_handContainer != null)
            {
                _pendingLocalPlayStart = WorldToCanvasPosition(_handContainer.position);
                _hasPendingLocalPlayStart = true;
            }
        }

        /// <summary>
        /// Calcola il punto di partenza della carta giocata dall'avversario.
        /// </summary>
        /// <returns>Posizione locale nel Canvas vicino alla zona avversaria.</returns>
        private Vector2 ResolveOpponentPlayStart()
        {
            if (_enemyCommander0 != null && _enemyCommander1 != null)
            {
                Vector3 midpoint = (_enemyCommander0.transform.position + _enemyCommander1.transform.position) * 0.5f;
                return WorldToCanvasPosition(midpoint);
            }

            Transform fallback = _enemyCommander0 != null
                ? _enemyCommander0.transform
                : _enemyCommander1 != null
                    ? _enemyCommander1.transform
                    : _handContainer;
            return fallback != null ? WorldToCanvasPosition(fallback.position) : Vector2.zero;
        }

        /// <summary>
        /// Converte una posizione mondo UI nelle coordinate locali del Canvas.
        /// </summary>
        /// <param name="worldPosition">Posizione mondo dell'elemento sorgente.</param>
        /// <returns>Posizione locale rispetto al RectTransform del Canvas.</returns>
        private Vector2 WorldToCanvasPosition(Vector3 worldPosition)
        {
            if (_canvas == null || _canvas.transform is not RectTransform canvasTransform)
            {
                return Vector2.zero;
            }

            Camera eventCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _canvas.worldCamera;
            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(eventCamera, worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasTransform,
                screenPosition,
                eventCamera,
                out Vector2 localPosition);
            return localPosition;
        }

        /// <summary>
        /// Compone il testo d'esito dal punto di vista del giocatore locale.
        /// </summary>
        private string ResolveOutcomeText(GameStateDTO state, int localActorNumber)
        {
            if (state.IsDraw)
            {
                return "Pareggio";
            }

            return state.WinnerActorNumber == localActorNumber ? "Hai vinto!" : "Hai perso";
        }

        /// <summary>
        /// Distrugge le card view generate e svuota la lista.
        /// </summary>
        /// <param name="spawned">Lista delle view da ripulire.</param>
        private void ClearSpawned(List<CardView> spawned)
        {
            foreach (CardView view in spawned)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            spawned.Clear();
        }

        /// <summary>
        /// Avvia la selezione bersaglio in base al tipo di scelta richiesto dalla carta:
        /// solo nemici, solo propri, entrambi (sequenziale), o qualsiasi (tutti e 4).
        /// </summary>
        /// <param name="card">Carta in attesa di bersaglio.</param>
        private void EnterTargetSelectionMode(CardDataSO card)
        {
            _pendingTargetCard = card;
            _pendingEnemyActorNumber = -1;
            _pendingEnemyCommanderIndex = -1;

            if (card.RequiresEnemyTargetSelection)
                ShowEnemySelection();
            else if (card.RequiresAnyTargetSelection)
                ShowAllSelection();
            else
                ShowOwnSelection();
        }

        /// <summary>Rende selezionabili tutti e 4 i comandanti (target: qualsiasi comandante).</summary>
        private void ShowAllSelection()
        {
            _localCommander0?.SetSelectable(true, OnAnyCommanderSelected);
            _localCommander1?.SetSelectable(true, OnAnyCommanderSelected);
            _enemyCommander0?.SetSelectable(true, OnAnyCommanderSelected);
            _enemyCommander1?.SetSelectable(true, OnAnyCommanderSelected);
        }

        /// <summary>
        /// Callback per selezione libera (qualsiasi comandante).
        /// Invia l'intent con il bersaglio scelto e chiude la selezione.
        /// </summary>
        private void OnAnyCommanderSelected(int actorNumber, int commanderIndex)
        {
            if (_pendingTargetCard == null) return;
            CardDataSO card = _pendingTargetCard;
            ExitTargetSelectionMode();
            _network.SubmitPlayCard(card, new[] { actorNumber }, new[] { commanderIndex });
        }

        /// <summary>Attiva solo i comandanti avversari come selezionabili (step nemico).</summary>
        private void ShowEnemySelection()
        {
            _localCommander0?.SetSelectable(false, null);
            _localCommander1?.SetSelectable(false, null);
            _enemyCommander0?.SetSelectable(true, OnEnemyCommanderSelected);
            _enemyCommander1?.SetSelectable(true, OnEnemyCommanderSelected);
        }

        /// <summary>Attiva solo i comandanti propri come selezionabili (step proprio).</summary>
        private void ShowOwnSelection()
        {
            _localCommander0?.SetSelectable(true, OnOwnCommanderSelected);
            _localCommander1?.SetSelectable(true, OnOwnCommanderSelected);
            _enemyCommander0?.SetSelectable(false, null);
            _enemyCommander1?.SetSelectable(false, null);
        }

        /// <summary>
        /// Callback del primo step: comandante avversario scelto.
        /// Se la carta richiede anche un comandante proprio, avanza allo step due.
        /// Altrimenti invia subito l'intent.
        /// </summary>
        private void OnEnemyCommanderSelected(int actorNumber, int commanderIndex)
        {
            if (_pendingTargetCard == null) return;

            if (_pendingTargetCard.RequiresOwnTargetSelection)
            {
                _pendingEnemyActorNumber = actorNumber;
                _pendingEnemyCommanderIndex = commanderIndex;
                _enemyCommander0?.SetSelectable(false, null);
                _enemyCommander1?.SetSelectable(false, null);
                ShowOwnSelection();
            }
            else
            {
                CardDataSO card = _pendingTargetCard;
                ExitTargetSelectionMode();
                _network.SubmitPlayCard(card, new[] { actorNumber }, new[] { commanderIndex });
            }
        }

        /// <summary>
        /// Callback del secondo step (o unico step se solo selezione propria):
        /// comandante proprio scelto. Invia l'intent con tutti i bersagli raccolti.
        /// </summary>
        private void OnOwnCommanderSelected(int actorNumber, int commanderIndex)
        {
            if (_pendingTargetCard == null) return;

            CardDataSO card = _pendingTargetCard;
            int savedEnemyActor = _pendingEnemyActorNumber;
            int savedEnemyIndex = _pendingEnemyCommanderIndex;
            ExitTargetSelectionMode();

            if (savedEnemyActor >= 0)
            {
                // Invia nemico prima, poi proprio: GameContext li smista per lato.
                _network.SubmitPlayCard(card,
                    new[] { savedEnemyActor, actorNumber },
                    new[] { savedEnemyIndex, commanderIndex });
            }
            else
            {
                _network.SubmitPlayCard(card, new[] { actorNumber }, new[] { commanderIndex });
            }
        }

        /// <summary>
        /// Esce dalla modalità selezione e rimuove tutti gli overlay dai comandanti.
        /// </summary>
        private void ExitTargetSelectionMode()
        {
            _pendingTargetCard = null;
            _pendingEnemyActorNumber = -1;
            _pendingEnemyCommanderIndex = -1;
            _localCommander0?.SetSelectable(false, null);
            _localCommander1?.SetSelectable(false, null);
            _enemyCommander0?.SetSelectable(false, null);
            _enemyCommander1?.SetSelectable(false, null);
        }
    }
}
