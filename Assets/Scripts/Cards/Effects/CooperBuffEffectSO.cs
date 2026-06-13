using UnityEngine;
using FourE.Commanders;
using FourE.Core;

namespace FourE.Cards.Effects
{
    /// <summary>
    /// Effetto speciale per Test di Cooper: applica +2 Note al comandante più debole.
    /// Se dopo l'applicazione la nota è ≤ 3, la carta ritorna in mano al giocatore.
    /// </summary>
    [CreateAssetMenu(fileName = "CooperBuffEffect", menuName = "4E/Effects/Cooper Buff", order = 5)]
    public sealed class CooperBuffEffectSO : CardEffectSO
    {
        [SerializeField] private int _magnitude = 2;

        /// <inheritdoc/>
        public override void Apply(GameContext context)
        {
            CommanderState lowestCmd = CommanderWithLowestNote(context.ActivePlayer);
            if (lowestCmd == null || context.Card == null)
            {
                return;
            }

            // Registra una modifica speciale che applica il buff e controlla la condizione
            context.RegisterChange(new CooperBuffChange(lowestCmd, context.ActivePlayer, context.Card, _magnitude));
        }

        /// <summary>
        /// Restituisce il comandante del giocatore con la Note corrente più bassa.
        /// In caso di parità restituisce il primo comandante (slot 0).
        /// </summary>
        private static CommanderState CommanderWithLowestNote(PlayerState player)
        {
            CommanderState lowest = player.Commanders[GameConstants.FirstCommanderIndex];
            foreach (CommanderState c in player.Commanders)
            {
                if (c.CurrentNote < lowest.CurrentNote)
                    lowest = c;
            }
            return lowest;
        }
    }
}
