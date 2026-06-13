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
            var gameStateGO = new GameObject("OfflineGameState");
            _gameStateManager = gameStateGO.AddComponent<FourE.Core.GameStateManager>();
            _gameStateManager.AutoStartOffline = false; // Non far partire la partita automaticamente

            var config = FourE.Config.GameConfigSO.Instance;
            var content = FourE.Config.GameContentSO.Instance;

            // Setup commander selections: usa il default da GameContent
            var p0Cmds = content.FirstPlayerCommanders.Take(2).ToArray();
            var p1Cmds = content.SecondPlayerCommanders.Take(2).ToArray();

            _gameStateManager.SetCommanderSelections(p0Cmds, p1Cmds);

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
                result.Add(($"Player 0 - {p0.Commanders[i].CommanderData.CommanderName}", 0, i));
                result.Add(($"Player 1 - {p1.Commanders[i].CommanderData.CommanderName}", 1, i));
            }

            return result.ToArray();
        }
    }
}
