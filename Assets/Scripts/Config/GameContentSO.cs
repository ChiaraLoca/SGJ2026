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
        [Tooltip("Tutti i comandanti selezionabili (uno per CommanderKind). Usato dalla schermata di selezione.")]
        [SerializeField] private CommanderDataSO[] _commanderCatalog;
        [Tooltip("Comandanti di default del primo giocatore, usati se non c'è selezione (es. test diretto).")]
        [SerializeField] private CommanderDataSO[] _firstPlayerCommanders;
        [Tooltip("Comandanti di default del secondo giocatore, usati se non c'è selezione.")]
        [SerializeField] private CommanderDataSO[] _secondPlayerCommanders;

        [Header("Carte")]
        [SerializeField] private CardDataSO _verificaCard;
        [SerializeField] private CardDataSO[] _shopCatalog;

        /// <summary>Tutti i comandanti selezionabili nella schermata di selezione (uno per <see cref="CommanderKind"/>).</summary>
        public IReadOnlyList<CommanderDataSO> CommanderCatalog => _commanderCatalog;

        /// <summary>Comandanti assegnati al primo giocatore (lunghezza CommandersPerPlayer).</summary>
        public IReadOnlyList<CommanderDataSO> FirstPlayerCommanders => _firstPlayerCommanders;

        /// <summary>Comandanti assegnati al secondo giocatore (lunghezza CommandersPerPlayer).</summary>
        public IReadOnlyList<CommanderDataSO> SecondPlayerCommanders => _secondPlayerCommanders;

        /// <summary>Carta Verifica assegnata a entrambi i giocatori nello slot dedicato.</summary>
        public CardDataSO VerificaCard => _verificaCard;

        /// <summary>Catalogo completo da cui generare e rinfrescare i pool shop.</summary>
        public IReadOnlyList<CardDataSO> ShopCatalog => _shopCatalog;

        /// <summary>
        /// Cerca il comandante corrispondente al tipo richiesto nel catalogo selezionabile.
        /// </summary>
        /// <param name="kind">Tipo del comandante da cercare.</param>
        /// <returns>Il comandante trovato, oppure null se non è presente.</returns>
        public CommanderDataSO GetCommanderByKind(CommanderKind kind)
        {
            if (_commanderCatalog == null)
            {
                return null;
            }

            foreach (CommanderDataSO commander in _commanderCatalog)
            {
                if (commander != null && commander.Kind == kind)
                {
                    return commander;
                }
            }

            return null;
        }

        private static void AddUniqueCommanders(
            List<CommanderDataSO> catalog,
            IEnumerable<CommanderDataSO> commanders)
        {
            if (commanders == null)
            {
                return;
            }

            foreach (CommanderDataSO commander in commanders)
            {
                if (commander != null && !catalog.Contains(commander))
                {
                    catalog.Add(commander);
                }
            }
        }

        private static CommanderDataSO FindCommanderByKind(
            IEnumerable<CommanderDataSO> commanders,
            CommanderKind kind)
        {
            if (commanders == null)
            {
                return null;
            }

            foreach (CommanderDataSO commander in commanders)
            {
                if (commander != null && commander.Kind == kind)
                {
                    return commander;
                }
            }

            return null;
        }
    }
}
