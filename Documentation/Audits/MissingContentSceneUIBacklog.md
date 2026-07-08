# Missing Content, Scene, and UI Backlog

## 1. Missing SO Content (Data)
While the ScriptableObject definitions are ready, the actual data assets in `Assets/Game/Resources` are incomplete:
- **Pokemon Species:** Only 24 defined. Needs the rest of the target roster.
- **Moves:** Only 115 defined. Needs full elemental/category coverage.
- **Abilities & Natures:** SO content needs to be expanded.
- **Quests & Role Boards:** Only 2 Quests exist. Needs generic Role Activity Board setups (Professor, Police, Ranger).
- **LifePaths:** Needs basic branch, perk, and reward SO instances to be fully testable.
- **Items:** Only 23 defined. Missing key TM/HM/Key Items content.

## 2. Missing Scene Setup
- **Camp / Travel:** Scene placement for social activity sources (camp spots, festival booths).
- **NPC Setup:** Scene placement for randomized NPC slots and companion GameObject components.
- **World Triggers:** Placement of region boundaries and situation event triggers in overworld scenes.

## 3. Missing UI Panels
Despite having `UIManager` backend scripts, the visual panels and prefabs are missing for:
- **Battle UI:** Command-palette or hybrid mode selection UI.
- **Competitions:** Registration, brackets, and rankings UI.
- **LifePath / Progress:** Perks tree, XP progress, and unlocked perks debug/player view.
- **Activities:** Role activity board displays for notice boards (Police, Ranger, Festival).
- **Notifications & Dialogues:** Styling for speech bubbles and the MMO-like notification feed UI.
- **Shops / Services:** Final UI for market/shelf interactions.
- **Reusable Radial Context Menu:** Planned shared option-selection UI for Party slots, inventory items, tools, world interactions, ability/technique choices and creature actions. Needs ring prefab, segment prefab, option tag/frame prefab, action icons and controller/provider scripts before screen-specific adoption.
