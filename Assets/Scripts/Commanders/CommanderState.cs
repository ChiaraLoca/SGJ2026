using System;
using System.Collections.Generic;
using UnityEngine;
using FourE.Cards;

namespace FourE.Commanders
{
    /// <summary>
    /// Stato runtime di un comandante durante la partita: Note correnti ed effetti attivi.
    /// La Note finale è derivata, non memorizzata direttamente.
    /// </summary>
    [Serializable]
    public sealed class CommanderState
    {
        [SerializeField] private CommanderDataSO _data;
        [SerializeField] private int _baseNote;
        [SerializeField] private int _instantNoteDelta;
        [SerializeField] private List<ActiveEffect> _activeBuffs = new();
        [SerializeField] private List<ActiveEffect> _activeDebuffs = new();
        [SerializeField] private int _debuffShields;
        [SerializeField] private bool _noteFloorLocked;
        [SerializeField] private bool _secondaryUnlocked;

        /// <summary>Definizione statica di origine del comandante.</summary>
        public CommanderDataSO Data => _data;

        /// <summary>Note di base, copiata dalla definizione all'avvio.</summary>
        public int BaseNote => _baseNote;

        /// <summary>
        /// Modifica accumulata dagli effetti istantanei del round corrente.
        /// Azzerata al reset post-Verifica.
        /// </summary>
        public int InstantNoteDelta => _instantNoteDelta;

        /// <summary>Buff a durata attivi sul comandante.</summary>
        public IReadOnlyList<ActiveEffect> ActiveBuffs => _activeBuffs;

        /// <summary>Debuff a durata attivi sul comandante.</summary>
        public IReadOnlyList<ActiveEffect> ActiveDebuffs => _activeDebuffs;

        /// <summary>True se il comandante ha almeno un debuff attivo.</summary>
        public bool HasActiveDebuff => _activeDebuffs.Count > 0;

        /// <summary>Scudi anti-debuff residui: ognuno annulla il prossimo debuff ricevuto.</summary>
        public int DebuffShields => _debuffShields;

        /// <summary>True se la Note non può calare (immunità temporanea da Fidanzata).</summary>
        public bool IsNoteFloorLocked => _noteFloorLocked;

        /// <summary>
        /// True se l'abilità secondaria del comandante è stata sbloccata.
        /// Lo sblocco è permanente per tutta la partita (non si azzera tra i round).
        /// </summary>
        public bool SecondaryUnlocked => _secondaryUnlocked;

        /// <summary>
        /// Note corrente effettiva: base + modifiche istantanee + buff attivi - debuff attivi.
        /// Non scende mai sotto zero.
        /// </summary>
        public int CurrentNote
        {
            get
            {
                int total = _baseNote + _instantNoteDelta;
                foreach (ActiveEffect buff in _activeBuffs)
                {
                    total += buff.Magnitude;
                }
                foreach (ActiveEffect debuff in _activeDebuffs)
                {
                    total -= debuff.Magnitude;
                }
                return total < 0 ? 0 : total;
            }
        }

        /// <summary>
        /// Crea lo stato runtime a partire dalla definizione del comandante.
        /// </summary>
        /// <param name="data">Definizione ScriptableObject del comandante.</param>
        public CommanderState(CommanderDataSO data)
        {
            _data = data;
            _baseNote = data.BaseNote;
        }

        /// <summary>
        /// Applica una modifica istantanea alla Note (positiva per buff, negativa per debuff).
        /// Un delta negativo è annullato da immunità o da uno scudo anti-debuff.
        /// </summary>
        /// <param name="delta">Variazione con segno.</param>
        public void ApplyInstantDelta(int delta)
        {
            if (delta < 0 && AbsorbNegative())
            {
                return;
            }

            _instantNoteDelta += delta;
        }

        /// <summary>
        /// Registra un buff a durata sul comandante.
        /// </summary>
        /// <param name="effect">Effetto attivo da aggiungere.</param>
        public void AddBuff(ActiveEffect effect)
        {
            _activeBuffs.Add(effect);
        }

        /// <summary>
        /// Registra un debuff a durata sul comandante, salvo immunità o scudo che lo annulli.
        /// </summary>
        /// <param name="effect">Effetto attivo da aggiungere.</param>
        public void AddDebuff(ActiveEffect effect)
        {
            if (AbsorbNegative())
            {
                return;
            }

            _activeDebuffs.Add(effect);
        }

        /// <summary>
        /// Aggiunge uno scudo che annullerà il prossimo debuff ricevuto.
        /// </summary>
        public void AddDebuffShield()
        {
            _debuffShields++;
        }

        /// <summary>
        /// Blocca o sblocca il calo della Note (immunità temporanea).
        /// </summary>
        /// <param name="locked">True per impedire qualsiasi riduzione di Note.</param>
        public void SetNoteFloorLocked(bool locked)
        {
            _noteFloorLocked = locked;
        }

        /// <summary>
        /// Sblocca in modo permanente l'abilità secondaria del comandante.
        /// </summary>
        public void MarkSecondaryUnlocked()
        {
            _secondaryUnlocked = true;
        }

        /// <summary>
        /// Determina se una riduzione di Note va annullata e consuma l'eventuale scudo.
        /// L'immunità blocca senza consumare; lo scudo blocca consumandosi.
        /// </summary>
        /// <returns>True se la riduzione deve essere annullata.</returns>
        private bool AbsorbNegative()
        {
            if (_noteFloorLocked)
            {
                return true;
            }

            if (_debuffShields > 0)
            {
                _debuffShields--;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Decrementa la durata di tutti gli effetti a turni e rimuove quelli scaduti.
        /// Da chiamare a fine turno del giocatore che subisce gli effetti.
        /// </summary>
        public void TickActiveEffects()
        {
            _activeBuffs.RemoveAll(effect => effect.TickTurn());
            _activeDebuffs.RemoveAll(effect => effect.TickTurn());
        }

        /// <summary>
        /// Azzera Note temporanee ed effetti attivi dopo la conversione post-Verifica.
        /// </summary>
        public void ResetForNewRound()
        {
            _instantNoteDelta = 0;
            _activeBuffs.Clear();
            _activeDebuffs.Clear();
            _debuffShields = 0;
            _noteFloorLocked = false;
        }
    }
}
