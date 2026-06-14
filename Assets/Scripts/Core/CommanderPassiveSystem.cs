using System;
using FourE.Cards;
using FourE.Commanders;
using FourE.Events;
using FourE.Players;

namespace FourE.Core
{
    /// <summary>
    /// Applica le passive dei comandanti (base e secondaria) descritte in CARDS.md.
    /// Le passive proattive (inizio round, sblocco a inizio turno) sono invocate esplicitamente
    /// dai manager di flusso; quelle reattive (aumenti di Nota, pesche, azioni, fine turno)
    /// reagiscono agli eventi dell'EventBus. Vive solo sull'host, dove gira la logica autoritativa.
    /// </summary>
    public sealed class CommanderPassiveSystem : IDisposable
    {
        private readonly GameStateManager _state;
        private readonly EffectResolver _resolver;

        // Carta e contesto in corso di risoluzione, usati dalla secondaria di Inglese (copia carta) e di Storia.
        private CardDataSO _resolvingCard;
        private GameContext _resolvingContext;

        // Guardia di rientranza: i bonus reattivi mutano la Nota senza ripubblicare NoteIncreasedEvent,
        // ma la guardia protegge da eventuali cascate future.
        private bool _applyingReactiveBonus;

        /// <summary>
        /// Crea il sistema e lo iscrive agli eventi reattivi.
        /// </summary>
        /// <param name="state">Stato di gioco autoritativo.</param>
        /// <param name="resolver">Resolver degli effetti carta, usato dalla secondaria di Inglese.</param>
        public CommanderPassiveSystem(GameStateManager state, EffectResolver resolver)
        {
            _state = state;
            _resolver = resolver;
            EventBus.Subscribe<CardResolvingEvent>(OnCardResolving);
            EventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Subscribe<NoteIncreasedEvent>(OnNoteIncreased);
            EventBus.Subscribe<CardsDrawnEvent>(OnCardsDrawn);
            EventBus.Subscribe<TurnEndedEvent>(OnTurnEnded);
        }

        /// <summary>
        /// Disiscrive il sistema dagli eventi. Da chiamare al teardown della partita.
        /// </summary>
        public void Dispose()
        {
            EventBus.Unsubscribe<CardResolvingEvent>(OnCardResolving);
            EventBus.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Unsubscribe<NoteIncreasedEvent>(OnNoteIncreased);
            EventBus.Unsubscribe<CardsDrawnEvent>(OnCardsDrawn);
            EventBus.Unsubscribe<TurnEndedEvent>(OnTurnEnded);
        }

        // =====================================================================
        // Passive proattive (chiamate dai manager di flusso)
        // =====================================================================

        /// <summary>
        /// Applica le passive di inizio round al giocatore indicato: Note di Storia
        /// (per le Verifiche giocate) e pesca aggiuntiva di Matematica.
        /// Da chiamare per entrambi i giocatori all'avvio della Fase PLAY di ogni round.
        /// </summary>
        /// <param name="player">Giocatore a cui applicare le passive.</param>
        public void ApplyRoundStartPassives(PlayerState player)
        {
            if (player?.Commanders == null)
            {
                return;
            }

            foreach (CommanderState commander in player.Commanders)
            {
                if (commander?.Data == null)
                {
                    continue;
                }

                switch (commander.Data.Kind)
                {
                    case CommanderKind.Storia:
                        int notes = player.VerificaPlayedCount * CommanderPassiveConstants.StoriaNotePerVerifica;
                        ApplyNoteDirect(commander, notes);
                        break;
                    case CommanderKind.Matematica:
                        DeckOps.DrawTopToHand(player, CommanderPassiveConstants.MateExtraCardsPerRound, publishEvent: false);
                        break;
                }
            }
        }

        /// <summary>
        /// Verifica le condizioni di sblocco della secondaria per i comandanti del giocatore.
        /// Lo sblocco è permanente. Da chiamare all'inizio di ogni turno del giocatore.
        /// </summary>
        /// <param name="player">Giocatore di cui controllare i comandanti.</param>
        /// <param name="roundIndex">Indice del round corrente (0-based).</param>
        public void CheckSecondaryUnlocks(PlayerState player, int roundIndex)
        {
            if (player?.Commanders == null)
            {
                return;
            }

            foreach (CommanderState commander in player.Commanders)
            {
                if (commander == null || commander.SecondaryUnlocked || commander.Data == null)
                {
                    continue;
                }

                if (IsUnlockConditionMet(commander, player, roundIndex))
                {
                    commander.MarkSecondaryUnlocked();
                }
            }
        }

        /// <summary>
        /// Valuta la condizione di sblocco della secondaria di un comandante.
        /// </summary>
        /// <param name="commander">Comandante da valutare.</param>
        /// <param name="owner">Giocatore proprietario.</param>
        /// <param name="roundIndex">Indice del round corrente.</param>
        /// <returns>True se la condizione è soddisfatta.</returns>
        private static bool IsUnlockConditionMet(CommanderState commander, PlayerState owner, int roundIndex)
        {
            return commander.Data.Kind switch
            {
                CommanderKind.Storia => owner.Credits >= CommanderPassiveConstants.StoriaSecondaryUnlockCredits,
                CommanderKind.Matematica => roundIndex >= CommanderPassiveConstants.MateSecondaryUnlockRoundIndex,
                CommanderKind.Inglese => owner.Deck.Count >= CommanderPassiveConstants.IngleseSecondaryUnlockDeckSize,
                CommanderKind.EducazioneFisica => commander.CurrentNote <= CommanderPassiveConstants.EduFisicaSecondaryUnlockNote,
                _ => false
            };
        }

        // =====================================================================
        // Passive reattive (handler eventi)
        // =====================================================================

        /// <summary>
        /// Memorizza carta e contesto in risoluzione per le passive che dipendono dalla carta sorgente.
        /// </summary>
        /// <param name="evt">Evento di carta in risoluzione.</param>
        private void OnCardResolving(CardResolvingEvent evt)
        {
            _resolvingCard = evt.Card;
            _resolvingContext = evt.Context;
        }

        /// <summary>
        /// Reagisce a un aumento di Nota: passive di Inglese (propagazione/copia all'altro comandante)
        /// e secondaria di Storia (raddoppio delle carte Studio giocate su di lui).
        /// </summary>
        /// <param name="evt">Evento di aumento Note.</param>
        private void OnNoteIncreased(NoteIncreasedEvent evt)
        {
            if (_applyingReactiveBonus)
            {
                return;
            }

            CommanderState commander = evt.Commander;
            PlayerState owner = OwnerOf(commander);
            if (owner == null || commander.Data == null)
            {
                return;
            }

            _applyingReactiveBonus = true;
            try
            {
                switch (commander.Data.Kind)
                {
                    case CommanderKind.Inglese:
                        // Base: sempre +1 all'altro comandante.
                        ApplyNoteDirect(OtherCommanderOf(owner, commander), CommanderPassiveConstants.IngleseBaseBonusToOther);

                        // Secondaria (aggiuntiva alla base): copia l'intera carta sull'altro comandante.
                        if (commander.SecondaryUnlocked
                            && _resolvingCard != null
                            && _resolvingContext != null)
                        {
                            CommanderState other = OtherCommanderOf(owner, commander);
                            if (other != null)
                            {
                                GameContext mirror = new GameContext(
                                    _resolvingContext.ActivePlayer,
                                    _resolvingContext.InactivePlayer,
                                    _resolvingContext.CurrentRoundIndex,
                                    _resolvingContext.Config,
                                    _resolvingContext.SelectedTargets,
                                    _resolvingContext.State);
                                mirror.SetCommanderRedirect(commander, other);
                                _resolver.Resolve(_resolvingCard, mirror);
                            }
                        }
                        break;
                    case CommanderKind.Storia:
                        // Secondaria: raddoppia l'effetto delle carte con tag Studio giocate su di lui.
                        if (commander.SecondaryUnlocked
                            && _resolvingCard != null
                            && _resolvingCard.HasTag(CardTag.Studio))
                        {
                            ApplyNoteDirect(commander, evt.Amount);
                        }
                        break;
                }
            }
            finally
            {
                _applyingReactiveBonus = false;
            }
        }

        /// <summary>
        /// Reagisce a una pesca: secondaria di Matematica (+1 Nota per carta pescata).
        /// </summary>
        /// <param name="evt">Evento di pesca.</param>
        private void OnCardsDrawn(CardsDrawnEvent evt)
        {
            PlayerState player = evt.Player;
            if (player?.Commanders == null)
            {
                return;
            }

            foreach (CommanderState commander in player.Commanders)
            {
                if (commander?.Data != null
                    && commander.Data.Kind == CommanderKind.Matematica
                    && commander.SecondaryUnlocked)
                {
                    ApplyNoteDirect(commander, evt.Count * CommanderPassiveConstants.MateNotePerDraw);
                }
            }
        }

        /// <summary>
        /// Reagisce al gioco di una carta: secondaria di Educazione Fisica
        /// (−1 Nota al comandante avversario con più Note). Azzera la carta in risoluzione.
        /// </summary>
        /// <param name="evt">Evento di carta giocata.</param>
        private void OnCardPlayed(CardPlayedEvent evt)
        {
            _resolvingCard = null;
            _resolvingContext = null;

            PlayerState player = evt.Player;
            PlayerState opponent = _state.OpponentOf(player);
            if (player?.Commanders == null || opponent == null)
            {
                return;
            }

            foreach (CommanderState commander in player.Commanders)
            {
                if (commander?.Data != null
                    && commander.Data.Kind == CommanderKind.EducazioneFisica
                    && commander.SecondaryUnlocked)
                {
                    ApplyNoteDirect(HighestNoteCommander(opponent), -CommanderPassiveConstants.EduFisicaActionPenalty);
                }
            }
        }

        /// <summary>
        /// Reagisce alla fine del turno: passiva base di Educazione Fisica
        /// (+1 Nota all'altro comandante se hai meno carte in mano dell'avversario).
        /// </summary>
        /// <param name="evt">Evento di fine turno.</param>
        private void OnTurnEnded(TurnEndedEvent evt)
        {
            PlayerState player = evt.Player;
            PlayerState opponent = _state.OpponentOf(player);
            if (player?.Commanders == null || opponent == null)
            {
                return;
            }

            // Condizione: meno carte in mano dell'avversario.
            if (player.Hand.Count >= opponent.Hand.Count)
            {
                return;
            }

            foreach (CommanderState commander in player.Commanders)
            {
                if (commander?.Data != null && commander.Data.Kind == CommanderKind.EducazioneFisica)
                {
                    ApplyNoteDirect(OtherCommanderOf(player, commander), CommanderPassiveConstants.EduFisicaTurnEndBonus);
                }
            }
        }

        // =====================================================================
        // Helper
        // =====================================================================

        /// <summary>
        /// Applica una variazione di Nota a un comandante senza ripubblicare <see cref="NoteIncreasedEvent"/>,
        /// per evitare che i bonus reattivi inneschino altre passive.
        /// </summary>
        /// <param name="commander">Comandante bersaglio.</param>
        /// <param name="amount">Variazione con segno.</param>
        private static void ApplyNoteDirect(CommanderState commander, int amount)
        {
            if (commander == null || amount == 0)
            {
                return;
            }

            commander.ApplyInstantDelta(amount);
            EventBus.Publish(new NoteChangedEvent(commander));
        }

        /// <summary>
        /// Restituisce il giocatore che possiede il comandante indicato, o null se non trovato.
        /// </summary>
        /// <param name="commander">Comandante di cui cercare il proprietario.</param>
        /// <returns>Il proprietario, o null.</returns>
        private PlayerState OwnerOf(CommanderState commander)
        {
            if (Owns(_state.Player0, commander))
            {
                return _state.Player0;
            }

            return Owns(_state.Player1, commander) ? _state.Player1 : null;
        }

        /// <summary>
        /// Verifica se un giocatore possiede il comandante indicato.
        /// </summary>
        private static bool Owns(PlayerState player, CommanderState commander)
        {
            if (player?.Commanders == null)
            {
                return false;
            }

            foreach (CommanderState c in player.Commanders)
            {
                if (c == commander)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Restituisce l'altro comandante del giocatore rispetto a quello indicato.
        /// </summary>
        /// <param name="player">Proprietario.</param>
        /// <param name="commander">Comandante di riferimento.</param>
        /// <returns>L'altro comandante, o null.</returns>
        private static CommanderState OtherCommanderOf(PlayerState player, CommanderState commander)
        {
            CommanderState[] commanders = player?.Commanders;
            if (commanders == null || commanders.Length < 2)
            {
                return null;
            }

            return commanders[0] == commander ? commanders[1] : commanders[0];
        }

        /// <summary>
        /// Restituisce il comandante del giocatore con la Nota corrente più alta.
        /// In caso di parità restituisce il primo comandante (slot 0).
        /// </summary>
        /// <param name="player">Giocatore di cui cercare il comandante.</param>
        /// <returns>Il comandante con più Note.</returns>
        private static CommanderState HighestNoteCommander(PlayerState player)
        {
            CommanderState[] commanders = player.Commanders;
            CommanderState highest = commanders[0];
            foreach (CommanderState c in commanders)
            {
                if (c.CurrentNote > highest.CurrentNote)
                {
                    highest = c;
                }
            }

            return highest;
        }
    }
}
