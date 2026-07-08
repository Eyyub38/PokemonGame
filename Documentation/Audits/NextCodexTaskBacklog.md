# Next Codex Task Backlog

This backlog mirrors `Docs/GameDevelopmentPlan.md` and should stay focused on remaining work, not already-completed backend inventory.

## 1. High Priority: Close Script-Only Core Gaps
- **Custom command-palette battle system:** Not implemented yet. Needs base actions, element/modifier selection, AP/stamina/energy costs, preview cards, battle state routing and AI support.
- **Dialogue graph tooling:** Foundations exist, but there is no graph-like authoring tool yet. Decide later whether to stay SO-authored or build a custom editor window.
- **Minigame venue bridge:** Not implemented yet. Needs arcade/casino/festival minigame definitions, entry costs, chip/currency ledger, prize exchange hooks, result records and links into shops, activities, competitions, quests or notifications without deciding the actual minigames yet.

## 2. Medium Priority: Node v2 Planning
- **Task:** Plan terrain-only node data, terrain/traversal profiles, companion subnodes, scene-ring loading and personality-gated encounter perception.
- **Why:** Current node/path systems are useful, but replacing player/NPC movement requires a careful design pass.
- **Do Not Do Yet:** Do not rewrite current movement or scene loading until the placeholder test scene and UI/data stabilization pass are done.

## 3. Medium Priority: Placeholder Core Test Scene
- **Task:** Design a compact test scene using colored Images/SpriteRenderers and simple placeholders, not final assets.
- **Coverage:** camp/care, shop shelf/basket, transit stop, role board, PokeNav/map, encounter, battle trigger, competition desk, ride/elevation, situation events and debug panels.
- **Goal:** Prove systems work end-to-end before final art, SO content and UI polish.

## 4. Medium Priority: Script-Ready Systems Waiting For Content/UI/Scene
- PokeNav / Pokedex / Map / Minimap.
- Market / Basket / Shelf / Services.
- Camp / Survival / Pokemon Care.
- Pokemon Ability Tree / Pokemon Skill Tree.
- New Game / Character Setup Flow.
- Life Path / Careers / Titles / Access.
- Role Activity Boards / Jobs / Pokemon Assignments.
- Situation Events / Journey Incidents / World Conditions.
- Rumor / Law / Investigation.
- Companions / Social Activities / Follower support.
- Transit / Ride / Region travel.
- Competitions / Contests / Battle Frontier / World Championship.
- Minigame venues / Arcade / Casino / Festival stands.
- Power mechanics: Mega Evolution, Z-Move, Dynamax and Gigantamax.
- Overworld encounter resolution / stealth capture.
- Radial context UI.
- Debug / validation / asset audit overlay.

## 5. Later: UI Visualization Pass
- **Task:** Build visual UI panels for the major UI surfaces tracked in GDP's `Main UI Surface Plan`.
- **Priority surfaces:** PokeNav, notification side log, speech bubbles, radial menu, camp/care, market basket, map/minimap, Life Path/career, role boards, transit, competition, minigame venues and debug overlay.
- **Rule:** UI filters/tabs should expose their real child panels; avoid decorative-only headers.

## 6. Later: Minimum Test SO / Tutorial Content
- **Task:** Create tiny example SO sets after core script direction stabilizes.
- **Owner:** The user will create final SOs; Codex can provide tutorial-style examples and small inspectable samples later.
- **Examples:** one Life Path, one role board, one camp station, one shop, one transit route, one situation event profile, one growth profile, one power mechanic, one competition and one minigame venue/prize exchange.

## 7. Final: Asset / Content Audit Pass
- **Task:** Run validators and audit tools after real SOs, prefabs, sprites, scenes and UI bindings exist.
- **Goal:** Catch missing sprites, missing scripts, broken scene references, invalid meta GUIDs, duplicate ids, missing prefab links and incomplete content.
