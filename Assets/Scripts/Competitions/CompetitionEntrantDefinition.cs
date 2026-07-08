using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CompetitionEntrantKind {
    Trainer,
    Rival,
    GymLeader,
    EliteFour,
    FrontierBrain,
    RegionalChampion,
    WorldChampion,
    WildCard,
    PlayerProxy,
    Custom
}

[CreateAssetMenu(menuName = "Competitions/Entrant Definition")]
public class CompetitionEntrantDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this entrant. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future bracket, roster or match UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation for this entrant.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad entrant kind used by filters and future UI.")]
    [SerializeField] CompetitionEntrantKind kind = CompetitionEntrantKind.Trainer;
    [Tooltip("Optional portrait/icon used by future bracket UI.")]
    [SerializeField] Sprite portrait;
    [Tooltip("Free-form tags such as kanto, elite-four, champion, rival, frontier or water-specialist.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Battle Data")]
    [Tooltip("Battle challenge used when this entrant becomes the player's opponent.")]
    [SerializeField] BattleChallengeDefinition challenge;
    [Tooltip("Default battle rule set used against this entrant if no bracket/rules override is selected.")]
    [SerializeField] BattleRuleSetDefinition defaultRuleSet;
    [Tooltip("Optional trainer party template for future runtime trainer generation.")]
    [SerializeField] TrainerPartyTemplateDefinition partyTemplate;
    [Tooltip("Small deterministic offset applied when generating this entrant party from a bracket seed.")]
    [SerializeField] int partySeedOffset;
    [Tooltip("Optional seeded rank used by fixed order or bracket sorting. Lower values appear earlier.")]
    [SerializeField] int seededRank;

    [Header("Availability")]
    [Tooltip("If disabled, this entrant is ignored by roster generation.")]
    [SerializeField] bool selectable = true;
    [Tooltip("If enabled, this entrant can appear only once in a generated roster.")]
    [SerializeField] bool unique = true;
    [Tooltip("Weighted random selection weight. 0 prevents weighted selection unless fixed order is used.")]
    [Min(0)]
    [SerializeField] int selectionWeight = 1;
    [Tooltip("Optional world region this entrant belongs to.")]
    [SerializeField] WorldRegionDefinition worldRegion;
    [Tooltip("Optional title, badge, permit or rank required before this entrant can appear.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this entrant can appear.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional honor required before this entrant can appear.")]
    [SerializeField] CompetitionHonorDefinition requiredHonor;
    [Tooltip("Optional ranking required before this entrant can appear.")]
    [SerializeField] CompetitionRankingDefinition requiredRanking;
    [Tooltip("Optional ranking tier id required before this entrant can appear.")]
    [SerializeField] string requiredRankingTierId = string.Empty;
    [Tooltip("How additional requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Additional activity-style requirements checked before this entrant can appear.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public CompetitionEntrantKind Kind => kind;
    public Sprite Portrait => portrait;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public BattleChallengeDefinition Challenge => challenge;
    public BattleRuleSetDefinition DefaultRuleSet => defaultRuleSet;
    public TrainerPartyTemplateDefinition PartyTemplate => partyTemplate;
    public int PartySeedOffset => partySeedOffset;
    public int SeededRank => seededRank;
    public bool Selectable => selectable;
    public bool Unique => unique;
    public int SelectionWeight => Mathf.Max(0, selectionWeight);
    public WorldRegionDefinition WorldRegion => worldRegion;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();

    public bool CanSelect(PlayerController player, out string failureMessage) {
        if(!selectable) {
            failureMessage = $"{DisplayName} is not selectable.";
            return false;
        }

        if(requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = $"You need {requiredTitle.DisplayName}.";
            return false;
        }

        if(requiredMilestone != null && !(player?.GetComponent<PlayerMilestones>()?.HasMilestone(requiredMilestone) ?? false)) {
            failureMessage = $"You need {requiredMilestone.DisplayName} first.";
            return false;
        }

        if(requiredHonor != null && !(player?.GetComponent<PlayerCompetitionHonorLog>()?.HasHonor(requiredHonor) ?? false)) {
            failureMessage = $"You need {requiredHonor.DisplayName}.";
            return false;
        }

        if(requiredRanking != null) {
            var rankingLog = player != null ? player.GetComponent<PlayerCompetitionRankingLog>() : null;
            if(!string.IsNullOrWhiteSpace(requiredRankingTierId)) {
                if(!(rankingLog?.HasReachedTier(requiredRanking, requiredRankingTierId) ?? false)) {
                    failureMessage = $"You need a higher rank in {requiredRanking.DisplayName}.";
                    return false;
                }
            } else if((rankingLog?.GetCurrentPoints(requiredRanking) ?? 0) <= 0) {
                failureMessage = $"You need progress in {requiredRanking.DisplayName}.";
                return false;
            }
        }

        if(!RequirementsMet(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public PlayerCompetitionBracketEntrantRecord CreateRecord(int seed, int slotIndex) {
        return new PlayerCompetitionBracketEntrantRecord {
            entrantId = Id,
            entrantName = DisplayName,
            kind = kind,
            slotIndex = Mathf.Max(0, slotIndex),
            challengeId = challenge != null ? challenge.Id : string.Empty,
            challengeName = challenge != null ? challenge.DisplayName : string.Empty,
            ruleSetId = defaultRuleSet != null ? defaultRuleSet.Id : string.Empty,
            ruleSetName = defaultRuleSet != null ? defaultRuleSet.DisplayName : string.Empty,
            partyTemplateId = partyTemplate != null ? partyTemplate.Id : string.Empty,
            partyTemplateName = partyTemplate != null ? partyTemplate.DisplayName : string.Empty,
            partySeed = seed + partySeedOffset + slotIndex,
            seededRank = seededRank,
            selectionWeight = SelectionWeight,
            tags = Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct().ToList(),
            defeated = false,
            isPlayer = false
        };
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

            failureMessage = activeRequirements.FirstOrDefault()?.FailureMessage ?? "Entrant requirements are not met.";
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
}
