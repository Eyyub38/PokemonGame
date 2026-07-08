using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RumorSourceType {
    NPC,
    NoticeBoard,
    PokeNavFeed,
    Radio,
    Shop,
    PoliceStation,
    ResearchLab,
    TransitStation,
    Club,
    Custom
}

public enum RumorShareMode {
    FirstAvailable,
    RandomAvailable,
    AllAvailable
}

public class RumorSource : MonoBehaviour, IPlayerTriggerable {
    [Header("Source")]
    [Tooltip("Optional source id used by save/repeat rules. Empty uses GameObject name.")]
    [SerializeField] string sourceId;
    [Tooltip("Name shown in debug/future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName;
    [Tooltip("Broad source type used by filters and future UI.")]
    [SerializeField] RumorSourceType sourceType = RumorSourceType.NPC;
    [Tooltip("Region where this source belongs. Used by rumor spread/lifecycle rules.")]
    [SerializeField] RegionInfoDefinition region;
    [Tooltip("Free-form tags used by rumor spread rules, such as village, police, market, trainer or professor.")]
    [SerializeField] List<string> sourceTags = new List<string>();
    [Tooltip("Rumors this source can share.")]
    [SerializeField] List<RumorDefinition> rumors = new List<RumorDefinition>();

    [Header("Sharing")]
    [Tooltip("How this source chooses rumors when triggered.")]
    [SerializeField] RumorShareMode shareMode = RumorShareMode.RandomAvailable;
    [Tooltip("If enabled, this source unlocks all listed rumors when the player triggers it.")]
    [SerializeField] bool unlockRumorsOnTrigger;
    [Tooltip("If enabled, lifecycle rumors can start spreading from this source when the player triggers it.")]
    [SerializeField] bool seedLifecycleRumorsOnTrigger = true;
    [Tooltip("If enabled, this source immediately shares rumor content when the player triggers it.")]
    [SerializeField] bool shareOnPlayerTrigger = true;
    [Tooltip("If enabled, repeated player triggers can call OnPlayerTriggered more than once.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this source can be used.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional faction whose reputation gates this source.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Message shown when source access is blocked.")]
    [SerializeField] string lockedMessage = "This rumor source is not available right now.";

    [Header("Debug")]
    [Tooltip("If enabled, share attempts are written to GameEventBus/GameDebugLogger.")]
    [SerializeField] bool logShareAttempts;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public RumorSourceType SourceType => sourceType;
    public RegionInfoDefinition Region => region;
    public IReadOnlyList<string> SourceTags => sourceTags != null ? (IReadOnlyList<string>)sourceTags : System.Array.Empty<string>();
    public IReadOnlyList<RumorDefinition> Rumors => rumors;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        if(player == null) {
            return;
        }

        if(!CanUse(player, out var failureMessage)) {
            PublishSourceEvent(player, "blocked", failureMessage, GameEventImportance.Warning);
            return;
        }

        var log = player.GetComponent<PlayerRumorLog>() ?? player.gameObject.AddComponent<PlayerRumorLog>();
        var lifecycleLog = player.GetComponent<PlayerRumorLifecycleLog>() ?? player.gameObject.AddComponent<PlayerRumorLifecycleLog>();
        if(unlockRumorsOnTrigger) {
            foreach(var rumor in rumors) {
                log.UnlockRumor(rumor, SourceId);
            }
        }

        if(seedLifecycleRumorsOnTrigger) {
            foreach(var rumor in rumors) {
                if(rumor != null && rumor.SpreadProfile != null && rumor.SeedLifecycleFromSources) {
                    lifecycleLog.SeedRumor(rumor, this, $"source:{SourceId}");
                }
            }
        }

        if(shareOnPlayerTrigger) {
            TryShare(player, out _);
        }
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

        failureMessage = null;
        return true;
    }

    public List<RumorDefinition> GetAvailableRumors(PlayerController player) {
        if(player == null || !CanUse(player, out _)) {
            return new List<RumorDefinition>();
        }

        var log = player.GetComponent<PlayerRumorLog>();
        return (rumors ?? new List<RumorDefinition>())
            .Where(rumor => rumor != null && rumor.CanHear(player, log, SourceId, this, out _))
            .OrderByDescending(rumor => rumor.Important)
            .ThenByDescending(rumor => rumor.Priority)
            .ThenBy(rumor => rumor.Title)
            .ToList();
    }

    public bool TryShare(PlayerController player, out List<RumorDefinition> sharedRumors) {
        sharedRumors = new List<RumorDefinition>();
        if(player == null) {
            PublishSourceEvent(null, "blocked", "A player is required to hear rumors.", GameEventImportance.Warning);
            return false;
        }

        if(!CanUse(player, out var failureMessage)) {
            PublishSourceEvent(player, "blocked", failureMessage, GameEventImportance.Warning);
            return false;
        }

        var available = GetAvailableRumors(player);
        if(available.Count == 0) {
            PublishSourceEvent(player, "empty", $"{DisplayName} has no available rumors.", GameEventImportance.Trace);
            return false;
        }

        if(shareMode == RumorShareMode.AllAvailable) {
            sharedRumors.AddRange(available);
        } else if(shareMode == RumorShareMode.FirstAvailable) {
            sharedRumors.Add(available[0]);
        } else {
            sharedRumors.Add(available[Random.Range(0, available.Count)]);
        }

        foreach(var rumor in sharedRumors) {
            rumor.Apply(player, SourceId, DisplayName);
        }

        PublishSourceEvent(player, "shared", $"{DisplayName} shared {sharedRumors.Count} rumor(s).", GameEventImportance.Info);
        return true;
    }

    public bool TryShare(PlayerController player, RumorDefinition rumor, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to hear rumors.";
            return false;
        }

        if(!CanUse(player, out failureMessage)) {
            return false;
        }

        if(rumor == null || !rumors.Contains(rumor)) {
            failureMessage = "This rumor is not available from this source.";
            return false;
        }

        var log = player.GetComponent<PlayerRumorLog>() ?? player.gameObject.AddComponent<PlayerRumorLog>();
        var lifecycleLog = player.GetComponent<PlayerRumorLifecycleLog>() ?? player.gameObject.AddComponent<PlayerRumorLifecycleLog>();
        if(rumor.SpreadProfile != null && rumor.SeedLifecycleFromSources) {
            lifecycleLog.SeedRumor(rumor, this, $"source:{SourceId}");
        }

        if(!rumor.CanHear(player, log, SourceId, this, out failureMessage)) {
            return false;
        }

        rumor.Apply(player, SourceId, DisplayName);
        PublishSourceEvent(player, "shared", $"{DisplayName} shared {rumor.Title}.", GameEventImportance.Info);
        failureMessage = null;
        return true;
    }

    void PublishSourceEvent(PlayerController player, string phase, string message, GameEventImportance importance) {
        if(!logShareAttempts && importance < GameEventImportance.Warning) {
            return;
        }

        GameEventPublishing.PublishOptional(
            null,
            $"rumor-source.{phase}.{SourceId}",
            message,
            GameEventCategory.Rumor,
            importance,
            player != null ? player : this,
            "RumorSource",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: logShareAttempts,
            GameEventPublishing.Value("sourceId", SourceId),
            GameEventPublishing.Value("sourceName", DisplayName),
            GameEventPublishing.Value("sourceType", sourceType),
            GameEventPublishing.Value("phase", phase));
    }

    public bool HasSourceTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && sourceTags != null
            && sourceTags.Any(entry => string.Equals(entry, tag, System.StringComparison.OrdinalIgnoreCase));
    }
}
