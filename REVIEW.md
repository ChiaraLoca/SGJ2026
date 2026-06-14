# Code Review — 4E: Le Menti (rev. 2)
> Analisi aggiornata dopo le fix del 2026-06-14. Tutti i bug critici e i rischi medi sono stati corretti.
> I punti aperti rimasti sono ambiguità di design o ottimizzazioni basse priorità.

---

## Indice

1. [Bug Risolti](#1-bug-risolti)
2. [Ambiguità di Design — Ancora Aperte](#2-ambiguità-di-design--ancora-aperte)
3. [Potenziali Problemi Runtime Risolti](#3-potenziali-problemi-runtime-risolti)
4. [Bottleneck e Performance](#4-bottleneck-e-performance)
5. [Osservazioni Architetturali](#5-osservazioni-architetturali)
6. [Documentazione per Sistema](#6-documentazione-per-sistema)

---

## 1. Bug Risolti

### BUG-01 ✅ — NetworkSerializer: `Kind` e `SecondaryUnlocked` aggiunti

**File:** `Assets/Scripts/Network/NetworkSerializer.cs`

`WritePlayer` ora scrive `commander.Kind` e `commander.SecondaryUnlocked` prima degli altri campi del comandante; `ReadPlayer` li legge nella stessa posizione. Il client online riceve correttamente l'identità del comandante (nome, ritratto) e lo stato di sblocco della secondaria.

---

### BUG-02 ✅ — CardRegistry.Build copre il CommanderCatalog

**File:** `Assets/Scripts/Network/CardRegistry.cs`

`Build` ora chiama `RegisterCommanderCards(registry, content.CommanderCatalog)` invece di registrare separatamente `FirstPlayerCommanders` e `SecondPlayerCommanders`. `CommanderCatalog` deduplicando li include entrambi e aggiunge eventuali nuovi comandanti aggiunti al catalogo.

---

### BUG-03 ✅ — `EqualizeNotesChange` e `SwapNotesChange` pubblicano `NoteIncreasedEvent`

**File:** `Assets/Scripts/Core/GameChangesExtended.cs`

- `EqualizeNotesChange.Apply()`: dopo `ApplyInstantDelta`, pubblica `NoteIncreasedEvent(lowest, amount)`.
- `SwapNotesChange.Apply()`: calcola i delta e pubblica `NoteIncreasedEvent` solo per il comandante che guadagna Note.

Tutor e Rappresentante di Classe ora triggerano correttamente le passive di Inglese (propagazione +1) e Storia secondaria.

---

### BUG-04 ✅ — `IsCooperCard` usa flag serializzato

**File:** `Assets/Scripts/Cards/CardDataSO.cs`, `Assets/Scripts/Core/TurnManager.cs`, `Assets/ScriptableObjects/Cards/Generated/EduF_TestCooper.asset`

Aggiunto `[SerializeField] private bool _isCooper` a `CardDataSO` con property pubblica `IsCooper`. `TurnManager.IsCooperCard` usa `card.IsCooper` al posto di `card.CardName == "Test di Cooper"`. Il flag è impostato a `1` nel SO di Test di Cooper.

---

### BUG-06 ✅ — Dead code rimosso

Rimosso:
- `GameConfigSO._startingCardsPerCommander` e property `StartingCardsPerCommander` (mai usata da MatchSetup)
- `GameConstants.StartingCardsPerCommander`, `CardsInStartingHand`, `RoundsBeforeFinalExam`
- `DoubleActionsChange` da `GameChangesExtended.cs`
- `ReturnCardToHandChange` da `GameChangesExtended.cs`
- `TurnManager.DoubleRemainingActions()`

---

## 2. Ambiguità di Design — Ancora Aperte

### AMB-01 — Wikipedia intercetta MA l'effetto della carta intercettata si risolve ugualmente

**File:** `Assets/Scripts/Core/TurnManager.cs` righe 176-182

```csharp
if (opponent != null && opponent.WikipediaInterceptActive)
{
    opponent.Hand.Add(card);
    opponent.WikipediaInterceptActive = false;
}
// La resolve continua comunque...
_resolver.Resolve(card, context);
```

Quando Wikipedia è attiva: chi gioca la carta la vede scartata, ma l'effetto si applica normalmente **E** l'avversario (chi aveva Wikipedia) riceve una copia della carta in mano per rigiuocarla.

Da decidere prima del rilascio: intende "ruba senza effetto" (rimuovere la resolve), oppure "copia in mano ma effetto normale" (rimuovere la `Hand.Add` + `Remove` dalla mano originale)?

---

### AMB-02 — `TiebreakRule.SumNotes` definita ma non implementata

**File:** `Assets/Scripts/Core/PhaseManager.cs` e `Assets/Scripts/Config/GameConfigSO.cs`

`PhaseManager.ResolveOutcome()` usa il numero di carte nel mazzo come secondo spareggio; la `TiebreakRule` non viene mai letta. La property è esposta con commento `(non ancora applicata)`. Da implementare o rimuovere prima del lancio.

---

## 3. Potenziali Problemi Runtime Risolti

### RISK-01 ✅ — `EventBus.Clear()` all'avvio

`GameStateManager.Awake()` chiama ora `EventBus.Clear()` come prima operazione. Garantisce una slate pulita su reload di scena senza dipendere dall'ordine degli `OnDestroy`.

---

### RISK-02 ✅ — `OnValidate` in `GameConfigSO`

`OnValidate()` (solo editor, sotto `#if UNITY_EDITOR`) emette `LogError` se `_cardsPlayablePerTurn` è null/vuoto e `LogWarning` se `_maxRounds <= 0`. Il problema ora è visibile subito nell'Inspector invece di bloccare la partita silenziosamente.

---

### RISK-03 ✅ — `UpdateHandSpacing` divisione sicura

Il merge precedente aveva già aggiunto `Mathf.Min(_handDefaultSpacing, ...)`. In C# `float / 0 = +Infinity`; `Mathf.Min(8f, +Inf) = 8f`. Il caso `_handCardsBeforeOverlap = 0` è ora sicuro.

---

### RISK-06 ✅ — `CommanderWithLowestNote` estratto in `PlayerState`

Il metodo duplicato tre volte (`TurnManager`, `GameContext`, `CooperBuffEffectSO`) è stato sostituito con `PlayerState.LowestNoteCommander()`. Tutti e tre i chiamanti ora usano `player.LowestNoteCommander()`. Logica unica, nessun rischio di divergenza.

---

### RISK-04 — Nessun cap sulla dimensione della mano

**Severità:** BASSO

`DrawAllChange` e `DrawToHandSizeChange` non applicano un tetto superiore. Con combo aggressive la mano può diventare arbitrariamente grande. Non causa crash, ma impatta UI e serializzazione DTO su mazzi molto ampi.

---

### RISK-05 — `ShopPool.RefreshSlots` non esclude carte già possedute

**Severità:** BASSO

Il pool esclude le carte già presenti nel pool (`pool.Contains(card)`), ma non quelle nel mazzo o nella mano. Un giocatore può riacquistare la stessa carta più volte. Potrebbe essere intenzionale (mazzi con duplicati).

---

## 4. Bottleneck e Performance

### PERF-01 — `RenderHand` / `RenderShop`: Destroy + Instantiate a ogni stato sync

**File:** `Assets/Scripts/UI/GameView.cs`

Ad ogni `GameStateSyncedEvent`, `ClearSpawned` distrugge tutti i GameObject delle card view e li ricrea da zero. Con 15+ carte in mano e 12 nello shop, sono 27+ Destroy + 27+ Instantiate + rebuild del layout per sync. In un turno normale: ~20-30 sync per round.

Soluzione futura: pool di CardView riciclate (`_handPool.Get()`/`Release()`).

---

### PERF-02 — `CurrentNote` calcolato on-demand

**File:** `Assets/Scripts/Commanders/CommanderState.cs`

Con pochi effetti attivi (0-3) è trascurabile. Da cachare solo se il numero di effetti crescesse molto.

---

## 5. Osservazioni Architetturali

### ARCH-01 — Rispetto dei pattern: OTTIMO

L'architettura è coerente con il CLAUDE.md:
- Strategy pattern applicato a tutti gli effetti
- Command pattern per `IGameChange`
- EventBus decoupled e fortemente tipizzato
- DTO-first per la rete
- Host-authoritative ovunque

---

### ARCH-02 — `EffectResolver` pubblica `CardResolvingEvent` prima degli effetti (intenzionale)

`CardResolvingEvent` viene pubblicato PRIMA di `effect.Apply(context)`. Permette a `CommanderPassiveSystem` di avere il contesto pronto quando i `NoteIncreasedEvent` arrivano durante `CommitChanges`. L'ordine è intenzionale e non va modificato.

---

### ARCH-03 — `SelectedOwnAndEnemy` non restituisce nulla da `ResolveCommanders`

**File:** `Assets/Scripts/Core/GameContext.cs`

Il target `SelectedOwnAndEnemy` ha un `break` vuoto in `ResolveCommanders`. Gli effetti che lo usano devono leggere `SelectedOwnTargets`/`SelectedEnemyTargets` direttamente. Un uso scorretto tramite `ResolveCommanders` fallisce silenziosamente.

**Raccomandazione:** aggiungere `Debug.LogWarning` o `throw` se questo target viene passato a `ResolveCommanders` durante lo sviluppo.

---

### ARCH-04 — `HotseatTransport` muta `LocalActorNumber` nel `BroadcastState`

La UI si ridisegna sempre dalla prospettiva del giocatore attivo (corretto per hotseat). Il campo `LocalActorNumber` è mutabile dopo la costruzione; `NetworkGameManager` non lo cacha, quindi funziona. Da tenere a mente se si aggiungono cache sul lato UI.

---

## 6. Documentazione per Sistema

### 6.1 Core

#### `GameStateManager`
Singleton. `Awake()` chiama `EventBus.Clear()` prima di qualsiasi altra operazione, garantendo una slate pulita. Costruisce i manager in `BuildManagers()`. Due modalità di boot: `_autoStartOffline = true` per test locali, `false` per il boot guidato da rete.

**Flusso:**
```
GameStateManager.Awake()          → EventBus.Clear() → registra singleton + config + RNG
NetworkGameManager.Awake()        → CardRegistry.Build(CommanderCatalog) → trasporto
NetworkGameManager.Start()        → [host] SetCommanderSelections → StartMatch → BroadcastState
                                    [client] RequestStateUntilSynced
GameStateManager.StartMatch()     → BuildPlayers() → BuildManagers() → PhaseManager.BeginMatch()
PhaseManager.BeginMatch()         → EnterPhase(Play) → BeginRound → passive inizio round → StartTurn(first)
TurnManager.StartTurn()           → SetActivePlayer → DrawTurnStartCards → CheckSecondaryUnlocks
```

#### `TurnManager`
Gestisce la Fase PLAY. Invarianti chiave:
- `RemainingActions = max(0, _cardsAllowedThisTurn - _cardsPlayedThisTurn)`
- `_cardsPlayedThisTurn += card.ActionCost` (Studio Notturno = 2)
- Cooper: nota pre-buff catturata via `player.LowestNoteCommander()` PRIMA di `Resolve`
- Copiare: flag catturato e resettato PRIMA della resolve, re-resolve DOPO

#### `PhaseManager`
`Setup → Play → Verifica → Shop → Draw → [Play...] → FinalExam`. `HandleVerifica`: converte Note→Credits; `FinishShop`: attende entrambi i giocatori via `HashSet<int>`; `ConvertAndAdvance`: Draw phase + avanzamento round.

#### `GameContext`
Contesto immutabile in lettura + lista `_pendingChanges`. `SetCommanderRedirect(from,to)` per il mirror della secondaria Inglese. `ResolveCommanders(OwnLowestNoteCommander)` delega a `ActivePlayer.LowestNoteCommander()`.

#### `CommanderPassiveSystem`
- **Proattive:** `ApplyRoundStartPassives` (Storia, Mate), `CheckSecondaryUnlocks`
- **Reattive:** `OnNoteIncreased` (Inglese base+sec, Storia sec — ora triggerate correttamente anche da Tutor/Rappresentante), `OnCardsDrawn` (Mate sec), `OnCardPlayed` (EduFisica sec), `OnTurnEnded` (EduFisica base)

---

### 6.2 Cards

#### `CardDataSO`
Campi chiave:
- `IsVerifica`: chiude il round
- `IsCooper` (nuovo): flag serializzato; usato da `TurnManager.IsCooperCard`; il confronto per nome è stato rimosso
- `ActionCost`: default 1; Studio Notturno = 2
- `RequiresTargetSelection`: property derivata dagli effetti

#### `EffectResolver`
`CardResolvingEvent` PRIMA degli effetti, `CardPlayedEvent` DOPO. Per ogni effetto: `Apply → CommitChanges`. I `NoteIncreasedEvent` prodotti durante `CommitChanges` trovano già il contesto pronto per le passive.

---

### 6.3 Network

#### `CardRegistry.Build`
Registra `content.CommanderCatalog` (include e deduplicano i default via `AddUniqueCommanders`), poi Verifica e ShopCatalog. Copre tutti i comandanti selezionabili, non solo quelli default.

#### `NetworkSerializer`
Ordine di serializzazione `CommanderDTO` (critico — lettura e scrittura devono essere identiche):
```
Kind (int) → SecondaryUnlocked (bool) → BaseNote (int) → CurrentNote (int)
→ HasDebuff (bool) → ActiveBuffCount (int) → ActiveDebuffCount (int)
```

---

### 6.4 Players

#### `PlayerState`
Aggiunto `LowestNoteCommander()`: restituisce il comandante con `CurrentNote` più bassa, parità → slot 0. Unica implementazione condivisa da `TurnManager`, `GameContext` e `CooperBuffEffectSO`.

---

## Riepilogo priorità

| ID | Tipo | Descrizione breve | Stato |
|---|---|---|---|
| BUG-01 | Bug | NetworkSerializer: Kind/SecondaryUnlocked mancanti | ✅ RISOLTO |
| BUG-02 | Bug | CardRegistry non copre CommanderCatalog | ✅ RISOLTO |
| BUG-03 | Bug | Tutor/Rappresentante non triggeravano passiva Inglese/Storia | ✅ RISOLTO |
| BUG-04 | Bug | IsCooperCard usava nome stringa fragile | ✅ RISOLTO |
| BUG-05 | Dead code | ReturnCardToHandChange orfano | ✅ RIMOSSO |
| BUG-06 | Dead code | StartingCardsPerCommander mai usato | ✅ RIMOSSO |
| AMB-01 | Design | Wikipedia: effetto si risolve anche quando intercetta | ❓ DA DECIDERE |
| AMB-02 | Design | TiebreakRule.SumNotes non implementata | ❓ DA DECIDERE |
| RISK-01 | Runtime | EventBus.Clear() mai chiamato | ✅ RISOLTO |
| RISK-02 | Runtime | GetCardsPlayablePerTurn può restituire 0 | ✅ RISOLTO (OnValidate) |
| RISK-03 | Runtime | UpdateHandSpacing: divisione sicura con Mathf.Min | ✅ RISOLTO (merge prec.) |
| RISK-04 | Design | Nessun cap sulla dimensione della mano | 🟢 BASSO |
| RISK-05 | Design | ShopPool non esclude carte già possedute | 🟢 BASSO |
| RISK-06 | Manutenzione | CommanderWithLowestNote duplicata 3 volte | ✅ RISOLTO |
| PERF-01 | Performance | RenderHand: Destroy+Instantiate a ogni sync | 🟡 MEDIO (post-jam) |
