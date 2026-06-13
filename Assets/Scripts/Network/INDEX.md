# INDEX — Network

Livello di rete host-authoritative, **Photon-agnostico** dietro un'astrazione di trasporto.
Gira offline col loopback; Photon si innesta sostituendo solo `INetworkTransport`.
Torna all'[indice generale](../INDEX.md).

## File

| File | Tipo | Responsabilità / API chiave |
|---|---|---|
| [PhotonEventCodes.cs](PhotonEventCodes.cs) | `static PhotonEventCodes` | Costanti `byte` dei codici evento: intent bassi (`PlayCard`…`FinishShop`), broadcast alti (`StateSync`, `GameStart`, `GameOver`) |
| [CardRegistry.cs](CardRegistry.cs) | `CardRegistry` | Mappa stabile id↔`CardDataSO` (gli SO non viaggiano in rete). `Build(content)`, `GetId()`, `GetCard()`, `ToIds()`, `NoCard = -1` |
| [GameIntent.cs](GameIntent.cs) | `enum IntentType` + struct `GameIntent` | Comando serializzabile (Command). Factory: `PlayCard()`, `BuyCard()`, `PlayVerifica()`, `EndTurn()`, `FinishShop()`. Bersagli come coppie (attore, indice comandante) |
| [GameStateDTO.cs](GameStateDTO.cs) | struct `GameStateDTO`, `PlayerDTO`, `CommanderDTO` | Snapshot completo e serializzabile: fase, round, attore attivo, azioni rimanenti, conteggi mazzo/scarti, giocatori, esito e ultima carta giocata |
| [INetworkTransport.cs](INetworkTransport.cs) | interfaccia `INetworkTransport` | Seam di rete: `IsHost`, `LocalActorNumber`, `SendIntent()`, `BroadcastState()`, eventi `IntentReceived`/`StateReceived`/`ClientJoined` (resync late-join) |
| [LocalLoopbackTransport.cs](LocalLoopbackTransport.cs) | `LocalLoopbackTransport` | Implementazione offline a giro chiuso (host locale, single-actor). Per test unitari e riferimento |
| [HotseatTransport.cs](HotseatTransport.cs) | `HotseatTransport` | Hotseat locale: `LocalActorNumber` si aggiorna a `state.ActiveActorNumber` ad ogni broadcast, la UI segue il giocatore attivo |
| [PhotonTransport.cs](PhotonTransport.cs) | `PhotonTransport` (`#if PHOTON_UNITY_NETWORKING`) | Implementazione PUN2 online host-authoritative. Intent→MasterClient, stato→Others via `RaiseEvent`; MasterClient = host (attore di gioco 0), ospite = 1. `RequestInitialState()` per il resync. `IDisposable` |
| [SessionConfig.cs](SessionConfig.cs) | `enum NetworkMode` + `static SessionConfig` | Scelte del menu che sopravvivono al cambio scena: `Mode` (Hotseat/Online), `RoomCode`. Lette dal `NetworkGameManager` per scegliere il transport |
| [NetworkSerializer.cs](NetworkSerializer.cs) | `static NetworkSerializer` | `byte[]`↔`GameIntent`/`GameStateDTO` con `BinaryWriter`/`Reader`. Indipendente da Photon |
| [OnlineLauncher.cs](OnlineLauncher.cs) | `OnlineLauncher : MonoBehaviour` | Lato menu: connessione cloud + accoppiamento per codice stanza (`HostRoom`/`JoinExistingRoom`). A 2 giocatori l'host fa `LoadLevel`. Compila sempre; logica PUN2 sotto `#if`. Eventi `StatusChanged`/`Failed` |
| [GameStateDtoBuilder.cs](GameStateDtoBuilder.cs) | `static GameStateDtoBuilder` | `Build(state, registry)`: costruisce il `GameStateDTO` dallo stato vivo |
| [GameStateSyncedEvent.cs](GameStateSyncedEvent.cs) | struct `GameStateSyncedEvent` | Evento EventBus consumato dalla UI: `State`, `LocalActorNumber` |
| [NetworkGameManager.cs](NetworkGameManager.cs) | `NetworkGameManager : MonoBehaviour` | Orchestratore. API UI `Submit*()`; host: `ProcessIntent()` (guard `IsHost`) → manager Core → `BroadcastState()`. Registra nei DTO carta e attore dell'ultima giocata per entrambi i client. `DefaultExecutionOrder(100)` |

## Note

- **Solo l'host** esegue gli intent (`if (!_transport.IsHost) return;`); dopo ogni azione fa `BroadcastState()`.
- **Selezione del transport**: `NetworkGameManager.Awake()` legge `SessionConfig.Mode` → `HotseatTransport` (locale) o `PhotonTransport` (online). Il boot è guidato dal NetworkGameManager (`GameStateManager.AutoStartOffline=false`): l'host avvia la partita e broadcasta, il client online chiede lo stato (`RequestInitialState` + retry) e attende.
- **PUN2 è installato** (`Assets/Photon/`, define `PHOTON_UNITY_NETWORKING` attivo). **Manca solo l'App ID**: incollarlo in `Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset` → `AppSettings/AppIdRealtime` (free tier su photonengine.com). Senza App ID l'online non si connette; l'hotseat funziona comunque.
- Senza il define (PUN2 disinstallato) `PhotonTransport`/`OnlineLauncher` ripiegano in sicurezza: l'online segnala il fallimento e si usa l'hotseat. Il progetto compila in entrambi i casi.
