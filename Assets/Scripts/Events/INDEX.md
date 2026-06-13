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
| `CardPlayedEvent` | una carta standard è risolta | `Card`, `Player` |
| `CardBoughtEvent` | acquisto shop | `Card`, `Player` |
| `VerificaPlayedEvent` | giocata la Verifica | `Player` |
| `TurnEndedEvent` | fine turno | `Player` |
| `RoundEndedEvent` | fine round (dopo DRAW) | `RoundIndex` |
| `PhaseChangedEvent` | transizione di fase | `Phase` |
| `NoteChangedEvent` | la Note di un comandante cambia | `Commander` |
| `GameOverEvent` | Esame Finale risolto | `WinnerActorNumber` (`NoWinner = -1`), `IsDraw` |

## Note

- Disiscriversi **sempre** in `OnDestroy()` (vedi regole `CLAUDE.md`).
- L'evento di sincronizzazione UI `GameStateSyncedEvent` vive nel [Network](../Network/INDEX.md) layer, non qui.
