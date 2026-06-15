using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FourE.UI
{
    /// <summary>
    /// Popup paginato del regolamento, navigabile tramite pulsanti o scorrimento orizzontale.
    /// </summary>
    public sealed class RulebookPopup : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField] private Text _sectionLabel;
        [SerializeField] private Text _bodyLabel;
        [SerializeField] private Text _pageLabel;
        [SerializeField] private Button _previousButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private float _swipeThreshold = 80f;

        private readonly RulePage[] _pages =
        {
            new(
                "OBIETTIVO E PREPARAZIONE",
                "Ogni giocatore sceglie 2 secchioni. Le loro carte formano il mazzo personale insieme alla carta Verifica.\n\n"
                + "Lo scopo e arrivare all'Esame Finale con piu Credits dell'avversario. Le Note ottenute durante i round "
                + "servono a guadagnare Credits e ad attivare gli effetti dei secchioni."),
            new(
                "IL TURNO",
                "All'inizio del turno peschi 2 carte. Durante la fase di gioco puoi usare le azioni rimaste per giocare carte "
                + "dalla mano: alcune richiedono un bersaglio, altre si risolvono subito.\n\n"
                + "Il costo in azioni e mostrato sulla carta. Quando hai finito puoi passare il turno all'avversario."),
            new(
                "NOTE E VERIFICA",
                "Le carte possono aggiungere, rimuovere o spostare Note tra i secchioni. Alcuni effetti durano piu turni o "
                + "si attivano solo in condizioni particolari.\n\n"
                + "La Verifica non puo essere giocata nel primo turno del round. Quando viene giocata chiude il round e "
                + "le Note accumulate vengono convertite in Credits."),
            new(
                "SHOP E NUOVO ROUND",
                "Dopo la Verifica si apre lo shop, dove puoi spendere Credits per comprare nuove carte. Il mercato contiene "
                + "8 carte di fasce diverse; il prezzo e indicato sulla carta.\n\n"
                + "Le carte acquistate entrano nel tuo ciclo di gioco. Alla fine dello shop si prepara il round successivo. "
                + "Dopo il terzo round lo shop viene saltato."),
            new(
                "SECCHIONI E VITTORIA",
                "Ogni secchione ha una passiva base e una seconda abilita che si sblocca soddisfacendo la sua condizione. "
                + "Tieni premuto sul suo ritratto per leggere i poteri; tieni premuto su una carta per ingrandirla.\n\n"
                + "Dopo 3 round si svolge l'Esame Finale: vince chi possiede piu Credits. In caso di parita si confrontano "
                + "le Note finali; se anche queste sono uguali, la partita termina in pareggio.")
        };

        private int _currentPageIndex;
        private float _dragStartX;

        /// <summary>
        /// Collega i controlli del popup.
        /// </summary>
        private void Awake()
        {
            _previousButton?.onClick.AddListener(ShowPreviousPage);
            _nextButton?.onClick.AddListener(ShowNextPage);
            _closeButton?.onClick.AddListener(Hide);
        }

        /// <summary>
        /// Rimuove i listener registrati dal popup.
        /// </summary>
        private void OnDestroy()
        {
            _previousButton?.onClick.RemoveListener(ShowPreviousPage);
            _nextButton?.onClick.RemoveListener(ShowNextPage);
            _closeButton?.onClick.RemoveListener(Hide);
        }

        /// <summary>
        /// Apre il regolamento dalla prima pagina.
        /// </summary>
        public void Show()
        {
            _currentPageIndex = 0;
            gameObject.SetActive(true);
            RenderCurrentPage();
        }

        /// <summary>
        /// Memorizza il punto iniziale dello scorrimento orizzontale.
        /// </summary>
        /// <param name="eventData">Dati del puntatore che ha iniziato il trascinamento.</param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragStartX = eventData.position.x;
        }

        /// <summary>
        /// Cambia pagina quando lo scorrimento supera la soglia configurata.
        /// </summary>
        /// <param name="eventData">Dati del puntatore al termine del trascinamento.</param>
        public void OnEndDrag(PointerEventData eventData)
        {
            float delta = eventData.position.x - _dragStartX;
            if (Mathf.Abs(delta) < _swipeThreshold)
            {
                return;
            }

            if (delta < 0f)
            {
                ShowNextPage();
                return;
            }

            ShowPreviousPage();
        }

        /// <summary>
        /// Nasconde il popup.
        /// </summary>
        private void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Mostra la pagina precedente, se disponibile.
        /// </summary>
        private void ShowPreviousPage()
        {
            _currentPageIndex = Mathf.Max(_currentPageIndex - 1, 0);
            RenderCurrentPage();
        }

        /// <summary>
        /// Mostra la pagina successiva, se disponibile.
        /// </summary>
        private void ShowNextPage()
        {
            _currentPageIndex = Mathf.Min(_currentPageIndex + 1, _pages.Length - 1);
            RenderCurrentPage();
        }

        /// <summary>
        /// Aggiorna testi e stato dei controlli per la pagina corrente.
        /// </summary>
        private void RenderCurrentPage()
        {
            RulePage page = _pages[_currentPageIndex];

            if (_sectionLabel != null)
            {
                _sectionLabel.text = page.Title;
            }

            if (_bodyLabel != null)
            {
                _bodyLabel.text = page.Body;
            }

            if (_pageLabel != null)
            {
                _pageLabel.text = $"{_currentPageIndex + 1} / {_pages.Length}";
            }

            if (_previousButton != null)
            {
                _previousButton.interactable = _currentPageIndex > 0;
            }

            if (_nextButton != null)
            {
                _nextButton.interactable = _currentPageIndex < _pages.Length - 1;
            }
        }

        private readonly struct RulePage
        {
            public RulePage(string title, string body)
            {
                Title = title;
                Body = body;
            }

            public string Title { get; }

            public string Body { get; }
        }
    }
}
