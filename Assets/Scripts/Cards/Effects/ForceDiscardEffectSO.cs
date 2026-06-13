using UnityEngine;
using FourE.Core;
using FourE.Players;

namespace FourE.Cards.Effects
{
    /// <summary>
    /// Modalità di scarto forzato di un <see cref="ForceDiscardEffectSO"/>.
    /// </summary>
    public enum ForceDiscardMode
    {
        /// <summary>Scarta N carte casuali (Gossip).</summary>
        Random,

        /// <summary>Scarta tutte le carte con un tag (Politica, Bullismo).</summary>
        ByTag
    }

    /// <summary>
    /// Fa scartare carte dalla mano del giocatore bersaglio (di norma l'avversario).
    /// La Verifica vive in uno slot separato e non viene mai scartata.
    /// </summary>
    [CreateAssetMenu(fileName = "ForceDiscardEffect", menuName = "4E/Effects/Force Discard", order = 6)]
    public sealed class ForceDiscardEffectSO : CardEffectSO
    {
        [SerializeField] private ForceDiscardMode _mode = ForceDiscardMode.Random;
        [Tooltip("Tag scartato in modalità ByTag.")]
        [SerializeField] private CardTag _tag = CardTag.None;
        [Tooltip("Carte da scartare in modalità Random.")]
        [SerializeField] private int _count = 1;

        /// <inheritdoc/>
        public override void Apply(GameContext context)
        {
            // Bersaglio predefinito: l'avversario.
            PlayerState player = context.ResolvePlayer(Target) ?? context.InactivePlayer;

            if (_mode == ForceDiscardMode.ByTag)
            {
                context.RegisterChange(new ForceDiscardByTagChange(player, _tag));
            }
            else
            {
                context.RegisterChange(new ForceDiscardRandomChange(player, _count));
            }
        }
    }
}
