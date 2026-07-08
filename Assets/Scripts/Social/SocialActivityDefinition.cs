using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SocialActivityKind {
    Hangout,
    Date,
    CompanionActivity,
    PokemonBonding,
    Camp,
    Meal,
    Festival,
    Training,
    Exploration,
    Custom
}

public enum SocialCompanionParticipantMode {
    None,
    AnyFollowingCompanion,
    SpecificRole,
    AllFollowingCompanions
}

public enum SocialPokemonParticipantMode {
    None,
    FirstPartyPokemon,
    FirstHealthyPokemon,
    WholeParty,
    SpecificPartySlot
}

[CreateAssetMenu(menuName = "Social/Social Activity Definition")]
public class SocialActivityDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this social activity. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI, prompts and debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this social activity.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad social activity type used by filters, UI tabs and future event logic.")]
    [SerializeField] SocialActivityKind kind = SocialActivityKind.Hangout;
    [Tooltip("Free-form tags used by UI filters, rules and future content checks.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Base Activity")]
    [Tooltip("Optional ActivityDefinition used for area checks, costs, XP and shared activity rewards. Leave empty for a pure social-only action.")]
    [SerializeField] ActivityDefinition baseActivity;
    [Tooltip("If enabled, base activity rewards are applied after the social activity succeeds.")]
    [SerializeField] bool applyBaseActivityRewards = true;
    [Tooltip("If enabled, relationship rewards stored on the base activity are also applied. Useful because generic activity rewards do not apply them automatically.")]
    [SerializeField] bool applyBaseRelationshipRewards = true;

    [Header("Repeat Rules")]
    [Tooltip("Daily limit used only when Base Activity is empty. If Base Activity exists, its own repeat rules handle this.")]
    [Min(0)]
    [SerializeField] int dailyLimit;
    [Tooltip("Cooldown in in-game hours used only when Base Activity is empty. If Base Activity exists, its own cooldown handles this.")]
    [Min(0)]
    [SerializeField] int cooldownHours;

    [Header("Companion Participants")]
    [Tooltip("How following companions are selected for this social activity.")]
    [SerializeField] SocialCompanionParticipantMode companionMode = SocialCompanionParticipantMode.None;
    [Tooltip("Minimum number of matching following companions required.")]
    [Min(0)]
    [SerializeField] int minCompanions;
    [Tooltip("Maximum number of matching companions affected by rewards. 0 means no maximum.")]
    [Min(0)]
    [SerializeField] int maxCompanions;
    [Tooltip("Required companion role when Companion Mode is Specific Role.")]
    [SerializeField] CompanionRoleDefinition requiredCompanionRole;
    [Tooltip("Minimum bond level required for each selected companion.")]
    [SerializeField] CompanionBondLevel minimumCompanionBond = CompanionBondLevel.Stranger;

    [Header("Pokemon Participants")]
    [Tooltip("How party Pokemon are selected for this social activity.")]
    [SerializeField] SocialPokemonParticipantMode pokemonMode = SocialPokemonParticipantMode.None;
    [Tooltip("Party slot used when Pokemon Mode is Specific Party Slot.")]
    [Min(0)]
    [SerializeField] int partySlotIndex;
    [Tooltip("If enabled, selected Pokemon must have HP above 0.")]
    [SerializeField] bool requireHealthyPokemon = true;
    [Tooltip("Minimum friendship required for each selected Pokemon.")]
    [Range(0, 255)]
    [SerializeField] int minimumPokemonFriendship;
    [Tooltip("Care need requirements checked on every selected Pokemon before the activity can start.")]
    [SerializeField] List<PokemonCareNeedRequirement> pokemonCareRequirements = new List<PokemonCareNeedRequirement>();

    [Header("Social Rewards")]
    [Tooltip("Bond points added to every selected companion.")]
    [Min(0)]
    [SerializeField] int companionBondGain = 2;
    [Tooltip("Friendship added to every selected Pokemon.")]
    [Min(0)]
    [SerializeField] int pokemonFriendshipGain = 2;
    [Tooltip("Mood changes applied to every selected Pokemon.")]
    [SerializeField] List<PokemonMoodChange> pokemonMoodChanges = new List<PokemonMoodChange>();
    [Tooltip("Care need changes applied to every selected Pokemon.")]
    [SerializeField] List<PokemonCareNeedChange> pokemonCareNeedChanges = new List<PokemonCareNeedChange>();
    [Tooltip("Relationship changes applied to the player's relationship log.")]
    [SerializeField] List<RelationshipChange> relationshipChanges = new List<RelationshipChange>();
    [Tooltip("Faction reputation changes applied to the player's reputation log.")]
    [SerializeField] List<ReputationChange> reputationChanges = new List<ReputationChange>();
    [Tooltip("Milestones completed when this social activity succeeds.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Titles, permits or medals granted when this social activity succeeds.")]
    [SerializeField] List<TitleGrant> titleGrants = new List<TitleGrant>();
    [Tooltip("Recipes learned when this social activity succeeds.")]
    [SerializeField] List<RecipeGrant> recipeGrants = new List<RecipeGrant>();
    [Tooltip("Life path XP, branch progress and tag counters awarded when this social activity succeeds.")]
    [SerializeField] List<LifePathReward> lifePathRewards = new List<LifePathReward>();

    [Header("Feedback")]
    [Tooltip("Message returned when the social activity succeeds. Empty uses a generated message.")]
    [TextArea]
    [SerializeField] string successMessage;
    [Tooltip("Optional event published when this social activity succeeds. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition completedEvent;
    [Tooltip("If enabled, completion is written into the custom debug log.")]
    [SerializeField] bool writeToDebugLog = true;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public SocialActivityKind Kind => kind;
    public IReadOnlyList<string> Tags => tags;
    public ActivityDefinition BaseActivity => baseActivity;
    public int DailyLimit => Mathf.Max(0, dailyLimit);
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public SocialCompanionParticipantMode CompanionMode => companionMode;
    public SocialPokemonParticipantMode PokemonMode => pokemonMode;
    public IReadOnlyList<PokemonMoodChange> PokemonMoodChanges => pokemonMoodChanges;
    public IReadOnlyList<PokemonCareNeedChange> PokemonCareNeedChanges => pokemonCareNeedChanges;
    public IReadOnlyList<RelationshipChange> RelationshipChanges => relationshipChanges;
    public IReadOnlyList<ReputationChange> ReputationChanges => reputationChanges;
    public IReadOnlyList<MilestoneDefinition> MilestonesToComplete => milestonesToComplete;
    public IReadOnlyList<TitleGrant> TitleGrants => titleGrants;
    public IReadOnlyList<RecipeGrant> RecipeGrants => recipeGrants;
    public IReadOnlyList<LifePathReward> LifePathRewards => lifePathRewards;

    public bool CanRun(PlayerController player, out string failureMessage) {
        return CanRun(player, out failureMessage, out _);
    }

    public bool TryRun(PlayerController player, string sourceId, UnityEngine.Object context, out SocialActivityResult result) {
        player = player != null ? player : PlayerController.i;
        result = SocialActivityResult.Failed(this, sourceId, "Player is missing.");

        if(!CanRun(player, out var failureMessage, out var participants)) {
            result = SocialActivityResult.Failed(this, sourceId, failureMessage);
            RecordResult(player, result);
            return false;
        }

        if(baseActivity != null && !baseActivity.TryPayCosts(player, out failureMessage)) {
            result = SocialActivityResult.Failed(this, sourceId, failureMessage);
            RecordResult(player, result);
            return false;
        }

        ApplyRewards(player, participants, context);

        string message = BuildSuccessMessage(participants);
        result = SocialActivityResult.Succeeded(this, sourceId, message, participants);
        RecordResult(player, result);
        PublishCompletedEvent(player, participants, context, message);

        if(writeToDebugLog) {
            GameDebug.Success(message, GameDebugCategory.Activity, context, "SocialActivityDefinition");
        }

        return true;
    }

    bool CanRun(PlayerController player, out string failureMessage, out SocialActivityParticipants participants) {
        participants = SocialActivityParticipants.Empty;

        if(player == null) {
            failureMessage = "Player is missing.";
            return false;
        }

        if(baseActivity != null && !baseActivity.CanPerform(player, out failureMessage)) {
            return false;
        }

        if(baseActivity == null) {
            var log = player.GetComponent<PlayerSocialActivityLog>();
            if(log != null && !log.CanRun(this, DailyLimit, CooldownHours, out failureMessage)) {
                return false;
            }
        }

        participants = ResolveParticipants(player);
        if(!ValidateCompanions(participants.Companions, out failureMessage)) {
            return false;
        }

        if(!ValidatePokemon(participants.Pokemon, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    SocialActivityParticipants ResolveParticipants(PlayerController player) {
        var companions = ResolveCompanions(player);
        var pokemon = ResolvePokemon(player);
        return new SocialActivityParticipants(companions, pokemon);
    }

    List<CompanionController> ResolveCompanions(PlayerController player) {
        if(companionMode == SocialCompanionParticipantMode.None) {
            return new List<CompanionController>();
        }

        IEnumerable<CompanionController> query = CompanionController.GetFollowingCompanions(player);
        if(companionMode == SocialCompanionParticipantMode.SpecificRole) {
            query = query.Where(companion => companion != null && companion.RoleDefinition == requiredCompanionRole);
        }

        query = query.Where(companion => companion != null && companion.BondLevel >= minimumCompanionBond);

        var list = companionMode == SocialCompanionParticipantMode.AnyFollowingCompanion
            ? query.Take(Mathf.Max(1, minCompanions)).ToList()
            : query.ToList();

        if(maxCompanions > 0) {
            list = list.Take(maxCompanions).ToList();
        }

        return list;
    }

    List<Pokemon> ResolvePokemon(PlayerController player) {
        if(pokemonMode == SocialPokemonParticipantMode.None) {
            return new List<Pokemon>();
        }

        var party = player.GetComponent<PokemonParty>();
        if(party == null || party.Pokemons == null) {
            return new List<Pokemon>();
        }

        List<Pokemon> selected;
        switch(pokemonMode) {
            case SocialPokemonParticipantMode.FirstHealthyPokemon:
                selected = new List<Pokemon> { party.GetHealthyPokemon() };
                break;
            case SocialPokemonParticipantMode.WholeParty:
                selected = party.Pokemons.ToList();
                break;
            case SocialPokemonParticipantMode.SpecificPartySlot:
                selected = partySlotIndex >= 0 && partySlotIndex < party.Pokemons.Count
                    ? new List<Pokemon> { party.Pokemons[partySlotIndex] }
                    : new List<Pokemon>();
                break;
            default:
                selected = party.Pokemons.Count > 0 ? new List<Pokemon> { party.Pokemons[0] } : new List<Pokemon>();
                break;
        }

        selected.RemoveAll(pokemon => pokemon == null);
        if(requireHealthyPokemon) {
            selected.RemoveAll(pokemon => pokemon.HP <= 0);
        }
        return selected;
    }

    bool ValidateCompanions(IReadOnlyList<CompanionController> companions, out string failureMessage) {
        if(companionMode == SocialCompanionParticipantMode.None) {
            failureMessage = null;
            return true;
        }

        int requiredCount = Mathf.Max(1, minCompanions);
        if(companions == null || companions.Count < requiredCount) {
            failureMessage = BuildCompanionFailureMessage(requiredCount);
            return false;
        }

        failureMessage = null;
        return true;
    }

    bool ValidatePokemon(IReadOnlyList<Pokemon> pokemonList, out string failureMessage) {
        if(pokemonMode == SocialPokemonParticipantMode.None) {
            failureMessage = null;
            return true;
        }

        if(pokemonList == null || pokemonList.Count == 0) {
            failureMessage = requireHealthyPokemon
                ? "No healthy Pokemon is available for this social activity."
                : "No Pokemon is available for this social activity.";
            return false;
        }

        foreach(var pokemon in pokemonList) {
            if(pokemon.Friendship < minimumPokemonFriendship) {
                failureMessage = $"{pokemon.NickName} needs at least {minimumPokemonFriendship} friendship for {DisplayName}.";
                return false;
            }

            foreach(var requirement in pokemonCareRequirements) {
                if(requirement != null && !requirement.IsMet(pokemon, out failureMessage)) {
                    return false;
                }
            }
        }

        failureMessage = null;
        return true;
    }

    void ApplyRewards(PlayerController player, SocialActivityParticipants participants, UnityEngine.Object context) {
        if(baseActivity != null && applyBaseActivityRewards) {
            baseActivity.ApplyRewards(player);
            if(applyBaseRelationshipRewards) {
                baseActivity.ApplyRelationshipRewards(player);
            }
        }

        foreach(var companion in participants.Companions) {
            companion?.AddBond(companionBondGain);
        }

        foreach(var pokemon in participants.Pokemon) {
            if(pokemon == null) {
                continue;
            }

            pokemon.IncreaseFriendship(pokemonFriendshipGain);
            foreach(var moodChange in pokemonMoodChanges) {
                if(moodChange != null) {
                    pokemon.ChangeMood(moodChange.mood, moodChange.amount);
                }
            }

            foreach(var needChange in pokemonCareNeedChanges) {
                if(needChange != null) {
                    pokemon.ChangeCareNeed(needChange.need, needChange.amount);
                }
            }
        }

        player.GetComponent<PlayerRelationships>()?.ApplyChanges(relationshipChanges);
        player.GetComponent<PlayerReputation>()?.ApplyChanges(reputationChanges);
        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);
        player.GetComponent<PlayerTitles>()?.ApplyGrants(titleGrants, context);
        player.GetComponent<PlayerRecipeBook>()?.ApplyGrants(recipeGrants, context);
        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(lifePathRewards, $"social-activity:{Id}", DisplayName, context != null ? context : this);
    }

    void RecordResult(PlayerController player, SocialActivityResult result) {
        if(player == null || result == null) {
            return;
        }

        var log = player.GetComponent<PlayerSocialActivityLog>();
        log?.Record(result);
    }

    void PublishCompletedEvent(PlayerController player, SocialActivityParticipants participants, UnityEngine.Object context, string message) {
        GameEventPublishing.PublishOptional(
            completedEvent,
            $"social.activity.completed.{Id}",
            message,
            GameEventCategory.Activity,
            GameEventImportance.Success,
            context != null ? context : player,
            "SocialActivityDefinition",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: writeToDebugLog,
            GameEventPublishing.Value("socialActivityId", Id),
            GameEventPublishing.Value("socialActivityName", DisplayName),
            GameEventPublishing.Value("kind", kind),
            GameEventPublishing.Value("companionCount", participants.Companions.Count),
            GameEventPublishing.Value("pokemonCount", participants.Pokemon.Count));
    }

    string BuildSuccessMessage(SocialActivityParticipants participants) {
        if(!string.IsNullOrWhiteSpace(successMessage)) {
            return successMessage;
        }

        int companionCount = participants.Companions.Count;
        int pokemonCount = participants.Pokemon.Count;
        if(companionCount > 0 && pokemonCount > 0) {
            return $"{DisplayName} completed with {companionCount} companion(s) and {pokemonCount} Pokemon.";
        }
        if(companionCount > 0) {
            return $"{DisplayName} completed with {companionCount} companion(s).";
        }
        if(pokemonCount > 0) {
            return $"{DisplayName} completed with {pokemonCount} Pokemon.";
        }
        return $"{DisplayName} completed.";
    }

    string BuildCompanionFailureMessage(int requiredCount) {
        if(companionMode == SocialCompanionParticipantMode.SpecificRole && requiredCompanionRole != null) {
            return $"{DisplayName} requires {requiredCount} following companion(s) with the {requiredCompanionRole.DisplayName} role.";
        }

        return $"{DisplayName} requires {requiredCount} following companion(s).";
    }

    public bool HasTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag) || tags == null) {
            return false;
        }

        return tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }
}

public class SocialActivityParticipants {
    public static readonly SocialActivityParticipants Empty = new SocialActivityParticipants(new List<CompanionController>(), new List<Pokemon>());

    public SocialActivityParticipants(IReadOnlyList<CompanionController> companions, IReadOnlyList<Pokemon> pokemon) {
        Companions = companions ?? Array.Empty<CompanionController>();
        Pokemon = pokemon ?? Array.Empty<Pokemon>();
    }

    public IReadOnlyList<CompanionController> Companions { get; }
    public IReadOnlyList<Pokemon> Pokemon { get; }
}

[Serializable]
public class SocialActivityResult {
    [Tooltip("If enabled, the social activity completed and rewards were applied.")]
    public bool success;
    [Tooltip("Social activity id used for save/history records.")]
    public string activityId;
    [Tooltip("Social activity display name saved for UI/debug fallback.")]
    public string activityName;
    [Tooltip("Broad social activity type saved for filters and history UI.")]
    public SocialActivityKind kind;
    [Tooltip("Scene/source id that triggered this social activity.")]
    public string sourceId;
    [Tooltip("Result message produced by the activity.")]
    public string message;
    [Tooltip("In-game day when the result was produced.")]
    public int day;
    [Tooltip("In-game hour when the result was produced.")]
    public int hour;
    [Tooltip("Absolute in-game hour when the result was produced.")]
    public int absoluteHour;
    [Tooltip("Companions included in this social activity.")]
    public List<SocialActivityParticipantRecord> companions = new List<SocialActivityParticipantRecord>();
    [Tooltip("Pokemon included in this social activity.")]
    public List<SocialActivityParticipantRecord> pokemon = new List<SocialActivityParticipantRecord>();

    public static SocialActivityResult Succeeded(SocialActivityDefinition definition, string sourceId, string message, SocialActivityParticipants participants) {
        var result = Create(definition, sourceId, message, true);
        if(participants != null) {
            result.companions = participants.Companions.Select(SocialActivityParticipantRecord.FromCompanion).Where(record => record != null).ToList();
            result.pokemon = participants.Pokemon.Select(SocialActivityParticipantRecord.FromPokemon).Where(record => record != null).ToList();
        }
        return result;
    }

    public static SocialActivityResult Failed(SocialActivityDefinition definition, string sourceId, string message) {
        return Create(definition, sourceId, message, false);
    }

    static SocialActivityResult Create(SocialActivityDefinition definition, string sourceId, string message, bool success) {
        return new SocialActivityResult {
            success = success,
            activityId = definition != null ? definition.Id : null,
            activityName = definition != null ? definition.DisplayName : string.Empty,
            kind = definition != null ? definition.Kind : SocialActivityKind.Custom,
            sourceId = sourceId,
            message = message,
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0,
            absoluteHour = TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0
        };
    }
}

[Serializable]
public class SocialActivityParticipantRecord {
    [Tooltip("Stable participant id, such as companion id or Pokemon instance id.")]
    public string id;
    [Tooltip("Display name saved for fallback/debug output.")]
    public string displayName;
    [Tooltip("Extra participant detail, such as role or species name.")]
    public string detail;

    public static SocialActivityParticipantRecord FromCompanion(CompanionController companion) {
        if(companion == null) {
            return null;
        }

        return new SocialActivityParticipantRecord {
            id = companion.CompanionId,
            displayName = companion.CompanionName,
            detail = companion.RoleDefinition != null ? companion.RoleDefinition.DisplayName : string.Empty
        };
    }

    public static SocialActivityParticipantRecord FromPokemon(Pokemon pokemon) {
        if(pokemon == null) {
            return null;
        }

        return new SocialActivityParticipantRecord {
            id = pokemon.InstanceId,
            displayName = pokemon.NickName,
            detail = pokemon.Base != null ? pokemon.Base.Name : string.Empty
        };
    }
}
