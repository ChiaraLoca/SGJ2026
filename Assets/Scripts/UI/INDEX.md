# INDEX — UI

Bridge UI↔logica: **nessuna logica di gioco**, solo presentazione e raccolta intent.
DTO-driven (ascolta `GameStateSyncedEvent`) e prefab-first.
Torna all'[indice generale](../INDEX.md).

## File

| File | Tipo | Responsabilità / API chiave |
|---|---|---|
| [CardView.cs](CardView.cs) | `CardView : MonoBehaviour` | Vista prefab di una carta: `Bind(card, onClick, interactable)` mostra l'artwork completo quando disponibile, altrimenti i placeholder testuali. Prefab in `Assets/Prefabs/UI/CardView.prefab` |
| [CommanderView.cs](CommanderView.cs) | `CommanderView : MonoBehaviour` | Vista comandante: `Bind(CommanderDTO, CommanderDataSO, actorNumber, commanderIndex)` → nome, Note, ritratto, debuff. `SetSelectable(bool, Action<int,int>)` attiva/disattiva l'overlay di selezione bersaglio |
| [GameAudioController.cs](GameAudioController.cs) | `GameAudioController : MonoBehaviour` | Controller persistente evento-driven: musica principale in loop, crowd per menu/selezione comandante, SFX su carta, Verifica, acquisto, Shop ed esito locale da `GameStateSyncedEvent`. Auto-bind clip in editor da `Assets/Audio/*` |
| [GameView.cs](GameView.cs) | `GameView : MonoBehaviour` | HUD principale. Si ridisegna su `GameStateSyncedEvent`, istanzia le carte di mano/shop (inclusa Verifica), inoltra le azioni a `NetworkGameManager.Submit*()`. Gestisce la selezione bersaglio per carte `SelectedCommanders`. `DefaultExecutionOrder(200)` |

## Note

- Usa `UnityEngine.UI` (Text/Button/Image legacy). Migrabile a TextMeshPro.
- Le carte (`CardView`) sono **istanziate** da prefab `[SerializeField]`; il resto della HUD è in scena (vedi memoria `project_scene_run_setup`).
- **Verifica in mano**: la carta Verifica è renderizzata in `RenderHand` come le altre; click → `SubmitPlayVerifica()`. Il bottone separato è disattivato via `SetActive(false)`.
- **Selezione bersaglio**: carte con `RequiresTargetSelection=true` entrano in modalità selezione (overlay giallo sui CommanderView); il click sul comandante invia l'intent con i target.
