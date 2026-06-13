# INDEX — Core

State machine della partita, turni, round, setup e contesto di risoluzione.
Torna all'[indice generale](../INDEX.md).

## File

| File | Tipo | Responsabilità / API chiave |
|---|---|---|
| [GamePhase.cs](GamePhase.cs) | enum `GamePhase` | Fasi: `Setup`, `Play`, `Verifica`, `Shop`, `Draw`, `FinalExam` |
| [GameStateManager.cs](GameStateManager.cs) | `GameStateManager : MonoBehaviour` (singleton) | Possiede `Player0`/`Player1`/`ActivePlayer`, riferimenti a config/content, costruisce i manager (incl. `Passives`). `SetCommanderSelections(p0,p1)` (prima di `StartMatch`), `StartMatch()`, `BuildContext()`, `GetPlayerByActor()`, `OpponentOf()`. Auto-start offline opzionale |
| [PhaseManager.cs](PhaseManager.cs) | `PhaseManager` | State machine: `BeginMatch()`, `HandleVerifica()` converte Note→Crediti e apre lo Shop salvo nell'ultimo round, `FinishShop()` → Draw, avanzamento round, Esame Finale (`ResolveOutcome()`) |
| [RoundManager.cs](RoundManager.cs) | `RoundManager` | Conteggio round: `CurrentRoundIndex`, `IsLastPlayableRound`, `IsFinalExamReached`, `Advance()` (pubblica `RoundEndedEvent`) |
| [TurnManager.cs](TurnManager.cs) | `TurnManager` | Fase PLAY: contatori `CardsPlayedThisTurn`, `CardsAllowedThisTurn`, `RemainingActions`; avvio/fine turno, gioco carte e Verifica, bonus/raddoppio azioni |
| [MatchSetup.cs](MatchSetup.cs) | `static MatchSetup` | `BuildPlayer()`: crea comandanti, mazzo mischiato, mano iniziale, Verifica, pool shop |
| [GameContext.cs](GameContext.cs) | `GameContext` + interfaccia `IGameChange` | Contesto di risoluzione: stato in lettura, `RegisterChange()`/`CommitChanges()`, `ResolveCommanders(target)`, `ResolvePlayer(target)`, `SelectedTargets`, `Card`, `State` (seam verso `GameStateManager`) |
| [CommanderPassiveSystem.cs](CommanderPassiveSystem.cs) | `CommanderPassiveSystem : IDisposable` | Passive comandanti (CARDS.md). Proattive: `ApplyRoundStartPassives(player)` (Storia/Mate), `CheckSecondaryUnlocks(player, round)`. Reattive (EventBus): Inglese (propaga/copia aumenti), Storia secondaria (raddoppio Studio), Mate secondaria (+1/pesca), EduFisica base (fine turno)/secondaria (azioni). Solo host; guardie anti-ricorsione |
| [GameChanges.cs](GameChanges.cs) | `InstantNoteChange`, `AddActiveEffectChange`, `DrawCardsChange` | Implementazioni base `IGameChange` (Command): mutano lo stato al commit e pubblicano `NoteChangedEvent` |
| [GameChangesExtended.cs](GameChangesExtended.cs) | `GrantActionsChange`, `DoubleActionsChange`, `DrawToHandSizeChange`, `DrawAllChange`, `ForceDiscardRandomChange`, `ForceDiscardByTagChange`, `EqualizeNotesChange`, `SwapNotesChange`, `ReturnFromDiscardChange` (+enum `ReturnDestination`), `AddShieldChange`, `SetImmunityChange`, `BlockVerificaChange`, `ReturnCardToHandChange`, `ActivateWikipediaInterceptChange` | Modifiche avanzate per i nuovi effetti; helper interno `DeckOps.DrawTopToHand()` |
| [IStartingPlayerDecider.cs](IStartingPlayerDecider.cs) | interfaccia `IStartingPlayerDecider` | `DecideStartingPlayer(first, second)` — astratto per test deterministici |
| [CoinFlipStartingPlayerDecider.cs](CoinFlipStartingPlayerDecider.cs) | `CoinFlipStartingPlayerDecider` | Implementazione a lancio di moneta (RNG host-authoritative) |
| [CollectionUtils.cs](CollectionUtils.cs) | `static CollectionUtils` | `Shuffle<T>()` Fisher-Yates con RNG iniettato |

## Note

- Dipendenza circolare turni↔fasi rotta via `TurnManager.SetPhaseManager()` dopo la costruzione.
- L'host-authority è applicata a monte nel [Network](../Network/INDEX.md) layer; i manager qui assumono di essere già sull'host.
