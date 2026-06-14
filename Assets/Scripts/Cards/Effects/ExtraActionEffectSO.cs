using UnityEngine;
using FourE.Core;

namespace FourE.Cards.Effects
{
    /// <summary>
    /// Concede o sottrae azioni nel turno corrente.
    /// Usato da Metodo, Test di Cooper, Progetto, Presentazione, Sciopero, Studio Notturno.
    /// </summary>
    [CreateAssetMenu(fileName = "ExtraActionEffect", menuName = "4E/Effects/Extra Action", order = 4)]
    public sealed class ExtraActionEffectSO : CardEffectSO
    {
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

            context.RegisterChange(new GrantActionsChange(turns, _actions));
        }
    }
}
