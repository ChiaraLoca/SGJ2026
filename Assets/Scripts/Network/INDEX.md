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
| [GameStateDTO.cs](GameStateDTO.cs) | struct `GameStateDTO`, `PlayerDTO`, `CommanderDTO` | Snapshot completo e serializzabile (solo primitivi/array): fase, round, attore attivo, giocatori, esito |
| [INetworkTransport.cs](INetworkTransport.cs) | interfaccia `INetworkTransport` | Seam di rete: `IsHost`, `LocalActorNumber`, `SendIntent()`, `BroadcastState()`, eventi `IntentReceived`/`StateReceived` |
| [LocalLoopbackTransport.cs](LocalLoopbackTransport.cs) | `LocalLoopbackTransport` | Implementazione offline a giro chiuso (host locale, single-actor). Per test e single-screen |
| [GameStateDtoBuilder.cs](GameStateDtoBuilder.cs) | `static GameStateDtoBuilder` | `Build(state, registry)`: costruisce il `GameStateDTO` dallo stato vivo |
| [GameStateSyncedEvent.cs](GameStateSyncedEvent.cs) | struct `GameStateSyncedEvent` | Evento EventBus consumato dalla UI: `State`, `LocalActorNumber` |
| [NetworkGameManager.cs](NetworkGameManager.cs) | `NetworkGameManager : MonoBehaviour` | Orchestratore. API UI `Submit*()`; host: `ProcessIntent()` (guard `IsHost`) → manager Core → `BroadcastState()`. `DefaultExecutionOrder(100)` |

## Note

- **Solo l'host** esegue gli intent (`if (!_transport.IsHost) return;`); dopo ogni azione fa `BroadcastState()`.
- Per il vero 1v1: installare PUN2 e aggiungere una `PhotonTransport : INetworkTransport` (usa `PhotonEventCodes`); nient'altro cambia.
- I codici evento Photon esistono già come costanti, ma la serializzazione Photon va scritta con la `PhotonTransport`.
