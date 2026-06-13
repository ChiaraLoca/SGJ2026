using System.Collections.Generic;
using UnityEngine;
using FourE.Cards;

namespace FourE.Commanders
{
    /// <summary>
    /// Definizione immutabile di un comandante (secchione): Note di base e carte legate.
    /// </summary>
    [CreateAssetMenu(fileName = "Commander", menuName = "4E/Commander", order = 20)]
    public sealed class CommanderDataSO : ScriptableObject
    {
        [SerializeField] private string _commanderName;
        [SerializeField] private CommanderKind _kind;
        [SerializeField] private int _baseNote;
        [SerializeField] private CardDataSO[] _linkedCards;
        [SerializeField] private Sprite _portrait;

        [Header("Descrizioni passive (UI selezione)")]
        [TextArea] [SerializeField] private string _baseAbilityDescription;
        [TextArea] [SerializeField] private string _unlockConditionDescription;
        [TextArea] [SerializeField] private string _secondaryAbilityDescription;

        /// <summary>Nome visualizzato del comandante.</summary>
        public string CommanderName => _commanderName;

        /// <summary>Identità del comandante: determina le passive applicate.</summary>
        public CommanderKind Kind => _kind;

        /// <summary>Note di partenza del comandante a inizio round.</summary>
        public int BaseNote => _baseNote;

        /// <summary>Le carte di partenza legate a questo comandante.</summary>
        public IReadOnlyList<CardDataSO> LinkedCards => _linkedCards;

        /// <summary>Ritratto del comandante per la UI.</summary>
        public Sprite Portrait => _portrait;

        /// <summary>Descrizione dell'abilità base, per la schermata di selezione.</summary>
        public string BaseAbilityDescription => _baseAbilityDescription;

        /// <summary>Descrizione della condizione di sblocco della secondaria, per la UI.</summary>
        public string UnlockConditionDescription => _unlockConditionDescription;

        /// <summary>Descrizione dell'abilità secondaria, per la UI.</summary>
        public string SecondaryAbilityDescription => _secondaryAbilityDescription;
    }
}
