using System.Collections.Generic;
using UnityEngine;
using FourE.Config;

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
        [SerializeField] private CardTag _tags = CardTag.None;
        [SerializeField] private Sprite _artwork;

        [Header("Economia")]
        [SerializeField] private CardTier _tier = CardTier.C;
        [Tooltip("Costo esplicito di fallback, usato solo se il GameConfig non è attivo.")]
        [SerializeField] private int _shopCost;
        [SerializeField] private int _minCreditsRequired;

        [Header("Effetti")]
        [SerializeField] private CardEffectSO[] _effects;
        [Tooltip("Azioni consumate dalla giocata (default 1). Studio Notturno = 2.")]
        [SerializeField] private int _actionCost = 1;
        [Tooltip("True solo per Test di Cooper: la carta torna in mano se la nota era <= 3 prima del buff.")]
        [SerializeField] private bool _isCooper;

        /// <summary>Nome visualizzato della carta.</summary>
        public string CardName => _cardName;

        /// <summary>Testo narrativo o descrizione della regola.</summary>
        public string Description => _description;

        /// <summary>Categoria della carta (Standard o Verifica).</summary>
        public CardType CardType => _cardType;

        /// <summary>Affinità con un comandante del giocatore.</summary>
        public CardAffinity Affinity => _affinity;

        /// <summary>Tag tematici della carta (combinabili).</summary>
        public CardTag Tags => _tags;

        /// <summary>Immagine completa usata dalla vista della carta.</summary>
        public Sprite Artwork => _artwork;

        /// <summary>Fascia di costo della carta.</summary>
        public CardTier Tier => _tier;

        /// <summary>
        /// Costo in Note pagato nello shop. Risolto dal <see cref="GameConfigSO"/> attivo
        /// in base al <see cref="Tier"/>; ricade su <c>_shopCost</c> se il config non è attivo.
        /// </summary>
        public int ShopCost => GameConfigSO.Instance != null
            ? GameConfigSO.Instance.GetTierCost(_tier)
            : _shopCost;

        /// <summary>Soglia minima di Credits per apparire nel pool shop.</summary>
        public int MinCreditsRequired => _minCreditsRequired;

        /// <summary>Effetti applicati in sequenza quando la carta viene giocata.</summary>
        public IReadOnlyList<CardEffectSO> Effects => _effects;

        /// <summary>Azioni consumate dalla giocata (1 per default; Studio Notturno = 2).</summary>
        public int ActionCost => _actionCost > 0 ? _actionCost : 1;

        /// <summary>True se la carta è una Verifica che chiude il round.</summary>
        public bool IsVerifica => _cardType == CardType.Verifica;

        /// <summary>True se la carta è il Test di Cooper (ritorno in mano condizionato alla nota).</summary>
        public bool IsCooper => _isCooper;

        /// <summary>Verifica se la carta possiede il tag indicato.</summary>
        /// <param name="tag">Tag da cercare.</param>
        /// <returns>True se la carta ha quel tag.</returns>
        public bool HasTag(CardTag tag) => (_tags & tag) != 0;

        /// <summary>True se almeno un effetto richiede la selezione di un qualsiasi comandante (qualsiasi lato).</summary>
        public bool RequiresAnyTargetSelection
        {
            get
            {
                if (_effects == null) return false;
                foreach (CardEffectSO e in _effects)
                {
                    if (e != null && (e.Target == EffectTarget.SelectedCommanders
                                      || e.Target == EffectTarget.SelectedFirstCommander
                                      || e.Target == EffectTarget.SelectedSecondCommander))
                        return true;
                }
                return false;
            }
        }

        /// <summary>True se almeno un effetto richiede la selezione di un comandante avversario.</summary>
        public bool RequiresEnemyTargetSelection
        {
            get
            {
                if (_effects == null) return false;
                foreach (CardEffectSO e in _effects)
                {
                    if (e != null && (e.Target == EffectTarget.SelectedEnemyCommanders
                                      || e.Target == EffectTarget.SelectedOwnAndEnemy))
                        return true;
                }
                return false;
            }
        }

        /// <summary>True se almeno un effetto richiede la selezione di un comandante proprio.</summary>
        public bool RequiresOwnTargetSelection
        {
            get
            {
                if (_effects == null) return false;
                foreach (CardEffectSO e in _effects)
                {
                    if (e != null && (e.Target == EffectTarget.SelectedOwnCommanders
                                      || e.Target == EffectTarget.SelectedOwnAndEnemy))
                        return true;
                }
                return false;
            }
        }

        /// <summary>True se la carta richiede la selezione di almeno un bersaglio a runtime.</summary>
        public bool RequiresTargetSelection => RequiresAnyTargetSelection || RequiresEnemyTargetSelection || RequiresOwnTargetSelection;

        /// <summary>True se la carta richiede due bersagli liberi scelti in ordine.</summary>
        public bool RequiresOrderedAnyTargetSelection
        {
            get
            {
                if (_effects == null)
                {
                    return false;
                }

                foreach (CardEffectSO effect in _effects)
                {
                    if (effect != null
                        && (effect.Target == EffectTarget.SelectedFirstCommander
                            || effect.Target == EffectTarget.SelectedSecondCommander))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
