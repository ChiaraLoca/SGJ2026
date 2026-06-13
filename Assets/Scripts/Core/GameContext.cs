using System.Collections.Generic;
using FourE.Cards;
using FourE.Commanders;
using FourE.Config;
using FourE.Players;

namespace FourE.Core
{
    /// <summary>
    /// Modifica atomica dello stato di gioco prodotta da un effetto carta.
    /// Applicata dal resolver dopo l'esecuzione dell'effetto (pattern Command).
    /// </summary>
    public interface IGameChange
    {
        /// <summary>Applica la modifica allo stato di gioco.</summary>
        void Apply();
    }

    /// <summary>
    /// Contesto passato a effetti e condizioni durante la risoluzione di una carta.
    /// Espone lo stato di gioco in lettura e raccoglie le modifiche da applicare al commit.
    /// </summary>
    public sealed class GameContext
    {
        private static readonly CommanderState[] EmptyTargets = new CommanderState[0];

        private readonly List<IGameChange> _pendingChanges = new();

        /// <summary>Giocatore che sta giocando la carta.</summary>
        public PlayerState ActivePlayer { get; }

        /// <summary>Giocatore avversario.</summary>
        public PlayerState InactivePlayer { get; }

        /// <summary>Indice del round corrente (0-based).</summary>
        public int CurrentRoundIndex { get; }

        /// <summary>Configurazione di gioco attiva.</summary>
        public GameConfigSO Config { get; }

        /// <summary>
        /// Stato di gioco autoritativo. Esposto agli effetti che devono agire oltre i
        /// comandanti (azioni di turno, manipolazione mazzo/scarti, flag inter-turno).
        /// Può essere null nei test che non costruiscono un GameStateManager.
        /// </summary>
        public GameStateManager State { get; }

        /// <summary>
        /// Comandanti scelti a runtime dal giocatore, usati dal bersaglio
        /// <see cref="EffectTarget.SelectedCommanders"/>. Vuoto se la carta non richiede scelta.
        /// </summary>
        public IReadOnlyList<CommanderState> SelectedTargets { get; }

        /// <summary>Modifiche registrate dagli effetti, in attesa di commit.</summary>
        public IReadOnlyList<IGameChange> PendingChanges => _pendingChanges;

        /// <summary>
        /// Affinità della carta in corso di risoluzione, impostata dal resolver.
        /// Usata dai bersagli <see cref="EffectTarget.AffinityCommander"/> e affini.
        /// </summary>
        public CardAffinity SourceAffinity { get; private set; } = CardAffinity.Neutral;

        /// <summary>
        /// Crea il contesto di risoluzione per una carta giocata.
        /// </summary>
        /// <param name="activePlayer">Giocatore attivo.</param>
        /// <param name="inactivePlayer">Giocatore avversario.</param>
        /// <param name="currentRoundIndex">Indice del round corrente.</param>
        /// <param name="config">Configurazione di gioco.</param>
        /// <param name="selectedTargets">Comandanti scelti a runtime, se la carta lo richiede.</param>
        /// <param name="state">Stato di gioco autoritativo, per gli effetti che ne hanno bisogno.</param>
        public GameContext(
            PlayerState activePlayer,
            PlayerState inactivePlayer,
            int currentRoundIndex,
            GameConfigSO config,
            IReadOnlyList<CommanderState> selectedTargets = null,
            GameStateManager state = null)
        {
            ActivePlayer = activePlayer;
            InactivePlayer = inactivePlayer;
            CurrentRoundIndex = currentRoundIndex;
            Config = config;
            SelectedTargets = selectedTargets ?? EmptyTargets;
            State = state;
        }

        /// <summary>
        /// Registra una modifica da applicare al commit. Gli effetti non mutano lo stato direttamente.
        /// </summary>
        /// <param name="change">Modifica da accodare.</param>
        public void RegisterChange(IGameChange change)
        {
            _pendingChanges.Add(change);
        }

        /// <summary>
        /// Imposta l'affinità della carta in risoluzione. Invocato dal resolver prima di applicare gli effetti.
        /// </summary>
        /// <param name="affinity">Affinità della carta giocata.</param>
        public void SetSourceAffinity(CardAffinity affinity)
        {
            SourceAffinity = affinity;
        }

        /// <summary>
        /// Applica in sequenza e svuota tutte le modifiche accodate.
        /// </summary>
        public void CommitChanges()
        {
            foreach (IGameChange change in _pendingChanges)
            {
                change.Apply();
            }

            _pendingChanges.Clear();
        }

        /// <summary>
        /// Risolve un bersaglio in uno o più comandanti.
        /// </summary>
        /// <param name="target">Bersaglio dell'effetto.</param>
        /// <returns>I comandanti colpiti; sequenza vuota se il bersaglio è un giocatore.</returns>
        public IEnumerable<CommanderState> ResolveCommanders(EffectTarget target)
        {
            switch (target)
            {
                case EffectTarget.OwnCommander0:
                    yield return ActivePlayer.Commanders[GameConstants.FirstCommanderIndex];
                    break;
                case EffectTarget.OwnCommander1:
                    yield return ActivePlayer.Commanders[GameConstants.SecondCommanderIndex];
                    break;
                case EffectTarget.EnemyCommander0:
                    yield return InactivePlayer.Commanders[GameConstants.FirstCommanderIndex];
                    break;
                case EffectTarget.EnemyCommander1:
                    yield return InactivePlayer.Commanders[GameConstants.SecondCommanderIndex];
                    break;
                case EffectTarget.AllOwnCommanders:
                    yield return ActivePlayer.Commanders[GameConstants.FirstCommanderIndex];
                    yield return ActivePlayer.Commanders[GameConstants.SecondCommanderIndex];
                    break;
                case EffectTarget.AllEnemyCommanders:
                    yield return InactivePlayer.Commanders[GameConstants.FirstCommanderIndex];
                    yield return InactivePlayer.Commanders[GameConstants.SecondCommanderIndex];
                    break;
                case EffectTarget.AllCommanders:
                    yield return ActivePlayer.Commanders[GameConstants.FirstCommanderIndex];
                    yield return ActivePlayer.Commanders[GameConstants.SecondCommanderIndex];
                    yield return InactivePlayer.Commanders[GameConstants.FirstCommanderIndex];
                    yield return InactivePlayer.Commanders[GameConstants.SecondCommanderIndex];
                    break;
                case EffectTarget.SelectedCommanders:
                    foreach (CommanderState selected in SelectedTargets)
                    {
                        yield return selected;
                    }

                    break;
                case EffectTarget.AffinityCommander:
                    yield return ActivePlayer.Commanders[AffinitySlot(SourceAffinity)];
                    break;
                case EffectTarget.AffinityOtherCommander:
                    yield return ActivePlayer.Commanders[1 - AffinitySlot(SourceAffinity)];
                    break;
            }
        }

        /// <summary>
        /// Converte un'affinità nello slot del comandante corrispondente (default slot 0).
        /// </summary>
        /// <param name="affinity">Affinità della carta.</param>
        /// <returns>Indice di slot del comandante (0 o 1).</returns>
        private static int AffinitySlot(CardAffinity affinity)
        {
            return affinity == CardAffinity.Commander1
                ? GameConstants.SecondCommanderIndex
                : GameConstants.FirstCommanderIndex;
        }

        /// <summary>
        /// Risolve un bersaglio in un giocatore (per effetti come la pesca).
        /// </summary>
        /// <param name="target">Bersaglio dell'effetto.</param>
        /// <returns>Il giocatore colpito, oppure null se il bersaglio è un comandante.</returns>
        public PlayerState ResolvePlayer(EffectTarget target)
        {
            return target switch
            {
                EffectTarget.ActivePlayer => ActivePlayer,
                EffectTarget.InactivePlayer => InactivePlayer,
                _ => null
            };
        }
    }
}
