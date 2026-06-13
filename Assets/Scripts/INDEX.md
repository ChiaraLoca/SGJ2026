# INDEX — Assets/Scripts (4E: Le Menti)

Indice generale del codice. Ogni cartella ha un proprio `INDEX.md` (sottoindice) con il dettaglio file per file.
**Mantieni questo indice aggiornato** quando aggiungi/rinomini/sposti file o cambi responsabilità (vedi regola in `CLAUDE.md`).

---

## Sottoindici per cartella

| Cartella | Contenuto | Sottoindice |
|---|---|---|
| **Cards** | Carte, effetti (Strategy), condizioni, resolver | [Cards/INDEX.md](Cards/INDEX.md) |
| **Commanders** | Definizione e stato runtime dei comandanti | [Commanders/INDEX.md](Commanders/INDEX.md) |
| **Config** | Bilanciamento, costanti, archivio contenuti | [Config/INDEX.md](Config/INDEX.md) |
| **Core** | State machine, turni, round, setup, contesto | [Core/INDEX.md](Core/INDEX.md) |
| **Events** | EventBus e tipi evento | [Events/INDEX.md](Events/INDEX.md) |
| **Network** | Host-authoritative, intent, DTO, transport | [Network/INDEX.md](Network/INDEX.md) |
| **Players** | Stato runtime del giocatore | [Players/INDEX.md](Players/INDEX.md) |
| **Shop** | Acquisti e generazione/refresh pool | [Shop/INDEX.md](Shop/INDEX.md) |
| **UI** | Bridge UI↔logica (DTO-driven, prefab) | [UI/INDEX.md](UI/INDEX.md) |

---

## Mappa funzionalità — "dove trovo…?"

| Voglio… | File / tipo |
|---|---|
| Avviare una partita | `Core/GameStateManager.cs` → `StartMatch()` |
| Capire il flusso delle fasi (Play→Verifica→Shop→Draw→Esame) | `Core/PhaseManager.cs` |
| Giocare una carta / Verifica / fine turno | `Core/TurnManager.cs` |
| Aggiungere un nuovo tipo di effetto carta | `Cards/CardEffectSO.cs` (base) + nuova classe in `Cards/Effects/` |
| Aggiungere una condizione per effetti condizionali | `Cards/CardConditionSO.cs` (base) + nuova classe in `Cards/Conditions/` |
| Applicare una modifica di stato (Note, pesca, effetto a durata) | `Core/GameChanges.cs` (`IGameChange`) |
| Risolvere i bersagli di un effetto | `Core/GameContext.cs` → `ResolveCommanders()` / `ResolvePlayer()` |
| Acquisto shop / refresh pool | `Shop/ShopManager.cs`, `Shop/ShopPool.cs` |
| Conversione Note→Credits e Esame Finale | `Core/PhaseManager.cs` → `ConvertAndAdvance()`, `ResolveOutcome()` |
| Pubblicare/ascoltare un evento di gioco | `Events/EventBus.cs`, tipi in `Events/GameEvents.cs` |
| Inviare un'azione dal client all'host (intent) | `Network/NetworkGameManager.cs` → `Submit*()`, `Network/GameIntent.cs` |
| Snapshot di stato per la rete/UI | `Network/GameStateDTO.cs`, costruito da `Network/GameStateDtoBuilder.cs` |
| Cambiare il trasporto (offline → Photon) | `Network/INetworkTransport.cs` (impl: `LocalLoopbackTransport.cs`) |
| Ridisegnare la UI sullo stato | `UI/GameView.cs` (ascolta `GameStateSyncedEvent`) |
| Gestire musica/ambiente/SFX evento-driven | `UI/GameAudioController.cs` |
| Valori di bilanciamento (round, mano, shop, conversione) | `Config/GameConfigSO.cs` |
| Costanti strutturali (n. giocatori, comandanti, carte) | `Config/GameConstants.cs` |
| Contenuti della partita (comandanti, carte, catalogo) | `Config/GameContentSO.cs` |

---

## Flusso principale (runtime)

```
GameStateManager.StartMatch()
  → MatchSetup.BuildPlayer() ×2            (Players, Commanders, mazzo, mano, shop pool)
  → PhaseManager.BeginMatch()              (entra in Play, primo turno)

UI click → NetworkGameManager.Submit*()    (UI/GameView → Network)
  → INetworkTransport.SendIntent()         (loopback offline)
  → NetworkGameManager.ProcessIntent()     (SOLO host)
      → TurnManager / ShopManager / PhaseManager
          → EffectResolver.Resolve()       (effetti carta → GameChanges → commit)
  → NetworkGameManager.BroadcastState()    (GameStateDtoBuilder → DTO)
  → GameStateSyncedEvent (EventBus)
  → GameView.Render()                       (ridisegno UI)
```

Pattern architetturali e regole di codice: vedi `CLAUDE.md`. Specifica di gioco: vedi `SPEC.md`.
