# Code Review — 4E: Le Menti
> Analisi statica completa del codice. Nessuna modifica apportata.  
> Data: 2026-06-14

---

## Indice

1. [Bug Critici](#1-bug-critici)
2. [Bug Confermati](#2-bug-confermati)
3. [Ambiguità di Design](#3-ambiguità-di-design)
4. [Dead Code](#4-dead-code)
5. [Potenziali Problemi Runtime](#5-potenziali-problemi-runtime)
6. [Bottleneck e Performance](#6-bottleneck-e-performance)
7. [Osservazioni Architetturali](#7-osservazioni-architetturali)
8. [Documentazione per Sistema](#8-documentazione-per-sistema)

---

## 1. Bug Critici

### BUG-01 — NetworkSerializer: `Kind` e `SecondaryUnlocked` mancano nella serializzazione

**File:** `Assets/Scripts/Network/NetworkSerializer.cs`, metodi `WritePlayer` / `ReadPlayer`  
**Severità:** CRITICO (rompe l'online in produzione)

`CommanderDTO` dichiara due campi:
```csharp
public int Kind;
public bool SecondaryUnlocked;
```
`GameStateDtoBuilder.BuildCommander` li popola correttamente, ma `NetworkSerializer.WritePlayer` **non li scrive**, e `ReadPlayer` **non li legge**. In modalità Photon il client riceve `Kind = 0` (primo tipo) e `SecondaryUnlocked = false` per tutti i comandanti.

**Conseguenze:**
- La UI del client mostra sempre la definizione del primo `CommanderKind` per tutti i comandanti (ritratti e nomi sbagliati).
- Lo sblocco della secondaria non si propaga mai al client: la UI remota non mostra mai il badge di secondaria sbloccata.

**Fix:** aggiungere in `WritePlayer`, dopo `ActiveDebuffCount`:
```csharp
writer.Write(commander.Kind);
writer.Write(commander.SecondaryUnlocked);
```
e in `ReadPlayer`, nel blocco `new CommanderDTO { ... }`:
```csharp
Kind = reader.ReadInt32(),
SecondaryUnlocked = reader.ReadBoolean()
```

---

### BUG-02 — CardRegistry costruito solo dai comandanti default: non copre il CommanderSelect

**File:** `Assets/Scripts/Network/CardRegistry.cs`, metodo `Build`  
**Severità:** CRITICO se il CommanderSelect è attivo

`CardRegistry.Build` registra le carte dei soli `FirstPlayerCommanders` e `SecondPlayerCommanders` (i comandanti di default del `GameContent`). Se un giocatore seleziona comandanti diversi via `CommanderSelectController`, le loro carte collegate non sono mai registrate. Al momento di serializzare la mano nel DTO, `GetId` restituirà `NoCard (-1)` per tutte quelle carte; la UI le ignorerà.

**Nota:** Attualmente il `_commanderCatalog` in `GameContent.asset` contiene 4 riferimenti nulli, quindi la schermata di selezione non funziona comunque. Il bug latente diventerà manifesto quando il catalogo verrà popolato.

**Fix:** `Build` dovrebbe registrare anche le carte di tutti i comandanti nel `CommanderCatalog`:
```csharp
foreach (CommanderDataSO c in content.CommanderCatalog)
    if (c != null)
        foreach (CardDataSO card in c.LinkedCards)
            registry.Register(card);
```

---

## 2. Bug Confermati

### BUG-03 — `EqualizeNotesChange` e `SwapNotesChange` non pubblicano `NoteIncreasedEvent`

**File:** `Assets/Scripts/Core/GameChangesExtended.cs`  
**Severità:** MEDIO

`EqualizeNotesChange.Apply()` chiama `ApplyInstantDelta` + `NoteChangedEvent`, ma **non** pubblica `NoteIncreasedEvent`. Stessa cosa per `SwapNotesChange`. Di conseguenza:
- Tutor non innesca la passiva base di Inglese (+1 all'altro comandante).
- Rappresentante di Classe non innesca né Inglese né Storia secondaria.

`InstantNoteChange` (usato dal 95% degli effetti) invece pubblica correttamente entrambi gli eventi quando `delta > 0`.

**Fix:** in `EqualizeNotesChange.Apply()`, dopo `lowest.ApplyInstantDelta(amount)`:
```csharp
if (amount > 0) EventBus.Publish(new NoteIncreasedEvent(lowest, amount));
```
In `SwapNotesChange.Apply()`, dopo le due chiamate `ApplyInstantDelta`, calcolare i delta e pubblicare `NoteIncreasedEvent` per il comandante il cui valore è aumentato.

---

### BUG-04 — `IsCooperCard` usa confronto per nome stringa

**File:** `Assets/Scripts/Core/TurnManager.cs`  
**Metodo:** `IsCooperCard(CardDataSO)`

```csharp
return card != null && card.CardName == "Test di Cooper";
```

Se il campo `_cardName` nel SO viene rinominato, localizzato, o contiene anche solo uno spazio extra, la verifica Cooper smette silenziosamente di funzionare: la carta non rientra più in mano. Non c'è errore di compilazione né warning a runtime.

**Fix:** aggiungere al `CardDataSO` (o a `CardDataSO`) un `[SerializeField] private bool _isCooper;` oppure riconoscere la carta tramite un tag dedicato (es. `CardTag.Cooper`) verificabile con `card.HasTag(CardTag.Cooper)`.

---

### BUG-05 — `ReturnCardToHandChange` è dead code (Cooper non la usa)

**File:** `Assets/Scripts/Core/GameChangesExtended.cs`  
**Severità:** BASSO (dead code, non causa bug)

La classe `ReturnCardToHandChange` esisteva per gestire il ritorno in mano di Cooper. Dopo il refactor di questa sessione, la logica è stata spostata direttamente in `TurnManager.TryPlayCard` (righe 195-199). La classe non è più istanziata né referenziata da nessun file. Rimane nel codebase creando confusione su "chi gestisce il ritorno di Cooper".

---

### BUG-06 — `GameConfigSO.StartingCardsPerCommander` non viene mai letto

**File:** `Assets/Scripts/Config/GameConfigSO.cs` e `Assets/Scripts/Core/MatchSetup.cs`  
**Severità:** BASSO (incoerenza)

`GameConfigSO` espone `StartingCardsPerCommander` come proprietà configurabile, ma `MatchSetup.BuildPlayer` non la usa: conta le carte da `commanderData.LinkedCards` (hardcoded nella SO). Il config field è muto. `GameConstants.StartingCardsPerCommander = 5` riduplica lo stesso valore. Nessun bug attivo, ma chi modifica `_startingCardsPerCommander` nel config non vede alcun effetto.

---

## 3. Ambiguità di Design

### AMB-01 — Wikipedia intercetta MA risolve anche l'effetto della carta rubata

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

Quando Wikipedia è attiva, l'avversario che subisce l'intercettazione:
1. Perde la carta dalla mano (va al cimitero)
2. L'effetto della carta SI risolve nel suo contesto
3. L'avversario (chi ha giocato Wikipedia) riceve la carta in mano

La carta "copiata" in mano permette di rigiuocarla nel turno successivo, MA gli effetti si applicano **anche** al giocatore che l'ha giocata questa volta. Effettivamente la carta produce effetti DUE volte in totale.

Se il design intende "ruba la carta senza che l'effetto succeda", la `_resolver.Resolve` non dovrebbe eseguirsi. Se intende "copia in mano senza rubare", la carta non dovrebbe essere rimossa dalla mano del giocatore attivo.

---

### AMB-02 — Spareggio `TiebreakRule.SumNotes` definito ma non implementato

**File:** `Assets/Scripts/Config/GameConfigSO.cs` e `Assets/Scripts/Core/PhaseManager.cs`

`TiebreakRule` è definita come enum con `SumNotes`, e il config la espone, ma `PhaseManager.ResolveOutcome()` non la usa: usa un secondo spareggio basato sul numero di carte nel mazzo. `TiebreakRule` è dead config al momento.

---

## 4. Dead Code

| Elemento | File | Note |
|---|---|---|
| `DoubleActionsChange` | `GameChangesExtended.cs` | Rimasta dopo il refactor di Copiare; non istanziata. |
| `TurnManager.DoubleRemainingActions()` | `TurnManager.cs` | Pubblica come `// legacy`; nessun chiamante. |
| `ReturnCardToHandChange` | `GameChangesExtended.cs` | Cooper ora gestito inline in TurnManager. |
| `GameContentSO.AddUniqueCommanders()` | `GameContentSO.cs` | Metodo privato statico mai chiamato. |
| `GameContentSO.FindCommanderByKind()` | `GameContentSO.cs` | Metodo privato statico mai chiamato. |
| `GameConfigSO.TiebreakRule` | `GameConfigSO.cs` | Non letto da PhaseManager. |
| `GameConfigSO.StartingCardsPerCommander` | `GameConfigSO.cs` | Non letto da MatchSetup. |
| `GameConstants.StartingCardsPerCommander` | `GameConstants.cs` | Duplicato del sopra, mai usato. |
| `GameConstants.CardsInStartingHand` | `GameConstants.cs` | Mai referenziato nel codice. |
| `GameConstants.RoundsBeforeFinalExam` | `GameConstants.cs` | `MaxRounds` viene da config, questa costante è ridondante. |

---

## 5. Potenziali Problemi Runtime

### RISK-01 — `EventBus.Clear()` mai chiamato: rischio leak al reload di scena

**File:** `Assets/Scripts/Events/EventBus.cs`

L'`EventBus` è statico. Tutti i sistemi si disiscriscono correttamente nei propri `OnDestroy` / `Dispose`. Tuttavia, se la scena viene ricaricata senza restart dell'app (es. "rivincita"), il `Dictionary<Type, Delegate>` sopravvive. Se per qualsiasi motivo un sistema non raggiunge `OnDestroy` (crash di un GameObject, exception in un listener), i vecchi handler restano iscritti nella nuova partita.

**Raccomandazione:** chiamare `EventBus.Clear()` come prima istruzione di `GameStateManager.Awake()`, prima di registrare qualsiasi cosa, per garantire una slate pulita.

---

### RISK-02 — `GetCardsPlayablePerTurn` può restituire 0 se il config è mal configurato

**File:** `Assets/Scripts/Config/GameConfigSO.cs`

Se `_cardsPlayablePerTurn` è null o vuoto nell'inspector, il metodo restituisce 0. Di conseguenza `_cardsAllowedThisTurn = 0`, `RemainingActions = 0`, e nessun giocatore può mai giocare carte. La partita si blocca senza errori.

**Raccomandazione:** aggiungere una validazione in `OnValidate()` del SO (solo editor) che segnali il problema.

---

### RISK-03 — `UpdateHandSpacing`: divisione per zero se `_handCardsBeforeOverlap == 0`

**File:** `Assets/Scripts/UI/GameView.cs`

```csharp
if (cardCount <= _handCardsBeforeOverlap)
{
    layout.spacing = _handDefaultSpacing;
    return;
}
int spacesCount = cardCount - GameConstants.IndexToCountOffset;  // cardCount - 1
layout.spacing = (availableWidth - cardsWidth) / spacesCount;
```

Se `_handCardsBeforeOverlap` è impostato a 0 nell'inspector, e il giocatore ha esattamente 1 carta in mano, `cardCount = 1 > 0`, si salta la early return, e `spacesCount = 0` causa una divisione per zero. Con il valore default (4) non si verifica mai.

---

### RISK-04 — Nessun limite massimo alla dimensione della mano

**File:** vari

Effetti come `DrawAllChange` (Approfondimento) e `DrawToHandSizeChange` (Biblioteca) non sono limitati da un tetto superiore di hand size. Se un giocatore accumula moltissimi effetti draw in un turno, la mano può diventare arbitrariamente grande. Non è un crash, ma può causare problemi UI (carte che escono dallo schermo) e rallentamenti nella serializzazione DTO (array grande).

---

### RISK-05 — `ShopPool.RefreshSlots` non esclude carte già possedute nel mazzo/mano

**File:** `Assets/Scripts/Shop/ShopPool.cs`

Il candidato viene escluso solo se `pool.Contains(card)`, ma non se la carta è già nel mazzo o nella mano del giocatore. Tecnicamente un giocatore potrebbe riacquistare la stessa carta più volte. Potrebbe essere intentenzionale (mazzi con duplicati), ma vale la pena chiarirlo nel design.

---

### RISK-06 — `CommanderWithLowestNote` duplicata in tre posti

**File:** `TurnManager.cs`, `GameContext.cs`, `CooperBuffEffectSO.cs`

La stessa logica "trova il comandante con nota più bassa" è copiata tre volte. Le tre copie sono attualmente identiche. Se una diverge (tiebreak diverso, gestione null diversa), Cooper si comporta in modo incoerente a seconda di quale versione viene eseguita.

**Raccomandazione:** estrarre il metodo in `PlayerState` (dove vive l'array dei comandanti) come `CommanderState LowestNoteCommander()`.

---

## 6. Bottleneck e Performance

### PERF-01 — `RenderHand` / `RenderShop`: Destroy + Instantiate a ogni stato sync

**File:** `Assets/Scripts/UI/GameView.cs`

Ad ogni `GameStateSyncedEvent` (cioè dopo ogni singola azione), `ClearSpawned` distrugge tutti i GameObject delle card view e poi li ricrca da zero. Con una mano di 15+ carte e uno shop di 12 carte, questo è 27+ `Destroy` + 27+ `Instantiate` + layout rebuild per sync.

In un turno normale con 3-4 carte giocate e 2 pesca, sono circa 20-30 sync per round. È gestibile in una jam ma è il principale collo di bottiglia UI. In produzione andrebbe sostituito con un pool di view riciclate.

---

### PERF-02 — `CurrentNote` calcolato on-demand, con loop multipli

**File:** `Assets/Scripts/Commanders/CommanderState.cs`

```csharp
public int CurrentNote
{
    get
    {
        int total = _baseNote + _instantNoteDelta;
        foreach (ActiveEffect buff in _activeBuffs) total += buff.Magnitude;
        foreach (ActiveEffect debuff in _activeDebuffs) total -= debuff.Magnitude;
        return total < 0 ? 0 : total;
    }
}
```

Con pochi effetti attivi (di solito 0-3) questo è O(n) con n piccolo: accettabile. Tuttavia `PlayerState.TotalNotes` chiama `CurrentNote` per due comandanti, e questo viene a sua volta chiamato in `BuildPlayer` per il DTO. Non è un problema ora, ma se il numero di effetti attivi crescesse, andrebbe cachato.

---

### PERF-03 — `ScalingEffectSO.CountDistinctTags`: bit-counting manuale

**File:** `Assets/Scripts/Cards/Effects/ScalingEffectSO.cs`

```csharp
int value = (int)union;
while (value != 0) { bits += value & 1; value >>= 1; }
```

Funziona correttamente, ma è la versione O(bit-width) del popcount. Con 6 tag (6 bit), è un ciclo da massimo 6 iterazioni: trascurabile. Nessun bottleneck.

---

### PERF-04 — `ForceDiscardRandomChange` costruisce una `List` temporanea a ogni Apply

**File:** `Assets/Scripts/Core/GameChangesExtended.cs`

Ogni apply di Gossip alloca una `List<CardDataSO>`. Con una mano di ~10 carte è una piccola allocazione. Nessun problema in jam, ma da notare per un profiler in produzione.

---

## 7. Osservazioni Architetturali

### ARCH-01 — Rispetto dei pattern: OTTIMO

L'architettura è coerente con il CLAUDE.md:
- Strategy pattern correttamente applicato a tutti gli effetti (CardEffectSO)
- Command pattern per IGameChange: gli effetti non mutano lo stato direttamente
- EventBus decoupled e fortemente tipizzato
- DTO-first per la rete: nessun UnityEngine.Object in rete
- Host-authoritative: tutti i check `if (!IsHost) return` sono presenti

### ARCH-02 — `EffectResolver.Resolve` pubblica `CardResolvingEvent` PRIMA di applicare gli effetti

**File:** `Assets/Scripts/Cards/EffectResolver.cs`

```csharp
EventBus.Publish(new CardResolvingEvent(card, context.ActivePlayer, context));
foreach (CardEffectSO effect in card.Effects)
{
    effect.Apply(context);
    context.CommitChanges();
}
EventBus.Publish(new CardPlayedEvent(card, context.ActivePlayer));
```

`CardResolvingEvent` permette a `CommanderPassiveSystem` di catturare `_resolvingCard` e `_resolvingContext` PRIMA che gli effetti vengano applicati. Questo significa che la passiva di Inglese (che reagisce a `NoteIncreasedEvent`, scatenato dentro `CommitChanges`) ha già il contesto pronto. L'ordine è corretto e intenzionale, ma è un meccanismo non ovvio: da documentare.

### ARCH-03 — `GameContext.SelectedOwnAndEnemy` è gestito dagli effetti concreti, non da `ResolveCommanders`

**File:** `Assets/Scripts/Core/GameContext.cs` riga 271

```csharp
case EffectTarget.SelectedOwnAndEnemy:
    // Gestito direttamente dall'effetto concreto (es. SwapNotesEffectSO).
    break;
```

`ResolveCommanders` per questo target non restituisce nulla. Chi usa `SelectedOwnAndEnemy` DEVE leggere `SelectedOwnTargets` e `SelectedEnemyTargets` direttamente. Questo comportamento silenzioso (yield vuoto senza errore) può confondere chi aggiunge nuovi effetti con questo target e usa `ResolveCommanders` aspettandosi qualcosa.

**Raccomandazione:** aggiungere un `Debug.LogWarning` o `throw` se il target è `SelectedOwnAndEnemy` e viene chiamato `ResolveCommanders`, per segnalare l'uso scorretto durante lo sviluppo.

### ARCH-04 — `HotseatTransport` muta `LocalActorNumber` nel `BroadcastState`

**File:** `Assets/Scripts/Network/HotseatTransport.cs`

```csharp
public void BroadcastState(GameStateDTO state)
{
    if (state.ActiveActorNumber >= 0)
        LocalActorNumber = state.ActiveActorNumber;
    StateReceived?.Invoke(state);
}
```

Questo fa sì che la UI si ridisegni sempre dalla prospettiva del giocatore attivo (utile per hotseat). L'effetto collaterale è che `LocalActorNumber` è mutabile dopo la costruzione: se `NetworkGameManager` ha cachato l'attore locale prima di questo aggiornamento, la cache sarebbe stale. Attualmente `NetworkGameManager` chiama `_transport.LocalActorNumber` direttamente (non lo cacha), quindi funziona. Da tenere a mente.

### ARCH-05 — Passiva EduFisica base non controlla `commander.SecondaryUnlocked` (CORRETTO)

La passiva base di EduFisica (fine turno, +1 Note all'altro comandante se meno carte dell'avversario) è applicata a tutti i comandanti EduFisica indipendentemente dallo sblocco della secondaria. Questo è corretto per design: la base è sempre attiva, la secondaria si sblocca in più.

---

## 8. Documentazione per Sistema

### 8.1 Core

#### `GameStateManager`
Singleton che possiede lo stato runtime della partita. È il punto di accesso centrale per tutti i sistemi. Costruisce e cabla i manager in `BuildManagers()`. Fornisce `BuildContext()` per creare il contesto di risoluzione carta. Due modalità di boot: `_autoStartOffline = true` per test locali, `false` per il boot guidato da rete.

**Flusso di avvio:**
```
GameStateManager.Awake()          → registra singleton, GameConfig, RNG
NetworkGameManager.Awake()        → costruisce CardRegistry, trasporto
NetworkGameManager.Start()        → [host] StartMatch() + BroadcastState
                                    [client] RequestStateUntilSynced()
GameStateManager.StartMatch()     → BuildPlayers() → BuildManagers() → PhaseManager.BeginMatch()
PhaseManager.BeginMatch()         → EnterPhase(Play) → BeginRound() → passive inizio round → StartTurn(firstPlayer)
TurnManager.StartTurn()           → SetActivePlayer → DrawTurnStartCards → CheckSecondaryUnlocks
```

#### `TurnManager`
Gestisce la Fase PLAY. Mantiene i contatori `_cardsPlayedThisTurn` / `_cardsAllowedThisTurn` e il flag `_copyNextCardActive` per Copiare.

**Invarianti chiave:**
- `RemainingActions = max(0, _cardsAllowedThisTurn - _cardsPlayedThisTurn)`
- `_cardsPlayedThisTurn += card.ActionCost` (non sempre +1; Studio Notturno costa 2)
- Cooper: la nota pre-buff viene catturata PRIMA di `_resolver.Resolve()` e verificata DOPO
- Copiare: il flag viene catturato e resettato PRIMA della resolve, la re-resolve avviene DOPO il normale flusso di gioco

**Attenzione:** `EndTurn` chiama `_state.OpponentOf(player)` e avvia il turno successivo solo se la fase è ancora `Play`. Se `HandleVerifica` ha già cambiato fase, il `StartTurn` non viene chiamato.

#### `PhaseManager`
State machine: `Setup → Play → Verifica → Shop → Draw → [Play...]  → FinalExam`.

Transizioni chiave:
- `HandleVerifica(closer)`: converte Note→Credits per entrambi; va allo Shop o direttamente a Draw (ultimo round)
- `FinishShop(player)`: usa `_shopFinished: HashSet<int>` per aspettare entrambi i giocatori
- `ConvertAndAdvance()`: esegue Draw phase (scarta mano, rimischia, pesca, reset Note), avanza round

**Nota:** `ConvertNotesToCredits` usa `Math.Round` con il moltiplicatore float: con `NoteToCreditsMultiplier = 1.0` i crediti sono sempre interi esatti.

#### `RoundManager`
Puro contatore. `Advance()` incrementa l'indice e pubblica `RoundEndedEvent`. `IsFinalExamReached` true quando `CurrentRoundIndex >= MaxRounds`.

#### `GameContext`
Contesto immutabile (stato visibile in lettura) + lista mutabile `_pendingChanges`. Gli effetti registrano modifiche, il resolver le committa. Il redirect usato dalla secondaria di Inglese (`SetCommanderRedirect`) modifica solo il comportamento di `ResolveCommanders`: non muta lo stato diretto.

#### `CommanderPassiveSystem`
Le passive si dividono in:
- **Proattive** (chiamate dai manager): `ApplyRoundStartPassives` (Storia, Mate), `CheckSecondaryUnlocks`
- **Reattive** (EventBus): `OnNoteIncreased` (Inglese base+secondaria, Storia secondaria), `OnCardsDrawn` (Mate secondaria), `OnCardPlayed` (EduFisica secondaria), `OnTurnEnded` (EduFisica base)

La guardia `_applyingReactiveBonus` previene cascate nell'handler `OnNoteIncreased`. La guardia è un semplice booleano, sicuro in un contesto single-threaded.

---

### 8.2 Cards

#### `CardDataSO`
Definizione immutabile. Campi chiave per il flusso:
- `IsVerifica`: distingue la carta speciale da quelle standard
- `RequiresTargetSelection` / `RequiresEnemyTargetSelection` / `RequiresOwnTargetSelection`: properties derivate dagli effetti, usate dalla UI per il flusso di selezione bersaglio
- `ActionCost`: default 1; Studio Notturno = 2. Usato da `TurnManager.TryPlayCard` per il controllo azioni e l'incremento del contatore
- `ShopCost`: risolto da `GameConfigSO.GetTierCost(_tier)`; usa il fallback `_shopCost` se il config non è attivo

#### `EffectResolver`
Applica gli effetti in sequenza. Per ogni effetto: `Apply(context)` → `context.CommitChanges()`. Questo garantisce che gli effetti successivi leggano lo stato aggiornato (es. un effetto draw dopo un effetto che aggiunge azioni). Pubblica `CardResolvingEvent` PRIMA degli effetti (per le passive) e `CardPlayedEvent` DOPO (per il cleanup e EduFisica secondaria).

#### `CardEffectSO` (base astratta)
Unica API: `Apply(GameContext context)`. L'effetto deve solo chiamare `context.RegisterChange(new SomeChange(...))`. Non deve mai mutare lo stato direttamente.

#### Effetti implementati

| Classe | Carta/e | Note implementative |
|---|---|---|
| `BuffEffectSO` | Varie | Istantaneo o a durata; bersaglio da `Target` enum |
| `DebuffEffectSO` | Minaccia, Rissa | Come sopra; il debuff a durata usa `AddActiveEffectChange` |
| `DrawEffectSO` | Biblioteca, Approfondimento | `DrawMode.Fixed`, `ToHandSize`, `DrawAll` |
| `ConditionalEffectSO` | Varie | Wrappa un `_innerEffect` e lo applica solo se `_condition.IsMet()` |
| `ExtraActionEffectSO` | Metodo, Progetto | `+N azioni`; `DoubleRemaining` per raddoppio |
| `CooperBuffEffectSO` | Test di Cooper | Applica +2 alla nota più bassa; TurnManager gestisce il ritorno in mano |
| `WikipediaEffectSO` | Storia Wikipedia | Attiva `WikipediaInterceptActive` sul giocatore **attivo** (non avversario) |
| `ScalingEffectSO` | Ripasso, Riassunto, Appunti, Sabotaggio | Scala Note/azioni/pesca in base a `CountSource` |
| `ForceDiscardEffectSO` | Gossip, Politica, Bullismo | `Random` (esclude Verifica) o `ByTag` |
| `EqualizeNotesEffectSO` | Tutor | Alza il comandante più basso verso il più alto |
| `SwapNotesEffectSO` | Rappresentante di Classe | Richiede `SelectedOwnAndEnemy` (2 step UI) |
| `ReturnFromDiscardEffectSO` | Schema, Compito a Casa | Carta casuale dal cimitero; destinazione `Hand` o `DeckTop` |
| `ShieldEffectSO` | Dialogo | +1 `_debuffShields` sul comandante |
| `ImmunityEffectSO` | Fidanzata | `SetNoteFloorLocked(true)` fino al prossimo turno del proprietario |
| `BlockVerificaEffectSO` | Sciopero | `VerificaBlocked = true` sull'avversario; si libera a fine turno |
| `MoveVerificaToDeckBottomEffectSO` | Occupazione | Cerca Verifica in mano → mazzo → scarti (ordine di priorità) |
| `CopyNextCardEffectSO` | Copiare | Imposta `_copyNextCardActive` su TurnManager; re-resolve della prossima carta |

---

### 8.3 Commanders

#### `CommanderState`
Stato runtime: `_baseNote + _instantNoteDelta + sum(buffs) - sum(debuffs)`, con floor a 0. Reset post-round azzera `_instantNoteDelta`, `_activeBuffs`, `_activeDebuffs`, `_debuffShields`, `_noteFloorLocked`. `_secondaryUnlocked` è **permanente** (non resettato).

**`AbsorbNegative()`:** L'immunità (`_noteFloorLocked`) ha priorità sullo scudo (`_debuffShields`): blocca senza consumarsi. Lo scudo blocca consumandosi. Questa semantica è importante: con entrambi attivi, l'immunità viene usata prima e lo scudo rimane intatto.

#### `CommanderDataSO`
Non mostrato nel review ma referenziato ovunque. Contiene `Kind`, `BaseNote`, `LinkedCards[]`. Il `Kind` è l'unico identificatore stabile cross-rete (serializato come int nel DTO).

---

### 8.4 Players

#### `PlayerState`
Aggregato dello stato di un giocatore. Espone liste mutabili (`Hand`, `Deck`, `DiscardPile`, `ShopPool`) direttamente: i sistemi le modificano in place. Non c'è incapsulamento intenzionale — l'accesso diretto è necessario per i `IGameChange`.

**Valute:** `Credits` (permanenti, valuta shop e vittoria) vs Note (temporanee per round, sum dei `CurrentNote` dei comandanti). La conversione Note→Credits avviene in `PhaseManager.ConvertNotesToCredits` con `Math.Round`.

`SpendCredits(amount)` clamps a 0 senza errore: se `ShopManager.TryPurchase` bypassa la validazione, i Credits non diventano negativi ma il bug è silenziato.

---

### 8.5 Network

#### `GameStateDTO` / `PlayerDTO` / `CommanderDTO`
Snapshot flat di tipi primitivi. Carta referenziate per id intero (risolto da `CardRegistry`). Il DTO è l'unica fonte di verità per la UI: la view non ha accesso diretto allo stato.

**Campi nota:**
- `CanPlayVerificaThisTurn`: propagato dal `TurnManager` via builder; usato dalla UI per abilitare la carta Verifica
- `PlayedCardSequence` + `LastPlayedCardId` + `LastPlayedActorNumber`: usati da `GameView.AnimatePlayedCard` per rilevare nuove carte giocate e avviare l'animazione
- `_displayedEnemyCredits` in `GameView`: i Credits avversari sono mostrati solo quando cambiano (per non rivelare l'accumulo in tempo reale)

#### `NetworkSerializer`
Serializzazione binaria compatta con `BinaryWriter/Reader`. L'ordine dei campi deve essere identico tra scrittura e lettura (non c'è header di versione). **Vedere BUG-01 sopra per il campo mancante.**

#### `CardRegistry`
Mappa `CardDataSO ↔ int`. L'id è l'indice di inserzione (stabile per l'intera partita). Host e client costruiscono il registry dallo stesso `GameContentSO`: gli id sono allineati automaticamente.

#### `HotseatTransport`
Loopback sincrono: `SendIntent` → `IntentReceived` nello stesso frame. `BroadcastState` aggiorna `LocalActorNumber` prima di notificare la UI, garantendo che la view si ridisegni dalla prospettiva del giocatore attivo.

#### `NetworkGameManager`
Orchestratore rete. È l'unico sistema che parla con il trasporto e con l'EventBus di rete (`GameStateSyncedEvent`). La UI non conosce Photon.

---

### 8.6 Events

#### `EventBus`
Bus statico typesafe. Usa `Dictionary<Type, Delegate>` con multicast delegate (`+=`). La subscription è O(1); la publish è O(n handler). Thread-unsafe (single-threaded Unity).

**Evento che attraversa più sistemi:**

```
TurnManager.TryPlayCard
  → _resolver.Resolve(card, context)
      → EventBus.Publish(CardResolvingEvent)   ← CommanderPassiveSystem cattura card/context
      → effect.Apply(context)
          → context.RegisterChange(InstantNoteChange)
      → context.CommitChanges()
          → InstantNoteChange.Apply()
              → EventBus.Publish(NoteChangedEvent)   ← UI aggiorna numeretti
              → EventBus.Publish(NoteIncreasedEvent) ← CommanderPassiveSystem (Inglese/Storia)
      → EventBus.Publish(CardPlayedEvent)      ← CommanderPassiveSystem (EduFisica sec.)
                                               ← NetworkGameManager (registra carta per animazione)
```

---

### 8.7 Shop

#### `ShopManager`
Controlla acquisti (limite per round, affordability) e delega il refresh del pool a `ShopPool`. Usa `_purchasesByActor: Dictionary<int,int>` resettato a ogni `EnterShop()`.

#### `ShopPool`
Stateless (metodi statici). `GeneratePool` filtra per `MinCreditsRequired <= credits`, rimischia, prende i primi `size`. `RefreshSlots` rimuove slot casuali e aggiunge nuovi candidati (rispettando Credits aggiornati e non duplicando carte già nel pool).

---

### 8.8 UI

#### `GameView`
Bridge puro: legge `GameStateDTO`, scrive intent su `NetworkGameManager`. Non conosce `GameStateManager`. Si ridisegna interamente a ogni `GameStateSyncedEvent`.

**Flusso selezione bersaglio multi-step (Rappresentante di Classe, Sciopero, ecc.):**
1. Click carta → `OnPlayCardClicked` → `EnterTargetSelectionMode`
2. Se `RequiresEnemyTargetSelection`: `ShowEnemySelection()` → attende `OnEnemyCommanderSelected`
3. Se la carta richiede anche `RequiresOwnTargetSelection`: `ShowOwnSelection()` → attende `OnOwnCommanderSelected`
4. Invia intent con bersagli in ordine [enemy..., own...]; `GameContext` smista per lato

**`AnimatePlayedCard`:** usa `PlayedCardSequence` (monotonicamente crescente) per rilevare nuove carte. Al primo sync inizializza senza animare (evita animazione fantasma alla connessione).

---

## Riepilogo priorità

| ID | Tipo | Descrizione breve | Priorità |
|---|---|---|---|
| BUG-01 | Bug | NetworkSerializer: Kind/SecondaryUnlocked mancanti | 🔴 CRITICO |
| BUG-02 | Bug | CardRegistry non copre comandanti da CommanderSelect | 🔴 CRITICO |
| BUG-03 | Bug | Tutor/Rappresentante non triggerano passiva Inglese/Storia | 🟠 ALTO |
| BUG-04 | Bug | IsCooperCard usa nome stringa fragile | 🟡 MEDIO |
| BUG-05 | Dead code | ReturnCardToHandChange orfano | 🟢 BASSO |
| BUG-06 | Incoerenza | GameConfigSO.StartingCardsPerCommander inutilizzato | 🟢 BASSO |
| AMB-01 | Design | Wikipedia: effetto si risolve anche quando intercetta | ❓ DA DECIDERE |
| AMB-02 | Design | TiebreakRule.SumNotes non implementata | ❓ DA DECIDERE |
| RISK-01 | Runtime | EventBus.Clear() mai chiamato (scene reload) | 🟡 MEDIO |
| RISK-02 | Runtime | GetCardsPlayablePerTurn può restituire 0 | 🟡 MEDIO |
| RISK-03 | Runtime | UpdateHandSpacing: divisione per zero se _handCardsBeforeOverlap=0 | 🟡 MEDIO |
| RISK-04 | Design | Nessun cap sulla dimensione della mano | 🟢 BASSO |
| RISK-05 | Design | ShopPool non esclude carte già possedute | 🟢 BASSO |
| RISK-06 | Manutenzione | CommanderWithLowestNote duplicata 3 volte | 🟢 BASSO |
| PERF-01 | Performance | RenderHand: Destroy+Instantiate a ogni sync | 🟡 MEDIO |
