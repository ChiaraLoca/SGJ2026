using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FourE.Commanders;
using FourE.Config;
using FourE.Network;
#if PHOTON_UNITY_NETWORKING
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
#endif

namespace FourE.UI
{
    /// <summary>
    /// Schermata di selezione comandanti. Ogni giocatore sceglie, in modo indipendente,
    /// <see cref="GameConstants.CommandersPerPlayer"/> comandanti distinti.
    /// In <b>hotseat</b> i due giocatori scelgono a turno sullo stesso dispositivo.
    /// In <b>online</b> ciascuno sceglie sul proprio dispositivo: la scelta viaggia come Custom
    /// Property Photon e l'host, ricevute entrambe, le salva in <see cref="SessionConfig"/> e
    /// carica la scena di gioco (sincronizzata sull'ospite). Bridge puro UI↔sessione.
    /// </summary>
    public sealed class CommanderSelectController : MonoBehaviour
#if PHOTON_UNITY_NETWORKING
        , IInRoomCallbacks
#endif
    {
        [Header("Contenuti")]
        [SerializeField] private GameContentSO _content;

        [Header("Opzioni (prefab-first)")]
        [SerializeField] private CommanderOptionView _optionPrefab;
        [SerializeField] private Transform _optionsContainer;

        [Header("Dimensioni griglia")]
        [Tooltip("Larghezza × altezza di ogni carta comandante nella griglia (proporzione 2/3 = verticale).")]
        [SerializeField] private Vector2 _commanderCardCellSize = new Vector2(200f, 300f);

        [Header("Pannello dettaglio")]
        [SerializeField] private GameObject _detailPanel;
        [SerializeField] private Text _detailNameLabel;
        [SerializeField] private Text _detailBaseAbilityLabel;
        [SerializeField] private Text _detailUnlockLabel;
        [SerializeField] private Text _detailSecondaryAbilityLabel;
        [SerializeField] private Button _selectCommanderButton;
        [SerializeField] private Text _selectCommanderButtonLabel;
        [SerializeField] private Button _closeDetailButton;

        [Header("Etichette")]
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _selectionLabel;

        [Header("Azioni")]
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _clearButton;

        [Header("Scena di gioco")]
        [Tooltip("Scena caricata dopo che tutti i giocatori hanno scelto.")]
        [SerializeField] private string _gameSceneName = "SampleUI";

        /// <summary>Chiave della Custom Property Photon che trasporta la scelta (array di int = CommanderKind).</summary>
        private const string CommanderSelectionPropertyKey = "cmd";

        private readonly List<CommanderOptionView> _options = new();
        private readonly List<CommanderKind> _currentPicks = new();
        private int _currentPlayerStep;
        private bool _online;
        private bool _awaitingOthers;
        private CommanderKind _inspectedKind;
        private bool _hasInspectedKind;

        /// <summary>
        /// Genera le opzioni dal catalogo, collega i pulsanti e avvia la selezione.
        /// </summary>
        private void Start()
        {
            _online = SessionConfig.Mode == NetworkMode.Online;

            if (_detailPanel != null) _detailPanel.SetActive(false);
            if (_selectCommanderButton != null) _selectCommanderButton.onClick.AddListener(OnSelectInspectedClicked);
            if (_closeDetailButton != null) _closeDetailButton.onClick.AddListener(HideDetailPanel);

            BuildOptions();

            if (_confirmButton != null) _confirmButton.onClick.AddListener(OnConfirmClicked);
            if (_clearButton != null) _clearButton.onClick.AddListener(ResetCurrentPicks);

            if (_online)
            {
                BeginOnlineSelection();
            }
            else
            {
                BeginPlayerStep(0);
            }
        }

        /// <summary>
        /// Istanzia una <see cref="CommanderOptionView"/> per ogni comandante del catalogo
        /// e applica le dimensioni configurate alla griglia.
        /// </summary>
        private void BuildOptions()
        {
            if (_optionPrefab == null || _optionsContainer == null || _content == null)
            {
                return;
            }

            if (_optionsContainer.TryGetComponent(out GridLayoutGroup grid))
            {
                grid.cellSize = _commanderCardCellSize;
            }

            foreach (CommanderDataSO data in _content.CommanderCatalog)
            {
                if (data == null)
                {
                    continue;
                }

                CommanderOptionView view = Instantiate(_optionPrefab, _optionsContainer);
                view.Bind(data, OnCommanderInspected);
                _options.Add(view);
            }
        }

        /// <summary>
        /// Mostra nel pannello di dettaglio le abilità del comandante selezionato.
        /// Non aggiunge ancora il comandante alle scelte correnti.
        /// </summary>
        /// <param name="kind">Comandante toccato nella griglia.</param>
        private void OnCommanderInspected(CommanderKind kind)
        {
            if (_awaitingOthers)
            {
                return;
            }

            _inspectedKind = kind;
            _hasInspectedKind = true;

            CommanderDataSO data = _content.GetCommanderByKind(kind);
            ShowDetailPanel(data);
        }

        /// <summary>
        /// Popola e mostra il pannello con nome, abilità e condizione di sblocco del comandante.
        /// </summary>
        /// <param name="data">Definizione del comandante da visualizzare; null nasconde il pannello.</param>
        private void ShowDetailPanel(CommanderDataSO data)
        {
            if (_detailPanel != null)
            {
                _detailPanel.SetActive(data != null);
            }

            if (data == null)
            {
                return;
            }

            if (_detailNameLabel != null) _detailNameLabel.text = data.CommanderName;
            if (_detailBaseAbilityLabel != null)
            {
                _detailBaseAbilityLabel.text = $"ABILITA BASE\n{data.BaseAbilityDescription}";
            }

            if (_detailUnlockLabel != null)
            {
                _detailUnlockLabel.text = $"SBLOCCO\n{data.UnlockConditionDescription}";
            }

            if (_detailSecondaryAbilityLabel != null)
            {
                _detailSecondaryAbilityLabel.text = $"ABILITA SECONDARIA\n{data.SecondaryAbilityDescription}";
            }

            RefreshSelectionButton();
        }

        /// <summary>
        /// Aggiunge o rimuove dalle scelte il comandante mostrato nel pannello di dettaglio.
        /// </summary>
        private void OnSelectInspectedClicked()
        {
            if (!_hasInspectedKind || _awaitingOthers)
            {
                return;
            }

            int selectedIndex = _currentPicks.IndexOf(_inspectedKind);
            if (selectedIndex >= 0)
            {
                _currentPicks.RemoveAt(selectedIndex);
                RefreshSelectionUI();
                HideDetailPanel();
                return;
            }

            OnCommanderPicked(_inspectedKind);
            HideDetailPanel();
        }

        /// <summary>
        /// Chiude il popup di dettaglio e azzera il comandante ispezionato.
        /// </summary>
        private void HideDetailPanel()
        {
            _hasInspectedKind = false;
            if (_detailPanel != null)
            {
                _detailPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Aggiunge un comandante distinto alle scelte correnti, se non si è già al massimo.
        /// </summary>
        /// <param name="kind">Comandante scelto.</param>
        private void OnCommanderPicked(CommanderKind kind)
        {
            if (_awaitingOthers ||
                _currentPicks.Count >= GameConstants.CommandersPerPlayer ||
                _currentPicks.Contains(kind))
            {
                return;
            }

            _currentPicks.Add(kind);
            RefreshSelectionUI();
        }

        /// <summary>
        /// Svuota le scelte correnti del giocatore.
        /// </summary>
        private void ResetCurrentPicks()
        {
            if (_awaitingOthers)
            {
                return;
            }

            _currentPicks.Clear();
            HideDetailPanel();
            RefreshSelectionUI();
        }

        /// <summary>
        /// Aggiorna etichetta di riepilogo, evidenziazioni e stato del pulsante di conferma.
        /// </summary>
        private void RefreshSelectionUI()
        {
            if (_selectionLabel != null)
            {
                _selectionLabel.text = _currentPicks.Count == 0
                    ? "Nessun comandante scelto"
                    : $"Scelti: {DescribePicks()}";
            }

            foreach (CommanderOptionView option in _options)
            {
                option.SetSelected(false);
            }

            foreach (CommanderKind pick in _currentPicks)
            {
                int catalogIndex = IndexOfKind(pick);
                if (catalogIndex >= 0 && catalogIndex < _options.Count)
                {
                    _options[catalogIndex].SetSelected(true);
                }
            }

            if (_confirmButton != null)
            {
                _confirmButton.interactable = _currentPicks.Count == GameConstants.CommandersPerPlayer;
            }

            RefreshSelectionButton();
        }

        /// <summary>
        /// Aggiorna testo e interattività del pulsante Seleziona/Deseleziona del dettaglio.
        /// </summary>
        private void RefreshSelectionButton()
        {
            if (_selectCommanderButton == null)
            {
                return;
            }

            bool isSelected = _hasInspectedKind && _currentPicks.Contains(_inspectedKind);
            bool hasAvailableSlot = _currentPicks.Count < GameConstants.CommandersPerPlayer;
            _selectCommanderButton.interactable = !_awaitingOthers && _hasInspectedKind;

            if (_selectCommanderButtonLabel != null)
            {
                _selectCommanderButtonLabel.text = isSelected
                    ? "DESELEZIONA"
                    : hasAvailableSlot
                        ? "SELEZIONA"
                        : "CHIUDI";
            }
        }

        /// <summary>
        /// Conferma le scelte: in hotseat passa al giocatore successivo o avvia la partita;
        /// in online pubblica la scelta e attende l'avversario.
        /// </summary>
        private void OnConfirmClicked()
        {
            if (_awaitingOthers || _currentPicks.Count != GameConstants.CommandersPerPlayer)
            {
                return;
            }

            if (_online)
            {
                ConfirmOnline();
            }
            else
            {
                ConfirmHotseat();
            }
        }

        // =====================================================================
        // Hotseat
        // =====================================================================

        /// <summary>
        /// Avvia la selezione del giocatore indicato, azzerando le scelte correnti.
        /// </summary>
        /// <param name="playerStep">Indice del giocatore che sta scegliendo (0-based).</param>
        private void BeginPlayerStep(int playerStep)
        {
            _currentPlayerStep = playerStep;
            ResetCurrentPicks();

            if (_titleLabel != null)
            {
                int playerNumber = playerStep + GameConstants.IndexToCountOffset;
                _titleLabel.text = $"Giocatore {playerNumber}: scegli {GameConstants.CommandersPerPlayer} comandanti";
            }
        }

        /// <summary>
        /// Conferma hotseat: salva le scelte e passa al giocatore successivo, o avvia la partita.
        /// </summary>
        private void ConfirmHotseat()
        {
            StorePicks(_currentPlayerStep, _currentPicks.ToArray());

            int nextStep = _currentPlayerStep + GameConstants.IndexToCountOffset;
            if (nextStep < GameConstants.PlayersPerMatch)
            {
                BeginPlayerStep(nextStep);
            }
            else
            {
                SceneManager.LoadScene(_gameSceneName);
            }
        }

        /// <summary>
        /// Salva le scelte di un giocatore nello <see cref="SessionConfig"/>.
        /// </summary>
        /// <param name="playerStep">Indice del giocatore (0 = Player0, 1 = Player1).</param>
        /// <param name="picks">Comandanti scelti.</param>
        private static void StorePicks(int playerStep, CommanderKind[] picks)
        {
            if (playerStep == 0)
            {
                SessionConfig.Player0Commanders = picks;
            }
            else
            {
                SessionConfig.Player1Commanders = picks;
            }
        }

        // =====================================================================
        // Online
        // =====================================================================

        /// <summary>
        /// Avvia la selezione online (un solo step: il giocatore locale sceglie per sé).
        /// </summary>
        private void BeginOnlineSelection()
        {
            ResetCurrentPicks();
            if (_titleLabel != null)
            {
                _titleLabel.text = $"Scegli i tuoi {GameConstants.CommandersPerPlayer} comandanti";
            }

#if PHOTON_UNITY_NETWORKING
            PhotonNetwork.AddCallbackTarget(this);
#endif
        }

        /// <summary>
        /// Conferma online: pubblica la scelta del giocatore locale e attende l'avversario.
        /// </summary>
        private void ConfirmOnline()
        {
#if PHOTON_UNITY_NETWORKING
            int[] encoded = EncodeKinds(_currentPicks);
            Hashtable props = new() { { CommanderSelectionPropertyKey, encoded } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            EnterWaitingState();
            TryHostStartMatch();
#else
            // Senza PUN2 l'online non è disponibile: ripiega caricando la partita con le scelte locali.
            SessionConfig.Player0Commanders = _currentPicks.ToArray();
            SceneManager.LoadScene(_gameSceneName);
#endif
        }

        /// <summary>
        /// Blocca la UI di selezione e mostra l'attesa dell'avversario.
        /// </summary>
        private void EnterWaitingState()
        {
            _awaitingOthers = true;
            HideDetailPanel();
            if (_confirmButton != null) _confirmButton.interactable = false;
            if (_clearButton != null) _clearButton.interactable = false;
            if (_titleLabel != null) _titleLabel.text = "In attesa dell'altro giocatore…";
            RefreshSelectionButton();
        }

#if PHOTON_UNITY_NETWORKING
        /// <summary>
        /// Sull'host: se entrambi i giocatori hanno pubblicato la scelta, le salva in
        /// <see cref="SessionConfig"/> (MasterClient → Player0, ospite → Player1) e carica la scena di gioco.
        /// </summary>
        private void TryHostStartMatch()
        {
            if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            {
                return;
            }

            CommanderKind[] masterPicks = null;
            CommanderKind[] guestPicks = null;

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                // La Hashtable Photon è non-generica: l'indexer restituisce null se la chiave manca.
                if (player.CustomProperties[CommanderSelectionPropertyKey] is not int[] encoded)
                {
                    return; // qualcuno non ha ancora scelto
                }

                CommanderKind[] decoded = DecodeKinds(encoded);
                if (player.IsMasterClient)
                {
                    masterPicks = decoded;
                }
                else
                {
                    guestPicks = decoded;
                }
            }

            if (masterPicks == null || guestPicks == null)
            {
                return;
            }

            // MasterClient è l'host = Player0; l'ospite = Player1.
            SessionConfig.Player0Commanders = masterPicks;
            SessionConfig.Player1Commanders = guestPicks;

            PhotonNetwork.LoadLevel(_gameSceneName);
        }

        /// <summary>Rimuove le callback Photon al teardown.</summary>
        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        /// <inheritdoc />
        public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (changedProps != null && changedProps.ContainsKey(CommanderSelectionPropertyKey))
            {
                TryHostStartMatch();
            }
        }

        /// <inheritdoc />
        public void OnPlayerEnteredRoom(Player newPlayer) { }
        /// <inheritdoc />
        public void OnPlayerLeftRoom(Player otherPlayer) { }
        /// <inheritdoc />
        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) { }
        /// <inheritdoc />
        public void OnMasterClientSwitched(Player newMasterClient) { }

        /// <summary>Codifica i kind scelti in un array di int per la Custom Property.</summary>
        private static int[] EncodeKinds(List<CommanderKind> kinds)
        {
            int[] encoded = new int[kinds.Count];
            for (int i = 0; i < kinds.Count; i++)
            {
                encoded[i] = (int)kinds[i];
            }

            return encoded;
        }

        /// <summary>Decodifica un array di int in kind comandante.</summary>
        private static CommanderKind[] DecodeKinds(int[] encoded)
        {
            CommanderKind[] kinds = new CommanderKind[encoded.Length];
            for (int i = 0; i < encoded.Length; i++)
            {
                kinds[i] = (CommanderKind)encoded[i];
            }

            return kinds;
        }
#endif

        // =====================================================================
        // Helper comuni
        // =====================================================================

        /// <summary>
        /// Compone un riepilogo testuale dei comandanti scelti.
        /// </summary>
        /// <returns>Nomi dei comandanti scelti separati da virgola.</returns>
        private string DescribePicks()
        {
            string[] names = new string[_currentPicks.Count];
            for (int i = 0; i < _currentPicks.Count; i++)
            {
                CommanderDataSO data = _content.GetCommanderByKind(_currentPicks[i]);
                names[i] = data != null ? data.CommanderName : _currentPicks[i].ToString();
            }

            return string.Join(", ", names);
        }

        /// <summary>
        /// Trova l'indice nel catalogo del comandante con il kind indicato.
        /// </summary>
        /// <param name="kind">Kind da cercare.</param>
        /// <returns>Indice nel catalogo, o -1 se assente.</returns>
        private int IndexOfKind(CommanderKind kind)
        {
            IReadOnlyList<CommanderDataSO> catalog = _content.CommanderCatalog;
            for (int i = 0; i < catalog.Count; i++)
            {
                if (catalog[i] != null && catalog[i].Kind == kind)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
