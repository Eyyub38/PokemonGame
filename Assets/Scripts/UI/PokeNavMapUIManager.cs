using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokeNavMapUIActionResultKind {
    None,
    Refreshed,
    NavigationTargetSet,
    NavigationTargetCleared,
    NavigationTargetReached,
    MarkerFavoriteChanged,
    MarkerHiddenChanged,
    GuideItemReadChanged,
    GuideItemPinnedChanged,
    GuideItemDismissedChanged,
    FeedItemReadChanged,
    FeedItemPinnedChanged,
    FeedItemDismissedChanged,
    FeedItemUnlocked,
    SocialPostReadChanged,
    RegionDiscovered,
    EntryDiscovered,
    Blocked
}

public class PokeNavMapUIManager : MonoBehaviour {
    [Header("Player")]
    [Tooltip("Player whose PokeNav and map state is shown. Empty uses PlayerController.i or the first PlayerController in the scene.")]
    [SerializeField] PlayerController playerOverride;
    [Tooltip("If enabled, missing player log components are created when UI actions need them.")]
    [SerializeField] bool createMissingLogsForActions = true;

    [Header("Map")]
    [Tooltip("Map view profile used to filter/sort markers for this UI. Empty uses the active MapMarkerRegistry records directly.")]
    [SerializeField] MapViewProfileDefinition mapViewProfile;
    [Tooltip("Optional origin used by distance-aware map profiles and marker rows. Empty uses the player transform.")]
    [SerializeField] Transform distanceOriginOverride;
    [Tooltip("If enabled, player-hidden markers can still appear in this UI snapshot.")]
    [SerializeField] bool includeHiddenMarkers;
    [Tooltip("If enabled, only minimap-eligible markers are shown when no Map View Profile is assigned.")]
    [SerializeField] bool fallbackMinimapMode;
    [Tooltip("If enabled, world-map-eligible markers are shown when no Map View Profile is assigned.")]
    [SerializeField] bool fallbackWorldMapMode = true;

    [Header("Guide")]
    [Tooltip("Guide sections shown by this UI. Empty reads all PokeNavGuideSectionDefinition assets from Resources.")]
    [SerializeField] List<PokeNavGuideSectionDefinition> guideSections = new List<PokeNavGuideSectionDefinition>();
    [Tooltip("Selected guide section whose item rows are exposed in the snapshot. Empty uses the first available section.")]
    [SerializeField] PokeNavGuideSectionDefinition selectedGuideSection;

    [Header("Feeds")]
    [Tooltip("Feed item pool shown by this UI. Empty reads all PokeNavFeedItemDefinition assets from Resources.")]
    [SerializeField] List<PokeNavFeedItemDefinition> feedPool = new List<PokeNavFeedItemDefinition>();
    [Tooltip("Social post pool shown by this UI. Empty reads all SocialPostDefinition assets from Resources.")]
    [SerializeField] List<SocialPostDefinition> socialPostPool = new List<SocialPostDefinition>();
    [Tooltip("If enabled, read feed items remain visible in the snapshot.")]
    [SerializeField] bool includeReadFeedItems = true;
    [Tooltip("If enabled, dismissed feed items remain visible in the snapshot.")]
    [SerializeField] bool includeDismissedFeedItems;
    [Tooltip("If enabled, read social posts remain visible in the snapshot.")]
    [SerializeField] bool includeReadSocialPosts = true;

    [Header("Pokedex")]
    [Tooltip("Minimum knowledge level required before Pokedex entries appear unless Include Unknown Pokedex Entries is enabled.")]
    [SerializeField] PokemonKnowledgeLevel minimumPokedexKnowledge = PokemonKnowledgeLevel.Seen;
    [Tooltip("If enabled, unknown Pokedex entries appear as locked/stub rows.")]
    [SerializeField] bool includeUnknownPokedexEntries;

    [Header("Snapshot")]
    [Tooltip("If enabled, Refresh is called when this component starts.")]
    [SerializeField] bool refreshOnStart = true;
    [Tooltip("If enabled, Refresh is called after every successful or blocked action.")]
    [SerializeField] bool refreshAfterActions = true;
    [Tooltip("Maximum rows copied per section. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxRowsPerSection = 60;
    [Tooltip("Source id written into navigation/feed/discovery actions.")]
    [SerializeField] string uiSourceId = "ui:pokenav-map";

    [Header("Debug")]
    [Tooltip("If enabled, successful UI backend actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulActions;
    [Tooltip("If enabled, blocked UI backend actions are written to GameDebug.")]
    [SerializeField] bool logBlockedActions = true;

    PokeNavMapUIScreenSnapshot currentSnapshot = new PokeNavMapUIScreenSnapshot();
    PokeNavMapUIActionResult lastResult = new PokeNavMapUIActionResult();

    public PokeNavMapUIScreenSnapshot CurrentSnapshot => currentSnapshot;
    public PokeNavMapUIActionResult LastResult => lastResult;
    public MapViewProfileDefinition MapViewProfile => mapViewProfile;
    public IReadOnlyList<PokeNavGuideSectionDefinition> GuideSections => guideSections;
    public PokeNavGuideSectionDefinition SelectedGuideSection => ResolveSelectedGuideSection();
    public IReadOnlyList<PokeNavFeedItemDefinition> FeedPool => feedPool;
    public IReadOnlyList<SocialPostDefinition> SocialPostPool => socialPostPool;
    public int MaxRowsPerSection => Mathf.Max(0, maxRowsPerSection);
    public event Action<PokeNavMapUIScreenSnapshot> OnSnapshotChanged;
    public event Action<PokeNavMapUIActionResult> OnActionResult;

    void Start() {
        if(refreshOnStart) {
            Refresh();
        }
    }

    [ContextMenu("Refresh PokeNav Map Snapshot")]
    public PokeNavMapUIScreenSnapshot RefreshFromContextMenu() {
        return Refresh();
    }

    public PokeNavMapUIScreenSnapshot Refresh() {
        var player = ResolvePlayer();
        var pokeNavLog = player != null ? player.GetComponent<PlayerPokeNavLog>() : null;
        var mapLog = player != null ? player.GetComponent<PlayerMapLog>() : null;
        var navigationLog = player != null ? player.GetComponent<PlayerMapNavigationLog>() : null;
        var guideLog = player != null ? player.GetComponent<PlayerPokeNavGuideLog>() : null;
        var feedLog = player != null ? player.GetComponent<PlayerPokeNavFeedLog>() : null;
        var origin = ResolveOrigin(player);
        var markerRecords = ResolveMapMarkers(player, origin);
        var selectedSection = ResolveSelectedGuideSection();

        currentSnapshot = new PokeNavMapUIScreenSnapshot {
            hasPlayer = player != null,
            playerName = player != null ? player.name : string.Empty,
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0,
            absoluteHour = GetCurrentAbsoluteHour(),
            mapViewProfileId = mapViewProfile != null ? mapViewProfile.Id : string.Empty,
            mapViewProfileName = mapViewProfile != null ? mapViewProfile.DisplayName : "Runtime Map",
            selectedGuideSectionId = selectedSection != null ? selectedSection.Id : string.Empty,
            selectedGuideSectionName = selectedSection != null ? selectedSection.DisplayName : string.Empty,
            activeTarget = navigationLog != null ? PokeNavMapNavigationTargetRow.FromTarget(navigationLog.ActiveTarget, origin) : null,
            markers = Limit(markerRecords.Select(record => PokeNavMapMarkerRow.FromRecord(record, navigationLog, origin))).ToList(),
            guideSections = BuildGuideSectionRows(player),
            guideItems = BuildGuideItemRows(player, selectedSection),
            feedItems = BuildFeedRows(player, feedLog),
            socialPosts = BuildSocialRows(player, pokeNavLog),
            pokedexEntries = BuildPokedexRows(pokeNavLog),
            regions = BuildRegionRows(player, pokeNavLog),
            knowledgeEntries = BuildKnowledgeRows(player, pokeNavLog),
            discoveredMarkerCount = mapLog != null ? mapLog.DiscoveredMarkerIds.Count : 0,
            favoriteMarkerCount = mapLog != null ? mapLog.FavoriteMarkerIds.Count : 0,
            unreadFeedCount = feedLog != null ? feedLog.GetUnreadCount() : 0,
            unreadGuideCount = guideLog != null ? guideLog.CountStates(read: false, dismissed: false) : 0,
            lastResult = lastResult
        };

        OnSnapshotChanged?.Invoke(currentSnapshot);
        return currentSnapshot;
    }

    public bool TrySetNavigationTarget(string markerId, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to set map navigation targets.", out feedback);
        }

        if(string.IsNullOrWhiteSpace(markerId)) {
            return Block("No marker id was selected.", out feedback);
        }

        var navigationLog = GetOrCreate<PlayerMapNavigationLog>(player);
        var marker = ResolveMapMarkers(player, ResolveOrigin(player))
            .FirstOrDefault(record => record != null && string.Equals(record.id, markerId, StringComparison.OrdinalIgnoreCase));

        if(marker != null && navigationLog.SetTarget(marker, ResolveSourceId(), discoverMarker: false)) {
            return Succeed(PokeNavMapUIActionResultKind.NavigationTargetSet, $"{marker.displayName} selected as navigation target.", out feedback);
        }

        var markerDefinition = FindResourceById<MapMarkerDefinition>(markerId, definition => definition.Id);
        if(markerDefinition != null && navigationLog.SetTarget(markerDefinition, ResolveSourceId(), discoverMarker: false)) {
            return Succeed(PokeNavMapUIActionResultKind.NavigationTargetSet, $"{markerDefinition.DisplayName} selected as navigation target.", out feedback);
        }

        return Block($"Map marker '{markerId}' could not be found.", out feedback);
    }

    public bool TryClearNavigationTarget(out string feedback) {
        var player = ResolvePlayer();
        var navigationLog = player != null ? player.GetComponent<PlayerMapNavigationLog>() : null;
        if(navigationLog != null && navigationLog.ClearTarget(ResolveSourceId())) {
            return Succeed(PokeNavMapUIActionResultKind.NavigationTargetCleared, "Navigation target cleared.", out feedback);
        }

        return Block("No active navigation target was cleared.", out feedback);
    }

    public bool TryMarkNavigationTargetReached(out string feedback) {
        var player = ResolvePlayer();
        var navigationLog = player != null ? player.GetComponent<PlayerMapNavigationLog>() : null;
        if(navigationLog != null && navigationLog.MarkTargetReached(ResolveSourceId())) {
            return Succeed(PokeNavMapUIActionResultKind.NavigationTargetReached, "Navigation target marked reached.", out feedback);
        }

        return Block("No active navigation target was reached.", out feedback);
    }

    public bool TrySetMarkerFavorite(string markerId, bool favorite, out string feedback) {
        var player = ResolvePlayer();
        var mapLog = GetOrCreate<PlayerMapLog>(player);
        if(mapLog == null || string.IsNullOrWhiteSpace(markerId)) {
            return Block("A player and marker id are required to favorite map markers.", out feedback);
        }

        mapLog.SetMarkerFavorite(markerId, favorite);
        return Succeed(PokeNavMapUIActionResultKind.MarkerFavoriteChanged, favorite ? "Marker favorited." : "Marker unfavorited.", out feedback);
    }

    public bool TrySetMarkerHidden(string markerId, bool hidden, out string feedback) {
        var player = ResolvePlayer();
        var mapLog = GetOrCreate<PlayerMapLog>(player);
        if(mapLog == null || string.IsNullOrWhiteSpace(markerId)) {
            return Block("A player and marker id are required to hide map markers.", out feedback);
        }

        mapLog.SetMarkerHidden(markerId, hidden);
        return Succeed(PokeNavMapUIActionResultKind.MarkerHiddenChanged, hidden ? "Marker hidden." : "Marker shown.", out feedback);
    }

    public bool TryMarkGuideItemRead(PokeNavGuideContentType contentType, string itemId, bool read, out string feedback) {
        var player = ResolvePlayer();
        var guideLog = GetOrCreate<PlayerPokeNavGuideLog>(player);
        if(guideLog != null && guideLog.MarkRead(contentType, itemId, read)) {
            return Succeed(PokeNavMapUIActionResultKind.GuideItemReadChanged, read ? "Guide item marked read." : "Guide item marked unread.", out feedback);
        }

        return Block("Guide item read state did not change.", out feedback);
    }

    public bool TrySetGuideItemPinned(PokeNavGuideContentType contentType, string itemId, bool pinned, out string feedback) {
        var player = ResolvePlayer();
        var guideLog = GetOrCreate<PlayerPokeNavGuideLog>(player);
        if(guideLog != null && guideLog.SetPinned(contentType, itemId, pinned)) {
            return Succeed(PokeNavMapUIActionResultKind.GuideItemPinnedChanged, pinned ? "Guide item pinned." : "Guide item unpinned.", out feedback);
        }

        return Block("Guide item pinned state did not change.", out feedback);
    }

    public bool TrySetGuideItemDismissed(PokeNavGuideContentType contentType, string itemId, bool dismissed, out string feedback) {
        var player = ResolvePlayer();
        var guideLog = GetOrCreate<PlayerPokeNavGuideLog>(player);
        if(guideLog != null && guideLog.SetDismissed(contentType, itemId, dismissed)) {
            return Succeed(PokeNavMapUIActionResultKind.GuideItemDismissedChanged, dismissed ? "Guide item dismissed." : "Guide item restored.", out feedback);
        }

        return Block("Guide item dismissed state did not change.", out feedback);
    }

    public bool TryMarkFeedItemRead(string itemId, bool read, out string feedback) {
        var player = ResolvePlayer();
        var item = FindFeedItem(itemId);
        var feedLog = GetOrCreate<PlayerPokeNavFeedLog>(player);
        if(item != null && feedLog != null && feedLog.MarkRead(item, read)) {
            return Succeed(PokeNavMapUIActionResultKind.FeedItemReadChanged, read ? "Feed item marked read." : "Feed item marked unread.", out feedback);
        }

        return Block("Feed item read state did not change.", out feedback);
    }

    public bool TrySetFeedItemPinned(string itemId, bool pinned, out string feedback) {
        var player = ResolvePlayer();
        var item = FindFeedItem(itemId);
        var feedLog = GetOrCreate<PlayerPokeNavFeedLog>(player);
        if(item != null && feedLog != null && feedLog.SetPinned(item, pinned)) {
            return Succeed(PokeNavMapUIActionResultKind.FeedItemPinnedChanged, pinned ? "Feed item pinned." : "Feed item unpinned.", out feedback);
        }

        return Block("Feed item pinned state did not change.", out feedback);
    }

    public bool TrySetFeedItemDismissed(string itemId, bool dismissed, out string feedback) {
        var player = ResolvePlayer();
        var item = FindFeedItem(itemId);
        var feedLog = GetOrCreate<PlayerPokeNavFeedLog>(player);
        if(item != null && feedLog != null && feedLog.SetDismissed(item, dismissed)) {
            return Succeed(PokeNavMapUIActionResultKind.FeedItemDismissedChanged, dismissed ? "Feed item dismissed." : "Feed item restored.", out feedback);
        }

        return Block("Feed item dismissed state did not change.", out feedback);
    }

    public bool TryUnlockFeedItem(string itemId, out PokeNavFeedItemRecord record, out string feedback) {
        record = null;
        var player = ResolvePlayer();
        var item = FindFeedItem(itemId);
        if(item == null) {
            return Block($"Feed item '{itemId}' could not be found.", out feedback);
        }

        if(item.TryUnlock(player, ResolveSourceId(), applyLinks: true, publish: true, out record, out feedback)) {
            return Succeed(PokeNavMapUIActionResultKind.FeedItemUnlocked, $"{item.Title} unlocked.", out feedback);
        }

        return Block(feedback, out feedback);
    }

    public bool TryMarkSocialPostRead(string postId, bool read, out string feedback) {
        var player = ResolvePlayer();
        var post = FindSocialPost(postId);
        var pokeNavLog = GetOrCreate<PlayerPokeNavLog>(player);
        if(post != null && pokeNavLog != null && pokeNavLog.MarkPostRead(post, read)) {
            return Succeed(PokeNavMapUIActionResultKind.SocialPostReadChanged, read ? "Social post marked read." : "Social post marked unread.", out feedback);
        }

        return Block("Social post read state did not change.", out feedback);
    }

    public bool TryDiscoverRegion(string regionId, out string feedback) {
        var player = ResolvePlayer();
        var region = FindResourceById<RegionInfoDefinition>(regionId, item => item.Id);
        var pokeNavLog = GetOrCreate<PlayerPokeNavLog>(player);
        feedback = null;
        if(region != null && pokeNavLog != null && pokeNavLog.DiscoverRegion(region, out feedback)) {
            return Succeed(PokeNavMapUIActionResultKind.RegionDiscovered, $"{region.DisplayName} discovered.", out feedback);
        }

        return Block(string.IsNullOrWhiteSpace(feedback) ? $"Region '{regionId}' could not be discovered." : feedback, out feedback);
    }

    public bool TryDiscoverEntry(string entryId, out string feedback) {
        var player = ResolvePlayer();
        var entry = FindResourceById<PokeNavEntryDefinition>(entryId, item => item.Id);
        var pokeNavLog = GetOrCreate<PlayerPokeNavLog>(player);
        feedback = null;
        if(entry != null && pokeNavLog != null && pokeNavLog.DiscoverEntry(entry, out feedback)) {
            return Succeed(PokeNavMapUIActionResultKind.EntryDiscovered, $"{entry.DisplayName} discovered.", out feedback);
        }

        return Block(string.IsNullOrWhiteSpace(feedback) ? $"Entry '{entryId}' could not be discovered." : feedback, out feedback);
    }

    IReadOnlyList<MapMarkerRecord> ResolveMapMarkers(PlayerController player, Vector3? origin) {
        if(mapViewProfile != null) {
            return mapViewProfile.GetVisibleMarkers(player, origin);
        }

        if(MapMarkerRegistry.i == null) {
            return Array.Empty<MapMarkerRecord>();
        }

        return MapMarkerRegistry.i.GetVisibleMarkers(player, fallbackMinimapMode, fallbackWorldMapMode, includeHiddenMarkers);
    }

    List<PokeNavGuideSectionRow> BuildGuideSectionRows(PlayerController player) {
        return Limit(ResolveGuideSections()
            .Select(section => PokeNavGuideSectionRow.FromSection(section, player)))
            .ToList();
    }

    List<PokeNavGuideItemRow> BuildGuideItemRows(PlayerController player, PokeNavGuideSectionDefinition section) {
        if(section == null) {
            return new List<PokeNavGuideItemRow>();
        }

        return Limit(section.BuildItems(player).Select(PokeNavGuideItemRow.FromRecord)).ToList();
    }

    List<PokeNavFeedItemRow> BuildFeedRows(PlayerController player, PlayerPokeNavFeedLog feedLog) {
        var pool = ResolveFeedPool();
        var items = pool
            .Where(item => item != null && item.CanShow(player, feedLog, out _))
            .Where(item => includeReadFeedItems || !(feedLog?.IsRead(item) ?? false))
            .Where(item => includeDismissedFeedItems || !(feedLog?.IsDismissed(item) ?? false))
            .OrderByDescending(item => feedLog != null ? feedLog.IsPinned(item) : item.PinnedByDefault)
            .ThenByDescending(item => item.Priority)
            .ThenBy(item => item.Title);

        return Limit(items.Select(item => PokeNavFeedItemRow.FromItem(item, feedLog))).ToList();
    }

    List<PokeNavSocialPostRow> BuildSocialRows(PlayerController player, PlayerPokeNavLog pokeNavLog) {
        var posts = pokeNavLog != null
            ? pokeNavLog.GetAvailablePosts(ResolveSocialPostPool(), includeReadSocialPosts)
            : ResolveSocialPostPool().Where(post => post != null && post.CanShow(player, null, out _)).OrderByDescending(post => post.Pinned).ThenByDescending(post => post.Priority).ThenBy(post => post.Title).ToList();

        return Limit(posts.Select(post => PokeNavSocialPostRow.FromPost(post, pokeNavLog))).ToList();
    }

    List<PokeNavPokedexRow> BuildPokedexRows(PlayerPokeNavLog pokeNavLog) {
        var rows = Resources.LoadAll<PokedexEntryDefinition>("")
            .Where(entry => entry != null && entry.Pokemon != null)
            .Select(entry => PokeNavPokedexRow.FromEntry(entry, pokeNavLog))
            .Where(row => includeUnknownPokedexEntries || row.knowledgeLevel >= minimumPokedexKnowledge)
            .OrderBy(row => row.displayName);

        return Limit(rows).ToList();
    }

    List<PokeNavRegionRow> BuildRegionRows(PlayerController player, PlayerPokeNavLog pokeNavLog) {
        var rows = Resources.LoadAll<RegionInfoDefinition>("")
            .Where(region => region != null)
            .Select(region => PokeNavRegionRow.FromRegion(region, player, pokeNavLog))
            .Where(row => row.visible || row.canDiscover)
            .OrderBy(row => row.regionType)
            .ThenBy(row => row.displayName);

        return Limit(rows).ToList();
    }

    List<PokeNavKnowledgeEntryRow> BuildKnowledgeRows(PlayerController player, PlayerPokeNavLog pokeNavLog) {
        var rows = Resources.LoadAll<PokeNavEntryDefinition>("")
            .Where(entry => entry != null)
            .Select(entry => PokeNavKnowledgeEntryRow.FromEntry(entry, player, pokeNavLog))
            .Where(row => row.visible || row.canDiscover)
            .OrderBy(row => row.entryType)
            .ThenBy(row => row.displayName);

        return Limit(rows).ToList();
    }

    IEnumerable<T> Limit<T>(IEnumerable<T> source) {
        if(source == null) {
            return Enumerable.Empty<T>();
        }

        return MaxRowsPerSection > 0 ? source.Take(MaxRowsPerSection) : source;
    }

    IEnumerable<PokeNavGuideSectionDefinition> ResolveGuideSections() {
        IEnumerable<PokeNavGuideSectionDefinition> source = guideSections != null && guideSections.Any(section => section != null)
            ? guideSections
            : Resources.LoadAll<PokeNavGuideSectionDefinition>("");

        return source.Where(section => section != null).OrderBy(section => section.DisplayName);
    }

    PokeNavGuideSectionDefinition ResolveSelectedGuideSection() {
        if(selectedGuideSection != null) {
            return selectedGuideSection;
        }

        return ResolveGuideSections().FirstOrDefault();
    }

    IEnumerable<PokeNavFeedItemDefinition> ResolveFeedPool() {
        return feedPool != null && feedPool.Any(item => item != null)
            ? feedPool.Where(item => item != null)
            : Resources.LoadAll<PokeNavFeedItemDefinition>("").Where(item => item != null);
    }

    IEnumerable<SocialPostDefinition> ResolveSocialPostPool() {
        return socialPostPool != null && socialPostPool.Any(post => post != null)
            ? socialPostPool.Where(post => post != null)
            : Resources.LoadAll<SocialPostDefinition>("").Where(post => post != null);
    }

    PokeNavFeedItemDefinition FindFeedItem(string itemId) {
        if(string.IsNullOrWhiteSpace(itemId)) {
            return null;
        }

        return ResolveFeedPool().FirstOrDefault(item => string.Equals(item.Id, itemId, StringComparison.OrdinalIgnoreCase))
            ?? FindResourceById<PokeNavFeedItemDefinition>(itemId, item => item.Id);
    }

    SocialPostDefinition FindSocialPost(string postId) {
        if(string.IsNullOrWhiteSpace(postId)) {
            return null;
        }

        return ResolveSocialPostPool().FirstOrDefault(post => string.Equals(post.Id, postId, StringComparison.OrdinalIgnoreCase))
            ?? FindResourceById<SocialPostDefinition>(postId, post => post.Id);
    }

    T FindResourceById<T>(string id, Func<T, string> getId) where T : UnityEngine.Object {
        if(string.IsNullOrWhiteSpace(id) || getId == null) {
            return null;
        }

        return Resources.LoadAll<T>("")
            .FirstOrDefault(asset => asset != null && string.Equals(getId(asset), id, StringComparison.OrdinalIgnoreCase));
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        if(PlayerController.i != null) {
            return PlayerController.i;
        }

        return FindAnyObjectByType<PlayerController>();
    }

    Vector3? ResolveOrigin(PlayerController player) {
        if(distanceOriginOverride != null) {
            return distanceOriginOverride.position;
        }

        return player != null ? player.transform.position : null;
    }

    T GetOrCreate<T>(PlayerController player) where T : Component {
        if(player == null) {
            return null;
        }

        var component = player.GetComponent<T>();
        return component != null || !createMissingLogsForActions ? component : player.gameObject.AddComponent<T>();
    }

    string ResolveSourceId() {
        return string.IsNullOrWhiteSpace(uiSourceId) ? "ui:pokenav-map" : uiSourceId;
    }

    bool Succeed(PokeNavMapUIActionResultKind kind, string message, out string feedback) {
        feedback = message;
        RecordResult(kind, true, message);
        return true;
    }

    bool Block(string message, out string feedback) {
        feedback = string.IsNullOrWhiteSpace(message) ? "Action was blocked." : message;
        RecordResult(PokeNavMapUIActionResultKind.Blocked, false, feedback);
        return false;
    }

    void RecordResult(PokeNavMapUIActionResultKind kind, bool success, string message) {
        lastResult = new PokeNavMapUIActionResult {
            kind = success ? kind : PokeNavMapUIActionResultKind.Blocked,
            success = success,
            message = message ?? string.Empty,
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0,
            absoluteHour = GetCurrentAbsoluteHour()
        };

        if(success && logSuccessfulActions) {
            GameDebug.Success(lastResult.message, GameDebugCategory.UI, this, "PokeNavMapUIManager");
        } else if(!success && logBlockedActions) {
            GameDebug.Warning(lastResult.message, GameDebugCategory.UI, this, "PokeNavMapUIManager");
        }

        OnActionResult?.Invoke(lastResult);
        if(refreshAfterActions) {
            Refresh();
        }
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }
}

[Serializable]
public class PokeNavMapUIScreenSnapshot {
    [Tooltip("If enabled, a player was resolved for this snapshot.")]
    public bool hasPlayer;
    [Tooltip("Resolved player name.")]
    public string playerName;
    [Tooltip("Current in-game day.")]
    public int day;
    [Tooltip("Current in-game hour.")]
    public int hour;
    [Tooltip("Current absolute in-game hour.")]
    public int absoluteHour;
    [Tooltip("Map view profile id used by this snapshot.")]
    public string mapViewProfileId;
    [Tooltip("Map view profile display name.")]
    public string mapViewProfileName;
    [Tooltip("Selected guide section id.")]
    public string selectedGuideSectionId;
    [Tooltip("Selected guide section display name.")]
    public string selectedGuideSectionName;
    [Tooltip("Current active navigation target, if any.")]
    public PokeNavMapNavigationTargetRow activeTarget;
    [Tooltip("Visible map marker rows.")]
    public List<PokeNavMapMarkerRow> markers = new List<PokeNavMapMarkerRow>();
    [Tooltip("Guide section summary rows.")]
    public List<PokeNavGuideSectionRow> guideSections = new List<PokeNavGuideSectionRow>();
    [Tooltip("Guide item rows for the selected guide section.")]
    public List<PokeNavGuideItemRow> guideItems = new List<PokeNavGuideItemRow>();
    [Tooltip("PokeNav feed rows.")]
    public List<PokeNavFeedItemRow> feedItems = new List<PokeNavFeedItemRow>();
    [Tooltip("Social post rows.")]
    public List<PokeNavSocialPostRow> socialPosts = new List<PokeNavSocialPostRow>();
    [Tooltip("Pokedex rows.")]
    public List<PokeNavPokedexRow> pokedexEntries = new List<PokeNavPokedexRow>();
    [Tooltip("Region info rows.")]
    public List<PokeNavRegionRow> regions = new List<PokeNavRegionRow>();
    [Tooltip("Generic PokeNav knowledge entry rows.")]
    public List<PokeNavKnowledgeEntryRow> knowledgeEntries = new List<PokeNavKnowledgeEntryRow>();
    [Tooltip("Number of discovered markers in PlayerMapLog.")]
    public int discoveredMarkerCount;
    [Tooltip("Number of favorited markers in PlayerMapLog.")]
    public int favoriteMarkerCount;
    [Tooltip("Number of unread feed records in PlayerPokeNavFeedLog.")]
    public int unreadFeedCount;
    [Tooltip("Number of unread guide item states in PlayerPokeNavGuideLog.")]
    public int unreadGuideCount;
    [Tooltip("Most recent UI backend action result.")]
    public PokeNavMapUIActionResult lastResult;
}

[Serializable]
public class PokeNavMapUIActionResult {
    [Tooltip("Kind of UI backend action that produced this result.")]
    public PokeNavMapUIActionResultKind kind;
    [Tooltip("If enabled, the action succeeded.")]
    public bool success;
    [Tooltip("Readable result, failure or feedback text.")]
    public string message;
    [Tooltip("In-game day when the result was produced.")]
    public int day;
    [Tooltip("In-game hour when the result was produced.")]
    public int hour;
    [Tooltip("Absolute in-game hour when the result was produced.")]
    public int absoluteHour;
}

[Serializable]
public class PokeNavMapMarkerRow {
    [Tooltip("Map marker id.")]
    public string markerId;
    [Tooltip("Map marker display name.")]
    public string displayName;
    [Tooltip("Map marker description.")]
    public string description;
    [Tooltip("Marker category.")]
    public MapMarkerCategory category;
    [Tooltip("World position copied from the marker record.")]
    public Vector3 worldPosition;
    [Tooltip("Scene name copied from the marker record.")]
    public string sceneName;
    [Tooltip("Optional region id.")]
    public string regionId;
    [Tooltip("If enabled, marker is discovered.")]
    public bool discovered;
    [Tooltip("If enabled, marker is hidden by player preference.")]
    public bool hidden;
    [Tooltip("If enabled, marker is favorited by player preference.")]
    public bool favorite;
    [Tooltip("If enabled, marker is important.")]
    public bool important;
    [Tooltip("If enabled, marker is currently the navigation target.")]
    public bool isNavigationTarget;
    [Tooltip("If enabled, marker may appear on the minimap.")]
    public bool showOnMinimap;
    [Tooltip("If enabled, marker may appear on the world map.")]
    public bool showOnWorldMap;
    [Tooltip("Distance from UI origin/player. -1 means unavailable.")]
    public float distance;
    [Tooltip("Free-form tags copied from the marker record.")]
    public List<string> tags = new List<string>();
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;

    public static PokeNavMapMarkerRow FromRecord(MapMarkerRecord record, PlayerMapNavigationLog navigationLog, Vector3? origin) {
        float distance = origin.HasValue ? Vector3.Distance(origin.Value, record.worldPosition) : -1f;
        bool isTarget = navigationLog != null && navigationLog.IsTarget(record.id);
        return new PokeNavMapMarkerRow {
            markerId = record.id,
            displayName = record.displayName,
            description = record.description,
            category = record.category,
            worldPosition = record.worldPosition,
            sceneName = record.sceneName,
            regionId = record.regionId,
            discovered = record.discovered,
            hidden = record.hidden,
            favorite = record.favorite,
            important = record.important,
            isNavigationTarget = isTarget,
            showOnMinimap = record.showOnMinimap,
            showOnWorldMap = record.showOnWorldMap,
            distance = distance,
            tags = record.tags != null ? record.tags.ToList() : new List<string>(),
            displayText = $"{record.displayName} [{record.category}]"
        };
    }
}

[Serializable]
public class PokeNavMapNavigationTargetRow {
    [Tooltip("Navigation target marker id.")]
    public string markerId;
    [Tooltip("Navigation target display name.")]
    public string markerName;
    [Tooltip("Navigation target category.")]
    public MapMarkerCategory category;
    [Tooltip("Source id that set this target.")]
    public string sourceId;
    [Tooltip("Related region id.")]
    public string regionId;
    [Tooltip("Scene name copied from the target.")]
    public string sceneName;
    [Tooltip("If enabled, World Position is useful.")]
    public bool hasWorldPosition;
    [Tooltip("World position of this target.")]
    public Vector3 worldPosition;
    [Tooltip("Distance from UI origin/player. -1 means unavailable.")]
    public float distance;
    [Tooltip("In-game day when this target was set.")]
    public int setDay;
    [Tooltip("Absolute in-game hour when this target was set.")]
    public int setAbsoluteHour;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;

    public static PokeNavMapNavigationTargetRow FromTarget(MapNavigationTargetState target, Vector3? origin) {
        if(target == null) {
            return null;
        }

        float distance = target.hasWorldPosition && origin.HasValue ? Vector3.Distance(origin.Value, target.worldPosition) : -1f;
        return new PokeNavMapNavigationTargetRow {
            markerId = target.markerId,
            markerName = target.markerName,
            category = target.category,
            sourceId = target.sourceId,
            regionId = target.regionId,
            sceneName = target.sceneName,
            hasWorldPosition = target.hasWorldPosition,
            worldPosition = target.worldPosition,
            distance = distance,
            setDay = target.setDay,
            setAbsoluteHour = target.setAbsoluteHour,
            displayText = $"Target: {target.markerName}"
        };
    }
}

[Serializable]
public class PokeNavGuideSectionRow {
    [Tooltip("Guide section id.")]
    public string sectionId;
    [Tooltip("Guide section display name.")]
    public string displayName;
    [Tooltip("Guide section description.")]
    public string description;
    [Tooltip("Total items returned by the section.")]
    public int itemCount;
    [Tooltip("Available item count.")]
    public int availableCount;
    [Tooltip("Locked item count.")]
    public int lockedCount;
    [Tooltip("Unread item count according to built rows.")]
    public int unreadCount;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;

    public static PokeNavGuideSectionRow FromSection(PokeNavGuideSectionDefinition section, PlayerController player) {
        var items = section.BuildItems(player).ToList();
        int available = items.Count(item => item != null && item.available);
        int locked = items.Count(item => item != null && item.locked);
        int unread = items.Count(item => item != null && !item.read);
        return new PokeNavGuideSectionRow {
            sectionId = section.Id,
            displayName = section.DisplayName,
            description = section.Description,
            itemCount = items.Count,
            availableCount = available,
            lockedCount = locked,
            unreadCount = unread,
            displayText = $"{section.DisplayName} ({available}/{items.Count})"
        };
    }
}

[Serializable]
public class PokeNavGuideItemRow {
    [Tooltip("Content type represented by this item.")]
    public PokeNavGuideContentType contentType;
    [Tooltip("Source item id.")]
    public string itemId;
    [Tooltip("Item title.")]
    public string title;
    [Tooltip("Item subtitle/category.")]
    public string subtitle;
    [Tooltip("Item body text.")]
    public string body;
    [Tooltip("Priority used by sorting.")]
    public int priority;
    [Tooltip("If enabled, item can be opened with details.")]
    public bool available;
    [Tooltip("If enabled, item is shown as locked.")]
    public bool locked;
    [Tooltip("If enabled, item is marked read.")]
    public bool read;
    [Tooltip("If enabled, item is pinned.")]
    public bool pinned;
    [Tooltip("If enabled, item is dismissed.")]
    public bool dismissed;
    [Tooltip("Related Pokemon display name.")]
    public string relatedPokemonName;
    [Tooltip("Related region display name.")]
    public string relatedRegionName;
    [Tooltip("Related map marker id.")]
    public string relatedMapMarkerId;
    [Tooltip("Knowledge level used by Pokedex items.")]
    public PokemonKnowledgeLevel knowledgeLevel;
    [Tooltip("Free-form tags copied from the guide item.")]
    public List<string> tags = new List<string>();
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;

    public static PokeNavGuideItemRow FromRecord(PokeNavGuideItemRecord record) {
        return new PokeNavGuideItemRow {
            contentType = record.contentType,
            itemId = record.itemId,
            title = record.title,
            subtitle = record.subtitle,
            body = record.body,
            priority = record.priority,
            available = record.available,
            locked = record.locked,
            read = record.read,
            pinned = record.pinned,
            dismissed = record.dismissed,
            relatedPokemonName = record.relatedPokemonName,
            relatedRegionName = record.relatedRegionName,
            relatedMapMarkerId = record.relatedMapMarkerId,
            knowledgeLevel = record.knowledgeLevel,
            tags = record.tags != null ? record.tags.ToList() : new List<string>(),
            displayText = $"{record.title} [{record.contentType}]"
        };
    }
}

[Serializable]
public class PokeNavFeedItemRow {
    [Tooltip("Feed item id.")]
    public string itemId;
    [Tooltip("Feed item title.")]
    public string title;
    [Tooltip("Feed source/channel name.")]
    public string sourceName;
    [Tooltip("Feed body text.")]
    public string body;
    [Tooltip("Feed item type.")]
    public PokeNavFeedItemType feedType;
    [Tooltip("Feed priority.")]
    public NotificationPriority priority;
    [Tooltip("If enabled, feed item is read.")]
    public bool read;
    [Tooltip("If enabled, feed item is pinned.")]
    public bool pinned;
    [Tooltip("If enabled, feed item is dismissed.")]
    public bool dismissed;
    [Tooltip("If enabled, feed item is currently active/showable.")]
    public bool active;
    [Tooltip("Related Pokemon display name.")]
    public string relatedPokemonName;
    [Tooltip("Related region display name.")]
    public string relatedRegionName;
    [Tooltip("Related map marker id.")]
    public string relatedMapMarkerId;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;

    public static PokeNavFeedItemRow FromItem(PokeNavFeedItemDefinition item, PlayerPokeNavFeedLog log) {
        bool active = log == null || item.VisibleByDefault || log.HasActiveItem(item, out _);
        return new PokeNavFeedItemRow {
            itemId = item.Id,
            title = item.Title,
            sourceName = item.SourceName,
            body = item.Body,
            feedType = item.FeedType,
            priority = item.Priority,
            read = log != null && log.IsRead(item),
            pinned = log != null ? log.IsPinned(item) : item.PinnedByDefault,
            dismissed = log != null && log.IsDismissed(item),
            active = active,
            relatedPokemonName = item.RelatedPokemon != null ? item.RelatedPokemon.Name : string.Empty,
            relatedRegionName = item.RelatedRegion != null ? item.RelatedRegion.DisplayName : string.Empty,
            relatedMapMarkerId = item.RelatedMapMarker != null ? item.RelatedMapMarker.Id : string.Empty,
            displayText = $"{item.Title} - {item.FeedType}"
        };
    }
}

[Serializable]
public class PokeNavSocialPostRow {
    [Tooltip("Social post id.")]
    public string postId;
    [Tooltip("Social post title.")]
    public string title;
    [Tooltip("Social post author/source.")]
    public string author;
    [Tooltip("Social post body text.")]
    public string body;
    [Tooltip("Social post type.")]
    public SocialPostType postType;
    [Tooltip("Post priority.")]
    public NotificationPriority priority;
    [Tooltip("If enabled, post is pinned.")]
    public bool pinned;
    [Tooltip("If enabled, post is read.")]
    public bool read;
    [Tooltip("Related Pokemon display name.")]
    public string relatedPokemonName;
    [Tooltip("Related region display name.")]
    public string relatedRegionName;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;

    public static PokeNavSocialPostRow FromPost(SocialPostDefinition post, PlayerPokeNavLog log) {
        return new PokeNavSocialPostRow {
            postId = post.Id,
            title = post.Title,
            author = post.Author,
            body = post.Body,
            postType = post.PostType,
            priority = post.Priority,
            pinned = post.Pinned,
            read = log != null && log.IsPostRead(post),
            relatedPokemonName = post.RelatedPokemon != null ? post.RelatedPokemon.Name : string.Empty,
            relatedRegionName = post.RelatedRegion != null ? post.RelatedRegion.DisplayName : string.Empty,
            displayText = $"{post.Title} - {post.Author}"
        };
    }
}

[Serializable]
public class PokeNavPokedexRow {
    [Tooltip("Pokedex entry id.")]
    public string entryId;
    [Tooltip("Pokemon asset id/name.")]
    public string pokemonId;
    [Tooltip("Pokemon display name.")]
    public string displayName;
    [Tooltip("Pokemon classification/species text.")]
    public string classification;
    [Tooltip("Current Pokemon knowledge level.")]
    public PokemonKnowledgeLevel knowledgeLevel;
    [Tooltip("If enabled, the entry has at least Seen-level information.")]
    public bool known;
    [Tooltip("Best note visible for current knowledge level.")]
    public string note;
    [Tooltip("Visible habitat count for current knowledge level.")]
    public int visibleHabitatCount;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;

    public static PokeNavPokedexRow FromEntry(PokedexEntryDefinition entry, PlayerPokeNavLog log) {
        var level = log != null ? log.GetPokemonKnowledgeLevel(entry.Pokemon) : PokemonKnowledgeLevel.Unknown;
        return new PokeNavPokedexRow {
            entryId = entry.Id,
            pokemonId = entry.Pokemon != null ? entry.Pokemon.name : string.Empty,
            displayName = entry.DisplayName,
            classification = entry.Classification,
            knowledgeLevel = level,
            known = level >= PokemonKnowledgeLevel.Seen,
            note = entry.GetBestNote(level),
            visibleHabitatCount = entry.GetVisibleHabitats(level).Count,
            displayText = $"{entry.DisplayName} - {level}"
        };
    }
}

[Serializable]
public class PokeNavRegionRow {
    [Tooltip("Region id.")]
    public string regionId;
    [Tooltip("Region display name.")]
    public string displayName;
    [Tooltip("Region description.")]
    public string description;
    [Tooltip("Region type.")]
    public RegionInfoType regionType;
    [Tooltip("Scene name connected to this region.")]
    public string sceneName;
    [Tooltip("If enabled, region should be visible in UI.")]
    public bool visible;
    [Tooltip("If enabled, player can discover this region now.")]
    public bool canDiscover;
    [Tooltip("Failure/reason when discovery is blocked.")]
    public string failureMessage;
    [Tooltip("Listed Pokemon count.")]
    public int listedPokemonCount;
    [Tooltip("Shop count linked to this region.")]
    public int shopCount;
    [Tooltip("Transit stop count linked to this region.")]
    public int transitStopCount;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;

    public static PokeNavRegionRow FromRegion(RegionInfoDefinition region, PlayerController player, PlayerPokeNavLog log) {
        bool visible = log != null ? log.HasDiscoveredRegion(region) : region.VisibleByDefault;
        bool canDiscover = region.CanDiscover(player, out var failure);
        return new PokeNavRegionRow {
            regionId = region.Id,
            displayName = region.DisplayName,
            description = visible || canDiscover ? region.Description : string.Empty,
            regionType = region.RegionType,
            sceneName = region.SceneName,
            visible = visible,
            canDiscover = canDiscover,
            failureMessage = failure,
            listedPokemonCount = region.GetListedPokemon().Count,
            shopCount = region.Shops != null ? region.Shops.Count(shop => shop != null) : 0,
            transitStopCount = region.TransitStops != null ? region.TransitStops.Count(stop => stop != null) : 0,
            displayText = $"{region.DisplayName} [{region.RegionType}]"
        };
    }
}

[Serializable]
public class PokeNavKnowledgeEntryRow {
    [Tooltip("PokeNav entry id.")]
    public string entryId;
    [Tooltip("PokeNav entry display name.")]
    public string displayName;
    [Tooltip("Entry body text.")]
    public string body;
    [Tooltip("Entry type.")]
    public PokeNavEntryType entryType;
    [Tooltip("If enabled, entry is visible/discovered.")]
    public bool visible;
    [Tooltip("If enabled, player can discover this entry now.")]
    public bool canDiscover;
    [Tooltip("Failure/reason when discovery is blocked.")]
    public string failureMessage;
    [Tooltip("Related Pokemon display name.")]
    public string relatedPokemonName;
    [Tooltip("Related region display name.")]
    public string relatedRegionName;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;

    public static PokeNavKnowledgeEntryRow FromEntry(PokeNavEntryDefinition entry, PlayerController player, PlayerPokeNavLog log) {
        bool visible = log != null ? log.HasDiscoveredEntry(entry) : entry.VisibleByDefault;
        bool canDiscover = entry.CanDiscover(player, out var failure);
        return new PokeNavKnowledgeEntryRow {
            entryId = entry.Id,
            displayName = entry.DisplayName,
            body = visible || canDiscover ? entry.Body : string.Empty,
            entryType = entry.EntryType,
            visible = visible,
            canDiscover = canDiscover,
            failureMessage = failure,
            relatedPokemonName = entry.RelatedPokemon != null ? entry.RelatedPokemon.Name : string.Empty,
            relatedRegionName = entry.RelatedRegion != null ? entry.RelatedRegion.DisplayName : string.Empty,
            displayText = $"{entry.DisplayName} [{entry.EntryType}]"
        };
    }
}
