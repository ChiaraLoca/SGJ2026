using System.Collections.Generic;
using UnityEngine;
using FourE.Cards;
using FourE.Commanders;

namespace FourE.Config
{
    /// <summary>
    /// Archivio dei contenuti di una partita: comandanti dei due giocatori,
    /// carta Verifica e catalogo dello shop. Riferito dal GameStateManager in Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "GameContent", menuName = "4E/Game Content", order = 1)]
    public sealed class GameContentSO : ScriptableObject
    {
        [Header("Comandanti")]
        [SerializeField] private CommanderDataSO[] _firstPlayerCommanders;
        [SerializeField] private CommanderDataSO[] _secondPlayerCommanders;

        [Header("Carte")]
        [SerializeField] private CardDataSO _verificaCard;
        [SerializeField] private CardDataSO[] _shopCatalog;

        /// <summary>Comandanti assegnati al primo giocatore (lunghezza CommandersPerPlayer).</summary>
        public IReadOnlyList<CommanderDataSO> FirstPlayerCommanders => _firstPlayerCommanders;

        /// <summary>Comandanti assegnati al secondo giocatore (lunghezza CommandersPerPlayer).</summary>
        public IReadOnlyList<CommanderDataSO> SecondPlayerCommanders => _secondPlayerCommanders;

        /// <summary>Carta Verifica assegnata a entrambi i giocatori nello slot dedicato.</summary>
        public CardDataSO VerificaCard => _verificaCard;

        /// <summary>Catalogo completo da cui generare e rinfrescare i pool shop.</summary>
        public IReadOnlyList<CardDataSO> ShopCatalog => _shopCatalog;
    }
}
