# INDEX — Events

Comunicazione decoupled tra sistemi via EventBus tipizzato.
Torna all'[indice generale](../INDEX.md).

## File

| File | Tipo | Responsabilità / API chiave |
|---|---|---|
| [EventBus.cs](EventBus.cs) | `static EventBus` | Pub/sub statico tipizzato: `Subscribe<T>()`, `Unsubscribe<T>()`, `Publish<T>(msg)`, `Clear()`. `T` deve essere `struct` |
| [GameEvents.cs](GameEvents.cs) | struct evento (readonly) | Tutti i tipi evento di gioco |

## Eventi definiti (GameEvents.cs)

| Evento | Pubblicato quando | Payload |
|---|---|---|
| `CardResolvingEvent` | inizio risoluzione carta (prima degli effetti) | `Card`, `Player`, `Context` — usato dalle passive che dipendono dalla carta sorgente (Storia secondaria, Inglese secondaria copia carta) |
| `CardPlayedEvent` | una carta standard è risolta | `Card`, `Player` |
| `CardBoughtEvent` | acquisto shop | `Card`, `Player` |
| `VerificaPlayedEvent` | giocata la Verifica | `Player` |
| `TurnEndedEvent` | fine turno | `Player` |
| `RoundEndedEvent` | fine round (dopo DRAW) | `RoundIndex` |
| `PhaseChangedEvent` | transizione di fase | `Phase` |
| `NoteChangedEvent` | la Note di un comandante cambia | `Commander` |
| `NoteIncreasedEvent` | aumento istantaneo di Note (delta>0) | `Commander`, `Amount` — passive Inglese/Storia. Non ripubblicato per i bonus reattivi (anti-ricorsione) |
| `CardsDrawnEvent` | pesca in Fase PLAY (inizio turno o effetto) | `Player`, `Count` — passiva secondaria Matematica |
| `GameOverEvent` | Esame Finale risolto | `WinnerActorNumber` (`NoWinner = -1`), `IsDraw` |

## Note

- Disiscriversi **sempre** in `OnDestroy()` (vedi regole `CLAUDE.md`).
- L'evento di sincronizzazione UI `GameStateSyncedEvent` vive nel [Network](../Network/INDEX.md) layer, non qui.
