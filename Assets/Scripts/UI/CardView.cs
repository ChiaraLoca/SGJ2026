using System;
using UnityEngine;
using UnityEngine.UI;
using FourE.Cards;

namespace FourE.UI
{
    /// <summary>
    /// Vista prefab di una singola carta: mostra nome, costo e descrizione e inoltra
    /// il click al chiamante. Non contiene logica di gioco: si limita a presentare i dati.
    /// </summary>
    public sealed class CardView : MonoBehaviour
    {
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Text _costLabel;
        [SerializeField] private Text _descriptionLabel;
        [SerializeField] private Button _button;

        private CardDataSO _card;
        private Action<CardDataSO> _onClick;

        /// <summary>
        /// Popola la vista con i dati di una carta e ne configura il click.
        /// </summary>
        /// <param name="card">Carta da rappresentare.</param>
        /// <param name="onClick">Callback invocata al click, con la carta come argomento.</param>
        /// <param name="interactable">Se il pulsante è cliccabile nello stato corrente.</param>
        public void Bind(CardDataSO card, Action<CardDataSO> onClick, bool interactable)
        {
            _card = card;
            _onClick = onClick;

            if (_nameLabel != null)
            {
                _nameLabel.text = card.CardName;
            }

            if (_costLabel != null)
            {
                _costLabel.text = card.ShopCost.ToString();
            }

            if (_descriptionLabel != null)
            {
                _descriptionLabel.text = card.Description;
            }

            if (_button != null)
            {
                _button.interactable = interactable;
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(HandleClick);
            }
        }

        /// <summary>
        /// Inoltra il click del pulsante alla callback registrata.
        /// </summary>
        private void HandleClick()
        {
            _onClick?.Invoke(_card);
        }
    }
}
