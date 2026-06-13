using UnityEngine;

namespace FourE.UI
{
    /// <summary>
    /// Adatta un pannello figlio del Canvas ai bordi fisici del dispositivo (safe area).
    /// Gestisce notch, Dynamic Island, barra home di iOS/Android e rotazione dello schermo.
    /// Da collegare al Canvas principale di ogni scena; il pannello figlio contiene tutta la UI.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class SafeAreaController : MonoBehaviour
    {
        [Tooltip("Pannello figlio del Canvas da adattare alla safe area del dispositivo.")]
        [SerializeField] private RectTransform _panel;

        private Rect _lastSafeArea;
        private ScreenOrientation _lastOrientation;
        private bool _isApplied;

        private void Start()
        {
            Canvas.ForceUpdateCanvases();
            Apply();
        }

        private void Update()
        {
            // Ricalcola solo se la safe area o l'orientamento sono cambiati.
            if (!_isApplied
                || Screen.safeArea != _lastSafeArea
                || Screen.orientation != _lastOrientation)
            {
                Apply();
            }
        }

        /// <summary>
        /// Converte la safe area in pixel in anchor normalizzati e li applica al pannello.
        /// </summary>
        private void Apply()
        {
            if (_panel == null)
            {
                return;
            }

            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            if (screenSize.x <= 0f || screenSize.y <= 0f)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            if (safeArea.width <= 0f || safeArea.height <= 0f)
            {
                safeArea = new Rect(Vector2.zero, screenSize);
            }

            _lastSafeArea = safeArea;
            _lastOrientation = Screen.orientation;

            Vector2 anchorMin = safeArea.position / screenSize;
            Vector2 anchorMax = (safeArea.position + safeArea.size) / screenSize;
            anchorMin = Vector2.Max(Vector2.zero, Vector2.Min(Vector2.one, anchorMin));
            anchorMax = Vector2.Max(anchorMin, Vector2.Min(Vector2.one, anchorMax));

            _panel.anchorMin = anchorMin;
            _panel.anchorMax = anchorMax;
            _panel.offsetMin = Vector2.zero;
            _panel.offsetMax = Vector2.zero;
            _panel.localScale = Vector3.one;
            _isApplied = true;
        }
    }
}
