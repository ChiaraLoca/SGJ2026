using System;
using System.Collections;
using FourE.Cards;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FourE.UI
{
    /// <summary>
    /// Vista prefab di una singola carta: presenta i dati, inoltra il tap e rileva
    /// la pressione prolungata per richiedere un'anteprima ingrandita.
    /// </summary>
    public sealed class CardView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Text _costLabel;
        [SerializeField] private Text _descriptionLabel;
        [SerializeField] private Image _artworkImage;
        [SerializeField] private Button _button;
        [SerializeField] private float _longPressDuration = 0.35f;

        private CardDataSO _card;
        private Action<CardDataSO> _onClick;
        private Action<CardDataSO> _onPreviewStarted;
        private Action _onPreviewEnded;
        private Coroutine _longPressRoutine;
        private Coroutine _clearSuppressionRoutine;
        private bool _isPreviewing;
        private bool _suppressNextClick;

        /// <summary>
        /// Popola la vista e configura tap e pressione prolungata.
        /// </summary>
        /// <param name="card">Carta da rappresentare.</param>
        /// <param name="onClick">Callback invocata al tap breve.</param>
        /// <param name="interactable">Se il pulsante e cliccabile nello stato corrente.</param>
        /// <param name="onPreviewStarted">Callback invocata quando la pressione diventa prolungata.</param>
        /// <param name="onPreviewEnded">Callback invocata al rilascio della pressione prolungata.</param>
        public void Bind(
            CardDataSO card,
            Action<CardDataSO> onClick,
            bool interactable,
            Action<CardDataSO> onPreviewStarted,
            Action onPreviewEnded)
        {
            _card = card;
            _onClick = onClick;
            _onPreviewStarted = onPreviewStarted;
            _onPreviewEnded = onPreviewEnded;
            _suppressNextClick = false;
            Populate(card);

            if (_button != null)
            {
                _button.enabled = true;
                _button.interactable = interactable;
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(HandleClick);
            }
        }

        /// <summary>
        /// Configura la vista come anteprima non interattiva della carta.
        /// </summary>
        /// <param name="card">Carta da mostrare ingrandita.</param>
        public void BindPreview(CardDataSO card)
        {
            _card = card;
            _onClick = null;
            _onPreviewStarted = null;
            _onPreviewEnded = null;
            Populate(card);

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.interactable = true;
                _button.transition = Selectable.Transition.None;
                _button.enabled = false;
            }

            ForceOpaqueGraphics();
        }

        /// <summary>
        /// Avvia il rilevamento della pressione prolungata.
        /// </summary>
        /// <param name="eventData">Dati del puntatore mouse o touch.</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (_card == null || _onPreviewStarted == null)
            {
                return;
            }

            StopLongPressRoutine();
            _longPressRoutine = StartCoroutine(LongPressRoutine());
        }

        /// <summary>
        /// Termina l'eventuale anteprima al rilascio del puntatore.
        /// </summary>
        /// <param name="eventData">Dati del puntatore mouse o touch.</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (_isPreviewing)
            {
                eventData.eligibleForClick = false;
            }

            StopLongPressRoutine();
            EndPreview();
        }

        /// <summary>
        /// Annulla la pressione quando il puntatore esce dalla carta.
        /// </summary>
        /// <param name="eventData">Dati del puntatore mouse o touch.</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            StopLongPressRoutine();
            EndPreview();
        }

        /// <summary>
        /// Ripristina lo stato del componente quando viene disabilitato o distrutto.
        /// </summary>
        private void OnDisable()
        {
            StopLongPressRoutine();
            StopClearSuppressionRoutine();
            EndPreview();
            _suppressNextClick = false;
        }

        /// <summary>
        /// Aggiorna gli elementi visivi con i dati della carta.
        /// </summary>
        /// <param name="card">Carta da rappresentare.</param>
        private void Populate(CardDataSO card)
        {
            bool hasArtwork = card.Artwork != null;

            if (_artworkImage != null)
            {
                _artworkImage.sprite = card.Artwork;
                _artworkImage.preserveAspect = hasArtwork;
            }

            if (_nameLabel != null)
            {
                _nameLabel.text = card.CardName;
                _nameLabel.gameObject.SetActive(!hasArtwork);
            }

            if (_costLabel != null)
            {
                _costLabel.text = card.ShopCost.ToString();
                _costLabel.gameObject.SetActive(!hasArtwork);
            }

            if (_descriptionLabel != null)
            {
                _descriptionLabel.text = card.Description;
                _descriptionLabel.gameObject.SetActive(!hasArtwork);
            }
        }

        /// <summary>
        /// Forza tutte le grafiche dell'anteprima alla piena opacita.
        /// </summary>
        private void ForceOpaqueGraphics()
        {
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
            {
                Color color = graphic.color;
                color.a = 1f;
                graphic.color = color;
            }
        }

        /// <summary>
        /// Inoltra il tap breve alla callback registrata.
        /// </summary>
        private void HandleClick()
        {
            if (_suppressNextClick)
            {
                _suppressNextClick = false;
                return;
            }

            _onClick?.Invoke(_card);
        }

        /// <summary>
        /// Attende la soglia configurata e apre l'anteprima se la pressione continua.
        /// </summary>
        /// <returns>Enumeratore della coroutine di attesa.</returns>
        private IEnumerator LongPressRoutine()
        {
            yield return new WaitForSecondsRealtime(_longPressDuration);
            _longPressRoutine = null;
            _isPreviewing = true;
            _suppressNextClick = true;
            _onPreviewStarted?.Invoke(_card);
        }

        /// <summary>
        /// Interrompe il timer della pressione prolungata, se attivo.
        /// </summary>
        private void StopLongPressRoutine()
        {
            if (_longPressRoutine == null)
            {
                return;
            }

            StopCoroutine(_longPressRoutine);
            _longPressRoutine = null;
        }

        /// <summary>
        /// Chiude l'anteprima aperta da questa carta.
        /// </summary>
        private void EndPreview()
        {
            if (!_isPreviewing)
            {
                return;
            }

            _isPreviewing = false;
            _onPreviewEnded?.Invoke();

            StopClearSuppressionRoutine();
            if (isActiveAndEnabled)
            {
                _clearSuppressionRoutine = StartCoroutine(ClearSuppressionRoutine());
            }
        }

        /// <summary>
        /// Rimuove la soppressione dopo che il sistema eventi ha concluso il rilascio corrente.
        /// </summary>
        /// <returns>Enumeratore della coroutine di attesa.</returns>
        private IEnumerator ClearSuppressionRoutine()
        {
            yield return null;
            _suppressNextClick = false;
            _clearSuppressionRoutine = null;
        }

        /// <summary>
        /// Interrompe il ripristino differito della soppressione click, se attivo.
        /// </summary>
        private void StopClearSuppressionRoutine()
        {
            if (_clearSuppressionRoutine == null)
            {
                return;
            }

            StopCoroutine(_clearSuppressionRoutine);
            _clearSuppressionRoutine = null;
        }
    }
}
