# INDEX - Cards/Editor

Automazioni Unity Editor per gli asset delle carte. Questi script non entrano nelle build runtime.
Torna all'[indice Cards](../INDEX.md).

## File

| File | Tipo | Responsabilita / API chiave |
|---|---|---|
| [CardArtworkAutoAssigner.cs](CardArtworkAutoAssigner.cs) | `CardArtworkAutoAssigner : AssetPostprocessor` | Importa le immagini in `Assets/cards` come Sprite e le assegna automaticamente a `CardDataSO.Artwork` in base al nome. Menu manuale: `4E/Cards/Assign Artwork` |
