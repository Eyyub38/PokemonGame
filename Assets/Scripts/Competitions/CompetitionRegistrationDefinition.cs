using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CompetitionRegistrationRepeatMode {
    Always,
    OnceEver,
    OncePerRoster,
    OncePerCompetition,
    OncePerSeason,
    OncePerWindow,
    OncePerDay,
    CooldownHours
}

public enum CompetitionRegistrationWindowMode {
    AlwaysOpen,
    AnyOpenWindow,
    AllOpenWindows
}

public enum CompetitionRegistrationInvitationMode {
    NotRequired,
    AnyMatchingInvitation,
    AnyListedInvitation
}

public enum CompetitionRegistrationVenueMode {
    NotRequired,
    AnyMatchingVenue,
    AnyListedVenue
}

[CreateAssetMenu(menuName = "Competitions/Registration Definition")]
public class CompetitionRegistrationDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this registration. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future registration UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation for this registration.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as kanto, frontier, championship, qualifier, weekly or invite-only.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Target")]
    [Tooltip("Roster/bracket this registration enters. Empty means this registration only records/logs access.")]
    [SerializeField] CompetitionRosterDefinition roster;
    [Tooltip("Optional competition override. Empty uses the roster competition.")]
    [SerializeField] CompetitionDefinition competitionOverride;
    [Tooltip("Optional season override. Empty uses the roster season.")]
    [SerializeField] CompetitionSeasonDefinition seasonOverride;
    [Tooltip("Optional ranking override. Empty uses the roster ranking.")]
    [SerializeField] CompetitionRankingDefinition rankingOverride;

    [Header("Access")]
    [Tooltip("If enabled, Roster.CanGenerate must pass before registration.")]
    [SerializeField] bool requireRosterCanGenerate = true;
    [Tooltip("If enabled, the target season must be active according to PlayerCompetitionSeasonLog and calendar data.")]
    [SerializeField] bool requireActiveSeason;
    [Tooltip("If enabled, the player cannot register while an active bracket already exists for this roster.")]
    [SerializeField] bool blockIfActiveBracketExists = true;
    [Tooltip("How additional registration requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Additional activity-style requirements checked before registration.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message shown when registration is blocked and no more specific reason exists.")]
    [TextArea]
    [SerializeField] string lockedMessage = "Registration is not available yet.";

    [Header("Registration Windows")]
    [Tooltip("How assigned registration windows are evaluated. Always Open ignores the window list.")]
    [SerializeField] CompetitionRegistrationWindowMode windowMode = CompetitionRegistrationWindowMode.AlwaysOpen;
    [Tooltip("Editable time/calendar windows that can open or close this registration.")]
    [SerializeField] List<CompetitionRegistrationWindowDefinition> registrationWindows = new List<CompetitionRegistrationWindowDefinition>();

    [Header("Invitation / Qualification")]
    [Tooltip("How invitations, qualifier passes or wildcards are required for this registration.")]
    [SerializeField] CompetitionRegistrationInvitationMode invitationMode = CompetitionRegistrationInvitationMode.NotRequired;
    [Tooltip("Specific invitations accepted by Any Listed Invitation mode.")]
    [SerializeField] List<CompetitionInvitationDefinition> requiredInvitations = new List<CompetitionInvitationDefinition>();
    [Tooltip("If enabled, the selected invitation is used/consumed when registration succeeds.")]
    [SerializeField] bool consumeInvitationOnRegister = true;

    [Header("Venue / Arena")]
    [Tooltip("How venues, arenas, gyms or stadiums are required for this registration.")]
    [SerializeField] CompetitionRegistrationVenueMode venueMode = CompetitionRegistrationVenueMode.NotRequired;
    [Tooltip("Specific venues accepted by Any Listed Venue mode.")]
    [SerializeField] List<CompetitionVenueDefinition> requiredVenues = new List<CompetitionVenueDefinition>();
    [Tooltip("If enabled, the selected venue is written to PlayerCompetitionVenueLog when registration succeeds.")]
    [SerializeField] bool recordVenueOnRegister = true;

    [Header("Costs")]
    [Tooltip("Money removed from Wallet when registration succeeds.")]
    [Min(0f)]
    [SerializeField] float moneyCost;
    [Tooltip("Inventory items consumed when registration succeeds.")]
    [SerializeField] List<ActivityItemCost> itemCosts = new List<ActivityItemCost>();

    [Header("Repeat")]
    [Tooltip("How often this registration can be made.")]
    [SerializeField] CompetitionRegistrationRepeatMode repeatMode = CompetitionRegistrationRepeatMode.OncePerRoster;
    [Tooltip("Maximum total registrations for this definition. 0 means no total cap.")]
    [Min(0)]
    [SerializeField] int maxRegistrationCount;
    [Tooltip("In-game hours before registration can be made again when Repeat Mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours;

    [Header("Result")]
    [Tooltip("If enabled, registration immediately generates a bracket through PlayerCompetitionBracketLog.")]
    [SerializeField] bool generateBracketOnRegister = true;
    [Tooltip("Seed used when generating a bracket. 0 lets the roster choose a deterministic seed.")]
    [SerializeField] int fixedBracketSeed;
    [Tooltip("If enabled, the linked competition receives a started record when registration succeeds.")]
    [SerializeField] bool recordCompetitionStartedOnRegister;

    [Header("Events")]
    [Tooltip("Optional event published when registration succeeds.")]
    [SerializeField] GameEventDefinition registeredEvent;
    [Tooltip("Optional event published when registration is blocked.")]
    [SerializeField] GameEventDefinition blockedEvent;
    [Tooltip("If enabled, generated registration events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, registration events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public CompetitionRosterDefinition Roster => roster;
    public CompetitionDefinition Competition => competitionOverride != null ? competitionOverride : roster != null ? roster.Competition : null;
    public CompetitionSeasonDefinition Season => seasonOverride != null ? seasonOverride : roster != null ? roster.Season : null;
    public CompetitionRankingDefinition Ranking => rankingOverride != null ? rankingOverride : roster != null ? roster.Ranking : null;
    public bool RequireRosterCanGenerate => requireRosterCanGenerate;
    public bool RequireActiveSeason => requireActiveSeason;
    public bool BlockIfActiveBracketExists => blockIfActiveBracketExists;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public CompetitionRegistrationWindowMode WindowMode => windowMode;
    public IReadOnlyList<CompetitionRegistrationWindowDefinition> RegistrationWindows => registrationWindows != null ? (IReadOnlyList<CompetitionRegistrationWindowDefinition>)registrationWindows : Array.Empty<CompetitionRegistrationWindowDefinition>();
    public CompetitionRegistrationInvitationMode InvitationMode => invitationMode;
    public IReadOnlyList<CompetitionInvitationDefinition> RequiredInvitations => requiredInvitations != null ? (IReadOnlyList<CompetitionInvitationDefinition>)requiredInvitations : Array.Empty<CompetitionInvitationDefinition>();
    public bool ConsumeInvitationOnRegister => consumeInvitationOnRegister;
    public CompetitionRegistrationVenueMode VenueMode => venueMode;
    public IReadOnlyList<CompetitionVenueDefinition> RequiredVenues => requiredVenues != null ? (IReadOnlyList<CompetitionVenueDefinition>)requiredVenues : Array.Empty<CompetitionVenueDefinition>();
    public bool RecordVenueOnRegister => recordVenueOnRegister;
    public float MoneyCost => Mathf.Max(0f, moneyCost);
    public IReadOnlyList<ActivityItemCost> ItemCosts => itemCosts != null ? (IReadOnlyList<ActivityItemCost>)itemCosts : Array.Empty<ActivityItemCost>();
    public CompetitionRegistrationRepeatMode RepeatMode => repeatMode;
    public int MaxRegistrationCount => Mathf.Max(0, maxRegistrationCount);
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public bool GenerateBracketOnRegister => generateBracketOnRegister;
    public int FixedBracketSeed => fixedBracketSeed;

    public bool CanRegister(PlayerController player, out string failureMessage) {
        return CanRegisterInternal(player, out failureMessage, out _, out _, out _);
    }

    bool CanRegisterInternal(PlayerController player, out string failureMessage, out CompetitionRegistrationWindowDefinition registrationWindow, out CompetitionVenueDefinition venue, out CompetitionInvitationDefinition invitation) {
        registrationWindow = null;
        venue = null;
        invitation = null;
        if(player == null) {
            failureMessage = "A player is required to register.";
            return false;
        }

        if(!TryResolveOpenWindow(player, out registrationWindow, out failureMessage)) {
            return false;
        }

        if(!TryResolveVenue(player, out venue, out failureMessage)) {
            return false;
        }

        if(!TryResolveInvitation(player, registrationWindow, out invitation, out failureMessage)) {
            return false;
        }

        var context = new CompetitionRegistrationContext(this, player, sourceId: null, moneyPaid: 0f, registrationWindow, invitation, venue);
        var log = player.GetComponent<PlayerCompetitionRegistrationLog>();
        if(log != null && !log.CanRegister(this, context.BuildContextKey(repeatMode), out failureMessage)) {
            return false;
        }

        var bracketLog = player.GetComponent<PlayerCompetitionBracketLog>();
        if(blockIfActiveBracketExists && roster != null && bracketLog != null && bracketLog.GetActiveBracket(roster) != null) {
            failureMessage = $"{roster.DisplayName} already has an active bracket.";
            return false;
        }

        if(requireRosterCanGenerate && roster != null && !roster.CanGenerate(player, out failureMessage)) {
            return false;
        }

        if(requireActiveSeason && Season != null && !(player.GetComponent<PlayerCompetitionSeasonLog>()?.IsActive(Season) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{Season.DisplayName} is not active." : lockedMessage;
            return false;
        }

        if(!RequirementsMet(player, out failureMessage)) {
            return false;
        }

        if(MoneyCost > 0f && (Wallet.i == null || !Wallet.i.HasMoney(MoneyCost))) {
            failureMessage = $"You need {MoneyCost:0} money for {DisplayName}.";
            return false;
        }

        var inventory = Inventory.GetInventory();
        foreach(var cost in ItemCosts) {
            if(cost == null || cost.item == null || cost.count <= 0) {
                continue;
            }

            int count = Mathf.Max(1, cost.count);
            if(inventory == null || !inventory.HasItemEnough(cost.item, count)) {
                failureMessage = $"You need {count}x {cost.item.Name} for {DisplayName}.";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public bool TryRegister(PlayerController player, string sourceId, out PlayerCompetitionBracketState bracketState, out string failureMessage) {
        bracketState = null;
        if(!CanRegisterInternal(player, out failureMessage, out var registrationWindow, out var venue, out var invitation)) {
            PublishRegistrationEvent(blockedEvent, "blocked", failureMessage, GameEventImportance.Warning, player, sourceId, null, registrationWindow);
            return false;
        }

        if(consumeInvitationOnRegister && invitation != null && !invitation.TryUse(player, this, registrationWindow, sourceId, out failureMessage)) {
            PublishRegistrationEvent(blockedEvent, "blocked", failureMessage, GameEventImportance.Warning, player, sourceId, null, registrationWindow, invitation, venue);
            return false;
        }

        float paidMoney = 0f;
        if(MoneyCost > 0f && Wallet.i != null) {
            Wallet.i.TakeMoney(MoneyCost);
            paidMoney = MoneyCost;
        }

        var inventory = Inventory.GetInventory();
        foreach(var cost in ItemCosts) {
            if(cost != null && cost.item != null && cost.count > 0) {
                inventory?.RemoveItem(cost.item, Mathf.Max(1, cost.count));
            }
        }

        if(recordCompetitionStartedOnRegister && Competition != null) {
            Competition.TryBegin(player, sourceId, out _);
        }

        if(generateBracketOnRegister && roster != null) {
            var bracketLog = player.GetComponent<PlayerCompetitionBracketLog>() ?? player.gameObject.AddComponent<PlayerCompetitionBracketLog>();
            if(bracketLog.GetActiveBracket(roster) == null) {
                bracketLog.GenerateBracket(roster, fixedBracketSeed, sourceId, out bracketState, out _);
            } else {
                bracketState = bracketLog.GetActiveBracket(roster);
            }
        }

        var registrationLog = player.GetComponent<PlayerCompetitionRegistrationLog>() ?? player.gameObject.AddComponent<PlayerCompetitionRegistrationLog>();
        if(recordVenueOnRegister && venue != null) {
            venue.RecordUse(player, CompetitionVenuePurpose.Registration, this, roster, sourceId, this, blocked: false, null);
        }

        var context = new CompetitionRegistrationContext(this, player, sourceId, paidMoney, registrationWindow, invitation, venue);
        var record = registrationLog.RecordRegistration(this, context, bracketState, sourceId);
        PublishRegistrationEvent(registeredEvent, "registered", $"{DisplayName} registered.", GameEventImportance.Success, player, sourceId, record, registrationWindow, invitation, venue);
        failureMessage = null;
        return true;
    }

    public List<CompetitionRegistrationWindowDefinition> GetOpenWindows(PlayerController player) {
        if(windowMode == CompetitionRegistrationWindowMode.AlwaysOpen) {
            return new List<CompetitionRegistrationWindowDefinition>();
        }

        return registrationWindows?
            .Where(window => window != null && window.IsOpen(player, this, out _))
            .ToList() ?? new List<CompetitionRegistrationWindowDefinition>();
    }

    public bool TryResolveOpenWindow(PlayerController player, out CompetitionRegistrationWindowDefinition registrationWindow, out string failureMessage) {
        registrationWindow = null;
        if(windowMode == CompetitionRegistrationWindowMode.AlwaysOpen) {
            failureMessage = null;
            return true;
        }

        var windows = registrationWindows?.Where(window => window != null).ToList() ?? new List<CompetitionRegistrationWindowDefinition>();
        if(windows.Count == 0) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? "No registration window is assigned." : lockedMessage;
            return false;
        }

        if(windowMode == CompetitionRegistrationWindowMode.AnyOpenWindow) {
            string firstFailure = null;
            foreach(var window in windows) {
                if(window.IsOpen(player, this, out var windowFailure)) {
                    registrationWindow = window;
                    failureMessage = null;
                    return true;
                }

                if(string.IsNullOrWhiteSpace(firstFailure)) {
                    firstFailure = windowFailure;
                }
            }

            failureMessage = string.IsNullOrWhiteSpace(firstFailure) ? lockedMessage : firstFailure;
            return false;
        }

        foreach(var window in windows) {
            if(!window.IsOpen(player, this, out failureMessage)) {
                return false;
            }

            registrationWindow ??= window;
        }

        failureMessage = null;
        return true;
    }

    public bool TryResolveVenue(PlayerController player, out CompetitionVenueDefinition venue, out string failureMessage) {
        venue = null;
        if(venueMode == CompetitionRegistrationVenueMode.NotRequired) {
            failureMessage = null;
            return true;
        }

        if(venueMode == CompetitionRegistrationVenueMode.AnyListedVenue) {
            var listedVenues = requiredVenues?.Where(entry => entry != null).ToList() ?? new List<CompetitionVenueDefinition>();
            if(listedVenues.Count == 0) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? "No accepted venues are assigned." : lockedMessage;
                return false;
            }

            foreach(var candidate in listedVenues) {
                if(candidate.CanHost(player, this, out _)) {
                    venue = candidate;
                    failureMessage = null;
                    return true;
                }
            }

            failureMessage = "No listed venue can host this registration right now.";
            return false;
        }

        foreach(var candidate in Resources.LoadAll<CompetitionVenueDefinition>("")) {
            if(candidate != null && candidate.CanHost(player, this, out _)) {
                venue = candidate;
                failureMessage = null;
                return true;
            }
        }

        failureMessage = "No usable venue was found for this registration.";
        return false;
    }

    public bool TryResolveInvitation(PlayerController player, CompetitionRegistrationWindowDefinition registrationWindow, out CompetitionInvitationDefinition invitation, out string failureMessage) {
        invitation = null;
        if(invitationMode == CompetitionRegistrationInvitationMode.NotRequired) {
            failureMessage = null;
            return true;
        }

        var log = player != null ? player.GetComponent<PlayerCompetitionInvitationLog>() : null;
        if(log == null) {
            failureMessage = "Player has no competition invitation log.";
            return false;
        }

        if(invitationMode == CompetitionRegistrationInvitationMode.AnyListedInvitation) {
            var listedInvitations = requiredInvitations?.Where(entry => entry != null).ToList() ?? new List<CompetitionInvitationDefinition>();
            if(listedInvitations.Count == 0) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? "No accepted invitations are assigned." : lockedMessage;
                return false;
            }

            invitation = log.FindUsableInvitation(listedInvitations, this, registrationWindow, out failureMessage);
        } else {
            invitation = log.FindAnyUsableInvitation(this, registrationWindow, out failureMessage);
        }

        if(invitation == null) {
            failureMessage = string.IsNullOrWhiteSpace(failureMessage) ? lockedMessage : failureMessage;
            return false;
        }

        failureMessage = null;
        return true;
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

    void PublishRegistrationEvent(GameEventDefinition eventDefinition, string phase, string message, GameEventImportance importance, PlayerController player, string sourceId, PlayerCompetitionRegistrationRecord record, CompetitionRegistrationWindowDefinition registrationWindow, CompetitionInvitationDefinition invitation = null, CompetitionVenueDefinition venue = null) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"competition-registration.{phase}.{Id}.{record?.contextKey}",
            message,
            GameEventCategory.BattleRule,
            importance,
            player != null ? player : this,
            "CompetitionRegistrationDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("registrationId", Id),
            GameEventPublishing.Value("registrationName", DisplayName),
            GameEventPublishing.Value("rosterId", roster != null ? roster.Id : string.Empty),
            GameEventPublishing.Value("competitionId", Competition != null ? Competition.Id : string.Empty),
            GameEventPublishing.Value("windowId", registrationWindow != null ? registrationWindow.Id : string.Empty),
            GameEventPublishing.Value("invitationId", invitation != null ? invitation.Id : string.Empty),
            GameEventPublishing.Value("venueId", venue != null ? venue.Id : string.Empty),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", sourceId));
    }
}

public class CompetitionRegistrationContext {
    public CompetitionRegistrationDefinition Registration { get; }
    public PlayerController Player { get; }
    public string SourceId { get; }
    public float MoneyPaid { get; }
    public CompetitionRegistrationWindowDefinition Window { get; }
    public CompetitionInvitationDefinition Invitation { get; }
    public CompetitionVenueDefinition Venue { get; }

    public CompetitionRegistrationContext(CompetitionRegistrationDefinition registration, PlayerController player, string sourceId, float moneyPaid, CompetitionRegistrationWindowDefinition window = null, CompetitionInvitationDefinition invitation = null, CompetitionVenueDefinition venue = null) {
        Registration = registration;
        Player = player;
        SourceId = sourceId;
        MoneyPaid = moneyPaid;
        Window = window;
        Invitation = invitation;
        Venue = venue;
    }

    public string BuildContextKey(CompetitionRegistrationRepeatMode repeatMode) {
        var roster = Registration != null ? Registration.Roster : null;
        var competition = Registration != null ? Registration.Competition : null;
        var season = Registration != null ? Registration.Season : null;

        int currentDay = TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day) : 0;
        return repeatMode switch {
            CompetitionRegistrationRepeatMode.OnceEver => "ever",
            CompetitionRegistrationRepeatMode.OncePerRoster => $"roster:{roster?.Id}",
            CompetitionRegistrationRepeatMode.OncePerCompetition => $"competition:{competition?.Id}",
            CompetitionRegistrationRepeatMode.OncePerSeason => $"season:{season?.Id}",
            CompetitionRegistrationRepeatMode.OncePerWindow => $"window:{(Window != null ? Window.BuildOccurrenceKey() : $"day:{currentDay}")}:{roster?.Id}",
            CompetitionRegistrationRepeatMode.OncePerDay => $"day:{currentDay}:{roster?.Id}",
            CompetitionRegistrationRepeatMode.CooldownHours => $"cooldown:{roster?.Id}:{competition?.Id}",
            _ => $"{Registration?.Id}:{Guid.NewGuid():N}"
        };
    }
}
