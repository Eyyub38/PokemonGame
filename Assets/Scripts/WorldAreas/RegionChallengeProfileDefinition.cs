using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RegionPartyTransferMode {
    KeepCurrentParty,
    OnePokemonOnly,
    StorePartyExceptSelected,
    LocalPokemonOnly,
    Custom
}

[CreateAssetMenu(menuName = "World Regions/Region Challenge Profile")]
public class RegionChallengeProfileDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this region challenge. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future travel/challenge UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining how this challenge should feel.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as league, nuzlocke, local-only, beginner or postgame.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Party Rules")]
    [Tooltip("How the player's party should be treated when this challenge starts.")]
    [SerializeField] RegionPartyTransferMode partyTransferMode = RegionPartyTransferMode.KeepCurrentParty;
    [Tooltip("Maximum number of Pokemon allowed in the active roster. 0 disables this metadata rule.")]
    [Min(0)]
    [SerializeField] int maxRosterPokemon;
    [Tooltip("If enabled, only the selected Pokemon instance is recorded as allowed at challenge start.")]
    [SerializeField] bool lockSelectedPokemon;
    [Tooltip("If enabled, the current party Pokemon instance ids are recorded as the challenge roster.")]
    [SerializeField] bool lockCurrentPartyRoster;
    [Tooltip("If enabled, future systems should treat PC/storage access as blocked until the challenge is completed.")]
    [SerializeField] bool lockStorageUntilCompleted;
    [Tooltip("If enabled, future encounter/reward systems can require Pokemon caught in this world region.")]
    [SerializeField] bool onlyLocalPokemonAllowed;
    [Tooltip("Maximum Pokemon level recommended/allowed for this region challenge. 0 disables this metadata rule.")]
    [Min(0)]
    [SerializeField] int levelCap;

    [Header("Battle And Event Links")]
    [Tooltip("Optional battle rule set used by this region challenge.")]
    [SerializeField] BattleRuleSetDefinition battleRuleSet;
    [Tooltip("Optional calendar event that represents this challenge or league.")]
    [SerializeField] CalendarEventDefinition calendarEvent;
    [Tooltip("Optional title, badge or permit granted when this challenge is completed.")]
    [SerializeField] List<TitleGrant> completionTitleGrants = new List<TitleGrant>();
    [Tooltip("Milestones completed when this challenge is completed.")]
    [SerializeField] List<MilestoneDefinition> completionMilestones = new List<MilestoneDefinition>();
    [Tooltip("Faction reputation changes applied when this challenge is completed.")]
    [SerializeField] List<ReputationChange> completionReputationChanges = new List<ReputationChange>();
    [Tooltip("Relationship changes applied when this challenge is completed.")]
    [SerializeField] List<RelationshipChange> completionRelationshipChanges = new List<RelationshipChange>();

    [Header("Access")]
    [Tooltip("How custom requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Requirements checked before this challenge can start.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message shown when this challenge cannot start.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This region challenge is not available yet.";

    [Header("Events")]
    [Tooltip("Optional event published when this challenge starts. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition startedEvent;
    [Tooltip("Optional event published when this challenge completes. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition completedEvent;
    [Tooltip("If enabled, generated challenge events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, generated challenge events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public RegionPartyTransferMode PartyTransferMode => partyTransferMode;
    public int MaxRosterPokemon => Mathf.Max(0, maxRosterPokemon);
    public bool LockSelectedPokemon => lockSelectedPokemon;
    public bool LockCurrentPartyRoster => lockCurrentPartyRoster;
    public bool LockStorageUntilCompleted => lockStorageUntilCompleted;
    public bool OnlyLocalPokemonAllowed => onlyLocalPokemonAllowed;
    public int LevelCap => Mathf.Max(0, levelCap);
    public BattleRuleSetDefinition BattleRuleSet => battleRuleSet;
    public CalendarEventDefinition CalendarEvent => calendarEvent;
    public IReadOnlyList<TitleGrant> CompletionTitleGrants => completionTitleGrants != null ? (IReadOnlyList<TitleGrant>)completionTitleGrants : Array.Empty<TitleGrant>();
    public IReadOnlyList<MilestoneDefinition> CompletionMilestones => completionMilestones != null ? (IReadOnlyList<MilestoneDefinition>)completionMilestones : Array.Empty<MilestoneDefinition>();
    public IReadOnlyList<ReputationChange> CompletionReputationChanges => completionReputationChanges != null ? (IReadOnlyList<ReputationChange>)completionReputationChanges : Array.Empty<ReputationChange>();
    public IReadOnlyList<RelationshipChange> CompletionRelationshipChanges => completionRelationshipChanges != null ? (IReadOnlyList<RelationshipChange>)completionRelationshipChanges : Array.Empty<RelationshipChange>();
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();

    public bool CanStart(PlayerController player, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to start a region challenge.";
            return false;
        }

        if(battleRuleSet != null && !battleRuleSet.CanAccess(player, out failureMessage)) {
            return false;
        }

        if(!RequirementsMet(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public List<string> BuildAllowedPokemonIds(PokemonParty party, Pokemon selectedPokemon) {
        var allowed = new List<string>();
        if(lockSelectedPokemon || partyTransferMode == RegionPartyTransferMode.OnePokemonOnly || partyTransferMode == RegionPartyTransferMode.StorePartyExceptSelected) {
            if(selectedPokemon != null) {
                allowed.Add(selectedPokemon.InstanceId);
            }
            return allowed;
        }

        if(lockCurrentPartyRoster) {
            foreach(var pokemon in party?.Pokemons ?? new List<Pokemon>()) {
                if(pokemon != null) {
                    allowed.Add(pokemon.InstanceId);
                }
            }
        }

        return allowed.Distinct().ToList();
    }

    public void ApplyCompletionRewards(PlayerController player, UnityEngine.Object context = null) {
        if(player == null) {
            return;
        }

        player.GetComponent<PlayerTitles>()?.ApplyGrants(completionTitleGrants, context);
        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(completionMilestones);
        player.GetComponent<PlayerReputation>()?.ApplyChanges(completionReputationChanges);
        player.GetComponent<PlayerRelationships>()?.ApplyChanges(completionRelationshipChanges);
    }

    public void PublishStarted(PlayerController player, WorldRegionDefinition region, UnityEngine.Object context = null) {
        PublishChallengeEvent(startedEvent, "started", GameEventImportance.Info, player, region, context);
    }

    public void PublishCompleted(PlayerController player, WorldRegionDefinition region, UnityEngine.Object context = null) {
        PublishChallengeEvent(completedEvent, "completed", GameEventImportance.Success, player, region, context);
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

    void PublishChallengeEvent(GameEventDefinition eventDefinition, string phase, GameEventImportance importance, PlayerController player, WorldRegionDefinition region, UnityEngine.Object context) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"region-challenge.{phase}.{Id}",
            $"{DisplayName} {phase}.",
            GameEventCategory.Activity,
            importance,
            context != null ? context : player,
            "RegionChallengeProfileDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("challengeId", Id),
            GameEventPublishing.Value("challengeName", DisplayName),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("regionId", region != null ? region.Id : string.Empty),
            GameEventPublishing.Value("regionName", region != null ? region.DisplayName : string.Empty));
    }
}
