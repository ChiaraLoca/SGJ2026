using FourE.Commanders;
using FourE.Core;
using FourE.Players;

namespace FourE.Network
{
    /// <summary>
    /// Costruisce un <see cref="GameStateDTO"/> a partire dallo stato vivo della partita.
    /// I campi di fine partita sono lasciati al valore di default e riempiti dal
    /// <see cref="NetworkGameManager"/>, che osserva l'esito sull'EventBus.
    /// </summary>
    public static class GameStateDtoBuilder
    {
        /// <summary>
        /// Produce uno snapshot completo dello stato corrente.
        /// </summary>
        /// <param name="state">Gestore dello stato di gioco.</param>
        /// <param name="registry">Registry per convertire le carte in id.</param>
        /// <returns>DTO con fase, round, giocatore attivo e stato dei due giocatori.</returns>
        public static GameStateDTO Build(GameStateManager state, CardRegistry registry)
        {
            return new GameStateDTO
            {
                Phase = (int)state.CurrentPhase,
                RoundIndex = state.CurrentRoundIndex,
                ActiveActorNumber = state.ActivePlayer?.ActorNumber ?? -1,
                Players = new[]
                {
                    BuildPlayer(state.Player0, registry),
                    BuildPlayer(state.Player1, registry)
                },
                WinnerActorNumber = -1,
                LastPlayedCardId = CardRegistry.NoCard,
                LastPlayedActorNumber = -1
            };
        }

        /// <summary>
        /// Costruisce lo snapshot di un singolo giocatore.
        /// </summary>
        /// <param name="player">Stato del giocatore.</param>
        /// <param name="registry">Registry per gli id carta.</param>
        /// <returns>DTO del giocatore.</returns>
        private static PlayerDTO BuildPlayer(PlayerState player, CardRegistry registry)
        {
            CommanderDTO[] commanders = new CommanderDTO[player.Commanders.Length];
            for (int i = 0; i < player.Commanders.Length; i++)
            {
                commanders[i] = BuildCommander(player.Commanders[i]);
            }

            return new PlayerDTO
            {
                ActorNumber = player.ActorNumber,
                Credits = player.Credits,
                Notes = player.TotalNotes,
                HandCardIds = registry.ToIds(player.Hand),
                DeckCount = player.Deck.Count,
                ShopPoolCardIds = registry.ToIds(player.ShopPool),
                Commanders = commanders
            };
        }

        /// <summary>
        /// Costruisce lo snapshot di un comandante.
        /// </summary>
        /// <param name="commander">Stato del comandante.</param>
        /// <returns>DTO del comandante.</returns>
        private static CommanderDTO BuildCommander(CommanderState commander)
        {
            return new CommanderDTO
            {
                BaseNote = commander.BaseNote,
                CurrentNote = commander.CurrentNote,
                HasDebuff = commander.HasActiveDebuff,
                ActiveBuffCount = commander.ActiveBuffs.Count,
                ActiveDebuffCount = commander.ActiveDebuffs.Count
            };
        }
    }
}
