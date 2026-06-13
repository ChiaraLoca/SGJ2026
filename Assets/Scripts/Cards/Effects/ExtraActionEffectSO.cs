using UnityEngine;
using FourE.Core;

namespace FourE.Cards.Effects
{
    /// <summary>
    /// Concede o sottrae azioni nel turno corrente, oppure raddoppia quelle rimanenti.
    /// Usato da Metodo, Test di Cooper, Progetto, Presentazione, Sciopero, Studio Notturno, Copiare.
    /// </summary>
    [CreateAssetMenu(fileName = "ExtraActionEffect", menuName = "4E/Effects/Extra Action", order = 4)]
    public sealed class ExtraActionEffectSO : CardEffectSO
    {
        [Tooltip("Se attivo, raddoppia le azioni rimanenti e ignora il valore numerico.")]
        [SerializeField] private bool _doubleActions;
        [Tooltip("Azioni da aggiungere (negativo per ridurle).")]
        [SerializeField] private int _actions = 1;

        /// <inheritdoc/>
        public override void Apply(GameContext context)
        {
            TurnManager turns = context.State?.Turns;
            if (turns == null)
            {
                return;
            }

            if (_doubleActions)
            {
                context.RegisterChange(new DoubleActionsChange(turns));
            }
            else
            {
                context.RegisterChange(new GrantActionsChange(turns, _actions));
            }
        }
    }
}
