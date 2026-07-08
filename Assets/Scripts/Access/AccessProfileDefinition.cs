using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AccessProfileCategory {
    General,
    Area,
    Activity,
    Shop,
    Transit,
    Research,
    Police,
    Professor,
    Organization,
    Event,
    Custom
}

public enum AccessRequirementMatchMode {
    All,
    Any
}

[CreateAssetMenu(menuName = "Access/Access Profile Definition")]
public class AccessProfileDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this access profile. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in debug and future UI. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing explanation of what this profile gates.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad category used by filters and future UI.")]
    [SerializeField] AccessProfileCategory category = AccessProfileCategory.General;
    [Tooltip("Free-form tags used by requirements, dialog and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Priority used by future UI or systems that sort several access profiles.")]
    [SerializeField] int priority;

    [Header("Title And Permit")]
    [Tooltip("Optional exact title, badge, permit or license required to pass this profile.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional title tag required to pass this profile. Empty disables tag checking.")]
    [SerializeField] string requiredTitleTag;
    [Tooltip("Optional title kind required to pass this profile.")]
    [SerializeField] bool requireTitleKind;
    [Tooltip("Title kind required when Require Title Kind is enabled.")]
    [SerializeField] TitleKind requiredTitleKind = TitleKind.Permit;

    [Header("Organization And Career")]
    [Tooltip("Optional organization membership required to pass this profile.")]
    [SerializeField] OrganizationDefinition requiredOrganization;
    [Tooltip("Minimum organization rank index required for Required Organization.")]
    [Min(0)]
    [SerializeField] int requiredOrganizationRankIndex;
    [Tooltip("Optional career path required to pass this profile.")]
    [SerializeField] CareerPathDefinition requiredCareer;
    [Tooltip("Minimum career rank index required for Required Career.")]
    [Min(0)]
    [SerializeField] int requiredCareerRankIndex;

    [Header("World Progress")]
    [Tooltip("Optional faction whose reputation gates this profile.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum reputation required with Required Faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional milestone required to pass this profile.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional calendar event that must currently be active.")]
    [SerializeField] CalendarEventDefinition requiredActiveCalendarEvent;

    [Header("Area Context")]
    [Tooltip("If enabled, the player must currently be inside any activity zone.")]
    [SerializeField] bool requireAnyActiveZone;
    [Tooltip("Optional exact activity zone required to pass this profile.")]
    [SerializeField] ActivityZoneDefinition requiredActivityZone;
    [Tooltip("Optional activity zone type required to pass this profile.")]
    [SerializeField] bool requireActivityZoneType;
    [Tooltip("Activity zone type required when Require Activity Zone Type is enabled.")]
    [SerializeField] ActivityZoneType requiredZoneType = ActivityZoneType.General;
    [Tooltip("Optional activity zone tag required to pass this profile. Empty disables tag checking.")]
    [SerializeField] string requiredActivityZoneTag;
    [Tooltip("Optional activity that must be allowed in the current zone.")]
    [SerializeField] ActivityDefinition requiredAllowedActivity;

    [Header("Extra Requirements")]
    [Tooltip("How Extra Requirements are evaluated.")]
    [SerializeField] AccessRequirementMatchMode extraRequirementMatchMode = AccessRequirementMatchMode.All;
    [Tooltip("Optional reusable requirements that must pass after the core access checks.")]
    [SerializeField] List<ActivityRequirement> extraRequirements = new List<ActivityRequirement>();

    [Header("Messages")]
    [Tooltip("Default message shown when this profile blocks access.")]
    [SerializeField] string lockedMessage = "You are not allowed to do that right now.";
    [Tooltip("Message used when access succeeds and no system-specific message is provided.")]
    [SerializeField] string passedMessage = "Access granted.";

    [Header("Events")]
    [Tooltip("Optional event published when this profile grants access.")]
    [SerializeField] GameEventDefinition passedEvent;
    [Tooltip("Optional event published when this profile denies access.")]
    [SerializeField] GameEventDefinition deniedEvent;
    [Tooltip("If enabled, access events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed;
    [Tooltip("If enabled, access events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public AccessProfileCategory Category => category;
    public IReadOnlyList<string> Tags => tags;
    public int Priority => priority;
    public IReadOnlyList<ActivityRequirement> ExtraRequirements => extraRequirements;
    public GameEventDefinition PassedEvent => passedEvent;
    public GameEventDefinition DeniedEvent => deniedEvent;
    public bool ShowEventsInFeed => showEventsInFeed;
    public bool WriteEventsToDebugLog => writeEventsToDebugLog;
    public string PassedMessage => string.IsNullOrWhiteSpace(passedMessage) ? "Access granted." : passedMessage;
    public string LockedMessage => string.IsNullOrWhiteSpace(lockedMessage) ? "You are not allowed to do that right now." : lockedMessage;

    public bool CanAccess(PlayerController player, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required for access checks.";
            return false;
        }

        if(!PassesCoreAccess(player, out failureMessage)) {
            return false;
        }

        if(!PassesExtraRequirements(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public void PublishChecked(PlayerController player, bool passed, string contextId, string message, UnityEngine.Object context = null) {
        string phase = passed ? "passed" : "denied";
        GameEventPublishing.PublishOptional(
            passed ? passedEvent : deniedEvent,
            $"access.{phase}.{Id}",
            string.IsNullOrWhiteSpace(message) ? (passed ? PassedMessage : LockedMessage) : message,
            GameEventCategory.Access,
            passed ? GameEventImportance.Success : GameEventImportance.Warning,
            context != null ? context : player,
            "AccessProfileDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("accessProfileId", Id),
            GameEventPublishing.Value("accessProfileName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("contextId", contextId));
    }

    bool PassesCoreAccess(PlayerController player, out string failureMessage) {
        var titles = player.GetComponent<PlayerTitles>();
        if(requiredTitle != null && !(titles?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(!string.IsNullOrWhiteSpace(requiredTitleTag) && !(titles?.HasTitleWithTag(requiredTitleTag) ?? false)) {
            failureMessage = LockedMessage;
            return false;
        }

        if(requireTitleKind && !(titles?.HasTitleKind(requiredTitleKind) ?? false)) {
            failureMessage = LockedMessage;
            return false;
        }

        if(requiredOrganization != null && !(player.GetComponent<PlayerOrganizationLog>()?.HasReachedRank(requiredOrganization, requiredOrganizationRankIndex) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more progress with {requiredOrganization.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredCareer != null && !(player.GetComponent<PlayerCareerLog>()?.HasReachedRank(requiredCareer, requiredCareerRankIndex) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more progress in {requiredCareer.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredFaction != null && (player.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0) < requiredReputation) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more reputation with {requiredFaction.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredMilestone != null && !(player.GetComponent<PlayerMilestones>()?.HasMilestone(requiredMilestone) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredMilestone.DisplayName} first." : lockedMessage;
            return false;
        }

        if(requiredActiveCalendarEvent != null && !requiredActiveCalendarEvent.IsActiveNow()) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{requiredActiveCalendarEvent.Title} is not active right now." : lockedMessage;
            return false;
        }

        if(requireAnyActiveZone && PlayerActivityContext.CurrentZone == null) {
            failureMessage = LockedMessage;
            return false;
        }

        if(requiredActivityZone != null && !PlayerActivityContext.HasActiveZone(requiredActivityZone)) {
            failureMessage = LockedMessage;
            return false;
        }

        if(requireActivityZoneType && !PlayerActivityContext.HasActiveZoneType(requiredZoneType)) {
            failureMessage = LockedMessage;
            return false;
        }

        if(!string.IsNullOrWhiteSpace(requiredActivityZoneTag) && !PlayerActivityContext.HasActiveTag(requiredActivityZoneTag)) {
            failureMessage = LockedMessage;
            return false;
        }

        if(requiredAllowedActivity != null && !PlayerActivityContext.IsAllowed(requiredAllowedActivity, player)) {
            failureMessage = LockedMessage;
            return false;
        }

        failureMessage = null;
        return true;
    }

    bool PassesExtraRequirements(PlayerController player, out string failureMessage) {
        var requirements = extraRequirements?.Where(requirement => requirement != null).ToList() ?? new List<ActivityRequirement>();
        if(requirements.Count == 0) {
            failureMessage = null;
            return true;
        }

        if(extraRequirementMatchMode == AccessRequirementMatchMode.Any) {
            foreach(var requirement in requirements) {
                if(requirement.IsMet(player)) {
                    failureMessage = null;
                    return true;
                }
            }

            failureMessage = requirements.FirstOrDefault()?.FailureMessage ?? LockedMessage;
            return false;
        }

        foreach(var requirement in requirements) {
            if(!requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }
}
