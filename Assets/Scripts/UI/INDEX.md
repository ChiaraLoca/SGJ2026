# INDEX — UI

Bridge UI↔logica: **nessuna logica di gioco**, solo presentazione e raccolta intent.
DTO-driven (ascolta `GameStateSyncedEvent`) e prefab-first.
Torna all'[indice generale](../INDEX.md).

## File

| File | Tipo | Responsabilità / API chiave |
|---|---|---|
| [CardView.cs](CardView.cs) | `CardView : MonoBehaviour` | Vista prefab di una carta: `Bind(...)` mostra l'artwork e distingue il tap dalla pressione prolungata; `BindPreview(card)` configura una copia ingrandita non interattiva. Prefab in `Assets/Prefabs/UI/CardView.prefab` |
| [CardPlayAnimationController.cs](CardPlayAnimationController.cs) | `CardPlayAnimationController` | Coda di presentazione delle carte giocate: movimento dalla zona del giocatore al centro, ingrandimento, permanenza e dissolvenza |
| [CommanderView.cs](CommanderView.cs) | `CommanderView : MonoBehaviour` | Vista comandante: `Bind(CommanderDTO, CommanderDataSO, actorNumber, commanderIndex)` → nome, Note, ritratto, debuff. `SetSelectable(bool, Action<int,int>)` attiva/disattiva l'overlay di selezione bersaglio |
| [GameAudioController.cs](GameAudioController.cs) | `GameAudioController : MonoBehaviour` | Controller persistente evento-driven: musica principale in loop, crowd per menu/selezione comandante, SFX su carta, Verifica, acquisto, Shop ed esito locale da `GameStateSyncedEvent`. Auto-bind clip in editor da `Assets/Audio/*` |
| [GameView.cs](GameView.cs) | `GameView : MonoBehaviour` | HUD principale. Mostra crediti, mazzo/scarti locali, azioni rimanenti solo al giocatore attivo, mano con sovrapposizione adattiva, mercato 4x2 durante `Shop`, anteprima/animazioni carte e inoltro intent |
| [MainMenuController.cs](MainMenuController.cs) | `MainMenuController : MonoBehaviour` | Menu iniziale (scena `MainMenu`). Sceglie tra **Stesso telefono** (hotseat → carica `SampleScene`) e **Online** (codice stanza → `OnlineLauncher`). Imposta `SessionConfig.Mode`/`RoomCode`; genera il codice stanza; pannelli modalità/stanza |
| [SafeAreaController.cs](SafeAreaController.cs) | `SafeAreaController : MonoBehaviour` | Da collegare al Canvas principale di ogni scena. Adatta il pannello figlio `_panel` alla `Screen.safeArea` del dispositivo (notch, home bar, Dynamic Island). Si ricalcola automaticamente al cambio di orientamento. `DefaultExecutionOrder(-100)` |

## Note

- Usa `UnityEngine.UI` (Text/Button/Image legacy). Migrabile a TextMeshPro.
- Le carte (`CardView`) sono **istanziate** da prefab `[SerializeField]`; il resto della HUD è in scena (vedi memoria `project_scene_run_setup`).
- **Verifica in mano**: la carta Verifica è renderizzata in `RenderHand` come le altre; click → `SubmitPlayVerifica()`. Il bottone separato è disattivato via `SetActive(false)`.
- **Selezione bersaglio**: carte con `RequiresTargetSelection=true` entrano in modalità selezione (overlay giallo sui CommanderView); il click sul comandante invia l'intent con i target.
- **Anteprima carta**: pressione prolungata con touch o mouse mostra una copia ingrandita al centro del Canvas; il rilascio la chiude senza giocare/acquistare la carta.
- **Mercato**: `ShopContainer` e le sue carte sono attivi solo durante la fase `Shop`.
- **Layout carte**: oltre quattro carte la mano riduce dinamicamente lo spacing per restare nel contenitore; lo Shop usa una griglia da quattro colonne.
- **Mazzo e scarti**: due indicatori locali mostrano i conteggi aggiornati dallo snapshot di rete.
- **Carta giocata**: ogni nuovo `PlayedCardSequence` ricevuto nel DTO anima la carta locale dalla mano e quella avversaria dalla zona alta del campo; le animazioni sono accodate.
- **Crediti avversari**: il valore mostrato viene aggiornato all'inizio del round, al cambio prospettiva hotseat e all'Esame Finale.
- **Azioni rimanenti**: il contatore è visibile esclusivamente al giocatore attivo durante la fase `Play`.
- **Menu iniziale**: la scena `Assets/Scenes/MainMenu.unity` (prima in Build Settings) ospita `MainMenuController` + `OnlineLauncher`. "Stesso telefono" carica `SampleScene` in hotseat; "Online" connette via Photon per codice stanza e poi `PhotonNetwork.LoadLevel("SampleScene")`.
