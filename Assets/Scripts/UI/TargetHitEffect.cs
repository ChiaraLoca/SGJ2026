using System.Collections;
using UnityEngine;

namespace FourE.UI
{
    /// <summary>
    /// Feedback visuale prefab-first mostrato sopra un comandante colpito da una carta avversaria.
    /// </summary>
    public sealed class TargetHitEffect : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _pulseScale = 1.2f;
        [SerializeField] private float _pulseAngularMultiplier = 4f;
        [SerializeField] private float _fadePortion = 0.3f;

        private Coroutine _routine;

        /// <summary>
        /// Riproduce il pulse per la durata indicata e distrugge l'istanza al termine.
        /// </summary>
        /// <param name="duration">Durata complessiva dell'effetto in secondi realtime.</param>
        public void Play(float duration)
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _routine = StartCoroutine(PlayRoutine(Mathf.Max(duration, Mathf.Epsilon)));
        }

        /// <summary>
        /// Anima scala e dissolvenza della X.
        /// </summary>
        private IEnumerator PlayRoutine(float duration)
        {
            Vector3 initialScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(progress * Mathf.PI * _pulseAngularMultiplier) * 0.5f + 0.5f;
                transform.localScale = initialScale * Mathf.Lerp(1f, _pulseScale, pulse);

                if (_canvasGroup != null)
                {
                    float fadeStart = 1f - Mathf.Clamp01(_fadePortion);
                    _canvasGroup.alpha = progress <= fadeStart
                        ? 1f
                        : 1f - Mathf.InverseLerp(fadeStart, 1f, progress);
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
