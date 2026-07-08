using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokeNavFeedItemType {
    General,
    PokemonSighting,
    TrainerSighting,
    EventNotice,
    MarketNotice,
    ResearchLead,
    PoliceNotice,
    TransitUpdate,
    ContestNotice,
    CompetitionNotice,
    Rumor,
    MapPin,
    NavigationHint,
    Custom
}

public enum PokeNavFeedRepeatMode {
    AlwaysRefreshOrAdd,
    OnceEver,
    RefreshExistingOnly
}

[CreateAssetMenu(menuName = "PokeNav/Feed Item Definition")]
public class PokeNavFeedItemDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this PokeNav feed item. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Short title shown by future PokeNav feed UI. Empty uses the asset name.")]
    [SerializeField] string title = string.Empty;
    [Tooltip("Author, source, channel or organization shown by future feed UI.")]
    [SerializeField] string sourceName = string.Empty;
    [Tooltip("Main feed text shown by future PokeNav UI.")]
    [TextArea]
    [SerializeField] string body = string.Empty;
    [Tooltip("Broad feed item type used by filters and future UI styling.")]
    [SerializeField] PokeNavFeedItemType feedType = PokeNavFeedItemType.General;
    [Tooltip("Priority used by feed sorting and notification importance.")]
    [SerializeField] NotificationPriority priority = NotificationPriority.Normal;
    [Tooltip("If enabled, future feed UI should highlight or pin this item by default.")]
    [SerializeField] bool pinnedByDefault;
    [Tooltip("Optional icon used by future feed UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags such as route, rare, league, market, police, research or outbreak.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Related Data")]
    [Tooltip("Optional related Pokemon for sightings or Pokedex links.")]
    [SerializeField] PokemonBase relatedPokemon;
    [Tooltip("Knowledge level granted to Related Pokemon when this item is unlocked.")]
    [SerializeField] PokemonKnowledgeLevel pokemonKnowledgeToGrant = PokemonKnowledgeLevel.Unknown;
    [Tooltip("Minimum knowledge level required for Related Pokemon before this item can show.")]
    [SerializeField] PokemonKnowledgeLevel requiredPokemonKnowledge = PokemonKnowledgeLevel.Unknown;
    [Tooltip("Optional related region discovered or highlighted by this item.")]
    [SerializeField] RegionInfoDefinition relatedRegion;
    [Tooltip("Optional PokeNav entry discovered or highlighted by this item.")]
    [SerializeField] PokeNavEntryDefinition relatedPokeNavEntry;
    [Tooltip("Optional social post unlocked or published with this item.")]
    [SerializeField] SocialPostDefinition relatedSocialPost;
    [Tooltip("Optional map marker discovered or highlighted by this item.")]
    [SerializeField] MapMarkerDefinition relatedMapMarker;
    [Tooltip("Optional calendar event revealed by this item.")]
    [SerializeField] CalendarEventDefinition relatedCalendarEvent;
    [Tooltip("Optional world discovery applied by this item.")]
    [SerializeField] WorldDiscoveryDefinition relatedWorldDiscovery;
    [Tooltip("Optional competition linked to this item.")]
    [SerializeField] CompetitionDefinition relatedCompetition;
    [Tooltip("Optional shop catalog linked to this item.")]
    [SerializeField] ShopCatalogDefinition relatedShop;
    [Tooltip("Optional transit route linked to this item.")]
    [SerializeField] TransitRouteDefinition relatedTransitRoute;
    [Tooltip("Optional navigation hint linked to this item.")]
    [SerializeField] NavigationHintDefinition relatedNavigationHint;
    [Tooltip("Optional encounter table linked to this item.")]
    [SerializeField] EncounterTableDefinition relatedEncounterTable;

    [Header("Visibility")]
    [Tooltip("If enabled, this item can appear without being explicitly unlocked in PlayerPokeNavFeedLog.")]
    [SerializeField] bool visibleByDefault;
    [Tooltip("How repeated unlocks of this item are handled.")]
    [SerializeField] PokeNavFeedRepeatMode repeatMode = PokeNavFeedRepeatMode.AlwaysRefreshOrAdd;
    [Tooltip("If enabled, this item expires after Default Duration Hours once unlocked.")]
    [SerializeField] bool expiresAfterUnlock;
    [Tooltip("In-game hours this item remains active when Expires After Unlock is enabled.")]
    [Min(0)]
    [SerializeField] int defaultDurationHours = 24;
    [Tooltip("If enabled, unlocking this item again refreshes its expiration time.")]
    [SerializeField] bool refreshExpirationOnUnlock = true;
    [Tooltip("If enabled, unlocking this item marks it unread even if it was read earlier.")]
    [SerializeField] bool markUnreadOnUnlock = true;
    [Tooltip("If enabled, Related Calendar Event must be visible through its own rules before this item shows.")]
    [SerializeField] bool requireCalendarEventVisible;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this item can be unlocked or shown.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this item can be unlocked or shown.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this item.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional region that must be discovered in PokeNav first.")]
    [SerializeField] RegionInfoDefinition requiredDiscoveredRegion;
    [Tooltip("Optional PokeNav entry that must be discovered first.")]
    [SerializeField] PokeNavEntryDefinition requiredDiscoveredEntry;
    [Tooltip("Optional world event whose active state gates this item.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent;
    [Tooltip("Expected active state for Required World Event.")]
    [SerializeField] bool requiredWorldEventActive = true;
    [Tooltip("Allowed day periods. Empty means any time.")]
    [SerializeField] List<DayPeriod> allowedPeriods = new List<DayPeriod>();
    [Tooltip("How extra requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Extra activity-style requirements checked before this item can unlock or show.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message/debug reason used when this feed item is blocked.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This PokeNav feed item is not available yet.";

    [Header("Apply Options")]
    [Tooltip("If enabled, Related Pokemon knowledge is written to PlayerPokeNavLog when this item unlocks.")]
    [SerializeField] bool recordPokemonKnowledge = true;
    [Tooltip("If enabled, Related Region is discovered when this item unlocks.")]
    [SerializeField] bool discoverRegion = true;
    [Tooltip("If enabled, Related PokeNav Entry is discovered when this item unlocks.")]
    [SerializeField] bool discoverPokeNavEntry = true;
    [Tooltip("If enabled, Related Social Post is unlocked and optionally published when this item unlocks.")]
    [SerializeField] bool unlockSocialPost = true;
    [Tooltip("If enabled, Related Map Marker is discovered when this item unlocks.")]
    [SerializeField] bool discoverMapMarker = true;
    [Tooltip("If enabled, Related Calendar Event is revealed when this item unlocks.")]
    [SerializeField] bool revealCalendarEvent = true;
    [Tooltip("If enabled, Related World Discovery is applied when this item unlocks.")]
    [SerializeField] bool applyWorldDiscovery;

    [Header("Events")]
    [Tooltip("Optional event published when this feed item unlocks. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition unlockedEvent;
    [Tooltip("If enabled, unlocking this item publishes a NotificationFeed entry.")]
    [SerializeField] bool publishNotification = true;
    [Tooltip("If enabled, feed item events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string Title => string.IsNullOrWhiteSpace(title) ? name : title;
    public string SourceName => sourceName;
    public string Body => body;
    public PokeNavFeedItemType FeedType => feedType;
    public NotificationPriority Priority => priority;
    public bool PinnedByDefault => pinnedByDefault;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public PokemonBase RelatedPokemon => relatedPokemon;
    public PokemonKnowledgeLevel PokemonKnowledgeToGrant => pokemonKnowledgeToGrant;
    public PokemonKnowledgeLevel RequiredPokemonKnowledge => requiredPokemonKnowledge;
    public RegionInfoDefinition RelatedRegion => relatedRegion;
    public PokeNavEntryDefinition RelatedPokeNavEntry => relatedPokeNavEntry;
    public SocialPostDefinition RelatedSocialPost => relatedSocialPost;
    public MapMarkerDefinition RelatedMapMarker => relatedMapMarker;
    public CalendarEventDefinition RelatedCalendarEvent => relatedCalendarEvent;
    public WorldDiscoveryDefinition RelatedWorldDiscovery => relatedWorldDiscovery;
    public CompetitionDefinition RelatedCompetition => relatedCompetition;
    public ShopCatalogDefinition RelatedShop => relatedShop;
    public TransitRouteDefinition RelatedTransitRoute => relatedTransitRoute;
    public NavigationHintDefinition RelatedNavigationHint => relatedNavigationHint;
    public EncounterTableDefinition RelatedEncounterTable => relatedEncounterTable;
    public bool VisibleByDefault => visibleByDefault;
    public PokeNavFeedRepeatMode RepeatMode => repeatMode;
    public bool ExpiresAfterUnlock => expiresAfterUnlock;
    public int DefaultDurationHours => Mathf.Max(0, defaultDurationHours);
    public bool RefreshExpirationOnUnlock => refreshExpirationOnUnlock;
    public bool MarkUnreadOnUnlock => markUnreadOnUnlock;
    public bool RequireCalendarEventVisible => requireCalendarEventVisible;
    public IReadOnlyList<DayPeriod> AllowedPeriods => allowedPeriods != null ? (IReadOnlyList<DayPeriod>)allowedPeriods : Array.Empty<DayPeriod>();
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public bool RecordPokemonKnowledge => recordPokemonKnowledge;
    public bool DiscoverRegion => discoverRegion;
    public bool DiscoverPokeNavEntry => discoverPokeNavEntry;
    public bool UnlockSocialPost => unlockSocialPost;
    public bool DiscoverMapMarker => discoverMapMarker;
    public bool RevealCalendarEvent => revealCalendarEvent;
    public bool ApplyWorldDiscovery => applyWorldDiscovery;

    public bool CanUnlock(PlayerController player, PlayerPokeNavFeedLog log, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to unlock PokeNav feed items.";
            return false;
        }

        if(log != null && !log.CanUnlock(this, out failureMessage)) {
            return false;
        }

        return PassesAccess(player, log, out failureMessage);
    }

    public bool CanShow(PlayerController player, PlayerPokeNavFeedLog log, out string failureMessage) {
        failureMessage = null;
        if(!visibleByDefault && (log == null || !log.HasActiveItem(this, out failureMessage))) {
            failureMessage = string.IsNullOrWhiteSpace(failureMessage)
                ? string.IsNullOrWhiteSpace(lockedMessage) ? $"{Title} is not unlocked." : lockedMessage
                : failureMessage;
            return false;
        }

        if(visibleByDefault && log != null && !log.IsActiveOrUnowned(this, out failureMessage)) {
            return false;
        }

        return PassesAccess(player, log, out failureMessage);
    }

    public bool TryUnlock(PlayerController player, string sourceId, bool applyLinks, bool publish, out PokeNavFeedItemRecord record, out string failureMessage) {
        record = null;
        var log = player != null ? player.GetComponent<PlayerPokeNavFeedLog>() : null;
        if(!CanUnlock(player, log, out failureMessage)) {
            return false;
        }

        log = player.GetComponent<PlayerPokeNavFeedLog>() ?? player.gameObject.AddComponent<PlayerPokeNavFeedLog>();
        record = log.RecordUnlock(this, sourceId);

        if(applyLinks) {
            ApplyLinkedData(player, sourceId, publish);
        }

        if(publish) {
            PublishUnlocked(player, sourceId);
        }

        failureMessage = null;
        return true;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    bool PassesAccess(PlayerController player, PlayerPokeNavFeedLog log, out string failureMessage) {
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

        if(requiredDiscoveredRegion != null && !(player?.GetComponent<PlayerPokeNavLog>()?.HasDiscoveredRegion(requiredDiscoveredRegion) ?? requiredDiscoveredRegion.VisibleByDefault)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You have not discovered {requiredDiscoveredRegion.DisplayName} yet." : lockedMessage;
            return false;
        }

        if(requiredDiscoveredEntry != null && !(player?.GetComponent<PlayerPokeNavLog>()?.HasDiscoveredEntry(requiredDiscoveredEntry) ?? requiredDiscoveredEntry.VisibleByDefault)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You have not discovered {requiredDiscoveredEntry.DisplayName} yet." : lockedMessage;
            return false;
        }

        if(requiredWorldEvent != null) {
            bool active = WorldEventManager.i != null && WorldEventManager.i.IsEventActive(requiredWorldEvent);
            if(active != requiredWorldEventActive) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{Title} is not active right now." : lockedMessage;
                return false;
            }
        }

        if(allowedPeriods != null && allowedPeriods.Count > 0) {
            DayPeriod current = TimeSystem.i != null ? TimeSystem.i.CurrentPeriod : DayPeriod.None;
            if(!allowedPeriods.Contains(current)) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{Title} is not available at this time." : lockedMessage;
                return false;
            }
        }

        if(relatedPokemon != null && requiredPokemonKnowledge > PokemonKnowledgeLevel.Unknown) {
            var pokeNav = player != null ? player.GetComponent<PlayerPokeNavLog>() : null;
            if(pokeNav == null || pokeNav.GetPokemonKnowledgeLevel(relatedPokemon) < requiredPokemonKnowledge) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more information about {relatedPokemon.Name}." : lockedMessage;
                return false;
            }
        }

        if(requireCalendarEventVisible && relatedCalendarEvent != null) {
            var calendarLog = player != null ? player.GetComponent<PlayerCalendarLog>() : null;
            if(!relatedCalendarEvent.CanShow(player, calendarLog, out failureMessage)) {
                return false;
            }
        }

        if(!ConsequenceChainDefinition.RequirementsMet(player, requirements, requirementMatchMode, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    void ApplyLinkedData(PlayerController player, string sourceId, bool publishLinkedPosts) {
        if(player == null) {
            return;
        }

        string source = $"pokenav-feed:{Id}";
        if(!string.IsNullOrWhiteSpace(sourceId)) {
            source = sourceId;
        }

        var pokeNav = player.GetComponent<PlayerPokeNavLog>() ?? player.gameObject.AddComponent<PlayerPokeNavLog>();
        var mapLog = player.GetComponent<PlayerMapLog>() ?? player.gameObject.AddComponent<PlayerMapLog>();

        if(recordPokemonKnowledge && relatedPokemon != null && pokemonKnowledgeToGrant > PokemonKnowledgeLevel.Unknown) {
            pokeNav.RecordPokemonKnowledge(relatedPokemon, pokemonKnowledgeToGrant, source);
        }

        if(discoverRegion && relatedRegion != null) {
            pokeNav.DiscoverRegion(relatedRegion, out _);
        }

        if(discoverPokeNavEntry && relatedPokeNavEntry != null) {
            pokeNav.DiscoverEntry(relatedPokeNavEntry, out _);
        }

        if(unlockSocialPost && relatedSocialPost != null) {
            pokeNav.UnlockPost(relatedSocialPost, publishLinkedPosts);
        }

        if(discoverMapMarker && relatedMapMarker != null) {
            mapLog.DiscoverMarker(relatedMapMarker, source);
        }

        if(revealCalendarEvent && relatedCalendarEvent != null) {
            relatedCalendarEvent.Reveal(player, source, Title);
        }

        if(applyWorldDiscovery && relatedWorldDiscovery != null) {
            relatedWorldDiscovery.Apply(player, source, Title, this);
        }
    }

    void PublishUnlocked(PlayerController player, string sourceId) {
        GameEventPublishing.PublishOptional(
            unlockedEvent,
            $"pokenav.feed.unlocked.{Id}",
            string.IsNullOrWhiteSpace(body) ? Title : body,
            GameEventCategory.PokeNav,
            ToGameEventImportance(priority),
            player != null ? player : this,
            "PokeNavFeedItemDefinition",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("feedItemId", Id),
            GameEventPublishing.Value("title", Title),
            GameEventPublishing.Value("feedType", feedType),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("relatedPokemon", relatedPokemon != null ? relatedPokemon.name : string.Empty),
            GameEventPublishing.Value("relatedRegion", relatedRegion != null ? relatedRegion.Id : string.Empty),
            GameEventPublishing.Value("relatedMapMarker", relatedMapMarker != null ? relatedMapMarker.Id : string.Empty),
            GameEventPublishing.Value("relatedCalendarEvent", relatedCalendarEvent != null ? relatedCalendarEvent.Id : string.Empty),
            GameEventPublishing.Value("relatedCompetition", relatedCompetition != null ? relatedCompetition.Id : string.Empty));

        if(publishNotification) {
            NotificationFeed.Publish(
                Title,
                string.IsNullOrWhiteSpace(body) ? Title : body,
                ToNotificationKind(feedType),
                priority,
                NotificationChannel.Story,
                "PokeNav",
                sourceEventId: Id,
                pinned: pinnedByDefault,
                values: new[] {
                    GameEventPublishing.Value("feedItemId", Id),
                    GameEventPublishing.Value("feedType", feedType),
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

    NotificationKind ToNotificationKind(PokeNavFeedItemType type) {
        return type switch {
            PokeNavFeedItemType.PokemonSighting => NotificationKind.Activity,
            PokeNavFeedItemType.TrainerSighting => NotificationKind.NPC,
            PokeNavFeedItemType.MarketNotice => NotificationKind.Item,
            PokeNavFeedItemType.PoliceNotice => NotificationKind.Quest,
            PokeNavFeedItemType.ResearchLead => NotificationKind.Activity,
            PokeNavFeedItemType.TransitUpdate => NotificationKind.World,
            PokeNavFeedItemType.EventNotice => NotificationKind.World,
            PokeNavFeedItemType.ContestNotice => NotificationKind.Activity,
            PokeNavFeedItemType.CompetitionNotice => NotificationKind.Battle,
            PokeNavFeedItemType.MapPin => NotificationKind.World,
            PokeNavFeedItemType.NavigationHint => NotificationKind.World,
            _ => NotificationKind.Social
        };
    }
}
