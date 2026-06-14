using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace FourE.UI
{
    /// <summary>
    /// Gestore della scena di test effetti offline. Crea uno stato di gioco completo con 2 giocatori
    /// e 2 comandanti ciascuno per testare gli effetti delle carte isolatamente, senza turni.
    /// </summary>
    public class EffectTestSceneManager : MonoBehaviour
    {
        [SerializeField] private CommanderTestView[] _commanderViews = new CommanderTestView[4];
        [SerializeField] private EffectTestUIController _uiController;

        private FourE.Core.GameStateManager _gameStateManager;
        private FourE.Cards.EffectResolver _effectResolver;
        private bool _passivesEnabled = true;

        /// <summary>
        /// Metodo pubblico per il setup editor: assegna le view dei comandanti.
        /// </summary>
        public void SetCommanderViews(CommanderTestView[] views)
        {
            _commanderViews = views;
        }

        /// <summary>
        /// Metodo pubblico per il setup editor: assegna il controller UI.
        /// </summary>
        public void SetUIController(EffectTestUIController controller)
        {
            _uiController = controller;
        }

        /// <summary>
        /// Ottiene il GameContent dal GameStateManager interno.
        /// </summary>
        public FourE.Config.GameContentSO GetGameContent()
        {
            return _gameStateManager?.Content;
        }

        private void Awake()
        {
            SetupOfflineGameState();
            if (_uiController != null)
            {
                _uiController.SetManager(this);
            }
        }

        /// <summary>
        /// Crea uno stato di gioco offline con 2 giocatori, 2 comandanti ciascuno.
        /// </summary>
        private void SetupOfflineGameState()
        {
            // Ottieni i reference necessari
            var config = FourE.Config.GameConfigSO.Instance;
            if (config == null)
            {
                Debug.LogError("GameConfig non trovato!");
                return;
            }

            // Carica il GameContent
            FourE.Config.GameContentSO content = FindObjectOfType<FourE.Config.GameContentSO>();
            if (content == null)
            {
                content = Resources.Load<FourE.Config.GameContentSO>("GameContent");
            }

            if (content == null)
            {
                Debug.LogError("GameContent non trovato! Assicurati che sia in una cartella Resources o nella scena.");
                return;
            }

            // Crea il GameObject offline DISABILITATO per evitare che Awake venga chiamato subito
            var gameStateGO = new GameObject("OfflineGameState");
            gameStateGO.SetActive(false);
            _gameStateManager = gameStateGO.AddComponent<FourE.Core.GameStateManager>();

            // Ora inizializza offline prima di attivare
            _gameStateManager.InitializeOffline(config);
            _gameStateManager.SetGameContent(content);
            _gameStateManager.AutoStartOffline = false;

            // Attiva il GameObject (ma non fa partire la partita perché AutoStartOffline = false)
            gameStateGO.SetActive(true);

            // Setup commander selections: converte da CommanderDataSO[] a CommanderKind[]
            var p0CmdsData = content.FirstPlayerCommanders.Take(2).ToArray();
            var p1CmdsData = content.SecondPlayerCommanders.Take(2).ToArray();

            var p0Kinds = p0CmdsData.Select(c => c.Kind).ToArray();
            var p1Kinds = p1CmdsData.Select(c => c.Kind).ToArray();

            _gameStateManager.SetCommanderSelections(p0Kinds, p1Kinds);

            // Inizializza lo stato senza avviare la vera partita
            _gameStateManager.StartMatch();

            _effectResolver = new FourE.Cards.EffectResolver();

            // Collega le view ai comandanti
            var player0 = _gameStateManager.Player0;
            var player1 = _gameStateManager.Player1;

            if (_commanderViews.Length >= 2)
            {
                _commanderViews[0].SetCommander(player0.Commanders[0]);
                _commanderViews[1].SetCommander(player0.Commanders[1]);
            }
            if (_commanderViews.Length >= 4)
            {
                _commanderViews[2].SetCommander(player1.Commanders[0]);
                _commanderViews[3].SetCommander(player1.Commanders[1]);
            }
        }

        /// <summary>
        /// Risolve un effetto di una carta su un bersaglio specifico, applicando istantaneamente le modifiche.
        /// </summary>
        public void ResolveEffect(FourE.Cards.CardDataSO card, int activePlayerIndex, int targetCommanderIndex)
        {
            if (card == null) return;

            var activePlayer = activePlayerIndex == 0 ? _gameStateManager.Player0 : _gameStateManager.Player1;
            var inactivePlayer = activePlayerIndex == 0 ? _gameStateManager.Player1 : _gameStateManager.Player0;

            // Crea un contesto con il bersaglio specificato
            var targetCommander = activePlayer.Commanders[targetCommanderIndex];
            var selectedTargets = new[] { targetCommander };

            var context = new FourE.Core.GameContext(
                activePlayer,
                inactivePlayer,
                0,
                _gameStateManager.GameConfig,
                selectedTargets,
                _gameStateManager
            );

            // Risolve gli effetti della carta
            _effectResolver.Resolve(card, context);

            // Aggiorna le view
            RefreshAllViews();
        }

        /// <summary>
        /// Risolve un effetto di una carta sull'avversario.
        /// </summary>
        public void ResolveEffectOnOpponent(FourE.Cards.CardDataSO card, int activePlayerIndex, int targetCommanderIndex)
        {
            if (card == null) return;

            var activePlayer = activePlayerIndex == 0 ? _gameStateManager.Player0 : _gameStateManager.Player1;
            var inactivePlayer = activePlayerIndex == 0 ? _gameStateManager.Player1 : _gameStateManager.Player0;
            var targetCommander = inactivePlayer.Commanders[targetCommanderIndex];

            var selectedTargets = new[] { targetCommander };

            var context = new FourE.Core.GameContext(
                activePlayer,
                inactivePlayer,
                0,
                _gameStateManager.GameConfig,
                selectedTargets,
                _gameStateManager
            );

            _effectResolver.Resolve(card, context);
            RefreshAllViews();
        }

        /// <summary>
        /// Abilita/disabilita le passive di tutti i comandanti.
        /// </summary>
        public void SetPassivesEnabled(bool enabled)
        {
            _passivesEnabled = enabled;
            // TODO: implementare l'effettiva abilitazione/disabilitazione delle passive se necessario
        }

        /// <summary>
        /// Resetta lo stato di gioco al setup iniziale.
        /// </summary>
        public void ResetGameState()
        {
            if (_gameStateManager != null)
            {
                Destroy(_gameStateManager.gameObject);
            }
            SetupOfflineGameState();
            RefreshAllViews();
        }

        /// <summary>
        /// Aggiorna tutte le view dei comandanti.
        /// </summary>
        private void RefreshAllViews()
        {
            foreach (var view in _commanderViews)
            {
                if (view != null)
                {
                    view.Refresh();
                }
            }
        }

        /// <summary>
        /// Ottiene l'elenco di tutti i comandanti per il dropdown di selezione target.
        /// </summary>
        public (string label, int playerIdx, int cmdIdx)[] GetAllCommanders()
        {
            var result = new List<(string, int, int)>();
            var p0 = _gameStateManager.Player0;
            var p1 = _gameStateManager.Player1;

            for (int i = 0; i < 2; i++)
            {
                result.Add(($"Player 0 - {p0.Commanders[i].Data.CommanderName}", 0, i));
                result.Add(($"Player 1 - {p1.Commanders[i].Data.CommanderName}", 1, i));
            }

            return result.ToArray();
        }
    }
}
