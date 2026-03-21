# Notes

Dumping ground for whatever's interesting in the game's source.

```
PlayerData_Inventory
    .instance
    .currencyAmount

    .ModifyCurrency(int)

PlayerDataInventory
    .itemMetas // Contains item's quantity

SyncHandler
    GeneralData .getGeneralData
        // Seems to be something that gets updated on activating Surrogate and not necessarily keeping track of current
        // area, even worse that it doesn't get updated on teleport
        .currentAreaNameTerm

        .mapData_customMarkers
            // Quantity, although it looks like the current build of the game has done away with fixed number markers
            ["Custom Marker 1"] 
            ["Custom Marker 2"]
            ["Custom Marker 3"]

CharacterCombatHandler
    .TakeHit()
    .TakeDamage()

LevelManager
MapMarkers
```
