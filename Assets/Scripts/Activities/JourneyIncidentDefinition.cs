using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum JourneyIncidentCategory {
    General,
    Route,
    Camp,
    Rescue,
    Sighting,
    Weather,
    Ranger,
    Research,
    Law,
    Social,
    Resource,
    Transit,
    Custom
}

public enum JourneyIncidentSeverity {
    Info,
    Minor,
    Moderate,
    Major,
    Critical
}

public enum JourneyIncidentPhase {
    Activated,
    Resolved,
    Expired,
    Blocked
}

[CreateAssetMenu(menuName = "Activities/Journey Incident")]
public class JourneyIncidentDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this journey incident. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future UI, logs and debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation of this journey incident.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad incident category used by future filters and balancing.")]
    [SerializeField] JourneyIncidentCategory category = JourneyIncidentCategory.General;
    [Tooltip("How serious this incident is for UI color, PokeNav priority and future balancing.")]
    [SerializeField] JourneyIncidentSeverity severity = JourneyIncidentSeverity.Minor;
    [Tooltip("Higher priority incidents can be sorted first by future UI or board selection.")]
    [SerializeField] int priority;
    [Tooltip("Free-form tags such as route, camp, rescue, ranger, rare, social or storm.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Optional icon used by future map, PokeNav or board UI.")]
    [SerializeField] Sprite icon = null;

    [Header("Access")]
    [Tooltip("Optional reusable access profile checked before this incident can activate.")]
    [SerializeField] AccessProfileDefinition accessProfile = null;
    [Tooltip("Additional requirements checked before this incident can activate.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("How additional requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Message shown when this incident is locked and no more specific failure exists.")]
    [SerializeField] string lockedMessage = "This journey incident is not available right now.";
    [Tooltip("If enabled, access profile checks are published to access logs/events when this incident is attempted.")]
    [SerializeField] bool publishAccessChecks = true;

    [Header("Availability")]
    [Tooltip("If disabled, another active instance of this incident blocks new activations in the same region/zone context.")]
    [SerializeField] bool allowMultipleActiveInstances;
    [Tooltip("Chance that this incident activates after all filters pass.")]
    [Range(0f, 1f)]
    [SerializeField] float activationChance = 1f;
    [Tooltip("Base weight used by journey incident boards. 0 keeps the incident selectable only when a board entry overrides the weight.")]
    [Min(0)]
    [SerializeField] int baseWeight = 1;
    [Tooltip("How often this incident can activate.")]
    [SerializeField] ConsequenceChainRepeatMode repeatMode = ConsequenceChainRepeatMode.CooldownHours;
    [Tooltip("Cooldown in in-game hours when repeat mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours = 12;
    [Tooltip("Maximum successful activation count. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxActivationCount;
    [Tooltip("If enabled, blocked activation attempts are stored in PlayerJourneyIncidentLog.")]
    [SerializeField] bool recordBlockedAttempts;

    [Header("Location Scope")]
    [Tooltip("If enabled, this incident can activate in any region or zone.")]
    [SerializeField] bool globalScope = true;
    [Tooltip("Specific regions where this incident can activate when Global Scope is disabled.")]
    [SerializeField] List<RegionInfoDefinition> allowedRegions = new List<RegionInfoDefinition>();
    [Tooltip("Region tags accepted when Global Scope is disabled.")]
    [SerializeField] List<string> allowedRegionTags = new List<string>();
    [Tooltip("Specific activity zones where this incident can activate when Global Scope is disabled.")]
    [SerializeField] List<ActivityZoneDefinition> allowedZones = new List<ActivityZoneDefinition>();
    [Tooltip("Activity zone tags accepted when Global Scope is disabled.")]
    [SerializeField] List<string> allowedZoneTags = new List<string>();

    [Header("Active Lifetime")]
    [Tooltip("How long this incident remains active. 0 means it records history but does not create an active state.")]
    [Min(0)]
    [SerializeField] int durationHours = 8;
    [Tooltip("If enabled, active records are expired by PlayerJourneyIncidentLog when their duration ends.")]
    [SerializeField] bool expireAutomatically = true;

    [Header("Activate Effects")]
    [Tooltip("Optional situation event started when this incident activates.")]
    [SerializeField] SituationEventDefinition situationEventOnActivate = null;
    [Tooltip("Optional situation event pool rolled when this incident activates.")]
    [SerializeField] SituationEventPoolDefinition situationPoolOnActivate = null;
    [Tooltip("Life Path rewards awarded when this incident activates.")]
    [SerializeField] List<LifePathReward> lifePathRewardsOnActivate = new List<LifePathReward>();
    [Tooltip("Consequence chains applied when this incident activates.")]
    [SerializeField] List<ConsequenceChainDefinition> consequenceChainsOnActivate = new List<ConsequenceChainDefinition>();

    [Header("Resolve Effects")]
    [Tooltip("Life Path rewards awarded when this incident is resolved.")]
    [SerializeField] List<LifePathReward> lifePathRewardsOnResolve = new List<LifePathReward>();
    [Tooltip("Consequence chains applied when this incident is resolved.")]
    [SerializeField] List<ConsequenceChainDefinition> consequenceChainsOnResolve = new List<ConsequenceChainDefinition>();

    [Header("Expire Effects")]
    [Tooltip("Life Path rewards awarded when this incident expires.")]
    [SerializeField] List<LifePathReward> lifePathRewardsOnExpire = new List<LifePathReward>();
    [Tooltip("Consequence chains applied when this incident expires.")]
    [SerializeField] List<ConsequenceChainDefinition> consequenceChainsOnExpire = new List<ConsequenceChainDefinition>();

    [Header("Events")]
    [Tooltip("Optional event published when this journey incident activates.")]
    [SerializeField] GameEventDefinition activatedEvent = null;
    [Tooltip("Optional event published when this journey incident resolves.")]
    [SerializeField] GameEventDefinition resolvedEvent = null;
    [Tooltip("Optional event published when this journey incident expires.")]
    [SerializeField] GameEventDefinition expiredEvent = null;
    [Tooltip("Optional event published when this journey incident is blocked.")]
    [SerializeField] GameEventDefinition blockedEvent = null;
    [Tooltip("If enabled, generated journey incident events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, generated journey incident events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public JourneyIncidentCategory Category => category;
    public JourneyIncidentSeverity Severity => severity;
    public int Priority => priority;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public Sprite Icon => icon;
    public AccessProfileDefinition AccessProfile => accessProfile;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public bool AllowMultipleActiveInstances => allowMultipleActiveInstances;
    public float ActivationChance => Mathf.Clamp01(activationChance);
    public int BaseWeight => Mathf.Max(0, baseWeight);
    public ConsequenceChainRepeatMode RepeatMode => repeatMode;
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public int MaxActivationCount => Mathf.Max(0, maxActivationCount);
    public bool RecordBlockedAttempts => recordBlockedAttempts;
    public bool GlobalScope => globalScope;
    public IReadOnlyList<RegionInfoDefinition> AllowedRegions => allowedRegions != null ? (IReadOnlyList<RegionInfoDefinition>)allowedRegions : Array.Empty<RegionInfoDefinition>();
    public IReadOnlyList<string> AllowedRegionTags => allowedRegionTags != null ? (IReadOnlyList<string>)allowedRegionTags : Array.Empty<string>();
    public IReadOnlyList<ActivityZoneDefinition> AllowedZones => allowedZones != null ? (IReadOnlyList<ActivityZoneDefinition>)allowedZones : Array.Empty<ActivityZoneDefinition>();
    public IReadOnlyList<string> AllowedZoneTags => allowedZoneTags != null ? (IReadOnlyList<string>)allowedZoneTags : Array.Empty<string>();
    public int DurationHours => Mathf.Max(0, durationHours);
    public bool ExpireAutomatically => expireAutomatically;
    public SituationEventDefinition SituationEventOnActivate => situationEventOnActivate;
    public SituationEventPoolDefinition SituationPoolOnActivate => situationPoolOnActivate;
    public IReadOnlyList<LifePathReward> LifePathRewardsOnActivate => lifePathRewardsOnActivate != null ? (IReadOnlyList<LifePathReward>)lifePathRewardsOnActivate : Array.Empty<LifePathReward>();
    public IReadOnlyList<LifePathReward> LifePathRewardsOnResolve => lifePathRewardsOnResolve != null ? (IReadOnlyList<LifePathReward>)lifePathRewardsOnResolve : Array.Empty<LifePathReward>();
    public IReadOnlyList<LifePathReward> LifePathRewardsOnExpire => lifePathRewardsOnExpire != null ? (IReadOnlyList<LifePathReward>)lifePathRewardsOnExpire : Array.Empty<LifePathReward>();
    public IReadOnlyList<ConsequenceChainDefinition> ConsequenceChainsOnActivate => consequenceChainsOnActivate != null ? (IReadOnlyList<ConsequenceChainDefinition>)consequenceChainsOnActivate : Array.Empty<ConsequenceChainDefinition>();
    public IReadOnlyList<ConsequenceChainDefinition> ConsequenceChainsOnResolve => consequenceChainsOnResolve != null ? (IReadOnlyList<ConsequenceChainDefinition>)consequenceChainsOnResolve : Array.Empty<ConsequenceChainDefinition>();
    public IReadOnlyList<ConsequenceChainDefinition> ConsequenceChainsOnExpire => consequenceChainsOnExpire != null ? (IReadOnlyList<ConsequenceChainDefinition>)consequenceChainsOnExpire : Array.Empty<ConsequenceChainDefinition>();

    public bool CanActivate(
        PlayerController player,
        PlayerJourneyIncidentLog log,
        RegionInfoDefinition region,
        ActivityZoneDefinition zone,
        string sourceId,
        out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to activate journey incidents.";
            return false;
        }

        if(accessProfile != null && !accessProfile.CanAccess(player, out failureMessage)) {
            failureMessage = string.IsNullOrWhiteSpace(failureMessage) ? lockedMessage : failureMessage;
            return false;
        }

        if(!MatchesLocation(region, zone)) {
            failureMessage = "Journey incident location filters did not match.";
            return false;
        }

        if(!RequirementsMet(player, out failureMessage)) {
            failureMessage = string.IsNullOrWhiteSpace(failureMessage) ? lockedMessage : failureMessage;
            return false;
        }

        if(log != null && !log.CanActivate(this, sourceId, repeatMode, CooldownHours, MaxActivationCount, allowMultipleActiveInstances, region, zone, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public JourneyIncidentActivationResult Activate(
        PlayerController player,
        RegionInfoDefinition region = null,
        ActivityZoneDefinition zone = null,
        string sourceId = null,
        string sourceName = null,
        UnityEngine.Object context = null) {
        var result = new JourneyIncidentActivationResult(this, sourceId, region, zone);
        var log = player != null ? player.GetComponent<PlayerJourneyIncidentLog>() ?? player.gameObject.AddComponent<PlayerJourneyIncidentLog>() : null;
        string resolvedSourceId = ResolveSourceId(sourceId);
        string resolvedSourceName = ResolveSourceName(sourceName);

        if(!CanActivate(player, log, region, zone, resolvedSourceId, out var failureMessage)) {
            result.blocked = true;
            result.failureMessage = failureMessage;
            if(recordBlockedAttempts) {
                log?.RecordBlocked(this, resolvedSourceId, resolvedSourceName, region, zone, failureMessage);
            }
            PublishAccessCheck(player, false, resolvedSourceId, failureMessage, context);
            PublishIncidentEvent(blockedEvent, JourneyIncidentPhase.Blocked, player, result, context, GameEventImportance.Warning);
            return result;
        }

        PublishAccessCheck(player, true, resolvedSourceId, accessProfile != null ? accessProfile.PassedMessage : null, context);
        if(UnityEngine.Random.value > ActivationChance) {
            result.blocked = true;
            result.failureMessage = "Journey incident activation chance failed.";
            if(recordBlockedAttempts) {
                log?.RecordBlocked(this, resolvedSourceId, resolvedSourceName, region, zone, result.failureMessage);
            }
            PublishIncidentEvent(blockedEvent, JourneyIncidentPhase.Blocked, player, result, context, GameEventImportance.Trace);
            return result;
        }

        result.activeState = log?.RecordActivated(this, resolvedSourceId, resolvedSourceName, region, zone, DurationHours, expireAutomatically);
        ApplyPhaseEffects(player, JourneyIncidentPhase.Activated, region, zone, resolvedSourceId, resolvedSourceName, context);
        PublishIncidentEvent(activatedEvent, JourneyIncidentPhase.Activated, player, result, context, GameEventImportance.Info);
        return result;
    }

    public int ResolveActive(PlayerController player, RegionInfoDefinition region = null, ActivityZoneDefinition zone = null, string sourceId = null, UnityEngine.Object context = null) {
        if(player == null) {
            return 0;
        }

        string resolvedSourceId = ResolveSourceId(sourceId);
        int resolved = player.GetComponent<PlayerJourneyIncidentLog>()?.Resolve(this, resolvedSourceId, region, zone) ?? 0;
        if(resolved > 0) {
            ApplyPhaseEffects(player, JourneyIncidentPhase.Resolved, region, zone, resolvedSourceId, DisplayName, context);
            PublishIncidentEvent(resolvedEvent, JourneyIncidentPhase.Resolved, player, new JourneyIncidentActivationResult(this, resolvedSourceId, region, zone), context, GameEventImportance.Success);
        }

        return resolved;
    }

    public int ExpireActive(PlayerController player, RegionInfoDefinition region = null, ActivityZoneDefinition zone = null, string sourceId = null, UnityEngine.Object context = null) {
        if(player == null) {
            return 0;
        }

        string resolvedSourceId = ResolveSourceId(sourceId);
        int expired = player.GetComponent<PlayerJourneyIncidentLog>()?.Expire(this, resolvedSourceId, region, zone) ?? 0;
        if(expired > 0) {
            ApplyPhaseEffects(player, JourneyIncidentPhase.Expired, region, zone, resolvedSourceId, DisplayName, context);
            PublishIncidentEvent(expiredEvent, JourneyIncidentPhase.Expired, player, new JourneyIncidentActivationResult(this, resolvedSourceId, region, zone), context, GameEventImportance.Info);
        }

        return expired;
    }

    public void ApplyExpiredEffects(PlayerController player, PlayerJourneyIncidentState state, UnityEngine.Object context = null) {
        if(player == null || state == null) {
            return;
        }

        ApplyPhaseEffects(player, JourneyIncidentPhase.Expired, state.ResolveRegion(), state.ResolveZone(), state.sourceId, state.sourceName, context);
        PublishIncidentEvent(expiredEvent, JourneyIncidentPhase.Expired, player, new JourneyIncidentActivationResult(this, state.sourceId, state.ResolveRegion(), state.ResolveZone()), context, GameEventImportance.Info);
    }

    public bool MatchesLocation(RegionInfoDefinition region, ActivityZoneDefinition zone) {
        if(globalScope) {
            return true;
        }

        bool hasFilters = AllowedRegions.Count > 0 || AllowedRegionTags.Count > 0 || AllowedZones.Count > 0 || AllowedZoneTags.Count > 0;
        if(!hasFilters) {
            return false;
        }

        if(region != null && (AllowedRegions.Contains(region) || AllowedRegionTags.Any(region.HasTag))) {
            return true;
        }

        if(zone != null && (AllowedZones.Contains(zone) || AllowedZoneTags.Any(zone.HasTag))) {
            return true;
        }

        return region != null && zone != null && AllowedRegions.Any(entry => entry != null && entry.ActivityZones.Contains(zone));
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    bool RequirementsMet(PlayerController player, out string failureMessage) {
        var activeRequirements = requirements?.Where(requirement => requirement != null).ToList() ?? new List<ActivityRequirement>();
        if(activeRequirements.Count == 0) {
            failureMessage = null;
            return true;
        }

        if(requirementMatchMode == ConsequenceRequirementMatchMode.Any) {
            foreach(var requirement in activeRequirements) {
                if(requirement.IsMet(player)) {
                    failureMessage = null;
                    return true;
                }
            }

            failureMessage = activeRequirements.FirstOrDefault()?.FailureMessage ?? lockedMessage;
            return false;
        }

        foreach(var requirement in activeRequirements) {
            if(!requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    void ApplyPhaseEffects(PlayerController player, JourneyIncidentPhase phase, RegionInfoDefinition region, ActivityZoneDefinition zone, string sourceId, string sourceName, UnityEngine.Object context) {
        if(player == null) {
            return;
        }

        IReadOnlyList<LifePathReward> lifePathRewards;
        IReadOnlyList<ConsequenceChainDefinition> chains;
        switch(phase) {
            case JourneyIncidentPhase.Resolved:
                lifePathRewards = LifePathRewardsOnResolve;
                chains = ConsequenceChainsOnResolve;
                break;
            case JourneyIncidentPhase.Expired:
                lifePathRewards = LifePathRewardsOnExpire;
                chains = ConsequenceChainsOnExpire;
                break;
            default:
                lifePathRewards = LifePathRewardsOnActivate;
                chains = ConsequenceChainsOnActivate;
                break;
        }

        if(phase == JourneyIncidentPhase.Activated) {
            situationEventOnActivate?.TryStart(player, region, zone, sourceId, sourceName, context != null ? context : this);
            situationPoolOnActivate?.Roll(player, region, zone, sourceId, sourceName, context != null ? context : this);
        }

        var lifePathLog = player.GetComponent<PlayerLifePathLog>() ?? player.gameObject.AddComponent<PlayerLifePathLog>();
        lifePathLog.ApplyRewards(lifePathRewards, sourceId, sourceName, context != null ? context : this);

        var chainContext = new ConsequenceChainContext {
            SourceId = sourceId,
            SourceName = sourceName,
            Region = region,
            Zone = zone,
            ContextObject = context != null ? context : this
        };

        foreach(var chain in chains) {
            chain?.Apply(player, chainContext, context != null ? context : this);
        }
    }

    void PublishIncidentEvent(GameEventDefinition eventDefinition, JourneyIncidentPhase phase, PlayerController player, JourneyIncidentActivationResult result, UnityEngine.Object context, GameEventImportance importance) {
        string sourceId = result != null ? ResolveSourceId(result.sourceId) : $"journey-incident:{Id}";
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"journey-incident.{phase.ToString().ToLowerInvariant()}.{Id}",
            phase == JourneyIncidentPhase.Activated ? $"{DisplayName} activated." : $"{DisplayName} {phase.ToString().ToLowerInvariant()}.",
            GameEventCategory.WorldEvent,
            importance,
            context != null ? context : player != null ? (UnityEngine.Object)player : this,
            "JourneyIncidentDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("journeyIncidentId", Id),
            GameEventPublishing.Value("journeyIncidentName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("severity", severity),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("regionId", result?.region != null ? result.region.Id : string.Empty),
            GameEventPublishing.Value("zoneId", result?.zone != null ? result.zone.Id : string.Empty),
            GameEventPublishing.Value("blocked", result != null && result.blocked),
            GameEventPublishing.Value("failureMessage", result != null ? result.failureMessage : string.Empty));
    }

    void PublishAccessCheck(PlayerController player, bool passed, string sourceId, string message, UnityEngine.Object context) {
        if(accessProfile == null || !publishAccessChecks) {
            return;
        }

        accessProfile.PublishChecked(player, passed, sourceId, message, context != null ? context : this);
    }

    string ResolveSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? $"journey-incident:{Id}" : sourceId;
    }

    string ResolveSourceName(string sourceName) {
        return string.IsNullOrWhiteSpace(sourceName) ? DisplayName : sourceName;
    }
}

public class JourneyIncidentActivationResult {
    public readonly string incidentId;
    public readonly string incidentName;
    public readonly string sourceId;
    public readonly RegionInfoDefinition region;
    public readonly ActivityZoneDefinition zone;
    public PlayerJourneyIncidentState activeState;
    public bool blocked;
    public string failureMessage;

    public JourneyIncidentActivationResult(JourneyIncidentDefinition definition, string sourceId, RegionInfoDefinition region, ActivityZoneDefinition zone) {
        incidentId = definition != null ? definition.Id : string.Empty;
        incidentName = definition != null ? definition.DisplayName : string.Empty;
        this.sourceId = sourceId;
        this.region = region;
        this.zone = zone;
    }
}
