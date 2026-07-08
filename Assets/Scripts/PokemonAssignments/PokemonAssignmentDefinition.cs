using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokemonAssignmentCategory {
    General,
    Camp,
    Farm,
    Ranch,
    Research,
    Guard,
    Scout,
    Delivery,
    Mining,
    Fishing,
    PokemonCare,
    CompanionSupport,
    Custom
}

public enum PokemonAssignmentRepeatMode {
    Once,
    Repeatable,
    Daily,
    CooldownHours
}

[CreateAssetMenu(menuName = "Pokemon Assignments/Pokemon Assignment Definition")]
public class PokemonAssignmentDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this Pokemon assignment. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation of this assignment.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad category used by filters, future UI and balancing.")]
    [SerializeField] PokemonAssignmentCategory category = PokemonAssignmentCategory.General;
    [Tooltip("Higher priority assignments can be sorted first by future UI.")]
    [SerializeField] int priority;
    [Tooltip("Free-form tags such as farm, camp, ranch, lab, guard, scout or delivery.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Optional icon used by future assignment UI.")]
    [SerializeField] Sprite icon = null;

    [Header("Activities")]
    [Tooltip("Optional activity checked and paid when the assignment starts.")]
    [SerializeField] ActivityDefinition startActivity = null;
    [Tooltip("Optional activity checked, paid and rewarded when the assignment is claimed.")]
    [SerializeField] ActivityDefinition claimActivity = null;

    [Header("Repeat Rules")]
    [Tooltip("How often this Pokemon assignment can be completed.")]
    [SerializeField] PokemonAssignmentRepeatMode repeatMode = PokemonAssignmentRepeatMode.Repeatable;
    [Tooltip("Cooldown in in-game hours when Repeat Mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours;
    [Tooltip("Maximum successful claim count for this assignment/source. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxClaimCount;
    [Tooltip("If enabled, the same assignment cannot start again while it is already active from the same source.")]
    [SerializeField] bool blockDuplicateActiveAssignment = true;
    [Tooltip("If enabled, the same Pokemon cannot be sent to more than one active assignment.")]
    [SerializeField] bool blockBusyPokemon = true;

    [Header("Location Rules")]
    [Tooltip("If enabled, the assignment must be started from a valid Activity Zone.")]
    [SerializeField] bool requiresActivityZone = true;
    [Tooltip("Specific zones that can start this assignment. Empty means any active zone is accepted when no tag/type filters are used.")]
    [SerializeField] List<ActivityZoneDefinition> allowedZones = new List<ActivityZoneDefinition>();
    [Tooltip("Zone types that can start this assignment. Empty means type is not checked.")]
    [SerializeField] List<ActivityZoneType> allowedZoneTypes = new List<ActivityZoneType>();
    [Tooltip("Zone tags that can start this assignment. Empty means tags are not checked.")]
    [SerializeField] List<string> allowedZoneTags = new List<string>();
    [Tooltip("Message shown when location rules block this assignment.")]
    [SerializeField] string locationLockedMessage = "This Pokemon assignment needs a suitable activity area.";

    [Header("Pokemon Requirements")]
    [Tooltip("Minimum Pokemon level required to start this assignment.")]
    [Min(1)]
    [SerializeField] int minimumLevel = 1;
    [Tooltip("If enabled, fainted Pokemon cannot start this assignment.")]
    [SerializeField] bool requireHealthyPokemon = true;
    [Tooltip("Minimum friendship required to start this assignment.")]
    [Range(0, 255)]
    [SerializeField] int minimumFriendship;
    [Tooltip("Allowed Pokemon types. Empty accepts every type unless blocked by Banned Types.")]
    [SerializeField] List<PokemonType> allowedTypes = new List<PokemonType>();
    [Tooltip("Pokemon types that cannot start this assignment.")]
    [SerializeField] List<PokemonType> bannedTypes = new List<PokemonType>();
    [Tooltip("Optional mood checked before this assignment can start.")]
    [SerializeField] PokemonMoodDefinition requiredMood = null;
    [Tooltip("Minimum value for Required Mood.")]
    [Range(0, 100)]
    [SerializeField] int minimumMoodValue;
    [Tooltip("Additional reusable requirements checked before this assignment can start.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message shown when Pokemon rules block this assignment.")]
    [SerializeField] string pokemonLockedMessage = "This Pokemon is not suitable for the assignment.";

    [Header("Timing")]
    [Tooltip("How many in-game hours must pass before this assignment can be claimed.")]
    [Min(0)]
    [SerializeField] int durationHours = 4;
    [Tooltip("If enabled, future controllers can auto-claim this assignment when it becomes ready.")]
    [SerializeField] bool allowAutoClaim;

    [Header("Success")]
    [Tooltip("Base chance for a successful claim.")]
    [Range(0f, 1f)]
    [SerializeField] float baseSuccessChance = 0.75f;
    [Tooltip("Success chance added for each 10 levels the Pokemon has.")]
    [Range(0f, 1f)]
    [SerializeField] float levelChanceBonusPer10 = 0.02f;
    [Tooltip("Success chance added for each 100 friendship points.")]
    [Range(0f, 1f)]
    [SerializeField] float friendshipChanceBonusPer100 = 0.03f;
    [Tooltip("Success chance added for each 100 points of Required Mood. Only used when Required Mood is assigned.")]
    [Range(0f, 1f)]
    [SerializeField] float moodChanceBonusPer100 = 0.02f;

    [Header("Pokemon Effects")]
    [Tooltip("Friendship added when the assignment starts. Negative values reduce friendship.")]
    [SerializeField] int startFriendshipChange;
    [Tooltip("Friendship added when the assignment succeeds. Negative values reduce friendship.")]
    [SerializeField] int successFriendshipChange = 3;
    [Tooltip("Friendship added when the assignment fails. Negative values reduce friendship.")]
    [SerializeField] int failureFriendshipChange = 1;
    [Tooltip("Mood changes applied when the assignment starts.")]
    [SerializeField] List<PokemonMoodChange> startMoodChanges = new List<PokemonMoodChange>();
    [Tooltip("Mood changes applied when the assignment succeeds.")]
    [SerializeField] List<PokemonMoodChange> successMoodChanges = new List<PokemonMoodChange>();
    [Tooltip("Mood changes applied when the assignment fails.")]
    [SerializeField] List<PokemonMoodChange> failureMoodChanges = new List<PokemonMoodChange>();

    [Header("Rewards")]
    [Tooltip("Activity outcomes rolled when this assignment succeeds.")]
    [SerializeField] List<ActivityOutcomeDefinition> successOutcomes = new List<ActivityOutcomeDefinition>();
    [Tooltip("Activity outcomes rolled when this assignment fails.")]
    [SerializeField] List<ActivityOutcomeDefinition> failureOutcomes = new List<ActivityOutcomeDefinition>();
    [Tooltip("Life Path rewards awarded when this assignment succeeds.")]
    [SerializeField] List<LifePathReward> successLifePathRewards = new List<LifePathReward>();
    [Tooltip("Life Path rewards awarded when this assignment fails.")]
    [SerializeField] List<LifePathReward> failureLifePathRewards = new List<LifePathReward>();
    [Tooltip("Career points awarded when this assignment succeeds.")]
    [SerializeField] List<CareerPointGrant> successCareerPointRewards = new List<CareerPointGrant>();
    [Tooltip("Career points awarded when this assignment fails.")]
    [SerializeField] List<CareerPointGrant> failureCareerPointRewards = new List<CareerPointGrant>();
    [Tooltip("Consequence chains applied when this assignment succeeds.")]
    [SerializeField] List<ConsequenceChainDefinition> successConsequenceChains = new List<ConsequenceChainDefinition>();
    [Tooltip("Consequence chains applied when this assignment fails.")]
    [SerializeField] List<ConsequenceChainDefinition> failureConsequenceChains = new List<ConsequenceChainDefinition>();

    [Header("Events")]
    [Tooltip("Optional event published when the assignment starts. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition startedEvent = null;
    [Tooltip("Optional event published when the assignment succeeds. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition succeededEvent = null;
    [Tooltip("Optional event published when the assignment fails. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition failedEvent = null;
    [Tooltip("Optional event published when the assignment is blocked. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition blockedEvent = null;
    [Tooltip("If enabled, assignment events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, assignment events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public PokemonAssignmentCategory Category => category;
    public int Priority => priority;
    public IReadOnlyList<string> Tags => tags != null ? tags : Array.Empty<string>();
    public Sprite Icon => icon;
    public ActivityDefinition StartActivity => startActivity;
    public ActivityDefinition ClaimActivity => claimActivity;
    public PokemonAssignmentRepeatMode RepeatMode => repeatMode;
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public int MaxClaimCount => Mathf.Max(0, maxClaimCount);
    public bool BlockDuplicateActiveAssignment => blockDuplicateActiveAssignment;
    public bool BlockBusyPokemon => blockBusyPokemon;
    public bool RequiresActivityZone => requiresActivityZone;
    public IReadOnlyList<ActivityZoneDefinition> AllowedZones => allowedZones != null ? allowedZones : Array.Empty<ActivityZoneDefinition>();
    public IReadOnlyList<ActivityZoneType> AllowedZoneTypes => allowedZoneTypes != null ? allowedZoneTypes : Array.Empty<ActivityZoneType>();
    public IReadOnlyList<string> AllowedZoneTags => allowedZoneTags != null ? allowedZoneTags : Array.Empty<string>();
    public int MinimumLevel => Mathf.Max(1, minimumLevel);
    public bool RequireHealthyPokemon => requireHealthyPokemon;
    public int MinimumFriendship => Mathf.Clamp(minimumFriendship, 0, 255);
    public IReadOnlyList<PokemonType> AllowedTypes => allowedTypes != null ? allowedTypes : Array.Empty<PokemonType>();
    public IReadOnlyList<PokemonType> BannedTypes => bannedTypes != null ? bannedTypes : Array.Empty<PokemonType>();
    public PokemonMoodDefinition RequiredMood => requiredMood;
    public int MinimumMoodValue => Mathf.Max(0, minimumMoodValue);
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? requirements : Array.Empty<ActivityRequirement>();
    public int DurationHours => Mathf.Max(0, durationHours);
    public bool AllowAutoClaim => allowAutoClaim;
    public float BaseSuccessChance => Mathf.Clamp01(baseSuccessChance);
    public IReadOnlyList<PokemonMoodChange> StartMoodChanges => startMoodChanges != null ? startMoodChanges : Array.Empty<PokemonMoodChange>();
    public IReadOnlyList<PokemonMoodChange> SuccessMoodChanges => successMoodChanges != null ? successMoodChanges : Array.Empty<PokemonMoodChange>();
    public IReadOnlyList<PokemonMoodChange> FailureMoodChanges => failureMoodChanges != null ? failureMoodChanges : Array.Empty<PokemonMoodChange>();
    public IReadOnlyList<ActivityOutcomeDefinition> SuccessOutcomes => successOutcomes != null ? successOutcomes : Array.Empty<ActivityOutcomeDefinition>();
    public IReadOnlyList<ActivityOutcomeDefinition> FailureOutcomes => failureOutcomes != null ? failureOutcomes : Array.Empty<ActivityOutcomeDefinition>();
    public IReadOnlyList<LifePathReward> SuccessLifePathRewards => successLifePathRewards != null ? successLifePathRewards : Array.Empty<LifePathReward>();
    public IReadOnlyList<LifePathReward> FailureLifePathRewards => failureLifePathRewards != null ? failureLifePathRewards : Array.Empty<LifePathReward>();
    public IReadOnlyList<CareerPointGrant> SuccessCareerPointRewards => successCareerPointRewards != null ? successCareerPointRewards : Array.Empty<CareerPointGrant>();
    public IReadOnlyList<CareerPointGrant> FailureCareerPointRewards => failureCareerPointRewards != null ? failureCareerPointRewards : Array.Empty<CareerPointGrant>();
    public IReadOnlyList<ConsequenceChainDefinition> SuccessConsequenceChains => successConsequenceChains != null ? successConsequenceChains : Array.Empty<ConsequenceChainDefinition>();
    public IReadOnlyList<ConsequenceChainDefinition> FailureConsequenceChains => failureConsequenceChains != null ? failureConsequenceChains : Array.Empty<ConsequenceChainDefinition>();
    public GameEventDefinition BlockedEvent => blockedEvent;

    public bool CanStart(PlayerController player, Pokemon pokemon, PlayerPokemonAssignmentLog log, ActivityZoneDefinition zone, string sourceId, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to start Pokemon assignments.";
            return false;
        }

        if(pokemon == null) {
            failureMessage = "No Pokemon selected for this assignment.";
            return false;
        }

        if(blockDuplicateActiveAssignment && log != null && log.HasActiveAssignment(this, sourceId)) {
            failureMessage = $"{DisplayName} is already active.";
            return false;
        }

        if(log != null && !log.CanStart(this, sourceId, repeatMode, CooldownHours, MaxClaimCount, out failureMessage)) {
            return false;
        }

        if(!MatchesZone(zone, out failureMessage)) {
            return false;
        }

        if(!PokemonCanWork(pokemon, out failureMessage)) {
            return false;
        }

        if(blockBusyPokemon && log != null) {
            var party = player.GetComponent<PokemonParty>();
            string pokemonKey = log.BuildPokemonKey(pokemon, party);
            if(log.HasActiveAssignmentForPokemon(pokemonKey)) {
                failureMessage = $"{pokemon.NickName} is already assigned to another task.";
                return false;
            }
        }

        foreach(var requirement in Requirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        if(startActivity != null && !startActivity.CanPerform(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool CanClaim(PlayerController player, PlayerPokemonAssignmentState state, out string failureMessage) {
        if(state == null) {
            failureMessage = "No Pokemon assignment selected.";
            return false;
        }

        if(!state.IsReady()) {
            failureMessage = $"{state.assignmentName} is not ready yet.";
            return false;
        }

        if(claimActivity != null && !claimActivity.CanPerform(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool PokemonCanWork(Pokemon pokemon, out string failureMessage) {
        if(pokemon == null) {
            failureMessage = "No Pokemon selected.";
            return false;
        }

        if(requireHealthyPokemon && pokemon.HP <= 0) {
            failureMessage = $"{pokemon.NickName} is not healthy enough for {DisplayName}.";
            return false;
        }

        if(pokemon.Level < MinimumLevel) {
            failureMessage = string.IsNullOrWhiteSpace(pokemonLockedMessage) ? $"{pokemon.NickName} needs to be level {MinimumLevel}." : pokemonLockedMessage;
            return false;
        }

        if(pokemon.Friendship < MinimumFriendship) {
            failureMessage = string.IsNullOrWhiteSpace(pokemonLockedMessage) ? $"{pokemon.NickName} needs more trust first." : pokemonLockedMessage;
            return false;
        }

        if(allowedTypes != null && allowedTypes.Count > 0 && !allowedTypes.Any(pokemon.HasType)) {
            failureMessage = string.IsNullOrWhiteSpace(pokemonLockedMessage) ? $"{pokemon.NickName}'s type does not fit {DisplayName}." : pokemonLockedMessage;
            return false;
        }

        if(bannedTypes != null && bannedTypes.Count > 0 && bannedTypes.Any(pokemon.HasType)) {
            failureMessage = string.IsNullOrWhiteSpace(pokemonLockedMessage) ? $"{pokemon.NickName}'s type cannot do {DisplayName}." : pokemonLockedMessage;
            return false;
        }

        if(requiredMood != null && pokemon.GetMoodValue(requiredMood) < MinimumMoodValue) {
            failureMessage = string.IsNullOrWhiteSpace(pokemonLockedMessage) ? $"{pokemon.NickName} does not feel ready for {DisplayName}." : pokemonLockedMessage;
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool MatchesZone(ActivityZoneDefinition zone, out string failureMessage) {
        if(!requiresActivityZone) {
            failureMessage = null;
            return true;
        }

        if(zone == null) {
            failureMessage = string.IsNullOrWhiteSpace(locationLockedMessage) ? "A valid activity zone is required." : locationLockedMessage;
            return false;
        }

        bool hasFilters = AllowedZones.Count > 0 || AllowedZoneTypes.Count > 0 || AllowedZoneTags.Count > 0;
        bool matches = !hasFilters
            || AllowedZones.Contains(zone)
            || AllowedZoneTypes.Contains(zone.ZoneType)
            || AllowedZoneTags.Any(zone.HasTag);

        failureMessage = matches ? null : (string.IsNullOrWhiteSpace(locationLockedMessage) ? $"{DisplayName} cannot be started here." : locationLockedMessage);
        return matches;
    }

    public float GetSuccessChance(Pokemon pokemon) {
        if(pokemon == null) {
            return BaseSuccessChance;
        }

        float chance = BaseSuccessChance
            + Mathf.FloorToInt(pokemon.Level / 10f) * levelChanceBonusPer10
            + Mathf.FloorToInt(pokemon.Friendship / 100f) * friendshipChanceBonusPer100;

        if(requiredMood != null) {
            chance += Mathf.FloorToInt(pokemon.GetMoodValue(requiredMood) / 100f) * moodChanceBonusPer100;
        }

        return Mathf.Clamp01(chance);
    }

    public void ApplyStarted(PlayerController player, Pokemon pokemon, string sourceId, UnityEngine.Object context = null) {
        ApplyPokemonEffects(pokemon, startFriendshipChange, StartMoodChanges);
        startActivity?.ApplyRewards(player);
        PublishEvent(startedEvent, "started", GameEventImportance.Info, player, pokemon, sourceId, false, GetSuccessChance(pokemon), context);
    }

    public void ApplyClaimed(PlayerController player, Pokemon pokemon, string sourceId, bool success, float successChance, ActivityZoneDefinition zone = null, UnityEngine.Object context = null) {
        ApplyPokemonEffects(pokemon, success ? successFriendshipChange : failureFriendshipChange, success ? SuccessMoodChanges : FailureMoodChanges);

        foreach(var outcome in success ? SuccessOutcomes : FailureOutcomes) {
            outcome?.TryApply(player);
        }

        player?.GetComponent<PlayerLifePathLog>()?.ApplyRewards(
            success ? SuccessLifePathRewards : FailureLifePathRewards,
            $"pokemon-assignment:{Id}",
            DisplayName,
            context != null ? context : this);

        player?.GetComponent<PlayerCareerLog>()?.ApplyPointGrants(
            success ? SuccessCareerPointRewards : FailureCareerPointRewards,
            $"pokemon-assignment:{Id}");

        claimActivity?.ApplyRewards(player);
        ApplyConsequenceChains(player, success ? SuccessConsequenceChains : FailureConsequenceChains, sourceId, zone, context);
        PublishEvent(success ? succeededEvent : failedEvent, success ? "succeeded" : "failed", success ? GameEventImportance.Success : GameEventImportance.Warning, player, pokemon, sourceId, success, successChance, context);
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    void ApplyPokemonEffects(Pokemon pokemon, int friendshipChange, IReadOnlyList<PokemonMoodChange> moodChanges) {
        if(pokemon == null) {
            return;
        }

        if(friendshipChange > 0) {
            pokemon.IncreaseFriendship(friendshipChange);
        } else if(friendshipChange < 0) {
            pokemon.Friendship = Mathf.Max(0, pokemon.Friendship + friendshipChange);
        }

        foreach(var change in moodChanges) {
            if(change != null && change.mood != null && change.amount != 0) {
                pokemon.ChangeMood(change.mood, change.amount);
            }
        }
    }

    void ApplyConsequenceChains(PlayerController player, IReadOnlyList<ConsequenceChainDefinition> chains, string sourceId, ActivityZoneDefinition zone, UnityEngine.Object context) {
        if(player == null) {
            return;
        }

        var chainContext = new ConsequenceChainContext {
            SourceId = string.IsNullOrWhiteSpace(sourceId) ? $"pokemon-assignment:{Id}" : sourceId,
            SourceName = DisplayName,
            Zone = zone,
            ContextObject = context != null ? context : this
        };

        foreach(var chain in chains) {
            chain?.Apply(player, chainContext, context != null ? context : this);
        }
    }

    void PublishEvent(GameEventDefinition eventDefinition, string phase, GameEventImportance importance, PlayerController player, Pokemon pokemon, string sourceId, bool success, float successChance, UnityEngine.Object context) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"pokemon-assignment.{phase}.{Id}.{pokemon?.Base?.Name}",
            $"{pokemon?.NickName ?? "Pokemon"} {phase} {DisplayName}.",
            GameEventCategory.PokemonCare,
            importance,
            context != null ? context : player != null ? (UnityEngine.Object)player : this,
            "PokemonAssignmentDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("assignmentId", Id),
            GameEventPublishing.Value("assignmentName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("pokemonName", pokemon != null ? pokemon.NickName : string.Empty),
            GameEventPublishing.Value("pokemonBase", pokemon != null && pokemon.Base != null ? pokemon.Base.Name : string.Empty),
            GameEventPublishing.Value("success", success),
            GameEventPublishing.Value("successChance", successChance));
    }
}
