using System.Collections.Generic;
using UnityEngine;

namespace FourE.Cards
{
    /// <summary>
    /// Definizione immutabile di una carta: identità, economia, affinità ed effetti.
    /// </summary>
    [CreateAssetMenu(fileName = "Card", menuName = "4E/Card", order = 10)]
    public sealed class CardDataSO : ScriptableObject
    {
        [Header("Identità")]
        [SerializeField] private string _cardName;
        [TextArea]
        [SerializeField] private string _description;
        [SerializeField] private CardType _cardType = CardType.Standard;
        [SerializeField] private CardAffinity _affinity = CardAffinity.Neutral;

        [Header("Economia")]
        [SerializeField] private int _shopCost;
        [SerializeField] private int _minCreditsRequired;

        [Header("Effetti")]
        [SerializeField] private CardEffectSO[] _effects;

        /// <summary>Nome visualizzato della carta.</summary>
        public string CardName => _cardName;

        /// <summary>Testo narrativo o descrizione della regola.</summary>
        public string Description => _description;

        /// <summary>Categoria della carta (Standard o Verifica).</summary>
        public CardType CardType => _cardType;

        /// <summary>Affinità con un comandante del giocatore.</summary>
        public CardAffinity Affinity => _affinity;

        /// <summary>Costo in Note pagato nello shop prima della conversione.</summary>
        public int ShopCost => _shopCost;

        /// <summary>Soglia minima di Credits per apparire nel pool shop.</summary>
        public int MinCreditsRequired => _minCreditsRequired;

        /// <summary>Effetti applicati in sequenza quando la carta viene giocata.</summary>
        public IReadOnlyList<CardEffectSO> Effects => _effects;

        /// <summary>True se la carta è una Verifica che chiude il round.</summary>
        public bool IsVerifica => _cardType == CardType.Verifica;
    }
}
