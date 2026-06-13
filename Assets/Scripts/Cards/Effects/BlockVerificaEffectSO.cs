using UnityEngine;
using FourE.Core;
using FourE.Players;

namespace FourE.Cards.Effects
{
    /// <summary>
    /// Impedisce al giocatore bersaglio (di norma l'avversario) di giocare la Verifica
    /// nel suo prossimo turno (Sciopero). Il blocco decade a fine turno del bersaglio.
    /// </summary>
    [CreateAssetMenu(fileName = "BlockVerificaEffect", menuName = "4E/Effects/Block Verifica", order = 12)]
    public sealed class BlockVerificaEffectSO : CardEffectSO
    {
        /// <inheritdoc/>
        public override void Apply(GameContext context)
        {
            PlayerState player = context.ResolvePlayer(Target) ?? context.InactivePlayer;
            context.RegisterChange(new BlockVerificaChange(player));
        }
    }
}
