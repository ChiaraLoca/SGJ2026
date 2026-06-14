using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace FourE.UI
{
    /// <summary>
    /// Controllo UI per la scena di test effetti. Gestisce dropdown per carta/giocatore/bersaglio,
    /// toggle passive e pulsante per applicare gli effetti.
    /// </summary>
    public class EffectTestUIController : MonoBehaviour
    {
        [SerializeField] private Dropdown _cardDropdown;
        [SerializeField] private Dropdown _playerDropdown;
        [SerializeField] private Dropdown _targetDropdown;
        [SerializeField] private Toggle _passivesToggle;
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _resetButton;
        [SerializeField] private Text _logText;

        private EffectTestSceneManager _manager;
        private List<FourE.Cards.CardDataSO> _cardList;

        private void Start()
        {
            if (_cardDropdown != null)
            {
                _cardDropdown.onValueChanged.AddListener(OnCardChanged);
            }
            if (_playerDropdown != null)
            {
                _playerDropdown.onValueChanged.AddListener(OnPlayerChanged);
            }
            if (_targetDropdown != null)
            {
                _targetDropdown.onValueChanged.AddListener(OnTargetChanged);
            }
            if (_applyButton != null)
            {
                _applyButton.onClick.AddListener(OnApplyClicked);
            }
            if (_resetButton != null)
            {
                _resetButton.onClick.AddListener(OnResetClicked);
            }
            if (_passivesToggle != null)
            {
                _passivesToggle.onValueChanged.AddListener(OnPassivesToggled);
                _passivesToggle.isOn = true;
            }
        }

        /// <summary>
        /// Metodo pubblico per il setup editor: assegna tutti i reference UI.
        /// </summary>
        public void SetUIReferences(
            Dropdown cardDropdown,
            Dropdown playerDropdown,
            Dropdown targetDropdown,
            Toggle passivesToggle,
            Button applyButton,
            Button resetButton,
            Text logText)
        {
            _cardDropdown = cardDropdown;
            _playerDropdown = playerDropdown;
            _targetDropdown = targetDropdown;
            _passivesToggle = passivesToggle;
            _applyButton = applyButton;
            _resetButton = resetButton;
            _logText = logText;
        }

        /// <summary>
        /// Assegna il manager scene e popola i dropdown.
        /// </summary>
        public void SetManager(EffectTestSceneManager manager)
        {
            _manager = manager;
            PopulateCardDropdown();
            PopulatePlayerDropdown();
            PopulateTargetDropdown();
        }

        /// <summary>
        /// Popola il dropdown delle carte con tutte le carte disponibili dal catalogo.
        /// </summary>
        private void PopulateCardDropdown()
        {
            if (_cardDropdown == null || _manager == null) return;

            _cardList = new List<FourE.Cards.CardDataSO>();
            var content = _manager.GetGameContent();

            if (content == null) return;

            _cardDropdown.ClearOptions();
            var options = new List<Dropdown.OptionData>();

            options.Add(new Dropdown.OptionData("--- Seleziona una carta ---"));

            if (content.ShopCatalog != null)
            {
                foreach (var card in content.ShopCatalog)
                {
                    if (card != null)
                    {
                        _cardList.Add(card);
                        options.Add(new Dropdown.OptionData(card.CardName));
                    }
                }
            }

            if (content.VerificaCard != null)
            {
                _cardList.Add(content.VerificaCard);
                options.Add(new Dropdown.OptionData(content.VerificaCard.CardName));
            }

            _cardDropdown.AddOptions(options);
        }

        /// <summary>
        /// Popola il dropdown dei giocatori attivi.
        /// </summary>
        private void PopulatePlayerDropdown()
        {
            if (_playerDropdown == null) return;

            _playerDropdown.ClearOptions();
            var options = new List<Dropdown.OptionData>
            {
                new Dropdown.OptionData("Player 0"),
                new Dropdown.OptionData("Player 1")
            };

            _playerDropdown.AddOptions(options);
        }

        /// <summary>
        /// Popola il dropdown dei bersagli (tutti i comandanti di entrambi i giocatori).
        /// </summary>
        private void PopulateTargetDropdown()
        {
            if (_targetDropdown == null || _manager == null) return;

            _targetDropdown.ClearOptions();
            var commanders = _manager.GetAllCommanders();
            var options = new List<Dropdown.OptionData>();

            foreach (var (label, _, _) in commanders)
            {
                options.Add(new Dropdown.OptionData(label));
            }

            _targetDropdown.AddOptions(options);
        }

        private void OnCardChanged(int index)
        {
            // Log che la carta è cambiata
            if (_logText != null)
            {
                _logText.text = $"Carta selezionata: {(index > 0 ? _cardDropdown.options[index].text : "nessuna")}";
            }
        }

        private void OnPlayerChanged(int index)
        {
            if (_logText != null)
            {
                _logText.text = $"Giocatore attivo: Player {index}";
            }
        }

        private void OnTargetChanged(int index)
        {
            if (_logText != null)
            {
                _logText.text = $"Bersaglio: {_targetDropdown.options[index].text}";
            }
        }

        private void OnPassivesToggled(bool enabled)
        {
            if (_manager != null)
            {
                _manager.SetPassivesEnabled(enabled);
            }
            if (_logText != null)
            {
                _logText.text = $"Passive: {(enabled ? "ABILITATE" : "DISABILITATE")}";
            }
        }

        private void OnApplyClicked()
        {
            if (_manager == null) return;

            int cardIndex = _cardDropdown.value;
            int playerIndex = _playerDropdown.value;
            int targetIndex = _targetDropdown.value;

            if (cardIndex <= 0)
            {
                LogMessage("Seleziona una carta!");
                return;
            }

            var card = _cardList[cardIndex - 1];
            var commanders = _manager.GetAllCommanders();

            if (targetIndex >= 0 && targetIndex < commanders.Length)
            {
                var (_, tgtPlayerIdx, tgtCmdIdx) = commanders[targetIndex];

                // Se il giocatore attivo è diverso dal giocatore del bersaglio, usa ResolveEffectOnOpponent
                if (playerIndex != tgtPlayerIdx)
                {
                    _manager.ResolveEffectOnOpponent(card, playerIndex, tgtCmdIdx);
                    LogMessage($"Effetto '{card.CardName}' applicato a {commanders[targetIndex].label}");
                }
                else
                {
                    _manager.ResolveEffect(card, playerIndex, tgtCmdIdx);
                    LogMessage($"Effetto '{card.CardName}' applicato a {commanders[targetIndex].label}");
                }
            }
        }

        private void OnResetClicked()
        {
            if (_manager != null)
            {
                _manager.ResetGameState();
                LogMessage("Stato resettato!");
            }
        }

        private void LogMessage(string message)
        {
            if (_logText != null)
            {
                _logText.text = message;
            }
            Debug.Log($"[EffectTest] {message}");
        }

        private void OnDestroy()
        {
            if (_cardDropdown != null)
                _cardDropdown.onValueChanged.RemoveListener(OnCardChanged);
            if (_playerDropdown != null)
                _playerDropdown.onValueChanged.RemoveListener(OnPlayerChanged);
            if (_targetDropdown != null)
                _targetDropdown.onValueChanged.RemoveListener(OnTargetChanged);
            if (_applyButton != null)
                _applyButton.onClick.RemoveListener(OnApplyClicked);
            if (_resetButton != null)
                _resetButton.onClick.RemoveListener(OnResetClicked);
            if (_passivesToggle != null)
                _passivesToggle.onValueChanged.RemoveListener(OnPassivesToggled);
        }
    }
}
