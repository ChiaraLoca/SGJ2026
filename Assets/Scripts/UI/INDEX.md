# INDEX — UI

Bridge UI↔logica: **nessuna logica di gioco**, solo presentazione e raccolta intent.
DTO-driven (ascolta `GameStateSyncedEvent`) e prefab-first.
Torna all'[indice generale](../INDEX.md).

## File

| File | Tipo | Responsabilità / API chiave |
|---|---|---|
| [CardView.cs](CardView.cs) | `CardView : MonoBehaviour` | Vista prefab di una carta: `Bind(card, onClick, interactable)`. Prefab in `Assets/Prefabs/UI/CardView.prefab` |
| [CommanderView.cs](CommanderView.cs) | `CommanderView : MonoBehaviour` | Vista comandante: `Bind(CommanderDTO, CommanderDataSO)` → nome, Note, ritratto, indicatore debuff |
| [GameView.cs](GameView.cs) | `GameView : MonoBehaviour` | HUD principale. Si ridisegna su `GameStateSyncedEvent`, istanzia le carte di mano/shop, inoltra le azioni a `NetworkGameManager.Submit*()`. `DefaultExecutionOrder(200)` |

## Note

- Usa `UnityEngine.UI` (Text/Button/Image legacy). Migrabile a TextMeshPro.
- Le carte (`CardView`) sono **istanziate** da prefab `[SerializeField]`; il resto della HUD è in scena (vedi memoria `project_scene_run_setup`).
- Limite attuale: un solo attore locale (single-screen). Selettore bersagli per `SelectedCommanders` non ancora implementato — l'infrastruttura intent però lo supporta già.
