using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PlayerLifestyleCategory {
    General,
    Trainer,
    Researcher,
    Farmer,
    Caretaker,
    Crafter,
    Merchant,
    Explorer,
    Social,
    Contest,
    Law,
    Survival,
    Custom
}

public enum LifestyleActivityRuleMode {
    AnyActivity,
    SpecificActivity,
    ActivityTag,
    ExperienceSource
}

[CreateAssetMenu(menuName = "Player/Lifestyles/Lifestyle Definition")]
public class PlayerLifestyleDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this lifestyle profile. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining what kind of play this lifestyle represents.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad lifestyle group used by requirements and future UI filters.")]
    [SerializeField] PlayerLifestyleCategory category = PlayerLifestyleCategory.General;
    [Tooltip("Free-form tags such as professor, care, stealth, merchant, battle, gathering or social.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Optional icon used by future profile, PokeNav or New Game summary UI.")]
    [SerializeField] Sprite icon = null;

    [Header("Scoring")]
    [Tooltip("Maximum points this lifestyle can store. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxPoints = 0;
    [Tooltip("If enabled, only the first matching activity rule gives points. If disabled, all matching rules add together.")]
    [SerializeField] bool stopAfterFirstMatchingActivityRule = true;
    [Tooltip("Rules that convert completed activities into lifestyle points.")]
    [SerializeField] List<LifestyleActivityRule> activityRules = new List<LifestyleActivityRule>();
    [Tooltip("Optional rank bands for future UI and requirements. Highest reached required point wins.")]
    [SerializeField] List<LifestyleRankDefinition> ranks = new List<LifestyleRankDefinition>();

    [Header("Events")]
    [Tooltip("Optional event published when this lifestyle gains or loses points. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition changedEvent = null;
    [Tooltip("If enabled, lifestyle events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = false;
    [Tooltip("If enabled, lifestyle events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog = false;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public PlayerLifestyleCategory Category => category;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public Sprite Icon => icon;
    public int MaxPoints => Mathf.Max(0, maxPoints);
    public bool StopAfterFirstMatchingActivityRule => stopAfterFirstMatchingActivityRule;
    public IReadOnlyList<LifestyleActivityRule> ActivityRules => activityRules != null ? (IReadOnlyList<LifestyleActivityRule>)activityRules : Array.Empty<LifestyleActivityRule>();
    public IReadOnlyList<LifestyleRankDefinition> Ranks => ranks != null ? (IReadOnlyList<LifestyleRankDefinition>)ranks : Array.Empty<LifestyleRankDefinition>();

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public int ClampPoints(int points) {
        points = Mathf.Max(0, points);
        return MaxPoints > 0 ? Mathf.Min(MaxPoints, points) : points;
    }

    public int GetActivityPoints(ActivityDefinition activity) {
        if(activity == null || activityRules == null) {
            return 0;
        }

        int total = 0;
        foreach(var rule in activityRules) {
            if(rule == null || !rule.Enabled || !rule.Matches(activity)) {
                continue;
            }

            total += rule.Points;
            if(stopAfterFirstMatchingActivityRule) {
                break;
            }
        }

        return total;
    }

    public LifestyleRankDefinition GetRankForPoints(int points) {
        if(ranks == null || ranks.Count == 0) {
            return null;
        }

        return ranks
            .Where(rank => rank != null && points >= rank.RequiredPoints)
            .OrderByDescending(rank => rank.RequiredPoints)
            .FirstOrDefault();
    }

    public int GetRankIndexForPoints(int points) {
        if(ranks == null || ranks.Count == 0) {
            return -1;
        }

        int bestIndex = -1;
        int bestPoints = int.MinValue;
        for(int i = 0; i < ranks.Count; i++) {
            var rank = ranks[i];
            if(rank == null || points < rank.RequiredPoints || rank.RequiredPoints < bestPoints) {
                continue;
            }

            bestIndex = i;
            bestPoints = rank.RequiredPoints;
        }

        return bestIndex;
    }

    public void PublishChanged(PlayerController player, PlayerLifestyleState state, int delta, string sourceId, string sourceName, UnityEngine.Object context) {
        if(state == null || delta == 0) {
            return;
        }

        string sign = delta > 0 ? "+" : string.Empty;
        GameEventPublishing.PublishOptional(
            changedEvent,
            $"lifestyle.changed.{Id}",
            $"{DisplayName} lifestyle {sign}{delta} points.",
            GameEventCategory.RPG,
            delta > 0 ? GameEventImportance.Info : GameEventImportance.Warning,
            context != null ? context : player,
            "PlayerLifestyleDefinition",
            GameEventScope.Player,
            showEventsInFeed,
            writeEventsToDebugLog,
            GameEventPublishing.Value("lifestyleId", Id),
            GameEventPublishing.Value("lifestyleName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("delta", delta),
            GameEventPublishing.Value("points", state.points),
            GameEventPublishing.Value("rank", state.rankName),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("sourceName", sourceName));
    }
}

[Serializable]
public class LifestyleActivityRule {
    [Tooltip("If disabled, this rule is skipped.")]
    [SerializeField] bool enabled = true;
    [Tooltip("How this rule checks a completed activity.")]
    [SerializeField] LifestyleActivityRuleMode mode = LifestyleActivityRuleMode.ActivityTag;
    [Tooltip("Activity checked by Specific Activity mode.")]
    [SerializeField] ActivityDefinition activity = null;
    [Tooltip("Tag checked by Activity Tag mode.")]
    [SerializeField] string activityTag = string.Empty;
    [Tooltip("Experience source checked by Experience Source mode.")]
    [SerializeField] PlayerExperienceSource experienceSource = PlayerExperienceSource.Exploration;
    [Tooltip("Lifestyle points added when this rule matches. Negative values can reduce this lifestyle.")]
    [SerializeField] int points = 1;

    public bool Enabled => enabled;
    public LifestyleActivityRuleMode Mode => mode;
    public ActivityDefinition Activity => activity;
    public string ActivityTag => activityTag;
    public PlayerExperienceSource ExperienceSource => experienceSource;
    public int Points => points;

    public bool Matches(ActivityDefinition candidate) {
        if(candidate == null) {
            return false;
        }

        return mode switch {
            LifestyleActivityRuleMode.AnyActivity => true,
            LifestyleActivityRuleMode.SpecificActivity => activity != null && candidate == activity,
            LifestyleActivityRuleMode.ActivityTag => !string.IsNullOrWhiteSpace(activityTag) && candidate.HasTag(activityTag),
            LifestyleActivityRuleMode.ExperienceSource => candidate.ExperienceSource == experienceSource,
            _ => false
        };
    }
}

[Serializable]
public class LifestyleRankDefinition {
    [Tooltip("Stable id for this rank band.")]
    [SerializeField] string rankId = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses Rank Id.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Minimum lifestyle points required for this rank.")]
    [Min(0)]
    [SerializeField] int requiredPoints = 0;
    [Tooltip("Designer note for what this rank means.")]
    [TextArea]
    [SerializeField] string description = string.Empty;

    public string RankId => string.IsNullOrWhiteSpace(rankId) ? DisplayName : rankId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? rankId : displayName;
    public int RequiredPoints => Mathf.Max(0, requiredPoints);
    public string Description => description;
}

[Serializable]
public class LifestylePointGrant {
    [Tooltip("Lifestyle profile that receives points.")]
    public PlayerLifestyleDefinition lifestyle;
    [Tooltip("Points added to the lifestyle. Negative values reduce points.")]
    public int points = 1;
    [Tooltip("Optional source id stored in lifestyle history. Empty uses the source component/id.")]
    public string sourceId;
    [Tooltip("Optional source name stored in lifestyle history. Empty uses the source component/name.")]
    public string sourceName;
}
