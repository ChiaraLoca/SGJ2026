using System;
using UnityEngine;
using FourE.Config;
using FourE.Events;

namespace FourE.Core
{
    /// <summary>
    /// Tiene il conto dei round e segnala quando si raggiunge l'Esame Finale.
    /// </summary>
    [Serializable]
    public sealed class RoundManager
    {
        private readonly GameConfigSO _config;

        [SerializeField] private int _currentRoundIndex;

        /// <summary>Indice del round corrente, 0-based.</summary>
        public int CurrentRoundIndex => _currentRoundIndex;

        /// <summary>True quando tutti i round di Verifica sono stati giocati.</summary>
        public bool IsFinalExamReached => CurrentRoundIndex >= _config.MaxRounds;

        /// <summary>
        /// Crea il gestore dei round.
        /// </summary>
        /// <param name="config">Configurazione di gioco.</param>
        public RoundManager(GameConfigSO config)
        {
            _config = config;
        }

        /// <summary>
        /// Conclude il round corrente e avanza l'indice, pubblicando <see cref="RoundEndedEvent"/>.
        /// </summary>
        public void Advance()
        {
            EventBus.Publish(new RoundEndedEvent(CurrentRoundIndex));
            _currentRoundIndex++;
        }
    }
}
