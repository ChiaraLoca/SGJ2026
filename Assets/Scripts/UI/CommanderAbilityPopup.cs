using UnityEngine;
using UnityEngine.UI;
using FourE.Commanders;

namespace FourE.UI
{
    /// <summary>
    /// Riquadro prefab che presenta le abilita di un comandante durante la partita.
    /// </summary>
    public sealed class CommanderAbilityPopup : MonoBehaviour
    {
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Text _baseAbilityLabel;
        [SerializeField] private Text _unlockConditionLabel;
        [SerializeField] private Text _secondaryAbilityLabel;
        [SerializeField] private Color _lockedSecondaryColor = new(0.72f, 0.72f, 0.72f, 1f);
        [SerializeField] private Color _unlockedSecondaryColor = new(1f, 0.86f, 0.2f, 1f);

        /// <summary>
        /// Popola il riquadro con le descrizioni statiche e lo stato della secondaria.
        /// </summary>
        /// <param name="data">Definizione del comandante da mostrare.</param>
        /// <param name="secondaryUnlocked">True se la passiva secondaria e gia attiva.</param>
        public void Bind(CommanderDataSO data, bool secondaryUnlocked)
        {
            if (data == null)
            {
                return;
            }

            if (_nameLabel != null)
            {
                _nameLabel.text = data.CommanderName;
            }

            if (_baseAbilityLabel != null)
            {
                _baseAbilityLabel.text = $"PASSIVA BASE\n{data.BaseAbilityDescription}";
            }

            if (_unlockConditionLabel != null)
            {
                string status = secondaryUnlocked ? "SBLOCCATA" : "BLOCCATA";
                _unlockConditionLabel.text = $"{status}\n{data.UnlockConditionDescription}";
            }

            if (_secondaryAbilityLabel != null)
            {
                _secondaryAbilityLabel.text = $"PASSIVA SECONDARIA\n{data.SecondaryAbilityDescription}";
                _secondaryAbilityLabel.color = secondaryUnlocked
                    ? _unlockedSecondaryColor
                    : _lockedSecondaryColor;
            }
        }
    }
}
