using System.Collections;
using System.Collections.Generic;
using FourE.Cards;
using UnityEngine;

namespace FourE.UI
{
    /// <summary>
    /// Gestisce in coda le animazioni delle carte pescate dal mazzo verso la mano locale.
    /// </summary>
    internal sealed class CardDrawAnimationController
    {
        private readonly MonoBehaviour _owner;
        private readonly Canvas _canvas;
        private readonly CardView _cardPrefab;
        private readonly float _startScale;
        private readonly float _moveDuration;
        private readonly float _arcHeight;
        private readonly Queue<AnimationRequest> _requests = new();

        private Coroutine _routine;
        private CardView _activeCard;
        private CanvasGroup _activeTargetGroup;

        /// <summary>
        /// Crea il controller per le animazioni di pescata.
        /// </summary>
        /// <param name="owner">MonoBehaviour usato per eseguire le coroutine.</param>
        /// <param name="canvas">Canvas che ospita le carte animate.</param>
        /// <param name="cardPrefab">Prefab visuale della carta.</param>
        /// <param name="startScale">Scala iniziale della carta sopra il mazzo.</param>
        /// <param name="moveDuration">Durata del movimento verso la mano.</param>
        /// <param name="arcHeight">Altezza dell'arco percorso dalla carta.</param>
        internal CardDrawAnimationController(
            MonoBehaviour owner,
            Canvas canvas,
            CardView cardPrefab,
            float startScale,
            float moveDuration,
            float arcHeight)
        {
            _owner = owner;
            _canvas = canvas;
            _cardPrefab = cardPrefab;
            _startScale = startScale;
            _moveDuration = moveDuration;
            _arcHeight = arcHeight;
        }

        /// <summary>
        /// Accoda una carta pescata e nasconde temporaneamente la sua copia definitiva in mano.
        /// </summary>
        internal void Enqueue(CardDataSO card, Vector2 startPosition, Vector2 endPosition, CardView targetView)
        {
            if (card == null || _canvas == null || _cardPrefab == null)
            {
                return;
            }

            CanvasGroup targetGroup = null;
            if (targetView != null)
            {
                targetGroup = targetView.GetComponent<CanvasGroup>();
                if (targetGroup == null)
                {
                    targetGroup = targetView.gameObject.AddComponent<CanvasGroup>();
                }

                targetGroup.alpha = 0f;
            }

            _requests.Enqueue(new AnimationRequest(card, startPosition, endPosition, targetGroup));
            if (_routine == null)
            {
                _routine = _owner.StartCoroutine(PlayQueueRoutine());
            }
        }

        /// <summary>
        /// Arresta le animazioni, ripristinando l'eventuale carta nascosta.
        /// </summary>
        internal void Dispose()
        {
            foreach (AnimationRequest request in _requests)
            {
                if (request.TargetGroup != null)
                {
                    request.TargetGroup.alpha = 1f;
                }
            }

            _requests.Clear();
            if (_routine != null)
            {
                _owner.StopCoroutine(_routine);
                _routine = null;
            }

            RestoreTarget();
            if (_activeCard != null)
            {
                Object.Destroy(_activeCard.gameObject);
                _activeCard = null;
            }
        }

        /// <summary>
        /// Riproduce in sequenza tutte le carte pescate.
        /// </summary>
        private IEnumerator PlayQueueRoutine()
        {
            while (_requests.Count > 0)
            {
                yield return PlayCardRoutine(_requests.Dequeue());
            }

            _routine = null;
        }

        /// <summary>
        /// Muove una singola carta dal mazzo alla posizione finale in mano.
        /// </summary>
        private IEnumerator PlayCardRoutine(AnimationRequest request)
        {
            _activeTargetGroup = request.TargetGroup;
            _activeCard = Object.Instantiate(_cardPrefab, _canvas.transform);
            _activeCard.BindPreview(request.Card);
            _activeCard.transform.SetAsLastSibling();

            RectTransform cardTransform = (RectTransform)_activeCard.transform;
            Vector2 centerAnchor = Vector2.one * FourE.Config.GameConstants.UiCenterAnchor;
            cardTransform.anchorMin = centerAnchor;
            cardTransform.anchorMax = centerAnchor;
            cardTransform.pivot = centerAnchor;
            cardTransform.anchoredPosition = request.StartPosition;
            cardTransform.localScale = Vector3.one * _startScale;

            CanvasGroup animatedGroup = _activeCard.gameObject.AddComponent<CanvasGroup>();
            animatedGroup.interactable = false;
            animatedGroup.blocksRaycasts = false;

            float elapsed = 0f;
            while (elapsed < _moveDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / _moveDuration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);
                Vector2 position = Vector2.LerpUnclamped(request.StartPosition, request.EndPosition, eased);
                position.y += Mathf.Sin(progress * Mathf.PI) * _arcHeight;
                cardTransform.anchoredPosition = position;
                cardTransform.localScale = Vector3.one * Mathf.Lerp(_startScale, 1f, eased);
                yield return null;
            }

            RestoreTarget();
            Object.Destroy(_activeCard.gameObject);
            _activeCard = null;
        }

        /// <summary>
        /// Rende nuovamente visibile la carta definitiva nella mano.
        /// </summary>
        private void RestoreTarget()
        {
            if (_activeTargetGroup != null)
            {
                _activeTargetGroup.alpha = 1f;
            }

            _activeTargetGroup = null;
        }

        /// <summary>
        /// Dati immutabili di una carta pescata in attesa di animazione.
        /// </summary>
        private readonly struct AnimationRequest
        {
            /// <summary>Carta pescata da animare.</summary>
            internal CardDataSO Card { get; }

            /// <summary>Posizione iniziale locale nel Canvas.</summary>
            internal Vector2 StartPosition { get; }

            /// <summary>Posizione finale locale nel Canvas.</summary>
            internal Vector2 EndPosition { get; }

            /// <summary>Gruppo della carta definitiva nascosta durante l'animazione.</summary>
            internal CanvasGroup TargetGroup { get; }

            /// <summary>
            /// Crea una richiesta immutabile di animazione pescata.
            /// </summary>
            internal AnimationRequest(
                CardDataSO card,
                Vector2 startPosition,
                Vector2 endPosition,
                CanvasGroup targetGroup)
            {
                Card = card;
                StartPosition = startPosition;
                EndPosition = endPosition;
                TargetGroup = targetGroup;
            }
        }
    }
}
