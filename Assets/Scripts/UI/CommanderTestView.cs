using UnityEngine;
using UnityEngine.UI;

namespace FourE.UI
{
    /// <summary>
    /// Visualizza lo stato di un comandante nella scena di test: nome, Note attuali,
    /// buff/debuff attivi e stato passiva secondaria.
    /// </summary>
    public class CommanderTestView : MonoBehaviour
    {
        [SerializeField] private Text _commanderNameText;
        [SerializeField] private Text _notesText;
        [SerializeField] private Text _debuffText;
        [SerializeField] private Text _buffText;
        [SerializeField] private Text _secondaryStatusText;
        [SerializeField] private Image _backgroundImage;

        private FourE.Commanders.CommanderState _commander;

        /// <summary>
        /// Metodo pubblico per il setup editor: assegna i reference di testo.
        /// </summary>
        public void SetTextReferences(
            Text commanderNameText,
            Text notesText,
            Text debuffText,
            Text buffText,
            Text secondaryStatusText)
        {
            _commanderNameText = commanderNameText;
            _notesText = notesText;
            _debuffText = debuffText;
            _buffText = buffText;
            _secondaryStatusText = secondaryStatusText;
        }

        /// <summary>
        /// Assegna il comandante da visualizzare.
        /// </summary>
        public void SetCommander(FourE.Commanders.CommanderState commander)
        {
            _commander = commander;
            Refresh();
        }

        /// <summary>
        /// Aggiorna la visualizzazione dello stato del comandante.
        /// </summary>
        public void Refresh()
        {
            if (_commander == null) return;

            if (_commanderNameText != null)
            {
                _commanderNameText.text = _commander.CommanderData.CommanderName;
            }

            if (_notesText != null)
            {
                _notesText.text = $"Note: {_commander.CurrentNote}";
            }

            // Visualizza i debuff attivi
            if (_debuffText != null)
            {
                if (_commander.ActiveDebuffs.Count > 0)
                {
                    string debuffStr = "Debuff: ";
                    foreach (var debuff in _commander.ActiveDebuffs)
                    {
                        debuffStr += $"{debuff.Magnitude} ({debuff.RemainingTurns}t) ";
                    }
                    _debuffText.text = debuffStr;
                    _debuffText.color = Color.red;
                }
                else
                {
                    _debuffText.text = "Debuff: Nessuno";
                    _debuffText.color = Color.green;
                }
            }

            // Visualizza i buff attivi
            if (_buffText != null)
            {
                if (_commander.ActiveBuffs.Count > 0)
                {
                    string buffStr = "Buff: ";
                    foreach (var buff in _commander.ActiveBuffs)
                    {
                        buffStr += $"+{buff.Magnitude} ({buff.RemainingTurns}t) ";
                    }
                    _buffText.text = buffStr;
                    _buffText.color = Color.green;
                }
                else
                {
                    _buffText.text = "Buff: Nessuno";
                    _buffText.color = Color.gray;
                }
            }

            // Visualizza lo stato della passiva secondaria
            if (_secondaryStatusText != null)
            {
                if (_commander.SecondaryUnlocked)
                {
                    _secondaryStatusText.text = "✓ Passiva Secondaria Sbloccata";
                    _secondaryStatusText.color = Color.yellow;
                }
                else
                {
                    _secondaryStatusText.text = "✗ Passiva Secondaria Bloccata";
                    _secondaryStatusText.color = Color.gray;
                }
            }
        }
    }
}
