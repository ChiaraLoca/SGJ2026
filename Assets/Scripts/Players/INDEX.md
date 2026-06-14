# INDEX — Players

Stato runtime del giocatore.
Torna all'[indice generale](../INDEX.md).

## File

| File | Tipo | Responsabilità / API chiave |
|---|---|---|
| [PlayerState.cs](PlayerState.cs) | `PlayerState` (`[Serializable]`) | Stato di un giocatore: `ActorNumber`, `Credits`, `Hand`/`Deck`/`DiscardPile`/`ShopPool`, `Commanders[]`, `VerificaBlocked` (Sciopero), `WikipediaInterceptActive`, `VerificaPlayedCount` (+`IncrementVerificaPlayedCount()`, passiva base Storia), `TotalNotes`, `AddCredits()`, `SpendCredits()`, `LowestNoteCommander()` (usato da TurnManager, GameContext e CooperBuffEffectSO). Campi serializzati per ispezione runtime |

## Note

- `ConstitutionProtectionActive` conserva la protezione di Costituzione fino all'inizio del prossimo turno del proprietario.

- **Due valute distinte:** `TotalNotes` (Note, punteggio **temporaneo** dei comandanti, azzerato a ogni round, convertito in Crediti dalla Verifica) vs `Credits` (punteggio **permanente**, valuta dello [Shop](../Shop/INDEX.md) e criterio di vittoria).
- La **Verifica è una carta normale nel mazzo** (5+5+1), non più uno slot dedicato. Pescata e giocata come le altre, identificata da `CardDataSO.IsVerifica`.
- Costruito da `MatchSetup.BuildPlayer()` (aggiunge la Verifica al mazzo).
