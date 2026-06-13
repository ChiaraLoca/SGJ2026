using UnityEngine;
using FourE.Core;

namespace FourE.Cards
{
    /// <summary>
    /// Base astratta di tutti gli effetti carta. Ogni effetto concreto è uno ScriptableObject
    /// separato che implementa <see cref="Apply"/> in modo puro sul <see cref="GameContext"/>.
    /// </summary>
    public abstract class CardEffectSO : ScriptableObject
    {
        [SerializeField] private EffectTarget _target;

        /// <summary>Bersaglio su cui l'effetto agisce.</summary>
        public EffectTarget Target => _target;

        /// <summary>
        /// Applica l'effetto leggendo dal contesto e registrando le modifiche tramite
        /// <see cref="GameContext.RegisterChange"/>. Non muta direttamente lo stato.
        /// </summary>
        /// <param name="context">Contesto contenente lo stato completo del gioco.</param>
        public abstract void Apply(GameContext context);
    }
}
