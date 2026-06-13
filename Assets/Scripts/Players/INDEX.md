# INDEX — Players

Stato runtime del giocatore.
Torna all'[indice generale](../INDEX.md).

## File

| File | Tipo | Responsabilità / API chiave |
|---|---|---|
| [PlayerState.cs](PlayerState.cs) | `PlayerState` | Stato di un giocatore: `ActorNumber`, `Credits`, `Hand`/`Deck`/`DiscardPile`/`ShopPool`, `Commanders[]`, `VerificaCard`, `TotalNotes`, `AvailableNotes` (totale − spese), `AddCredits()`, `SpendNotes()`, `ResetSpentNotes()` |

## Note

- `AvailableNotes` è la valuta usata sia per lo [Shop](../Shop/INDEX.md) sia per la conversione in Credits ([Core/PhaseManager](../Core/INDEX.md)).
- Costruito da `MatchSetup.BuildPlayer()`.
