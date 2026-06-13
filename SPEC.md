# SPEC.md — 4E: Le Menti
## Game Design & Technical Specification

**Versione:** 0.1 — Game Jam Draft
**Data:** Giugno 2026

---

## 1. Concept

Due giocatori interpretano altrettanti **bulli** il cui obiettivo è superare l'esame finale di fine anno. Ognuno gestisce due **secchioni** (i propri Comandanti) potenziandoli con carte buff, mentre cerca di indebolire i secchioni avversari con carte debuff. Chi accumula più **Crediti** (punteggio permanente) dopo 3 round di Verifica vince la Partita.

---

## 2. Entità Principali

### 2.1 Giocatore (Player)
| Campo | Tipo | Note |
|---|---|---|
| Credits | int | Punteggio permanente. Aumenta solo alla Verifica. |
| Hand | List\<CardData\> | Mano corrente. |
| Deck | List\<CardData\> | Mazzo personale (mischiate). |
| ShopPool | List\<CardData\> | Pool separato e randomico. Filtrato da `minCreditsRequired`. |
| Commanders | CommanderState[2] | I due secchioni del giocatore. |

### 2.2 Comandante (Commander)
| Campo | Tipo | Note |
|---|---|---|
| Note | int | Punteggio temporaneo. Resetta tra round (dopo conversione). |
| BaseNote | int | Nota di partenza del comandante (configurabile per SO). |
| ActiveBuffs | List\<ActiveEffect\> | Buff attivi con durata residua. |
| ActiveDebuffs | List\<ActiveEffect\> | Debuff attivi con durata residua. |
| LinkedDeckSlice | List\<CardData\> | Le 5 carte di partenza legate a questo comandante. |

### 2.3 Carta (Card)
| Campo | Tipo | Note |
|---|---|---|
| CardName | string | |
| Description | string | Testo narrativo / regola. |
| CardType | enum | `Standard` \| `Verifica` |
| CommanderAffinity | int? | 0, 1, o null (carta neutra). |
| ShopCost | int | Costo in Note (pagato prima della conversione). |
| MinCreditsRequired | int | Soglia minima di Credits per apparire nel pool shop. |
| Effects | CardEffect[] | Array di effetti applicati in sequenza. |

---

## 3. Setup Partita

1. Ogni giocatore sceglie (o riceve) **2 Comandanti**.
2. Ogni Comandante porta le proprie **5 carte** → mano iniziale di **10 carte**.
3. Ogni giocatore riceve **1 carta Verifica** (non parte del mazzo, slot dedicato).
4. I mazzi vengono mischiati separatamente.
5. Il pool shop di ogni giocatore viene generato: N carte randomiche dal catalogo, filtrate da `MinCreditsRequired ≤ Credits attuali`.
6. Si determina casualmente chi va per primo.

---

## 4. Struttura della Partita

```
Partita
└── Round ×3
    ├── Fase PLAY       (turni alternati)
    ├── Fase VERIFICA   (chi gioca la carta Verifica chiude il round)
    ├── Fase SHOP       (acquisti prima della conversione)
    └── Fase DRAW       (nuove carte, reset parziale)
└── ESAME FINALE       (chi ha più Credits vince)
```

### 4.1 Fase PLAY

- I giocatori si alternano.
- Per turno si possono giocare **Y carte** (valore configurabile per round).
  - Round 1: Y = `cardsPlayablePerTurn[0]` (es. 2)
  - Round 2: Y = `cardsPlayablePerTurn[1]` (es. 3)
  - Round 3: Y = `cardsPlayablePerTurn[2]` (es. 4)
- Giocare una carta: scegliere carta dalla mano → scegliere target (comandante proprio o avversario, a seconda dell'effetto) → `EffectResolver.Resolve()`.
- La carta **Verifica** è giocabile in qualsiasi momento del proprio turno; farlo termina immediatamente la Fase PLAY.

### 4.2 Fase VERIFICA

Triggered quando un giocatore gioca la carta Verifica.

1. Si interrompe la Fase PLAY.
2. Viene rivelata la Note finale di ogni Comandante (BaseNote ± buff ± debuff attivi).
3. Il giocatore che ha giocato la Verifica riceve bonus/malus dalla Verifica stessa (effetto carta).
4. Si procede alla Fase SHOP.

### 4.3 Fase SHOP

1. Le Note di ogni Comandante sono ancora "attive" come valuta.
2. Il giocatore può acquistare **M carte** dal proprio ShopPool.
   - Il costo di ogni carta viene scalato dalle Note del Comandante con affinità (o dalle Note totali se la carta è neutra).
   - Si possono acquistare carte anche spendendo Note di comandanti diversi (TBD: regola da bilanciare).
3. Al termine dello shop, le Note vengono convertite in Credits (1:1 o con moltiplicatore — configurabile in GameConfig).
4. Il pool shop viene parzialmente rinfrescato (N_refresh carte nuove, filtrate da Credits aggiornati).

### 4.4 Fase DRAW

1. Le carte rimanenti in mano vengono scartate (oppure parzialmente tenute — TBD).
2. Ogni giocatore pesca dal proprio mazzo fino alla dimensione mano stabilita (configurabile).
3. Le carte acquistate nel corso della partita vengono mischiate nel mazzo.
4. I buff/debuff con durata scaduta vengono rimossi.
5. Si passa al round successivo (o all'Esame Finale se erano 3 round).

### 4.5 Esame Finale

- Avviene dopo il terzo round.
- Nessuna ulteriore azione.
- Il giocatore con più **Credits** vince.
- In caso di parità: TBD (es. somma Note finali come spareggio).

---

## 5. Sistema Carte ed Effetti

### 5.1 Tipi di Effetto

| EffectType | Descrizione |
|---|---|
| `Buff` | Aumenta la Note di un Comandante (proprio o avversario) |
| `Debuff` | Riduce la Note di un Comandante |
| `Draw` | Pesca N carte dal mazzo |
| `Special` | Effetto non standard (vedi carte specifiche) |

### 5.2 Target

| Target | Descrizione |
|---|---|
| `OwnCommander0` | Comandante 0 del giocatore attivo |
| `OwnCommander1` | Comandante 1 del giocatore attivo |
| `EnemyCommander0` | Comandante 0 dell'avversario |
| `EnemyCommander1` | Comandante 1 dell'avversario |
| `AllOwnCommanders` | Entrambi i propri comandanti |
| `AllEnemyCommanders` | Entrambi i comandanti avversari |
| `AllCommanders` | Tutti e quattro i comandanti |
| `ActivePlayer` | Il giocatore che sta giocando la carta |
| `InactivePlayer` | L'avversario |

### 5.3 Durata Effetti

- **Istantanea:** applicata e dimenticata subito.
- **Durata N turni:** registrata come `ActiveEffect` sul CommanderState; decrementata a ogni fine turno del giocatore che la subisce.
- **Permanente (fino a Verifica):** rimossa solo al reset post-Verifica.

### 5.4 Effetti Condizionali

`ConditionalEffect` contiene un riferimento a `CardCondition` (ScriptableObject).

Esempi di condizioni:
- `IfNoteAboveThreshold` — se la Note del target supera X
- `IfOwnsCardsInHand` — se il giocatore ha almeno N carte in mano
- `IfRoundIndex` — se il round corrente è X
- `IfCommanderHasDebuff` — se il comandante target ha debuff attivi

### 5.5 Effetti Trigger

`TriggerEffect` si iscrive all'EventBus su un evento specificato:
- `OnCardPlayed` — quando qualsiasi carta viene giocata
- `OnRoundEnd` — alla fine del round
- `OnVerificaPlayed` — quando viene giocata la Verifica
- `OnEnemyBuysCard` — quando l'avversario acquista

---

## 6. Sistema Shop

### 6.1 Pool Personale
- Generato all'inizio della partita per ogni giocatore.
- Pool di **N** carte (configurabile, es. 12).
- Filtrato: mostrate solo le carte con `MinCreditsRequired ≤ Credits attuali`.
- Se una carta viene acquistata, sparisce dal pool (non rientra).

### 6.2 Acquisto
- Il giocatore può acquistare fino a **M** carte per round (configurabile).
- Il costo è in Note, scalato prima della conversione in Credits.
- Le carte acquistate entrano nel mazzo alla Fase DRAW successiva.

### 6.3 Refresh
- A ogni Fase SHOP un numero configurabile di slot del pool vengono sostituiti con nuove carte casuali (rispettando `MinCreditsRequired`).

---

## 7. Networking

### 7.1 Stack
- **Photon PUN2**
- **Authority:** Host-Authoritative (MasterClient = Host)

### 7.2 Flusso Rete

```
Client                          Host (MasterClient)
  |                                     |
  |--- RaiseEvent(PLAY_CARD, data) ---->|
  |                             Valida intent
  |                             Esegue EffectResolver
  |                             Aggiorna GameState
  |<--- RaiseEvent(STATE_SYNC, DTO) ----|
  |                                     |
Applica DTO                       Applica DTO
```

### 7.3 Codici Evento Photon (PhotonEventCodes.cs)

| Costante | Byte | Direzione | Payload |
|---|---|---|---|
| `PLAY_CARD` | 1 | Client→Host | `{ cardId: string, commanderTargetIndex: int }` |
| `BUY_CARD` | 2 | Client→Host | `{ cardId: string }` |
| `PLAY_VERIFICA` | 3 | Client→Host | `{ cardId: string }` |
| `END_TURN` | 4 | Client→Host | `{}` |
| `STATE_SYNC` | 10 | Host→All | `GameStateDTO` serializzato |
| `GAME_START` | 11 | Host→All | `GameStateDTO` iniziale |
| `GAME_OVER` | 12 | Host→All | `{ winnerActorNumber: int }` |

### 7.4 GameStateDTO
Struct serializzabile, nessun riferimento Unity. Contiene snapshot completo dello stato necessario per ricostruire la view del client.

---

## 8. Configurazione (GameConfig ScriptableObject)

| Parametro | Default | Note |
|---|---|---|
| `maxRounds` | 3 | Numero di Verifiche prima dell'Esame Finale |
| `commandersPerPlayer` | 2 | |
| `startingCardsPerCommander` | 5 | |
| `cardsPlayablePerTurn` | [2, 3, 4] | Array, un valore per round |
| `shopPoolSize` | 12 | Carte totali nel pool personale |
| `shopPurchasesPerRound` | 2 | Acquisti massimi per round |
| `shopRefreshSlots` | 3 | Slot refreshati ogni Fase SHOP |
| `noteToCreditsMultiplier` | 1.0 | Moltiplicatore conversione |
| `tiebreakRule` | `SumNotes` | Regola spareggio finale |

---

## 9. TBD / Decisioni Aperte

- [ ] Regola esatta di pesca a fine round (si scarta tutta la mano o si tiene parte?)
- [ ] Le Note di comandanti diversi si possono combinare per pagare carte neutre nel shop?
- [ ] Spareggio in caso di Credits pareggiati all'Esame Finale
- [ ] Numero esatto di carte nel catalogo base
- [ ] Effetto specifico carta Verifica (solo chiude il round o ha anche un effetto gameplay?)
- [ ] Vengono rivelate le Note avversarie durante la fase PLAY o solo alla Verifica?
