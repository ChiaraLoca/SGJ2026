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
        [SerializeField] private int _baseNote;
        [SerializeField] private CardDataSO[] _linkedCards;
        [SerializeField] private Sprite _portrait;

        /// <summary>Nome visualizzato del comandante.</summary>
        public string CommanderName => _commanderName;

        /// <summary>Note di partenza del comandante a inizio round.</summary>
        public int BaseNote => _baseNote;

        /// <summary>Le carte di partenza legate a questo comandante.</summary>
        public IReadOnlyList<CardDataSO> LinkedCards => _linkedCards;

        /// <summary>Ritratto del comandante per la UI.</summary>
        public Sprite Portrait => _portrait;
    }
}
