namespace FourE.Cards
{
    /// <summary>
    /// Effetto con durata attivo su un comandante. Contribuisce alla Note corrente
    /// finché non scade (a turni) o viene rimosso al reset post-Verifica.
    /// </summary>
    public sealed class ActiveEffect
    {
        /// <summary>Nome della carta che ha generato l'effetto, per UI e log.</summary>
        public string SourceCardName { get; }

        /// <summary>Entità della modifica alla Note, sempre positiva.</summary>
        public int Magnitude { get; }

        /// <summary>Tipo di durata dell'effetto.</summary>
        public EffectDuration Duration { get; }

        /// <summary>Turni residui prima della rimozione (rilevante se Duration è Turns).</summary>
        public int RemainingTurns { get; private set; }

        /// <summary>
        /// Crea un nuovo effetto attivo.
        /// </summary>
        /// <param name="sourceCardName">Carta di origine dell'effetto.</param>
        /// <param name="magnitude">Entità positiva della modifica alla Note.</param>
        /// <param name="duration">Tipo di durata.</param>
        /// <param name="durationTurns">Turni iniziali, usati solo se la durata è a turni.</param>
        public ActiveEffect(string sourceCardName, int magnitude, EffectDuration duration, int durationTurns)
        {
            SourceCardName = sourceCardName;
            Magnitude = magnitude;
            Duration = duration;
            RemainingTurns = durationTurns;
        }

        /// <summary>
        /// Decrementa i turni residui di un effetto a durata limitata.
        /// Gli effetti istantanei o fino-a-Verifica non vengono toccati.
        /// </summary>
        /// <returns>True se l'effetto è scaduto e va rimosso.</returns>
        public bool TickTurn()
        {
            if (Duration != EffectDuration.Turns)
            {
                return false;
            }

            RemainingTurns--;
            return RemainingTurns <= 0;
        }
    }
}
