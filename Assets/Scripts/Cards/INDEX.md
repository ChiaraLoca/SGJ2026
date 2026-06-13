# INDEX — Cards

Sistema carte: definizione dati, effetti (Strategy via ScriptableObject), condizioni e resolver.
Torna all'[indice generale](../INDEX.md).

## File

| File | Tipo | Responsabilità / API chiave |
|---|---|---|
| [CardEnums.cs](CardEnums.cs) | enum `CardTag` (Flags), `CardTier`, `CountSource`, `CardType`, `CardAffinity`, `EffectTarget`, `EffectDuration` | Tutte le enumerazioni del dominio carte/effetti |
| [CardDataSO.cs](CardDataSO.cs) | `CardDataSO : ScriptableObject` | Definizione immutabile di una carta: nome, descrizione, artwork, tipo, affinità, `Tags`, `Tier`, `ShopCost` (derivato dal config per tier), `MinCreditsRequired`, `Effects[]`, `IsVerifica`, `RequiresTargetSelection`, `HasTag()` |
| [CardEffectSO.cs](CardEffectSO.cs) | abstract `CardEffectSO : ScriptableObject` | Base Strategy di tutti gli effetti. `Target`, `Apply(GameContext)` puro (registra `IGameChange`) |
| [CardConditionSO.cs](CardConditionSO.cs) | abstract `CardConditionSO : ScriptableObject` | Base delle condizioni. `IsMet(GameContext) → bool` |
| [ActiveEffect.cs](ActiveEffect.cs) | `ActiveEffect` | Effetto a durata runtime su un comandante: `Magnitude`, `Duration`, `RemainingTurns`, `TickTurn()` |
| [EffectResolver.cs](EffectResolver.cs) | `EffectResolver` | `Resolve(card, context)`: applica+committa ogni effetto in sequenza, poi pubblica `CardPlayedEvent` |

### Effects/ (effetti concreti)

| File | Tipo | Effetto |
|---|---|---|
| [Effects/BuffEffectSO.cs](Effects/BuffEffectSO.cs) | `BuffEffectSO` | +Note al bersaglio (istantaneo o a durata) |
| [Effects/DebuffEffectSO.cs](Effects/DebuffEffectSO.cs) | `DebuffEffectSO` | −Note al bersaglio (istantaneo o a durata) |
| [Effects/DrawEffectSO.cs](Effects/DrawEffectSO.cs) | `DrawEffectSO` + enum `DrawMode` | Pesca: conteggio fisso, fino a dimensione mano (Biblioteca), o intero mazzo (Approfondimento) |
| [Effects/ConditionalEffectSO.cs](Effects/ConditionalEffectSO.cs) | `ConditionalEffectSO` | Applica un `_innerEffect` solo se `_condition.IsMet()` |
| [Effects/ExtraActionEffectSO.cs](Effects/ExtraActionEffectSO.cs) | `ExtraActionEffectSO` | +/− azioni nel turno, o raddoppio (Metodo, Progetto, Copiare, Studio Notturno…) |
| [Effects/CooperBuffEffectSO.cs](Effects/CooperBuffEffectSO.cs) | `CooperBuffEffectSO` | Applica +2 Note al comandante più debole; se nota ≤ 3 dopo, ritorna in mano (Test di Cooper) |
| [Effects/WikipediaEffectSO.cs](Effects/WikipediaEffectSO.cs) | `WikipediaEffectSO` | Attiva scudo di intercettazione: la prossima carta dell'avversario viene copiata in mano |
| [Effects/ScalingEffectSO.cs](Effects/ScalingEffectSO.cs) | `ScalingEffectSO` | Note/azioni/pesca proporzionali a un `CountSource` (Ripasso, Riassunto, Appunti, Sabotaggio) |
| [Effects/ForceDiscardEffectSO.cs](Effects/ForceDiscardEffectSO.cs) | `ForceDiscardEffectSO` + enum `ForceDiscardMode` | Avversario scarta carte: casuali (Gossip) o per tag (Politica, Bullismo) |
| [Effects/EqualizeNotesEffectSO.cs](Effects/EqualizeNotesEffectSO.cs) | `EqualizeNotesEffectSO` | Alza il comandante più basso verso il più alto (Tutor) |
| [Effects/SwapNotesEffectSO.cs](Effects/SwapNotesEffectSO.cs) | `SwapNotesEffectSO` | Scambia le Note tra 2 comandanti scelti (Rappresentante di Classe) |
| [Effects/ReturnFromDiscardEffectSO.cs](Effects/ReturnFromDiscardEffectSO.cs) | `ReturnFromDiscardEffectSO` | Recupera carte dal cimitero in mano o in cima al mazzo (Schema, Compito a Casa) |
| [Effects/ShieldEffectSO.cs](Effects/ShieldEffectSO.cs) | `ShieldEffectSO` | Scudo che annulla il prossimo debuff (Dialogo) |
| [Effects/ImmunityEffectSO.cs](Effects/ImmunityEffectSO.cs) | `ImmunityEffectSO` | Blocca il calo di Note fino al prossimo turno (Fidanzata) |
| [Effects/BlockVerificaEffectSO.cs](Effects/BlockVerificaEffectSO.cs) | `BlockVerificaEffectSO` | Impedisce all'avversario di giocare la Verifica un turno (Sciopero) |
| [Effects/MoveVerificaToDeckBottomEffectSO.cs](Effects/MoveVerificaToDeckBottomEffectSO.cs) | `MoveVerificaToDeckBottomEffectSO` | Sposta la Verifica avversaria in fondo al mazzo (Occupazione) |

### Editor/ (automazioni asset)

Vedi [Editor/INDEX.md](Editor/INDEX.md).

| File | Tipo | Responsabilità |
|---|---|---|
| [Editor/CardArtworkAutoAssigner.cs](Editor/CardArtworkAutoAssigner.cs) | `CardArtworkAutoAssigner : AssetPostprocessor` | Importa e collega automaticamente gli artwork da `Assets/cards` alle definizioni carta tramite nome |

### Conditions/ (condizioni concrete)

| File | Tipo | Condizione |
|---|---|---|
| [Conditions/NoteAboveThresholdConditionSO.cs](Conditions/NoteAboveThresholdConditionSO.cs) | `NoteAboveThresholdConditionSO` | Vera se un comandante bersaglio ha `CurrentNote > soglia` |
| [Conditions/CardsInHandConditionSO.cs](Conditions/CardsInHandConditionSO.cs) | `CardsInHandConditionSO` | Vera se il giocatore attivo ha ≥ N carte in mano |
| [Conditions/CommanderHasDebuffConditionSO.cs](Conditions/CommanderHasDebuffConditionSO.cs) | `CommanderHasDebuffConditionSO` | Vera se un comandante bersaglio ha un debuff attivo |

## Note

- Gli effetti **non mutano lo stato**: registrano `IGameChange` sul `GameContext` (vedi [Core](../Core/INDEX.md)), il resolver committa dopo ogni effetto.
- Per un nuovo effetto/condizione: estendi la base + `[CreateAssetMenu]`, nessun enum+switch.
