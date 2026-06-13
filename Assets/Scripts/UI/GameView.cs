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
        private CardDataSO _pendingTargetCard;

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
                _notesLabel.text = $"Note: {local.Notes}";
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
            view.Bind(player.Commanders[index], definition, player.ActorNumber, index);
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

                bool affordable = phase == GamePhase.Shop && local.Credits >= card.ShopCost;
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
            if (_pendingTargetCard != null)
            {
                return;
            }

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

        /// <summary>
        /// Entra in modalità selezione bersaglio: mostra l'overlay su tutti i comandanti.
        /// </summary>
        /// <param name="card">Carta in attesa di bersaglio.</param>
        private void EnterTargetSelectionMode(CardDataSO card)
        {
            _pendingTargetCard = card;
            Action<int, int> onSelect = OnCommanderSelected;
            _localCommander0?.SetSelectable(true, onSelect);
            _localCommander1?.SetSelectable(true, onSelect);
            _enemyCommander0?.SetSelectable(true, onSelect);
            _enemyCommander1?.SetSelectable(true, onSelect);
        }

        /// <summary>
        /// Riceve il bersaglio scelto, invia l'intent e termina la selezione.
        /// </summary>
        /// <param name="actorNumber">Attore del comandante bersaglio.</param>
        /// <param name="commanderIndex">Indice del comandante bersaglio.</param>
        private void OnCommanderSelected(int actorNumber, int commanderIndex)
        {
            if (_pendingTargetCard == null)
            {
                return;
            }

            CardDataSO card = _pendingTargetCard;
            ExitTargetSelectionMode();
            _network.SubmitPlayCard(card, new[] { actorNumber }, new[] { commanderIndex });
        }

        /// <summary>
        /// Esce dalla modalità selezione e nasconde gli overlay comandante.
        /// </summary>
        private void ExitTargetSelectionMode()
        {
            _pendingTargetCard = null;
            _localCommander0?.SetSelectable(false, null);
            _localCommander1?.SetSelectable(false, null);
            _enemyCommander0?.SetSelectable(false, null);
            _enemyCommander1?.SetSelectable(false, null);
        }
    }
}
