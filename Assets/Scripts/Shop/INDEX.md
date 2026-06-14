# INDEX — Shop

Acquisti e generazione/refresh del pool shop personale.
Torna all'[indice generale](../INDEX.md).

## File

| File | Tipo | Responsabilità / API chiave |
|---|---|---|
| [ShopManager.cs](ShopManager.cs) | `ShopManager` | `TryPurchase(player, card)` (valida costo/limite/pool, paga in Note, pubblica `CardBoughtEvent`), `RefreshPool(player)`, `ResetPurchases()` |
| [ShopPool.cs](ShopPool.cs) | `static ShopPool` | `GeneratePool(catalog, credits, config, rng)` e `RefreshSlots(...)`: filtrano per `MinCreditsRequired`, garantiscono le quote minime A/B/C configurate e riempiono casualmente gli slot restanti |

## Note

- Le carte acquistate vanno nello scarto e rientrano nel mazzo alla Fase DRAW.
- Il pool è filtrato per Credits: carte con soglia più alta compaiono solo dopo aver guadagnato Credits.
