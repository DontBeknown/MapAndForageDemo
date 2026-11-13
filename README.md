# Map and Forage Demo

A standalone Unity portfolio demo for the area map and persistent forage systems. It follows the reference game's manager and prefab structure and reuses its prefab UID editor workflow.

## Run the demo

Open the project in **Unity 6000.3.26f1**, open [Initialize](Assets/Scenes/Initialize.unity), and enter Play Mode. Initialize is first in the global scene list.

- **WASD** moves between Meadow and Grove. **E** harvests the closest active plant in reach.
- **M** opens or closes the map. **Previous** and **Next** browse both areas.
- **Advance day** progresses forage. **Save** and **Load** use one local slot. **Start over** resets the session to day one and keeps the saved slot available.

Try browsing Grove while standing in Meadow. Then close the map, harvest a plant, and reopen the map to see its pin disappear. Save after harvesting, restart Play Mode, and load to restore the day and forage state.

## Persistent forage

[BreakableObjectManager](Assets/Script/MapForageDemo/Gameplay/Manager/BreakableObjectManager.cs) discovers the breakables under each Area when the session starts and initializes the roster on day one. It stores one record per `(areaId, pointId)`, with the current state, the day that state was set, and the earliest day the point can activate again.

1. **Activate.** When TimeHopManager advances the day, the forage manager fills open slots up to two active plants per area. Eligible inactive points are ranked by a deterministic score based on the seed, day, area ID, and point ID.
2. **Harvest.** A successful harvest changes the point from `Active` to `Broken`, shows stubble, disables its interaction collider, and hides its map pin. The point becomes `Inactive` the next day and becomes eligible two days after harvest.
3. **Expire.** An untouched plant becomes inactive after three days and becomes eligible again the following day. It can stay inactive if the area's slots are full.

[UserDataManager](Assets/Script/MapForageDemo/Gameplay/Manager/UserDataManager.cs) saves the day, seed, and point records as JSON under `Application.persistentDataPath`. Load validates the version, day, and forage records against the current roster before restoring them. A successful restore updates plant visuals, interaction colliders, and map pins from the saved state.

## Area map

[MapManager](Assets/Script/MapForageDemo/Gameplay/Manager/MapManager.cs) stores the exported texture and local capture rectangle for each area ID. Opening the map selects the area that contains the player using the level's gameplay bounds. Browsing changes the viewed area while the player stays in place.

MapManager converts each world position to an area-relative position and normalizes it against the capture rectangle. UIAreaMap uses those normalized coordinates to place pins over the texture. The cream player pin appears on the player's current area map. Green forage pins show active plants in the viewed area and refresh after a harvest, day change, or load.

To regenerate maps, register the level prefab in the local Addressables **PrefabLevel** group. In Edit Mode, select **Manager > MapManager** and click **Generate All Level Maps**. The [exporter](Assets/Editor/LevelMapExport.cs) opens each registered level in an isolated preview, hides forage, and frames each area with a temporary orthographic camera. It writes PNGs to `Assets/Art/MapForage/Generated` and assigns the texture and capture rectangle to the MapManager prefab. Keep Area transforms unrotated at unit scale, and rerun the export after changing scenery or bounds.

## Prefab UID assignment

[PrefabUidAssignment](Assets/Editor/PrefabUidAssignment.cs) runs in supported level prefab stages, including the demo's **LevelInstance**. BreakableObject implements `IPrefabUidOwner`, identifies its serialized `_pointId` field, and reports validation through `PrefabUidValidation`.

1. Open **LevelInstance** in Prefab Mode and add or duplicate a breakable inside an Area. The editor queues newly created points for a fresh GUID, including duplicates that carry the original point's ID.
2. Opening the prefab or validating an existing point queues a check. If a point requires an ID and its field is empty, the editor assigns one. Existing nonempty IDs are kept during this check.
3. Save the level prefab to keep the IDs. Nested breakables store them as prefab overrides. Keep a point's existing ID stable so saved forage records can still find it by area and point ID.
