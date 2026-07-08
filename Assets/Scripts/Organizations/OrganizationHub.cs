using System.Collections.Generic;
using UnityEngine;

public class OrganizationHub : MonoBehaviour, IPlayerTriggerable {
    [Header("Hub")]
    [Tooltip("Stable hub/source id used by organization logs. Empty uses GameObject name.")]
    [SerializeField] string hubId;
    [Tooltip("Name shown in debug/future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName;
    [Tooltip("Primary organization represented by this hub.")]
    [SerializeField] OrganizationDefinition organization;

    [Header("Trigger Actions")]
    [Tooltip("If enabled, triggering this hub unlocks the primary organization.")]
    [SerializeField] bool unlockOrganizationOnTrigger = true;
    [Tooltip("If enabled, triggering this hub joins or refreshes the primary organization.")]
    [SerializeField] bool joinOrganizationOnTrigger;
    [Tooltip("If enabled, the hub can satisfy invitation/story-only join modes.")]
    [SerializeField] bool countsAsInvitation = true;
    [Tooltip("Membership grant used when Join Organization On Trigger is enabled.")]
    [SerializeField] OrganizationMembershipGrant membershipGrant;
    [Tooltip("Additional membership grants applied when this hub is triggered.")]
    [SerializeField] List<OrganizationMembershipGrant> extraMembershipGrants = new List<OrganizationMembershipGrant>();
    [Tooltip("Organization point grants applied when this hub is triggered.")]
    [SerializeField] List<OrganizationPointGrant> pointGrants = new List<OrganizationPointGrant>();
    [Tooltip("If enabled, repeated player triggers can call OnPlayerTriggered more than once.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this hub can be used.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional faction whose reputation gates this hub.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum reputation required with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional career required before this hub can be used.")]
    [SerializeField] CareerPathDefinition requiredCareer;
    [Tooltip("Minimum career rank index required for Required Career.")]
    [Min(0)]
    [SerializeField] int requiredCareerRankIndex;
    [Tooltip("Message shown when hub access is blocked.")]
    [SerializeField] string lockedMessage = "This organization hub is not available right now.";

    [Header("Debug")]
    [Tooltip("If enabled, trigger attempts are written to GameDebug.")]
    [SerializeField] bool logAttempts;

    public string HubId => string.IsNullOrWhiteSpace(hubId) ? name : hubId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public OrganizationDefinition Organization => organization;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        if(player == null) {
            return;
        }

        if(!CanUse(player, out var failureMessage)) {
            PublishHubEvent(player, "blocked", failureMessage, GameEventImportance.Warning);
            return;
        }

        var log = player.GetComponent<PlayerOrganizationLog>() ?? player.gameObject.AddComponent<PlayerOrganizationLog>();
        if(unlockOrganizationOnTrigger) {
            log.UnlockOrganization(organization, HubId);
        }

        if(joinOrganizationOnTrigger && organization != null) {
            if(membershipGrant != null && membershipGrant.organization != null) {
                if(!log.GrantMembership(membershipGrant, HubId, out failureMessage)) {
                    PublishHubEvent(player, "blocked", failureMessage, GameEventImportance.Warning);
                    return;
                }
            } else if(!log.JoinOrganization(organization, countsAsInvitation, organization.PermanentByDefault ? -1 : organization.DefaultDurationHours, HubId, refreshExisting: true, initialPoints: 0, out failureMessage)) {
                PublishHubEvent(player, "blocked", failureMessage, GameEventImportance.Warning);
                return;
            }
        }

        log.ApplyMembershipGrants(extraMembershipGrants, HubId);
        log.ApplyPointGrants(pointGrants, HubId);
        PublishHubEvent(player, "used", $"{DisplayName} updated organization progress.", GameEventImportance.Info);
    }

    public bool CanUse(PlayerController player, out string failureMessage) {
        if(requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredFaction != null) {
            int reputation = player?.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0;
            if(reputation < requiredReputation) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more reputation with {requiredFaction.DisplayName}." : lockedMessage;
                return false;
            }
        }

        if(requiredCareer != null && !(player?.GetComponent<PlayerCareerLog>()?.HasReachedRank(requiredCareer, requiredCareerRankIndex) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more progress in {requiredCareer.DisplayName}." : lockedMessage;
            return false;
        }

        failureMessage = null;
        return true;
    }

    void PublishHubEvent(PlayerController player, string phase, string message, GameEventImportance importance) {
        if(logAttempts) {
            GameDebug.Step(message, GameDebugCategory.Organization, player != null ? player : this, "OrganizationHub");
        }

        GameEventPublishing.PublishOptional(
            null,
            $"organization-hub.{phase}.{HubId}",
            message,
            GameEventCategory.Organization,
            importance,
            player != null ? player : this,
            "OrganizationHub",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: logAttempts,
            GameEventPublishing.Value("hubId", HubId),
            GameEventPublishing.Value("hubName", DisplayName),
            GameEventPublishing.Value("organizationId", organization != null ? organization.Id : string.Empty),
            GameEventPublishing.Value("phase", phase));
    }
}
