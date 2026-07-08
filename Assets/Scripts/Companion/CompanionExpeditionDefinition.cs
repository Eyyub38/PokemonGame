using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CompanionExpeditionCategory {
    General,
    Foraging,
    Research,
    Delivery,
    Patrol,
    Social,
    Training,
    PokemonCare,
    Custom
}

public enum CompanionExpeditionRepeatMode {
    Once,
    Repeatable,
    Daily,
    CooldownHours
}

[CreateAssetMenu(menuName = "Companion/Expedition Definition")]
public class CompanionExpeditionDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this companion expedition. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this expedition.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad expedition category used by future UI filters.")]
    [SerializeField] CompanionExpeditionCategory category = CompanionExpeditionCategory.General;
    [Tooltip("Free-form tags used by validators and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Priority used by future UI sorting. Higher priority appears first.")]
    [SerializeField] int priority;

    [Header("Activities")]
    [Tooltip("Optional activity checked and paid when the expedition starts.")]
    [SerializeField] ActivityDefinition startActivity;
    [Tooltip("Optional activity checked, paid and rewarded when the expedition is claimed.")]
    [SerializeField] ActivityDefinition claimActivity;

    [Header("Repeat Rules")]
    [Tooltip("How often this expedition can be completed.")]
    [SerializeField] CompanionExpeditionRepeatMode repeatMode = CompanionExpeditionRepeatMode.Repeatable;
    [Tooltip("Cooldown in in-game hours when Repeat Mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours;
    [Tooltip("If enabled, the same expedition cannot be started while already active from the same source.")]
    [SerializeField] bool blockDuplicateActiveExpedition = true;
    [Tooltip("If enabled, a companion cannot start another expedition while already active in any expedition.")]
    [SerializeField] bool blockBusyCompanion = true;

    [Header("Companion Requirements")]
    [Tooltip("Optional role required before this expedition can start.")]
    [SerializeField] CompanionRoleDefinition requiredRole;
    [Tooltip("Optional active perk required before this expedition can start.")]
    [SerializeField] CompanionPerkDefinition requiredPerk;
    [Tooltip("Minimum companion bond level required before this expedition can start.")]
    [SerializeField] CompanionBondLevel minimumBondLevel = CompanionBondLevel.Stranger;
    [Tooltip("Minimum raw bond points required before this expedition can start.")]
    [Min(0)]
    [SerializeField] int minimumBondPoints;
    [Tooltip("Additional requirements checked before this expedition can start.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message shown when access rules block this expedition.")]
    [SerializeField] string lockedMessage = "This companion expedition is not available yet.";

    [Header("Timing")]
    [Tooltip("How many in-game hours must pass before this expedition can be claimed.")]
    [Min(0)]
    [SerializeField] int durationHours = 6;
    [Tooltip("If enabled, starting this expedition stops the companion from following the player.")]
    [SerializeField] bool stopFollowingOnStart = true;
    [Tooltip("If enabled, claiming this expedition starts the companion following the player again when the companion object is available.")]
    [SerializeField] bool resumeFollowingOnClaim = true;

    [Header("Success")]
    [Tooltip("Base chance for a successful result.")]
    [Range(0f, 1f)]
    [SerializeField] float baseSuccessChance = 0.65f;
    [Tooltip("Success chance added for each bond level above Stranger.")]
    [Range(0f, 1f)]
    [SerializeField] float bondLevelChanceBonus = 0.05f;
    [Tooltip("Success chance added for each 100 bond points.")]
    [Range(0f, 1f)]
    [SerializeField] float bondPointChancePer100 = 0.01f;
    [Tooltip("Optional success chance modifiers based on companion role, perk or bond.")]
    [SerializeField] List<CompanionExpeditionSuccessModifier> successModifiers = new List<CompanionExpeditionSuccessModifier>();

    [Header("Bond Rewards")]
    [Tooltip("Bond points granted when the expedition starts.")]
    [Min(0)]
    [SerializeField] int startBondReward;
    [Tooltip("Bond points granted when the expedition succeeds.")]
    [Min(0)]
    [SerializeField] int successBondReward = 5;
    [Tooltip("Bond points granted when the expedition fails.")]
    [Min(0)]
    [SerializeField] int failureBondReward = 1;

    [Header("Outcomes")]
    [Tooltip("Outcomes rolled when the expedition succeeds.")]
    [SerializeField] List<ActivityOutcomeDefinition> successOutcomes = new List<ActivityOutcomeDefinition>();
    [Tooltip("Outcomes rolled when the expedition fails.")]
    [SerializeField] List<ActivityOutcomeDefinition> failureOutcomes = new List<ActivityOutcomeDefinition>();

    [Header("Events")]
    [Tooltip("Optional event published when the expedition starts. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition startedEvent;
    [Tooltip("Optional event published when the expedition succeeds. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition succeededEvent;
    [Tooltip("Optional event published when the expedition fails. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition failedEvent;
    [Tooltip("If enabled, expedition events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, expedition events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public CompanionExpeditionCategory Category => category;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : System.Array.Empty<string>();
    public int Priority => priority;
    public ActivityDefinition StartActivity => startActivity;
    public ActivityDefinition ClaimActivity => claimActivity;
    public CompanionExpeditionRepeatMode RepeatMode => repeatMode;
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public bool BlockDuplicateActiveExpedition => blockDuplicateActiveExpedition;
    public bool BlockBusyCompanion => blockBusyCompanion;
    public CompanionRoleDefinition RequiredRole => requiredRole;
    public CompanionPerkDefinition RequiredPerk => requiredPerk;
    public CompanionBondLevel MinimumBondLevel => minimumBondLevel;
    public int MinimumBondPoints => Mathf.Max(0, minimumBondPoints);
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : System.Array.Empty<ActivityRequirement>();
    public int DurationHours => Mathf.Max(0, durationHours);
    public bool StopFollowingOnStart => stopFollowingOnStart;
    public bool ResumeFollowingOnClaim => resumeFollowingOnClaim;
    public float BaseSuccessChance => Mathf.Clamp01(baseSuccessChance);
    public float BondLevelChanceBonus => bondLevelChanceBonus;
    public float BondPointChancePer100 => bondPointChancePer100;
    public IReadOnlyList<CompanionExpeditionSuccessModifier> SuccessModifiers => successModifiers != null ? (IReadOnlyList<CompanionExpeditionSuccessModifier>)successModifiers : System.Array.Empty<CompanionExpeditionSuccessModifier>();
    public int StartBondReward => startBondReward;
    public int SuccessBondReward => successBondReward;
    public int FailureBondReward => failureBondReward;
    public IReadOnlyList<ActivityOutcomeDefinition> SuccessOutcomes => successOutcomes != null ? (IReadOnlyList<ActivityOutcomeDefinition>)successOutcomes : System.Array.Empty<ActivityOutcomeDefinition>();
    public IReadOnlyList<ActivityOutcomeDefinition> FailureOutcomes => failureOutcomes != null ? (IReadOnlyList<ActivityOutcomeDefinition>)failureOutcomes : System.Array.Empty<ActivityOutcomeDefinition>();

    public bool CanStart(PlayerController player, CompanionController companion, PlayerCompanionExpeditionLog log, string sourceId, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to start companion expeditions.";
            return false;
        }

        if(companion == null) {
            failureMessage = "No companion selected.";
            return false;
        }

        if(blockBusyCompanion && log != null && log.HasActiveExpeditionForCompanion(companion.CompanionId)) {
            failureMessage = $"{companion.CompanionName} is already on an expedition.";
            return false;
        }

        if(blockDuplicateActiveExpedition && log != null && log.HasActiveExpedition(this, sourceId)) {
            failureMessage = $"{DisplayName} is already active.";
            return false;
        }

        if(log != null && !log.CanStart(this, sourceId, repeatMode, CooldownHours, out failureMessage)) {
            return false;
        }

        if(requiredRole != null && companion.RoleDefinition != requiredRole) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} requires {requiredRole.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredPerk != null && !companion.HasActivePerk(requiredPerk)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} requires {requiredPerk.DisplayName}." : lockedMessage;
            return false;
        }

        if(companion.BondLevel < minimumBondLevel || companion.BondPoints < MinimumBondPoints) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{companion.CompanionName} needs a stronger bond first." : lockedMessage;
            return false;
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

    public bool CanClaim(PlayerController player, PlayerCompanionExpeditionState state, out string failureMessage) {
        if(state == null) {
            failureMessage = "No companion expedition selected.";
            return false;
        }

        if(!state.IsReady()) {
            failureMessage = $"{state.expeditionName} is not ready yet.";
            return false;
        }

        if(claimActivity != null && !claimActivity.CanPerform(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public float GetSuccessChance(CompanionController companion) {
        if(companion == null) {
            return BaseSuccessChance;
        }

        float chance = BaseSuccessChance
            + Mathf.Max(0, (int)companion.BondLevel) * bondLevelChanceBonus
            + Mathf.FloorToInt(companion.BondPoints / 100f) * bondPointChancePer100;

        foreach(var modifier in SuccessModifiers) {
            if(modifier != null && modifier.AppliesTo(companion)) {
                chance += modifier.chanceBonus;
            }
        }

        return Mathf.Clamp01(chance);
    }

    public void ApplyStarted(PlayerController player, CompanionController companion, string sourceId) {
        companion?.AddBond(startBondReward);
        startActivity?.ApplyRewards(player);
        PublishEvent(startedEvent, "started", GameEventImportance.Info, player, companion, sourceId, false, 0f);
    }

    public void ApplyClaimed(PlayerController player, CompanionController companion, string sourceId, bool success, float successChance) {
        if(success) {
            companion?.AddBond(successBondReward);
            foreach(var outcome in SuccessOutcomes) {
                outcome?.TryApply(player);
            }
        } else {
            companion?.AddBond(failureBondReward);
            foreach(var outcome in FailureOutcomes) {
                outcome?.TryApply(player);
            }
        }

        claimActivity?.ApplyRewards(player);
        PublishEvent(success ? succeededEvent : failedEvent, success ? "succeeded" : "failed", success ? GameEventImportance.Success : GameEventImportance.Warning, player, companion, sourceId, success, successChance);
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, System.StringComparison.OrdinalIgnoreCase));
    }

    void PublishEvent(GameEventDefinition eventDefinition, string phase, GameEventImportance importance, PlayerController player, CompanionController companion, string sourceId, bool success, float successChance) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"companion.expedition.{phase}.{Id}.{companion?.CompanionId}",
            $"{companion?.CompanionName ?? "Companion"} {phase} {DisplayName}.",
            GameEventCategory.Companion,
            importance,
            player != null ? player : companion,
            "CompanionExpeditionDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("expeditionId", Id),
            GameEventPublishing.Value("expeditionName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("companionId", companion != null ? companion.CompanionId : null),
            GameEventPublishing.Value("companionName", companion != null ? companion.CompanionName : null),
            GameEventPublishing.Value("success", success),
            GameEventPublishing.Value("successChance", successChance));
    }
}

[System.Serializable]
public class CompanionExpeditionSuccessModifier {
    [Tooltip("Optional role required for this success modifier.")]
    public CompanionRoleDefinition role;
    [Tooltip("Optional active perk required for this success modifier.")]
    public CompanionPerkDefinition perk;
    [Tooltip("Minimum bond level required for this modifier.")]
    public CompanionBondLevel minimumBondLevel = CompanionBondLevel.Stranger;
    [Tooltip("Minimum raw bond points required for this modifier.")]
    [Min(0)]
    public int minimumBondPoints;
    [Tooltip("Success chance added when this modifier applies. Negative values reduce success chance.")]
    public float chanceBonus;

    public bool AppliesTo(CompanionController companion) {
        if(companion == null) {
            return false;
        }

        if(role != null && companion.RoleDefinition != role) {
            return false;
        }

        if(perk != null && !companion.HasActivePerk(perk)) {
            return false;
        }

        return companion.BondLevel >= minimumBondLevel && companion.BondPoints >= Mathf.Max(0, minimumBondPoints);
    }
}
