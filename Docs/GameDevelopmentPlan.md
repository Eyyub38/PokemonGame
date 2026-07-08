# PokemonProject Game Development Plan

Last updated: 2026-07-03

This document is the living GDP/GDD for the project. It tracks what exists, what is script-ready, what still needs ScriptableObject content, UI, scene setup, balancing or assets, and what should be built next.

## Status Legend

- **Playable:** Scene, UI, data and runtime flow are ready enough to test as gameplay.
- **Script Ready:** Core scripts, logs, requirements and validators exist; needs ScriptableObject assets, UI, scene wiring or balancing.
- **Base:** Foundation exists, but the final gameplay loop still needs more scripts or integration.
- **Planned:** Design direction is accepted, but scripts are not implemented yet.
- **Deferred:** Intentionally postponed.

## Current Project State

The project now has a broad backend layer for a custom Pokemon-like RPG. Most newer systems are intentionally **Script Ready**, not content-complete. This matches the current workflow: scripts and base architecture are created first, while ScriptableObject assets, UI design, scene hierarchy and final balancing will be handled later by the user.

Current summary:

- Core Pokemon project foundation: **Base/Playable mix**
- New RPG/world/shop systems: **mostly Script Ready**
- New UI flows: **mostly Planned**
- ScriptableObject content: **mostly Pending/User-authored**
- Scene placement/alignment: **mostly Pending**
- Current cleanup focus: **next script-ready integration layer / UI backend contracts**
- Latest backend addition: **radial party menu provider backend**
- Custom command-palette battle system: **Deferred/Removed for now**
- Multiplayer: **Deferred/Optional late-game consideration**



## Strategic Design Update: Modular Pokemon-World Life RPG

Status: **Accepted Design Direction / Roadmap Layer**

This project should no longer be treated as a narrow linear monster-battling clone. The intended design direction is a modular Pokemon-world life RPG sandbox where the player can live and travel with Pokemon while gradually forming a personal role identity through repeated activities.

The core fantasy is:

- The player is not only a battle trainer.
- The player is a person living in a Pokemon-like world.
- Becoming a Champion is one valid path, not the only path.
- Other valid paths include Ranger, Farmer/Rancher, Performer, Researcher, Caretaker/Breeder, Explorer, Merchant/Crafter and Investigator/Law-aligned play.
- Pokemon should matter in battle, camp, care, travel, research, rescue, farming, crafting, social scenes and world events.

### Design Pillars

Every new system should support at least one of these pillars:

1. **Living with Pokemon**
   - Pokemon have vitality, stamina, needs, personality, mood, memory-like event effects, camp behavior and non-battle utility.

2. **Traveling through Regions**
   - Routes, camps, travel points, rides, weather, region conditions, journey events and PokeNav knowledge should make the world feel like a long journey rather than a sequence of battle screens.

3. **Building a Role Identity**
   - The player should become known through actions: Champion, Ranger, Researcher, Farmer, Performer, Merchant, Caretaker, Explorer or Investigator. This identity should emerge through activity rewards, requirements, licenses, reputation and life-path progression.

4. **Reactive World**
   - Rumors, law, NPC memory/reactions, organizations, permits, access profiles, PokeNav entries and event history should let the world react to what the player repeatedly does.

### Scope Philosophy

The project is intentionally a personal dream-project and systems experiment, not a deadline-driven commercial production. Therefore, broad systems are allowed if they remain modular, optional and data-driven.

Codex should not remove or reject broad systems only because they are not required for a minimal vertical slice. Instead:

- Keep systems optional.
- Avoid hard dependencies between unrelated career paths.
- Let incomplete paths fail gracefully.
- Prefer ScriptableObject-driven definitions and reusable requirement/outcome layers.
- Avoid monolithic all-in-one managers.
- Let individual paths be enabled, disabled, tested or ignored without breaking the rest of the game.

### Core Gameplay Identity

The intended high-level loop is:

1. Travel through a region.
2. Encounter Pokemon, NPCs, events, rumors, resources or route obstacles.
3. Resolve situations through battle, care, research, stealth, assignments, social choices, licenses or role-specific options.
4. Pokemon gain fatigue, injuries, mood changes, bond changes, skill progress and activity experience.
5. The player camps, visits settlements, uses services, manages Pokemon, accepts role-specific tasks or continues deeper into the journey.
6. Repeated actions build a long-term life path, reputation, access rights and world identity.

The game should support both focused and hybrid identities. Example: a player can be mainly a Ranger but also maintain a farm, perform contests and occasionally compete in tournaments.



## Life Path / Vocation Progression System

Status: **Script Ready / High-Priority Roadmap Layer**

This is the recommended progression layer for connecting the project's many systems into one understandable player identity model.

### Design Goal

The player should not choose a fixed class at the start. Instead, identity should emerge from repeated activity.

Examples:

- Battle, tournament and league activity builds the **Trainer / Champion** path.
- Rescue, calming, wildlife handling and anti-poaching activity builds the **Ranger** path.
- Grooming, treatment, feeding and bonding activity builds the **Caretaker / Breeder** path.
- Farming, ranching, gathering and Pokemon-assisted production builds the **Farmer / Rancher** path.
- Contest, stage, appearance and move-combo activity builds the **Performer** path.
- Observation, Pokedex/PokeNav research and field study builds the **Researcher** path.
- Mapping, camping, route survival and travel activity builds the **Explorer** path.
- Crafting, trade, deliveries and regional goods activity builds the **Merchant / Crafter** path.
- Law, clues, rumors, witness reports and illegal activity investigation builds the **Investigator / Law** path.

### Recommended Hybrid Model

Use a hybrid of CK3-style lifestyle XP and activity-tag/branch progress.

Each activity may grant:

1. **Life Path XP**
   - Fills the main bar for one or more life paths.
   - When the bar fills, the player gains a perk point for that life path.

2. **Branch Progress**
   - Tracks which sub-branch of a life path the player is actually practicing.
   - Example: Caretaker can split into Grooming, Medical Care, Bonding and Nutrition.

3. **Activity Tags / Counters**
   - Record repeated behavior for requirements.
   - Example tags: `grooming`, `medical`, `rescue`, `tracking`, `contest`, `field_biology`, `camping`, `trade`, `law`, `stealth`, `cooking`.

### Perk Unlock Logic

Life Path XP should create perk points. Branch progress and activity tags should decide which perks are eligible.

Example:

- Cleaning a Pokemon gives Caretaker XP and Grooming progress.
- Treating an injured Pokemon gives Caretaker XP, Ranger XP, Medical progress and Rescue progress.
- Winning a gym battle gives Trainer XP and Competitive progress.
- Observing rare behavior gives Researcher XP and Field Biology progress.

A Caretaker point earned through general Caretaker XP can unlock medical perks only if the player has enough medical branch progress or treatment activity history.

### Suggested Life Paths and Branches

#### Trainer / Champion

- Competitive Battling
- Team Tactics
- Stamina/Core Vitality Management
- Switching and Entry Effects
- Tournament/League Discipline

#### Ranger

- Rescue
- Wildlife Handling
- Tracking
- Anti-Poaching / Field Law
- Non-Battle Resolution

#### Caretaker / Breeder

- Grooming / Appearance
- Medical Care
- Bonding / Mood
- Nutrition
- Recovery and Rest

#### Farmer / Rancher

- Crop Production
- Pokemon Work Roles
- Ranch Facilities
- Cooking Ingredients
- Resource Processing

#### Performer

- Stage Presence
- Appearance / Grooming
- Move Choreography
- Audience Response
- Costume / Accessory Preparation

#### Researcher

- Pokedex Plus Knowledge
- Field Biology
- Rare Behavior Observation
- Region/Habitat Study
- Knowledge-Gated Encounters

#### Explorer

- Camping
- Route Survival
- Mapping
- Ride/Travel Mastery
- Weather and Regional Adaptation

#### Merchant / Crafter

- Regional Trade
- Delivery Work
- Crafting Professions
- Shop/Market Knowledge
- Service and Supply Chains

#### Investigator / Law

- Clues and Witnesses
- Rumor Analysis
- Access/Infiltration
- Anti-Poaching Cases
- Legal/Licensed Authority

### Codex Implementation Notes

Prefer adding this as a modular progression layer over existing Activity and Requirement systems.

Recommended ScriptableObject/data types:

- `LifePathDefinition`
- `LifePathBranchDefinition`
- `LifePathPerkDefinition`
- `ActivityLifePathReward`
- `LifePathPerkEffectDefinition` or reusable outcome hooks
- `PlayerLifePathLog`

Recommended behavior:

- Activities without life-path rewards should continue to work normally.
- Perk requirements should reuse the existing requirement framework where possible.
- Perks should provide effects through existing hooks: requirements, activity modifiers, care effects, battle rule modifiers, PokeNav reveals, dialogue options, shop/service access, event weights or reputation bonuses.
- Avoid hardcoded path names in core logic where possible.

Current implementation:

- `LifePathDefinition` stores editable path identity, category, tags, XP-per-perk-point, branch definitions and linked perk assets.
- `LifePathPerkDefinition` stores editable perk identity, owning path, branch id, perk point cost, eligibility requirements and one-time unlock effects.
- `LifePathReward` can grant path XP, branch progress, tag counters and optional direct perk unlocks without hardcoded path names.
- `PlayerLifePathLog` saves path XP, earned/spent perk points, branch progress, tag counters, unlocked perks and history records.
- `LifePathRequirement` lets any requirement-driven system check path XP, branch progress, tag counters, perk points, unlocked perks or dominant path.
- `LifePathRewardSource` can apply rewards from overworld triggers/interactables for testing or scene-driven content.
- `LifePathUIManager` exposes a UI-facing backend snapshot and perk unlock action without requiring final UI.
- Activity, ActivityOutcome, SocialActivity, Job, Service, ServicePackage, BattleChallenge, ResearchTarget, PokemonCareStation, PokemonCareFacility, FarmNode and ResourceNode can now optionally grant Life Path rewards.
- Quest, Contest, Competition, Competition Prize, Competition Ranking Tier, Competition Honor, Competition Season, Investigation, Assignment, Organization, DialogGraphEffect and ConsequenceChainStep flows can also emit Life Path rewards.
- PlayerSystemsInstaller and RuntimeHealthMonitor know about `PlayerLifePathLog`.
- ProjectValidator checks life path ids, perk ids, duplicate ids, invalid branch references, empty tags, duplicate branch/perk entries, impossible max-XP/perk-cost setups, prerequisite self-links/cycles, cross-path prerequisite dependencies, malformed `LifePathRequirement` assets, perk unlock effect payloads, dialog/consequence reward payloads and malformed life path rewards across the main reward sources.



## Accepted External-Mechanic Adaptation Review

Status: **Design Review / Roadmap Input**

This section records which borrowed/general mechanics fit the project and how they should be adapted. These are not literal clone targets; each must be reinterpreted for the Pokemon-world life RPG identity.

### Scoring Legend

- **Full Plus:** Strong fit. Should become part of the core or near-core design.
- **Half Plus:** Good idea, but requires scope adjustment, reinterpretation or careful implementation.
- **Half Minus:** Risky or potentially tedious. Only use in a narrow modified form.
- **Full Minus:** Not recommended. No current entries.

### Full Plus Systems

#### Life Paths / Lifestyle Trees

Accepted as the main role-progression umbrella. Use activity XP, branch progress, activity tags and perk unlocks to make the player's identity emerge from play.

#### Open Career / Reputation-Driven Progression

Accepted. Careers should not be fixed classes. Rumors, organizations, titles, permits, licenses, jobs and reputation should let the world recognize what the player repeatedly does.

#### Battle Aftermath / Injury / Core Vitality Consequences

Accepted. Core HP, stamina, fatigue and injury should make long journeys and repeated battles matter. Avoid overly punitive permanent loss; prefer staged penalties, treatment needs and camp/clinic recovery.

#### Non-Battle Resolution

Accepted. Wild encounters and quests should support calming, feeding, treating, observing, tracking, escorting, rescuing, stealth approaches and licensed authority options, not only battle/capture.

#### Companion Support Layer

Accepted. Companions should provide quests, activities, relationship content, comments, skill-based support and role-specific insight. Companion abilities can support Ranger, Researcher, Merchant, Performer, Caretaker and other paths.

#### License / Permit / Access System

Accepted. Permits and licenses should gate or improve region travel, rare species handling, ranger authority, research sampling, medical care, contests, league entry, trade, ride usage and restricted-area access.

#### Knowledge-Gated Encounters and Discovery

Accepted. PokeNav/Pokedex/Researcher systems should unlock encounters, hidden behavior, rare sightings, alternate capture/calm methods and special route knowledge through learned information, not only level.

#### Pokemon Skill Trees / Role Growth

Accepted. Pokemon can unlock attacks, role abilities and non-battle utility through species-compatible growth trees. This supports deeper build identity without relying on a four-move limit.

Current implementation:

- `PokemonAbilityTreeDefinition` stores editable tree identity, species/type eligibility, player requirements and node definitions.
- Ability nodes store point cost, prerequisite node ids, level/friendship gates, required growth trait ids, required known technique ids, reusable requirements and unlock effects.
- Ability effects can currently act as custom flags, stat modifiers, growth training grants, passive trait grants, technique memory grants, friendship changes and generic care/assignment/battle values for future consumers.
- `PokemonAbilityTreeState` saves unspent points per tree, unlocked node records, applied effect records and point/unlock history on each Pokemon.
- `PokemonAbilityTreeSource` can grant points and/or unlock a node from scene triggers/interactables for party slot, first party, first healthy or whole-party targets.
- `ProjectValidator` and `ContentAuditProfileDefinition` understand Pokemon ability trees and sources.

Remaining:

- Visual ability tree UI/backend panels.
- Real tree SO content and balancing.
- Deeper battle, care, assignment, travel and overworld consumers for custom effect flags/tags.

#### Inner Voice / Skill-Based Dialogue Insight

Accepted. Life-path perks can unlock internal observations and dialogue options. Examples: Ranger reads fear/aggression, Researcher notices behavior, Caretaker sees injury signs, Investigator notices contradictions.

### Half Plus Systems

#### Region Event Pools

Use map/event regions with event pools, weights, cooldowns and decay after occurrence. Repeated events should become less likely for a while to reduce repetition. Add variants by event type. Keep frequency controlled.

#### Situation Events

In addition to region events, allow condition-based events triggered by party fatigue, weather, time phase, reputation, companion presence, Pokemon personality, low supplies or active quests. Use strict requirements and cooldowns.

Current implementation:

- `SituationEventDefinition` stores editable event identity, category, tags, region/zone filters, time filters, world/calendar/condition requirements, repeat rules, active duration, start/resolve/expire effects and event publishing settings.
- `SituationEventPoolDefinition` stores weighted regional/zone event pools, roll chance, per-roll limits, duplicate prevention and optional world-condition weight modifiers.
- `PlayerSituationEventLog` saves unlocked event ids, active timed event states, start/resolve/expire/blocked history and prunes expired events on time changes.
- `SituationEventController` can roll configured pools on start, time changes or day changes using optional region/zone context.
- `SituationEventSource` lets an interactable start a specific event, resolve a specific event, roll one pool or roll several pools.
- `SituationEventSignalProfileDefinition` stores editable signal rules that can watch start/time/day, survival need changes, Pokemon care need changes and area profile changes.
- Signal rules can check survival/care thresholds, weak party health, following companions, quest status, active area profiles, activity zone tags and world condition states before rolling event pools.
- `SituationEventSignalController` subscribes to runtime signals and rolls configured pools with rule-level chance, cooldown, region/zone overrides and optional debug logging.
- `PlayerSituationEventSignalLog` saves signal rule evaluation history and cooldown state separately from active situation event history.
- `SituationEventSignalUIManager` exposes signal profile rule previews, cooldown/blocked reasons, evaluation history and manual evaluate actions for future debug, PokeNav or event-tuning panels without hardcoded signal content.
- Situation events can activate world conditions, apply consequence chains and grant Life Path rewards without hardcoded event names.
- PlayerSystemsInstaller, RuntimeHealthMonitor and ProjectValidator know about the situation event log, signal log, definitions, pools, controllers, signal profiles, UI manager and sources.

#### Pokemon Assignment System

Use in camp, farm, expedition, NPC tasks, delivery, rescue and investigation contexts. Assigning a flying Pokemon to scout or a scent-focused Pokemon to search for a missing Pokemon should create meaningful non-battle utility.

Current implementation:

- `PokemonAssignmentDefinition` stores editable assignment identity, category, tags, optional activity costs/rewards, repeat rules, zone filters, Pokemon requirements, duration, success chance, Pokemon mood/friendship effects, outcomes, Life Path rewards, career rewards, consequence chains and event publishing settings.
- `PokemonAssignmentBoardDefinition` groups several assignments into editable camp, farm, ranch, lab, ranger or NPC board offer lists without creating hardcoded content.
- `PlayerPokemonAssignmentLog` saves active Pokemon assignment states and claimed history, including assignment id, source, zone, captured party slot, Pokemon name/base/level, ready hour and success chance.
- `PokemonAssignmentSource` can start an assignment with the first eligible party Pokemon, start with a specific party index, claim the first ready assignment, or start/claim from one interactable source.
- `PokemonAssignmentUIManager` exposes a UI-facing snapshot for visible offers, eligible Pokemon rows, active assignments, ready counts and start/claim/cancel actions. This is backend only; final UI layout can be built later.
- Assignment requirements can check active Activity Zones, zone tags/types, Pokemon level, health, friendship, type filters, required mood and reusable `ActivityRequirement` assets.
- Assignment claims can apply activity outcomes, Life Path rewards, career points, consequence chains and direct Pokemon friendship/mood changes without hardcoded role names.
- PlayerSystemsInstaller, RuntimeHealthMonitor and ProjectValidator know about Pokemon assignment logs, definitions, boards, sources and UI managers.

Remaining:

- Final visual UI for selecting a specific Pokemon, viewing ready assignments and showing success/reward preview.
- ScriptableObject content for farm help, camp guard, scout, delivery, search/rescue, research assistant and investigation tasks.
- Scene placement for assignment boards, camp stations, farm/ranch stations and NPC task sources.

#### Personality and Event Effects

Pokemon personalities are accepted. Event scars/memories are useful but should be moderate. Use mood, affinity, activity preference, small bonuses, fears or recovery behavior rather than heavy simulation.

#### Regional Ecology

Use lightweight regional modifiers rather than full Monster-Hunter-level simulation. Regions can affect Pokemon comfort, stamina, encounter access, camp bonuses and available assignments.

Current implementation:

- `JourneyEnvironmentProfileDefinition` stores editable region/zone/weather/environment rules without hardcoded route names.
- Environment rules can filter by region, region tag, activity zone, zone tag, day period, active world conditions, world condition tags and reusable `ActivityRequirement` assets.
- Rules can apply hourly survival need changes, Pokemon care need changes, optional situation event pool rolls and optional Life Path rewards.
- `JourneyEnvironmentController` can evaluate profiles on start, hourly time changes, area profile changes, world condition changes or manual debug calls.
- `PlayerJourneyEnvironmentLog` saves rule evaluation history, blocked checks, applied survival/care counts, rolled pools, started events and reward counts.
- PlayerSystemsInstaller, RuntimeHealthMonitor and ProjectValidator know about journey environment logs, profiles, rules and controllers.

Remaining:

- ScriptableObject profiles for rain, heat, cold, caves, mountains, forests, safe camps, dangerous routes and weather/world-condition combinations.
- Scene placement for JourneyEnvironmentController objects on route/camp/region managers.
- Balancing survival/care drain and recovery values after the first real playtest.

#### Food / Climate / Cooking Effects

Use cooking as a camp and preparation system. Meals can affect stamina recovery, core recovery, mood, contest appearance, weather resistance, journey fatigue or temporary role bonuses.

#### Time Phases

Use game-day phases instead of real time or strict Persona-style forced night rest. Activities and scene transitions consume phase budget. Rest is expected but not mandatory every night; skipped rest stacks fatigue/debuffs.

#### Enemy Intent Reading

Do not show full enemy intent by default. Add it through Trainer, Researcher or Ranger perks. Start vague, then upgrade to move-category or risk-type insight.

#### Pokemon Builds / Switch and Entry Effects

Use Pokemon role growth and team-flow effects to deepen combat and utility. Entry/exit effects can support battle builds, Ranger calming builds, Research analysis builds or Performer stage builds.

#### Farming

Accepted after core farm logic is integrated with Pokemon utility. The farm should be optional but deep for players who choose Farmer/Rancher play.

#### Camp Stations / Decorations

Use station-style camp objects rather than purely cosmetic decoration. Stations can include cooking, medical, care, research, training, performance, storage, guard posts, mini-farm plots and Pokemon play areas. Companions can unlock or improve stations.

#### Multi-Solution Quest Design

Use once quest systems are deeper. Quest objectives should support multiple approaches based on life path, license, Pokemon assignment, companion, knowledge and reputation.

#### PokeNav / Bestiary Contracts

Use PokeNav/Pokedex to support research notes, sightings, wildlife contracts, region danger reports, rare behavior hints and ranger requests.

#### Delivery / Route Item Conditions

Use delivery as a quest and travel system. Weather, route danger, cargo type, Pokemon stamina, ride capacity and camp access can affect delivery outcomes.

#### Equipment / Bags / Accessories as Modifiers

Use wearable items, bags, kits and tools for small statistical or permission-like bonuses. Some perks can require related equipment, similar to tool-gated specialization.

#### Camp / Headquarters / Farm Upgrades

Use different upgrade models by path: camps for travelers/champions/explorers, farms/ranches for Farmer/Caretaker play, Ranger outposts or rescue posts for Ranger play.

#### Care Minigame Interactions

Use in camp: petting, brushing, feeding, washing, playing, exercise and comfort actions. Keep mechanics light; tie them to mood, bond, cleanliness, appearance, recovery and small activity progress.

#### Infiltration / Restricted Area Tasks

Use in specific quest types such as criminal bases, poacher camps, treasure hunts, rescue missions or restricted facilities. Do not turn the whole game into stealth.

#### Crafting / Professions

Use as support for multiple paths: herbalist, cook, ball smith, camp gear maker, performer costume maker, research tool maker, medic, trader. Tie recipes and access to life paths and shops/services.

### Half Minus Systems

#### General Break / Guard System

Avoid a universal break system on every enemy because it can lengthen and slow combat. Reinterpret it as a narrow shield-layer mechanic attached to specific defensive moves such as Protect, Iron Defense, Reflect, Shell Guard or Brace.

Recommended form:

- Protect creates a one-turn shield.
- Iron Defense creates a temporary physical shield.
- Reflect creates a team shield.
- Shell Guard reduces Core HP pressure.
- Brace spends stamina to reduce an incoming hit.

### Design Filter for Future Mechanics

Before adding a new borrowed mechanic, check:

1. Which life path does it support?
2. Does it make Pokemon feel more alive or useful?
3. Does it improve journey, role identity or reactive world behavior?
4. Can it be optional and data-driven?
5. Can it fail gracefully if content is missing?

If a mechanic does not support any pillar, postpone it.


## General System Status

### Core Pokemon Foundation

Status: **Base/Playable mix**

- Pokemon party, Pokemon data, moves, abilities, natures and status foundations.
- Pokemon core growth profiles for potential, training points and passive traits.
- Pokemon growth state is saved on each Pokemon and can modify HP, Attack, Defense, Sp. Attack, Sp. Defense and Speed without replacing the older nature/EV/stat formula yet.
- Pokemon growth initializer sources can apply a growth profile to party Pokemon from Start, interaction, trigger or context menu.
- Pokemon care actions can grant growth training rewards, so care/training content can raise long-term stat development through editable SO data.
- Pokemon evolution definitions can now extend the old level/item/time evolution list with SO-driven routes for level, item, friendship, gender, nature, personality, growth traits, time, region, zone, scene and reusable requirements.
- Pokemon evolution runtime state is saved on each Pokemon, including deferred route ids and evolution history.
- Evolution sources can trigger manual/interactable/trigger-based evolution routes, while the old level-up and item-use flows can also discover eligible SO-driven evolution definitions.
- Pokemon technique memory now stores every learned/remembered move separately from the classic active 4-move list.
- Technique learning definitions and sources can teach moves from tutors, TM-like sources, quests, care/training, events or manual scene interactions.
- Classic active moves remain compatible with the current battle UI while future command-palette battle can read the full known technique pool.
- Pokemon held item/equipment backend can give, take, replace, swap and clear held items through a reusable service and scene source.
- Player held item history records save equip/unequip/swap attempts with source, target Pokemon, previous item, next item and result messages.
- Inventory now has an optional Held Items category while Pokemon save data still stores the item each Pokemon is holding.
- Pokemon core vitality state for long-term health, physical stamina, elemental stamina and short-term battle stamina.
- Pokemon timed recovery effects for regen multipliers, end-turn recovery and temporary stat modifiers.
- Existing battle system, battle states, HUD, move selection and rule context hooks.
- Inventory, wallet, item pickup/giver and item usage.
- Quest, saving, storage, scene portals and basic overworld interaction.
- Audio, dialogue manager and menu/selection UI foundations.

Notes:

- These are older project systems. Some are playable, but many still need cleanup, balancing and new-system integration.
- The newer custom command-palette battle system is deferred until its UI is ready.
- Core vitality is stored on each Pokemon and can be saved/restored. The classic battle flow now caps battle HP by core health on entry and can spend move-level battle stamina costs through move data or the active battle rule profile when configured.
- Core growth is intentionally layered over the old EV/Nature system for now. Later balancing can choose whether to keep classic EVs, convert them into visible training points, or disable EV gain in favor of growth profiles.

Remaining:

- Growth profile SO content for wild Pokemon, starters, trained Pokemon, rare Pokemon and companion-owned Pokemon.
- Passive trait SO content such as brave, timid, sturdy, energetic, careful, aquatic, flying, work-ready or performer traits.
- Trainer/encounter/gift hooks that assign suitable growth profiles automatically when Pokemon are created.
- UI rows for potential, training values, passive traits and recent training history.
- Evolution definition SO content for normal species evolutions, friendship evolutions, region/special evolutions and quest/activity-gated evolutions.
- Future confirmation/deferral UI that asks the player before applying eligible evolution routes.
- Technique learning SO content for tutors, manuals, training stations, quest rewards and special regional technique teachers.
- Held item UI actions for party menu, bag menu, storage/phone transfer and Pokemon summary screens.
- Held item content balancing for battle items, care items, travel/assignment utility items and rare regional equipment.
- Future move/technique management UI where the player can choose active classic moves, favorites and command-palette technique filters.

### Reusable UI Interaction Patterns

Status: **Base / Core Script Ready**

- Radial Context Menu / Segmented Ring UI is accepted as a future reusable option-selection pattern.
- It should not be implemented as a Party UI-only shortcut. The architecture should support Party slots, inventory items, tools, world interactions, ability/technique choices, creature actions and other option-based UI surfaces.
- The ring should carry action icons only. Text belongs to a separate hidden/focus-driven option tag/frame.
- The system should be data-driven through theme/layout assets rather than hardcoded per screen.
- Core scripts now exist for `RadialMenuOption`, `IRadialMenuProvider`, `RadialMenuController`, `RadialMenuView`, `RadialSegmentView`, `RadialOptionTagView`, `RadialMenuInputRouter`, `RadialMenuOpenBridge`, `RadialMenuTheme` and `RadialMenuLayoutProfile`.
- `RadialPartyMenuProvider` can expose Party slot actions such as Summary, Switch, Item, Held Item, Ability, Moves, Follow and Cancel as radial options without replacing the current PartyState/DynamicMenu flow yet.
- `RadialInventoryMenuProvider` can expose inventory item actions such as Use, Give, Equip, Teach, Info, Drop and Cancel as radial options without consuming, dropping or mutating items itself.
- `RadialWorldToolMenuProvider` can expose current overworld interaction info and optional tool actions as radial options without forcing the existing prompt/interactable flow to change.
- `RadialEncounterMenuProvider` can expose non-battle encounter resolution choice rows as radial options without replacing the existing `EncounterResolutionUIManager` or choice-source backend.

MVP scope:

- Done at core level: `RadialMenuOption` data model with id, icon, label, description, disabled state and callback/action payload.
- Done at core level: `IRadialMenuProvider` provider interface so existing UI slots can expose options without owning the view logic.
- Done at core level: `RadialMenuController` to open/close the menu, track selected segment, reset state on owner change and dispatch confirm/cancel.
- Done at core level: `RadialMenuView`, `RadialSegmentView` and `RadialOptionTagView` for visual ring segments, icon highlight and label frame.
- Done at core level: simple keyboard/input-router methods for previous/next/confirm/cancel.
- Done at core/bridge level: `RadialMenuOpenBridge` can open a controller from generic, party slot, inventory item, world interaction or encounter contexts without hardcoding a specific UI screen.
- First integration target: Party/creature slot actions such as Summary, Move, Item, Ability and Back/Cancel.
- Done at backend/provider level: Party slot option provider can resolve selected Pokemon from context, payload or current PartyScreen selection and emit selected action events.
- Done at backend/provider level: Inventory item option provider can resolve selected items from context payload, context index, item override or current InventoryUI selection and emit selected action events.
- Done at backend/provider level: World/tool option provider can resolve the current `OverworldInteractionSensor`, `InteractionPromptSource`, `Interactable` override or context payload and emit interaction/tool action events.
- Done at backend/provider level: Encounter option provider can resolve `EncounterResolutionUIManager` or `EncounterResolutionChoiceSource` rows and optionally run selected choices.

Later/polish scope:

- `RadialMenuTheme` ScriptableObject for icon colors, disabled colors, highlight colors, outline/glow settings and optional audio hooks.
- `RadialMenuLayoutProfile` ScriptableObject for radius, segment spacing, selected offset, animation duration, tag anchor rules and screen-edge clamping.
- Analog-stick angle selection.
- Sub-menu support.
- Context-specific themes for party, inventory, tools, world interaction, battle command helpers and Pokemon care.
- Accessibility/reduced-motion option.
- Pixel-art wedge sprites or mesh-based perfect pie segments if the simple rect/sprite MVP feels too rough.
- Adaptive positioning for screen-edge slots.

Affected existing UI areas:

- Party UI / party slots.
- Inventory item options and held item actions.
- Tool selection and activity action HUD.
- World interaction option prompts.
- Pokemon summary, storage/phone transfer and care/camp action choices.
- Encounter resolution choice UI.
- Future ability/technique/action command selection.

Dependencies and notes:

- Existing `SelectionUI`/slot focus behavior should remain the selection source; radial menu should sit on top as a reusable view/action layer.
- Requires icon assets for common actions such as Summary, Move, Item, Ability, Use, Equip, Drop, Back and Cancel.
- Requires a small visual prefab setup in the UI Scene: ring root, segment prefab and option tag/frame prefab.
- Should support disabled options and blocked reasons from existing UI backend rows.
- Slot changes must always close/reset the previous radial menu, selected segment and tag frame.
- First callbacks may log/debug, but the API should be ready to bind into real party, inventory and world action systems.
- Remaining: actual UI prefab construction, wedge/segment art, placing/configuring `RadialMenuOpenBridge` instances on PartyScreen/InventoryUI/OverworldInteractionSensor/EncounterResolutionUIManager surfaces, translating provider events into existing Summary/Switch/Item/Use/Equip/Interact/Encounter flows and final animation polish.

### Activity / Requirement Framework

Status: **Script Ready**

- Data-driven `ActivityDefinition`, zones, permissions, outcomes and requirement checks.
- Many reusable requirement types now exist for titles, milestones, reputation, relationships, research, jobs, shops, services, map, PokeNav, competitions, law, rumors, care, rides and other systems.
- Intended use: future systems can stay editable through ScriptableObject requirements instead of hardcoded checks.

Remaining:

- ScriptableObject content.
- Scene area setup.
- UI/debug views for activity availability.

### Shops / Services / Crafting

Status: **Script Ready**

- Shop catalog definitions, item brands and item models.
- Shop stock ledger and purchase history.
- Player shop basket log and basket sources for market/self-checkout style flows.
- Shop shelf definitions/sources for market aisles, counters, premium displays and filtered product sections that feed the basket system.
- Learnable offer definitions/sources for special shops that sell recipes, permits, research leads, PokeNav intel and map knowledge.
- Loyalty program definitions/sources for shop, clinic, inn, care, research or police memberships with points, tiers and price benefits.
- Services for reusable paid/free actions.
- Recovery items can use fixed values, percent HP, core/battle vital changes, timed recovery multipliers, end-turn recovery and temporary stat modifiers.
- Service package definitions/sources for bundled clinic, inn, daycare, professor, police, membership or appointment-like service flows.
- Sponsor definitions/logs/sources for shop discounts, sell bonuses, competition rewards and organization-style benefits.
- Recipe definitions, recipe grants and recipe vendor foundations.

Design notes:

- Big markets can be assembled from `ShopShelfSource` objects. Each shelf filters catalog offers and can add default, first visible or all visible offers to the active basket.
- `PlayerShopBasketLog` is the central basket state for future market UI and checkout UI.
- Loyalty programs are separate from sponsors: sponsors represent external agreements, while loyalty programs represent player memberships with points, tiers, discounts and access flags.
- Service packages should be used for multi-step services such as lodging plus food, Pokemon care plus grooming, professor starter research kits or paid membership perks.
- Item design can favor proportional effects over flat numbers: for example, a medicine can increase future healing rate, restore a percentage of core stamina, or grant a timed stat multiplier instead of only adding fixed HP.

- Latest cleanup:

- `ServicePackageDefinition` now builds a runnable execution plan before applying package contents, records expected/required totals, skips optional entries that cannot run and marks partial success if a required entry fails after changes were already applied.
- `ShopCatalog.TryCheckoutCart` now freezes checkout line prices before money, stock, inventory, sponsor and loyalty mutations.
- `LoyaltyProgramDefinition.CanJoin` now blocks `RefreshExistingOnly` programs when the player has no loyalty log/membership context.
- `ShopShelfSource.AddAllVisibleOffers` now reports partial add failures, and `ShopShelfDefinition` can match direct item override offers by offer id, display name, item asset/name or item type tags.
- `ProjectValidator` now scans editor project assets through an AssetDatabase-backed finder while keeping `Resources` as runtime fallback.
- `ProjectValidator` now also sees matching component sources on prefabs, not only open-scene instances.
- `ContentAuditProfileDefinition` Editor Project Assets scope now reads sub-assets and prefab components for component-backed content types.
- Validator rules now flag model-only shop/loyalty filters and optional paid service-package entries more clearly.
- `ShopPaymentRuleDefinition` now provides editable checkout rules for self-checkout, cashier, service counter and kiosk flows.
- `ShopCheckoutTerminal` can preview quotes, checkout baskets or clear baskets from scene triggers/interactables.
- `PlayerShopBasketLog` checkout records now store item subtotal, fees, discounts, amount due, amount paid, payment mode, payment rule and terminal kind.
- `ShopCatalog.TryCheckoutCart` now accepts payment quotes while preserving fixed line snapshots before inventory, stock, sponsor and loyalty mutations.
- `ShopReturnPolicyDefinition` now provides editable return windows, refund percentages, restocking fees, same-shop rules, payment-mode filters and access requirements.
- `PlayerShopReceiptLog` now records refund history, prevents duplicate refunded bundles and can refund full receipts or line items.
- `ShopRefundSource` can preview refund quotes or execute refunds from scene triggers/interactables.
- `PlayerShopLedger.RecordReturn` can restore limited-stock purchase counts when policy allows it.
- `ShopSecurityPolicyDefinition` now provides editable unpaid-basket security checks, catalog filters, thresholds, exit-block hints and Risk/Law consequence routing.
- `ShopSecuritySource` can preview/evaluate shop security from scene triggers or interactables, such as exits, guards, cameras or cashier alarm zones.
- `PlayerShopSecurityLog` records security incidents, unpaid basket value, line/bundle counts, risk record ids, law incident ids and block/clear outcomes.
- `ProjectValidator` and `ContentAuditProfileDefinition` now understand shop security policies, sources and player security logs.
- `ShopStockLimitPeriod` now supports Weekly stock windows while preserving existing serialized Daily/Total values.
- `ShopRestockScheduleDefinition` now provides editable manual, daily, every-N-days, weekly and calendar-event-linked stock refresh rules.
- `ShopRestockSource` can preview due restocks, run due restocks, force-run schedules or clean old stock history from scene triggers/interactables and TimeSystem events.
- `PlayerShopRestockLog` records restock history, restored bundle counts, affected offers, skipped/blocked states and source ids.
- `PlayerShopLedger` now exposes stock restore and old daily/weekly cleanup helpers used by scheduled restocks.
- `ProjectValidator` and `ContentAuditProfileDefinition` now understand shop restock schedules, sources and player restock logs.
- `ShopDeliveryServiceDefinition` now converts active shop baskets into delayed delivery orders with editable courier type, destination rules, fee rules, duration rules, cancellation rules and access requirements.
- `ShopDeliverySource` can preview delivery quotes, place delivery orders, claim due destination deliveries or cancel the latest pending order from scene triggers/interactables.
- `PlayerDeliveryLog` stores pending/delivered/cancelled delivery orders, auto-delivers due auto-fulfillment orders, claim-delivers destination orders and restores limited stock on cancellation when configured.
- Delivery orders reserve limited stock and charge Wallet at order time, then add items to Inventory only when delivered.
- `ProjectValidator` and `ContentAuditProfileDefinition` now understand shop delivery services, sources and player delivery logs.
- `ServiceAppointmentDefinition` now provides editable reservation rules for clinic, daycare, inn, grooming, professor, police or premium service appointments.
- Appointments can run a single `ServiceDefinition`, run a `ServicePackageDefinition`, or act as reservation-only reminders with no payload.
- Appointment schedules support manual slots, daily slots, every-N-days, weekly rules and calendar-event-linked availability.
- Booking fees, wallet charging, cancellation windows, refund percentages, provider tags, title/milestone/reputation/world-event gates and extra requirements are data-driven.
- `ServiceAppointmentSource` can preview the next slot, book appointments, complete due provider-claim appointments or cancel the latest pending appointment from triggers/interactables.
- `PlayerServiceAppointmentLog` stores pending/completed/cancelled records, auto-completes due auto-mode appointments and publishes appointment change events.
- Appointments can reveal linked calendar events on booking and complete those calendar entries when the appointment is completed.
- `ProjectValidator` and `ContentAuditProfileDefinition` now understand service appointments, appointment sources and player appointment logs.
- `MarketServiceUIManager` now provides a UI-facing backend contract for market/service screens without forcing final UI design.
- The market/service UI snapshot exposes shelf offers, basket lines, checkout history, pending deliveries, loyalty programs, service packages, appointment records and the most recent preview quotes.
- The manager includes action methods for basket updates, checkout preview/payment, refund preview/execution, delivery preview/order/claim/cancel, loyalty joins, service package use and appointment preview/book/complete/cancel.
- `ProjectValidator` and `ContentAuditProfileDefinition` now understand market/service UI managers.

Remaining:

- **Next priority:** Pokemon/player survival needs expansion.
- Market/basket UI bound to `PlayerShopBasketLog`, `ShopBasketSource` and `ShopShelfSource`.
- Shelf/grid UI bound to `ShopShelfSource.GetVisibleOffers` and `PlayerShopShelfLog`.
- UI Scene placeholder/mockup panels for market/service screens using simple Image/Text/Button layouts.
- Shop security SO content for normal marts, premium stores, police-supervised shops and self-checkout areas.
- Scene placement for exit alarms, guards, cameras and shop security zones.
- Restock schedule SO content for normal daily marts, weekly rare goods, event markets and region-specific vendor refreshes.
- Scene placement for shop restock sources or manager objects.
- Delivery service SO content for Pidgey/Pelipper/courier variants, fee tiers, delivery durations and destination rules.
- Scene placement for delivery counters, mailbox/claim points and destination-specific claim sources.
- Special shop scene setups.
- Recipe catalog content.
- Learnable offer content for herbalist, Pokeball smith, bait seller, professor/research desks and police shops.
- Loyalty program content for marts, clinics, inns, daycare/ranch services, regional stores and special organizations.
- Service package content for clinics, inns, daycare/ranch counters, professor desks, police desks and premium memberships.
- Service appointment SO content for clinics, daycare/ranch visits, grooming, inns, professor meetings and police appointments.
- Scene placement for appointment booking counters, claim desks, cancellation desks and calendar-linked event providers.
- Sponsor SO content and sponsor UI/log presentation.

Known issues from latest system check:

- `ServicePackageDefinition` is safer but still not fully rollback-transactional. If external state changes between preflight and execution, it records `partialSuccess` instead of undoing already-applied effects.
- Runtime `Resources.LoadAll` usage should eventually be replaced or cached for heavier systems.
- Shop shelf category/brand/quality filters still require `ItemModelDefinition`; direct `itemOverride` offers can now match tag-like filters but not model-only filters.
- `ShopSecuritySource` reports `blockedExit`, but actual movement/portal blocking needs the door, portal or scene transition script to call `TryEvaluateAndShouldBlockExit` and respect the result.
- Restock model refreshes ledger purchase counters. It does not yet create rotating offer pools; seasonal/event shelves should use schedule-filtered limited stock plus existing shelf/catalog filters until a dedicated rotating-stock layer is needed.
- Delivery order placement does not reuse `ShopCatalog.TryCheckoutCart` because checkout grants items immediately. Delivery has its own reservation flow, so future loyalty/sponsor presentation may need a delivery-specific UI/log pass.
- Appointment booking fee is separate from the service/package payload cost. If an appointment should be fully prepaid, configure the payload service/package as free or treat the booking fee as the full price in content.
- `MarketServiceUIManager` is a backend/view-model contract only. Final UI binding and UI Scene placeholder layout still need a later scene pass.

### Map / PokeNav / Pokedex Plus

Status: **Script Ready**

- Pokedex-style Pokemon knowledge.
- Region info cards.
- PokeNav entries, guide sections, social posts and feed items.
- Map marker definitions and player map discovery log.
- Map view profiles and map navigation targets.
- Discovery sources for map/PokeNav unlocks.
- `PokeNavMapUIManager` now provides a UI-facing backend contract for minimap, world map, Pokedex, PokeNav guide, feed, social posts, region info and knowledge entries.
- The PokeNav/map UI snapshot exposes visible map markers, active navigation target, guide sections/items, feed rows, social post rows, Pokedex rows, region rows and knowledge entry rows.
- The manager includes action methods for map target set/clear/reached, marker favorite/hidden toggles, guide read/pin/dismiss, feed read/pin/dismiss/unlock, social post read, region discovery and knowledge entry discovery.
- `PokeNavMapFilterUIManager` exposes marker filter rows for profile presets, categories, tags, regions, scenes, search text and favorite/discovered/important/hidden toggles without requiring final UI layout.
- PokeNav feed items can publish into the global `NotificationFeed`, so rare sightings, market notices, police notices, transit updates and research leads can appear in both PokeNav and the live side-log.
- `ProjectValidator` and `ContentAuditProfileDefinition` now understand PokeNav/map UI managers.

Design notes:

- PokeNav is broader than a Pokemon encyclopedia: it can host region info, sightings, social/news posts, event hints and map-related data.
- Map UI/minimap UI should later read these systems instead of hardcoding markers.

Remaining:

- PokeNav UI.
- Minimap/world map UI.
- `PokeNavMapUIManager` exposes map markers, guide sections/items, feeds, social posts, Pokedex rows, region rows and generic knowledge rows for tabbed PokeNav screens.
- `PokeNavKnowledgeDetailUIManager` exposes selected Pokedex, region or knowledge-entry detail snapshots, including Pokemon habitats, care hints, region Pokemon, shops, activities, transit stops, job boards and linked content rows.
- `PokeNavMapFilterUIManager` exposes category/tag/region/scene/profile/search filter snapshots and filtered marker rows for future minimap/world map sidebars.
- UI Scene placeholder/mockup panels for PokeNav, minimap, world map, Pokedex, guide and feed screens.
- Pokedex/PokeNav content.
- Region/event/social feed content.
- Example PokeNav/map SO content at the final tutorial/example-SO stage.

### Encounter / Capture / Research

Status: **Script Ready**

- Encounter table foundations.
- Encounter source profiles for grass, water, tree, cave, roaming/static/event-style sources.
- `EncounterSource` scene component for touch/interact/manual source execution without hardcoding source behavior.
- Encounter source profiles can gate rolls by activity zone type/tag and extra reusable requirements.
- Encounter source outcomes can start battle, try stealth capture, try stealth-only capture or record seen-only sightings.
- World conditions can now multiply encounter rates through `WorldConditionDefinition.EncounterRateMultiplier`.
- Encounter log requirements.
- Stealth/silent capture foundations.
- Non-battle encounter resolution definitions can now model capture, calm, feed, observe, distract, treat or custom approaches without forcing a battle.
- Encounter resolutions support reusable requirements, item costs, Pokemon type/source modifiers, success/failure outcomes, activity outcome rewards and result history.
- `EncounterResolutionSource` can run an exact Pokemon or encounter-table-based resolution attempt from interaction/trigger sources.
- `EncounterResolutionChoiceSetDefinition` can group several resolution actions into one editable choice set, such as observe, feed, calm, treat, capture or flee-style options.
- `EncounterResolutionChoiceSource` can expose a full choice set for an exact Pokemon or encounter-table rolled Pokemon and optionally run the first available choice for quick tests.
- `EncounterResolutionUIManager` exposes UI-ready choice rows, blocked reasons, item cost labels, chance previews, outcomes and recent resolution history rows.
- `PlayerEncounterLog` now saves peaceful resolution and failed resolution counts plus detailed resolution attempt history.
- Overworld encounter node/path foundations for visible roaming Pokemon, rare routes, nests, tree/cave/water hotspots and controlled patrol movement.
- Overworld encounter nodes now support editable terrain/access flags, actor blocks, movement capability requirements and weighted route costs.
- Overworld encounter connections now support editable route flags such as walk, surf, swim, fly, climb, jump, cut, ride, dangerous, stealth route, one-way, map transition and blocked-by-default.
- `OverworldEncounterPathfinding` can calculate full or partial routes through the node graph while respecting agent kind, movement capabilities, node requirements, connection requirements, dangerous-route preferences and optional node group limits.
- `OverworldEncounterPathAgent` can now follow temporary manual routes, so flee, escort, lure, scripted travel or future player/NPC adapters can reuse the same movement component without replacing patrol/random modes.
- `OverworldNodeMovementAdapter` can translate an input/AI direction into the best valid outgoing node connection and move through `OverworldEncounterPathAgent`, giving player/NPC node movement a script-ready bridge without replacing the current PlayerController yet.
- `OverworldNodeMovementInputBridge` can read an Input System move action and forward pressed/held directions into `OverworldNodeMovementAdapter` as an optional player-control bridge.
- `OverworldEncounterDebugSource` can build node/group/agent/connection/flee-record snapshots and run test path or test move checks for future debug UI and scene validation.
- `OverworldEncounterDebugUIManager` exposes UI-ready node rows, selected-node connection rows, active flee rows, filters and test path/move actions for future debug panels.
- `OverworldEncounterFleeController` can send alerted Pokemon/NPCs toward safe, hidden, spawn-exit or map-exit nodes based on threat distance, node tags, node flags and reachable path score.
- `PlayerOverworldFleeLog` saves virtual/unloaded-map flee records, including fleeing entity id, optional species info, scene, start node, escape node, last position, threat position, timer expiry and recovered/expired/cleared state.
- Flee controllers can automatically record virtual flee state when ending on Spawn Exit or Map Exit nodes, or through explicit record-and-pause/resume/disable/destroy finish modes.
- `OverworldFleeRecoverySource` can read active virtual flee records and either mark them recovered, spawn a prefab, or re-enable an existing object when the player reaches the matching scene/node before expiry.
- Project validator and content audit understand encounter source profiles, encounter source components, encounter tables, stealth capture profiles, encounter resolution definitions/sources and overworld encounter path components.
- Research subjects and player research log.
- Research progress can be granted through activities, learnable offers and services.

Design notes:

- Node-path roaming is preferred over fully random roaming for visible overworld Pokemon. It keeps movement readable, avoids bad random drift and allows special mechanics such as stealth approach routes, rare node locks, nests, lures, feeding spots and timed migration paths.
- This should stay lightweight: designers place nodes and connections in allowed encounter areas instead of running expensive global pathfinding for every wild Pokemon.
- Movement flags are intentionally general enough to later serve player movement, NPC movement, ride traversal, elevation gates, stealth flee routes and loaded-neighbor map escape logic without replacing the current player controller yet.
- Planned node v2 direction: keep node data terrain-only/lightweight where possible, and move movement cost, speed, slide, traversal permission and visibility behavior into data-driven terrain/traversal/perception profiles.
- Planned subnode direction: use subnodes for local companion/follower placement, formation and camp positioning rather than as the main global pathfinding graph.
- Planned scene streaming direction: use a scene-ring model where the current scene runs full simulation, neighbor scenes keep visual/collision/navigation with limited simulation, and neighbors-of-neighbors provide navigation data only.
- Planned encounter AI direction: Pokemon should not auto-trigger on sight. Species, personality, mood/vitals/state, stealth/approach style, weather, terrain and perception profile should decide whether the response is flee, approach, observe, warn, attack, hide or ignore.
- Planned flee/pursuit direction: replace simple "run outside player circle" logic with an escape resolver that scores safe nodes, line-of-sight breaks, terrain suitability, traversal abilities, dead ends, neighbor-scene exits and nav-only scene targets.

Remaining:

- Spawn placement and balancing.
- Scene node placement for routes, forests, caves, water edges, trees, nests and rare Pokemon paths.
- Node v2 planning pass for terrain-only node data, terrain profiles, traversal profiles, subnode placement and migration strategy from current GameObject/node components.
- Scene navigation data planning pass for sceneId, bounds, terrain grid, exits, neighbor references, entry nodes, escape candidate nodes, traversal links, encounter regions and biome/area metadata.
- Scene-ring streaming planning pass for Ring 0 full simulation, Ring 1 limited simulation and Ring 2 navigation-only data.
- Personality-gated encounter trigger planning pass that connects Pokemon personality/nature, mood, vitals, stealth, weather, world conditions and perception profiles.
- Flee/pursuit persistence planning pass for richer fleeing encounter records, last-known sightings, pursuit recovery, rare Pokemon rumors and quest-critical escape behavior.
- Optional scene test where a duplicate/player test object uses node movement input without replacing the current main PlayerController.
- Recovery prefab/content setup for Pokemon/NPCs that can reappear after virtual flee.
- Final UI/debug overlay prefab that renders `OverworldEncounterDebugUIManager` snapshots in a panel.
- Final visual UI/minigame for calm, feed, observe, treat, bait and capture attempts using `EncounterResolutionUIManager`.
- Encounter resolution SO content for bait, calming, field research observation, injured Pokemon treatment, rare Pokemon approach and no-battle capture variants.
- Encounter resolution choice set SO content for common wild Pokemon, rare Pokemon, injured Pokemon, ranger tasks, research tasks and tutorial encounters.
- Research content and rewards.
- Encounter source SO content for grass, water, tree, cave, roaming/static/event and rare-source variants.
- Scene placement for encounter sources and roaming Pokemon.

### Farming / Resources / Pokemon Care

Status: **Script Ready**

- Farmable definitions.
- Resource node definitions.
- Pokemon care actions, care needs and care facilities.
- Pokemon care actions can restore or change core health, core physical stamina, core elemental stamina and battle stamina through editable vital changes.
- Passive party Pokemon care need controller for hunger, energy, cleanliness, mood-like or custom care loops.
- Pokemon care needs can define hourly active/rest/sleep changes, low/critical thresholds and optional threshold events.
- Player survival needs now keep recent change history, publish low/critical/recovered events and can trigger Pokemon care rest/sleep updates.
- `SurvivalNeedsUIManager` exposes UI-ready player need rows, worst state, action penalty, low/critical counts, recent change history and safe Eat/Rest/Sleep/Change actions without forcing a final HUD layout.
- Area/profile/permission requirements can restrict farming/mining/care activities to allowed zones.
- Camp Station definitions can now bundle campfire, tent, picnic, care mat, training spot, ranger camp or research camp actions into editable station assets.
- `CampStationDefinition` supports zone filters, access profiles, requirements, visible/locked action rows and mixed actions such as activities, rest, sleep, Pokemon care actions, social activities, Pokemon assignments, assignment boards, situation events, situation pools, role activity boards and Life Path rewards.
- `CampStationSource` can sit on an overworld object as an interactable or trigger source, produce a UI-ready snapshot, record views, run a configured action or run the first currently available action.
- `PlayerCampStationLog` saves station view/run/blocked history with source, region, zone, action and result data for future camp UI/debugging.
- `CampStationUIManager` exposes station action rows, station history, view recording and run actions for future camp/care/rest UI panels without final UI layout.
- `PokemonPartyCareStatusUIManager` exposes party Pokemon care needs, core/battle vital resources, urgent treatment/rest flags and recent care need changes to future party, camp, care and debug UI.
- PlayerSystemsInstaller, RuntimeHealthMonitor and ProjectValidator know about camp station logs, definitions, sources and UI managers.

Design notes:

- Farming and mining should not be full sandbox everywhere. They should work in permitted areas.
- Farming should include Pokemon care-style loops, not only berries/apricorns.
- Care services can distinguish quick battle HP healing from deeper long-term treatment/rest through Pokemon vital resources.
- Camp stations should be placed in permitted camp, farm, route, facility or settlement zones rather than turning the whole world into a free-form camp sandbox.

Remaining:

- Allowed area/zone SO content.
- Farmable/resource/care assets.
- Camp station SO content for campfires, tents, picnic tables, Pokemon care mats, training spots, ranger camps and research camps.
- Pokemon vital profile SO content for normal, hardcore, relaxed or region/ruleset-specific recovery balancing.
- Pokemon care need SO content for hunger, energy, cleanliness, affection or custom needs.
- Player survival need SO content and balancing for decay/rest/sleep values.
- Scene object placement for care stations and camp station sources.
- UI Scene binding for player survival rows and recent change history using `SurvivalNeedsUIManager`.
- UI Scene binding for party Pokemon care/vital rows and recent care changes using `PokemonPartyCareStatusUIManager`.
- UI Scene binding for facility/care/player survival/camp station state, using `CampStationUIManager` where station action rows are needed.

### RPG Progression / Access

Status: **Script Ready**

- Player progression.
- Titles, badges, permits and licenses.
- Milestones.
- Reputation factions.
- Relationships.
- Lifestyle/playstyle points.
- Life Path / Vocation XP, branch progress, tag counters and perk unlocks.
- Careers.
- Organizations.
- Assignments.
- Generic access profiles.
- `ProgressionAccessUIManager` exposes titles, permits, badges, careers, milestones, reputation and access checks as one UI-facing snapshot/action contract without requiring final UI layout.
- `ProgressionFocusedPanelUIManager` can read a `ProgressionAccessUIManager` snapshot and expose smaller focused panels for overview, titles, careers, milestones, reputation and access rows with search/tag/status filters.

Design notes:

- Titles and permits should unlock playstyles, services, research rights, shops, activities and regions.
- Life Paths should sit above lifestyle/career systems as the main long-term role identity layer.
- Professor/police/organization routes can grant temporary or permanent access.
- Progression/access UI should be usable as a status screen, unlock browser, permit/license screen or professor/police/title board depending on which pools and filters are assigned.

Remaining:

- Content design for life paths, branches, perks, titles, permits, jobs, organizations and career paths.
- More Life Path reward hooks for quests, contests, competitions, investigations, assignments, organizations, dialogue graphs and future event systems.
- UI Scene binding for progression/access panels using `ProgressionAccessUIManager`.
- UI Scene binding for focused progression sub-panels using `ProgressionFocusedPanelUIManager`, such as title/license tab, career tab, milestone tab, reputation tab and access/permit tab.

### World / Region / Travel / Ride

Status: **Script Ready**

- World region definitions.
- Region travel routes and travel points.
- Region travel policies for selectable travel styles such as full party, one Pokemon only, store party except selected Pokemon, optional challenge or required challenge travel.
- Region challenge profiles.
- Transit routes and stops.
- Transit journey definitions/sources/logs for vehicle interiors, active onboard travel, stop dwell windows, disembark/continue choices and onboard activity history.
- `TransitJourneyUIManager` exposes vehicle journey options, active journey state, leg rows, history rows and onboard activity actions without requiring final UI layout.
- Transit journeys can trigger Journey Incident definitions or boards on journey start, leg departure, stop arrival, stop continuation, disembark, completion, cancellation or onboard activity records.
- Transit-region handoff definitions/sources bridge active vehicle journeys or stop states into region travel routes and policy choices.
- Ride Pokemon definitions, ride points and player ride controller/log.
- Ride companion capacity policies, companion self-travel capability, coordination log and player coordinator.
- Journey incident definitions, boards, sources and player log for route/camp/travel incidents.
- Journey incident UI backend for PokeNav/map/route-event panels.
- Location visit logs.
- Navigation hints.
- Area profiles.
- Calendar events.

Design notes:

- Multi-region open-world structure is supported at the backend level.
- Region-specific tournaments, rules, travel restrictions and optional Pokemon travel constraints can be built on top.
- Transit is no longer limited to instant point-to-point travel: `TransitJourneyDefinition` can represent trains, ferries, buses or airships with ordered legs, vehicle interior scene hints, dwell timers and stop rules.
- `PlayerTransitJourneyLog` tracks the active vehicle journey, current stop/leg, remaining travel time, disembark/continue state and onboard activities such as sleeping, research, waiting or talking.
- Transit journey UI can be split into separate station boards, vehicle interior panels, stop prompt popups and journey history tabs because it reads from the same snapshot/action manager.
- Transit journey incident hooks let content create optional events such as delays, onboard NPC encounters, rare sightings from a ferry deck, police checks, weather interruptions or station incidents without hardcoding them into routes.
- Transit-region handoff rows expose active journey/stop state, destination region, route policy, selected Pokemon behavior and blocked reasons so stations, vehicle exits or map triggers can choose a valid regional transfer without hardcoding it.
- Region travel can now record which policy option was used, which Pokemon was selected and which party transfer mode applied.
- A route can keep old behavior with no policy, or use policy options for choices like "go with all Pokemon" versus "start fresh with one Pokemon".
- Ride system is prepared as a gameplay backend; asset alignment/animation matching remains a content task.
- Ride capacity can be data-driven per ride, ride mode, ride tag, mounted Pokemon species or mounted Pokemon type.
- Companions can fit within capacity, self-travel if they have a matching capability component, detach until dismount, return after an in-game delay, or force dismount depending on policy.
- Companion self-travel can optionally require a healthy matching Pokemon from that companion's own `CompanionPokemonTeam`, using ride species/type/move/level rules.
- Journey incidents sit above situation events as route/camp/travel-level events. They can activate timed active states, resolve or expire, trigger situation events/pools, apply Life Path rewards and run consequence chains.
- Incident boards can be attached to route signs, camp objects, ranger/police sources, transit stops or region triggers to roll weighted incidents without hardcoding the content.
- `JourneyIncidentUIManager` exposes active incidents, recent history, direct incident rows and board snapshots without committing to final UI layout.

Remaining:

- Region assets/maps.
- Region travel policy SO content for normal ferries, airports, league challenge starts and new-region reset variants.
- Transit/ride scene setup.
- Transit journey SO content for trains, ferries, buses, airships and special routes, plus vehicle interior scene binding if used.
- Transit journey incident hook SO content for route delays, onboard social events, research sightings, station disruptions and regional surprises.
- Transit-region handoff SO content for airport/ferry/train exits, border gates, league challenge departures and one-Pokemon regional reset variants.
- UI Scene binding for `TransitJourneyUIManager` panels such as station journey list, onboard travel status, stop arrival prompt and activity buttons.
- Journey incident SO content for route help requests, camp visitors, rare sightings, ranger alerts, weather trouble, transit delays and regional surprises.
- Scene placement for `JourneyIncidentSource` components and future board UI bindings.
- UI Scene binding for `JourneyIncidentUIManager` panels, including PokeNav/map route event tabs and active incident detail cards.
- Ride companion policy SO content for small/large mounts, surf/fly/climb rides and companion overflow behavior.
- CompanionRideCapability and CompanionPokemonTeam setup for important companions that can keep up with their own travel method.
- UI for map, travel and ride selection.
- Regional event/challenge content.

### Rumor / Risk / Law / Investigation

Status: **Script Ready**

- Rumor definitions and player rumor log.
- Rumor lifecycle/spread/decay backend.
- Risk incidents and player risk log.
- Law violations and law log.
- Investigation cases/clues.
- NPC memory/reaction/witness/report propagation systems.

Design notes:

- Rumors should not instantly become global knowledge. Importance, location, severity and decay should control spread and forgetting.
- Minor events can stay local and fade quickly; serious incidents can reach nearby towns or police and last longer.

Remaining:

- Rumor spread content.
- Law/investigation case content.
- NPC witness placement and scene wiring.
- UI/debug presentation.

### NPC / Companion / Jobs / Customization

Status: **Script Ready**

- NPC visual sets, variant pools and randomizer foundations.
- Scene-level NPC randomization profiles for route/town/chunk first-load generation.
- `NPCSceneRandomizationSlot` marks eligible common NPCs while fixed/story NPCs can be protected.
- `NPCSceneRandomizationController` can discover manual, child or whole-scene slots and apply deterministic generated variants.
- Scene NPC randomization can reuse slot-level pool overrides, profile rules, role filters, tag filters and requirement gates.
- Generated scene NPC records can be saved and restored so the same save keeps the same common NPCs unless rerolled.
- Project validator and content audit understand NPC scene randomization profiles, slots and controllers.
- Trainer party templates.
- NPC memory and reactions.
- NPC schedules.
- Companion roles/perks.
- Companion expeditions and multi-stage expedition routes.
- Social activity definitions, sources and player log for hangouts, dates, camp activities, meals, festivals and Pokemon/companion bonding.
- Social activities can reuse `ActivityDefinition` costs, area checks, XP and rewards while adding companion bond, Pokemon friendship, mood/care changes, relationships, reputation, titles, recipes and milestones.
- `SocialActivitySource` lets a scene object run a social activity from overworld interaction or trigger logic, while future UI can sit on top as a selection layer.
- Pokemon follower catalog/visual definitions, player follower controller, follower log and simple selection source.
- Active party Pokemon can follow the player through catalog-resolved prefabs or directional/fallback sprites while saving selected slot/mode and follower history.
- Companion Pokemon roster definitions, companion runtime team component and simple overworld team source.
- Companions can be given editable Pokemon rosters, restore/save runtime teams, heal/reset from roster and expose matching Pokemon for ride self-travel checks.
- Companion node-follow profiles, player node trail tracking and companion node-follow controllers.
- Companions and future follower Pokemon can follow the player's recent valid nodes through existing path agents instead of chasing the player transform directly.
- Node-follow uses node groups, movement capabilities, node flags, pathfinding and catch-up policies so ride, elevation, stealth and traversal gates can be shared.
- Job definitions, boards and repeatable tasks.
- Generic Role Activity Boards can now expose mixed actions from one data-driven board: activities, jobs, Pokemon assignments, Pokemon assignment boards, social activities, situation events, situation event pools and Life Path rewards.
- `RoleActivityBoardDefinition` stores board identity, category, tags, access profile, requirements, visible/hidden row behavior and editable mixed entries without hardcoded police/professor/ranger/camp content.
- `RoleActivityBoardSource` can sit on a scene object as an interactable/trigger source, produce a UI-ready snapshot, record views, run a configured entry or run the first currently available entry.
- `PlayerRoleActivityBoardLog` saves board view/run/blocked history with source, region, zone, entry and result data for future UI/debugging.
- `RoleActivityBoardUIManager` exposes board rows, visible/locked counts, history, view records, configured entry runs and first-available runs for future professor, police, ranger, festival, camp notice or route task panels.
- PlayerSystemsInstaller, RuntimeHealthMonitor and ProjectValidator know about role activity board logs, definitions, sources and UI managers.
- Player origins and customization foundations.

Remaining:

- NPC sprite/base/clothing content.
- NPC scene randomization SO content for common town/route/market/trainer populations.
- Scene setup for random/common NPC slots and controllers.
- Companion/social activity content and UI.
- Social activity SO content for camp, date, festival, Pokemon play/care, training and companion route events.
- Scene placement for social activity sources such as camp spots, festival booths, cafes, picnic tables or companion hangout points.
- Pokemon follower visual SO content, species prefab/sprite setup and polish for offsets/sorting/animation.
- Optional follower selection UI for party menu, camp UI or PokeNav/player menu.
- Companion Pokemon roster SO content for named companions, rival parties, guest allies and travel-capable partners.
- Scene setup for companion team components on recruitable or important companion GameObjects.
- Companion node-follow profile SO content for normal walkers, swimmers, climbers, flyers, stealth followers and large-body followers.
- Scene/prefab setup for follower path agents, node groups and player trail tracking.
- Role board SO content for professor research boards, police task boards, camp notice boards, ranger help boards, festival boards and route-specific activity boards.
- UI Scene binding for job/role board panels using `RoleActivityBoardUIManager`.
- New game UI and customization UI.

### Competition / Battle Rules / Power Mechanics

Status: **Script Ready**

- Battle AI profiles now support editable tiers such as wild, amateur, skilled, expert and champion.
- AI profiles can score move + target combinations instead of choosing a move and random target separately.
- AI profiles can use targeting policies, type effectiveness, STAB, accuracy, low-HP finish bonuses and status-move weighting.
- Trainer AI can optionally consider switching Pokemon when low on HP or in a bad matchup, while still respecting battle rule switch limits.
- TrainerController can override the default BattleSystem trainer AI profile per trainer.
- NPC variant pools can optionally assign a trainer AI profile to generated trainer variants.
- Project validator and content audit understand Battle AI profile assets.
- Battle rule set definitions and battle challenge hooks.
- Battle mode definitions now separate classic/current battle flow from future command-palette or hybrid modes.
- Player battle mode preferences can be saved and resolved by battle challenges when allowed.
- Battle challenges and negotiators can provide default, allowed or forced battle modes without hardcoding them into trainers.
- `BattleModeOptionsUIManager` exposes new-game/options/challenge battle mode rows, selected preference state, forced/default/resolved context data, preview results and select/clear/prefer-for-challenges actions without requiring the future command-palette battle backend.
- Move definitions can now define battle physical/elemental stamina costs and optional core-health pressure for severe or special attacks.
- Battle rule sets can now provide a vital profile and decide whether Pokemon spend core stamina on battle entry and whether battle HP is capped by core health for that ruleset.
- BattleSystem, BattleUnit, move selection and trainer AI now read the active rule vital profile as a fallback for move stamina costs, move availability, battle entry vitality and core-health pressure.
- Competitions, rankings, honors, seasons, entrants, rosters and bracket sources.
- Prize tables, registration windows, invitations and venues.
- `CompetitionRegistrationUIManager` exposes tournament/contest registration rows, invitation/pass state, venue availability, registration history and bracket summaries without requiring final UI layout.
- `CompetitionBracketRankingUIManager` exposes ranking tracks, tier progress, point history, seasons, active/completed brackets and per-match rows without requiring final UI layout.
- Contest definitions.
- Sponsor integration for competition-like rewards.
- Power mechanics for Mega Evolution, Z-Move and Gigantamax-style systems.

Design notes:

- Battle rules can limit party count, type eligibility, duration, allowed power mechanics and other format rules.
- Battle modes are metadata/profile assets for routing and UI decisions. The current backend safely falls back to the classic battle loop until a custom battle backend exists.
- Core vitality supports RDR2-like split resources: current battle HP/stamina can recover from long-term core reserves, while configured severe/overkill/special move damage can reduce core health.
- Pokemon entering battle have battle HP capped by their core health ratio, so a Pokemon at 70% core health cannot act as if fully healthy even if quick HP healing was used.
- Power mechanics should obey battle rules and trainer charge/use limits.
- Battle Frontier and World Championship can be built with the competition stack.
- Competition registration UI should support both small desk/kiosk flows and larger tournament browser panels by assigning pools, scene sources or Resource-backed definitions.
- Competition bracket/ranking UI should support separate league pages, PokeNav ranking pages, stadium bracket screens and match-history panes by reading the same snapshot rows.

Remaining:

- UI Scene binding for registration, invitation, venue and bracket panels using `CompetitionRegistrationUIManager`.
- UI Scene binding for bracket/ranking/season/match-history panels using `CompetitionBracketRankingUIManager`.
- UI Scene binding for choosing a preferred battle mode at new game/options/challenge time using `BattleModeOptionsUIManager`.
- Tournament content.
- Battle integration testing.
- Ruleset-specific vital profile SO content, move cost balancing and battle UI display for HP/core/stamina state.
- Power mechanic battle UI/animation/content.

### Events / Notifications / Dialogue / Debugging

Status: **Script Ready**

- Game events and event bus.
- Notification feed foundations.
- `NotificationFeedUIManager` exposes the global notification feed as a UI-ready snapshot with filters for read/unread, pinned entries, kind, channel, priority and search text.
- Notification feed UI actions can publish template/manual notifications, mark one/all entries read or unread, remove entries and clear normal or pinned entries.
- Speech bubble dialog manager and speech bubble styles.
- Conditional dialogue definitions.
- GameDebug logger.
- Runtime health monitor.
- Project validator.
- Content audit profiles/runners.
- Asset audit foundations.
- Runtime survival and Pokemon care need history records for future UI/debug views.

Design notes:

- Dialogue direction is speech bubbles above characters instead of only classic Pokemon dialog boxes.
- World notifications can later be logged like an MMO-side feed.
- Validator/audit systems should keep catching missing references and duplicate ids as content grows.
- When UI backend contracts are added, the UI Scene should get simple placeholder/mockup panels later so the user can visually edit layout before final binding.

Remaining:

- UI styling for speech bubbles and notification feed using `NotificationFeedUIManager`.
- More validator rules as new content patterns appear.
- Asset scan report pass near the end of system work.

### Main UI Surface Plan

This list tracks the major UI surfaces that should exist once the core systems are stable. These are design targets, not all final prefabs.

1. **PokeNav / Pokedex Hub**
   - Central phone/tablet-style interface for map, minimap expansion, Pokedex, region guide, social/news feed, sightings, events, trainer info, rumors and research notes.
   - Backend status: `PokeNavMapUIManager`, `PokeNavKnowledgeDetailUIManager` and `PokeNavMapFilterUIManager` are script-ready; final visual UI and content are pending.

2. **Notification Side Log**
   - MMO-like side feed for discoveries, pickups, warnings, sightings, shop/security events, transit notices, quest updates and world reactions.
   - Backend status: `NotificationFeed` and `NotificationFeedUIManager` are script-ready; final styling and placement are pending.

3. **Speech Bubble Dialogue**
   - Above-character dialogue bubbles instead of only classic Pokemon-style dialogue boxes, with optional emotion color, icon, emphasis and short animation support.
   - Backend status: speech bubble styles and conditional dialogue foundations exist; final visual prefabs, animation rules and binding polish are pending.

4. **Contextual Interaction Prompt**
   - Small nearby-object interaction UI for shelves, trees, rocks, phones, camp stations, transit gates, NPCs, activity zones and encounter objects.
   - Should show only relevant actions and blocked reasons, avoiding full-screen menus for simple world interactions.

5. **Radial / Segmented Ring Menu**
   - Reusable option wheel for party slot actions, inventory item actions, tool/world interaction choices, Pokemon care actions and encounter resolution choices.
   - Backend status: radial option model, provider contract, controller, view, input router, open bridge, theme/layout profiles and party/inventory/world/encounter providers are script-ready; prefab art and screen wiring are pending.

6. **Camp / Pokemon Care UI**
   - Care, rest, feeding, treatment, grooming, play, station actions and party care status panels.
   - Backend status: `SurvivalNeedsUIManager`, `PokemonPartyCareStatusUIManager` and `CampStationUIManager` are script-ready; UI Scene binding and care content are pending.

7. **Battle Command Palette UI**
   - Future custom battle interface for base action selection, element/modifier selection, info card preview, AP/stamina/energy costs and confirmation flow.
   - Backend status: not implemented yet; existing battle mode/options contracts only prepare routing between classic and future modes.

8. **Party Status / Vital HUD**
   - Compact party strip showing battle HP, core health, stamina, care warnings, status effects and urgent treatment/rest flags.
   - Backend status: party care/vital snapshots are script-ready; final HUD and status-effect animation overlays are pending.

9. **Activity / Role Board UI**
   - Shared board UI for professor tasks, police requests, ranger work, farm/camp boards, festivals, contests, investigations and route jobs.
   - Backend status: `RoleActivityBoardUIManager` is script-ready; content and visual board layouts are pending.

10. **Market / Basket / Shelf UI**
   - Small basket panel plus shelf interaction panels for products, brands, models, checkout, returns, delivery, stock, security and service appointments.
   - Backend status: shop/service/basket/shelf/checkout/refund/security/restock/delivery systems and `MarketServiceUIManager` are script-ready; final shelf and basket UI are pending.

11. **Map / Minimap UI**
   - Moving minimap plus larger map view with markers, filters, transit stops, activity zones, sightings, quest/event markers, danger and region knowledge.
   - Backend status: PokeNav map managers are script-ready; moving minimap camera/marker binding and visual layout are pending.

12. **Life Path / Career / Access UI**
   - Player identity and progression UI for paths, branches, perks, titles, permits, badges, careers, milestones, reputation and access checks.
   - Backend status: `LifePathUIManager`, `ProgressionAccessUIManager` and `ProgressionFocusedPanelUIManager` are script-ready; final tree/panel visualization is pending.

13. **Pokemon Growth / Ability UI**
   - Pokemon training, growth traits, known techniques, active moves, potential, care state, evolution hints and future Pokemon ability tree screens.
   - Backend status: growth, technique memory and evolution foundations exist; Pokemon-specific ability tree is still planned.

14. **Transit / Vehicle Interior UI**
   - Station board, active journey panel, onboard activities, stop arrival prompt, disembark/continue actions and travel history.
   - Backend status: `TransitJourneyUIManager` and transit-region handoff systems are script-ready; vehicle interior UI and scene binding are pending.

15. **Competition / Tournament UI**
   - Registration, invitations, venues, bracket, rankings, seasons, match history, Battle Frontier and World Championship pages.
   - Backend status: competition registration and bracket/ranking UI managers are script-ready; final venue/stadium/PokeNav views are pending.

16. **Minigame Venue UI**
   - Arcade, casino, festival booth or special venue screens for entry cost, chip balance, minigame launch, result summary, prize exchange and venue history.
   - Backend status: planned bridge layer; actual minigame types can be decided later.

17. **Debug / Developer Overlay**
   - Development-only panels for active zone, current node, path agent, encounter state, situation events, signal logs, player needs, party care, notifications, validation and asset audit.
   - Backend status: several debug/UI managers exist, including situation signal and overworld encounter debug; unified overlay layout is pending.

## Deferred / Removed For Now

- Custom new battle command-palette system: deferred until Battle UI is ready.
- Multiplayer: optional late-game idea, best kept separate from main single-player backend.
- Full UI pass: intentionally later, after system foundations stabilize.
- Scene-wide ScriptableObject creation and assignment: user will handle content creation first; tutorial can be made later.

## Remaining System Backlog

This is the current source of truth for systems that are not fully closed yet. Many existing systems are script-ready, but still need ScriptableObject content, UI prefabs, scene placement, balancing or end-to-end testing.

### Not Implemented Yet / Major Design Work

1. **Custom Command-Palette Battle System**
   - Status: Planned, not implemented.
   - Scope: base attack categories, element/modifier selection, AP/stamina/elemental energy costs, info card preview, action confirmation, energy-gather/rest actions, battle state machine and AI support.
   - Dependency: Battle UI direction and command palette mockup should be settled first.

2. **Node v2 / Scene Ring / Terrain Profile Movement**
   - Status: Planned upgrade, not immediate replacement.
   - Scope: terrain-only node data, terrain profiles, traversal capability profiles, subnodes for followers, ring 0/1/2 scene loading, personality-gated perception, smarter flee/pursuit selection and persistent fleeing entities.
   - Dependency: current node/path foundations, scene-loading review and PokeNav/map last-known-location hooks.

3. **Full Dialogue Graph Tooling**
   - Status: Foundations exist, visual authoring tool not built.
   - Scope: graph-like dialogue authoring, branching options, inner voice/skill-based insights, emotion styling, phone dialogue, quest completion calls and conditional response expansion.
   - Dependency: choose whether to keep SO-authored dialogue or build a custom editor window later.

4. **Minigame Venue Bridge**
   - Status: Planned, not implemented.
   - Scope: arcade/casino/festival venue definitions, entry costs, chip or ticket currency, prize exchange catalogs, win/loss/result records, cooldowns, venue restrictions, notification hooks and optional links to quests, shops, competitions, Life Path rewards or reputation.
   - Dependency: actual minigame types can be decided later; first pass should only build the bridge between minigames and the main economy/progression systems.

### Script-Ready But Needs UI / Scene / Content

1. **PokeNav / Pokedex / Map / Minimap**
   - Backend exists for map markers, filters, knowledge detail, feed/social posts and region/Pokedex rows.
   - Remaining: moving minimap binding, visual PokeNav pages, marker content, region knowledge and example SOs.

2. **Market / Basket / Shelf / Service Systems**
   - Backend exists for catalog offers, brands/models, basket, checkout, returns, security, restock, delivery, services and appointments.
   - Remaining: shelf colliders/interactables, basket UI, checkout UI, shop layouts and product/service content.

3. **Camp / Survival / Pokemon Care**
   - Backend exists for survival needs, Pokemon care needs, camp stations, care/vital snapshots and care action hooks.
   - Remaining: camp scene objects, care UI, balancing, Pokemon care content, treatment/rest services and animation/feedback.

4. **Pokemon Ability Tree / Pokemon Skill Tree**
   - Backend exists for editable tree definitions, node prerequisites, ability point state, unlock records, applied effects, Pokemon save integration, point/unlock source components, ProjectValidator checks and ContentAudit targeting.
   - Remaining: real tree SO content, balancing, UI backend/visual tree, deeper battle/care/overworld effect consumers and tutorial/sample content.

5. **New Game / Character Setup Flow**
   - Backend exists for editable `NewGameSetupDefinition` assets that can apply origin packages, customization presets/parts, battle mode preferences, initial battle rule unlocks and lifestyle grants.
   - `PlayerNewGameSetupLog` saves selected setup and history; `NewGameSetupSource` can apply setups from scene triggers/interactables; ProjectValidator and ContentAudit know about setup assets/sources/logs.
   - Remaining: New Game UI, appearance color workflow, actual setup SO content, start scene/spawn binding and final customization asset strategy.

6. **Life Path / Careers / Titles / Access**
   - Backend exists for paths, branches, perks, rewards, requirements, progression/access UI snapshots and validation.
   - Remaining: real path/perk SOs, unlock balance, Life Path UI and tutorial/sample content.

7. **Role Activity Boards / Jobs / Assignments**
   - Backend exists for mixed role boards, jobs/activities, Pokemon assignments and board UI snapshots.
   - Remaining: professor/police/ranger/festival board content, scene sources, UI binding and reward balance.

8. **Situation Events / Journey Incidents / World Conditions**
   - Backend exists for event definitions, pools, signal profiles, logs, sources, journey incidents and signal UI snapshots.
   - Remaining: signal/event SO content for fatigue, low care, weak party, route danger, weather, camps and area incidents.

9. **Rumor / Law / Investigation**
   - Backend exists for rumor/law/investigation direction and logs.
   - Remaining: propagation/decay content, law cases, police station boards, witness/dialogue links and UI/debug presentation.

10. **Companions / Social Activities / Follower Support**
   - Backend exists for companion roles/perks, social activities, companion Pokemon teams, ride self-travel checks and node-follow foundations.
   - Remaining: named companion content, relationship events, social activity UI, follower selection UI and companion-specific dialogue.

11. **Transit / Ride / Region Travel**
   - Backend exists for transit journeys, stop/dwell logic, onboard activity records, transit-region handoff, ride definitions/logs and companion ride capacity policies.
   - Remaining: vehicle interior scenes, station UI, route content, ride point setup, mount sprites/animation alignment and ride policy content.

12. **Competitions / Contests / Battle Frontier / World Championship**
   - Backend exists for competition registration, invitations, venues, seasons, bracket/ranking snapshots and honors.
   - Remaining: contest/tournament content, stadium/desk UI, Battle Frontier/World Championship rulesets and prize/qualification balance.

13. **Minigame Venues / Arcade / Casino / Festival Stands**
   - Planned bridge for location-bound minigames that can charge money, chips or tickets, record attempts/results, grant prizes, unlock venue rewards and connect to shops, quests, competitions, Life Path rewards, reputation or notifications.
   - Remaining: bridge scripts, venue/source definitions, chip ledger, prize exchange hooks, UI/backend snapshots, actual minigame-specific adapters and test venue content.

14. **Power Mechanics: Mega Evolution / Z-Move / Dynamax / Gigantamax**
   - Backend exists for power mechanic definitions, unlock/logging, battle rule restrictions and validator checks.
   - Remaining: real mechanic SOs, region/gym restrictions, trainer charge balancing, battle UI selection, animation feedback and actual battle-effect integration polish.

15. **Overworld Encounter Resolution / Stealth Capture**
   - Backend exists for node-path roaming, flee/recovery logs, non-battle choices, item costs and encounter UI snapshots.
   - Remaining: minigame/visual UI, encounter content, stealth tuning, node placement, capture/calm/feed outcome balance and map recovery behavior.

16. **Radial Context UI**
   - Backend exists for controller/view/provider contracts and party/inventory/world/encounter providers.
   - Remaining: final radial prefab, wedge/segment art, input polish, screen bindings and action routing into existing UI flows.

17. **Debug / Validation / Asset Audit**
   - Backend exists for runtime debug logger, health monitor, project validator, content audit and asset audit foundations.
   - Remaining: unified developer overlay, final asset scan pass, missing sprite/prefab checks after real content is added and test scene reporting.

### Finalization / Test Passes

1. **Core Test Scene**
   - Build a small placeholder scene using colored Images/SpriteRenderers instead of final assets.
   - Include stations for camp/care, shop, transit, role board, PokeNav, encounter, battle trigger, competition desk, ride/elevation, situation event and debug panels.

2. **Minimum Test SO Set**
   - Create tiny test content for the major systems only after script foundations are stable.
   - The user will own final SO authoring; Codex can later provide tutorial-style setup guides and sample assets.

3. **End-to-End Mini Loop**
   - Verify a short loop: get Pokemon, care/rest, run activity, buy item, encounter/capture, complete board task, travel, receive notification/PokeNav update and gain Life Path progress.

4. **Final UI Binding Pass**
   - Build or bind placeholder UI panels in the UI Scene, then replace with final visuals gradually.

5. **Content / Asset Audit Pass**
   - Run validators and audit tools after real SOs, prefabs, sprites and scenes exist.

## Next Systems List

### Short-Term Next

1. **Close remaining script-only core gaps**
   - Primary open systems are the custom command-palette battle system, node v2 planning and dialogue graph tooling.
   - Do not start full SO/content creation until these major script direction decisions are settled.

2. **Core test scene planning**
   - Prepare a placeholder scene layout using simple colored objects and Images, not final assets.
   - This scene should test the systems listed in Remaining System Backlog before the final UI/content pass.

### Medium-Term Next

3. **Completed script-ready integration layer**
   - Done for lightweight weather-region modifiers through Journey Environment profiles/controllers/logs.
   - Done for generic role activity boards that can mix activities, jobs, Pokemon assignments, social actions, situation events and Life Path rewards behind one UI/source contract.
   - Done for camp station definitions/sources/logs that connect rest, sleep, Pokemon care, social activities, assignments, situation events, role boards and Life Path rewards inside permitted areas.
   - Done for route/camp journey incident definitions, boards, sources and logs that can activate timed travel events, roll weighted incident boards, trigger situation events and apply Life Path/consequence rewards.
   - Done for journey incident UI backend snapshots/actions that expose active incidents, history, direct rows and board rows to future PokeNav/map/route-event panels.
   - Done for camp station UI backend snapshots/actions that expose station rows, history, view records, configured actions and first-available actions to future camp/care/rest panels.
   - Done for role activity board UI backend snapshots/actions that expose board rows, history, view records, configured entry runs and first-available actions to future professor/police/ranger/festival panels.
   - Done for notification feed UI backend snapshots/actions that expose global feed rows, unread/pinned counts, read/remove/clear actions and manual/template publish hooks for MMO-like side logs.
   - Done for battle mode UI/options snapshots/actions that expose classic/current, command-palette, hybrid or custom battle mode preferences while safely falling back until future battle backends exist.
   - Done for progression/access UI backend snapshots/actions that expose titles, careers, milestones, reputation factions and access checks to future status, license, permit, professor or police panels.
   - Done for competition registration UI backend snapshots/actions that expose registration options, invitations, venues, registration history and bracket summaries to future tournament desk, stadium or PokeNav panels.
   - Done for competition bracket/ranking UI backend snapshots that expose rankings, tier progress, point history, seasons, bracket runs and match rows to future league, stadium, Battle Frontier or PokeNav panels.
   - Done for transit vehicle journey backend definitions/sources/logs that allow active onboard travel, stop dwell windows, disembark/continue choices and onboard activity records.
   - Done for transit journey UI backend snapshots/actions that expose journey options, active onboard state, leg sequence, stop choices, history and onboard activity recording.
   - Done for transit journey incident hooks that can activate direct journey incidents or roll incident boards at journey phases and selected stops.
   - Done for overworld encounter node/path foundations that let visible roaming Pokemon move through designer-authored nodes instead of unrestricted random wandering.
   - Done for overworld movement flags, connection capability gates and lightweight graph pathfinding that can later support player/NPC node movement, ride elevation gates and stealth flee routes.
   - Done for manual route following and stealth flee controller foundations for alerted Pokemon/NPC escape behavior.
   - Done for virtual flee logging so Pokemon/NPCs escaping to spawn-exit or map-exit nodes can remain recoverable until an expiry timer.
   - Done for virtual flee recovery sources that can mark records recovered, spawn a prefab or re-enable an existing object when the player follows before expiry.
   - Done for node movement adapter that turns input/AI directions into valid connected-node movement through the existing path agent.
   - Done for optional node movement input bridge that forwards Input System movement into the adapter.
   - Done for encounter debug source snapshots and test path/move actions.
   - Done for companion node-follow profiles, player node trail tracking and companion node-follow controllers that let companions follow valid node trails instead of the player transform.
   - Done for Pokemon core growth profiles, saved growth state, passive traits, growth initializers and care-action growth training rewards.
   - Done for Pokemon evolution definitions, saved evolution runtime state, evolution source triggers and old flow integration for level/item evolution checks.
   - Done for Pokemon technique memory state, technique learning definitions/sources, active move synchronization and TM/level-up replacement integration.
   - Done for Pokemon held item/equipment service, inventory held item category, source component, saved history log and validator/runtime monitor integration.
   - Done for non-battle encounter resolution definitions/sources, item-costed calm/feed/observe/treat/capture attempts, outcome hooks and encounter resolution history.
   - Done for encounter resolution choice sets, choice sources and UI-ready backend snapshots/actions for multi-option wild Pokemon approaches.
   - Done for overworld encounter debug UI backend snapshots/actions that expose node, connection, path-agent and virtual flee information to future debug panels.
   - Done for transit-region handoff definitions/sources that bridge vehicle journey stop states into region travel routes, selectable policies and selected Pokemon behavior.
   - Done for PokeNav knowledge detail UI backend snapshots/actions that expose selected Pokedex, region and generic knowledge-entry detail panels.
   - Done for PokeNav map filter UI backend snapshots/actions that expose profile, category, tag, region, scene, search and marker filter rows.
   - Done for focused progression panel UI backend snapshots/actions that split progression/access data into overview, titles, careers, milestones, reputation and access panels.
   - Done for radial context menu core UI backend classes, provider contract, theme/layout profiles, controller, view, segment, tag frame and simple input router.
   - Done for radial party menu provider backend that exposes party-slot actions without replacing the current party menu flow.
   - Done for situation event signal UI backend snapshots/actions that expose profile rules, preview blockers, cooldowns, history and manual evaluation to future debug/PokeNav/event tuning panels.
   - Remaining code-side candidates are tracked in Remaining System Backlog.

4. **Node v2 / scene-ring / personality encounter planning**
   - Priority: Medium-later, not an immediate implementation task.
   - Add after the current backend/UI/data stabilization work, and before replacing core player movement.
   - Scope: terrain-only node data, TerrainProfile-driven movement behavior, subnodes for companion/follower placement, scene-ring streaming, scene navigation data, personality-gated encounter triggers, conditional perception/threat fields, smarter flee/pursuit target selection and fleeing entity persistence.
   - Dependencies: current overworld node/path foundations, companion node-follow, encounter resolution, personality/mood/vital systems, scene loading/portal review, map/PokeNav last-known-location hooks and validator/content audit updates.

5. **Pokemon assignment example SO/tutorial content**
   - Add example SO/tutorial content near the content pass, not now.

6. **Battle mode UI/options pass**
   - Done at backend contract level with `BattleModeOptionsUIManager`.

### Later

7. **UI pass**
    - Speech bubbles, notification feed, shop/basket/shelf UI, PokeNav, map, quest and progression screens.
    - Build reusable Radial Context Menu / Segmented Ring UI prefabs during this pass, after the first priority UI panels have a basic visual direction.
    - Radial MVP target: Party slot action menu first, then inventory item actions, tool/world interaction choices and encounter resolution choices.
    - Keep radial UI split into reusable option data/provider/controller/view/tag-frame pieces so it does not become a Party UI-only implementation.

8. **Example SO / tutorial content pack**
    - Small sample ScriptableObject sets for each major system so the user can inspect and clone setup patterns.
    - Include one sample social activity setup path: base activity, social activity definition, source component, companion/Pokemon participant rules and expected reward changes.

9. **Asset/content audit pass**
    - Scan missing sprites, prefabs, icons, SO content gaps and scene wiring issues.

## Definition of Done

Each system should be tracked separately:

1. **Script Ready:** code, logs, requirements and validator exist.
2. **Data Ready:** Minimum Viable Content (MVC) ScriptableObject assets are created and validated.
3. **Scene Ready:** relevant GameObjects/components are placed and wired.
4. **UI Backend Ready:** UIManager scripts exist and expose snapshots.
5. **UI Prefab Ready:** Visual canvas prefabs and panels are built and wired so the player can use the system in-game.
6. **Save Ready:** System state is properly serialized and restorable.
7. **Tested:** manual Unity test or compile/runtime validation completed.

Most current new systems are at step 1.

## Update Rule

Every time a new system is added:

1. Add it to this document.
2. Mark its status.
3. Add remaining setup tasks.
4. Add any ScriptableObject creation notes.
5. Update the planned/next systems list.

This keeps the project from becoming a pile of cool systems with no map.



## Roadmap Addendum: Recommended Next Direction for Codex

### Immediate Direction

*Update based on the Project System Audit:* The codebase has achieved significant backend maturity (approx. 600 scripts). The "role-progression and activity-integration layer" is almost entirely complete at the script level. The immediate focus must now pivot heavily away from new scripts and towards Data, Scene, and UI visualization.

Recommended priority:

1. **Data & Content Bootstrapping:** Create Minimum Viable Content (MVC) ScriptableObject asset templates for LifePaths, Quests, Role Boards, and missing Pokemon/Moves. The 100+ activity scripts cannot be tested without these SOs.
2. **UI Visualization:** Build frontend UI Canvas prefabs for `RoleActivityBoardUIManager`, `LifePathUIManager`, speech bubbles, and notification feeds that currently only exist in code.
   - During this UI phase, introduce the reusable Radial Context Menu / Segmented Ring UI as a shared option-selection pattern rather than a one-off Party UI feature.
   - Priority order: establish basic UI panels first, then prototype radial MVP on Party slot actions, then expand to inventory/tools/world interactions where it genuinely improves flow.
3. **Situation Event Integration:** Create the `SituationEventDefinition` SOs for world conditions (fatigue, low care, weak party) matching the existing signal hooks.
4. **Battle UI / Mode Selection:** Create the UI/options flow for choosing preferred battle modes at new game/options/challenge time.
5. **Save System Unification:** Ensure core battle state, Pokemon party state, and map locations are integrated into a cohesive save system alongside the existing player logs.

### Suggested System Packages

#### Package A: Identity / Role

- Life Path / Vocation System
- Open career reputation hooks
- Titles, permits, licenses and access profile integration
- Inner voice/dialogue insight hooks
- Companion role support hooks

#### Package B: Journey / World

- Region event pools
- Situation events
- Time phase model
- Regional ecology modifiers
- Journey environment survival/care effects
- Camp and travel integration
- PokeNav discovery support

#### Package C: Pokemon Life

- Personality and mood effects
- Core HP/stamina injury consequences
- Camp care interactions
- Pokemon assignment roles
- Pokemon skill trees and role growth

#### Package D: Career Activities

- Champion battles and tournaments
- Ranger rescue and non-battle resolution
- Farmer/ranch production loops
- Performer contest/stage activities
- Researcher observation/Pokedex progression
- Merchant/crafting/delivery work
- Investigator/law/rumor cases

#### Package E: Reactive World

- Rumor propagation from player actions
- Law/risk consequences
- NPC memory/reaction hooks
- Multi-solution quest outcomes
- Organization reputation and invitation hooks

### Implementation Constraints

- No fixed hardcoded player class.
- No required single main story route.
- Every major system should be optional and modular.
- Do not require all life paths to have content before the game can run.
- Do not make battle systems depend on farm systems, or farm systems depend on law systems, etc.
- Use IDs, ScriptableObject definitions, requirement checks and reusable outcome/effect systems.
- Add validator/audit rules for missing IDs, duplicate IDs, invalid path references, invalid branch references and perks with impossible requirements.
- Prefer small UI-facing backend managers and snapshots over final UI design at this phase.

## Proposed Backend Architecture & System Upgrades (Suggestions)

*If these systems have not been built or planned, add them to a logical place on the list; if they have been built but do not have the same features, add them to the list as an update; if the content is the same, add them.*

### Zero-Collider Grid Movement & Terrain Momentum

Status: **Planned (Suggestion — pending re-evaluation)**

> **Re-evaluation note:** The existing `OverworldNodeMovementAdapter`, `OverworldEncounterPathAgent` and portal/encounter systems will be reviewed first. Rigidbody2D may be retained for specific systems where physics triggers remain the simpler solution. Only systems where physics-free movement is genuinely cleaner or more advantageous will be migrated. This section describes the target direction, not a forced immediate replacement.

- Physical physics dependency removal: Player and NPC movement to be executed entirely via C# node matrix and coordinate math (`Vector2.MoveTowards`), eliminating `Rigidbody2D` and `Collider2D` collision bugs.
- `TileData` ScriptableObject structure for terrain rules (`walkable`, `speedMultiplier`, `isIce`, `isSlope`, `blocksView`).
- Input Buffer system: caches the next directional input when a character is at 80% of the current path, ensuring zero-frame delay and smooth tile transitions.
- Ice Sliding (`extraSlideNodes`) momentum: characters automatically continue sliding in their last direction based on entry speed when input is released on ice nodes.
- Slope Gravity Simulation: characters on slope nodes are automatically shifted to the downward neighbor node via a timer if no upward input is applied.

Design notes:
- Guarantees deterministic movement, preventing wall-clipping and tunneling bugs common in 2D physics engines.
- Integrates with the existing `OverworldNodeMovementAdapter` as an extension, not a full replacement at this stage.
- Per-system approach: if removing Rigidbody2D from a given system (portal, encounter trigger, etc.) is simpler and more advantageous than keeping it, that system is updated; otherwise Rigidbody2D is kept.

Remaining:
- Evaluate each physics-dependent system (portal, encounter trigger, zone detector) individually.
- Base `GridMovement` script implementation for systems where physics-free is cleaner.
- Terrain momentum math (ice/slope).
- Core test scene validation.

### 32x32 Chunk Streaming & Vertical Layering (Sky/Surface/Underground)

Status: **Planned (Suggestion — pending re-evaluation)**

> **Re-evaluation note:** This system covers the encounter/event/NPC data layer. For visual Tilemaps, Unity's built-in chunk rendering is already used and does not need replacement. The 9-grid streaming concept applies to data assets (encounter zones, NPC spawn records, elemental state) rather than Tilemap visuals. Unity Addressables is preferred over hand-rolled async file loading.

- `MapNodeBaker` editor tool: automatically scans Tilemap layers in the Unity Editor and bakes 32x32 unit areas into independent C# `ChunkData_X_Y_Layer.asset` files.
- 9-Grid Asynchronous Stream Manager: dynamically loads the center chunk and its 8 neighbors into RAM as the player moves, unloading distant chunks. Async loading via Unity Addressables (`AsyncOperationHandle`) rather than hand-rolled file I/O.
- Vertical Layer Management: divides the world into `Sky`, `Surface`, and `Underground` planes sharing the same (X, Y) coordinate matrix.
- Anchor Nodes (`Sky_Anchor`, `Dive_Anchor`, `Dig_Anchor`) act as the only valid transition gates between layers.
- Dynamic minimap reads and renders only the node matrix of the player's active layer.

Design notes:
- Reduces CPU/RAM load for encounter/event data in large open-world areas.
- Does not replace Unity's Tilemap rendering system; applies only to the game data (encounter, event, zone, elemental state) layer.
- Async loading should use Unity Addressables for better integration with the Unity asset pipeline and error handling.

Remaining:
- Decide scope: data-only chunks vs. full scene streaming.
- Editor script for baking `ChunkData` assets.
- Addressables-based async load/unload controller.
- Anchor node component setup and layer transition logic.

### Distance-Based Stealth, Encounter Radius & Utility AI

Status: **Planned**

- Node-Based Vision Cone: NPCs scan N-nodes ahead based on facing direction, terminating the scan early if a `gorusEngellerMi == true` node is found, replacing heavy trigger colliders.
- `CurrentEncounterRadius` (Aura): a dynamic mathematical radius ($R$) tracking the player's sound/scent emission. Distance checks (`Vector2.Distance`) determine trigger states instead of physics overlaps.
- Item/Environment Manipulation: repels reduce the aura radius; thrown bait creates temporary attraction coordinates that override NPC patrol logic.
- Utility AI (Need-Based) Architecture: replaces static roaming. NPCs calculate action scores based on internal stats (Hunger, Curiosity, Fear, Territory) and feed the highest-scoring target node to the A* pathfinding agent.
- `NatureModifier` (Personality) Integration: at 100% alertness, Timid NPCs use A* to flee to the nearest safe node outside the encounter radius, while Aggressive NPCs rush the player's node to trigger the Turn-Based state.

Design notes:
- Serves as the core AI upgrade for the existing `OverworldEncounterPathAgent` and `EncounterResolutionSource`.
- Directly supports the "Reactive World" and "Non-Battle Resolution" pillars by treating wild Pokemon as living entities with needs rather than simple trigger zones.

Remaining:
- Vision cone algorithms and math calculations.
- Utility AI behavior tree and scoring logic.
- Stealth item SO content and aura manipulation scripts.

### Mathematical Trajectories, URP Day/Night & GPU-Friendly UI

Status: **Planned**

- Bezier Curve Pokeball Throwing: calculates projectile arcs mathematically using Start, Peak, and Target Node coordinates without gravity/physics simulation.
- UI Data Managers for Vitals: bridges `fillAmount` UI properties (Vertical, Horizontal, Radial) to core vitality states (hunger, thirst, stamina) and triggers programmatic UI Shake on critical thresholds.
- URP 2D Global Light Day/Night Cycle: utilizes `DayNightController` with `Gradient` and `AnimationCurve` to modify Global Light 2D intensity and color based on in-game time (`Time.deltaTime`). Automatically enables `PointLight2D` objects (torches/lamps) at evening thresholds.
- Emissive Masking (`_EmissionMap`): uses URP Secondary Textures to exclude specific pixels (e.g., Charmander's tail fire) from Global Light darkening, paired with a script-driven flicker effect.
- `RadarChart` Mesh Generation: a custom `MaskableGraphic` script that dynamically draws a 6-vertex polygon for Pokemon stats using C# math, rendered via a low-res, Point-filtered Render Texture for pixel-art consistency.

Design notes:
- Offloads heavy visual processing and lighting to the GPU via URP, ensuring the CPU is entirely free for Life Path, Utility AI, and chunk-streaming calculations.
- Discards screen-overlay color tinting in favor of true 2D lighting, enhancing the "Living World" aesthetic.

Remaining:
- Bezier curve script implementation.
- URP 2D asset setup and Day/Night controller.
- `RadarChart` UI script and Render Texture pipeline setup.

### Custom Command-Palette & Active-Timing Battle System

Status: **Planned** (Replaces the deferred status)

- **Nested Command UI:** Replaces the classic 4-move limit with a tree-based UI. Players select a base category (e.g., Bite, Punch) and then apply elemental modifiers or stamina-heavy upgrades (e.g., Fire-Fang).
- **AP Economy & CTB Timeline:** Uses Action Points (AP) and a Conditional Turn-Based timeline. Heavy attacks push the entity further back on the timeline.
- **Active-Timing Defense (QTE):** A programmatic, UI-based shrinking ring for defense/evasion. The timing window is calculated mathematically via `(DefenderSpeed / AttackerSpeed)`. Outputs Perfect, Good, or Fail states.
- **Stacking Penalty (Posture/Stagger):** Missing consecutive defensive QTEs fills a hidden "Stress/Posture" meter. When full, the Pokemon is staggered, losing its turn and falling back on the timeline.

Design notes:

- Breaks the monotony of traditional turn-based combat by forcing active risk/reward management and reflexes.
- UI elements (Shrinking Ring) rely on `Image.fillAmount` and timers, completely bypassing the need for complex 2D hitbox synchronization.

Remaining:

- Command-Palette tree UI layout.
- Timeline manager and AP economy backend.
- QTE math formula and visual ring implementation.

### Timeout (Mola) & Endurance Mechanics

Status: **Planned**

- **Timeout Periods:** Configured via `Battle Rule Set Definitions`. Timeouts trigger based on rules (e.g., every 15 turns, or after 2 Pokemon faint).
- **Restricted Item Usage:** Items can _only_ be used during Timeout phases. Players receive limited "Timeout Points" to spend on healing.
- **Zero-Revive Policy:** Revive items are permanently removed. Fainted Pokemon are locked out of the current battle, enforcing true team rotation and eliminating the "meat shield" meta.
- **AI Parity:** Enemy trainers use the exact same Timeout rules and limited inventory, creating fair, chess-like tactical phases.
- **Bench Regeneration & Stamina Collapse:** Benched Pokemon passively recover a small percentage of HP/Stamina per turn. If a Pokemon enters battle with 0 Core Stamina, they must rely solely on QTE defense until stamina naturally regenerates.

Design notes:

- Transforms battles into endurance marathons.
- Core HP acts as a long-term battery; depleting it completely locks the Pokemon out until Camp/Caretaker recovery, perfectly tying battles to the Life Path simulation.

Remaining:

- Battle Rule Set expansions for Timeout triggers.
- Timeout UI overlay (corner mechanic).
- Benched regeneration event hooks (`OnTurnStart`).

### Relationship Inertia & Loyalty Consequences (CK3 Style)

Status: **Planned**

- **6-Tier Relationship Scale:** Progresses from Hatred (1) to Soulbound (6). Wild catches start at Neutral (3). Drives AP regeneration, QTE window size, and passive combat buffs.
- **The Ultimatum Event:** A Pokemon cannot drop silently into the Hatred tier. When on the verge, a `Situation Event` forces a confrontation (e.g., refusing to walk). The player's dialogue/action choice determines if the bond breaks completely or holds.
- **Toxic Party Morale:** Cruelty is witnessed. Forcing an exhausted Pokemon to fight causes "Distrust" debuffs across the entire active party.
- **Hatred Consequences & Redemption:** Hated Pokemon actively sabotage battles (ignoring commands, failing QTEs) or abandon the player. Redemption requires extreme sacrifice (e.g., Player sacrificing AP to block a fatal blow).
- **Data-Driven Boundaries:** All relationship thresholds and XP requirements must be stored in `RelationshipTierSettings` ScriptableObjects, completely decoupled from hardcoded logic.
### Custom Command-Palette & Active-Timing Battle System

Status: **Planned** (Replaces the deferred status)

- **Nested Command UI:** Replaces the classic 4-move limit with a tree-based UI. Players select a base category (e.g., Bite, Punch) and then apply elemental modifiers or stamina-heavy upgrades (e.g., Fire-Fang).
- **AP Economy & CTB Timeline:** Uses Action Points (AP) and a Conditional Turn-Based timeline. Heavy attacks push the entity further back on the timeline, while defensive actions can advance it.
- **Active-Timing Defense (QTE):** A programmatic, UI-based shrinking ring for defense/evasion. The timing window is calculated mathematically via `(DefenderSpeed / AttackerSpeed)`. Outputs Perfect, Good, or Fail states without relying on hitboxes.
- **Stacking Penalty (Posture/Stagger):** Missing consecutive defensive QTEs fills a hidden "Stress/Posture" meter. When full, the Pokemon is staggered, losing its turn and falling back on the timeline.

Design notes:
- Breaks the monotony of traditional turn-based combat by forcing active risk/reward management and reflexes.
- Connects directly to the Skill Tree, where players can unlock starting AP bonuses or maximum stamina buffs.

Remaining:
- Command-Palette tree UI layout.
- Timeline manager and AP economy backend.
- QTE math formula and visual ring implementation.

### Timeout (Mola) & Endurance Mechanics

Status: **Planned**

- **Timeout Periods:** Configured via `Battle Rule Set Definitions`. Timeouts trigger based on rules (e.g., every 15 turns, or after 2 Pokemon faint). 
- **Restricted Item Usage:** Items can *only* be used during Timeout phases. Players receive limited "Timeout Points" to spend on healing, eliminating potion-spam.
- **Zero-Revive Policy:** Revive items are permanently removed. Fainted Pokemon are locked out of the current battle, enforcing true team rotation.
- **AI Parity:** Enemy trainers use the exact same Timeout rules and limited inventory, creating fair, chess-like tactical phases.
- **Bench Regeneration & Stamina Collapse:** Benched Pokemon passively recover a small percentage of HP/Stamina per turn (`OnTurnStart` hooks). If a Pokemon enters battle with 0 Core Stamina, they cannot attack and must rely solely on QTE defense until stamina naturally regenerates.

Design notes:
- Transforms battles into endurance marathons. 
- Core HP acts as a long-term battery; depleting it completely locks the Pokemon out until Camp/Caretaker recovery, perfectly tying battles to the Life Path simulation.

Remaining:
- Battle Rule Set expansions for Timeout triggers.
- Timeout UI overlay (corner mechanic).
- Benched regeneration event hooks (`OnTurnStart`).

### Relationship Inertia & Loyalty Consequences (CK3 Style)

Status: **Planned**

- **6-Tier Relationship Scale:** Progresses from Hatred (1) to Soulbound (6). Wild catches start at Neutral (3). Modifies AP regeneration, QTE window size, and grants passive combat buffs/debuffs.
- **The Ultimatum Event:** A Pokemon cannot drop silently into the Hatred tier. When on the verge, a `Situation Event` forces a confrontation in the overworld. The player's choice determines if the bond breaks completely or holds.
- **Toxic Party Morale:** Cruelty is witnessed. Forcing an exhausted Pokemon to fight causes "Distrust" or "Fear" debuffs across the entire active party, multiplied by the Pokemon's `NatureModifier`.
- **Hatred Consequences & Redemption:** Hated Pokemon actively sabotage battles (ignoring commands, failing QTEs) or abandon the player. Redemption requires extreme sacrifice (e.g., Player sacrificing AP to block a fatal blow).
- **Data-Driven Boundaries:** All relationship thresholds and XP requirements must be stored in `RelationshipTierSettings` ScriptableObjects, completely decoupled from hardcoded logic via an `AddFriendship(int amount, sourceTag)` event bus.
- **Consequence Realization:** Trading or abandoning a hated Pokemon tags them as a "Grudge" in the `PlayerEncounterLog`. They can return later as an over-buffed Ace Pokemon for a Rival NPC. Sending them to the Professor results in a severe Reputation penalty and rehabilitation fees.

Design notes:
- Provides massive narrative weight to the Caretaker path. Pokemon are treated as living companions with moral boundaries, preventing mechanic exploitation.

Remaining:
- `RelationshipTierSettings` SO structure.
- `AddFriendship` decoupled event bus implementation.
- Ultimatum Situation Event triggers and Rival Grudge injection logic.

### Cellular Automata Ecological Interaction

Status: **Planned**

- **Matrix-Based Elemental Spread:** Utilizes the existing 32x32 node matrix to simulate a living ecosystem. Each node reads the state of its 8 neighbors.
- **Chain Reactions:** A fire attack sets a node to "Burning." Based on wind direction and time ticks, the fire spreads to adjacent grass nodes. 
- **Elemental Synergies:** Water attacks extinguish fire nodes, turning them into "Mud" (slowing movement). Electric attacks hitting water nodes turn the entire connected water body into a "Shocked" state.

Design notes:
- Elevates the Ranger and Explorer life paths. Offers massive non-battle utility and environmental puzzle-solving without using a physics engine.
- Extremely performant since it relies purely on C# array/matrix iterations (Conway's Game of Life logic) inside the active 9-chunk stream.

Remaining:
- Cellular Automata tick manager.
- Elemental state definitions for `TileData`.
- Visual shader feedback for node states (burning, wet, shocked).

### Mendelian Genetics & Detailed Breeding

Status: **Planned**

- **Hidden Gene Bitmask:** Every Pokemon possesses a `ulong genes` field (64 bits = 32 gene positions at 2 bits each: `11` Dominant, `10` Heterozygous, `00` Recessive) stored directly on `Pokemon`. No heap allocation, fully serializable to the save system.
- **Genetic Inheritance:** Breeding uses bitwise operations to combine parent gene pools and calculate offspring traits. Zero allocation at breeding time — pure integer math.
- **Mutations & Rare Traits:** Controlled breeding over generations can unlock dormant recessive genes, producing rare `Passive Traits` (e.g., "Winter Coat" for cold resistance, or "Nocturnal" for night-time combat buffs). Mutation chance is a configurable float in `GeneticTraitTableDefinition` SO.
- **GeneticTraitTableDefinition SO:** Maps gene bit positions to visible traits, stat modifiers and passive trait unlocks. Decoupled from Pokemon species data.

Design notes:
- Fills the massive gap in the Caretaker/Breeder Life Path, giving players hundreds of hours of end-game optimization.
- `GeneticProfile` should be a struct (value type), not a class, to avoid heap allocation per Pokemon.
- Implementation decision: `string` DNA representation was considered and rejected. `string` causes one heap allocation per Pokemon, another on every string operation, and cannot be compared with bitwise ops. A `ulong` bitmask handles 32 gene pairs, is stack-allocated, serializes as a single integer and supports bitwise crossing in O(1).

Remaining:
- `ulong genes` field added to `Pokemon.cs` and serialized through `SavingSystem`.
- `GeneticProfile` struct with bit-position helper methods.
- `GeneticTraitTableDefinition` SO for trait mappings and mutation tables.
- `BreedingResolver` static class for offspring gene calculation.
- `GeneticInitializer` component to assign random genes on wild Pokemon spawn.

### Custom Command-Palette & Active-Timing Battle System

Status: **Planned** (replaces the previously deferred status)

- **Two-Phase Transition:** The battle system will be split into two coexisting implementations. The existing classic coroutine-based system (`RunTurnState`) remains as Phase 1. The new Command-Palette system is built as a separate `CommandPaletteRunState` routed through `BattleModeDefinition`. The old system is removed only after the new one is fully playable and tested.
- **Nested Command UI:** Replaces the classic 4-move limit with a tree-based UI. Players select a base category (e.g., Bite, Punch) and then apply elemental modifiers or stamina-heavy upgrades (e.g., Fire-Fang).
- **AP Economy & CTB Timeline:** Uses Action Points (AP) and a Conditional Turn-Based timeline. Heavy attacks push the entity further back on the timeline, while defensive actions can advance it.
- **Active-Timing Defense (QTE):** A programmatic, UI-based shrinking ring for defense/evasion. The timing window is calculated mathematically via `(DefenderSpeed / AttackerSpeed)`. Outputs Perfect, Good or Fail states. `Image.fillAmount` driven by `Time.deltaTime` — no hitbox synchronization needed.
- **Stacking Penalty (Posture/Stagger):** Missing consecutive defensive QTEs fills a hidden "Stress/Posture" meter. When full, the Pokemon is staggered, losing its turn and falling back on the timeline.

Design notes:

- The two-phase split means the existing `BattleModeOptionsUIManager` routing contract is already the correct integration point. No modification to `RunTurnState` is needed for the new system.
- Both systems share the same `BattleUnit`, `Pokemon`, `PokemonParty` and `BattleRuleSetDefinition` data — only the turn resolution state machine differs.
- Connects directly to the Skill Tree for AP bonuses and maximum stamina unlocks.

Remaining:

- `CommandPaletteRunState` state machine implementation (separate from `RunTurnState`).
- Command-Palette tree UI layout.
- Timeline manager and AP economy backend.
- QTE math formula and visual shrinking ring implementation.
- Migration plan: feature-flag classic system off once Command-Palette is stable.

### Timeout (Mola) & Endurance Mechanics

Status: **Planned**

- **Timeout Periods:** Configured via `Battle Rule Set Definitions`. Timeouts trigger based on rules (e.g., every 15 turns, or after 2 Pokemon faint).
- **Restricted Item Usage:** Items can _only_ be used during Timeout phases. Players receive limited "Timeout Points" to spend on healing, eliminating potion-spam.
- **Zero-Revive Policy:** Revive items are permanently removed. Fainted Pokemon are locked out of the current battle, enforcing true team rotation.
- **AI Parity:** Enemy trainers use the exact same Timeout rules and limited inventory, creating fair, chess-like tactical phases.
- **Bench Regeneration & Stamina Collapse:** Benched Pokemon passively recover a small percentage of HP/Stamina per turn (`OnTurnStart` hooks). If a Pokemon enters battle with 0 Core Stamina, they cannot attack and must rely solely on QTE defense until stamina naturally regenerates.

Design notes:

- Transforms battles into endurance marathons.
- Core HP acts as a long-term battery; depleting it completely locks the Pokemon out until Camp/Caretaker recovery, perfectly tying battles to the Life Path simulation.

Remaining:

- Battle Rule Set expansions for Timeout triggers.
- Timeout UI overlay (corner mechanic).
- Benched regeneration event hooks (`OnTurnStart`).

### Relationship Inertia & Loyalty Consequences (CK3 Style)

Status: **Planned**

- **6-Tier Relationship Scale:** Progresses from Hatred (1) to Soulbound (6). Wild catches start at Neutral (3). Modifies AP regeneration, QTE window size, and grants passive combat buffs/debuffs.
- **The Ultimatum Event:** A Pokemon cannot drop silently into the Hatred tier. When on the verge, a `Situation Event` forces a confrontation in the overworld. The player's choice determines if the bond breaks completely or holds.
- **Toxic Party Morale:** Cruelty is witnessed. Forcing an exhausted Pokemon to fight causes "Distrust" or "Fear" debuffs across the entire active party, multiplied by the Pokemon's `NatureModifier`.
- **Hatred Consequences & Redemption:** Hated Pokemon actively sabotage battles (ignoring commands, failing QTEs) or abandon the player. Redemption requires extreme sacrifice (e.g., Player sacrificing AP to block a fatal blow).
- **Data-Driven Boundaries:** All relationship thresholds and XP requirements must be stored in `RelationshipTierSettings` ScriptableObjects, completely decoupled from hardcoded logic via an `AddFriendship(int amount, sourceTag)` event bus.
- **Consequence Realization:** Trading or abandoning a hated Pokemon tags them as a "Grudge" in the `PlayerEncounterLog`. They can return later as an over-buffed Ace Pokemon for a Rival NPC. Sending them to the Professor results in a severe Reputation penalty and rehabilitation fees.

Design notes:

- Provides massive narrative weight to the Caretaker path. Pokemon are treated as living companions with moral boundaries, preventing mechanic exploitation.

Remaining:

- `RelationshipTierSettings` SO structure.
- `AddFriendship` decoupled event bus implementation.
- Ultimatum Situation Event triggers and Rival Grudge injection logic.

### Cellular Automata Ecological Interaction

Status: **Planned**

- **Matrix-Based Elemental Spread:** Utilizes the existing 32x32 node matrix to simulate a living ecosystem. Each node reads the state of its 8 neighbors.
- **Chain Reactions:** A fire attack sets a node to "Burning." Based on wind direction and time ticks, the fire spreads to adjacent grass nodes.
- **Elemental Synergies:** Water attacks extinguish fire nodes, turning them into "Mud" (slowing movement). Electric attacks hitting water nodes turn the entire connected water body into a "Shocked" state.

Design notes:

- Elevates the Ranger and Explorer life paths. Offers massive non-battle utility and environmental puzzle-solving without using a physics engine.
- Extremely performant since it relies purely on C# array/matrix iterations (Conway's Game of Life logic) inside the active 9-chunk stream.

Remaining:

- Cellular Automata tick manager.
- Elemental state definitions for `TileData`.
- Visual shader feedback for node states (burning, wet, shocked).



### Codex Prompt Seed

When asking Codex to implement the next update, use this direction:

> Add a modular Life Path / Vocation progression layer for the existing PokemonProject. Use the current Activity/Requirement/Progression systems where possible. Activities, social activities, care actions, research events, battles, jobs and quests should be able to optionally grant Life Path XP, branch progress and activity tags. Life Path XP grants perk points. Branch progress and tag counters control perk eligibility through the requirement framework. Keep the system optional, data-driven and safe when no rewards are configured. Add debug/UI-facing snapshot access and validator/audit support. Do not build final UI yet.
