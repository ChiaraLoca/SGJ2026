using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FourE.Network;

namespace FourE.UI
{
    /// <summary>
    /// Menu iniziale: sceglie tra partita sullo stesso dispositivo (hotseat) e partita online
    /// per codice stanza. Bridge puro UI↔rete: imposta <see cref="SessionConfig"/> e delega
    /// connessione/accoppiamento a <see cref="OnlineLauncher"/>, senza logica di gioco.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Rete")]
        [SerializeField] private OnlineLauncher _launcher;
        [Tooltip("Scena di gioco caricata in modalità hotseat (l'online la carica via Photon).")]
        [SerializeField] private string _gameSceneName = "SampleScene";
        [Tooltip("Scena di selezione comandanti caricata in hotseat prima della partita.")]
        [SerializeField] private string _selectionSceneName = "CommanderSelect";

        [Header("Pannelli")]
        [SerializeField] private GameObject _modePanel;
        [SerializeField] private GameObject _roomPanel;

        [Header("Scelta modalità")]
        [SerializeField] private Button _sameDeviceButton;
        [SerializeField] private Button _onlineButton;

        [Header("Stanza online")]
        [SerializeField] private Button _createRoomButton;
        [SerializeField] private Button _joinRoomButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private InputField _roomCodeInput;
        [SerializeField] private Text _roomCodeDisplay;
        [SerializeField] private Text _statusLabel;

        /// <summary>Caratteri ammessi nei codici stanza generati (niente caratteri ambigui).</summary>
        private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        /// <summary>Lunghezza del codice stanza generato dall'host.</summary>
        private const int CodeLength = 5;

        /// <summary>
        /// Collega i pulsanti, sottoscrive gli eventi del launcher e mostra il pannello iniziale.
        /// </summary>
        private void Start()
        {
            _sameDeviceButton?.onClick.AddListener(OnSameDeviceClicked);
            _onlineButton?.onClick.AddListener(OnOnlineClicked);
            _createRoomButton?.onClick.AddListener(OnCreateRoomClicked);
            _joinRoomButton?.onClick.AddListener(OnJoinRoomClicked);
            _backButton?.onClick.AddListener(ShowModePanel);

            if (_launcher != null)
            {
                _launcher.StatusChanged += OnStatusChanged;
                _launcher.Failed += OnLaunchFailed;
            }

            ShowModePanel();
        }

        /// <summary>Disiscrive dagli eventi del launcher al teardown.</summary>
        private void OnDestroy()
        {
            if (_launcher != null)
            {
                _launcher.StatusChanged -= OnStatusChanged;
                _launcher.Failed -= OnLaunchFailed;
            }
        }

        /// <summary>Mostra il pannello di scelta modalità e nasconde quello stanza.</summary>
        private void ShowModePanel()
        {
            _modePanel?.SetActive(true);
            _roomPanel?.SetActive(false);
            SetStatus(string.Empty);
        }

        /// <summary>
        /// Avvia una partita hotseat: prima la selezione comandanti, poi la scena di gioco.
        /// </summary>
        private void OnSameDeviceClicked()
        {
            SessionConfig.Mode = NetworkMode.Hotseat;
            SessionConfig.RoomCode = string.Empty;
            // Azzera eventuali selezioni precedenti: si rifanno nella schermata di selezione.
            SessionConfig.Player0Commanders = null;
            SessionConfig.Player1Commanders = null;
            string nextScene = string.IsNullOrEmpty(_selectionSceneName) ? _gameSceneName : _selectionSceneName;
            SceneManager.LoadScene(nextScene);
        }

        /// <summary>Passa al pannello della stanza online.</summary>
        private void OnOnlineClicked()
        {
            _modePanel?.SetActive(false);
            _roomPanel?.SetActive(true);
            if (_roomCodeDisplay != null)
            {
                _roomCodeDisplay.text = string.Empty;
            }

            SetStatus(string.Empty);
        }

        /// <summary>Crea una stanza con un codice generato e attende l'avversario.</summary>
        private void OnCreateRoomClicked()
        {
            string code = GenerateRoomCode();
            SessionConfig.Mode = NetworkMode.Online;
            SessionConfig.RoomCode = code;

            if (_roomCodeDisplay != null)
            {
                _roomCodeDisplay.text = $"Codice: {code}";
            }

            SetOnlineButtonsInteractable(false);
            _launcher?.HostRoom(code);
        }

        /// <summary>Si unisce alla stanza il cui codice è stato digitato.</summary>
        private void OnJoinRoomClicked()
        {
            string code = _roomCodeInput != null ? _roomCodeInput.text.Trim().ToUpperInvariant() : string.Empty;
            if (string.IsNullOrEmpty(code))
            {
                SetStatus("Inserisci un codice stanza.");
                return;
            }

            SessionConfig.Mode = NetworkMode.Online;
            SessionConfig.RoomCode = code;

            SetOnlineButtonsInteractable(false);
            _launcher?.JoinExistingRoom(code);
        }

        /// <summary>Inoltra il testo di stato del launcher all'etichetta del menu.</summary>
        private void OnStatusChanged(string message)
        {
            SetStatus(message);
        }

        /// <summary>Riabilita i pulsanti quando la procedura online fallisce.</summary>
        private void OnLaunchFailed()
        {
            SetOnlineButtonsInteractable(true);
        }

        /// <summary>Abilita o disabilita i pulsanti della stanza durante la connessione.</summary>
        private void SetOnlineButtonsInteractable(bool interactable)
        {
            if (_createRoomButton != null)
            {
                _createRoomButton.interactable = interactable;
            }

            if (_joinRoomButton != null)
            {
                _joinRoomButton.interactable = interactable;
            }
        }

        /// <summary>Aggiorna l'etichetta di stato, se presente.</summary>
        private void SetStatus(string message)
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = message;
            }
        }

        /// <summary>Genera un codice stanza casuale leggibile.</summary>
        /// <returns>Codice di <see cref="CodeLength"/> caratteri.</returns>
        private static string GenerateRoomCode()
        {
            StringBuilder builder = new(CodeLength);
            for (int i = 0; i < CodeLength; i++)
            {
                builder.Append(CodeAlphabet[Random.Range(0, CodeAlphabet.Length)]);
            }

            return builder.ToString();
        }
    }
}
