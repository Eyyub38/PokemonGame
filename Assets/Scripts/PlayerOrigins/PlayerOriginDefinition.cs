using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PlayerOriginCategory {
    General,
    Trainer,
    Researcher,
    Farmer,
    Caretaker,
    Crafter,
    Merchant,
    Ranger,
    Explorer,
    Contest,
    Police,
    Companion,
    Custom
}

[CreateAssetMenu(menuName = "Player/Origins/Origin Definition")]
public class PlayerOriginDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this player origin. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future New Game UI, debug logs and notifications. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation for this starting background.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad origin group used by requirements and future UI filters.")]
    [SerializeField] PlayerOriginCategory category = PlayerOriginCategory.General;
    [Tooltip("Free-form tags such as professor, farm, care, combat, social, crafting, police or hard-mode.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Optional icon used by future New Game or character profile UI.")]
    [SerializeField] Sprite icon = null;

    [Header("Selection Rules")]
    [Tooltip("If enabled, this origin can replace an already selected origin when Force Replace is used by code/source components.")]
    [SerializeField] bool allowReplacingExistingOrigin = false;
    [Tooltip("If enabled, successful and blocked attempts are stored in PlayerOriginLog.")]
    [SerializeField] bool recordHistory = true;
    [Tooltip("If enabled, blocked selection attempts are stored in PlayerOriginLog.")]
    [SerializeField] bool recordBlockedAttempts = true;
    [Tooltip("How origin requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Requirements checked before this origin can be applied.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();

    [Header("Starting Location")]
    [Tooltip("Starting region connected to this origin.")]
    [SerializeField] RegionInfoDefinition startingRegion = null;
    [Tooltip("Starting map marker connected to this origin.")]
    [SerializeField] MapMarkerDefinition startingMapMarker = null;
    [Tooltip("Location visit applied when this origin is selected.")]
    [SerializeField] LocationVisitDefinition startingLocationVisit = null;
    [Tooltip("Navigation hint activated when this origin is selected.")]
    [SerializeField] NavigationHintDefinition startingNavigationHint = null;
    [Tooltip("Optional scene name for save/debug/future New Game spawning.")]
    [SerializeField] string startingSceneName = string.Empty;
    [Tooltip("Optional spawn point id for future New Game spawning.")]
    [SerializeField] string spawnPointId = string.Empty;

    [Header("Starting Resources")]
    [Tooltip("Money added to Wallet when this origin is selected.")]
    [Min(0f)]
    [SerializeField] float startingMoney = 0f;
    [Tooltip("Items added to Inventory when this origin is selected.")]
    [SerializeField] List<PlayerOriginItemGrant> itemGrants = new List<PlayerOriginItemGrant>();
    [Tooltip("Pokemon added to the player's party/storage when this origin is selected.")]
    [SerializeField] List<PlayerOriginPokemonGrant> pokemonGrants = new List<PlayerOriginPokemonGrant>();
    [Tooltip("Tools added or repaired when this origin is selected.")]
    [SerializeField] List<PlayerOriginToolGrant> toolGrants = new List<PlayerOriginToolGrant>();
    [Tooltip("Recipes learned when this origin is selected.")]
    [SerializeField] List<RecipeGrant> recipeGrants = new List<RecipeGrant>();
    [Tooltip("Trainer experience awarded when this origin is selected.")]
    [Min(0)]
    [SerializeField] int trainerExperience = 0;
    [Tooltip("Experience source used for the starting trainer experience grant.")]
    [SerializeField] PlayerExperienceSource experienceSource = PlayerExperienceSource.Exploration;

    [Header("Progression Unlocks")]
    [Tooltip("Titles, badges, permits or licenses granted when this origin is selected.")]
    [SerializeField] List<TitleGrant> titleGrants = new List<TitleGrant>();
    [Tooltip("Milestones completed when this origin is selected.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Career paths unlocked when this origin is selected.")]
    [SerializeField] List<CareerPathDefinition> careersToUnlock = new List<CareerPathDefinition>();
    [Tooltip("Career paths joined when this origin is selected.")]
    [SerializeField] List<PlayerOriginCareerJoin> careersToJoin = new List<PlayerOriginCareerJoin>();
    [Tooltip("Career points granted when this origin is selected.")]
    [SerializeField] List<CareerPointGrant> careerPointGrants = new List<CareerPointGrant>();
    [Tooltip("Organization memberships granted when this origin is selected.")]
    [SerializeField] List<OrganizationMembershipGrant> organizationMembershipGrants = new List<OrganizationMembershipGrant>();
    [Tooltip("Organization points granted when this origin is selected.")]
    [SerializeField] List<OrganizationPointGrant> organizationPointGrants = new List<OrganizationPointGrant>();
    [Tooltip("Faction reputation changes applied when this origin is selected.")]
    [SerializeField] List<ReputationChange> reputationChanges = new List<ReputationChange>();
    [Tooltip("Relationship changes applied when this origin is selected.")]
    [SerializeField] List<RelationshipChange> relationshipChanges = new List<RelationshipChange>();
    [Tooltip("Research progress granted when this origin is selected.")]
    [SerializeField] List<PlayerOriginResearchGrant> researchGrants = new List<PlayerOriginResearchGrant>();

    [Header("Knowledge Unlocks")]
    [Tooltip("PokeNav entries discovered when this origin is selected.")]
    [SerializeField] List<PokeNavEntryDefinition> pokeNavEntries = new List<PokeNavEntryDefinition>();
    [Tooltip("Regions discovered in PokeNav when this origin is selected.")]
    [SerializeField] List<RegionInfoDefinition> regionsToDiscover = new List<RegionInfoDefinition>();
    [Tooltip("Social posts unlocked when this origin is selected.")]
    [SerializeField] List<SocialPostDefinition> socialPostsToUnlock = new List<SocialPostDefinition>();
    [Tooltip("Map markers discovered when this origin is selected.")]
    [SerializeField] List<MapMarkerDefinition> mapMarkersToDiscover = new List<MapMarkerDefinition>();
    [Tooltip("World discoveries applied when this origin is selected.")]
    [SerializeField] List<WorldDiscoveryDefinition> worldDiscoveries = new List<WorldDiscoveryDefinition>();

    [Header("Consequences")]
    [Tooltip("Consequence chains applied after this origin is successfully selected.")]
    [SerializeField] List<ConsequenceChainDefinition> selectedChains = new List<ConsequenceChainDefinition>();
    [Tooltip("Consequence chains applied when this origin selection is blocked.")]
    [SerializeField] List<ConsequenceChainDefinition> blockedChains = new List<ConsequenceChainDefinition>();

    [Header("Events")]
    [Tooltip("Optional event published when this origin is selected. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition selectedEvent = null;
    [Tooltip("Optional event published when this origin selection is blocked. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition blockedEvent = null;
    [Tooltip("If enabled, origin events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, origin events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog = false;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public PlayerOriginCategory Category => category;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public Sprite Icon => icon;
    public bool AllowReplacingExistingOrigin => allowReplacingExistingOrigin;
    public bool RecordHistory => recordHistory;
    public bool RecordBlockedAttempts => recordBlockedAttempts;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public RegionInfoDefinition StartingRegion => startingRegion;
    public MapMarkerDefinition StartingMapMarker => startingMapMarker;
    public LocationVisitDefinition StartingLocationVisit => startingLocationVisit;
    public NavigationHintDefinition StartingNavigationHint => startingNavigationHint;
    public string StartingSceneName => startingSceneName;
    public string SpawnPointId => spawnPointId;
    public float StartingMoney => Mathf.Max(0f, startingMoney);
    public IReadOnlyList<PlayerOriginItemGrant> ItemGrants => itemGrants != null ? (IReadOnlyList<PlayerOriginItemGrant>)itemGrants : Array.Empty<PlayerOriginItemGrant>();
    public IReadOnlyList<PlayerOriginPokemonGrant> PokemonGrants => pokemonGrants != null ? (IReadOnlyList<PlayerOriginPokemonGrant>)pokemonGrants : Array.Empty<PlayerOriginPokemonGrant>();
    public IReadOnlyList<PlayerOriginToolGrant> ToolGrants => toolGrants != null ? (IReadOnlyList<PlayerOriginToolGrant>)toolGrants : Array.Empty<PlayerOriginToolGrant>();
    public IReadOnlyList<RecipeGrant> RecipeGrants => recipeGrants != null ? (IReadOnlyList<RecipeGrant>)recipeGrants : Array.Empty<RecipeGrant>();
    public int TrainerExperience => Mathf.Max(0, trainerExperience);
    public PlayerExperienceSource ExperienceSource => experienceSource;
    public IReadOnlyList<TitleGrant> TitleGrants => titleGrants != null ? (IReadOnlyList<TitleGrant>)titleGrants : Array.Empty<TitleGrant>();
    public IReadOnlyList<MilestoneDefinition> MilestonesToComplete => milestonesToComplete != null ? (IReadOnlyList<MilestoneDefinition>)milestonesToComplete : Array.Empty<MilestoneDefinition>();
    public IReadOnlyList<CareerPathDefinition> CareersToUnlock => careersToUnlock != null ? (IReadOnlyList<CareerPathDefinition>)careersToUnlock : Array.Empty<CareerPathDefinition>();
    public IReadOnlyList<PlayerOriginCareerJoin> CareersToJoin => careersToJoin != null ? (IReadOnlyList<PlayerOriginCareerJoin>)careersToJoin : Array.Empty<PlayerOriginCareerJoin>();
    public IReadOnlyList<CareerPointGrant> CareerPointGrants => careerPointGrants != null ? (IReadOnlyList<CareerPointGrant>)careerPointGrants : Array.Empty<CareerPointGrant>();
    public IReadOnlyList<OrganizationMembershipGrant> OrganizationMembershipGrants => organizationMembershipGrants != null ? (IReadOnlyList<OrganizationMembershipGrant>)organizationMembershipGrants : Array.Empty<OrganizationMembershipGrant>();
    public IReadOnlyList<OrganizationPointGrant> OrganizationPointGrants => organizationPointGrants != null ? (IReadOnlyList<OrganizationPointGrant>)organizationPointGrants : Array.Empty<OrganizationPointGrant>();
    public IReadOnlyList<ReputationChange> ReputationChanges => reputationChanges != null ? (IReadOnlyList<ReputationChange>)reputationChanges : Array.Empty<ReputationChange>();
    public IReadOnlyList<RelationshipChange> RelationshipChanges => relationshipChanges != null ? (IReadOnlyList<RelationshipChange>)relationshipChanges : Array.Empty<RelationshipChange>();
    public IReadOnlyList<PlayerOriginResearchGrant> ResearchGrants => researchGrants != null ? (IReadOnlyList<PlayerOriginResearchGrant>)researchGrants : Array.Empty<PlayerOriginResearchGrant>();
    public IReadOnlyList<PokeNavEntryDefinition> PokeNavEntries => pokeNavEntries != null ? (IReadOnlyList<PokeNavEntryDefinition>)pokeNavEntries : Array.Empty<PokeNavEntryDefinition>();
    public IReadOnlyList<RegionInfoDefinition> RegionsToDiscover => regionsToDiscover != null ? (IReadOnlyList<RegionInfoDefinition>)regionsToDiscover : Array.Empty<RegionInfoDefinition>();
    public IReadOnlyList<SocialPostDefinition> SocialPostsToUnlock => socialPostsToUnlock != null ? (IReadOnlyList<SocialPostDefinition>)socialPostsToUnlock : Array.Empty<SocialPostDefinition>();
    public IReadOnlyList<MapMarkerDefinition> MapMarkersToDiscover => mapMarkersToDiscover != null ? (IReadOnlyList<MapMarkerDefinition>)mapMarkersToDiscover : Array.Empty<MapMarkerDefinition>();
    public IReadOnlyList<WorldDiscoveryDefinition> WorldDiscoveries => worldDiscoveries != null ? (IReadOnlyList<WorldDiscoveryDefinition>)worldDiscoveries : Array.Empty<WorldDiscoveryDefinition>();
    public IReadOnlyList<ConsequenceChainDefinition> SelectedChains => selectedChains != null ? (IReadOnlyList<ConsequenceChainDefinition>)selectedChains : Array.Empty<ConsequenceChainDefinition>();
    public IReadOnlyList<ConsequenceChainDefinition> BlockedChains => blockedChains != null ? (IReadOnlyList<ConsequenceChainDefinition>)blockedChains : Array.Empty<ConsequenceChainDefinition>();

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public bool CanApply(PlayerController player, PlayerOriginLog log, bool forceReplace, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required for origin selection.";
            return false;
        }

        if(log != null && log.HasSelectedOrigin && !forceReplace) {
            failureMessage = $"Player already has origin {log.SelectedOriginName}.";
            return false;
        }

        if(log != null && log.HasSelectedOrigin && forceReplace && !allowReplacingExistingOrigin) {
            failureMessage = $"{DisplayName} cannot replace an existing origin.";
            return false;
        }

        if(!ConsequenceChainDefinition.RequirementsMet(player, requirements, requirementMatchMode, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public PlayerOriginApplyResult Apply(PlayerController player, string sourceId = null, string sourceName = null, UnityEngine.Object context = null, bool forceReplace = false) {
        var result = new PlayerOriginApplyResult(Id, DisplayName, category, NormalizeSourceId(sourceId), string.IsNullOrWhiteSpace(sourceName) ? DisplayName : sourceName, startingSceneName, spawnPointId);
        var log = player != null ? player.GetComponent<PlayerOriginLog>() ?? player.gameObject.AddComponent<PlayerOriginLog>() : null;

        if(!CanApply(player, log, forceReplace, out var failureMessage)) {
            result.blocked = true;
            result.failureMessage = failureMessage;
            if(recordHistory && recordBlockedAttempts) {
                log?.RecordBlocked(this, result);
            }

            ApplyChains(player, blockedChains, result, context, "blocked");
            PublishOriginEvent(blockedEvent, "blocked", result, context, GameEventImportance.Warning);
            return result;
        }

        ApplyStartingPackage(player, result, context);
        if(recordHistory) {
            log?.RecordSelected(this, result, forceReplace);
        }

        ApplyChains(player, selectedChains, result, context, "selected");
        PublishOriginEvent(selectedEvent, "selected", result, context, GameEventImportance.Success);
        return result;
    }

    void ApplyStartingPackage(PlayerController player, PlayerOriginApplyResult result, UnityEngine.Object context) {
        ApplyMoney(result);
        ApplyItems(player, result);
        ApplyPokemon(player, result);
        ApplyTools(player, result);
        ApplyRecipes(player, result, context);
        ApplyProgression(player, result);
        ApplyKnowledge(player, result, context);
    }

    void ApplyMoney(PlayerOriginApplyResult result) {
        if(StartingMoney <= 0f) {
            return;
        }

        if(Wallet.i == null) {
            result.messages.Add("Wallet is missing; starting money was skipped.");
            return;
        }

        Wallet.i.AddMoney(StartingMoney);
        result.moneyGranted = StartingMoney;
    }

    void ApplyItems(PlayerController player, PlayerOriginApplyResult result) {
        var inventory = player.GetComponent<Inventory>();
        if(inventory == null) {
            if(ItemGrants.Count > 0) {
                result.messages.Add("Inventory is missing; starting item grants were skipped.");
            }
            return;
        }

        foreach(var grant in ItemGrants) {
            if(grant == null || grant.Item == null || grant.Count <= 0) {
                result.skippedItemGrants++;
                continue;
            }

            inventory.AddItem(grant.Item, grant.Count);
            result.itemGrants++;
        }
    }

    void ApplyPokemon(PlayerController player, PlayerOriginApplyResult result) {
        var party = player.GetComponent<PokemonParty>();
        if(party == null) {
            if(PokemonGrants.Count > 0) {
                result.messages.Add("PokemonParty is missing; starter Pokemon grants were skipped.");
            }
            return;
        }

        foreach(var grant in PokemonGrants) {
            if(grant == null || grant.Pokemon == null) {
                result.skippedPokemonGrants++;
                continue;
            }

            var pokemon = grant.CreatePokemon();
            if(pokemon == null) {
                result.skippedPokemonGrants++;
                continue;
            }

            party.AddPokemon(pokemon);
            result.pokemonGrants++;
        }
    }

    void ApplyTools(PlayerController player, PlayerOriginApplyResult result) {
        if(ToolGrants.Count == 0) {
            return;
        }

        var tools = player.GetComponent<PlayerToolInventory>() ?? player.gameObject.AddComponent<PlayerToolInventory>();
        foreach(var grant in ToolGrants) {
            if(grant == null || grant.Tool == null) {
                result.skippedToolGrants++;
                continue;
            }

            tools.AddOrRepairTool(grant.Tool, grant.Level, grant.Durability);
            result.toolGrants++;
        }
    }

    void ApplyRecipes(PlayerController player, PlayerOriginApplyResult result, UnityEngine.Object context) {
        if(RecipeGrants.Count == 0) {
            return;
        }

        var recipes = player.GetComponent<PlayerRecipeBook>() ?? player.gameObject.AddComponent<PlayerRecipeBook>();
        foreach(var grant in RecipeGrants) {
            if(grant == null || !grant.IsValid) {
                result.skippedRecipeGrants++;
                continue;
            }

            if(recipes.Learn(grant, context != null ? context : this)) {
                result.recipeGrants++;
            }
        }
    }

    void ApplyProgression(PlayerController player, PlayerOriginApplyResult result) {
        player.GetComponent<PlayerProgression>()?.AddExperience(TrainerExperience, experienceSource);
        if(TrainerExperience > 0) {
            result.trainerExperienceGranted = TrainerExperience;
        }

        player.GetComponent<PlayerTitles>()?.ApplyGrants(titleGrants, this);
        result.titleGrants = CountValid(titleGrants, grant => grant != null && grant.title != null);

        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);
        result.milestonesCompleted = CountValid(milestonesToComplete, milestone => milestone != null);

        var careerLog = player.GetComponent<PlayerCareerLog>();
        if(careerLog != null) {
            foreach(var career in CareersToUnlock) {
                if(career != null && careerLog.UnlockCareer(career, Id)) {
                    result.careersUnlocked++;
                }
            }

            foreach(var join in CareersToJoin) {
                if(join == null || join.Career == null) {
                    continue;
                }

                if(careerLog.JoinCareer(join.Career, join.ViaMentor, join.ResolveSource(Id), out var failure)) {
                    result.careersJoined++;
                } else if(!string.IsNullOrWhiteSpace(failure)) {
                    result.messages.Add(failure);
                }
            }

            careerLog.ApplyPointGrants(careerPointGrants, Id);
            result.careerPointGrants = CountValid(careerPointGrants, grant => grant != null && grant.career != null && grant.points > 0);
        }

        var organizationLog = player.GetComponent<PlayerOrganizationLog>();
        if(organizationLog != null) {
            organizationLog.ApplyMembershipGrants(organizationMembershipGrants, Id);
            organizationLog.ApplyPointGrants(organizationPointGrants, Id);
            result.organizationMembershipGrants = CountValid(organizationMembershipGrants, grant => grant != null && grant.organization != null);
            result.organizationPointGrants = CountValid(organizationPointGrants, grant => grant != null && grant.organization != null && grant.points > 0);
        }

        player.GetComponent<PlayerReputation>()?.ApplyChanges(reputationChanges);
        result.reputationChanges = CountValid(reputationChanges, change => change != null && change.faction != null && change.amount != 0);

        player.GetComponent<PlayerRelationships>()?.ApplyChanges(relationshipChanges);
        result.relationshipChanges = CountValid(relationshipChanges, change => change != null && change.subject != null && change.amount != 0);

        var researchLog = player.GetComponent<PlayerResearchLog>() ?? player.gameObject.AddComponent<PlayerResearchLog>();
        foreach(var grant in ResearchGrants) {
            if(grant == null || grant.Subject == null || grant.Points <= 0) {
                result.skippedResearchGrants++;
                continue;
            }

            researchLog.AddProgress(grant.Subject, grant.Points);
            result.researchGrants++;
        }
    }

    void ApplyKnowledge(PlayerController player, PlayerOriginApplyResult result, UnityEngine.Object context) {
        var pokeNav = player.GetComponent<PlayerPokeNavLog>() ?? player.gameObject.AddComponent<PlayerPokeNavLog>();
        var mapLog = player.GetComponent<PlayerMapLog>() ?? player.gameObject.AddComponent<PlayerMapLog>();

        foreach(var region in RegionsToDiscover) {
            if(region == null) {
                continue;
            }

            if(pokeNav.DiscoverRegion(region, out var failure)) {
                result.regionsDiscovered++;
            } else if(!string.IsNullOrWhiteSpace(failure)) {
                result.messages.Add(failure);
            }
        }

        if(startingRegion != null) {
            if(pokeNav.DiscoverRegion(startingRegion, out var startRegionFailure)) {
                result.regionsDiscovered++;
            } else if(!string.IsNullOrWhiteSpace(startRegionFailure)) {
                result.messages.Add(startRegionFailure);
            }
        }

        foreach(var entry in PokeNavEntries) {
            if(entry == null) {
                continue;
            }

            if(pokeNav.DiscoverEntry(entry, out var failure)) {
                result.pokeNavEntriesDiscovered++;
            } else if(!string.IsNullOrWhiteSpace(failure)) {
                result.messages.Add(failure);
            }
        }

        foreach(var post in SocialPostsToUnlock) {
            if(post != null && pokeNav.UnlockPost(post)) {
                result.socialPostsUnlocked++;
            }
        }

        foreach(var marker in MapMarkersToDiscover) {
            if(marker != null && mapLog.DiscoverMarker(marker, Id)) {
                result.mapMarkersDiscovered++;
            }
        }

        if(startingMapMarker != null && mapLog.DiscoverMarker(startingMapMarker, Id)) {
            result.mapMarkersDiscovered++;
        }

        foreach(var discovery in WorldDiscoveries) {
            if(discovery == null) {
                result.skippedWorldDiscoveries++;
                continue;
            }

            var discoveryResult = discovery.Apply(player, Id, DisplayName, context != null ? context : this);
            if(discoveryResult != null && !discoveryResult.blocked) {
                result.worldDiscoveriesApplied++;
            } else {
                result.blockedWorldDiscoveries++;
                if(discoveryResult != null && !string.IsNullOrWhiteSpace(discoveryResult.failureMessage)) {
                    result.messages.Add(discoveryResult.failureMessage);
                }
            }
        }

        if(startingLocationVisit != null) {
            var visit = startingLocationVisit.Apply(player, Id, DisplayName, context != null ? context : this);
            if(visit != null && !visit.blocked) {
                result.locationVisitApplied = true;
            } else if(visit != null && !string.IsNullOrWhiteSpace(visit.failureMessage)) {
                result.messages.Add(visit.failureMessage);
            }
        }

        if(startingNavigationHint != null) {
            var hint = startingNavigationHint.Activate(player, Id, DisplayName, context != null ? context : this);
            if(hint != null && !hint.blocked) {
                result.navigationHintActivated = true;
            } else if(hint != null && !string.IsNullOrWhiteSpace(hint.failureMessage)) {
                result.messages.Add(hint.failureMessage);
            }
        }
    }

    void ApplyChains(PlayerController player, IEnumerable<ConsequenceChainDefinition> chains, PlayerOriginApplyResult result, UnityEngine.Object context, string phase) {
        if(player == null || chains == null) {
            return;
        }

        var chainContext = new ConsequenceChainContext {
            SourceId = $"{Id}:{phase}",
            SourceName = DisplayName,
            Region = startingRegion,
            ContextObject = context != null ? context : this
        };

        foreach(var chain in chains) {
            if(chain == null) {
                result.skippedChains++;
                continue;
            }

            var chainResult = chain.Apply(player, chainContext, context != null ? context : this);
            if(chainResult != null && !chainResult.blocked) {
                result.appliedChains++;
            } else {
                result.blockedChains++;
                if(chainResult != null && !string.IsNullOrWhiteSpace(chainResult.failureMessage)) {
                    result.messages.Add(chainResult.failureMessage);
                }
            }
        }
    }

    void PublishOriginEvent(GameEventDefinition eventDefinition, string phase, PlayerOriginApplyResult result, UnityEngine.Object context, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"player-origin.{phase}.{Id}",
            result.blocked
                ? $"{DisplayName} origin blocked: {result.failureMessage}"
                : $"{DisplayName} origin selected.",
            GameEventCategory.RPG,
            importance,
            context != null ? context : this,
            "PlayerOriginDefinition",
            GameEventScope.Player,
            showEventsInFeed,
            writeEventsToDebugLog,
            GameEventPublishing.Value("originId", Id),
            GameEventPublishing.Value("originName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", result.sourceId));
    }

    string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? Id : sourceId;
    }

    int CountValid<T>(IEnumerable<T> entries, Func<T, bool> predicate) {
        return entries == null ? 0 : entries.Count(predicate);
    }
}

[Serializable]
public class PlayerOriginItemGrant {
    [Tooltip("Item added when this origin is selected.")]
    [SerializeField] ItemBase item = null;
    [Tooltip("Number of this item added when this origin is selected.")]
    [Min(1)]
    [SerializeField] int count = 1;

    public ItemBase Item => item;
    public int Count => Mathf.Max(1, count);
}

[Serializable]
public class PlayerOriginPokemonGrant {
    [Tooltip("Pokemon species added when this origin is selected.")]
    [SerializeField] PokemonBase pokemon = null;
    [Tooltip("Level of the generated Pokemon.")]
    [Min(1)]
    [SerializeField] int level = 5;
    [Tooltip("Optional Pokeball assigned to the generated Pokemon.")]
    [SerializeField] PokeballItem pokeball = null;
    [Tooltip("Optional fixed gender. None lets normal Pokemon initialization decide.")]
    [SerializeField] Gender gender = Gender.None;
    [Tooltip("Optional nickname for this starter Pokemon.")]
    [SerializeField] string nickname = string.Empty;

    public PokemonBase Pokemon => pokemon;
    public int Level => Mathf.Max(1, level);
    public PokeballItem Pokeball => pokeball;
    public Gender Gender => gender;
    public string Nickname => nickname;

    public Pokemon CreatePokemon() {
        if(pokemon == null) {
            return null;
        }

        var created = new Pokemon(pokemon, Level, pokeball);
        if(gender != Gender.None) {
            created.Gender = gender;
        }

        if(!string.IsNullOrWhiteSpace(nickname)) {
            created.Nickname = nickname;
        }

        return created;
    }
}

[Serializable]
public class PlayerOriginToolGrant {
    [Tooltip("Tool added or repaired when this origin is selected.")]
    [SerializeField] ToolDefinition tool = null;
    [Tooltip("Minimum tool level granted.")]
    [Min(1)]
    [SerializeField] int level = 1;
    [Tooltip("Durability added. Negative value repairs/fills to max durability.")]
    [SerializeField] int durability = -1;

    public ToolDefinition Tool => tool;
    public int Level => Mathf.Max(1, level);
    public int Durability => durability;
}

[Serializable]
public class PlayerOriginCareerJoin {
    [Tooltip("Career path joined when this origin is selected.")]
    [SerializeField] CareerPathDefinition career = null;
    [Tooltip("If enabled, this join attempt counts as mentor-supported.")]
    [SerializeField] bool viaMentor = true;
    [Tooltip("Optional source label stored in career state. Empty uses the origin id.")]
    [SerializeField] string sourceOverride = string.Empty;

    public CareerPathDefinition Career => career;
    public bool ViaMentor => viaMentor;

    public string ResolveSource(string fallback) {
        return string.IsNullOrWhiteSpace(sourceOverride) ? fallback : sourceOverride;
    }
}

[Serializable]
public class PlayerOriginResearchGrant {
    [Tooltip("Research subject receiving starting progress.")]
    [SerializeField] ResearchSubjectDefinition subject = null;
    [Tooltip("Research points added to this subject.")]
    [Min(1)]
    [SerializeField] int points = 1;

    public ResearchSubjectDefinition Subject => subject;
    public int Points => Mathf.Max(1, points);
}

[Serializable]
public class PlayerOriginApplyResult {
    [Tooltip("Origin id used by this result.")]
    public string originId;
    [Tooltip("Origin display name used by this result.")]
    public string originName;
    [Tooltip("Origin category used by this result.")]
    public PlayerOriginCategory category;
    [Tooltip("Source id that requested the origin selection.")]
    public string sourceId;
    [Tooltip("Source display name that requested the origin selection.")]
    public string sourceName;
    [Tooltip("Starting scene name stored by this origin.")]
    public string startingSceneName;
    [Tooltip("Spawn point id stored by this origin.")]
    public string spawnPointId;
    [Tooltip("Whether the origin selection was blocked.")]
    public bool blocked;
    [Tooltip("Reason for a blocked origin selection.")]
    public string failureMessage;
    [Tooltip("Money granted by this origin.")]
    public float moneyGranted;
    [Tooltip("Number of item grants applied.")]
    public int itemGrants;
    [Tooltip("Number of item grants skipped.")]
    public int skippedItemGrants;
    [Tooltip("Number of Pokemon grants applied.")]
    public int pokemonGrants;
    [Tooltip("Number of Pokemon grants skipped.")]
    public int skippedPokemonGrants;
    [Tooltip("Number of tool grants applied.")]
    public int toolGrants;
    [Tooltip("Number of tool grants skipped.")]
    public int skippedToolGrants;
    [Tooltip("Number of recipes learned.")]
    public int recipeGrants;
    [Tooltip("Number of recipe grants skipped.")]
    public int skippedRecipeGrants;
    [Tooltip("Trainer experience granted.")]
    public int trainerExperienceGranted;
    [Tooltip("Number of valid title grants requested.")]
    public int titleGrants;
    [Tooltip("Number of valid milestones requested.")]
    public int milestonesCompleted;
    [Tooltip("Number of careers unlocked.")]
    public int careersUnlocked;
    [Tooltip("Number of careers joined.")]
    public int careersJoined;
    [Tooltip("Number of career point grants requested.")]
    public int careerPointGrants;
    [Tooltip("Number of organization memberships requested.")]
    public int organizationMembershipGrants;
    [Tooltip("Number of organization point grants requested.")]
    public int organizationPointGrants;
    [Tooltip("Number of reputation changes requested.")]
    public int reputationChanges;
    [Tooltip("Number of relationship changes requested.")]
    public int relationshipChanges;
    [Tooltip("Number of research grants applied.")]
    public int researchGrants;
    [Tooltip("Number of research grants skipped.")]
    public int skippedResearchGrants;
    [Tooltip("Number of PokeNav entries discovered.")]
    public int pokeNavEntriesDiscovered;
    [Tooltip("Number of regions discovered.")]
    public int regionsDiscovered;
    [Tooltip("Number of social posts unlocked.")]
    public int socialPostsUnlocked;
    [Tooltip("Number of map markers discovered.")]
    public int mapMarkersDiscovered;
    [Tooltip("Number of world discoveries applied.")]
    public int worldDiscoveriesApplied;
    [Tooltip("Number of world discoveries blocked.")]
    public int blockedWorldDiscoveries;
    [Tooltip("Number of world discoveries skipped.")]
    public int skippedWorldDiscoveries;
    [Tooltip("Whether starting location visit applied.")]
    public bool locationVisitApplied;
    [Tooltip("Whether starting navigation hint activated.")]
    public bool navigationHintActivated;
    [Tooltip("Number of consequence chains applied.")]
    public int appliedChains;
    [Tooltip("Number of consequence chains blocked.")]
    public int blockedChains;
    [Tooltip("Number of consequence chains skipped.")]
    public int skippedChains;
    [Tooltip("Additional messages from linked systems.")]
    public List<string> messages = new List<string>();

    public PlayerOriginApplyResult(string originId, string originName, PlayerOriginCategory category, string sourceId, string sourceName, string startingSceneName, string spawnPointId) {
        this.originId = originId;
        this.originName = originName;
        this.category = category;
        this.sourceId = sourceId;
        this.sourceName = sourceName;
        this.startingSceneName = startingSceneName;
        this.spawnPointId = spawnPointId;
    }
}
