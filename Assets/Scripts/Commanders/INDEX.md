# INDEX — Commanders

I "secchioni" del giocatore: definizione statica e stato runtime.
Torna all'[indice generale](../INDEX.md).

## File

| File | Tipo | Responsabilità / API chiave |
|---|---|---|
| [CommanderKind.cs](CommanderKind.cs) | enum `CommanderKind` | Identità del comandante: `Storia`, `Matematica`, `Inglese`, `EducazioneFisica`. Determina le passive applicate |
| [CommanderDataSO.cs](CommanderDataSO.cs) | `CommanderDataSO : ScriptableObject` | Definizione immutabile: `CommanderName`, `Kind`, `BaseNote`, `LinkedCards` (5 carte di partenza), `Portrait`, descrizioni passive per la UI di selezione (`BaseAbilityDescription`, `UnlockConditionDescription`, `SecondaryAbilityDescription`) |
| [CommanderState.cs](CommanderState.cs) | `CommanderState` (`[Serializable]`) | Stato runtime: `CurrentNote` (derivata: base + delta istantaneo + buff − debuff, ≥0), `ApplyInstantDelta()`, `AddBuff()`/`AddDebuff()`, `AddDebuffShield()`, `SetNoteFloorLocked()`, `SecondaryUnlocked`/`MarkSecondaryUnlocked()` (sblocco permanente passiva secondaria), `TickActiveEffects()`, `ResetForNewRound()`, `HasActiveDebuff`, `DebuffShields`, `IsNoteFloorLocked` |
| [CommanderPassiveConstants.cs](CommanderPassiveConstants.cs) | `static CommanderPassiveConstants` | Costanti di design delle passive (da CARDS.md): magnitudini (+3 Storia/Verifica, +3 carte Mate, +1 Inglese, ecc.) e soglie di sblocco (20 crediti, round 3, 15 carte mazzo, 0 note) |

## Note

- **Passive comandanti**: la logica vive in [`Core/CommanderPassiveSystem`](../Core/INDEX.md) (host-only). Le abilità base/secondaria e le condizioni di sblocco sono definite in CARDS.md; le secondarie si sbloccano (in modo permanente) controllando le condizioni a inizio di ogni turno.
- `CurrentNote` è **calcolata**, mai memorizzata direttamente.
- Gli effetti a durata vivono qui come `ActiveEffect` (vedi [Cards](../Cards/INDEX.md)); scalano a fine turno di chi li subisce e si azzerano al reset post-Verifica.
- **Mitigazione cali**: un calo di Note (delta negativo o debuff a durata) è annullato da `IsNoteFloorLocked` (immunità Fidanzata, non consuma) o da uno `DebuffShields` (Dialogo, consuma uno scudo). Logica centralizzata in `AbsorbNegative()`.
