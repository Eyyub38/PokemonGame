# Pokemon World Life RPG — Project Overview

> A personal dream-project and systems experiment inspired by classic creature-collecting RPGs. The goal is not to build a small linear monster-battling game, but to explore how far a Pokemon-like world can grow when battle, travel, care, farming, research, social life and role-playing systems all exist on the same platform.

## Project Vision

This project imagines a Pokemon-like RPG where the player is not forced into one fixed identity. Becoming a Champion is possible, but it is only one path among many.

The player can become a competitive trainer, a Ranger who protects wild Pokemon, a caretaker who heals and bonds with Pokemon, a farmer who builds a Pokemon-assisted ranch, a performer who prepares stage shows, a researcher who studies habitats and rare behavior, a merchant who trades regional goods, an explorer who maps routes and camps across the world, or an investigator who handles rumors, poachers and law-related cases.

The main idea is simple:

**The player lives in a Pokemon-like world, and their role emerges from what they repeatedly do.**

## Core Design Pillars

### 1. Living with Pokemon

Pokemon should not exist only as battle units. They should have long-term condition, stamina, personality, mood, camp behavior, care needs and non-battle usefulness.

A Pokemon may help with cooking, scouting, farming, rescue work, research, guarding a camp, performing on stage or bonding with companions. Different species and personalities should feel useful in different contexts.

### 2. Traveling Through Regions

The game is built around the feeling of a long journey. Routes, weather, region conditions, camps, ride Pokemon, events, encounters, services and PokeNav information should all make travel feel meaningful.

Each region can have its own ecology, dangers, travel rules, camp scenes and event pools.

### 3. Building a Role Identity

The player does not need to pick a permanent class at the start. Instead, their repeated actions shape their identity.

Examples:

- Winning battles and tournaments builds a Trainer or Champion identity.
- Treating injured Pokemon builds a Caretaker or Ranger identity.
- Observing rare behavior builds a Researcher identity.
- Farming with Pokemon builds a Farmer or Rancher identity.
- Completing deliveries and crafting goods builds a Merchant or Crafter identity.
- Investigating rumors or poaching incidents builds an Investigator identity.

### 4. Reactive World

The world should remember and respond to the player. Reputation, rumors, licenses, permits, organizations, NPC memory, law systems and PokeNav entries can all reflect the player's choices and repeated behavior.

A player who constantly rescues wild Pokemon may become known by Rangers. A player who wins competitions may attract sponsors. A player who investigates illegal activity may gain access to restricted cases or police-related quests.

## Main Gameplay Loop

The intended loop is flexible rather than linear:

1. The player travels through a route, region, settlement, camp or activity zone.
2. They encounter Pokemon, NPCs, rumors, resources, events or obstacles.
3. They resolve situations through battle, care, research, stealth, calming, assignments, crafting, licenses, companions or dialogue.
4. Pokemon gain fatigue, injury, mood changes, bond changes, skill progress and activity experience.
5. The player rests, camps, visits services, accepts tasks, manages Pokemon or continues traveling.
6. Repeated activities build life paths, perks, reputation and access to new roles.

The game should support both focused and hybrid play. A player may mainly be a Ranger, while also maintaining a small farm and entering contests occasionally.

## Life Path System

The Life Path system is the planned progression umbrella for the game.

Instead of fixed classes, the player develops through activity. Activities grant life-path experience, branch progress and activity tags. When a life-path bar fills, the player gains a perk point for that path. Branch progress and activity history determine which perks are available.

### Example

Cleaning a Pokemon may grant:

- Caretaker XP
- Grooming branch progress
- Bonding tag progress

Treating an injured wild Pokemon may grant:

- Caretaker XP
- Ranger XP
- Medical branch progress
- Rescue branch progress

Winning a gym battle may grant:

- Trainer XP
- Competitive branch progress

Observing a rare Pokemon behavior may grant:

- Researcher XP
- Field Biology branch progress
- PokeNav knowledge progress

This means two players can both be Caretakers but specialize differently. One may become a grooming and contest-preparation expert, while another becomes a field medic for injured Pokemon.

## Planned Life Paths

### Trainer / Champion

Focused on battles, tournaments, team tactics, stamina management, switching, battle rules and league progression.

Possible branches:

- Competitive Battling
- Team Tactics
- Core Vitality Management
- Switching and Entry Effects
- League Discipline

### Ranger

Focused on wild Pokemon rescue, calming, anti-poaching, tracking and non-battle solutions.

Possible branches:

- Rescue
- Wildlife Handling
- Tracking
- Anti-Poaching
- Field Authority

### Caretaker / Breeder

Focused on Pokemon care, treatment, grooming, nutrition, recovery, mood and bonding.

Possible branches:

- Grooming
- Medical Care
- Bonding
- Nutrition
- Recovery

### Farmer / Rancher

Focused on farming, ranch systems, Pokemon-assisted production, resource processing and food ingredients.

Possible branches:

- Crop Production
- Pokemon Work Roles
- Ranch Facilities
- Cooking Ingredients
- Processing

### Performer

Focused on contests, stage performance, appearance, choreography, move combinations, audience reaction and presentation.

Possible branches:

- Stage Presence
- Appearance
- Move Choreography
- Audience Response
- Costumes and Accessories

### Researcher

Focused on Pokedex Plus, PokeNav knowledge, field observation, rare behavior, habitats and knowledge-gated encounters.

Possible branches:

- Field Biology
- Rare Behavior
- Region Study
- PokeNav Knowledge
- Encounter Discovery

### Explorer

Focused on routes, camping, map discovery, ride usage, weather, regional adaptation and long journeys.

Possible branches:

- Camping
- Mapping
- Route Survival
- Ride Mastery
- Weather Adaptation

### Merchant / Crafter

Focused on trade, crafting, deliveries, regional goods, shop services, recipes and economic support systems.

Possible branches:

- Regional Trade
- Delivery Work
- Crafting Professions
- Shop Knowledge
- Supply Chains

### Investigator / Law

Focused on rumors, clues, witnesses, illegal activity, poachers, restricted areas and law-related tasks.

Possible branches:

- Clue Analysis
- Rumor Work
- Witness Handling
- Infiltration
- Anti-Poaching Cases

## Pokemon Vitality and Battle Condition

The game uses a deeper vitality model than simple battle HP.

### Battle HP

Battle HP is the short-term HP used during combat. It can be depleted in battle, causing the Pokemon to be defeated for that fight.

### Core HP / Condition

Core HP represents long-term condition. After a battle, missing Battle HP can recover by spending Core HP. If Core HP reaches zero, the Pokemon becomes exhausted and needs deeper recovery through camp, clinic or special care.

### Stamina

Stamina is used for moves, effort and action economy. Stronger moves may cost more stamina. Some moves can restore stamina, while some situations may reduce stamina recovery.

### Injury and Fatigue

Low Core HP should not always fully disable a Pokemon. Instead, it can create staged consequences:

- Slower stamina recovery
- Lower starting battle condition
- Mood loss
- Reduced use of risky moves
- Need for camp care or clinic service

Only severe exhaustion should make a Pokemon temporarily unavailable.

## Battle Direction

The battle system should eventually move beyond the classic four-move limit.

Pokemon may learn many moves, but the UI should organize them through categories or role-based menus.

Possible move categories:

- Quick Attacks
- Heavy Attacks
- Defense
- Status Effects
- Stamina Tools
- Core HP Pressure
- Support
- Area Effects
- Last Resort Moves
- Performance Moves
- Camp or Utility Skills

Some moves may affect Core HP directly, but these should be special and clearly balanced through stamina cost, accuracy, risk or telegraphing.

## Enemy Intent and Skill-Based Reading

The player should not automatically see all enemy intentions. Instead, intent reading can be unlocked through life-path perks.

Example progression:

- No perk: “The enemy looks aggressive.”
- Basic perk: “The enemy may be preparing a strong attack.”
- Advanced perk: “The enemy may use a Core HP pressure move.”
- Expert perk: “The enemy is likely preparing a crushing physical attack.”

This supports Trainer, Researcher and Ranger builds without making the whole battle system feel unrealistic from the start.

## Shield Layer Instead of Universal Break

A universal break/guard system for every enemy could make battles too slow. Instead, defensive moves can create temporary shield layers.

Examples:

- Protect creates a one-turn shield.
- Iron Defense creates a temporary physical shield.
- Reflect creates a team-wide shield.
- Shell Guard reduces Core HP pressure.
- Brace spends stamina to reduce an incoming hit.

This keeps defensive tactics meaningful without forcing every battle to revolve around guard breaking.

## Camp System

Camps are an important part of the journey. The player should not have only one fixed camp. Different regions can have different camp scenes, with local visuals, conditions and available stations.

Camp should support:

- Resting and recovery
- Pokemon care
- Cooking
- Pokemon assignments
- Social activities
- Companion interactions
- Research review
- Training
- Performance practice
- Storage and preparation

### Camp Stations

Camp decoration should be functional rather than purely cosmetic. Stations can define what the player can do in a camp.

Examples:

- Cooking Station
- Care Station
- Medical Station
- Research Table
- Training Ground
- Performance Corner
- Storage Chest
- Sleeping Area
- Pokemon Play Area
- Guard Post
- Mini Farm Plot

Companions may unlock or upgrade stations depending on their role.

## Pokemon Assignment System

Pokemon should be assignable to tasks outside battle.

Example assignments:

- Guard the camp
- Search for a missing Pokemon
- Scout from the air
- Track a scent
- Gather herbs
- Help cook
- Water crops
- Power a device
- Calm a wild Pokemon
- Assist medical care
- Carry delivery cargo

Species, type, personality, condition and learned utility skills can affect assignment results.

For example, a flying Pokemon may help search a wide area, while a scent-focused Pokemon may be better at tracking a missing Pokemon.

## Region and Situation Events

The world can use two event models.

### Region Events

These are tied to locations or map zones. Each region can have event pools with chances, cooldowns and weight decay after occurrence. This prevents the same event from appearing too often.

Examples:

- Injured wild Pokemon
- Lost NPC
- Poacher trace
- Rare berry tree
- Aggressive Pokemon pack
- Weather hazard
- Research sighting

### Situation Events

These depend on the player’s current state.

Examples:

- A tired party may trigger a fatigue event.
- A Ranger may be asked for emergency help.
- Rain may trigger flooding or rare encounters.
- A specific companion may notice a clue.
- A Pokemon personality may create a camp event.

Both event types should use requirements and cooldowns to avoid becoming noisy.

## Time Phase System

The game should not rely on real-world time or strict forced bedtime. Instead, days can be divided into phases such as morning, midday, evening, night and rest.

Activities and scene transitions consume phase budget. If the player skips rest for too long, fatigue and debuffs increase.

This gives time meaning without forcing the player into an overly restrictive schedule.

## Regional Ecology and Climate

Regions can apply lightweight ecological effects.

Examples:

- Desert regions may strain Water-type Pokemon but benefit Ground or Rock types.
- Snow regions may reward Fire-type camp support and Ice-type comfort.
- Swamps may increase cleanliness issues and poison-related events.
- Forests may improve Grass and Bug utility assignments.

This does not need to be a full simulation. The goal is to make regions feel different and make team choice matter beyond battle typing.

## Cooking and Food

Cooking can support travel, care, farming and preparation.

Food can provide temporary effects such as:

- Stamina recovery boost
- Core HP recovery efficiency
- Weather resistance
- Mood improvement
- Contest appearance bonus
- Research focus
- Ranger calming bonus
- Farming productivity bonus

Ingredients can come from farming, shops, gathering, deliveries or region-specific sources.

## Farming and Ranching

Farming should be optional but meaningful for players who choose that path.

Pokemon can support farming through type and assignment roles:

- Water Pokemon help with irrigation.
- Grass Pokemon improve crop growth or plant care.
- Electric Pokemon power machines.
- Fire Pokemon help with cooking or processing.
- Bug Pokemon help with pollination.
- Rock or Ground Pokemon help with mining and land work.

The farm can connect to cooking, crafting, markets, care items, contest preparation and regional trade.

## Research, PokeNav and Knowledge

Research should be more than filling a list. PokeNav and Pokedex systems can store learned information that changes gameplay.

Knowledge can reveal:

- Rare encounter conditions
- Weather-based behavior
- Preferred food
- Calming methods
- Habitat patterns
- Special quest hints
- Regional dangers
- Alternate routes

Some encounters can be knowledge-gated. The player may need to learn when, where or how a Pokemon appears before reliably finding it.

## Quests and Multiple Solutions

Quests should support multiple approaches when possible.

Example: a farmer’s Pokemon escaped into the forest.

Possible solutions:

- Trainer: defeat aggressive wild Pokemon blocking the path.
- Ranger: track and calm the escaped Pokemon.
- Caretaker: identify that it is injured or scared.
- Researcher: understand its behavior and destination.
- Farmer: use familiar feed or habitat knowledge.
- Investigator: discover that someone opened the gate.

Not every quest needs all solutions, but important quests should support more than one life path.

## Rumor, Law and Reputation

The world can react through rumor and law systems.

Rumors can spread information about the player’s actions:

- Rescuing Pokemon
- Winning tournaments
- Helping towns
- Breaking rules
- Discovering rare species
- Investigating poachers

Law and permit systems can control access to restricted areas, rare species handling, medical treatment, research sampling, transport and official Ranger or Investigator tasks.

## Companions

Companions should support more than combat. They can provide:

- New quests
- Activities
- Relationship scenes
- Camp station upgrades
- Field support
- Dialogue insight
- Role-specific bonuses
- Pokemon team support

A Researcher companion may notice rare behavior. A Ranger companion may help with tracking. A Merchant companion may identify regional trade opportunities. A Performer companion may help prepare stage routines.

## Equipment, Bags and Accessories

Instead of only selecting a single keepsake, the player can gain small bonuses from bags, kits, accessories and tools.

Examples:

- Ranger Bag: better rescue item capacity.
- Research Notebook: more research progress from observations.
- Care Kit: more efficient grooming and treatment.
- Performer Ribbon Case: contest preparation bonuses.
- Farmer Belt: more seed or berry capacity.
- Travel Pack: better camp or route endurance.

Some perks can require related equipment or licenses.

## Pokemon Personality

Pokemon personalities can affect behavior, mood and activities.

Examples:

- Brave Pokemon handle dangerous tasks better.
- Timid Pokemon may dislike loud events but avoid danger well.
- Gluttonous Pokemon gain more from food activities.
- Curious Pokemon help with exploration and research.
- Protective Pokemon may defend weaker party members.
- Stubborn Pokemon may bond slowly but become very loyal.

Personality should add flavor and small mechanical differences, not become a frustrating punishment system.

## Project Direction

This project is intentionally broad. The goal is not necessarily to finish every system at commercial quality. The goal is to build a coherent framework for a dream Pokemon-world RPG where many playstyles can exist together.

The key rule is:

**Do not shrink the dream; modularize it.**

A system can be large as long as it remains optional, data-driven and connected to the core fantasy.

## Design Filter for Future Systems

Before adding a new mechanic, ask:

1. Which life path does it support?
2. Does it make Pokemon feel more alive or useful?
3. Does it improve travel, camp, role identity or world reaction?
4. Can it be optional?
5. Can it work through data and ScriptableObject definitions?
6. Can the game still run if this path has incomplete content?

If the answer is yes, the mechanic probably fits the project.

## Current Development Philosophy

The project currently favors backend architecture first. Many systems are script-ready but still need content, UI, scene setup, balancing and assets.

That is acceptable for this project’s purpose. The important thing is to keep a clear map of what each system is meant to support and avoid turning the project into disconnected feature lists.

The intended final identity is:

**A modular Pokemon-world life RPG where the player's actions define their role, Pokemon matter in and out of battle, and the world reacts through travel, care, reputation, licenses, rumors, companions and discovery.**
