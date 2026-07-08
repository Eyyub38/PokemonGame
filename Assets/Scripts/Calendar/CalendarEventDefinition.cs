using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CalendarEventCategory {
    General,
    Festival,
    Tournament,
    Contest,
    MarketSale,
    PokemonMigration,
    PoliceCall,
    Research,
    Transit,
    Club,
    Weather,
    Story,
    Custom
}

public enum CalendarRepeatMode {
    Once,
    Daily,
    EveryNDays,
    Weekly,
    SpecificDays
}

public enum CalendarEventVisibilityMode {
    AlwaysWhenUnlocked,
    OnlyWhenActive,
    VisibleBeforeAndDuring
}

[CreateAssetMenu(menuName = "Calendar/Event Definition")]
public class CalendarEventDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this calendar event. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Short title shown in future calendar/PokeNav UI. Empty uses the asset name.")]
    [SerializeField] string title;
    [Tooltip("Short preview shown in event lists.")]
    [TextArea]
    [SerializeField] string summary;
    [Tooltip("Full event details shown in future calendar UI.")]
    [TextArea]
    [SerializeField] string details;
    [Tooltip("Broad calendar category used by filters and future UI styling.")]
    [SerializeField] CalendarEventCategory category = CalendarEventCategory.General;
    [Tooltip("Priority used for sorting and notifications.")]
    [SerializeField] NotificationPriority priority = NotificationPriority.Normal;
    [Tooltip("If enabled, future UI can pin or highlight this event.")]
    [SerializeField] bool important;
    [Tooltip("Optional icon used by future calendar UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags used by filters, rumors, PokeNav and jobs.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Schedule")]
    [Tooltip("How this event repeats after Start Day.")]
    [SerializeField] CalendarRepeatMode repeatMode = CalendarRepeatMode.Once;
    [Tooltip("First in-game day this event can occur.")]
    [Min(1)]
    [SerializeField] int startDay = 1;
    [Tooltip("If enabled, End Day limits future occurrences.")]
    [SerializeField] bool useEndDay = true;
    [Tooltip("Last in-game day this event can occur when Use End Day is enabled.")]
    [Min(1)]
    [SerializeField] int endDay = 1;
    [Tooltip("Interval in days when Repeat Mode is Every N Days.")]
    [Min(1)]
    [SerializeField] int repeatEveryDays = 1;
    [Tooltip("Weekdays used by Weekly repeat mode. Empty means every weekday is valid.")]
    [SerializeField] List<WeekDay> activeWeekDays = new List<WeekDay>();
    [Tooltip("Specific in-game days used by Specific Days repeat mode.")]
    [SerializeField] List<int> specificDays = new List<int>();
    [Tooltip("Allowed day periods. Empty means any time during a scheduled day.")]
    [SerializeField] List<DayPeriod> activePeriods = new List<DayPeriod>();

    [Header("Visibility")]
    [Tooltip("When this event appears in calendar/PokeNav lists.")]
    [SerializeField] CalendarEventVisibilityMode visibilityMode = CalendarEventVisibilityMode.VisibleBeforeAndDuring;
    [Tooltip("How many in-game days before the next occurrence this event becomes visible.")]
    [Min(0)]
    [SerializeField] int visibleDaysBeforeStart = 3;
    [Tooltip("If enabled, this event can be seen without PlayerCalendarLog unlocking it.")]
    [SerializeField] bool unlockedByDefault = true;
    [Tooltip("If enabled, this event is marked seen when it becomes visible through a source.")]
    [SerializeField] bool markSeenOnReveal = true;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this event can be visible.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this event can be visible.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this event.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional world event whose active state gates this calendar event.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent;
    [Tooltip("Expected active state for the required world event.")]
    [SerializeField] bool requiredWorldEventActive = true;
    [Tooltip("Optional region that must be discovered in PokeNav first.")]
    [SerializeField] RegionInfoDefinition requiredDiscoveredRegion;
    [Tooltip("Optional PokeNav entry that must be discovered first.")]
    [SerializeField] PokeNavEntryDefinition requiredPokeNavEntry;
    [Tooltip("Optional rumor that must be heard before this event appears.")]
    [SerializeField] RumorDefinition requiredRumor;
    [Tooltip("Message/debug reason used when this event is blocked.")]
    [SerializeField] string lockedMessage = "This event is not available yet.";

    [Header("Related Data")]
    [Tooltip("Optional related Pokemon.")]
    [SerializeField] PokemonBase relatedPokemon;
    [Tooltip("Knowledge level granted for the related Pokemon when this event is revealed.")]
    [SerializeField] PokemonKnowledgeLevel pokemonKnowledgeToGrant = PokemonKnowledgeLevel.Unknown;
    [Tooltip("Optional related region discovered when this event is revealed.")]
    [SerializeField] RegionInfoDefinition relatedRegion;
    [Tooltip("Optional PokeNav entry discovered when this event is revealed.")]
    [SerializeField] PokeNavEntryDefinition relatedPokeNavEntry;
    [Tooltip("Optional social post unlocked/published when this event is revealed.")]
    [SerializeField] SocialPostDefinition relatedSocialPost;
    [Tooltip("Optional rumor unlocked when this event is revealed.")]
    [SerializeField] RumorDefinition relatedRumor;
    [Tooltip("Optional map marker discovered when this event is revealed.")]
    [SerializeField] MapMarkerDefinition relatedMapMarker;
    [Tooltip("Optional world event this calendar event represents.")]
    [SerializeField] WorldEventDefinition relatedWorldEvent;
    [Tooltip("Optional shop connected to this event.")]
    [SerializeField] ShopCatalogDefinition relatedShop;
    [Tooltip("Optional transit route connected to this event.")]
    [SerializeField] TransitRouteDefinition relatedTransitRoute;
    [Tooltip("Optional activity connected to this event.")]
    [SerializeField] ActivityDefinition relatedActivity;

    [Header("Events")]
    [Tooltip("Optional event published when this calendar event is revealed. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition revealedEvent;
    [Tooltip("Optional event published when this calendar event is completed. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition completedEvent;
    [Tooltip("If enabled, revealing this event publishes a NotificationFeed entry.")]
    [SerializeField] bool publishNotification = true;
    [Tooltip("If enabled, calendar events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string Title => string.IsNullOrWhiteSpace(title) ? name : title;
    public string Summary => summary;
    public string Details => details;
    public CalendarEventCategory Category => category;
    public NotificationPriority Priority => priority;
    public bool Important => important;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags;
    public CalendarRepeatMode RepeatMode => repeatMode;
    public int StartDay => Mathf.Max(1, startDay);
    public bool UseEndDay => useEndDay;
    public int EndDay => Mathf.Max(StartDay, endDay);
    public int RepeatEveryDays => Mathf.Max(1, repeatEveryDays);
    public IReadOnlyList<WeekDay> ActiveWeekDays => activeWeekDays;
    public IReadOnlyList<int> SpecificDays => specificDays;
    public IReadOnlyList<DayPeriod> ActivePeriods => activePeriods;
    public CalendarEventVisibilityMode VisibilityMode => visibilityMode;
    public int VisibleDaysBeforeStart => Mathf.Max(0, visibleDaysBeforeStart);
    public bool UnlockedByDefault => unlockedByDefault;
    public bool MarkSeenOnReveal => markSeenOnReveal;
    public PokemonBase RelatedPokemon => relatedPokemon;
    public RegionInfoDefinition RelatedRegion => relatedRegion;
    public SocialPostDefinition RelatedSocialPost => relatedSocialPost;
    public RumorDefinition RelatedRumor => relatedRumor;
    public MapMarkerDefinition RelatedMapMarker => relatedMapMarker;

    public bool IsActiveNow() {
        if(TimeSystem.i == null) {
            return IsScheduledForDay(1);
        }

        return IsScheduledForDay(TimeSystem.i.Day)
            && (activePeriods == null || activePeriods.Count == 0 || activePeriods.Contains(TimeSystem.i.CurrentPeriod));
    }

    public bool IsScheduledForDay(int day) {
        day = Mathf.Max(1, day);
        if(day < StartDay) {
            return false;
        }

        if(useEndDay && day > EndDay) {
            return false;
        }

        return repeatMode switch {
            CalendarRepeatMode.Daily => true,
            CalendarRepeatMode.EveryNDays => (day - StartDay) % RepeatEveryDays == 0,
            CalendarRepeatMode.Weekly => activeWeekDays == null || activeWeekDays.Count == 0 || activeWeekDays.Contains(GetWeekDay(day)),
            CalendarRepeatMode.SpecificDays => specificDays != null && specificDays.Contains(day),
            _ => day == StartDay
        };
    }

    public int GetNextOccurrenceDay(int currentDay, int maxLookAheadDays = 30) {
        currentDay = Mathf.Max(1, currentDay);
        int lookAhead = Mathf.Max(0, maxLookAheadDays);
        for(int day = currentDay; day <= currentDay + lookAhead; day++) {
            if(IsScheduledForDay(day)) {
                return day;
            }
        }

        return -1;
    }

    public bool CanShow(PlayerController player, PlayerCalendarLog log, out string failureMessage) {
        if(!unlockedByDefault && !(log?.HasUnlockedEvent(this) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{Title} is not unlocked." : lockedMessage;
            return false;
        }

        if(!PassesAccess(player, out failureMessage)) {
            return false;
        }

        if(!PassesScheduleVisibility(out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public void Reveal(PlayerController player, string sourceId, string sourceName) {
        var log = player != null ? player.GetComponent<PlayerCalendarLog>() : null;
        if(markSeenOnReveal) {
            log?.MarkSeen(this, sourceId, sourceName);
        }

        UnlockRelatedInfo(player);
        PublishRevealed(player, sourceId, sourceName);
    }

    public void PublishCompleted(PlayerController player, string sourceId = null) {
        GameEventPublishing.PublishOptional(
            completedEvent,
            $"calendar.completed.{Id}",
            $"{Title} completed.",
            GameEventCategory.Calendar,
            GameEventImportance.Success,
            player != null ? player : this,
            "CalendarEventDefinition",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("eventId", Id),
            GameEventPublishing.Value("title", Title),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("sourceId", sourceId));
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
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

        if(requiredWorldEvent != null) {
            bool active = WorldEventManager.i != null && WorldEventManager.i.IsEventActive(requiredWorldEvent);
            if(active != requiredWorldEventActive) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{Title} is not active right now." : lockedMessage;
                return false;
            }
        }

        if(requiredDiscoveredRegion != null && !(player?.GetComponent<PlayerPokeNavLog>()?.HasDiscoveredRegion(requiredDiscoveredRegion) ?? requiredDiscoveredRegion.VisibleByDefault)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You have not discovered {requiredDiscoveredRegion.DisplayName} yet." : lockedMessage;
            return false;
        }

        if(requiredPokeNavEntry != null && !(player?.GetComponent<PlayerPokeNavLog>()?.HasDiscoveredEntry(requiredPokeNavEntry) ?? requiredPokeNavEntry.VisibleByDefault)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You have not discovered {requiredPokeNavEntry.DisplayName} yet." : lockedMessage;
            return false;
        }

        if(requiredRumor != null && !(player?.GetComponent<PlayerRumorLog>()?.HasHeardRumor(requiredRumor) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You have not heard about {requiredRumor.Title} yet." : lockedMessage;
            return false;
        }

        failureMessage = null;
        return true;
    }

    bool PassesScheduleVisibility(out string failureMessage) {
        int day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
        switch(visibilityMode) {
            case CalendarEventVisibilityMode.OnlyWhenActive:
                if(IsActiveNow()) {
                    failureMessage = null;
                    return true;
                }
                break;
            case CalendarEventVisibilityMode.VisibleBeforeAndDuring:
                if(IsActiveNow() || GetNextOccurrenceDay(day, VisibleDaysBeforeStart) >= 0) {
                    failureMessage = null;
                    return true;
                }
                break;
            default:
                failureMessage = null;
                return true;
        }

        failureMessage = "This event is not visible yet.";
        return false;
    }

    void UnlockRelatedInfo(PlayerController player) {
        if(player == null) {
            return;
        }

        var pokeNav = player.GetComponent<PlayerPokeNavLog>();
        var mapLog = player.GetComponent<PlayerMapLog>();
        var rumorLog = player.GetComponent<PlayerRumorLog>();

        if(relatedPokemon != null && pokemonKnowledgeToGrant > PokemonKnowledgeLevel.Unknown) {
            pokeNav?.RecordPokemonKnowledge(relatedPokemon, pokemonKnowledgeToGrant, $"calendar:{Id}");
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

        if(relatedRumor != null) {
            rumorLog?.UnlockRumor(relatedRumor, $"calendar:{Id}");
        }

        if(relatedMapMarker != null) {
            mapLog?.DiscoverMarker(relatedMapMarker, $"calendar:{Id}");
        }
    }

    void PublishRevealed(PlayerController player, string sourceId, string sourceName) {
        GameEventPublishing.PublishOptional(
            revealedEvent,
            $"calendar.revealed.{Id}",
            string.IsNullOrWhiteSpace(summary) ? Title : summary,
            GameEventCategory.Calendar,
            ToGameEventImportance(priority),
            player != null ? player : this,
            "CalendarEventDefinition",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("eventId", Id),
            GameEventPublishing.Value("title", Title),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("sourceName", sourceName),
            GameEventPublishing.Value("nextDay", GetNextOccurrenceDay(TimeSystem.i != null ? TimeSystem.i.Day : 1, 60)),
            GameEventPublishing.Value("relatedPokemon", relatedPokemon != null ? relatedPokemon.name : string.Empty),
            GameEventPublishing.Value("relatedRegion", relatedRegion != null ? relatedRegion.Id : string.Empty),
            GameEventPublishing.Value("relatedWorldEvent", relatedWorldEvent != null ? relatedWorldEvent.Id : string.Empty),
            GameEventPublishing.Value("relatedShop", relatedShop != null ? relatedShop.Id : string.Empty),
            GameEventPublishing.Value("relatedTransitRoute", relatedTransitRoute != null ? relatedTransitRoute.Id : string.Empty),
            GameEventPublishing.Value("relatedActivity", relatedActivity != null ? relatedActivity.Id : string.Empty));

        if(publishNotification) {
            NotificationFeed.Publish(
                Title,
                string.IsNullOrWhiteSpace(summary) ? details : summary,
                ToNotificationKind(category),
                priority,
                NotificationChannel.Story,
                "Calendar",
                sourceEventId: Id,
                pinned: important,
                values: new[] {
                    GameEventPublishing.Value("calendarEventId", Id),
                    GameEventPublishing.Value("calendarCategory", category),
                    GameEventPublishing.Value("nextDay", GetNextOccurrenceDay(TimeSystem.i != null ? TimeSystem.i.Day : 1, 60)),
                    GameEventPublishing.Value("sourceId", sourceId)
                });
        }
    }

    WeekDay GetWeekDay(int day) {
        int index = Mathf.Abs(Mathf.Max(1, day) - 1) % 7;
        return (WeekDay)index;
    }

    GameEventImportance ToGameEventImportance(NotificationPriority notificationPriority) {
        return notificationPriority switch {
            NotificationPriority.Low => GameEventImportance.Trace,
            NotificationPriority.High => GameEventImportance.Warning,
            NotificationPriority.Critical => GameEventImportance.Error,
            _ => GameEventImportance.Info
        };
    }

    NotificationKind ToNotificationKind(CalendarEventCategory eventCategory) {
        return eventCategory switch {
            CalendarEventCategory.Tournament => NotificationKind.Battle,
            CalendarEventCategory.Contest => NotificationKind.Activity,
            CalendarEventCategory.MarketSale => NotificationKind.Item,
            CalendarEventCategory.PokemonMigration => NotificationKind.Activity,
            CalendarEventCategory.PoliceCall => NotificationKind.Quest,
            CalendarEventCategory.Research => NotificationKind.Activity,
            CalendarEventCategory.Transit => NotificationKind.World,
            CalendarEventCategory.Club => NotificationKind.Social,
            CalendarEventCategory.Weather => NotificationKind.World,
            CalendarEventCategory.Story => NotificationKind.Quest,
            _ => NotificationKind.World
        };
    }
}
