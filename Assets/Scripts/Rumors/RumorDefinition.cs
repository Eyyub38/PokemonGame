using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RumorCategory {
    General,
    PokemonSighting,
    TrainerSighting,
    Event,
    Shop,
    Police,
    Research,
    Transit,
    Contest,
    Club,
    Resource,
    Danger,
    Secret
}

public enum RumorReliability {
    Unknown,
    Low,
    Medium,
    High,
    Confirmed
}

public enum RumorRepeatMode {
    Unlimited,
    OnceEver,
    OncePerSource,
    Daily,
    CooldownHours
}

[CreateAssetMenu(menuName = "Rumors/Rumor Definition")]
public class RumorDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this rumor. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Short title shown in future rumor/PokeNav UI. Empty uses the asset name.")]
    [SerializeField] string title;
    [Tooltip("Short preview text used by future UI lists.")]
    [TextArea]
    [SerializeField] string teaser;
    [Tooltip("Full rumor text shown when the player hears or opens this rumor.")]
    [TextArea]
    [SerializeField] string body;
    [Tooltip("Broad rumor category used by filters and future UI styling.")]
    [SerializeField] RumorCategory category = RumorCategory.General;
    [Tooltip("How reliable this rumor is. UI can show this as a confidence hint.")]
    [SerializeField] RumorReliability reliability = RumorReliability.Unknown;
    [Tooltip("Priority used for sorting and notification importance.")]
    [SerializeField] NotificationPriority priority = NotificationPriority.Normal;
    [Tooltip("If enabled, future UI can pin or highlight this rumor.")]
    [SerializeField] bool important;
    [Tooltip("Free-form tags used by sources, filters, jobs and PokeNav.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Lifecycle")]
    [Tooltip("Optional spread/lifecycle profile. Empty keeps the old always-available rumor behavior.")]
    [SerializeField] RumorSpreadProfileDefinition spreadProfile;
    [Tooltip("Optional importance override. When None, the spread profile importance is used.")]
    [SerializeField] RumorImportanceLevel importanceOverride = RumorImportanceLevel.Local;
    [Tooltip("If enabled, Importance Override is used instead of the spread profile importance.")]
    [SerializeField] bool overrideImportance;
    [Tooltip("Optional region where this rumor begins. Empty uses the spread profile default or source region.")]
    [SerializeField] RegionInfoDefinition originRegion;
    [Tooltip("If enabled, a compatible RumorSource can seed this rumor the first time it is triggered.")]
    [SerializeField] bool seedLifecycleFromSources = true;

    [Header("Repeat Rules")]
    [Tooltip("How often this rumor can be heard.")]
    [SerializeField] RumorRepeatMode repeatMode = RumorRepeatMode.Unlimited;
    [Tooltip("Cooldown in in-game hours when repeat mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours;
    [Tooltip("Maximum total times this rumor can be heard. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHeardCount;

    [Header("Access")]
    [Tooltip("If enabled, this rumor can be heard without being explicitly unlocked in PlayerRumorLog.")]
    [SerializeField] bool unlockedByDefault = true;
    [Tooltip("Optional title, badge, permit or license required before this rumor can be heard.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this rumor can be heard.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this rumor.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional world event whose active state gates this rumor.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent;
    [Tooltip("Expected active state for the required world event.")]
    [SerializeField] bool requiredWorldEventActive = true;
    [Tooltip("If enabled, start/end day limits are checked.")]
    [SerializeField] bool scheduledByDay;
    [Tooltip("First in-game day this rumor can be heard.")]
    [Min(1)]
    [SerializeField] int startDay = 1;
    [Tooltip("Last in-game day this rumor can be heard.")]
    [Min(1)]
    [SerializeField] int endDay = 1;
    [Tooltip("Allowed day periods. Empty means any time.")]
    [SerializeField] List<DayPeriod> allowedPeriods = new List<DayPeriod>();
    [Tooltip("Message/debug reason used when this rumor is blocked.")]
    [SerializeField] string lockedMessage = "This rumor is not available right now.";

    [Header("Related Data")]
    [Tooltip("Optional related Pokemon.")]
    [SerializeField] PokemonBase relatedPokemon;
    [Tooltip("Knowledge level granted for the related Pokemon when this rumor is heard.")]
    [SerializeField] PokemonKnowledgeLevel pokemonKnowledgeToGrant = PokemonKnowledgeLevel.Unknown;
    [Tooltip("Optional related region discovered when this rumor is heard.")]
    [SerializeField] RegionInfoDefinition relatedRegion;
    [Tooltip("Optional related map marker discovered when this rumor is heard.")]
    [SerializeField] MapMarkerDefinition relatedMapMarker;
    [Tooltip("Optional related PokeNav entry discovered when this rumor is heard.")]
    [SerializeField] PokeNavEntryDefinition relatedPokeNavEntry;
    [Tooltip("Optional related social post unlocked/published when this rumor is heard.")]
    [SerializeField] SocialPostDefinition relatedSocialPost;
    [Tooltip("Optional related encounter table shown by future UI/debug.")]
    [SerializeField] EncounterTableDefinition relatedEncounterTable;
    [Tooltip("Optional related shop shown by future UI/debug.")]
    [SerializeField] ShopCatalogDefinition relatedShop;
    [Tooltip("Optional related transit route shown by future UI/debug.")]
    [SerializeField] TransitRouteDefinition relatedTransitRoute;
    [Tooltip("Optional related trainer/NPC name shown by future UI/debug.")]
    [SerializeField] string relatedCharacterName;

    [Header("Effects")]
    [Tooltip("If enabled, related PokeNav/region/social/map data is unlocked when this rumor is heard.")]
    [SerializeField] bool unlockRelatedInfo = true;
    [Tooltip("If enabled, hearing this rumor publishes a NotificationFeed entry.")]
    [SerializeField] bool publishNotification = true;
    [Tooltip("Optional event published when this rumor is heard. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition heardEvent;
    [Tooltip("If enabled, rumor events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string Title => string.IsNullOrWhiteSpace(title) ? name : title;
    public string Teaser => teaser;
    public string Body => body;
    public RumorCategory Category => category;
    public RumorReliability Reliability => reliability;
    public NotificationPriority Priority => priority;
    public bool Important => important;
    public IReadOnlyList<string> Tags => tags;
    public RumorSpreadProfileDefinition SpreadProfile => spreadProfile;
    public RumorImportanceLevel Importance => overrideImportance ? importanceOverride : spreadProfile != null ? spreadProfile.Importance : RumorImportanceLevel.Local;
    public RegionInfoDefinition OriginRegion => originRegion != null ? originRegion : spreadProfile != null ? spreadProfile.DefaultOriginRegion : null;
    public bool SeedLifecycleFromSources => seedLifecycleFromSources;
    public RumorRepeatMode RepeatMode => repeatMode;
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public int MaxHeardCount => Mathf.Max(0, maxHeardCount);
    public bool UnlockedByDefault => unlockedByDefault;
    public PokemonBase RelatedPokemon => relatedPokemon;
    public RegionInfoDefinition RelatedRegion => relatedRegion;
    public MapMarkerDefinition RelatedMapMarker => relatedMapMarker;
    public PokeNavEntryDefinition RelatedPokeNavEntry => relatedPokeNavEntry;
    public SocialPostDefinition RelatedSocialPost => relatedSocialPost;

    public bool CanHear(PlayerController player, PlayerRumorLog log, string sourceId, out string failureMessage) {
        return CanHear(player, log, sourceId, null, out failureMessage);
    }

    public bool CanHear(PlayerController player, PlayerRumorLog log, string sourceId, RumorSource source, out string failureMessage) {
        if(!unlockedByDefault && !(log?.HasUnlockedRumor(this) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{Title} is not unlocked." : lockedMessage;
            return false;
        }

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

        if(requiredWorldEvent != null) {
            bool active = WorldEventManager.i != null && WorldEventManager.i.IsEventActive(requiredWorldEvent);
            if(active != requiredWorldEventActive) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{Title} is not active right now." : lockedMessage;
                return false;
            }
        }

        if(scheduledByDay && TimeSystem.i != null && (TimeSystem.i.Day < startDay || TimeSystem.i.Day > Mathf.Max(startDay, endDay))) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{Title} is not available today." : lockedMessage;
            return false;
        }

        if(allowedPeriods != null && allowedPeriods.Count > 0) {
            DayPeriod current = TimeSystem.i != null ? TimeSystem.i.CurrentPeriod : DayPeriod.None;
            if(!allowedPeriods.Contains(current)) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{Title} is not available at this time." : lockedMessage;
                return false;
            }
        }

        if(log != null && !log.CanHear(this, sourceId, repeatMode, CooldownHours, MaxHeardCount, out failureMessage)) {
            return false;
        }

        if(spreadProfile != null) {
            var lifecycleLog = player != null ? player.GetComponent<PlayerRumorLifecycleLog>() : null;
            if(lifecycleLog == null) {
                failureMessage = $"{Title} has no rumor lifecycle log.";
                return false;
            }

            if(!lifecycleLog.CanHear(this, source, out failureMessage)) {
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public void Apply(PlayerController player, string sourceId, string sourceName) {
        var log = player != null ? player.GetComponent<PlayerRumorLog>() : null;
        log?.RecordHeard(this, sourceId, sourceName);
        player?.GetComponent<PlayerRumorLifecycleLog>()?.GetStage(this);

        if(unlockRelatedInfo && player != null) {
            UnlockRelatedInfo(player);
        }

        PublishHeard(player, sourceId, sourceName);
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    void UnlockRelatedInfo(PlayerController player) {
        var pokeNav = player.GetComponent<PlayerPokeNavLog>();
        var mapLog = player.GetComponent<PlayerMapLog>();

        if(relatedPokemon != null && pokemonKnowledgeToGrant > PokemonKnowledgeLevel.Unknown) {
            pokeNav?.RecordPokemonKnowledge(relatedPokemon, pokemonKnowledgeToGrant, $"rumor:{Id}");
        }

        if(relatedRegion != null) {
            pokeNav?.DiscoverRegion(relatedRegion, out _);
        }

        if(relatedPokeNavEntry != null) {
            pokeNav?.DiscoverEntry(relatedPokeNavEntry, out _);
        }

        if(relatedSocialPost != null) {
            pokeNav?.UnlockPost(relatedSocialPost, publish: true);
        }

        if(relatedMapMarker != null) {
            mapLog?.DiscoverMarker(relatedMapMarker, $"rumor:{Id}");
        }
    }

    void PublishHeard(PlayerController player, string sourceId, string sourceName) {
        GameEventPublishing.PublishOptional(
            heardEvent,
            $"rumor.heard.{Id}",
            string.IsNullOrWhiteSpace(body) ? Title : body,
            GameEventCategory.Rumor,
            ToGameEventImportance(priority),
            player != null ? player : this,
            "RumorDefinition",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("rumorId", Id),
            GameEventPublishing.Value("title", Title),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("reliability", reliability),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("sourceName", sourceName),
            GameEventPublishing.Value("relatedPokemon", relatedPokemon != null ? relatedPokemon.name : string.Empty),
            GameEventPublishing.Value("relatedRegion", relatedRegion != null ? relatedRegion.Id : string.Empty),
            GameEventPublishing.Value("relatedMapMarker", relatedMapMarker != null ? relatedMapMarker.Id : string.Empty),
            GameEventPublishing.Value("relatedCharacter", relatedCharacterName),
            GameEventPublishing.Value("relatedEncounterTable", relatedEncounterTable != null ? relatedEncounterTable.Id : string.Empty),
            GameEventPublishing.Value("relatedShop", relatedShop != null ? relatedShop.Id : string.Empty),
            GameEventPublishing.Value("relatedTransitRoute", relatedTransitRoute != null ? relatedTransitRoute.Id : string.Empty));

        if(publishNotification) {
            NotificationFeed.Publish(
                Title,
                string.IsNullOrWhiteSpace(body) ? teaser : body,
                ToNotificationKind(category),
                priority,
                NotificationChannel.Story,
                "Rumor",
                sourceEventId: Id,
                pinned: important,
                values: new[] {
                    GameEventPublishing.Value("rumorId", Id),
                    GameEventPublishing.Value("rumorCategory", category),
                    GameEventPublishing.Value("rumorReliability", reliability),
                    GameEventPublishing.Value("sourceId", sourceId)
                });
        }
    }

    GameEventImportance ToGameEventImportance(NotificationPriority notificationPriority) {
        return notificationPriority switch {
            NotificationPriority.Low => GameEventImportance.Trace,
            NotificationPriority.High => GameEventImportance.Warning,
            NotificationPriority.Critical => GameEventImportance.Error,
            _ => GameEventImportance.Info
        };
    }

    NotificationKind ToNotificationKind(RumorCategory rumorCategory) {
        return rumorCategory switch {
            RumorCategory.PokemonSighting => NotificationKind.Activity,
            RumorCategory.TrainerSighting => NotificationKind.NPC,
            RumorCategory.Shop => NotificationKind.Item,
            RumorCategory.Police => NotificationKind.Quest,
            RumorCategory.Research => NotificationKind.Activity,
            RumorCategory.Transit => NotificationKind.World,
            RumorCategory.Event => NotificationKind.World,
            RumorCategory.Contest => NotificationKind.Activity,
            RumorCategory.Club => NotificationKind.Social,
            RumorCategory.Danger => NotificationKind.World,
            _ => NotificationKind.Social
        };
    }
}
