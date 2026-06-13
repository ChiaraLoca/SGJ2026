using System;
using UnityEngine;

namespace FourE.Cards
{
    /// <summary>
    /// Effetto con durata attivo su un comandante. Contribuisce alla Note corrente
    /// finché non scade (a turni) o viene rimosso al reset post-Verifica.
    /// </summary>
    [Serializable]
    public sealed class ActiveEffect
    {
        [SerializeField] private string _sourceCardName;
        [SerializeField] private int _magnitude;
        [SerializeField] private EffectDuration _duration;
        [SerializeField] private int _remainingTurns;

        /// <summary>Nome della carta che ha generato l'effetto, per UI e log.</summary>
        public string SourceCardName => _sourceCardName;

        /// <summary>Entità della modifica alla Note, sempre positiva.</summary>
        public int Magnitude => _magnitude;

        /// <summary>Tipo di durata dell'effetto.</summary>
        public EffectDuration Duration => _duration;

        /// <summary>Turni residui prima della rimozione (rilevante se Duration è Turns).</summary>
        public int RemainingTurns => _remainingTurns;

        /// <summary>
        /// Crea un nuovo effetto attivo.
        /// </summary>
        /// <param name="sourceCardName">Carta di origine dell'effetto.</param>
        /// <param name="magnitude">Entità positiva della modifica alla Note.</param>
        /// <param name="duration">Tipo di durata.</param>
        /// <param name="durationTurns">Turni iniziali, usati solo se la durata è a turni.</param>
        public ActiveEffect(string sourceCardName, int magnitude, EffectDuration duration, int durationTurns)
        {
            _sourceCardName = sourceCardName;
            _magnitude = magnitude;
            _duration = duration;
            _remainingTurns = durationTurns;
        }

        /// <summary>
        /// Decrementa i turni residui di un effetto a durata limitata.
        /// Gli effetti istantanei o fino-a-Verifica non vengono toccati.
        /// </summary>
        /// <returns>True se l'effetto è scaduto e va rimosso.</returns>
        public bool TickTurn()
        {
            if (_duration != EffectDuration.Turns)
            {
                return false;
            }

            _remainingTurns--;
            return _remainingTurns <= 0;
        }
    }
}
