using System;
using UnityEngine;
using UnityEngine.UI;
using FourE.Commanders;

namespace FourE.UI
{
    /// <summary>
    /// Vista compatta di un comandante nella griglia di selezione.
    /// Mostra solo il ritratto e il nome; il tap segnala il comandante da ispezionare
    /// nel pannello di dettaglio del controller. L'evidenziazione indica le scelte correnti.
    /// </summary>
    public sealed class CommanderOptionView : MonoBehaviour
    {
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Image _portrait;
        [SerializeField] private GameObject _selectedHighlight;
        [SerializeField] private Button _inspectButton;

        private CommanderKind _kind;
        private Action<CommanderKind> _onInspect;

        /// <summary>
        /// Configura la vista con la definizione del comandante e il callback di ispezione.
        /// </summary>
        /// <param name="data">Definizione del comandante da mostrare.</param>
        /// <param name="onInspect">Callback invocato col <see cref="CommanderKind"/> al tap, per aprire il pannello dettaglio.</param>
        public void Bind(CommanderDataSO data, Action<CommanderKind> onInspect)
        {
            _kind = data.Kind;
            _onInspect = onInspect;

            if (_nameLabel != null)
            {
                _nameLabel.text = data.CommanderName;
            }

            if (_portrait != null && data.Portrait != null)
            {
                _portrait.sprite = data.Portrait;
            }

            SetSelected(false);

            if (_inspectButton != null)
            {
                _inspectButton.onClick.RemoveAllListeners();
                _inspectButton.onClick.AddListener(() => _onInspect?.Invoke(_kind));
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
