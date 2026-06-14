# INDEX — UI

Bridge UI↔logica: **nessuna logica di gioco**, solo presentazione e raccolta intent.
DTO-driven (ascolta `GameStateSyncedEvent`) e prefab-first.
Torna all'[indice generale](../INDEX.md).

## File

| File | Tipo | Responsabilità / API chiave |
|---|---|---|
| [CardView.cs](CardView.cs) | `CardView : MonoBehaviour` | Vista prefab di una carta: root cliccabile con contenuti visuali sotto `CardMask` (`Image` + `Mask`, sprite `Assets/Cards/RoundedCard.png`); `Bind(...)` mostra artwork e costo, `BindPreview(card)` configura l'anteprima. Prefab in `Assets/Prefabs/UI/CardView.prefab` |
| [CardPlayAnimationController.cs](CardPlayAnimationController.cs) | `CardPlayAnimationController` | Coda di presentazione delle carte giocate: movimento dalla zona del giocatore al centro, ingrandimento, permanenza e dissolvenza |
| [CardDrawAnimationController.cs](CardDrawAnimationController.cs) | `CardDrawAnimationController` | Coda di presentazione delle carte pescate: movimento ad arco dall'icona mazzo alla posizione definitiva nella mano locale |
| [TargetHitEffect.cs](TargetHitEffect.cs) | `TargetHitEffect : MonoBehaviour` | Effetto prefab-first a X rossa pulsante mostrato sui comandanti locali colpiti durante la presentazione di una carta avversaria |
| [CommanderView.cs](CommanderView.cs) | `CommanderView : MonoBehaviour` | Vista comandante: `Bind(...)` → nome, Note, ritratto, debuff, cornice dorata persistente della secondaria e callback di pressione prolungata. `PlaySecondaryUnlockEffect()` riproduce il pulse di sblocco; `SetSelectable(...)` gestisce l'overlay bersaglio |
| [CommanderAbilityPopup.cs](CommanderAbilityPopup.cs) | `CommanderAbilityPopup : MonoBehaviour` | Riquadro mostrato tenendo premuto un comandante: passiva base, requisito, passiva secondaria e stato bloccata/sbloccata. Prefab in `Assets/Prefabs/UI/CommanderAbilityPopup.prefab` |
| [GameAudioController.cs](GameAudioController.cs) | `GameAudioController : MonoBehaviour` | Controller persistente evento-driven: musica principale in loop, crowd per menu/selezione comandante, SFX su carta, Verifica, acquisto, Shop ed esito locale da `GameStateSyncedEvent`. Auto-bind clip in editor da `Assets/Audio/*` |
| [CommanderSelectController.cs](CommanderSelectController.cs) | `CommanderSelectController : MonoBehaviour` (online: `IInRoomCallbacks` sotto `#if`) | Schermata di selezione (scena `CommanderSelectUI`, griglia 3x2 fino a 6 comandanti). Il tocco apre il dettaglio come popup centrale su `Assets/Background/Note.png`; il pulsante alterna selezione/deselezione e una `X` consente la chiusura senza cambiare scelta. **Hotseat**: i due giocatori scelgono a turno; salva in `SessionConfig.Player0/1Commanders` e carica `SampleUI`. **Online**: ciascuno sceglie sul proprio device, pubblica la scelta come Custom Property Photon (`"cmd"`); l'host raccoglie entrambe e carica il livello |
| [CommanderOptionView.cs](CommanderOptionView.cs) | `CommanderOptionView : MonoBehaviour` | Vista prefab di un comandante selezionabile: ritratto esteso sull'area principale e nome nella fascia inferiore; tutta la carta è cliccabile tramite `Bind(data, onSelected)`, evidenziazione con `SetSelected(bool)`. Prefab in `Assets/Prefabs/UI/CommanderOption.prefab` |
| [GameView.cs](GameView.cs) | `GameView : MonoBehaviour` | HUD principale. Mostra come soli valori numerici crediti, Note, mazzo/scarti locali e azioni rimanenti solo al giocatore attivo; gestisce mano con sovrapposizione adattiva, mercato 4x2 durante `Shop`, selezione bersagli annullabile, anteprima/animazioni carte e inoltro intent |
| [MainMenuController.cs](MainMenuController.cs) | `MainMenuController : MonoBehaviour` | Menu iniziale (scena `MainMenu`). **Stesso telefono** (hotseat → carica `CommanderSelectUI`, poi `SampleUI`) e **Online** (codice stanza → `OnlineLauncher`). Imposta `SessionConfig.Mode`/`RoomCode`; azzera le selezioni comandanti; genera il codice stanza; pannelli modalità/stanza |
| [SafeAreaController.cs](SafeAreaController.cs) | `SafeAreaController : MonoBehaviour` | Da collegare al Canvas principale di ogni scena. Adatta il pannello figlio `_panel` alla `Screen.safeArea` dopo l'inizializzazione del Canvas, gestendo dimensioni schermo non ancora valide; si ricalcola al cambio di orientamento. `DefaultExecutionOrder(-100)` |
| [EffectTestSceneManager.cs](EffectTestSceneManager.cs) | `EffectTestSceneManager : MonoBehaviour` | Gestore della scena di test effetti offline. Crea 2 giocatori con 2 comandanti ciascuno; permette applicare effetti carte istantaneamente senza turni. Risolve effetti via `EffectResolver`. Scena: `Assets/Scenes/EffectTest.unity` |
| [EffectTestUIController.cs](EffectTestUIController.cs) | `EffectTestUIController : MonoBehaviour` | UI per il test: dropdown carte/giocatore/bersaglio, toggle passive, bottoni Apply/Reset, log risultati. Comunica con `EffectTestSceneManager` |
| [CommanderTestView.cs](CommanderTestView.cs) | `CommanderTestView : MonoBehaviour` | Vista di un comandante nel test: nome, Note, buff/debuff attivi, stato passiva secondaria. `SetCommander()` e `Refresh()` per aggiornamenti real-time |
| [GameOutcomePanel.cs](GameOutcomePanel.cs) | `GameOutcomePanel : MonoBehaviour` | Pannello modale di fine partita: `Bind(isWin, isDraw)` imposta titolo ("VITTORIA!" / "SCONFITTA" / "PAREGGIO") e sottotitolo. Pulsante → `SceneManager.LoadScene("MainMenu")` + disconnect Photon. Prefab in `Assets/Prefabs/UI/GameOutcomePanel.prefab` |

### Editor/ (automazioni scene, setup)

| File | Tipo | Responsabilità |
|---|---|---|
| [Editor/EffectTestSceneSetup.cs](Editor/EffectTestSceneSetup.cs) | `EffectTestSceneSetup` | Setup automatico della scena `EffectTest.unity`. Menu editor `4E → Setup Scena Test Effetti`: genera Canvas, UI dropdowns, view comandanti, log output. Salva in `Assets/Scenes/EffectTest.unity` |
| [Editor/GameOutcomePanelSetup.cs](Editor/GameOutcomePanelSetup.cs) | `GameOutcomePanelSetup` | Crea il prefab `GameOutcomePanel.prefab` in `Assets/Prefabs/UI/`. Menu editor `4E → Crea prefab GameOutcomePanel`. |

## Note

- Usa `UnityEngine.UI` (Text/Button/Image legacy). Migrabile a TextMeshPro.
- Le carte (`CardView`) sono **istanziate** da prefab `[SerializeField]`; il resto della HUD è in scena (vedi memoria `project_scene_run_setup`).
- **Verifica in mano**: la carta Verifica è renderizzata in `RenderHand` come le altre; click → `SubmitPlayVerifica()`. Il vecchio bottone separato è stato riutilizzato come annullamento della selezione bersagli.
- **Selezione bersaglio**: carte con `RequiresTargetSelection=true` entrano in modalità selezione (overlay giallo sui CommanderView); il pulsante `ANNULLA` torna alla mano senza inviare intent, mentre il click sul comandante completa la scelta.
- **Abilità comandanti**: pressione prolungata sul ritratto apre il popup descrittivo; il passaggio `SecondaryUnlocked` da falso a vero produce un pulse dorato e mantiene una cornice dorata permanente sulla view corretta, visibile a entrambi i giocatori e anche al cambio di prospettiva hotseat.
- **Anteprima carta**: pressione prolungata con touch o mouse mostra una copia ingrandita al centro del Canvas; il rilascio la chiude senza giocare/acquistare la carta.
- **Mercato**: `ShopContainer` e le sue carte sono attivi solo durante la fase `Shop`.
- **Layout carte**: oltre quattro carte la mano riduce dinamicamente lo spacing per restare nel contenitore; lo Shop usa una griglia da quattro colonne.
- **Mazzo e scarti**: due indicatori locali mostrano i conteggi aggiornati dallo snapshot di rete.
- **Carta giocata**: ogni nuovo `PlayedCardSequence` ricevuto nel DTO anima la carta locale dalla mano e quella avversaria dalla zona alta del campo; durante la permanenza al centro una X pulsa sui comandanti locali bersagliati.
- **Pescata**: ogni nuovo `DrawSequence` locale anima le carte effettivamente pescate dall'indicatore del mazzo alla loro posizione nella mano.
- **Crediti avversari**: il valore mostrato viene aggiornato all'inizio del round, al cambio prospettiva hotseat e all'Esame Finale.
- **Azioni rimanenti**: il contatore è visibile esclusivamente al giocatore attivo durante la fase `Play`.
- **Menu iniziale**: la scena `Assets/Scenes/MainMenu.unity` (prima in Build Settings) ospita `MainMenuController` + `OnlineLauncher`. "Stesso telefono" carica `CommanderSelectUI` in hotseat e poi la scena di gioco (`SampleUI`); "Online" connette via Photon per codice stanza, sincronizza `CommanderSelectUI` e infine carica `SampleUI`.
