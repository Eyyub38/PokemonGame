using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum ChronicleCategory {
    General,
    Story,
    Discovery,
    Travel,
    Battle,
    Activity,
    Research,
    PokemonCare,
    Shop,
    Social,
    Law,
    Investigation,
    Rumor,
    WorldEvent,
    Debug,
    Custom
}

[CreateAssetMenu(menuName = "Chronicle/Entry Definition")]
public class ChronicleEntryDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this chronicle entry. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining when this entry should be recorded.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad category used by future UI filters and requirements.")]
    [SerializeField] ChronicleCategory category = ChronicleCategory.General;
    [Tooltip("Free-form tags such as story, rumor, lab, first-visit, crime, discovery or debug.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Default Content")]
    [Tooltip("Default short title stored in the chronicle. Empty uses Display Name.")]
    [SerializeField] string title = string.Empty;
    [Tooltip("Default body text stored in the chronicle.")]
    [TextArea]
    [SerializeField] string message = string.Empty;
    [Tooltip("Importance used by future UI styling and filters.")]
    [SerializeField] GameEventImportance importance = GameEventImportance.Info;
    [Tooltip("If enabled, future UI can keep this entry visible when normal entries are cleared.")]
    [SerializeField] bool pinnedByDefault;

    [Header("Context")]
    [Tooltip("Optional region connected to this entry.")]
    [SerializeField] RegionInfoDefinition region = null;
    [Tooltip("Optional map marker connected to this entry.")]
    [SerializeField] MapMarkerDefinition mapMarker = null;
    [Tooltip("Optional Pokemon connected to this entry.")]
    [SerializeField] PokemonBase relatedPokemon = null;
    [Tooltip("Optional scene name connected to this entry. Empty uses the active scene when recording.")]
    [SerializeField] string sceneName = string.Empty;
    [Tooltip("Optional custom location key such as route, building, room, board or area id.")]
    [SerializeField] string locationKey = string.Empty;

    [Header("Repeat Rules")]
    [Tooltip("How often this entry can be recorded for the current player.")]
    [SerializeField] ConsequenceChainRepeatMode repeatMode = ConsequenceChainRepeatMode.Unlimited;
    [Tooltip("Cooldown in in-game hours when Repeat Mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours;
    [Tooltip("Maximum successful records for this entry definition. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxRecordCount;
    [Tooltip("If enabled, successful entries are stored in PlayerChronicleLog.")]
    [SerializeField] bool recordHistory = true;
    [Tooltip("If enabled, blocked entry attempts are also stored in PlayerChronicleLog.")]
    [SerializeField] bool recordBlockedAttempts;

    [Header("Requirements")]
    [Tooltip("How entry-level requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Requirements checked before this entry can be recorded.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();

    [Header("Linked Consequences")]
    [Tooltip("Consequence chains applied after this entry is recorded. These are skipped when a consequence chain records the entry to avoid accidental loops.")]
    [SerializeField] List<ConsequenceChainDefinition> entryChains = new List<ConsequenceChainDefinition>();

    [Header("Events")]
    [Tooltip("Optional event published when this entry is recorded. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition recordedEvent = null;
    [Tooltip("Optional event published when this entry is blocked. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition blockedEvent = null;
    [Tooltip("If enabled, chronicle events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed;
    [Tooltip("If enabled, chronicle events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public ChronicleCategory Category => category;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public string Title => string.IsNullOrWhiteSpace(title) ? DisplayName : title;
    public string Message => message;
    public GameEventImportance Importance => importance;
    public bool PinnedByDefault => pinnedByDefault;
    public RegionInfoDefinition Region => region;
    public MapMarkerDefinition MapMarker => mapMarker;
    public PokemonBase RelatedPokemon => relatedPokemon;
    public string SceneName => sceneName;
    public string LocationKey => locationKey;
    public ConsequenceChainRepeatMode RepeatMode => repeatMode;
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public int MaxRecordCount => Mathf.Max(0, maxRecordCount);
    public bool RecordHistory => recordHistory;
    public bool RecordBlockedAttempts => recordBlockedAttempts;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public IReadOnlyList<ConsequenceChainDefinition> EntryChains => entryChains != null ? (IReadOnlyList<ConsequenceChainDefinition>)entryChains : Array.Empty<ConsequenceChainDefinition>();

    public bool CanRecord(PlayerController player, PlayerChronicleLog log, string sourceId, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required for chronicle entries.";
            return false;
        }

        if(log != null && !log.CanRecord(this, sourceId, repeatMode, CooldownHours, MaxRecordCount, out failureMessage)) {
            return false;
        }

        if(!ConsequenceChainDefinition.RequirementsMet(player, requirements, requirementMatchMode, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public ChronicleEntryResult Apply(
        PlayerController player,
        string sourceId = null,
        string sourceName = null,
        UnityEngine.Object context = null,
        string titleOverride = null,
        string messageOverride = null,
        bool applyConsequences = true
    ) {
        var result = new ChronicleEntryResult(
            Id,
            DisplayName,
            ResolveTitle(titleOverride),
            ResolveMessage(messageOverride),
            category,
            importance,
            NormalizeSourceId(sourceId),
            sourceName,
            ResolveSceneName(),
            ResolveLocationKey());

        var log = player != null ? player.GetComponent<PlayerChronicleLog>() ?? player.gameObject.AddComponent<PlayerChronicleLog>() : null;
        if(!CanRecord(player, log, result.sourceId, out var failureMessage)) {
            result.blocked = true;
            result.failureMessage = failureMessage;
            if(recordHistory && recordBlockedAttempts) {
                result.record = log?.RecordEntry(this, result.sourceId, sourceName, result);
            }

            PublishChronicleEvent(blockedEvent, "blocked", result, player, context, GameEventImportance.Warning);
            return result;
        }

        if(recordHistory) {
            result.record = log?.RecordEntry(this, result.sourceId, sourceName, result);
        }

        if(applyConsequences) {
            ApplyEntryChains(player, result, context);
            SyncRecordWithResult(result);
        }

        PublishChronicleEvent(recordedEvent, "recorded", result, player, context, importance);
        return result;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    string ResolveTitle(string titleOverride) {
        return string.IsNullOrWhiteSpace(titleOverride) ? Title : titleOverride;
    }

    string ResolveMessage(string messageOverride) {
        return string.IsNullOrWhiteSpace(messageOverride) ? message : messageOverride;
    }

    string ResolveSceneName() {
        if(!string.IsNullOrWhiteSpace(sceneName)) {
            return sceneName;
        }

        if(region != null && !string.IsNullOrWhiteSpace(region.SceneName)) {
            return region.SceneName;
        }

        return SceneManager.GetActiveScene().name;
    }

    string ResolveLocationKey() {
        if(!string.IsNullOrWhiteSpace(locationKey)) {
            return locationKey;
        }

        if(mapMarker != null) {
            return mapMarker.Id;
        }

        if(region != null) {
            return region.Id;
        }

        return Id;
    }

    void ApplyEntryChains(PlayerController player, ChronicleEntryResult result, UnityEngine.Object context) {
        if(player == null) {
            return;
        }

        var chainContext = new ConsequenceChainContext {
            SourceId = result.sourceId,
            SourceName = result.sourceName,
            Region = region,
            ContextObject = context != null ? context : this
        };

        foreach(var chain in EntryChains) {
            if(chain == null) {
                result.skippedChains++;
                continue;
            }

            var chainResult = chain.Apply(player, chainContext, context != null ? context : this);
            if(chainResult != null && !chainResult.blocked) {
                result.appliedChains++;
            } else {
                result.blockedChains++;
                if(chainResult != null && !string.IsNullOrWhiteSpace(chainResult.failureMessage)) {
                    result.messages.Add($"{chain.DisplayName}: {chainResult.failureMessage}");
                }
            }
        }
    }

    void SyncRecordWithResult(ChronicleEntryResult result) {
        if(result?.record == null) {
            return;
        }

        result.record.appliedChains = Mathf.Max(0, result.appliedChains);
        result.record.blockedChains = Mathf.Max(0, result.blockedChains);
        result.record.skippedChains = Mathf.Max(0, result.skippedChains);
        result.record.messages = result.messages != null ? result.messages.Where(message => !string.IsNullOrWhiteSpace(message)).ToList() : new List<string>();
    }

    static string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? "chronicle-entry" : sourceId;
    }

    void PublishChronicleEvent(GameEventDefinition eventDefinition, string phase, ChronicleEntryResult result, PlayerController player, UnityEngine.Object context, GameEventImportance eventImportance) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"chronicle.{phase}.{Id}",
            phase == "recorded" ? $"{result?.title ?? DisplayName} recorded." : $"{DisplayName} blocked: {result?.failureMessage}",
            GameEventCategory.Chronicle,
            eventImportance,
            context != null ? context : player != null ? player : this,
            "ChronicleEntryDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("chronicleEntryId", Id),
            GameEventPublishing.Value("chronicleEntryName", DisplayName),
            GameEventPublishing.Value("chronicleRecordId", result?.record?.recordId ?? string.Empty),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", result != null ? result.sourceId : string.Empty),
            GameEventPublishing.Value("sceneName", result != null ? result.sceneName : string.Empty),
            GameEventPublishing.Value("locationKey", result != null ? result.locationKey : string.Empty),
            GameEventPublishing.Value("region", region != null ? region.Id : string.Empty),
            GameEventPublishing.Value("mapMarker", mapMarker != null ? mapMarker.Id : string.Empty),
            GameEventPublishing.Value("blocked", result != null && result.blocked));
    }
}

public class ChronicleEntryResult {
    public readonly string entryId;
    public readonly string entryName;
    public readonly string title;
    public readonly string message;
    public readonly ChronicleCategory category;
    public readonly GameEventImportance importance;
    public readonly string sourceId;
    public readonly string sourceName;
    public readonly string sceneName;
    public readonly string locationKey;
    public PlayerChronicleRecord record;
    public int appliedChains;
    public int blockedChains;
    public int skippedChains;
    public bool blocked;
    public string failureMessage;
    public readonly List<string> messages = new List<string>();

    public ChronicleEntryResult(string entryId, string entryName, string title, string message, ChronicleCategory category, GameEventImportance importance, string sourceId, string sourceName, string sceneName, string locationKey) {
        this.entryId = entryId;
        this.entryName = entryName;
        this.title = title;
        this.message = message;
        this.category = category;
        this.importance = importance;
        this.sourceId = sourceId;
        this.sourceName = sourceName;
        this.sceneName = sceneName;
        this.locationKey = locationKey;
    }
}
