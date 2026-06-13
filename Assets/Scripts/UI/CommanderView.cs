using UnityEngine;
using UnityEngine.UI;
using FourE.Commanders;
using FourE.Network;

namespace FourE.UI
{
    /// <summary>
    /// Vista prefab/scena di un comandante: mostra nome, Note corrente, ritratto e
    /// l'indicatore di debuff. Combina lo snapshot di rete con i dati statici dell'SO.
    /// </summary>
    public sealed class CommanderView : MonoBehaviour
    {
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Text _noteLabel;
        [SerializeField] private Image _portrait;
        [SerializeField] private GameObject _debuffIndicator;

        /// <summary>
        /// Aggiorna la vista con lo snapshot del comandante e la sua definizione statica.
        /// </summary>
        /// <param name="snapshot">Stato di rete del comandante.</param>
        /// <param name="data">Definizione SO per nome e ritratto; può essere null.</param>
        public void Bind(CommanderDTO snapshot, CommanderDataSO data)
        {
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
    }
}
