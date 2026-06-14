using System.Collections;
using System.Collections.Generic;
using FourE.Cards;
using FourE.Config;
using UnityEngine;

namespace FourE.UI
{
    /// <summary>
    /// Gestisce in coda le animazioni delle carte giocate usando il prefab della carta.
    /// </summary>
    internal sealed class CardPlayAnimationController
    {
        private readonly MonoBehaviour _owner;
        private readonly Canvas _canvas;
        private readonly CardView _cardPrefab;
        private readonly TargetHitEffect _targetHitEffectPrefab;
        private readonly float _targetScale;
        private readonly float _moveDuration;
        private readonly float _holdDuration;
        private readonly float _fadeDuration;
        private readonly Queue<AnimationRequest> _requests = new();

        private Coroutine _routine;
        private CardView _activeCard;

        /// <summary>
        /// Crea il controller per il Canvas indicato.
        /// </summary>
        /// <param name="owner">MonoBehaviour usato per eseguire le coroutine.</param>
        /// <param name="canvas">Canvas che ospita le carte animate.</param>
        /// <param name="cardPrefab">Prefab visuale della carta.</param>
        /// <param name="targetHitEffectPrefab">Prefab della X mostrata sul comandante colpito.</param>
        /// <param name="targetScale">Scala della carta al centro del campo.</param>
        /// <param name="moveDuration">Durata del movimento verso il centro.</param>
        /// <param name="holdDuration">Durata della permanenza al centro.</param>
        /// <param name="fadeDuration">Durata della dissolvenza finale.</param>
        internal CardPlayAnimationController(
            MonoBehaviour owner,
            Canvas canvas,
            CardView cardPrefab,
            TargetHitEffect targetHitEffectPrefab,
            float targetScale,
            float moveDuration,
            float holdDuration,
            float fadeDuration)
        {
            _owner = owner;
            _canvas = canvas;
            _cardPrefab = cardPrefab;
            _targetHitEffectPrefab = targetHitEffectPrefab;
            _targetScale = targetScale;
            _moveDuration = moveDuration;
            _holdDuration = holdDuration;
            _fadeDuration = fadeDuration;
        }

        /// <summary>
        /// Accoda una carta da animare partendo da una posizione locale del Canvas.
        /// </summary>
        /// <param name="card">Carta da mostrare.</param>
        /// <param name="startPosition">Posizione iniziale locale rispetto al Canvas.</param>
        /// <param name="targets">Comandanti locali colpiti, se presenti.</param>
        internal void Enqueue(CardDataSO card, Vector2 startPosition, RectTransform[] targets)
        {
            if (card == null || _canvas == null || _cardPrefab == null)
            {
                return;
            }

            _requests.Enqueue(new AnimationRequest(card, startPosition, targets));
            if (_routine == null)
            {
                _routine = _owner.StartCoroutine(PlayQueueRoutine());
            }
        }

        /// <summary>
        /// Arresta le animazioni e distrugge la carta eventualmente visibile.
        /// </summary>
        internal void Dispose()
        {
            _requests.Clear();
            if (_routine != null)
            {
                _owner.StopCoroutine(_routine);
                _routine = null;
            }

            if (_activeCard != null)
            {
                Object.Destroy(_activeCard.gameObject);
                _activeCard = null;
            }
        }

        /// <summary>
        /// Riproduce in ordine tutte le richieste accodate.
        /// </summary>
        /// <returns>Enumeratore della coroutine.</returns>
        private IEnumerator PlayQueueRoutine()
        {
            while (_requests.Count > 0)
            {
                AnimationRequest request = _requests.Dequeue();
                yield return PlayCardRoutine(request);
            }

            _routine = null;
        }

        /// <summary>
        /// Muove una carta al centro, la mantiene visibile e infine la dissolve.
        /// </summary>
        /// <param name="request">Richiesta da riprodurre.</param>
        /// <returns>Enumeratore della coroutine.</returns>
        private IEnumerator PlayCardRoutine(AnimationRequest request)
        {
            _activeCard = Object.Instantiate(_cardPrefab, _canvas.transform);
            _activeCard.BindPreview(request.Card);
            _activeCard.transform.SetAsLastSibling();

            RectTransform cardTransform = (RectTransform)_activeCard.transform;
            Vector2 centerAnchor = Vector2.one * GameConstants.UiCenterAnchor;
            cardTransform.anchorMin = centerAnchor;
            cardTransform.anchorMax = centerAnchor;
            cardTransform.pivot = centerAnchor;
            cardTransform.anchoredPosition = request.StartPosition;
            cardTransform.localScale = Vector3.one;

            CanvasGroup canvasGroup = _activeCard.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 1f;

            yield return AnimateToCenterRoutine(cardTransform);
            ShowTargetHitEffects(request.Targets);
            yield return new WaitForSecondsRealtime(_holdDuration);
            yield return FadeRoutine(canvasGroup);

            Object.Destroy(_activeCard.gameObject);
            _activeCard = null;
        }

        /// <summary>
        /// Mostra la X sopra il comandante colpito per la permanenza della carta al centro.
        /// </summary>
        private void ShowTargetHitEffects(RectTransform[] targets)
        {
            if (targets == null || _targetHitEffectPrefab == null)
            {
                return;
            }

            foreach (RectTransform target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                TargetHitEffect effect = Object.Instantiate(_targetHitEffectPrefab, target);
                if (effect.transform is RectTransform effectTransform)
                {
                    effectTransform.anchorMin = Vector2.zero;
                    effectTransform.anchorMax = Vector2.one;
                    effectTransform.offsetMin = Vector2.zero;
                    effectTransform.offsetMax = Vector2.zero;
                }

                effect.transform.SetAsLastSibling();
                effect.Play(_holdDuration + _fadeDuration);
            }
        }

        /// <summary>
        /// Interpola posizione e scala della carta verso il centro.
        /// </summary>
        /// <param name="cardTransform">RectTransform della carta animata.</param>
        /// <returns>Enumeratore della coroutine.</returns>
        private IEnumerator AnimateToCenterRoutine(RectTransform cardTransform)
        {
            Vector2 startPosition = cardTransform.anchoredPosition;
            Vector3 startScale = cardTransform.localScale;
            float elapsed = 0f;

            while (elapsed < _moveDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / _moveDuration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);
                cardTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, new Vector2(0f, 80f), eased);
                cardTransform.localScale = Vector3.LerpUnclamped(
                    startScale,
                    Vector3.one * _targetScale,
                    eased);
                yield return null;
            }

            cardTransform.anchoredPosition = new Vector2(0f, 80f);
            cardTransform.localScale = Vector3.one * _targetScale;
        }

        /// <summary>
        /// Dissolve la carta al termine della permanenza al centro.
        /// </summary>
        /// <param name="canvasGroup">Gruppo grafico da dissolvere.</param>
        /// <returns>Enumeratore della coroutine.</returns>
        private IEnumerator FadeRoutine(CanvasGroup canvasGroup)
        {
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / _fadeDuration);
                yield return null;
            }
        }

        /// <summary>
        /// Dati immutabili di una carta in attesa di animazione.
        /// </summary>
        private readonly struct AnimationRequest
        {
            /// <summary>Carta da animare.</summary>
            internal CardDataSO Card { get; }

            /// <summary>Posizione iniziale locale nel Canvas.</summary>
            internal Vector2 StartPosition { get; }

            /// <summary>Comandanti locali colpiti dalla carta, se presenti.</summary>
            internal RectTransform[] Targets { get; }

            /// <summary>
            /// Crea una richiesta di animazione.
            /// </summary>
            /// <param name="card">Carta da animare.</param>
            /// <param name="startPosition">Posizione iniziale locale nel Canvas.</param>
            /// <param name="targets">Comandanti locali colpiti, se presenti.</param>
            internal AnimationRequest(CardDataSO card, Vector2 startPosition, RectTransform[] targets)
            {
                Card = card;
                StartPosition = startPosition;
                Targets = targets;
            }
        }
    }
}
