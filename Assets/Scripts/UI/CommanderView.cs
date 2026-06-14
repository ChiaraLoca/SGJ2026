using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using FourE.Commanders;
using FourE.Network;

namespace FourE.UI
{
    /// <summary>
    /// Vista di un comandante: mostra nome, Note, ritratto e debuff. Supporta la selezione
    /// come bersaglio, la pressione prolungata e il feedback di sblocco della secondaria.
    /// </summary>
    public sealed class CommanderView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Text _noteLabel;
        [SerializeField] private Image _portrait;
        [SerializeField] private GameObject _debuffIndicator;
        [SerializeField] private Button _selectButton;
        [SerializeField] private Outline _secondaryUnlockedOutline;
        [SerializeField] private float _longPressDuration = 0.35f;
        [SerializeField] private float _unlockPulseDuration = 0.8f;
        [SerializeField] private float _unlockPulseScale = 1.18f;
        [SerializeField] private Color _unlockPulseColor = new(1f, 0.86f, 0.2f, 1f);

        private int _actorNumber;
        private int _commanderIndex;
        private CommanderDataSO _data;
        private bool _secondaryUnlocked;
        private Action<CommanderDataSO, bool> _onPreviewStarted;
        private Action _onPreviewEnded;
        private Coroutine _longPressRoutine;
        private Coroutine _unlockPulseRoutine;
        private bool _isPreviewing;

        /// <summary>
        /// Aggiorna la vista con lo snapshot, la definizione e i callback di anteprima.
        /// </summary>
        /// <param name="snapshot">Stato di rete del comandante.</param>
        /// <param name="data">Definizione SO per nome, ritratto e abilita.</param>
        /// <param name="actorNumber">Numero attore del giocatore proprietario.</param>
        /// <param name="commanderIndex">Indice del comandante nel team.</param>
        /// <param name="onPreviewStarted">Callback invocato all'apertura del riquadro abilita.</param>
        /// <param name="onPreviewEnded">Callback invocato al rilascio della pressione.</param>
        public void Bind(
            CommanderDTO snapshot,
            CommanderDataSO data,
            int actorNumber,
            int commanderIndex,
            Action<CommanderDataSO, bool> onPreviewStarted,
            Action onPreviewEnded)
        {
            _actorNumber = actorNumber;
            _commanderIndex = commanderIndex;
            _data = data;
            _secondaryUnlocked = snapshot.SecondaryUnlocked;
            _onPreviewStarted = onPreviewStarted;
            _onPreviewEnded = onPreviewEnded;

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

            if (_secondaryUnlockedOutline != null)
            {
                _secondaryUnlockedOutline.enabled = snapshot.SecondaryUnlocked;
            }
        }

        /// <summary>
        /// Avvia il rilevamento della pressione prolungata sul comandante.
        /// </summary>
        /// <param name="eventData">Dati del puntatore mouse o touch.</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (_data == null || _onPreviewStarted == null)
            {
                return;
            }

            StopLongPressRoutine();
            _longPressRoutine = StartCoroutine(LongPressRoutine());
        }

        /// <summary>
        /// Chiude il riquadro abilita al rilascio del puntatore.
        /// </summary>
        /// <param name="eventData">Dati del puntatore mouse o touch.</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            StopLongPressRoutine();
            EndPreview();
        }

        /// <summary>
        /// Annulla la pressione quando il puntatore esce dall'icona.
        /// </summary>
        /// <param name="eventData">Dati del puntatore mouse o touch.</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            StopLongPressRoutine();
            EndPreview();
        }

        /// <summary>
        /// Riproduce il feedback visivo dello sblocco della passiva secondaria.
        /// </summary>
        public void PlaySecondaryUnlockEffect()
        {
            if (_unlockPulseRoutine != null)
            {
                StopCoroutine(_unlockPulseRoutine);
            }

            _unlockPulseRoutine = StartCoroutine(SecondaryUnlockEffectRoutine());
        }

        /// <summary>
        /// Abilita o disabilita l'overlay di selezione bersaglio sul comandante.
        /// </summary>
        /// <param name="active">True per mostrare l'overlay e registrare il callback.</param>
        /// <param name="onSelected">Callback invocato con attore e indice alla selezione.</param>
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
                int index = _commanderIndex;
                _selectButton.onClick.AddListener(() => onSelected(actor, index));
            }
        }

        /// <summary>
        /// Ripristina timer, popup e animazione quando la vista viene disabilitata.
        /// </summary>
        private void OnDisable()
        {
            StopLongPressRoutine();
            EndPreview();

            if (_unlockPulseRoutine != null)
            {
                StopCoroutine(_unlockPulseRoutine);
                _unlockPulseRoutine = null;
            }

            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Attende la soglia e apre il riquadro delle abilita.
        /// </summary>
        /// <returns>Enumeratore della coroutine.</returns>
        private IEnumerator LongPressRoutine()
        {
            yield return new WaitForSecondsRealtime(_longPressDuration);
            _longPressRoutine = null;
            _isPreviewing = true;
            _onPreviewStarted?.Invoke(_data, _secondaryUnlocked);
        }

        /// <summary>
        /// Interrompe il timer della pressione prolungata.
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
        /// Chiude il riquadro aperto da questa vista.
        /// </summary>
        private void EndPreview()
        {
            if (!_isPreviewing)
            {
                return;
            }

            _isPreviewing = false;
            _onPreviewEnded?.Invoke();
        }

        /// <summary>
        /// Esegue un pulse di scala e colore sul ritratto del comandante.
        /// </summary>
        /// <returns>Enumeratore della coroutine.</returns>
        private IEnumerator SecondaryUnlockEffectRoutine()
        {
            Vector3 initialScale = transform.localScale;
            Color initialColor = _portrait != null ? _portrait.color : Color.white;
            float elapsed = 0f;

            while (elapsed < _unlockPulseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / _unlockPulseDuration);
                float pulse = Mathf.Sin(progress * Mathf.PI);
                transform.localScale = initialScale * Mathf.Lerp(1f, _unlockPulseScale, pulse);

                if (_portrait != null)
                {
                    _portrait.color = Color.Lerp(initialColor, _unlockPulseColor, pulse);
                }

                yield return null;
            }

            transform.localScale = initialScale;
            if (_portrait != null)
            {
                _portrait.color = initialColor;
            }

            _unlockPulseRoutine = null;
        }
    }
}
