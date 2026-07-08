using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ActivityPermissionMode {
    AdditionalRequirement,
    PermitOverride,
    BlockWhenMet
}

[CreateAssetMenu(menuName = "Activities/Activity Permission Definition")]
public class ActivityPermissionDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this permission rule. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in debug and future UI. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note explaining when this permission should apply.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Free-form tags used by validators, dialog and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Decision")]
    [Tooltip("How this permission affects the zone decision. Additional Requirement blocks when requirements fail, Permit Override can allow a normally blocked activity, Block When Met denies when requirements pass.")]
    [SerializeField] ActivityPermissionMode mode = ActivityPermissionMode.AdditionalRequirement;
    [Tooltip("Higher priority permissions are evaluated before lower priority permissions.")]
    [SerializeField] int priority;
    [Tooltip("Optional message shown when this permission blocks the activity.")]
    [TextArea]
    [SerializeField] string failureMessage;

    [Header("Activity Match")]
    [Tooltip("Activities affected by this permission. Empty accepts any activity unless Activity Tags are set.")]
    [SerializeField] List<ActivityDefinition> activities = new List<ActivityDefinition>();
    [Tooltip("Activity tags affected by this permission. Empty accepts any tag unless Activities are set.")]
    [SerializeField] List<string> activityTags = new List<string>();

    [Header("Zone Match")]
    [Tooltip("Zones affected by this permission. Empty accepts any zone unless Zone Types or Zone Tags are set.")]
    [SerializeField] List<ActivityZoneDefinition> zones = new List<ActivityZoneDefinition>();
    [Tooltip("Zone types affected by this permission. Empty accepts any type unless Zones or Zone Tags are set.")]
    [SerializeField] List<ActivityZoneType> zoneTypes = new List<ActivityZoneType>();
    [Tooltip("Zone tags affected by this permission. Empty accepts any tag unless Zones or Zone Types are set.")]
    [SerializeField] List<string> zoneTags = new List<string>();

    [Header("Requirements")]
    [Tooltip("Requirements checked by this permission. Empty means the permission requirement is already met.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();

    [Header("Events")]
    [Tooltip("Optional event published when this permission allows an activity through override.")]
    [SerializeField] GameEventDefinition grantedEvent;
    [Tooltip("Optional event published when this permission blocks an activity.")]
    [SerializeField] GameEventDefinition blockedEvent;
    [Tooltip("If enabled, permission decisions publish events. Keep this off for UI polling-heavy checks.")]
    [SerializeField] bool publishDecisionEvents;
    [Tooltip("If enabled, permission events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed;
    [Tooltip("If enabled, permission events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags;
    public ActivityPermissionMode Mode => mode;
    public int Priority => priority;
    public IReadOnlyList<ActivityDefinition> Activities => activities;
    public IReadOnlyList<string> ActivityTags => activityTags;
    public IReadOnlyList<ActivityZoneDefinition> Zones => zones;
    public IReadOnlyList<ActivityZoneType> ZoneTypes => zoneTypes;
    public IReadOnlyList<string> ZoneTags => zoneTags;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements;

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, System.StringComparison.OrdinalIgnoreCase));
    }

    public bool AppliesTo(ActivityDefinition activity, ActivityZoneDefinition zone) {
        return MatchesActivity(activity) && MatchesZone(zone);
    }

    public bool RequirementsMet(PlayerController player, out string requirementFailureMessage) {
        requirementFailureMessage = null;
        if(requirements == null) {
            return true;
        }

        foreach(var requirement in requirements) {
            if(requirement == null) {
                continue;
            }

            if(!requirement.IsMet(player)) {
                requirementFailureMessage = requirement.FailureMessage;
                return false;
            }
        }

        return true;
    }

    public string GetFailureMessage(ActivityDefinition activity, ActivityZoneDefinition zone, string requirementFailureMessage = null) {
        if(!string.IsNullOrWhiteSpace(failureMessage)) {
            return failureMessage;
        }

        if(!string.IsNullOrWhiteSpace(requirementFailureMessage)) {
            return requirementFailureMessage;
        }

        string activityName = activity != null ? activity.DisplayName : "This activity";
        string zoneName = zone != null ? zone.DisplayName : "this area";
        return $"{activityName} is not permitted in {zoneName}.";
    }

    public void PublishDecision(bool allowed, PlayerController player, ActivityDefinition activity, ActivityZoneDefinition zone, string reason) {
        if(!publishDecisionEvents) {
            return;
        }

        GameEventPublishing.PublishOptional(
            allowed ? grantedEvent : blockedEvent,
            allowed ? $"activity-permission.granted.{Id}" : $"activity-permission.blocked.{Id}",
            allowed ? $"{DisplayName} allowed {activity?.DisplayName}." : $"{DisplayName} blocked {activity?.DisplayName}.",
            GameEventCategory.Activity,
            allowed ? GameEventImportance.Info : GameEventImportance.Warning,
            player,
            "ActivityPermissionDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("permissionId", Id),
            GameEventPublishing.Value("permissionName", DisplayName),
            GameEventPublishing.Value("mode", mode),
            GameEventPublishing.Value("activityId", activity != null ? activity.Id : string.Empty),
            GameEventPublishing.Value("activityName", activity != null ? activity.DisplayName : string.Empty),
            GameEventPublishing.Value("zoneId", zone != null ? zone.Id : string.Empty),
            GameEventPublishing.Value("zoneName", zone != null ? zone.DisplayName : string.Empty),
            GameEventPublishing.Value("allowed", allowed),
            GameEventPublishing.Value("reason", reason));
    }

    bool MatchesActivity(ActivityDefinition activity) {
        bool hasActivityFilters = (activities != null && activities.Count > 0)
            || (activityTags != null && activityTags.Any(tag => !string.IsNullOrWhiteSpace(tag)));

        if(!hasActivityFilters) {
            return true;
        }

        if(activity == null) {
            return false;
        }

        bool exactMatch = activities != null && activities.Any(entry => entry == activity);
        bool tagMatch = activityTags != null && activityTags.Any(tag => activity.HasTag(tag));
        return exactMatch || tagMatch;
    }

    bool MatchesZone(ActivityZoneDefinition zone) {
        bool hasZoneFilters = (zones != null && zones.Count > 0)
            || (zoneTypes != null && zoneTypes.Count > 0)
            || (zoneTags != null && zoneTags.Any(tag => !string.IsNullOrWhiteSpace(tag)));

        if(!hasZoneFilters) {
            return true;
        }

        if(zone == null) {
            return false;
        }

        bool exactMatch = zones != null && zones.Any(entry => entry == zone);
        bool typeMatch = zoneTypes != null && zoneTypes.Contains(zone.ZoneType);
        bool tagMatch = zoneTags != null && zoneTags.Any(tag => zone.HasTag(tag));
        return exactMatch || typeMatch || tagMatch;
    }
}
