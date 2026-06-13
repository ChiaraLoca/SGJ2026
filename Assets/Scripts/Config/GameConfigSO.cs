using UnityEngine;
using FourE.Cards;

namespace FourE.Config
{
    /// <summary>
    /// Regola di spareggio applicata in caso di parità di Credits all'Esame Finale.
    /// </summary>
    public enum TiebreakRule
    {
        /// <summary>Vince chi ha la somma delle Note finali più alta.</summary>
        SumNotes
    }

    /// <summary>
    /// Contenitore ScriptableObject di tutti i valori di bilanciamento configurabili a runtime.
    /// Esposto come singleton tramite <see cref="Instance"/>, registrato dal bootstrap.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "4E/Game Config", order = 0)]
    public sealed class GameConfigSO : ScriptableObject
    {
        [Header("Round")]
        [SerializeField] private int _maxRounds = 3;
        [SerializeField] private int[] _cardsPlayablePerTurn = { 2, 3, 4 };

        [Header("Mano e Mazzo")]
        [SerializeField] private int _startingCardsPerCommander = 5;
        [SerializeField] private int _startingHandSize = 3;

        [Header("Shop")]
        [SerializeField] private int _shopPoolSize = 12;
        [SerializeField] private int _shopPurchasesPerRound = 2;
        [SerializeField] private int _shopRefreshSlots = 3;

        [Header("Costi per Tier (Note)")]
        [SerializeField] private int _tierCostC = 1;
        [SerializeField] private int _tierCostB = 3;
        [SerializeField] private int _tierCostA = 10;

        [Header("Conversione")]
        [SerializeField] private float _noteToCreditsMultiplier = 1f;

        [Header("Spareggio")]
        [SerializeField] private TiebreakRule _tiebreakRule = TiebreakRule.SumNotes;

        /// <summary>Istanza attiva del config, registrata all'avvio della partita.</summary>
        public static GameConfigSO Instance { get; private set; }

        /// <summary>Numero di round di Verifica prima dell'Esame Finale.</summary>
        public int MaxRounds => _maxRounds;

        /// <summary>Carte di partenza legate a ciascun comandante.</summary>
        public int StartingCardsPerCommander => _startingCardsPerCommander;

        /// <summary>Carte pescate da ciascun giocatore nella Fase DRAW.</summary>
        public int StartingHandSize => _startingHandSize;

        /// <summary>Dimensione del pool shop personale di ogni giocatore.</summary>
        public int ShopPoolSize => _shopPoolSize;

        /// <summary>Acquisti massimi consentiti per round nello shop.</summary>
        public int ShopPurchasesPerRound => _shopPurchasesPerRound;

        /// <summary>Slot del pool shop rinfrescati a ogni Fase SHOP.</summary>
        public int ShopRefreshSlots => _shopRefreshSlots;

        /// <summary>Moltiplicatore di conversione da Note a Credits.</summary>
        public float NoteToCreditsMultiplier => _noteToCreditsMultiplier;

        /// <summary>Regola di spareggio dell'Esame Finale.</summary>
        public TiebreakRule TiebreakRule => _tiebreakRule;

        /// <summary>
        /// Restituisce il costo in Note di una fascia di carte.
        /// </summary>
        /// <param name="tier">Fascia di costo della carta.</param>
        /// <returns>Costo in Note configurato per la fascia.</returns>
        public int GetTierCost(CardTier tier)
        {
            return tier switch
            {
                CardTier.A => _tierCostA,
                CardTier.B => _tierCostB,
                _ => _tierCostC
            };
        }

        /// <summary>
        /// Restituisce il numero di carte giocabili per turno nel round indicato.
        /// </summary>
        /// <param name="roundIndex">Indice del round corrente (0-based).</param>
        /// <returns>Carte giocabili per turno; l'ultimo valore se l'indice eccede l'array.</returns>
        public int GetCardsPlayablePerTurn(int roundIndex)
        {
            if (_cardsPlayablePerTurn == null || _cardsPlayablePerTurn.Length == 0)
            {
                return 0;
            }

            int clampedIndex = Mathf.Clamp(roundIndex, 0, _cardsPlayablePerTurn.Length - 1);
            return _cardsPlayablePerTurn[clampedIndex];
        }

        /// <summary>
        /// Registra questa istanza come config attiva del gioco. Chiamato dal bootstrap all'avvio.
        /// </summary>
        public void RegisterAsActive()
        {
            Instance = this;
        }
    }
}
