using FourE.Cards;
using FourE.Commanders;
using FourE.Core;
using FourE.Players;

namespace FourE.Events
{
    /// <summary>Pubblicato dopo che una carta standard è stata risolta.</summary>
    public readonly struct CardPlayedEvent
    {
        /// <summary>Carta giocata.</summary>
        public CardDataSO Card { get; }

        /// <summary>Giocatore che ha giocato la carta.</summary>
        public PlayerState Player { get; }

        /// <summary>Crea l'evento di carta giocata.</summary>
        /// <param name="card">Carta giocata.</param>
        /// <param name="player">Giocatore attivo.</param>
        public CardPlayedEvent(CardDataSO card, PlayerState player)
        {
            Card = card;
            Player = player;
        }
    }

    /// <summary>Pubblicato dopo l'acquisto di una carta dallo shop.</summary>
    public readonly struct CardBoughtEvent
    {
        /// <summary>Carta acquistata.</summary>
        public CardDataSO Card { get; }

        /// <summary>Giocatore acquirente.</summary>
        public PlayerState Player { get; }

        /// <summary>Crea l'evento di acquisto.</summary>
        /// <param name="card">Carta acquistata.</param>
        /// <param name="player">Giocatore acquirente.</param>
        public CardBoughtEvent(CardDataSO card, PlayerState player)
        {
            Card = card;
            Player = player;
        }
    }

    /// <summary>Pubblicato quando un giocatore gioca la carta Verifica, chiudendo la Fase PLAY.</summary>
    public readonly struct VerificaPlayedEvent
    {
        /// <summary>Giocatore che ha chiuso il round.</summary>
        public PlayerState Player { get; }

        /// <summary>Crea l'evento di Verifica giocata.</summary>
        /// <param name="player">Giocatore che ha giocato la Verifica.</param>
        public VerificaPlayedEvent(PlayerState player)
        {
            Player = player;
        }
    }

    /// <summary>Pubblicato al termine del turno di un giocatore.</summary>
    public readonly struct TurnEndedEvent
    {
        /// <summary>Giocatore il cui turno è terminato.</summary>
        public PlayerState Player { get; }

        /// <summary>Crea l'evento di fine turno.</summary>
        /// <param name="player">Giocatore di cui termina il turno.</param>
        public TurnEndedEvent(PlayerState player)
        {
            Player = player;
        }
    }

    /// <summary>Pubblicato alla fine di un round, dopo la Fase DRAW.</summary>
    public readonly struct RoundEndedEvent
    {
        /// <summary>Indice del round appena concluso (0-based).</summary>
        public int RoundIndex { get; }

        /// <summary>Crea l'evento di fine round.</summary>
        /// <param name="roundIndex">Indice del round concluso.</param>
        public RoundEndedEvent(int roundIndex)
        {
            RoundIndex = roundIndex;
        }
    }

    /// <summary>Pubblicato a ogni transizione di fase della state machine.</summary>
    public readonly struct PhaseChangedEvent
    {
        /// <summary>Nuova fase entrante.</summary>
        public GamePhase Phase { get; }

        /// <summary>Crea l'evento di cambio fase.</summary>
        /// <param name="phase">Fase entrante.</param>
        public PhaseChangedEvent(GamePhase phase)
        {
            Phase = phase;
        }
    }

    /// <summary>Pubblicato quando la Note corrente di un comandante cambia.</summary>
    public readonly struct NoteChangedEvent
    {
        /// <summary>Comandante la cui Note è cambiata.</summary>
        public CommanderState Commander { get; }

        /// <summary>Crea l'evento di variazione Note.</summary>
        /// <param name="commander">Comandante interessato.</param>
        public NoteChangedEvent(CommanderState commander)
        {
            Commander = commander;
        }
    }

    /// <summary>Pubblicato all'Esame Finale, con l'esito della partita.</summary>
    public readonly struct GameOverEvent
    {
        /// <summary>Valore di attore usato quando la partita finisce in pareggio.</summary>
        public const int NoWinner = -1;

        /// <summary>Numero attore Photon del vincitore, o <see cref="NoWinner"/> in pareggio.</summary>
        public int WinnerActorNumber { get; }

        /// <summary>True se la partita è terminata in pareggio.</summary>
        public bool IsDraw { get; }

        /// <summary>Crea l'evento di fine partita.</summary>
        /// <param name="winnerActorNumber">Attore vincente, o <see cref="NoWinner"/> in pareggio.</param>
        /// <param name="isDraw">True se è un pareggio.</param>
        public GameOverEvent(int winnerActorNumber, bool isDraw)
        {
            WinnerActorNumber = winnerActorNumber;
            IsDraw = isDraw;
        }
    }
}
