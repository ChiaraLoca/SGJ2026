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

        [Header("Contenitori")]
        [SerializeField] private Transform _handContainer;
        [SerializeField] private Transform _shopContainer;

        [Header("Etichette")]
        [SerializeField] private Text _phaseLabel;
        [SerializeField] private Text _turnLabel;
        [SerializeField] private Text _creditsLabel;
        [SerializeField] private Text _notesLabel;
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

        /// <summary>
        /// Si iscrive agli aggiornamenti di stato.
        /// </summary>
        private void Awake()
        {
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

            if (_verificaButton != null)
            {
                _verificaButton.onClick.AddListener(_network.SubmitPlayVerifica);
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

            RenderLabels(state, phase, local, isLocalTurn);
            RenderCommanders(local, enemy, localIndex);
            RenderHand(local, phase, isLocalTurn);
            RenderShop(local, phase);
            RenderButtons(phase, isLocalTurn, local);
        }

        /// <summary>
        /// Aggiorna le etichette testuali di fase, turno, credits, note ed esito.
        /// </summary>
        private void RenderLabels(GameStateDTO state, GamePhase phase, PlayerDTO local, bool isLocalTurn)
        {
            if (_phaseLabel != null)
            {
                _phaseLabel.text = phase.ToString();
            }

            if (_turnLabel != null)
            {
                _turnLabel.text = isLocalTurn ? "Il tuo turno" : "Turno avversario";
            }

            if (_creditsLabel != null)
            {
                _creditsLabel.text = $"Credits: {local.Credits}";
            }

            if (_notesLabel != null)
            {
                _notesLabel.text = $"Note: {local.AvailableNotes}";
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
            IReadOnlyList<CommanderDataSO> localData = CommanderDataFor(localIndex);
            IReadOnlyList<CommanderDataSO> enemyData = CommanderDataFor(1 - localIndex);

            BindCommander(_localCommander0, local, GameConstants.FirstCommanderIndex, localData);
            BindCommander(_localCommander1, local, GameConstants.SecondCommanderIndex, localData);
            BindCommander(_enemyCommander0, enemy, GameConstants.FirstCommanderIndex, enemyData);
            BindCommander(_enemyCommander1, enemy, GameConstants.SecondCommanderIndex, enemyData);
        }

        /// <summary>
        /// Lega una view comandante allo snapshot e alla definizione, se entrambi presenti.
        /// </summary>
        private void BindCommander(CommanderView view, PlayerDTO player, int index, IReadOnlyList<CommanderDataSO> data)
        {
            if (view == null || player.Commanders == null || index >= player.Commanders.Length)
            {
                return;
            }

            CommanderDataSO definition = data != null && index < data.Count ? data[index] : null;
            view.Bind(player.Commanders[index], definition);
        }

        /// <summary>
        /// Rigenera le carte in mano del giocatore locale come pulsanti giocabili.
        /// </summary>
        private void RenderHand(PlayerDTO local, GamePhase phase, bool isLocalTurn)
        {
            ClearSpawned(_spawnedHand);
            if (_cardPrefab == null || _handContainer == null)
            {
                return;
            }

            bool playable = phase == GamePhase.Play && isLocalTurn;
            foreach (int cardId in local.HandCardIds)
            {
                CardDataSO card = _network.Registry.GetCard(cardId);
                if (card == null)
                {
                    continue;
                }

                CardView view = Instantiate(_cardPrefab, _handContainer);
                view.Bind(card, OnPlayCardClicked, playable);
                _spawnedHand.Add(view);
            }
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

            foreach (int cardId in local.ShopPoolCardIds)
            {
                CardDataSO card = _network.Registry.GetCard(cardId);
                if (card == null)
                {
                    continue;
                }

                bool affordable = phase == GamePhase.Shop && local.AvailableNotes >= card.ShopCost;
                CardView view = Instantiate(_cardPrefab, _shopContainer);
                view.Bind(card, OnBuyCardClicked, affordable);
                _spawnedShop.Add(view);
            }
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

            if (_verificaButton != null)
            {
                _verificaButton.interactable = inPlay && local.VerificaCardId != CardRegistry.NoCard;
            }

            if (_finishShopButton != null)
            {
                _finishShopButton.interactable = phase == GamePhase.Shop;
            }
        }

        /// <summary>
        /// Inoltra il gioco di una carta standard come intent di rete.
        /// </summary>
        /// <param name="card">Carta cliccata in mano.</param>
        private void OnPlayCardClicked(CardDataSO card)
        {
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
        /// Restituisce le definizioni dei comandanti per l'indice di giocatore (0 o 1).
        /// </summary>
        /// <param name="playerIndex">Indice del giocatore nello stato.</param>
        /// <returns>Lista delle definizioni comandante, o null.</returns>
        private IReadOnlyList<CommanderDataSO> CommanderDataFor(int playerIndex)
        {
            return playerIndex == 0 ? _content.FirstPlayerCommanders : _content.SecondPlayerCommanders;
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
    }
}
