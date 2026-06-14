# INDEX — Config

Bilanciamento, costanti strutturali e archivio dei contenuti di partita.
Torna all'[indice generale](../INDEX.md).

## File

| File | Tipo | Responsabilità / API chiave |
|---|---|---|
| [GameConfigSO.cs](GameConfigSO.cs) | `GameConfigSO : ScriptableObject` + enum `TiebreakRule` | Valori di bilanciamento runtime: `MaxRounds`, `StartingHandSize`, `ShopPoolSize`, `ShopPurchasesPerRound`, `ShopRefreshSlots`, `NoteToCreditsMultiplier`, costi per tier `GetTierCost(tier)` (A/B/C), `GetCardsPlayablePerTurn(round)`, singleton `Instance` (via `RegisterAsActive()`) |
| [GameConstants.cs](GameConstants.cs) | `static GameConstants` | Costanti **non** configurabili: giocatori/comandanti, setup mano, indici slot/mazzo, round, conversione indice/conteggio ed ancoraggio UI centrale |
| [GameContentSO.cs](GameContentSO.cs) | `GameContentSO : ScriptableObject` | Archivio contenuti: `CommanderCatalog` (catalogo selezionabile con fallback sui comandanti default) + `GetCommanderByKind(kind)`, `FirstPlayerCommanders`/`SecondPlayerCommanders`, `VerificaCard`, `ShopCatalog`. Riferito dal `GameStateManager` in Inspector |

## Note

- **No magic number**: ogni valore numerico vive in `GameConfigSO` (runtime) o `GameConstants` (strutturale).
- Asset di esempio generati in `Assets/ScriptableObjects/Config/` (`GameConfig.asset`, `GameContent.asset`).
