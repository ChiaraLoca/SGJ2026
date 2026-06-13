using System.Collections.Generic;
using UnityEngine;
using FourE.Cards;
using FourE.Commanders;
using FourE.Config;
using FourE.Players;
using FourE.Shop;

namespace FourE.Core
{
    /// <summary>
    /// Gestore dello stato globale della partita. Singleton che possiede gli stati dei
    /// giocatori e costruisce/coordina i manager di flusso (round, turni, shop, fasi).
    /// </summary>
    public sealed class GameStateManager : MonoBehaviour
    {
        [Header("Configurazione")]
        [SerializeField] private GameConfigSO _gameConfig;
        [SerializeField] private GameContentSO _content;

        [Header("Avvio")]
        [Tooltip("Se attivo, la partita parte da sola in Start (utile per test offline).")]
        [SerializeField] private bool _autoStartOffline = true;
        [Tooltip("Seme casuale fisso per partite deterministiche; 0 = casuale.")]
        [SerializeField] private int _randomSeed;

        [Header("Debug Runtime")]
        [SerializeField] private RoundManager _rounds;
        [SerializeField] private TurnManager _turns;
        [SerializeField] private ShopManager _shop;
        [SerializeField] private PhaseManager _phases;
        private IStartingPlayerDecider _startingPlayerDecider;
        private System.Random _rng;

        /// <summary>Istanza singleton accessibile dai sistemi che non si risolvono via Inspector.</summary>
        public static GameStateManager Instance { get; private set; }

        /// <summary>Configurazione di gioco attiva.</summary>
        public GameConfigSO GameConfig => _gameConfig;

        /// <summary>Archivio dei contenuti della partita.</summary>
        public GameContentSO Content => _content;

        /// <summary>Stato del primo giocatore.</summary>
        [field:SerializeField] public PlayerState Player0 { get; private set; }

        /// <summary>Stato del secondo giocatore.</summary>
        [field:SerializeField] public PlayerState Player1 { get; private set; }

        /// <summary>Giocatore di turno corrente.</summary>
        [field: SerializeField] public PlayerState ActivePlayer { get; private set; }

        /// <summary>Avversario del giocatore di turno.</summary>
        public PlayerState InactivePlayer => ActivePlayer == Player0 ? Player1 : Player0;

        /// <summary>Indice del round corrente, 0-based.</summary>
        public int CurrentRoundIndex => _rounds?.CurrentRoundIndex ?? 0;

        /// <summary>Fase corrente della partita.</summary>
        public GamePhase CurrentPhase => _phases?.CurrentPhase ?? GamePhase.Setup;

        /// <summary>Gestore dei turni, usato dal livello di rete per inoltrare gli intent.</summary>
        public TurnManager Turns => _turns;

        /// <summary>Gestore dello shop, usato dal livello di rete per inoltrare gli intent.</summary>
        public ShopManager Shop => _shop;

        /// <summary>Gestore delle fasi.</summary>
        public PhaseManager Phases => _phases;

        /// <summary>
        /// Inizializza il singleton, registra il config e prepara il generatore casuale.
        /// </summary>
        private void Awake()
        {
            Instance = this;
            _gameConfig.RegisterAsActive();
            _rng = _randomSeed != 0 ? new System.Random(_randomSeed) : new System.Random();
        }

        /// <summary>
        /// Avvia la partita in automatico se configurato per il test offline.
        /// </summary>
        private void Start()
        {
            if (_autoStartOffline)
            {
                StartMatch();
            }
        }

        /// <summary>
        /// Costruisce stati e manager e avvia la partita scegliendo casualmente il primo giocatore.
        /// </summary>
        public void StartMatch()
        {
            BuildPlayers();
            BuildManagers();

            PlayerState first = _startingPlayerDecider.DecideStartingPlayer(Player0, Player1);
            _phases.BeginMatch(first);
        }

        /// <summary>
        /// Imposta il giocatore attivo. Invocato dal TurnManager.
        /// </summary>
        /// <param name="player">Nuovo giocatore attivo.</param>
        public void SetActivePlayer(PlayerState player)
        {
            ActivePlayer = player;
        }

        /// <summary>
        /// Restituisce l'avversario del giocatore indicato.
        /// </summary>
        /// <param name="player">Giocatore di riferimento.</param>
        /// <returns>L'altro giocatore.</returns>
        public PlayerState OpponentOf(PlayerState player)
        {
            return player == Player0 ? Player1 : Player0;
        }

        /// <summary>
        /// Risolve un giocatore dal suo numero attore Photon.
        /// </summary>
        /// <param name="actorNumber">Numero attore.</param>
        /// <returns>Lo stato del giocatore, o null se non trovato.</returns>
        public PlayerState GetPlayerByActor(int actorNumber)
        {
            if (Player0 != null && Player0.ActorNumber == actorNumber)
            {
                return Player0;
            }

            return Player1 != null && Player1.ActorNumber == actorNumber ? Player1 : null;
        }

        /// <summary>
        /// Crea un contesto di risoluzione per la carta del giocatore attivo.
        /// </summary>
        /// <param name="selectedTargets">Comandanti scelti a runtime, se la carta lo richiede.</param>
        /// <returns>Contesto con lo stato corrente.</returns>
        public GameContext BuildContext(IReadOnlyList<CommanderState> selectedTargets = null)
        {
            return new GameContext(ActivePlayer, InactivePlayer, CurrentRoundIndex, _gameConfig, selectedTargets, this);
        }

        /// <summary>
        /// Costruisce gli stati dei due giocatori dall'archivio contenuti.
        /// </summary>
        private void BuildPlayers()
        {
            Player0 = MatchSetup.BuildPlayer(
                GameConstants.FirstCommanderIndex,
                _content.FirstPlayerCommanders,
                _content.VerificaCard,
                _content.ShopCatalog,
                _gameConfig,
                _rng);

            Player1 = MatchSetup.BuildPlayer(
                GameConstants.SecondCommanderIndex,
                _content.SecondPlayerCommanders,
                _content.VerificaCard,
                _content.ShopCatalog,
                _gameConfig,
                _rng);
        }

        /// <summary>
        /// Costruisce e cabla i manager di flusso, rompendo la dipendenza turni↔fasi.
        /// </summary>
        private void BuildManagers()
        {
            _rounds = new RoundManager(_gameConfig);
            _shop = new ShopManager(this, _rng);
            _turns = new TurnManager(this, new EffectResolver());
            _startingPlayerDecider = new CoinFlipStartingPlayerDecider(_rng);
            _phases = new PhaseManager(this, _turns, _shop, _rounds, _startingPlayerDecider, _rng);
            _turns.SetPhaseManager(_phases);
        }

        /// <summary>
        /// Rilascia il singleton alla distruzione.
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
