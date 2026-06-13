# INDEX — Commanders

I "secchioni" del giocatore: definizione statica e stato runtime.
Torna all'[indice generale](../INDEX.md).

## File

| File | Tipo | Responsabilità / API chiave |
|---|---|---|
| [CommanderDataSO.cs](CommanderDataSO.cs) | `CommanderDataSO : ScriptableObject` | Definizione immutabile: `CommanderName`, `BaseNote`, `LinkedCards` (5 carte di partenza), `Portrait` |
| [CommanderState.cs](CommanderState.cs) | `CommanderState` | Stato runtime: `CurrentNote` (derivata: base + delta istantaneo + buff − debuff, ≥0), `ApplyInstantDelta()`, `AddBuff()`/`AddDebuff()`, `TickActiveEffects()`, `ResetForNewRound()`, `HasActiveDebuff` |

## Note

- `CurrentNote` è **calcolata**, mai memorizzata direttamente.
- Gli effetti a durata vivono qui come `ActiveEffect` (vedi [Cards](../Cards/INDEX.md)); scalano a fine turno di chi li subisce e si azzerano al reset post-Verifica.
