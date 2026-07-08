# Full Project Systems Audit

## Core Pokemon Foundation
- **Scripts:** `Pokemon`, `Item`, `PokemonMood`, `PokemonVitals`, `PowerMechanics` (approx 26 scripts total)
- **Status:** **Script Ready**. Data structures (SOs) and MonoBehaviours exist.
- **Details:** Core Pokemon logic (Natures, Abilities, Status Conditions) exists in script form.

## Battle System
- **Scripts:** `Battle`, `BattleRules`, `Battle/State`, `UI` (for battle) (approx 53 scripts)
- **Status:** **Script Ready**. AI Profiles, rule definitions, and state machine exist.
- **Details:** A significant UI backend exists (31 UI scripts). The rule sets and AI tiers are implemented in code.

## Activities & Role Systems
- **Scripts:** `Activities` (100 scripts), `LifePaths` (4), `Careers` (4), `Jobs` (4), `Milestones` (2)
- **Status:** **Script Ready**. Heavily modularized.
- **Details:** This is the most extensive part of the codebase. It covers generic role activity boards, job definitions, and life path progression. Saves (`PlayerLifePathLog`) exist.

## Social & NPCs
- **Scripts:** `Dialogues` (17), `NPCGeneration` (5), `NPCMemory` (3), `NPCReactions` (3), `NPCSchedule` (2), `Social` (5), `Witnesses` (5)
- **Status:** **Script Ready**.
- **Details:** NPC variant pools, speech bubble dialogues, and witness report systems exist.

## World & Environment
- **Scripts:** `WorldAreas` (7), `WorldConditions` (6), `WorldDiscovery` (3), `WorldEvents` (2), `WorldTriggers` (4), `Map` (8), `Transit` (4)
- **Status:** **Script Ready**.
- **Details:** Region event pools, time phase models, and journey environments are implemented as SOs and Managers.

## Economy & Shops
- **Scripts:** `Shops` (21), `Services` (7), `MarketServiceUIManager.cs`
- **Status:** **Script Ready**.
- **Details:** Shop items, baskets, and market UI managers exist.

## Validation & Audit
- **Scripts:** `ContentAudit` (2), `Debugging` (8), `Editor/SOTreeTool`
- **Status:** **Script Ready**.
- **Details:** Project validators and content audit profiles exist to maintain SO integrity.
