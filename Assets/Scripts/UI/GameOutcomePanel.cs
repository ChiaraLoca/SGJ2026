using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FourE.UI
{
    /// <summary>
    /// Pannello modale di fine partita: mostra l'esito (vittoria, sconfitta, pareggio)
    /// e offre il pulsante per tornare al menu principale.
    /// Si sovrappone all'HUD istanziandosi nel Canvas radice di <see cref="GameView"/>.
    /// </summary>
    public sealed class GameOutcomePanel : MonoBehaviour
    {
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _subtitleLabel;
        [SerializeField] private Button _mainMenuButton;

        [Header("Immagini esito")]
        [SerializeField] private Image _outcomeImage;
        [SerializeField] private Sprite _victorySprite;
        [SerializeField] private Sprite _defeatSprite;
        [SerializeField] private Sprite _drawSprite;

        private const string MainMenuScene = "MainMenu";

        /// <summary>
        /// Popola il pannello con testi e immagine dell'esito e registra l'azione del pulsante.
        /// </summary>
        /// <param name="isWin">True se il giocatore locale ha vinto.</param>
        /// <param name="isDraw">True se la partita è terminata in pareggio.</param>
        public void Bind(bool isWin, bool isDraw)
        {
            if (_titleLabel != null)
            {
                _titleLabel.text = isDraw ? "PAREGGIO" : isWin ? "VITTORIA!" : "SCONFITTA";
            }

            if (_subtitleLabel != null)
            {
                _subtitleLabel.text = isDraw ? "Alla prossima!" : isWin ? "Ottimo lavoro!" : "Peccato...";
            }

            if (_outcomeImage != null)
            {
                Sprite sprite = isDraw ? _drawSprite : isWin ? _victorySprite : _defeatSprite;
                _outcomeImage.sprite = sprite;
                _outcomeImage.gameObject.SetActive(sprite != null);
            }

            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveAllListeners();
                _mainMenuButton.onClick.AddListener(GoToMainMenu);
            }
        }

        /// <summary>
        /// Disconnette dalla stanza Photon (se connessi) e torna al menu principale.
        /// </summary>
        private static void GoToMainMenu()
        {
#if PHOTON_UNITY_NETWORKING
            if (Photon.Pun.PhotonNetwork.IsConnected)
            {
                Photon.Pun.PhotonNetwork.LeaveRoom();
                Photon.Pun.PhotonNetwork.Disconnect();
            }
#endif
            SceneManager.LoadScene(MainMenuScene);
        }
    }
}
