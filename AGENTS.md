# AGENTS.md — 4E: Le Menti

Questo file guida Codex nel progetto Unity. Leggilo integralmente prima di generare qualsiasi codice.

---

## Contesto Progetto

**Titolo:** 4E: Le Menti
**Tipo:** Gioco di carte mobile 1v1, turn-based
**Engine:** Unity (ultima LTS stabile)
**Linguaggio:** C# 9+
**Networking:** Photon PUN2 (Host-Authoritative)
**Target:** Android / iOS
**Contesto:** Game Jam 3 giorni — il codice deve essere pulito e manutenibile nonostante i tempi stretti.

---

## Architettura di Riferimento

```
Assets/
├── Scripts/
│   ├── Core/               # GameStateManager, RoundManager, TurnManager
│   ├── Cards/              # CardData, CardEffect e sottoclassi, EffectResolver
│   ├── Commanders/         # CommanderState, CommanderController
│   ├── Players/            # PlayerState, PlayerController
│   ├── Shop/               # ShopManager, ShopPool
│   ├── Network/            # NetworkGameManager, GameStateDTO, PhotonEventCodes
│   ├── Events/             # EventBus, GameEvents (tipi evento)
│   ├── Config/             # GameConfig SO, costanti globali
│   └── UI/                 # (solo bridge UI↔logica, no logica di gioco qui)
├── ScriptableObjects/
│   ├── Cards/
│   ├── Effects/
│   ├── Conditions/
│   └── Config/
└── Prefabs/
    ├── Cards/
    ├── Commanders/
    └── UI/
```

### Pattern Architetturali Obbligatori

| Pattern | Dove si usa |
|---|---|
| **ScriptableObject** | CardData, CardEffect, CardCondition, GameConfig |
| **Strategy** | CardEffect (Apply polimorfismo) |
| **Command** | Photon network events (intent → azione serializzabile) |
| **EventBus** | Comunicazione decoupled tra sistemi (no dipendenze dirette) |
| **Host-Authoritative** | Solo MasterClient esegue logica di gioco; client manda intent |
| **DTO** | GameStateDTO serializzato e broadcastato dopo ogni azione |

---

## Indice del Codice (INDEX.md) — OBBLIGATORIO

Il codice è mappato in file `INDEX.md`:
- **Indice generale:** `Assets/Scripts/INDEX.md` — panoramica, mappa "dove trovo X", flusso runtime, link ai sottoindici.
- **Sottoindici per cartella:** un `INDEX.md` in ognuna di `Cards/`, `Commanders/`, `Config/`, `Core/`, `Events/`, `Network/`, `Players/`, `Shop/`, `UI/` — un file per riga con tipo, responsabilità e API chiave.

### Regole
1. **Leggi prima di agire:** prima di scrivere nuovo codice o cercare una funzionalità, consulta `Assets/Scripts/INDEX.md` e il sottoindice della cartella pertinente. Serve a trovare in fretta ciò che esiste ed **evitare duplicati**.
2. **Mantieni aggiornato:** ogni volta che aggiungi, rinomini o sposti un file, o cambi la responsabilità/API pubblica di una classe, **aggiorna nello stesso commit** il sottoindice della cartella e, se cambia la mappa funzionalità o il flusso, anche l'indice generale.
3. **Nuova cartella di script** → crea il suo `INDEX.md` e aggiungila alla tabella dei sottoindici nell'indice generale.

---

## Regole di Codice — NON DEROGABILI

### 1. Lingua
- **Codice, nomi, API:** inglese (classi, metodi, variabili, costanti, enum, file)
- **Commenti XML `///`:** italiano
- **Commenti inline `//`:** italiano
- **Log e messaggi di errore:** inglese

```csharp
/// <summary>
/// Applica l'effetto della carta al contesto di gioco corrente.
/// </summary>
/// <param name="context">Contesto contenente lo stato completo del gioco.</param>
public abstract void Apply(GameContext context);
```

### 2. Documentazione XML Obbligatoria
Ogni `class`, `struct`, `enum`, metodo `public` o `internal`, e property pubblica **deve** avere il blocco `/// <summary>`.
I parametri non ovvi usano `/// <param>` e `/// <returns>`.

```csharp
/// <summary>
/// Gestore dello stato globale della partita. Singleton accessibile da tutti i sistemi.
/// </summary>
public class GameStateManager : MonoBehaviour { ... }
```

### 3. No Magic Numbers
Nessun letterale numerico sparso nel codice. Tutto in `GameConfig` (ScriptableObject) o in costanti statiche.

```csharp
// ❌ VIETATO
if (roundIndex >= 3) EndGame();

// ✅ CORRETTO
if (roundIndex >= GameConfig.Instance.MaxRounds) EndGame();
```

```csharp
/// <summary>Costanti di gioco non configurabili a runtime.</summary>
public static class GameConstants
{
    public const int CommandersPerPlayer = 2;
    public const int StartingCardsPerCommander = 5;
    public const int CardsInStartingHand = 10; // 2 comandanti × 5
    public const int RoundsBeforeFinalExam = 3;
}
```

### 4. Niente GameObject.Find / FindObjectOfType
Le dipendenze si risolvono **solo** in questi modi (in ordine di preferenza):
1. **Inspector** → `[SerializeField] private MySystem _mySystem;`
2. **Singleton esplicito** → `GameStateManager.Instance`
3. **EventBus** → per comunicazione one-to-many

```csharp
// ❌ VIETATO
var manager = GameObject.Find("GameStateManager").GetComponent<GameStateManager>();

// ✅ CORRETTO — via Inspector
[SerializeField] private GameStateManager _gameStateManager;

// ✅ CORRETTO — via Singleton
GameStateManager.Instance.DoSomething();
```

### 5. Prefab-First
Ogni entità visiva che appare in scena **deve** essere un prefab.
- Nessun oggetto visivo viene creato interamente via codice senza un prefab di riferimento.
- `Instantiate()` accetta sempre un prefab da `[SerializeField]`, mai una stringa.

```csharp
// ❌ VIETATO
Instantiate(Resources.Load("Prefabs/Card"));

// ✅ CORRETTO
[SerializeField] private CardView _cardPrefab;
var card = Instantiate(_cardPrefab, _handContainer);
```

### 6. Naming Conventions

| Tipo | Convenzione | Esempio |
|---|---|---|
| Classi / Struct | PascalCase | `CardEffectResolver` |
| Interfacce | IPascalCase | `ICardEffect` |
| Enum | PascalCase | `GamePhase.Shop` |
| Metodi pubblici | PascalCase | `ApplyEffect()` |
| Metodi privati | PascalCase | `ValidateIntent()` |
| Campi privati | `_camelCase` | `_currentRound` |
| Proprietà pubbliche | PascalCase | `CurrentRound` |
| Costanti | SCREAMING_SNAKE | `MAX_ROUNDS` *(solo GameConstants)* |
| SerializeField | `_camelCase` | `[SerializeField] private int _shopSlots` |
| Coroutine | suffisso `Routine` | `DrawCardsRoutine()` |

### 7. Null Safety
- Preferire `TryGet` pattern invece di controlli null sparsi.
- Usare `?.` e `??` dove appropriato.
- Mai assunzione implicita che un riferimento sia non-null senza `[SerializeField]` o assegnazione garantita.

### 8. MonoBehaviour — Regole
- Nessuna logica di business in `Update()` salvo input polling.
- `Awake()` → inizializzazione riferimenti interni.
- `Start()` → registrazione eventi, setup post-inizializzazione.
- `OnDestroy()` → **sempre** disiscrizione da EventBus e Photon callbacks.

```csharp
private void OnDestroy()
{
    EventBus.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
    PhotonNetwork.RemoveCallbackTarget(this);
}
```

### 9. Serializzazione Rete
- I dati di rete viaggiano solo come `GameStateDTO` (struct serializzabile).
- Nessun riferimento a MonoBehaviour o UnityEngine.Object nei DTO.
- Usare tipi primitivi o array di primitivi per la serializzazione Photon.

---

## Regole Specifiche per Sistema

### CardEffect
- Ogni effetto è un **ScriptableObject separato**, non un enum + switch.
- `Apply(GameContext ctx)` è puro: legge dal context, restituisce modifiche tramite `ctx.RegisterChange()`, non tocca direttamente lo stato.
- Gli effetti con durata registrano un `ActiveEffect` sul `CommanderState`.

### EventBus
- Usare tipi evento `struct` fortemente tipizzati, non stringhe.
- Esempio: `EventBus.Publish(new CardPlayedEvent { Card = cardData, Player = playerState });`

### Photon / Networking
- Tutti i codici evento Photon sono costanti in `PhotonEventCodes` (byte).
- Solo il MasterClient esegue `ProcessIntent()` — aggiungere sempre il guard: `if (!PhotonNetwork.IsMasterClient) return;`
- Dopo ogni azione l'host chiama `BroadcastState()`.

### GameConfig (ScriptableObject)
Tutti i valori di bilanciamento vivono qui:
- `maxRounds`, `startingCardsPerCommander`, `cardsPlayablePerTurn[]` (array per round), `shopSlots`, `shopRefreshCost`, ecc.

---

## Cosa NON Fare (Jam Anti-Patterns)

- ❌ Classi "God Object" — se una classe supera ~200 righe, spaccarla
- ❌ Static ovunque — solo per Singleton e costanti
- ❌ Coroutine per logica di stato — usare state machine o flag espliciti
- ❌ `Debug.Log` lasciati in produzione — wrappare in un `GameLogger` con flag `[Conditional("DEVELOPMENT_BUILD")]`
- ❌ Hardcodare ID carte come stringhe — usare riferimenti SO diretti

---

## Workflow Atteso da Codex

1. **Prima di scrivere o cercare codice:** consultare `Assets/Scripts/INDEX.md` e il sottoindice della cartella pertinente (cosa esiste già, dove sta); verificare che la struttura file rispetti `Assets/Scripts/` come da architettura
2. **Ogni nuovo file:** header con namespace corretto + `using` minimi necessari
3. **Ogni metodo pubblico:** blocco `///` prima di scrivere il corpo
4. **Ogni costante numerica:** verificare se appartiene a `GameConstants` o `GameConfig`
5. **Ogni `Instantiate`:** verificare che il prefab venga da `[SerializeField]`
6. **Dopo ogni modifica strutturale** (file nuovo/rinominato/spostato, cambio di responsabilità o API pubblica): aggiornare il sottoindice `INDEX.md` della cartella e, se serve, l'indice generale
7. **Commit:** **NO `Co-Authored-By`** footer. Messaggi chiari, concisi. Ogni commit deve essere logicamente coerente (una feature, una fix, una refactoring — non mescolare).


