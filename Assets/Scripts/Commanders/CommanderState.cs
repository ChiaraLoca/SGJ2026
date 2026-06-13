using System.Collections.Generic;
using FourE.Cards;

namespace FourE.Commanders
{
    /// <summary>
    /// Stato runtime di un comandante durante la partita: Note correnti ed effetti attivi.
    /// La Note finale è derivata, non memorizzata direttamente.
    /// </summary>
    public sealed class CommanderState
    {
        private readonly List<ActiveEffect> _activeBuffs = new();
        private readonly List<ActiveEffect> _activeDebuffs = new();

        /// <summary>Definizione statica di origine del comandante.</summary>
        public CommanderDataSO Data { get; }

        /// <summary>Note di base, copiata dalla definizione all'avvio.</summary>
        public int BaseNote { get; }

        /// <summary>
        /// Modifica accumulata dagli effetti istantanei del round corrente.
        /// Azzerata al reset post-Verifica.
        /// </summary>
        public int InstantNoteDelta { get; private set; }

        /// <summary>Buff a durata attivi sul comandante.</summary>
        public IReadOnlyList<ActiveEffect> ActiveBuffs => _activeBuffs;

        /// <summary>Debuff a durata attivi sul comandante.</summary>
        public IReadOnlyList<ActiveEffect> ActiveDebuffs => _activeDebuffs;

        /// <summary>True se il comandante ha almeno un debuff attivo.</summary>
        public bool HasActiveDebuff => _activeDebuffs.Count > 0;

        /// <summary>
        /// Note corrente effettiva: base + modifiche istantanee + buff attivi - debuff attivi.
        /// Non scende mai sotto zero.
        /// </summary>
        public int CurrentNote
        {
            get
            {
                int total = BaseNote + InstantNoteDelta;
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
            Data = data;
            BaseNote = data.BaseNote;
        }

        /// <summary>
        /// Applica una modifica istantanea alla Note (positiva per buff, negativa per debuff).
        /// </summary>
        /// <param name="delta">Variazione con segno.</param>
        public void ApplyInstantDelta(int delta)
        {
            InstantNoteDelta += delta;
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
        /// Registra un debuff a durata sul comandante.
        /// </summary>
        /// <param name="effect">Effetto attivo da aggiungere.</param>
        public void AddDebuff(ActiveEffect effect)
        {
            _activeDebuffs.Add(effect);
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
            InstantNoteDelta = 0;
            _activeBuffs.Clear();
            _activeDebuffs.Clear();
        }
    }
}
