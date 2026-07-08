using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CompetitionVenueKind {
    Arena,
    Gym,
    Stadium,
    BattleFrontierFacility,
    ContestHall,
    LeagueHall,
    WorldChampionshipHall,
    ClubRoom,
    ResearchField,
    PoliceTrainingGround,
    OutdoorField,
    Custom
}

public enum CompetitionVenuePurpose {
    Enter,
    Registration,
    Bracket,
    Match,
    Contest,
    Custom
}

[CreateAssetMenu(menuName = "Competitions/Venue Definition")]
public class CompetitionVenueDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this venue, arena, gym or stadium. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future venue, map or tournament UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation for this venue.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad venue kind used by filters and future UI styling.")]
    [SerializeField] CompetitionVenueKind kind = CompetitionVenueKind.Arena;
    [Tooltip("Optional icon used by future venue/map UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags such as kanto, water-arena, elite, indoor, outdoor, frontier or championship.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("World Links")]
    [Tooltip("Optional world region that owns this venue.")]
    [SerializeField] WorldRegionDefinition worldRegion;
    [Tooltip("Optional PokeNav/map region card for this venue.")]
    [SerializeField] RegionInfoDefinition regionInfo;
    [Tooltip("Optional area profile applied when this venue is entered or used.")]
    [SerializeField] AreaProfileDefinition areaProfile;
    [Tooltip("Optional activity zone connected to this venue.")]
    [SerializeField] ActivityZoneDefinition activityZone;
    [Tooltip("Optional map marker connected to this venue.")]
    [SerializeField] MapMarkerDefinition mapMarker;
    [Tooltip("Optional scene name connected to this venue. Empty uses linked region info/world region data when possible.")]
    [SerializeField] string sceneName = string.Empty;
    [Tooltip("Optional location key such as arena room, floor, field id or facility id. Empty uses this venue id.")]
    [SerializeField] string locationKey = string.Empty;

    [Header("Hosted Content Filters")]
    [Tooltip("Exact registrations this venue can host. Empty means any registration can match other filters.")]
    [SerializeField] List<CompetitionRegistrationDefinition> allowedRegistrations = new List<CompetitionRegistrationDefinition>();
    [Tooltip("Exact rosters this venue can host. Empty means any roster can match other filters.")]
    [SerializeField] List<CompetitionRosterDefinition> allowedRosters = new List<CompetitionRosterDefinition>();
    [Tooltip("Exact competitions this venue can host. Empty means any competition can match other filters.")]
    [SerializeField] List<CompetitionDefinition> allowedCompetitions = new List<CompetitionDefinition>();
    [Tooltip("Exact seasons this venue can host. Empty means any season can match other filters.")]
    [SerializeField] List<CompetitionSeasonDefinition> allowedSeasons = new List<CompetitionSeasonDefinition>();
    [Tooltip("Exact rankings this venue can host. Empty means any ranking can match other filters.")]
    [SerializeField] List<CompetitionRankingDefinition> allowedRankings = new List<CompetitionRankingDefinition>();
    [Tooltip("Registration tags required before this venue can host a registration. Empty means no registration tag filter.")]
    [SerializeField] List<string> requiredRegistrationTags = new List<string>();
    [Tooltip("Roster tags required before this venue can host a roster. Empty means no roster tag filter.")]
    [SerializeField] List<string> requiredRosterTags = new List<string>();
    [Tooltip("Competition tags required before this venue can host a competition. Empty means no competition tag filter.")]
    [SerializeField] List<string> requiredCompetitionTags = new List<string>();
    [Tooltip("How tag filters are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode tagMatchMode = ConsequenceRequirementMatchMode.All;

    [Header("Battle Overrides")]
    [Tooltip("Optional challenge used when a bracket match has no challenge. Empty keeps the match/entrant challenge.")]
    [SerializeField] BattleChallengeDefinition fallbackChallenge;
    [Tooltip("Optional rule set that overrides roster, entrant or challenge rule selection while this venue hosts a match.")]
    [SerializeField] BattleRuleSetDefinition ruleSetOverride;
    [Tooltip("If enabled, venue override rule sets are only used when the player can access them.")]
    [SerializeField] bool requireRuleSetAccess = true;

    [Header("Access")]
    [Tooltip("How venue requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Extra activity-style requirements checked before this venue can be entered or used.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("How often this venue can be used for the same source id.")]
    [SerializeField] ConsequenceChainRepeatMode repeatMode = ConsequenceChainRepeatMode.Unlimited;
    [Tooltip("Cooldown in in-game hours when Repeat Mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours;
    [Tooltip("Maximum successful venue records for this venue. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxUseCount;
    [Tooltip("If enabled, successful venue use is written to PlayerCompetitionVenueLog.")]
    [SerializeField] bool recordHistory = true;
    [Tooltip("If enabled, blocked venue attempts are also written to PlayerCompetitionVenueLog.")]
    [SerializeField] bool recordBlockedAttempts;
    [Tooltip("If enabled, linked Area Profile is entered when this venue is used.")]
    [SerializeField] bool enterAreaProfileOnUse;
    [Tooltip("Message/debug reason used when this venue is blocked.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This venue is not available yet.";

    [Header("Events")]
    [Tooltip("Optional event published when this venue is used.")]
    [SerializeField] GameEventDefinition usedEvent;
    [Tooltip("Optional event published when this venue blocks access.")]
    [SerializeField] GameEventDefinition blockedEvent;
    [Tooltip("If enabled, venue events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, venue events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public CompetitionVenueKind Kind => kind;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public WorldRegionDefinition WorldRegion => worldRegion;
    public RegionInfoDefinition RegionInfo => regionInfo;
    public AreaProfileDefinition AreaProfile => areaProfile;
    public ActivityZoneDefinition ActivityZone => activityZone;
    public MapMarkerDefinition MapMarker => mapMarker;
    public string SceneName => sceneName;
    public string LocationKey => locationKey;
    public IReadOnlyList<CompetitionRegistrationDefinition> AllowedRegistrations => allowedRegistrations != null ? (IReadOnlyList<CompetitionRegistrationDefinition>)allowedRegistrations : Array.Empty<CompetitionRegistrationDefinition>();
    public IReadOnlyList<CompetitionRosterDefinition> AllowedRosters => allowedRosters != null ? (IReadOnlyList<CompetitionRosterDefinition>)allowedRosters : Array.Empty<CompetitionRosterDefinition>();
    public IReadOnlyList<CompetitionDefinition> AllowedCompetitions => allowedCompetitions != null ? (IReadOnlyList<CompetitionDefinition>)allowedCompetitions : Array.Empty<CompetitionDefinition>();
    public IReadOnlyList<CompetitionSeasonDefinition> AllowedSeasons => allowedSeasons != null ? (IReadOnlyList<CompetitionSeasonDefinition>)allowedSeasons : Array.Empty<CompetitionSeasonDefinition>();
    public IReadOnlyList<CompetitionRankingDefinition> AllowedRankings => allowedRankings != null ? (IReadOnlyList<CompetitionRankingDefinition>)allowedRankings : Array.Empty<CompetitionRankingDefinition>();
    public IReadOnlyList<string> RequiredRegistrationTags => requiredRegistrationTags != null ? (IReadOnlyList<string>)requiredRegistrationTags : Array.Empty<string>();
    public IReadOnlyList<string> RequiredRosterTags => requiredRosterTags != null ? (IReadOnlyList<string>)requiredRosterTags : Array.Empty<string>();
    public IReadOnlyList<string> RequiredCompetitionTags => requiredCompetitionTags != null ? (IReadOnlyList<string>)requiredCompetitionTags : Array.Empty<string>();
    public BattleChallengeDefinition FallbackChallenge => fallbackChallenge;
    public BattleRuleSetDefinition RuleSetOverride => ruleSetOverride;
    public bool RequireRuleSetAccess => requireRuleSetAccess;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public ConsequenceChainRepeatMode RepeatMode => repeatMode;
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public int MaxUseCount => Mathf.Max(0, maxUseCount);
    public bool RecordHistory => recordHistory;
    public bool RecordBlockedAttempts => recordBlockedAttempts;
    public bool EnterAreaProfileOnUse => enterAreaProfileOnUse;

    public bool CanHost(PlayerController player, CompetitionRegistrationDefinition registration, out string failureMessage) {
        if(!MatchesRegistration(registration, out failureMessage)) {
            return false;
        }

        return PassesAccess(player, out failureMessage);
    }

    public bool CanHost(PlayerController player, CompetitionRosterDefinition roster, out string failureMessage) {
        if(!MatchesRoster(roster, out failureMessage)) {
            return false;
        }

        return PassesAccess(player, out failureMessage);
    }

    public bool CanEnter(PlayerController player, out string failureMessage) {
        return PassesAccess(player, out failureMessage);
    }

    public bool MatchesRegistration(CompetitionRegistrationDefinition registration, out string failureMessage) {
        if(registration == null) {
            failureMessage = "A registration is required for venue hosting.";
            return false;
        }

        if(AllowedRegistrations.Count > 0 && !AllowedRegistrations.Contains(registration)) {
            failureMessage = "Venue registration filter does not match.";
            return false;
        }

        if(!MatchesRoster(registration.Roster, out failureMessage)) {
            return false;
        }

        if(!MatchesTags(RequiredRegistrationTags, registration.HasTag)) {
            failureMessage = "Venue registration tag filter does not match.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool MatchesRoster(CompetitionRosterDefinition roster, out string failureMessage) {
        if(roster == null) {
            failureMessage = "A roster is required for venue hosting.";
            return false;
        }

        if(AllowedRosters.Count > 0 && !AllowedRosters.Contains(roster)) {
            failureMessage = "Venue roster filter does not match.";
            return false;
        }

        if(AllowedCompetitions.Count > 0 && (roster.Competition == null || !AllowedCompetitions.Contains(roster.Competition))) {
            failureMessage = "Venue competition filter does not match.";
            return false;
        }

        if(AllowedSeasons.Count > 0 && (roster.Season == null || !AllowedSeasons.Contains(roster.Season))) {
            failureMessage = "Venue season filter does not match.";
            return false;
        }

        if(AllowedRankings.Count > 0 && (roster.Ranking == null || !AllowedRankings.Contains(roster.Ranking))) {
            failureMessage = "Venue ranking filter does not match.";
            return false;
        }

        if(!MatchesTags(RequiredRosterTags, roster.HasTag)) {
            failureMessage = "Venue roster tag filter does not match.";
            return false;
        }

        if(!MatchesTags(RequiredCompetitionTags, tag => roster.Competition != null && roster.Competition.HasTag(tag))) {
            failureMessage = "Venue competition tag filter does not match.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public BattleChallengeDefinition ResolveChallenge(BattleChallengeDefinition currentChallenge) {
        return currentChallenge != null ? currentChallenge : fallbackChallenge;
    }

    public BattleRuleSetDefinition ResolveRuleSet(PlayerController player, BattleRuleSetDefinition currentRuleSet, out string failureMessage) {
        if(ruleSetOverride == null) {
            failureMessage = null;
            return currentRuleSet;
        }

        if(requireRuleSetAccess && !ruleSetOverride.CanAccess(player, out failureMessage)) {
            return currentRuleSet;
        }

        failureMessage = null;
        return ruleSetOverride;
    }

    public PlayerCompetitionVenueRecord RecordUse(PlayerController player, CompetitionVenuePurpose purpose, CompetitionRegistrationDefinition registration, CompetitionRosterDefinition roster, string sourceId, UnityEngine.Object context, bool blocked, string failureMessage) {
        if(player == null || (!recordHistory && (!blocked || !recordBlockedAttempts))) {
            return null;
        }

        var log = player.GetComponent<PlayerCompetitionVenueLog>() ?? player.gameObject.AddComponent<PlayerCompetitionVenueLog>();
        var record = log.RecordUse(this, purpose, registration, roster, sourceId, blocked, failureMessage);

        if(!blocked && enterAreaProfileOnUse && areaProfile != null) {
            areaProfile.Enter(player, sourceId, DisplayName, context != null ? context : this, null, applyConsequences: true);
        }

        PublishVenueEvent(blocked ? blockedEvent : usedEvent, blocked ? "blocked" : "used", purpose, player, sourceId, registration, roster, blocked, failureMessage, context);
        return record;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public string ResolveSceneName() {
        if(!string.IsNullOrWhiteSpace(sceneName)) {
            return sceneName;
        }

        if(regionInfo != null && !string.IsNullOrWhiteSpace(regionInfo.SceneName)) {
            return regionInfo.SceneName;
        }

        if(worldRegion != null && !string.IsNullOrWhiteSpace(worldRegion.DefaultSceneName)) {
            return worldRegion.DefaultSceneName;
        }

        return string.Empty;
    }

    public string ResolveLocationKey() {
        if(!string.IsNullOrWhiteSpace(locationKey)) {
            return locationKey;
        }

        if(mapMarker != null) {
            return mapMarker.Id;
        }

        if(activityZone != null) {
            return activityZone.Id;
        }

        return Id;
    }

    bool PassesAccess(PlayerController player, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required for venue access.";
            return false;
        }

        var log = player.GetComponent<PlayerCompetitionVenueLog>();
        if(log != null && !log.CanUse(this, null, repeatMode, CooldownHours, MaxUseCount, out failureMessage)) {
            return false;
        }

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

    bool MatchesTags(IReadOnlyList<string> requiredTags, Func<string, bool> hasTag) {
        var activeTags = requiredTags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() ?? new List<string>();
        if(activeTags.Count == 0) {
            return true;
        }

        if(tagMatchMode == ConsequenceRequirementMatchMode.Any) {
            return activeTags.Any(hasTag);
        }

        return activeTags.All(hasTag);
    }

    void PublishVenueEvent(GameEventDefinition eventDefinition, string phase, CompetitionVenuePurpose purpose, PlayerController player, string sourceId, CompetitionRegistrationDefinition registration, CompetitionRosterDefinition roster, bool blocked, string failureMessage, UnityEngine.Object context) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"competition-venue.{phase}.{Id}.{purpose}",
            blocked ? $"{DisplayName} blocked: {failureMessage}" : $"{DisplayName} {phase}.",
            GameEventCategory.BattleRule,
            blocked ? GameEventImportance.Warning : GameEventImportance.Info,
            context != null ? context : player != null ? player : this,
            "CompetitionVenueDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("venueId", Id),
            GameEventPublishing.Value("venueName", DisplayName),
            GameEventPublishing.Value("kind", kind),
            GameEventPublishing.Value("purpose", purpose),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("registrationId", registration != null ? registration.Id : string.Empty),
            GameEventPublishing.Value("rosterId", roster != null ? roster.Id : string.Empty),
            GameEventPublishing.Value("competitionId", roster?.Competition != null ? roster.Competition.Id : registration?.Competition != null ? registration.Competition.Id : string.Empty),
            GameEventPublishing.Value("sceneName", ResolveSceneName()),
            GameEventPublishing.Value("locationKey", ResolveLocationKey()),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("blocked", blocked));
    }
}
