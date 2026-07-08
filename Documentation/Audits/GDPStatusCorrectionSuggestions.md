# GDP Status Correction Suggestions

Based on the audit of the current project state vs the Game Development Plan (GDP), here are the suggested corrections to the GDP:

## 1. UI Maturity Overstatement
- **GDP States:** UI backend for many systems is "Done".
- **Reality:** While `UIManager` scripts (e.g., `LifePathUIManager`, `RoleActivityBoardUIManager`) exist and expose snapshots, the actual frontend UI panels and scene-wiring for these are mostly non-existent. The GDP should distinguish clearly between "UI Data Backend Ready" and "UI Prefabs/Views Ready".

## 2. Save System Integration
- **GDP States:** "Save system foundations" exist.
- **Reality:** While logs like `PlayerLifePathLog`, `PlayerWorldTriggerLog` exist, there is no unified `SaveManager` bridging the core battle state, Pokemon party state, or current map location natively seen in the root. If it exists, it is disjointed. Suggest marking "Game State Serialization" as an explicit pending task.

## 3. ScriptableObject (SO) Content Deficit
- **GDP States:** "Data Ready" implies SOs are created.
- **Reality:** The script definition for SOs exists, but the actual SO assets in `Assets/Game/Resources` are heavily lacking (e.g., only 115 moves, 24 Pokemons, 23 Items, 2 Quests).
- **Suggestion:** GDP should add a metric for "Minimum Viable Content (MVC)" to indicate how many SOs must be instantiated before a system is considered playable/testable.

## 4. Activities Complexity
- **Reality:** With ~100 activity-related scripts, the activity system has grown significantly larger than standard core Pokemon mechanics.
- **Suggestion:** GDP should break down "Activities" into smaller manageable modules (e.g., Role Activities, Minigame Activities, Camp Activities) rather than tracking them as one massive block.
