using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ActivityZoneType {
    General,
    Farming,
    Mining,
    PokemonCare,
    Research,
    Settlement,
    Wild,
    Restricted,
    Custom
}

public enum ActivityZoneRuleMode {
    AllowListedActivities,
    BlockListedActivities,
    AllowAll,
    BlockAll
}

[CreateAssetMenu(menuName = "Activities/Activity Zone Definition")]
public class ActivityZoneDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this zone. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this activity zone.")]
    [TextArea][SerializeField] string description;

    [Header("Area Rules")]
    [Tooltip("Broad area type used by validators, dialog conditions and future UI filters.")]
    [SerializeField] ActivityZoneType zoneType = ActivityZoneType.General;
    [Tooltip("Higher priority zones win when the player overlaps multiple activity zones.")]
    [SerializeField] int priority;
    [Tooltip("How this zone decides whether an activity can be performed.")]
    [SerializeField] ActivityZoneRuleMode ruleMode = ActivityZoneRuleMode.AllowListedActivities;
    [Tooltip("If set, this message is shown when an activity is blocked by this zone.")]
    [TextArea]
    [SerializeField] string blockedMessage;
    [Tooltip("Free-form tags for future dialog/UI/quest logic, such as farm, mine, lab, ranch or sacred.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Activity Lists")]
    [Tooltip("Activities allowed by this zone when rule mode is Allow Listed Activities.")]
    [SerializeField] List<ActivityDefinition> allowedActivities = new List<ActivityDefinition>();
    [Tooltip("Activities blocked by this zone when rule mode is Block Listed Activities or Allow All.")]
    [SerializeField] List<ActivityDefinition> blockedActivities = new List<ActivityDefinition>();

    [Header("Permissions")]
    [Tooltip("Optional permission rules that add permit, override or block logic on top of this zone's activity lists.")]
    [SerializeField] List<ActivityPermissionDefinition> permissions = new List<ActivityPermissionDefinition>();

    [Header("Modifiers")]
    [Tooltip("Optional area modifiers applied while the player is inside this zone.")]
    [SerializeField] List<ActivityZoneModifierDefinition> modifiers = new List<ActivityZoneModifierDefinition>();

    [Header("Events")]
    [Tooltip("Optional event published when the player enters this zone.")]
    [SerializeField] GameEventDefinition enteredEvent;
    [Tooltip("Optional event published when the player exits this zone.")]
    [SerializeField] GameEventDefinition exitedEvent;
    [Tooltip("If enabled, enter/exit events can appear in the notification feed.")]
    [SerializeField] bool showZoneEventsInFeed;
    [Tooltip("If enabled, enter/exit events are written to the debug log.")]
    [SerializeField] bool writeZoneEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public ActivityZoneType ZoneType => zoneType;
    public int Priority => priority;
    public ActivityZoneRuleMode RuleMode => ruleMode;
    public string BlockedMessage => blockedMessage;
    public IReadOnlyList<string> Tags => tags;
    public IReadOnlyList<ActivityDefinition> AllowedActivities => allowedActivities;
    public IReadOnlyList<ActivityDefinition> BlockedActivities => blockedActivities;
    public IReadOnlyList<ActivityPermissionDefinition> Permissions => permissions;
    public IReadOnlyList<ActivityZoneModifierDefinition> Modifiers => modifiers;
    public GameEventDefinition EnteredEvent => enteredEvent;
    public GameEventDefinition ExitedEvent => exitedEvent;
    public bool ShowZoneEventsInFeed => showZoneEventsInFeed;
    public bool WriteZoneEventsToDebugLog => writeZoneEventsToDebugLog;

    public bool Allows(ActivityDefinition activity) {
        return Allows(activity, null, out _);
    }

    public bool Allows(ActivityDefinition activity, out string failureMessage) {
        return Allows(activity, null, out failureMessage);
    }

    public bool Allows(ActivityDefinition activity, PlayerController player, out string failureMessage) {
        if(activity == null) {
            failureMessage = GetBlockedMessage(null);
            return false;
        }

        bool zoneRuleAllows = AllowsByZoneRules(activity, out var zoneRuleFailureMessage);
        if(EvaluatePermissionDenials(activity, player, out failureMessage)) {
            return false;
        }

        bool permissionOverrideAllows = EvaluatePermissionOverrides(activity, player, out var permission);
        if(zoneRuleAllows || permissionOverrideAllows) {
            if(EvaluateAdditionalRequirements(activity, player, out failureMessage)) {
                return false;
            }

            if(permissionOverrideAllows) {
                permission?.PublishDecision(true, player, activity, this, "Permit override");
            }

            failureMessage = null;
            return true;
        }

        failureMessage = zoneRuleFailureMessage ?? GetBlockedMessage(activity);
        return false;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag) && tags.Any(t => string.Equals(t, tag, System.StringComparison.OrdinalIgnoreCase));
    }

    public string GetBlockedMessage(ActivityDefinition activity) {
        if(!string.IsNullOrWhiteSpace(blockedMessage)) {
            return blockedMessage;
        }

        string activityName = activity != null ? activity.DisplayName : "This activity";
        return $"{activityName} cannot be done in {DisplayName}.";
    }

    bool AllowsByZoneRules(ActivityDefinition activity, out string failureMessage) {
        if(blockedActivities.Contains(activity)) {
            failureMessage = GetBlockedMessage(activity);
            return false;
        }

        bool allowed = ruleMode switch {
            ActivityZoneRuleMode.AllowAll => true,
            ActivityZoneRuleMode.BlockAll => false,
            ActivityZoneRuleMode.BlockListedActivities => true,
            _ => allowedActivities.Contains(activity)
        };

        failureMessage = allowed ? null : GetBlockedMessage(activity);
        return allowed;
    }

    bool EvaluatePermissionDenials(ActivityDefinition activity, PlayerController player, out string failureMessage) {
        foreach(var permission in GetMatchingPermissions(activity)) {
            if(permission.Mode != ActivityPermissionMode.BlockWhenMet) {
                continue;
            }

            if(permission.RequirementsMet(player, out var requirementFailureMessage)) {
                failureMessage = permission.GetFailureMessage(activity, this, requirementFailureMessage);
                permission.PublishDecision(false, player, activity, this, failureMessage);
                return true;
            }
        }

        failureMessage = null;
        return false;
    }

    bool EvaluatePermissionOverrides(ActivityDefinition activity, PlayerController player, out ActivityPermissionDefinition allowingPermission) {
        foreach(var permission in GetMatchingPermissions(activity)) {
            if(permission.Mode != ActivityPermissionMode.PermitOverride) {
                continue;
            }

            if(permission.RequirementsMet(player, out _)) {
                allowingPermission = permission;
                return true;
            }
        }

        allowingPermission = null;
        return false;
    }

    bool EvaluateAdditionalRequirements(ActivityDefinition activity, PlayerController player, out string failureMessage) {
        foreach(var permission in GetMatchingPermissions(activity)) {
            if(permission.Mode != ActivityPermissionMode.AdditionalRequirement) {
                continue;
            }

            if(!permission.RequirementsMet(player, out var requirementFailureMessage)) {
                failureMessage = permission.GetFailureMessage(activity, this, requirementFailureMessage);
                permission.PublishDecision(false, player, activity, this, failureMessage);
                return true;
            }
        }

        failureMessage = null;
        return false;
    }

    IEnumerable<ActivityPermissionDefinition> GetMatchingPermissions(ActivityDefinition activity) {
        return (permissions ?? new List<ActivityPermissionDefinition>())
            .Where(permission => permission != null && permission.AppliesTo(activity, this))
            .OrderByDescending(permission => permission.Priority);
    }
}
