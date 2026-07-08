using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CompetitionInvitationKind {
    Invitation,
    Qualification,
    Wildcard,
    ProfessorRecommendation,
    PolicePermit,
    SponsorPass,
    FrontierPass,
    ChampionshipInvite,
    Custom
}

public enum CompetitionInvitationGrantMode {
    AlwaysRefreshOrAdd,
    OnceEver,
    RefreshExistingOnly
}

[CreateAssetMenu(menuName = "Competitions/Invitation Definition")]
public class CompetitionInvitationDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this invitation, qualifier pass or wildcard. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future registration, PokeNav or tournament UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation for this invitation.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad invitation kind used by filters and future UI styling.")]
    [SerializeField] CompetitionInvitationKind kind = CompetitionInvitationKind.Invitation;
    [Tooltip("Optional icon used by future invitation UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags such as professor, police, wildcard, frontier, world, regional, elite or sponsor.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Target Filters")]
    [Tooltip("Optional exact registration this invitation can unlock. Empty allows any matching registration.")]
    [SerializeField] CompetitionRegistrationDefinition registrationFilter;
    [Tooltip("Optional roster this invitation can unlock. Empty allows any roster.")]
    [SerializeField] CompetitionRosterDefinition rosterFilter;
    [Tooltip("Optional competition this invitation can unlock. Empty allows any competition.")]
    [SerializeField] CompetitionDefinition competitionFilter;
    [Tooltip("Optional season this invitation can unlock. Empty allows any season.")]
    [SerializeField] CompetitionSeasonDefinition seasonFilter;
    [Tooltip("Optional ranking track this invitation can unlock. Empty allows any ranking.")]
    [SerializeField] CompetitionRankingDefinition rankingFilter;
    [Tooltip("Registration tags required before this invitation can match. Empty means no tag filter.")]
    [SerializeField] List<string> requiredRegistrationTags = new List<string>();
    [Tooltip("How required registration tags are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode registrationTagMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Window tags required before this invitation can match a scheduled registration window. Empty means no window tag filter.")]
    [SerializeField] List<string> requiredWindowTags = new List<string>();
    [Tooltip("How required registration window tags are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode windowTagMatchMode = ConsequenceRequirementMatchMode.All;

    [Header("Grant Rules")]
    [Tooltip("How repeated grants of this invitation are handled.")]
    [SerializeField] CompetitionInvitationGrantMode grantMode = CompetitionInvitationGrantMode.AlwaysRefreshOrAdd;
    [Tooltip("Number of registration uses granted each time this invitation is awarded. 0 means unlimited uses while active.")]
    [Min(0)]
    [SerializeField] int usesGranted = 1;
    [Tooltip("If enabled, the invitation expires after Default Duration Hours.")]
    [SerializeField] bool expires;
    [Tooltip("In-game hours this invitation remains usable when Expires is enabled.")]
    [Min(0)]
    [SerializeField] int defaultDurationHours = 24;
    [Tooltip("If enabled, granting this invitation again refreshes its expiration time.")]
    [SerializeField] bool refreshExpirationOnGrant = true;

    [Header("Access")]
    [Tooltip("How grant requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Extra activity-style requirements checked before this invitation can be granted.")]
    [SerializeField] List<ActivityRequirement> grantRequirements = new List<ActivityRequirement>();
    [Tooltip("Message/debug reason used when this invitation cannot be granted or used.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This invitation is not available yet.";

    [Header("Events")]
    [Tooltip("Optional event published when this invitation is granted.")]
    [SerializeField] GameEventDefinition grantedEvent;
    [Tooltip("Optional event published when this invitation is used by a registration.")]
    [SerializeField] GameEventDefinition usedEvent;
    [Tooltip("If enabled, invitation events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, invitation events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public CompetitionInvitationKind Kind => kind;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public CompetitionRegistrationDefinition RegistrationFilter => registrationFilter;
    public CompetitionRosterDefinition RosterFilter => rosterFilter;
    public CompetitionDefinition CompetitionFilter => competitionFilter;
    public CompetitionSeasonDefinition SeasonFilter => seasonFilter;
    public CompetitionRankingDefinition RankingFilter => rankingFilter;
    public IReadOnlyList<string> RequiredRegistrationTags => requiredRegistrationTags != null ? (IReadOnlyList<string>)requiredRegistrationTags : Array.Empty<string>();
    public IReadOnlyList<string> RequiredWindowTags => requiredWindowTags != null ? (IReadOnlyList<string>)requiredWindowTags : Array.Empty<string>();
    public CompetitionInvitationGrantMode GrantMode => grantMode;
    public int UsesGranted => Mathf.Max(0, usesGranted);
    public bool UnlimitedUses => UsesGranted == 0;
    public bool Expires => expires;
    public int DefaultDurationHours => Mathf.Max(0, defaultDurationHours);
    public bool RefreshExpirationOnGrant => refreshExpirationOnGrant;
    public IReadOnlyList<ActivityRequirement> GrantRequirements => grantRequirements != null ? (IReadOnlyList<ActivityRequirement>)grantRequirements : Array.Empty<ActivityRequirement>();

    public bool CanGrant(PlayerController player, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to grant invitations.";
            return false;
        }

        var log = player.GetComponent<PlayerCompetitionInvitationLog>();
        if(log == null && grantMode == CompetitionInvitationGrantMode.RefreshExistingOnly) {
            failureMessage = $"{DisplayName} cannot be refreshed because it is not owned.";
            return false;
        }

        if(log != null && !log.CanGrant(this, out failureMessage)) {
            return false;
        }

        if(!RequirementsMet(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool TryGrant(PlayerController player, string sourceId, out PlayerCompetitionInvitationRecord record, out string failureMessage) {
        record = null;
        if(!CanGrant(player, out failureMessage)) {
            return false;
        }

        var log = player.GetComponent<PlayerCompetitionInvitationLog>() ?? player.gameObject.AddComponent<PlayerCompetitionInvitationLog>();
        record = log.RecordGrant(this, sourceId);
        PublishInvitationEvent(grantedEvent, "granted", $"{DisplayName} granted.", GameEventImportance.Success, player, sourceId, null, null);
        failureMessage = null;
        return true;
    }

    public bool CanUse(PlayerController player, CompetitionRegistrationDefinition registration, CompetitionRegistrationWindowDefinition registrationWindow, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to use invitations.";
            return false;
        }

        if(!MatchesRegistration(registration, registrationWindow)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} does not match this registration." : lockedMessage;
            return false;
        }

        var log = player.GetComponent<PlayerCompetitionInvitationLog>();
        if(log == null) {
            failureMessage = $"{DisplayName} is not available.";
            return false;
        }

        if(!log.HasUsableInvitation(this, out failureMessage)) {
            failureMessage = string.IsNullOrWhiteSpace(failureMessage) ? $"{DisplayName} is not available." : failureMessage;
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool TryUse(PlayerController player, CompetitionRegistrationDefinition registration, CompetitionRegistrationWindowDefinition registrationWindow, string sourceId, out string failureMessage) {
        if(!CanUse(player, registration, registrationWindow, out failureMessage)) {
            return false;
        }

        var log = player.GetComponent<PlayerCompetitionInvitationLog>();
        if(log == null || !log.RecordUse(this, registration, registrationWindow, sourceId, out failureMessage)) {
            return false;
        }

        PublishInvitationEvent(usedEvent, "used", $"{DisplayName} used.", GameEventImportance.Info, player, sourceId, registration, registrationWindow);
        failureMessage = null;
        return true;
    }

    public bool MatchesRegistration(CompetitionRegistrationDefinition registration, CompetitionRegistrationWindowDefinition registrationWindow = null) {
        if(registration == null) {
            return registrationFilter == null
                && rosterFilter == null
                && competitionFilter == null
                && seasonFilter == null
                && rankingFilter == null;
        }

        if(registrationFilter != null && registration != registrationFilter) {
            return false;
        }

        if(rosterFilter != null && registration.Roster != rosterFilter) {
            return false;
        }

        if(competitionFilter != null && registration.Competition != competitionFilter) {
            return false;
        }

        if(seasonFilter != null && registration.Season != seasonFilter) {
            return false;
        }

        if(rankingFilter != null && registration.Ranking != rankingFilter) {
            return false;
        }

        if(!MatchesTags(requiredRegistrationTags, registrationTagMatchMode, registration.HasTag)) {
            return false;
        }

        if(!MatchesTags(requiredWindowTags, windowTagMatchMode, tag => registrationWindow != null && registrationWindow.HasTag(tag))) {
            return false;
        }

        return true;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    bool MatchesTags(List<string> requiredTags, ConsequenceRequirementMatchMode matchMode, Func<string, bool> hasTag) {
        var activeTags = requiredTags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() ?? new List<string>();
        if(activeTags.Count == 0) {
            return true;
        }

        if(matchMode == ConsequenceRequirementMatchMode.Any) {
            return activeTags.Any(hasTag);
        }

        return activeTags.All(hasTag);
    }

    bool RequirementsMet(PlayerController player, out string failureMessage) {
        var activeRequirements = grantRequirements?.Where(requirement => requirement != null).ToList() ?? new List<ActivityRequirement>();
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

    void PublishInvitationEvent(GameEventDefinition eventDefinition, string phase, string message, GameEventImportance importance, PlayerController player, string sourceId, CompetitionRegistrationDefinition registration, CompetitionRegistrationWindowDefinition registrationWindow) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"competition-invitation.{phase}.{Id}.{sourceId}",
            message,
            GameEventCategory.BattleRule,
            importance,
            player != null ? player : this,
            "CompetitionInvitationDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("invitationId", Id),
            GameEventPublishing.Value("invitationName", DisplayName),
            GameEventPublishing.Value("kind", kind),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("registrationId", registration != null ? registration.Id : string.Empty),
            GameEventPublishing.Value("windowId", registrationWindow != null ? registrationWindow.Id : string.Empty),
            GameEventPublishing.Value("sourceId", sourceId));
    }
}
