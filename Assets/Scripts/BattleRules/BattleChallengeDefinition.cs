using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BattleChallengeKind {
    Trainer,
    Tournament,
    Gym,
    Contest,
    Research,
    Police,
    Club,
    Custom
}

[CreateAssetMenu(menuName = "Battle Rules/Challenge Definition")]
public class BattleChallengeDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this battle challenge. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in future challenge UI. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer or player-facing explanation of this challenge.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad kind used by filters, dialog and future UI styling.")]
    [SerializeField] BattleChallengeKind kind = BattleChallengeKind.Trainer;
    [Tooltip("Free-form tags used by access checks, dialog and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Rules")]
    [Tooltip("Default rule set used when no specific rule is selected.")]
    [SerializeField] BattleRuleSetDefinition defaultRuleSet;
    [Tooltip("Optional rule sets that can be negotiated or selected for this challenge.")]
    [SerializeField] List<BattleRuleSetDefinition> alternativeRuleSets = new List<BattleRuleSetDefinition>();
    [Tooltip("If enabled, the default rule is included in the selectable rule list.")]
    [SerializeField] bool includeDefaultRuleInAlternatives = true;
    [Tooltip("If enabled, the player's party must pass the selected rule before battle starts.")]
    [SerializeField] bool validatePlayerParty = true;

    [Header("Battle Mode")]
    [Tooltip("Default battle mode used by this challenge when no player/explicit mode is selected. Empty means classic current behavior.")]
    [SerializeField] BattleModeDefinition defaultBattleMode;
    [Tooltip("Battle modes allowed by this challenge. Empty means any accessible mode may be used.")]
    [SerializeField] List<BattleModeDefinition> allowedBattleModes = new List<BattleModeDefinition>();
    [Tooltip("If enabled, PlayerBattleModeSettings can choose the mode when it passes access and allowed-mode checks.")]
    [SerializeField] bool allowPlayerPreferredBattleMode = true;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this challenge can start.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this challenge can start.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this challenge.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional calendar event that must be active for this challenge.")]
    [SerializeField] CalendarEventDefinition requiredActiveCalendarEvent;
    [Tooltip("Message shown when access fails and no more specific reason exists.")]
    [SerializeField] string lockedMessage = "This challenge is not available yet.";

    [Header("Completion Rewards")]
    [Tooltip("Reputation changes applied when this challenge is won.")]
    [SerializeField] List<ReputationChange> winReputationChanges = new List<ReputationChange>();
    [Tooltip("Milestones completed when this challenge is won.")]
    [SerializeField] List<MilestoneDefinition> winMilestones = new List<MilestoneDefinition>();
    [Tooltip("Titles, badges, permits or ranks granted when this challenge is won.")]
    [SerializeField] List<TitleGrant> winTitleGrants = new List<TitleGrant>();
    [Tooltip("Career points awarded whenever this challenge completes, win or lose.")]
    [SerializeField] List<CareerPointGrant> completionCareerPointRewards = new List<CareerPointGrant>();
    [Tooltip("Career points awarded only when this challenge is won.")]
    [SerializeField] List<CareerPointGrant> winCareerPointRewards = new List<CareerPointGrant>();
    [Tooltip("Life path XP, branch progress and tag counters awarded whenever this challenge completes, win or lose.")]
    [SerializeField] List<LifePathReward> completionLifePathRewards = new List<LifePathReward>();
    [Tooltip("Life path XP, branch progress and tag counters awarded only when this challenge is won.")]
    [SerializeField] List<LifePathReward> winLifePathRewards = new List<LifePathReward>();
    [Tooltip("Organization memberships granted whenever this challenge completes, win or lose.")]
    [SerializeField] List<OrganizationMembershipGrant> completionOrganizationMembershipRewards = new List<OrganizationMembershipGrant>();
    [Tooltip("Organization points awarded whenever this challenge completes, win or lose.")]
    [SerializeField] List<OrganizationPointGrant> completionOrganizationPointRewards = new List<OrganizationPointGrant>();
    [Tooltip("Organization memberships granted only when this challenge is won.")]
    [SerializeField] List<OrganizationMembershipGrant> winOrganizationMembershipRewards = new List<OrganizationMembershipGrant>();
    [Tooltip("Organization points awarded only when this challenge is won.")]
    [SerializeField] List<OrganizationPointGrant> winOrganizationPointRewards = new List<OrganizationPointGrant>();
    [Tooltip("Optional calendar event completed when this challenge is won.")]
    [SerializeField] CalendarEventDefinition calendarEventToCompleteOnWin;

    [Header("Events")]
    [Tooltip("Optional event published when this challenge starts.")]
    [SerializeField] GameEventDefinition startedEvent;
    [Tooltip("Optional event published when this challenge is completed.")]
    [SerializeField] GameEventDefinition completedEvent;
    [Tooltip("If enabled, challenge events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, challenge events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public BattleChallengeKind Kind => kind;
    public IReadOnlyList<string> Tags => tags;
    public BattleRuleSetDefinition DefaultRuleSet => defaultRuleSet;
    public IReadOnlyList<BattleRuleSetDefinition> AlternativeRuleSets => alternativeRuleSets;
    public bool ValidatePlayerParty => validatePlayerParty;
    public BattleModeDefinition DefaultBattleMode => defaultBattleMode;
    public IReadOnlyList<BattleModeDefinition> AllowedBattleModes => allowedBattleModes;
    public bool AllowPlayerPreferredBattleMode => allowPlayerPreferredBattleMode;
    public IReadOnlyList<CareerPointGrant> CompletionCareerPointRewards => completionCareerPointRewards;
    public IReadOnlyList<CareerPointGrant> WinCareerPointRewards => winCareerPointRewards;
    public IReadOnlyList<LifePathReward> CompletionLifePathRewards => completionLifePathRewards;
    public IReadOnlyList<LifePathReward> WinLifePathRewards => winLifePathRewards;
    public IReadOnlyList<OrganizationMembershipGrant> CompletionOrganizationMembershipRewards => completionOrganizationMembershipRewards;
    public IReadOnlyList<OrganizationPointGrant> CompletionOrganizationPointRewards => completionOrganizationPointRewards;
    public IReadOnlyList<OrganizationMembershipGrant> WinOrganizationMembershipRewards => winOrganizationMembershipRewards;
    public IReadOnlyList<OrganizationPointGrant> WinOrganizationPointRewards => winOrganizationPointRewards;

    public List<BattleRuleSetDefinition> GetAvailableRuleSets(PlayerController player) {
        var rules = new List<BattleRuleSetDefinition>();
        if(includeDefaultRuleInAlternatives && defaultRuleSet != null) {
            rules.Add(defaultRuleSet);
        }

        if(alternativeRuleSets != null) {
            rules.AddRange(alternativeRuleSets.Where(rule => rule != null));
        }

        return rules
            .Distinct()
            .Where(rule => rule.CanAccess(player, out _))
            .ToList();
    }

    public List<BattleModeDefinition> GetAvailableBattleModes(PlayerController player) {
        var modes = new List<BattleModeDefinition>();
        if(defaultBattleMode != null) {
            modes.Add(defaultBattleMode);
        }

        if(allowedBattleModes != null) {
            modes.AddRange(allowedBattleModes.Where(mode => mode != null));
        }

        return modes
            .Distinct()
            .Where(mode => mode.CanAccess(player, out _))
            .ToList();
    }

    public BattleRuleSetDefinition ResolveRuleSet(BattleRuleSetDefinition selectedRuleSet) {
        if(selectedRuleSet != null) {
            return selectedRuleSet;
        }

        return defaultRuleSet;
    }

    public BattleModeDefinition ResolveBattleMode(PlayerController player, BattleModeDefinition selectedMode) {
        if(selectedMode != null && IsBattleModeAllowed(player, selectedMode)) {
            return selectedMode;
        }

        if(allowPlayerPreferredBattleMode) {
            var preferred = player != null ? player.GetComponent<PlayerBattleModeSettings>()?.ResolvePreferredMode(player, this) : null;
            if(preferred != null && IsBattleModeAllowed(player, preferred)) {
                return preferred;
            }
        }

        return IsBattleModeAllowed(player, defaultBattleMode) ? defaultBattleMode : null;
    }

    public bool IsBattleModeAllowed(PlayerController player, BattleModeDefinition mode) {
        if(mode == null) {
            return true;
        }

        if(allowedBattleModes != null && allowedBattleModes.Count > 0 && !allowedBattleModes.Contains(mode)) {
            return false;
        }

        return mode.CanAccess(player, out _);
    }

    public bool CanStart(PlayerController player, BattleRuleSetDefinition selectedRuleSet, out string failureMessage) {
        if(!PassesAccess(player, out failureMessage)) {
            return false;
        }

        var ruleSet = ResolveRuleSet(selectedRuleSet);
        if(ruleSet == null) {
            failureMessage = "This challenge has no battle rule set.";
            return false;
        }

        if(!ruleSet.CanAccess(player, out failureMessage)) {
            return false;
        }

        if(validatePlayerParty) {
            var party = player != null ? player.GetComponent<PokemonParty>() : null;
            if(!ruleSet.ValidateParty(party, out var report)) {
                failureMessage = report.FirstIssue;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public void ApplyCompletionRewards(PlayerController player, bool won) {
        if(player == null) {
            return;
        }

        player.GetComponent<PlayerCareerLog>()?.ApplyPointGrants(completionCareerPointRewards, $"battle-challenge:{Id}");
        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(completionLifePathRewards, $"battle-challenge:{Id}", DisplayName, this);
        var organizationLog = player.GetComponent<PlayerOrganizationLog>();
        organizationLog?.ApplyMembershipGrants(completionOrganizationMembershipRewards, $"battle-challenge:{Id}");
        organizationLog?.ApplyPointGrants(completionOrganizationPointRewards, $"battle-challenge:{Id}");

        if(!won) {
            return;
        }

        player.GetComponent<PlayerReputation>()?.ApplyChanges(winReputationChanges);
        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(winMilestones);
        player.GetComponent<PlayerTitles>()?.ApplyGrants(winTitleGrants, this);
        player.GetComponent<PlayerCareerLog>()?.ApplyPointGrants(winCareerPointRewards, $"battle-challenge-win:{Id}");
        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(winLifePathRewards, $"battle-challenge-win:{Id}", DisplayName, this);
        organizationLog?.ApplyMembershipGrants(winOrganizationMembershipRewards, $"battle-challenge-win:{Id}");
        organizationLog?.ApplyPointGrants(winOrganizationPointRewards, $"battle-challenge-win:{Id}");

        if(calendarEventToCompleteOnWin != null) {
            player.GetComponent<PlayerCalendarLog>()?.CompleteEvent(calendarEventToCompleteOnWin, Id);
        }
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, System.StringComparison.OrdinalIgnoreCase));
    }

    public void PublishStarted(PlayerController player, BattleRuleSetDefinition ruleSet, string sourceId = null, BattleModeDefinition battleMode = null) {
        PublishChallengeEvent(startedEvent, "started", $"{DisplayName} started.", GameEventImportance.Info, player, ruleSet, battleMode, sourceId, won: null);
    }

    public void PublishCompleted(PlayerController player, BattleRuleSetDefinition ruleSet, bool won, string sourceId = null, BattleModeDefinition battleMode = null) {
        PublishChallengeEvent(completedEvent, "completed", $"{DisplayName} completed.", won ? GameEventImportance.Success : GameEventImportance.Info, player, ruleSet, battleMode, sourceId, won);
    }

    bool PassesAccess(PlayerController player, out string failureMessage) {
        if(requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredMilestone != null && !(player?.GetComponent<PlayerMilestones>()?.HasMilestone(requiredMilestone) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredMilestone.DisplayName} first." : lockedMessage;
            return false;
        }

        if(requiredFaction != null) {
            int reputation = player?.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0;
            if(reputation < requiredReputation) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more reputation with {requiredFaction.DisplayName}." : lockedMessage;
                return false;
            }
        }

        if(requiredActiveCalendarEvent != null && !requiredActiveCalendarEvent.IsActiveNow()) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{requiredActiveCalendarEvent.Title} is not active right now." : lockedMessage;
            return false;
        }

        failureMessage = null;
        return true;
    }

    void PublishChallengeEvent(GameEventDefinition eventDefinition, string phase, string message, GameEventImportance importance, PlayerController player, BattleRuleSetDefinition ruleSet, BattleModeDefinition battleMode, string sourceId, bool? won) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"battle-challenge.{phase}.{Id}",
            message,
            GameEventCategory.BattleRule,
            importance,
            player != null ? player : this,
            "BattleChallengeDefinition",
            GameEventScope.Battle,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("challengeId", Id),
            GameEventPublishing.Value("challengeName", DisplayName),
            GameEventPublishing.Value("kind", kind),
            GameEventPublishing.Value("ruleId", ruleSet != null ? ruleSet.Id : string.Empty),
            GameEventPublishing.Value("battleModeId", battleMode != null ? battleMode.Id : string.Empty),
            GameEventPublishing.Value("battleModeName", battleMode != null ? battleMode.DisplayName : string.Empty),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("won", won.HasValue ? won.Value.ToString() : string.Empty));
    }
}
