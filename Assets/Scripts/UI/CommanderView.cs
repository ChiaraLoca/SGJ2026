using System;
using UnityEngine;
using UnityEngine.UI;
using FourE.Commanders;
using FourE.Network;

namespace FourE.UI
{
    /// <summary>
    /// Vista prefab/scena di un comandante: mostra nome, Note corrente, ritratto e
    /// l'indicatore di debuff. Supporta la selezione come bersaglio tramite un overlay Button.
    /// </summary>
    public sealed class CommanderView : MonoBehaviour
    {
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Text _noteLabel;
        [SerializeField] private Image _portrait;
        [SerializeField] private GameObject _debuffIndicator;
        [SerializeField] private Button _selectButton;

        private int _actorNumber;
        private int _commanderIndex;

        /// <summary>
        /// Aggiorna la vista con lo snapshot del comandante e la sua definizione statica.
        /// </summary>
        /// <param name="snapshot">Stato di rete del comandante.</param>
        /// <param name="data">Definizione SO per nome e ritratto; può essere null.</param>
        /// <param name="actorNumber">Numero attore del giocatore proprietario, per l'intent di selezione.</param>
        /// <param name="commanderIndex">Indice del comandante nel team, per l'intent di selezione.</param>
        public void Bind(CommanderDTO snapshot, CommanderDataSO data, int actorNumber, int commanderIndex)
        {
            _actorNumber = actorNumber;
            _commanderIndex = commanderIndex;

            if (_nameLabel != null && data != null)
            {
                _nameLabel.text = data.CommanderName;
            }

            if (_noteLabel != null)
            {
                _noteLabel.text = snapshot.CurrentNote.ToString();
            }

            if (_portrait != null && data != null && data.Portrait != null)
            {
                _portrait.sprite = data.Portrait;
            }

            if (_debuffIndicator != null)
            {
                _debuffIndicator.SetActive(snapshot.HasDebuff);
            }
        }

        /// <summary>
        /// Abilita o disabilita l'overlay di selezione bersaglio sul comandante.
        /// </summary>
        /// <param name="active">True per mostrare l'overlay e registrare il callback.</param>
        /// <param name="onSelected">Callback invocato con (actorNumber, commanderIndex) alla selezione.</param>
        public void SetSelectable(bool active, Action<int, int> onSelected)
        {
            if (_selectButton == null)
            {
                return;
            }

            _selectButton.gameObject.SetActive(active);
            _selectButton.onClick.RemoveAllListeners();
            if (active && onSelected != null)
            {
                int actor = _actorNumber;
                int idx = _commanderIndex;
                _selectButton.onClick.AddListener(() => onSelected(actor, idx));
            }
        }
    }
}
