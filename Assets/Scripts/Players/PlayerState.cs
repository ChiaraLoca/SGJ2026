using System;
using System.Collections.Generic;
using UnityEngine;
using FourE.Cards;
using FourE.Commanders;
using FourE.Config;

namespace FourE.Players
{
    /// <summary>
    /// Stato runtime di un giocatore.
    /// Distingue due valute: <b>Note</b> (punteggio temporaneo dei comandanti, azzerato a ogni round
    /// e convertito in Crediti dalla Verifica) e <b>Crediti</b> (punteggio permanente, valuta dello shop
    /// e criterio di vittoria). La Verifica è una carta normale nel mazzo, non uno slot dedicato.
    /// </summary>
    [Serializable]
    public sealed class PlayerState
    {
        [SerializeField] private int _actorNumber;
        [SerializeField] private int _credits;
        [SerializeField] private List<CardDataSO> _hand = new();
        [SerializeField] private List<CardDataSO> _deck = new();
        [SerializeField] private List<CardDataSO> _discardPile = new();
        [SerializeField] private List<CardDataSO> _shopPool = new();
        [SerializeField] private CommanderState[] _commanders;
        [SerializeField] private bool _verificaBlocked;
        [SerializeField] private bool _wikipediaInterceptActive;
        [SerializeField] private bool _constitutionProtectionActive;
        [SerializeField] private int _verificaPlayedCount;

        /// <summary>Numero attore Photon che identifica il giocatore.</summary>
        public int ActorNumber => _actorNumber;

        /// <summary>
        /// Punteggio permanente e valuta dello shop. Cresce con la conversione delle Note alla Verifica;
        /// cala con gli acquisti. Determina il vincitore della partita.
        /// </summary>
        public int Credits => _credits;

        /// <summary>Carte attualmente in mano (inclusa la Verifica, se pescata).</summary>
        public List<CardDataSO> Hand => _hand;

        /// <summary>Mazzo personale da cui si pesca.</summary>
        public List<CardDataSO> Deck => _deck;

        /// <summary>Carte scartate e acquistate, rimischiate nel mazzo alla Fase DRAW.</summary>
        public List<CardDataSO> DiscardPile => _discardPile;

        /// <summary>Pool shop personale, filtrato per Credits.</summary>
        public List<CardDataSO> ShopPool => _shopPool;

        /// <summary>I due comandanti del giocatore.</summary>
        public CommanderState[] Commanders => _commanders;

        /// <summary>True se al giocatore è impedito giocare la Verifica nel turno corrente (Sciopero).</summary>
        public bool VerificaBlocked { get => _verificaBlocked; set => _verificaBlocked = value; }

        /// <summary>True se è attivo uno scudo Wikipedia che intercetta la prossima carta dell'avversario.</summary>
        public bool WikipediaInterceptActive { get => _wikipediaInterceptActive; set => _wikipediaInterceptActive = value; }

        /// <summary>
        /// True se Costituzione annulla aumenti di Note e carte ottenuti dalle carte dell'avversario.
        /// Decade all'inizio del prossimo turno del proprietario.
        /// </summary>
        public bool ConstitutionProtectionActive
        {
            get => _constitutionProtectionActive;
            set => _constitutionProtectionActive = value;
        }

        /// <summary>Numero di Verifiche giocate da questo giocatore nell'intera partita (passiva base Storia).</summary>
        public int VerificaPlayedCount => _verificaPlayedCount;

        /// <summary>
        /// Somma delle Note correnti dei due comandanti: punteggio temporaneo del round,
        /// convertito in Crediti quando si gioca la Verifica.
        /// </summary>
        public int TotalNotes => _commanders[GameConstants.FirstCommanderIndex].CurrentNote
                                + _commanders[GameConstants.SecondCommanderIndex].CurrentNote;

        /// <summary>
        /// Crea lo stato del giocatore con i suoi comandanti.
        /// </summary>
        /// <param name="actorNumber">Identificativo attore Photon.</param>
        /// <param name="commanders">Array dei comandanti, di lunghezza <see cref="GameConstants.CommandersPerPlayer"/>.</param>
        public PlayerState(int actorNumber, CommanderState[] commanders)
        {
            _actorNumber = actorNumber;
            _commanders = commanders;
        }

        /// <summary>
        /// Aggiunge Crediti al giocatore (conversione delle Note alla Verifica).
        /// </summary>
        /// <param name="amount">Crediti da aggiungere.</param>
        public void AddCredits(int amount)
        {
            _credits += amount;
        }

        /// <summary>
        /// Spende Crediti per un acquisto shop. Non scende mai sotto zero.
        /// </summary>
        /// <param name="amount">Crediti da spendere.</param>
        public void SpendCredits(int amount)
        {
            _credits -= amount;
            if (_credits < 0)
            {
                _credits = 0;
            }
        }

        /// <summary>
        /// Incrementa il conteggio delle Verifiche giocate dal giocatore nella partita.
        /// </summary>
        public void IncrementVerificaPlayedCount()
        {
            _verificaPlayedCount++;
        }

        /// <summary>
        /// Restituisce il comandante con la Note corrente più bassa.
        /// In caso di parità restituisce il primo comandante (slot 0).
        /// </summary>
        public CommanderState LowestNoteCommander()
        {
            CommanderState lowest = _commanders[GameConstants.FirstCommanderIndex];
            foreach (CommanderState c in _commanders)
            {
                if (c.CurrentNote < lowest.CurrentNote)
                    lowest = c;
            }
            return lowest;
        }
    }
}
