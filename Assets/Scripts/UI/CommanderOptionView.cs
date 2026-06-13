using System;
using UnityEngine;
using UnityEngine.UI;
using FourE.Commanders;

namespace FourE.UI
{
    /// <summary>
    /// Vista prefab di un comandante selezionabile nella schermata di selezione.
    /// Mostra nome, ritratto e descrizioni delle passive; il tap invoca il callback di scelta.
    /// </summary>
    public sealed class CommanderOptionView : MonoBehaviour
    {
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Image _portrait;
        [SerializeField] private Text _baseAbilityLabel;
        [SerializeField] private Text _unlockLabel;
        [SerializeField] private Text _secondaryAbilityLabel;
        [SerializeField] private Button _selectButton;
        [SerializeField] private GameObject _selectedHighlight;

        private CommanderKind _kind;
        private Action<CommanderKind> _onSelected;

        /// <summary>
        /// Configura la vista con la definizione del comandante e il callback di selezione.
        /// </summary>
        /// <param name="data">Definizione del comandante da mostrare.</param>
        /// <param name="onSelected">Callback invocato col <see cref="CommanderKind"/> al tap.</param>
        public void Bind(CommanderDataSO data, Action<CommanderKind> onSelected)
        {
            _kind = data.Kind;
            _onSelected = onSelected;

            if (_nameLabel != null)
            {
                _nameLabel.text = data.CommanderName;
            }

            if (_portrait != null && data.Portrait != null)
            {
                _portrait.sprite = data.Portrait;
            }

            if (_baseAbilityLabel != null)
            {
                _baseAbilityLabel.text = data.BaseAbilityDescription;
            }

            if (_unlockLabel != null)
            {
                _unlockLabel.text = data.UnlockConditionDescription;
            }

            if (_secondaryAbilityLabel != null)
            {
                _secondaryAbilityLabel.text = data.SecondaryAbilityDescription;
            }

            SetSelected(false);

            if (_selectButton != null)
            {
                _selectButton.onClick.RemoveAllListeners();
                _selectButton.onClick.AddListener(() => _onSelected?.Invoke(_kind));
            }
        }

        /// <summary>
        /// Mostra o nasconde l'evidenziazione di "scelto".
        /// </summary>
        /// <param name="selected">True se il comandante è tra i selezionati correnti.</param>
        public void SetSelected(bool selected)
        {
            if (_selectedHighlight != null)
            {
                _selectedHighlight.SetActive(selected);
            }
        }
    }
}
