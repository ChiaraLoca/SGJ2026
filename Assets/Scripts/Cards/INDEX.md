# INDEX — Cards

Sistema carte: definizione dati, effetti (Strategy via ScriptableObject), condizioni e resolver.
Torna all'[indice generale](../INDEX.md).

## File

| File | Tipo | Responsabilità / API chiave |
|---|---|---|
| [CardEnums.cs](CardEnums.cs) | enum `CardType`, `CardAffinity`, `EffectTarget`, `EffectDuration` | Tutte le enumerazioni del dominio carte/effetti |
| [CardDataSO.cs](CardDataSO.cs) | `CardDataSO : ScriptableObject` | Definizione immutabile di una carta: nome, descrizione, tipo, affinità, costo, `MinCreditsRequired`, `Effects[]`, `IsVerifica` |
| [CardEffectSO.cs](CardEffectSO.cs) | abstract `CardEffectSO : ScriptableObject` | Base Strategy di tutti gli effetti. `Target`, `Apply(GameContext)` puro (registra `IGameChange`) |
| [CardConditionSO.cs](CardConditionSO.cs) | abstract `CardConditionSO : ScriptableObject` | Base delle condizioni. `IsMet(GameContext) → bool` |
| [ActiveEffect.cs](ActiveEffect.cs) | `ActiveEffect` | Effetto a durata runtime su un comandante: `Magnitude`, `Duration`, `RemainingTurns`, `TickTurn()` |
| [EffectResolver.cs](EffectResolver.cs) | `EffectResolver` | `Resolve(card, context)`: applica+committa ogni effetto in sequenza, poi pubblica `CardPlayedEvent` |

### Effects/ (effetti concreti)

| File | Tipo | Effetto |
|---|---|---|
| [Effects/BuffEffectSO.cs](Effects/BuffEffectSO.cs) | `BuffEffectSO` | +Note al bersaglio (istantaneo o a durata) |
| [Effects/DebuffEffectSO.cs](Effects/DebuffEffectSO.cs) | `DebuffEffectSO` | −Note al bersaglio (istantaneo o a durata) |
| [Effects/DrawEffectSO.cs](Effects/DrawEffectSO.cs) | `DrawEffectSO` | Fa pescare N carte (default: giocatore attivo) |
| [Effects/ConditionalEffectSO.cs](Effects/ConditionalEffectSO.cs) | `ConditionalEffectSO` | Applica un `_innerEffect` solo se `_condition.IsMet()` |

### Conditions/ (condizioni concrete)

| File | Tipo | Condizione |
|---|---|---|
| [Conditions/NoteAboveThresholdConditionSO.cs](Conditions/NoteAboveThresholdConditionSO.cs) | `NoteAboveThresholdConditionSO` | Vera se un comandante bersaglio ha `CurrentNote > soglia` |
| [Conditions/CardsInHandConditionSO.cs](Conditions/CardsInHandConditionSO.cs) | `CardsInHandConditionSO` | Vera se il giocatore attivo ha ≥ N carte in mano |
| [Conditions/CommanderHasDebuffConditionSO.cs](Conditions/CommanderHasDebuffConditionSO.cs) | `CommanderHasDebuffConditionSO` | Vera se un comandante bersaglio ha un debuff attivo |

## Note

- Gli effetti **non mutano lo stato**: registrano `IGameChange` sul `GameContext` (vedi [Core](../Core/INDEX.md)), il resolver committa dopo ogni effetto.
- Per un nuovo effetto/condizione: estendi la base + `[CreateAssetMenu]`, nessun enum+switch.
