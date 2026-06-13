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

        /// <summary>
        /// Metodo pubblico per assegnare il pannello da adattare (utile per setup editor).
        /// </summary>
        public void SetPanel(RectTransform panel)
        {
            _panel = panel;
        }

        private void Awake()
        {
            Apply();
        }

        private void Update()
        {
            // Ricalcola solo se la safe area o l'orientamento sono cambiati.
            if (Screen.safeArea != _lastSafeArea || Screen.orientation != _lastOrientation)
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

            _lastSafeArea = Screen.safeArea;
            _lastOrientation = Screen.orientation;

            Vector2 screenSize = new Vector2(Screen.width, Screen.height);

            Vector2 anchorMin = _lastSafeArea.position / screenSize;
            Vector2 anchorMax = (_lastSafeArea.position + _lastSafeArea.size) / screenSize;

            _panel.anchorMin = anchorMin;
            _panel.anchorMax = anchorMax;
            _panel.offsetMin = Vector2.zero;
            _panel.offsetMax = Vector2.zero;
        }
    }
}
