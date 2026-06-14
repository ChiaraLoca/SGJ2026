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
    /// Contratto per una modifica carta che concede Note positive a un comandante.
    /// Permette a Costituzione di annullarla prima del commit.
    /// </summary>
    public interface IPositiveCommanderCardChange
    {
        /// <summary>Comandante che riceverebbe il beneficio.</summary>
        CommanderState BeneficiaryCommander { get; }

        /// <summary>True se la modifica concede effettivamente Note positive.</summary>
        bool IsPositiveBenefit { get; }
    }

    /// <summary>
    /// Contratto per una modifica carta che concede carte a un giocatore.
    /// Permette a Costituzione di annullarla prima del commit.
    /// </summary>
    public interface IPositivePlayerCardChange
    {
        /// <summary>Giocatore che riceverebbe le carte.</summary>
        PlayerState BeneficiaryPlayer { get; }
    }

    /// <summary>
    /// Contesto passato a effetti e condizioni durante la risoluzione di una carta.
    /// Espone lo stato di gioco in lettura e raccoglie le modifiche da applicare al commit.
    /// </summary>
    public sealed class GameContext
    {
        private static readonly CommanderState[] EmptyTargets = new CommanderState[0];
        private static readonly IReadOnlyList<CommanderState> EmptyList = new CommanderState[0];

        private readonly List<IGameChange> _pendingChanges = new();

        // Redirect usato dalla secondaria di Inglese: ogni bersaglio uguale a _redirectFrom
        // viene sostituito con _redirectTo durante ResolveCommanders.
        private CommanderState _redirectFrom;
        private CommanderState _redirectTo;

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
        /// Tutti i comandanti scelti a runtime dal giocatore (lista completa, ordinata).
        /// Vuoto se la carta non richiede scelta.
        /// </summary>
        public IReadOnlyList<CommanderState> SelectedTargets { get; }

        /// <summary>
        /// Sottoinsieme di <see cref="SelectedTargets"/> appartenenti al giocatore avversario.
        /// Usato da <see cref="EffectTarget.SelectedEnemyCommanders"/> e <see cref="EffectTarget.SelectedOwnAndEnemy"/>.
        /// </summary>
        public IReadOnlyList<CommanderState> SelectedEnemyTargets { get; }

        /// <summary>
        /// Sottoinsieme di <see cref="SelectedTargets"/> appartenenti al giocatore attivo.
        /// Usato da <see cref="EffectTarget.SelectedOwnCommanders"/> e <see cref="EffectTarget.SelectedOwnAndEnemy"/>.
        /// </summary>
        public IReadOnlyList<CommanderState> SelectedOwnTargets { get; }

        /// <summary>Modifiche registrate dagli effetti, in attesa di commit.</summary>
        public IReadOnlyList<IGameChange> PendingChanges => _pendingChanges;

        /// <summary>
        /// Affinità della carta in corso di risoluzione, impostata dal resolver.
        /// Usata dai bersagli <see cref="EffectTarget.AffinityCommander"/> e affini.
        /// </summary>
        public CardAffinity SourceAffinity { get; private set; } = CardAffinity.Neutral;

        /// <summary>Carta in corso di risoluzione, impostata dal resolver.</summary>
        public CardDataSO Card { get; set; }

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
            Card = null;

            // Divide i bersagli selezionati per lato (proprio vs avversario).
            if (SelectedTargets.Count == 0)
            {
                SelectedOwnTargets = EmptyList;
                SelectedEnemyTargets = EmptyList;
            }
            else
            {
                List<CommanderState> own = new();
                List<CommanderState> enemy = new();
                foreach (CommanderState c in SelectedTargets)
                {
                    if (IsCommanderOwned(c, activePlayer))
                        own.Add(c);
                    else
                        enemy.Add(c);
                }
                SelectedOwnTargets = own;
                SelectedEnemyTargets = enemy;
            }
        }

        /// <summary>
        /// Verifica se un comandante appartiene al giocatore indicato.
        /// </summary>
        private static bool IsCommanderOwned(CommanderState commander, PlayerState player)
        {
            foreach (CommanderState c in player.Commanders)
            {
                if (c == commander) return true;
            }
            return false;
        }

        /// <summary>
        /// Registra una modifica da applicare al commit. Gli effetti non mutano lo stato direttamente.
        /// </summary>
        /// <param name="change">Modifica da accodare.</param>
        public void RegisterChange(IGameChange change)
        {
            if (ShouldSuppressForConstitution(change))
            {
                return;
            }

            _pendingChanges.Add(change);
        }

        /// <summary>
        /// Verifica se Costituzione annulla il beneficio generato dalla carta avversaria.
        /// </summary>
        /// <param name="change">Modifica proposta dall'effetto.</param>
        /// <returns>True se la modifica non deve essere registrata.</returns>
        private bool ShouldSuppressForConstitution(IGameChange change)
        {
            if (InactivePlayer?.ConstitutionProtectionActive != true)
            {
                return false;
            }

            if (change is IPositiveCommanderCardChange commanderChange
                && commanderChange.IsPositiveBenefit
                && IsCommanderOwned(commanderChange.BeneficiaryCommander, InactivePlayer))
            {
                return true;
            }

            return change is IPositivePlayerCardChange playerChange
                   && playerChange.BeneficiaryPlayer == InactivePlayer;
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
        /// Imposta un redirect di comandante: ogni volta che <see cref="ResolveCommanders"/> restituirebbe
        /// <paramref name="from"/>, restituisce invece <paramref name="to"/>.
        /// Usato dalla secondaria di Inglese per copiare la carta sull'altro comandante.
        /// </summary>
        /// <param name="from">Comandante da sostituire.</param>
        /// <param name="to">Comandante sostituto.</param>
        public void SetCommanderRedirect(CommanderState from, CommanderState to)
        {
            _redirectFrom = from;
            _redirectTo = to;
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
        /// Risolve un bersaglio in uno o più comandanti, applicando l'eventuale redirect impostato
        /// da <see cref="SetCommanderRedirect"/> (usato dalla secondaria di Inglese).
        /// </summary>
        /// <param name="target">Bersaglio dell'effetto.</param>
        /// <returns>I comandanti colpiti; sequenza vuota se il bersaglio è un giocatore.</returns>
        public IEnumerable<CommanderState> ResolveCommanders(EffectTarget target)
        {
            foreach (CommanderState resolved in ResolveCommandersCore(target))
            {
                yield return (_redirectFrom != null && resolved == _redirectFrom) ? _redirectTo : resolved;
            }
        }

        /// <summary>
        /// Risoluzione interna senza redirect: usata da <see cref="ResolveCommanders"/>.
        /// </summary>
        private IEnumerable<CommanderState> ResolveCommandersCore(EffectTarget target)
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
                case EffectTarget.SelectedEnemyCommanders:
                    foreach (CommanderState selected in SelectedEnemyTargets)
                    {
                        yield return selected;
                    }

                    break;
                case EffectTarget.SelectedOwnCommanders:
                    foreach (CommanderState selected in SelectedOwnTargets)
                    {
                        yield return selected;
                    }

                    break;
                case EffectTarget.SelectedOwnAndEnemy:
                    // Gestito direttamente dall'effetto concreto (es. SwapNotesEffectSO).
                    break;
                case EffectTarget.SelectedFirstCommander:
                    if (SelectedTargets.Count > 0)
                    {
                        yield return SelectedTargets[0];
                    }
                    break;
                case EffectTarget.SelectedSecondCommander:
                    if (SelectedTargets.Count > 1)
                    {
                        yield return SelectedTargets[1];
                    }
                    break;
                case EffectTarget.OwnLowestNoteCommander:
                    yield return ActivePlayer.LowestNoteCommander();
                    break;
                case EffectTarget.AffinityCommander:
                    yield return ActivePlayer.Commanders[AffinitySlot()];
                    break;
                case EffectTarget.AffinityOtherCommander:
                    yield return ActivePlayer.Commanders[1 - AffinitySlot()];
                    break;
            }
        }

        /// <summary>
        /// Converte un'affinità nello slot del comandante corrispondente (default slot 0).
        /// </summary>
        /// <param name="affinity">Affinità della carta.</param>
        /// <returns>Indice di slot del comandante (0 o 1).</returns>
        private int AffinitySlot()
        {
            if (Card != null && ActivePlayer?.Commanders != null)
            {
                for (int i = 0; i < ActivePlayer.Commanders.Length; i++)
                {
                    CommanderDataSO data = ActivePlayer.Commanders[i]?.Data;
                    if (data != null && ContainsCard(data.LinkedCards, Card))
                    {
                        return i;
                    }
                }
            }

            return SourceAffinity == CardAffinity.Commander1
                ? GameConstants.SecondCommanderIndex
                : GameConstants.FirstCommanderIndex;
        }

        /// <summary>Verifica se una raccolta contiene la carta indicata.</summary>
        private static bool ContainsCard(IReadOnlyList<CardDataSO> cards, CardDataSO card)
        {
            foreach (CardDataSO candidate in cards)
            {
                if (candidate == card)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Risolve i bersagli di un debuff e include l'altro comandante avversario
        /// quando la secondaria di Arte è sbloccata.
        /// </summary>
        /// <param name="target">Bersaglio configurato dall'effetto.</param>
        /// <returns>Comandanti da colpire senza duplicati.</returns>
        public IEnumerable<CommanderState> ResolveDebuffCommanders(EffectTarget target)
        {
            HashSet<CommanderState> resolved = new();
            bool spreadToOther = HasUnlockedArteCommander(ActivePlayer);

            foreach (CommanderState commander in ResolveCommanders(target))
            {
                if (commander != null && resolved.Add(commander))
                {
                    yield return commander;
                }

                if (!spreadToOther || !IsCommanderOwned(commander, InactivePlayer))
                {
                    continue;
                }

                foreach (CommanderState enemyCommander in InactivePlayer.Commanders)
                {
                    if (enemyCommander != null && resolved.Add(enemyCommander))
                    {
                        yield return enemyCommander;
                    }
                }
            }
        }

        /// <summary>Verifica se il giocatore possiede Arte con secondaria sbloccata.</summary>
        private static bool HasUnlockedArteCommander(PlayerState player)
        {
            if (player?.Commanders == null)
            {
                return false;
            }

            foreach (CommanderState commander in player.Commanders)
            {
                if (commander?.Data?.Kind == CommanderKind.Arte && commander.SecondaryUnlocked)
                {
                    return true;
                }
            }

            return false;
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
