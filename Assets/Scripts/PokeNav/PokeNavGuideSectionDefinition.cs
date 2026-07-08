using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokeNavGuideContentType {
    PokedexEntry,
    RegionInfo,
    PokeNavEntry,
    FeedItem,
    SocialPost,
    MapMarker,
    CalendarEvent,
    WorldDiscovery
}

public enum PokeNavGuideLockMode {
    AvailableOnly,
    AvailableAndLockedStubs,
    DebugShowAll
}

public enum PokeNavGuideSortMode {
    PinnedPriorityTitle,
    Title,
    ContentTypeThenTitle,
    RecentlySeen,
    KnowledgeThenTitle
}

[CreateAssetMenu(menuName = "PokeNav/Guide Section Definition")]
public class PokeNavGuideSectionDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id for this guide section. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future PokeNav/Pokedex UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining what this section collects.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Optional icon used by future PokeNav section UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags such as pokedex, region, police, research, shop, event or debug.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Content")]
    [Tooltip("Content types collected by this section.")]
    [SerializeField] List<PokeNavGuideContentType> contentTypes = new List<PokeNavGuideContentType> {
        PokeNavGuideContentType.PokedexEntry,
        PokeNavGuideContentType.RegionInfo,
        PokeNavGuideContentType.PokeNavEntry
    };
    [Tooltip("How unavailable content is handled.")]
    [SerializeField] PokeNavGuideLockMode lockMode = PokeNavGuideLockMode.AvailableOnly;
    [Tooltip("Minimum Pokemon knowledge needed before Pokedex entries appear as available.")]
    [SerializeField] PokemonKnowledgeLevel minimumPokemonKnowledge = PokemonKnowledgeLevel.Seen;
    [Tooltip("Optional region filter. Empty accepts any related region.")]
    [SerializeField] RegionInfoDefinition region;
    [Tooltip("Optional Pokemon filter. Empty accepts any related Pokemon.")]
    [SerializeField] PokemonBase pokemon;
    [Tooltip("Optional map view profile used when this section collects map markers.")]
    [SerializeField] MapViewProfileDefinition mapViewProfile;

    [Header("Tags")]
    [Tooltip("Tags that content must match before it appears.")]
    [SerializeField] List<string> requiredTags = new List<string>();
    [Tooltip("If enabled, any required tag may match. If disabled, all required tags must match.")]
    [SerializeField] bool matchAnyRequiredTag;
    [Tooltip("Tags that always filter content out.")]
    [SerializeField] List<string> blockedTags = new List<string>();

    [Header("State Filters")]
    [Tooltip("If disabled, read guide items are filtered out.")]
    [SerializeField] bool includeReadItems = true;
    [Tooltip("If disabled, dismissed guide items are filtered out.")]
    [SerializeField] bool includeDismissedItems;
    [Tooltip("If enabled, records returned by Build Items are marked seen in PlayerPokeNavGuideLog.")]
    [SerializeField] bool markSeenWhenBuilt;

    [Header("Sorting")]
    [Tooltip("How matching guide items are sorted.")]
    [SerializeField] PokeNavGuideSortMode sortMode = PokeNavGuideSortMode.PinnedPriorityTitle;
    [Tooltip("Maximum items returned after filtering. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxItems;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public IReadOnlyList<PokeNavGuideContentType> ContentTypes => contentTypes != null ? (IReadOnlyList<PokeNavGuideContentType>)contentTypes : Array.Empty<PokeNavGuideContentType>();
    public PokeNavGuideLockMode LockMode => lockMode;
    public PokemonKnowledgeLevel MinimumPokemonKnowledge => minimumPokemonKnowledge;
    public RegionInfoDefinition Region => region;
    public PokemonBase Pokemon => pokemon;
    public MapViewProfileDefinition MapViewProfile => mapViewProfile;
    public IReadOnlyList<string> RequiredTags => requiredTags != null ? (IReadOnlyList<string>)requiredTags : Array.Empty<string>();
    public IReadOnlyList<string> BlockedTags => blockedTags != null ? (IReadOnlyList<string>)blockedTags : Array.Empty<string>();
    public bool MatchAnyRequiredTag => matchAnyRequiredTag;
    public bool IncludeReadItems => includeReadItems;
    public bool IncludeDismissedItems => includeDismissedItems;
    public bool MarkSeenWhenBuilt => markSeenWhenBuilt;
    public PokeNavGuideSortMode SortMode => sortMode;
    public int MaxItems => Mathf.Max(0, maxItems);

    public IReadOnlyList<PokeNavGuideItemRecord> BuildItems(PlayerController player) {
        var items = new List<PokeNavGuideItemRecord>();
        var selectedTypes = ContentTypes.Count > 0
            ? new HashSet<PokeNavGuideContentType>(ContentTypes)
            : new HashSet<PokeNavGuideContentType>((PokeNavGuideContentType[])Enum.GetValues(typeof(PokeNavGuideContentType)));

        if(selectedTypes.Contains(PokeNavGuideContentType.PokedexEntry)) {
            items.AddRange(BuildPokedexItems(player));
        }

        if(selectedTypes.Contains(PokeNavGuideContentType.RegionInfo)) {
            items.AddRange(BuildRegionItems(player));
        }

        if(selectedTypes.Contains(PokeNavGuideContentType.PokeNavEntry)) {
            items.AddRange(BuildKnowledgeItems(player));
        }

        if(selectedTypes.Contains(PokeNavGuideContentType.FeedItem)) {
            items.AddRange(BuildFeedItems(player));
        }

        if(selectedTypes.Contains(PokeNavGuideContentType.SocialPost)) {
            items.AddRange(BuildSocialItems(player));
        }

        if(selectedTypes.Contains(PokeNavGuideContentType.MapMarker)) {
            items.AddRange(BuildMapMarkerItems(player));
        }

        if(selectedTypes.Contains(PokeNavGuideContentType.CalendarEvent)) {
            items.AddRange(BuildCalendarItems(player));
        }

        if(selectedTypes.Contains(PokeNavGuideContentType.WorldDiscovery)) {
            items.AddRange(BuildWorldDiscoveryItems(player));
        }

        var guideLog = player != null ? player.GetComponent<PlayerPokeNavGuideLog>() : null;
        foreach(var item in items) {
            guideLog?.ApplyState(item);
        }

        items = items
            .Where(item => item != null && MatchesCommonFilters(item))
            .Where(item => includeReadItems || !item.read)
            .Where(item => includeDismissedItems || !item.dismissed)
            .ToList();

        if(markSeenWhenBuilt && guideLog != null) {
            foreach(var item in items) {
                guideLog.MarkSeen(item, Id);
                guideLog.ApplyState(item);
            }
        }

        var sorted = SortItems(items);
        if(MaxItems > 0) {
            sorted = sorted.Take(MaxItems);
        }

        return sorted.ToList();
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    IEnumerable<PokeNavGuideItemRecord> BuildPokedexItems(PlayerController player) {
        var pokeNav = player != null ? player.GetComponent<PlayerPokeNavLog>() : null;
        foreach(var entry in Resources.LoadAll<PokedexEntryDefinition>("")) {
            if(entry == null || entry.Pokemon == null) {
                continue;
            }

            var knowledge = pokeNav != null ? pokeNav.GetPokemonKnowledgeLevel(entry.Pokemon) : PokemonKnowledgeLevel.Unknown;
            bool available = knowledge >= minimumPokemonKnowledge;
            if(!ShouldIncludeAvailability(available)) {
                continue;
            }

            yield return new PokeNavGuideItemRecord {
                contentType = PokeNavGuideContentType.PokedexEntry,
                itemId = entry.Id,
                title = entry.DisplayName,
                subtitle = entry.Classification,
                body = available ? entry.GetBestNote(knowledge) : string.Empty,
                icon = entry.Pokemon.IconSprite != null ? entry.Pokemon.IconSprite : entry.Pokemon.FrontSprite,
                priority = (int)knowledge,
                available = available,
                locked = !available,
                relatedPokemonId = entry.Pokemon.name,
                relatedPokemonName = entry.Pokemon.Name,
                knowledgeLevel = knowledge,
                sourceAsset = entry,
                tags = entry.Tags != null ? entry.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() : new List<string>()
            };
        }
    }

    IEnumerable<PokeNavGuideItemRecord> BuildRegionItems(PlayerController player) {
        var pokeNav = player != null ? player.GetComponent<PlayerPokeNavLog>() : null;
        foreach(var info in Resources.LoadAll<RegionInfoDefinition>("")) {
            if(info == null) {
                continue;
            }

            bool available = pokeNav != null ? pokeNav.HasDiscoveredRegion(info) : info.VisibleByDefault;
            if(!ShouldIncludeAvailability(available)) {
                continue;
            }

            yield return new PokeNavGuideItemRecord {
                contentType = PokeNavGuideContentType.RegionInfo,
                itemId = info.Id,
                title = info.DisplayName,
                subtitle = info.RegionType.ToString(),
                body = available ? info.Description : string.Empty,
                icon = info.Icon,
                priority = info.VisibleByDefault ? 1 : 0,
                available = available,
                locked = !available,
                relatedRegionId = info.Id,
                relatedRegionName = info.DisplayName,
                sourceAsset = info,
                tags = info.Tags != null ? info.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() : new List<string>()
            };
        }
    }

    IEnumerable<PokeNavGuideItemRecord> BuildKnowledgeItems(PlayerController player) {
        var pokeNav = player != null ? player.GetComponent<PlayerPokeNavLog>() : null;
        foreach(var entry in Resources.LoadAll<PokeNavEntryDefinition>("")) {
            if(entry == null) {
                continue;
            }

            bool available = pokeNav != null ? pokeNav.HasDiscoveredEntry(entry) : entry.VisibleByDefault;
            if(!ShouldIncludeAvailability(available)) {
                continue;
            }

            yield return new PokeNavGuideItemRecord {
                contentType = PokeNavGuideContentType.PokeNavEntry,
                itemId = entry.Id,
                title = entry.DisplayName,
                subtitle = entry.EntryType.ToString(),
                body = available ? entry.Body : string.Empty,
                icon = entry.Icon,
                priority = entry.VisibleByDefault ? 1 : 0,
                available = available,
                locked = !available,
                relatedPokemonId = entry.RelatedPokemon != null ? entry.RelatedPokemon.name : string.Empty,
                relatedPokemonName = entry.RelatedPokemon != null ? entry.RelatedPokemon.Name : string.Empty,
                relatedRegionId = entry.RelatedRegion != null ? entry.RelatedRegion.Id : string.Empty,
                relatedRegionName = entry.RelatedRegion != null ? entry.RelatedRegion.DisplayName : string.Empty,
                sourceAsset = entry,
                tags = entry.Tags != null ? entry.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() : new List<string>()
            };
        }
    }

    IEnumerable<PokeNavGuideItemRecord> BuildFeedItems(PlayerController player) {
        var feedLog = player != null ? player.GetComponent<PlayerPokeNavFeedLog>() : null;
        foreach(var item in Resources.LoadAll<PokeNavFeedItemDefinition>("")) {
            if(item == null) {
                continue;
            }

            bool available = item.CanShow(player, feedLog, out _);
            if(!ShouldIncludeAvailability(available)) {
                continue;
            }

            yield return new PokeNavGuideItemRecord {
                contentType = PokeNavGuideContentType.FeedItem,
                itemId = item.Id,
                title = item.Title,
                subtitle = item.FeedType.ToString(),
                body = available ? item.Body : string.Empty,
                icon = item.Icon,
                priority = (int)item.Priority,
                available = available,
                locked = !available,
                read = feedLog != null && feedLog.IsRead(item),
                pinned = feedLog != null ? feedLog.IsPinned(item) : item.PinnedByDefault,
                dismissed = feedLog != null && feedLog.IsDismissed(item),
                relatedPokemonId = item.RelatedPokemon != null ? item.RelatedPokemon.name : string.Empty,
                relatedPokemonName = item.RelatedPokemon != null ? item.RelatedPokemon.Name : string.Empty,
                relatedRegionId = item.RelatedRegion != null ? item.RelatedRegion.Id : string.Empty,
                relatedRegionName = item.RelatedRegion != null ? item.RelatedRegion.DisplayName : string.Empty,
                relatedMapMarkerId = item.RelatedMapMarker != null ? item.RelatedMapMarker.Id : string.Empty,
                sourceName = item.SourceName,
                sourceAsset = item,
                tags = item.Tags != null ? item.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() : new List<string>()
            };
        }
    }

    IEnumerable<PokeNavGuideItemRecord> BuildSocialItems(PlayerController player) {
        var pokeNav = player != null ? player.GetComponent<PlayerPokeNavLog>() : null;
        foreach(var post in Resources.LoadAll<SocialPostDefinition>("")) {
            if(post == null) {
                continue;
            }

            bool available = post.CanShow(player, pokeNav, out _);
            if(!ShouldIncludeAvailability(available)) {
                continue;
            }

            yield return new PokeNavGuideItemRecord {
                contentType = PokeNavGuideContentType.SocialPost,
                itemId = post.Id,
                title = post.Title,
                subtitle = post.PostType.ToString(),
                body = available ? post.Body : string.Empty,
                priority = (int)post.Priority,
                available = available,
                locked = !available,
                read = pokeNav != null && pokeNav.IsPostRead(post),
                pinned = post.Pinned,
                relatedPokemonId = post.RelatedPokemon != null ? post.RelatedPokemon.name : string.Empty,
                relatedPokemonName = post.RelatedPokemon != null ? post.RelatedPokemon.Name : string.Empty,
                relatedRegionId = post.RelatedRegion != null ? post.RelatedRegion.Id : string.Empty,
                relatedRegionName = post.RelatedRegion != null ? post.RelatedRegion.DisplayName : string.Empty,
                sourceName = post.Author,
                sourceAsset = post,
                tags = post.Tags != null ? post.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() : new List<string>()
            };
        }
    }

    IEnumerable<PokeNavGuideItemRecord> BuildMapMarkerItems(PlayerController player) {
        if(mapViewProfile != null) {
            foreach(var marker in mapViewProfile.GetVisibleMarkers(player)) {
                yield return new PokeNavGuideItemRecord {
                    contentType = PokeNavGuideContentType.MapMarker,
                    itemId = marker.id,
                    title = marker.displayName,
                    subtitle = marker.category.ToString(),
                    body = marker.description,
                    icon = marker.icon,
                    color = marker.color,
                    priority = marker.priority,
                    available = true,
                    locked = false,
                    read = marker.discovered,
                    pinned = marker.favorite,
                    dismissed = marker.hidden,
                    relatedRegionId = marker.regionId,
                    relatedMapMarkerId = marker.id,
                    sourceName = marker.source,
                    tags = marker.tags != null ? marker.tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() : new List<string>()
                };
            }

            yield break;
        }

        var mapLog = player != null ? player.GetComponent<PlayerMapLog>() : null;
        foreach(var marker in Resources.LoadAll<MapMarkerDefinition>("")) {
            if(marker == null) {
                continue;
            }

            bool available = marker.CanShow(player, mapLog, out _);
            if(!ShouldIncludeAvailability(available)) {
                continue;
            }

            yield return new PokeNavGuideItemRecord {
                contentType = PokeNavGuideContentType.MapMarker,
                itemId = marker.Id,
                title = marker.DisplayName,
                subtitle = marker.Category.ToString(),
                body = available ? marker.Description : string.Empty,
                icon = marker.Icon,
                color = marker.Color,
                priority = marker.Priority,
                available = available,
                locked = !available,
                pinned = marker.Important,
                relatedPokemonId = marker.RelatedPokemon != null ? marker.RelatedPokemon.name : string.Empty,
                relatedPokemonName = marker.RelatedPokemon != null ? marker.RelatedPokemon.Name : string.Empty,
                relatedRegionId = marker.Region != null ? marker.Region.Id : string.Empty,
                relatedRegionName = marker.Region != null ? marker.Region.DisplayName : string.Empty,
                relatedMapMarkerId = marker.Id,
                sourceAsset = marker,
                tags = marker.Tags != null ? marker.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() : new List<string>()
            };
        }
    }

    IEnumerable<PokeNavGuideItemRecord> BuildCalendarItems(PlayerController player) {
        var calendarLog = player != null ? player.GetComponent<PlayerCalendarLog>() : null;
        foreach(var evt in Resources.LoadAll<CalendarEventDefinition>("")) {
            if(evt == null) {
                continue;
            }

            bool available = evt.CanShow(player, calendarLog, out _);
            if(!ShouldIncludeAvailability(available)) {
                continue;
            }

            yield return new PokeNavGuideItemRecord {
                contentType = PokeNavGuideContentType.CalendarEvent,
                itemId = evt.Id,
                title = evt.Title,
                subtitle = evt.Category.ToString(),
                body = available ? string.IsNullOrWhiteSpace(evt.Summary) ? evt.Details : evt.Summary : string.Empty,
                icon = evt.Icon,
                priority = (int)evt.Priority + (evt.Important ? 10 : 0),
                available = available,
                locked = !available,
                pinned = evt.Important,
                relatedPokemonId = evt.RelatedPokemon != null ? evt.RelatedPokemon.name : string.Empty,
                relatedPokemonName = evt.RelatedPokemon != null ? evt.RelatedPokemon.Name : string.Empty,
                relatedRegionId = evt.RelatedRegion != null ? evt.RelatedRegion.Id : string.Empty,
                relatedRegionName = evt.RelatedRegion != null ? evt.RelatedRegion.DisplayName : string.Empty,
                relatedMapMarkerId = evt.RelatedMapMarker != null ? evt.RelatedMapMarker.Id : string.Empty,
                sourceAsset = evt,
                tags = evt.Tags != null ? evt.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() : new List<string>()
            };
        }
    }

    IEnumerable<PokeNavGuideItemRecord> BuildWorldDiscoveryItems(PlayerController player) {
        var discoveryLog = player != null ? player.GetComponent<PlayerWorldDiscoveryLog>() : null;
        foreach(var discovery in Resources.LoadAll<WorldDiscoveryDefinition>("")) {
            if(discovery == null) {
                continue;
            }

            bool available = discoveryLog != null && discoveryLog.HasDiscovered(discovery);
            if(!ShouldIncludeAvailability(available)) {
                continue;
            }

            yield return new PokeNavGuideItemRecord {
                contentType = PokeNavGuideContentType.WorldDiscovery,
                itemId = discovery.Id,
                title = discovery.DisplayName,
                subtitle = discovery.Kind.ToString(),
                body = available ? discovery.Description : string.Empty,
                priority = available ? 1 : 0,
                available = available,
                locked = !available,
                relatedPokemonId = discovery.RelatedPokemon != null ? discovery.RelatedPokemon.name : string.Empty,
                relatedPokemonName = discovery.RelatedPokemon != null ? discovery.RelatedPokemon.Name : string.Empty,
                relatedRegionId = discovery.RelatedRegion != null ? discovery.RelatedRegion.Id : string.Empty,
                relatedRegionName = discovery.RelatedRegion != null ? discovery.RelatedRegion.DisplayName : string.Empty,
                relatedMapMarkerId = discovery.MapMarker != null ? discovery.MapMarker.Id : string.Empty,
                sourceAsset = discovery,
                tags = discovery.Tags != null ? discovery.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() : new List<string>()
            };
        }
    }

    bool ShouldIncludeAvailability(bool available) {
        return available || lockMode != PokeNavGuideLockMode.AvailableOnly;
    }

    bool MatchesCommonFilters(PokeNavGuideItemRecord item) {
        if(item == null) {
            return false;
        }

        if(region != null && !string.Equals(item.relatedRegionId, region.Id, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        if(pokemon != null && !string.Equals(item.relatedPokemonId, pokemon.name, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        if(requiredTags != null && requiredTags.Any(tag => !string.IsNullOrWhiteSpace(tag))) {
            var validRequiredTags = requiredTags.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList();
            bool matches = matchAnyRequiredTag
                ? validRequiredTags.Any(item.HasTag)
                : validRequiredTags.All(item.HasTag);
            if(!matches) {
                return false;
            }
        }

        if(blockedTags != null && blockedTags.Any(item.HasTag)) {
            return false;
        }

        return lockMode == PokeNavGuideLockMode.DebugShowAll || item.available || !item.locked || lockMode == PokeNavGuideLockMode.AvailableAndLockedStubs;
    }

    IEnumerable<PokeNavGuideItemRecord> SortItems(IEnumerable<PokeNavGuideItemRecord> items) {
        return sortMode switch {
            PokeNavGuideSortMode.Title => items.OrderBy(item => item.title),
            PokeNavGuideSortMode.ContentTypeThenTitle => items.OrderBy(item => item.contentType).ThenBy(item => item.title),
            PokeNavGuideSortMode.RecentlySeen => items.OrderByDescending(item => item.lastSeenAbsoluteHour).ThenBy(item => item.title),
            PokeNavGuideSortMode.KnowledgeThenTitle => items.OrderByDescending(item => item.knowledgeLevel).ThenBy(item => item.title),
            _ => items.OrderByDescending(item => item.pinned).ThenByDescending(item => item.priority).ThenBy(item => item.title)
        };
    }
}

[Serializable]
public class PokeNavGuideItemRecord {
    [Tooltip("Content type represented by this guide item.")]
    public PokeNavGuideContentType contentType;
    [Tooltip("Stable id of the source item.")]
    public string itemId;
    [Tooltip("Title shown in future PokeNav guide UI.")]
    public string title;
    [Tooltip("Small subtitle/category shown in future PokeNav guide UI.")]
    public string subtitle;
    [Tooltip("Main text shown in future PokeNav guide UI.")]
    [TextArea]
    public string body;
    [Tooltip("Optional icon used by future guide UI.")]
    public Sprite icon;
    [Tooltip("Optional tint color used by future guide UI.")]
    public Color color = Color.white;
    [Tooltip("Higher priority items sort above lower priority items.")]
    public int priority;
    [Tooltip("If enabled, the item can be opened with full details.")]
    public bool available;
    [Tooltip("If enabled, the item is intentionally shown as locked.")]
    public bool locked;
    [Tooltip("If enabled, future UI should treat this item as read.")]
    public bool read;
    [Tooltip("If enabled, future UI should pin this item.")]
    public bool pinned;
    [Tooltip("If enabled, future UI should hide this item from normal lists.")]
    public bool dismissed;
    [Tooltip("Related Pokemon asset id.")]
    public string relatedPokemonId;
    [Tooltip("Related Pokemon display name.")]
    public string relatedPokemonName;
    [Tooltip("Related region id.")]
    public string relatedRegionId;
    [Tooltip("Related region display name.")]
    public string relatedRegionName;
    [Tooltip("Related map marker id.")]
    public string relatedMapMarkerId;
    [Tooltip("Optional source/channel/author text.")]
    public string sourceName;
    [Tooltip("Knowledge level used by Pokedex records.")]
    public PokemonKnowledgeLevel knowledgeLevel;
    [Tooltip("Last seen absolute in-game hour from PlayerPokeNavGuideLog.")]
    public int lastSeenAbsoluteHour = -1;
    [Tooltip("Source asset or component that produced this item.")]
    public UnityEngine.Object sourceAsset;
    [Tooltip("Free-form tags used by filters and future UI.")]
    public List<string> tags = new List<string>();

    public string Key => BuildKey(contentType, itemId);

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public static string BuildKey(PokeNavGuideContentType type, string id) {
        return $"{type}:{id}";
    }
}
