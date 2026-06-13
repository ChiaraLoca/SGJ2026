using System.Collections.Generic;
using FourE.Cards;
using FourE.Commanders;
using FourE.Config;

namespace FourE.Network
{
    /// <summary>
    /// Mappa bidirezionale tra <see cref="CardDataSO"/> e un id intero stabile.
    /// Gli ScriptableObject non viaggiano in rete: gli intent e i DTO referenziano
    /// le carte tramite questi id, che host e client risolvono dallo stesso contenuto.
    /// </summary>
    public sealed class CardRegistry
    {
        /// <summary>Id usato quando non c'è alcuna carta (es. slot Verifica vuoto).</summary>
        public const int NoCard = -1;

        private readonly List<CardDataSO> _cards = new();
        private readonly Dictionary<CardDataSO, int> _ids = new();

        /// <summary>Numero di carte registrate.</summary>
        public int Count => _cards.Count;

        /// <summary>
        /// Registra una carta se non già presente, assegnandole il prossimo id libero.
        /// </summary>
        /// <param name="card">Carta da registrare; i null sono ignorati.</param>
        public void Register(CardDataSO card)
        {
            if (card == null || _ids.ContainsKey(card))
            {
                return;
            }

            _ids[card] = _cards.Count;
            _cards.Add(card);
        }

        /// <summary>
        /// Restituisce l'id stabile di una carta.
        /// </summary>
        /// <param name="card">Carta di cui ottenere l'id.</param>
        /// <returns>L'id della carta, o <see cref="NoCard"/> se assente.</returns>
        public int GetId(CardDataSO card)
        {
            return card != null && _ids.TryGetValue(card, out int id) ? id : NoCard;
        }

        /// <summary>
        /// Risolve un id nella carta corrispondente.
        /// </summary>
        /// <param name="id">Id stabile della carta.</param>
        /// <returns>La carta, o null se l'id non è valido.</returns>
        public CardDataSO GetCard(int id)
        {
            return id >= 0 && id < _cards.Count ? _cards[id] : null;
        }

        /// <summary>
        /// Converte una sequenza di carte nei rispettivi id.
        /// </summary>
        /// <param name="cards">Carte da convertire.</param>
        /// <returns>Array di id, nello stesso ordine.</returns>
        public int[] ToIds(IReadOnlyList<CardDataSO> cards)
        {
            int[] ids = new int[cards.Count];
            for (int i = 0; i < cards.Count; i++)
            {
                ids[i] = GetId(cards[i]);
            }

            return ids;
        }

        /// <summary>
        /// Costruisce un registry completo da tutte le carte referenziate dal contenuto:
        /// carte legate ai comandanti, carta Verifica e catalogo shop.
        /// </summary>
        /// <param name="content">Archivio contenuti della partita.</param>
        /// <returns>Registry popolato e pronto all'uso.</returns>
        public static CardRegistry Build(GameContentSO content)
        {
            CardRegistry registry = new();

            RegisterCommanderCards(registry, content.FirstPlayerCommanders);
            RegisterCommanderCards(registry, content.SecondPlayerCommanders);
            registry.Register(content.VerificaCard);

            foreach (CardDataSO card in content.ShopCatalog)
            {
                registry.Register(card);
            }

            return registry;
        }

        /// <summary>
        /// Registra le carte di partenza di una lista di comandanti.
        /// </summary>
        /// <param name="registry">Registry da popolare.</param>
        /// <param name="commanders">Comandanti di cui registrare le carte legate.</param>
        private static void RegisterCommanderCards(CardRegistry registry, IReadOnlyList<CommanderDataSO> commanders)
        {
            foreach (CommanderDataSO commander in commanders)
            {
                foreach (CardDataSO card in commander.LinkedCards)
                {
                    registry.Register(card);
                }
            }
        }
    }
}
