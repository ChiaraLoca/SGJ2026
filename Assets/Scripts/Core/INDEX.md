# INDEX — Core

State machine della partita, turni, round, setup e contesto di risoluzione.
Torna all'[indice generale](../INDEX.md).

## File

| File | Tipo | Responsabilità / API chiave |
|---|---|---|
| [GamePhase.cs](GamePhase.cs) | enum `GamePhase` | Fasi: `Setup`, `Play`, `Verifica`, `Shop`, `Draw`, `FinalExam` |
| [GameStateManager.cs](GameStateManager.cs) | `GameStateManager : MonoBehaviour` (singleton) | Possiede `Player0`/`Player1`/`ActivePlayer`, riferimenti a config/content, costruisce i manager. `StartMatch()`, `BuildContext()`, `GetPlayerByActor()`, `OpponentOf()`. Auto-start offline opzionale |
| [PhaseManager.cs](PhaseManager.cs) | `PhaseManager` | State machine: `BeginMatch()`, `HandleVerifica()` (converte Note→Crediti per entrambi, poi SHOP), `FinishShop()` → Fase DRAW (reset Note, pesca), avanzamento round, Esame Finale (`ResolveOutcome()`) |
| [RoundManager.cs](RoundManager.cs) | `RoundManager` | Conteggio round: `CurrentRoundIndex`, `IsFinalExamReached`, `Advance()` (pubblica `RoundEndedEvent`) |
| [TurnManager.cs](TurnManager.cs) | `TurnManager` | Fase PLAY: `BeginRound()` (azzera contatore turni), `StartTurn()` (azzera immunità, incrementa `TurnInRound`), `TryPlayCard()`, `TryPlayVerifica()` (cerca la Verifica in mano; vietata nel 1° turno o se bloccata da Sciopero), `EndTurn()`, `GrantExtraActions()`, `DoubleRemainingActions()`, `CanPlayVerificaThisTurn` |
| [MatchSetup.cs](MatchSetup.cs) | `static MatchSetup` | `BuildPlayer()`: crea comandanti, mazzo mischiato, mano iniziale, Verifica, pool shop |
| [GameContext.cs](GameContext.cs) | `GameContext` + interfaccia `IGameChange` | Contesto di risoluzione: stato in lettura, `RegisterChange()`/`CommitChanges()`, `ResolveCommanders(target)`, `ResolvePlayer(target)`, `SelectedTargets`, `State` (seam verso `GameStateManager`) |
| [GameChanges.cs](GameChanges.cs) | `InstantNoteChange`, `AddActiveEffectChange`, `DrawCardsChange` | Implementazioni base `IGameChange` (Command): mutano lo stato al commit e pubblicano `NoteChangedEvent` |
| [GameChangesExtended.cs](GameChangesExtended.cs) | `GrantActionsChange`, `DoubleActionsChange`, `DrawToHandSizeChange`, `DrawAllChange`, `ForceDiscardRandomChange`, `ForceDiscardByTagChange`, `EqualizeNotesChange`, `SwapNotesChange`, `ReturnFromDiscardChange` (+enum `ReturnDestination`), `AddShieldChange`, `SetImmunityChange`, `BlockVerificaChange` | Modifiche avanzate per i nuovi effetti; helper interno `DeckOps.DrawTopToHand()` |
| [IStartingPlayerDecider.cs](IStartingPlayerDecider.cs) | interfaccia `IStartingPlayerDecider` | `DecideStartingPlayer(first, second)` — astratto per test deterministici |
| [CoinFlipStartingPlayerDecider.cs](CoinFlipStartingPlayerDecider.cs) | `CoinFlipStartingPlayerDecider` | Implementazione a lancio di moneta (RNG host-authoritative) |
| [CollectionUtils.cs](CollectionUtils.cs) | `static CollectionUtils` | `Shuffle<T>()` Fisher-Yates con RNG iniettato |

## Note

- Dipendenza circolare turni↔fasi rotta via `TurnManager.SetPhaseManager()` dopo la costruzione.
- L'host-authority è applicata a monte nel [Network](../Network/INDEX.md) layer; i manager qui assumono di essere già sull'host.
