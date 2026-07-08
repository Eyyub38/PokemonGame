using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ProgressionFocusedPanelType {
    Overview,
    Titles,
    Careers,
    Milestones,
    Reputation,
    Access
}

public enum ProgressionFocusedPanelActionKind {
    None,
    Refreshed,
    PanelChanged,
    SearchChanged,
    TagChanged,
    FlagsChanged,
    Cleared,
    Blocked
}

public class ProgressionFocusedPanelUIManager : MonoBehaviour {
    [Header("Source")]
    [Tooltip("Source progression/access UI manager. Empty searches this GameObject, then the scene, then creates one on this GameObject.")]
    [SerializeField] ProgressionAccessUIManager source;
    [Tooltip("If enabled, Source.Refresh is called before building this focused snapshot.")]
    [SerializeField] bool refreshSourceBeforeSnapshot = true;

    [Header("Panel")]
    [Tooltip("Focused progression panel shown by this backend.")]
    [SerializeField] ProgressionFocusedPanelType activePanel = ProgressionFocusedPanelType.Overview;
    [Tooltip("Case-insensitive search text matched against names, descriptions, ids and tags.")]
    [SerializeField] string searchText = string.Empty;
    [Tooltip("Optional tag filters. Empty accepts all tags.")]
    [SerializeField] List<string> activeTags = new List<string>();
    [Tooltip("Required tag matching mode when Active Tags has entries.")]
    [SerializeField] MapMarkerFilterMatchMode activeTagMatchMode = MapMarkerFilterMatchMode.Any;

    [Header("Visibility Filters")]
    [Tooltip("If enabled, inactive titles are hidden in the focused title panel.")]
    [SerializeField] bool showOnlyActiveTitles;
    [Tooltip("If enabled, unjoined careers are hidden in the focused career panel.")]
    [SerializeField] bool showOnlyJoinedCareers;
    [Tooltip("If enabled, locked careers are hidden in the focused career panel.")]
    [SerializeField] bool hideLockedCareers;
    [Tooltip("If enabled, incomplete milestones are hidden in the focused milestone panel.")]
    [SerializeField] bool showOnlyCompletedMilestones;
    [Tooltip("If enabled, reputation rows with zero value are hidden in the focused reputation panel.")]
    [SerializeField] bool hideNeutralReputation;
    [Tooltip("If enabled, access rows that cannot currently pass are hidden in the focused access panel.")]
    [SerializeField] bool showOnlyAccessibleAccess;

    [Header("Snapshot")]
    [Tooltip("If enabled, Refresh is called when this component starts.")]
    [SerializeField] bool refreshOnStart = true;
    [Tooltip("If enabled, Refresh is called after panel/filter actions.")]
    [SerializeField] bool refreshAfterActions = true;
    [Tooltip("Maximum rows copied per focused list. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxRows = 60;

    [Header("Debug")]
    [Tooltip("If enabled, successful focused panel actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulActions;
    [Tooltip("If enabled, blocked focused panel actions are written to GameDebug.")]
    [SerializeField] bool logBlockedActions = true;

    ProgressionFocusedPanelSnapshot currentSnapshot = new ProgressionFocusedPanelSnapshot();
    ProgressionFocusedPanelActionResult lastResult = new ProgressionFocusedPanelActionResult();

    public ProgressionAccessUIManager Source => source;
    public ProgressionFocusedPanelSnapshot CurrentSnapshot => currentSnapshot;
    public ProgressionFocusedPanelActionResult LastResult => lastResult;
    public ProgressionFocusedPanelType ActivePanel => activePanel;
    public string SearchText => searchText;
    public IReadOnlyList<string> ActiveTags => activeTags;
    public int MaxRows => Mathf.Max(0, maxRows);
    public event Action<ProgressionFocusedPanelSnapshot> OnSnapshotChanged;
    public event Action<ProgressionFocusedPanelActionResult> OnActionResult;

    void Start() {
        if(refreshOnStart) {
            Refresh();
        }
    }

    [ContextMenu("Refresh Focused Progression Snapshot")]
    public ProgressionFocusedPanelSnapshot RefreshFromContextMenu() {
        return Refresh();
    }

    public ProgressionFocusedPanelSnapshot Refresh() {
        var manager = ResolveSource();
        if(manager == null) {
            currentSnapshot = BuildBlockedSnapshot("ProgressionAccessUIManager could not be resolved.");
            OnSnapshotChanged?.Invoke(currentSnapshot);
            return currentSnapshot;
        }

        var sourceSnapshot = refreshSourceBeforeSnapshot ? manager.Refresh() : manager.CurrentSnapshot;
        if(sourceSnapshot == null) {
            currentSnapshot = BuildBlockedSnapshot("Progression source snapshot is empty.");
            OnSnapshotChanged?.Invoke(currentSnapshot);
            return currentSnapshot;
        }

        var titles = FilterTitles(sourceSnapshot.titleRows).ToList();
        var careers = FilterCareers(sourceSnapshot.careerRows).ToList();
        var milestones = FilterMilestones(sourceSnapshot.milestoneRows).ToList();
        var reputation = FilterReputation(sourceSnapshot.reputationRows).ToList();
        var access = FilterAccess(sourceSnapshot.accessRows).ToList();

        currentSnapshot = new ProgressionFocusedPanelSnapshot {
            hasSource = manager != null,
            hasPlayer = sourceSnapshot.hasPlayer,
            playerName = sourceSnapshot.playerName,
            activePanel = activePanel,
            searchText = searchText,
            activeTags = activeTags != null ? activeTags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() : new List<string>(),
            sourceActiveTitleCount = sourceSnapshot.activeTitleCount,
            sourceJoinedCareerCount = sourceSnapshot.joinedCareerCount,
            sourceCompletedMilestoneCount = sourceSnapshot.completedMilestoneCount,
            sourceKnownReputationCount = sourceSnapshot.knownReputationCount,
            sourceAccessPassedCount = sourceSnapshot.accessPassedCount,
            sourceAccessDeniedCount = sourceSnapshot.accessDeniedCount,
            visibleTitleCount = titles.Count,
            visibleCareerCount = careers.Count,
            visibleMilestoneCount = milestones.Count,
            visibleReputationCount = reputation.Count,
            visibleAccessCount = access.Count,
            panelRows = BuildPanelRows(titles, careers, milestones, reputation, access),
            titleRows = activePanel == ProgressionFocusedPanelType.Titles || activePanel == ProgressionFocusedPanelType.Overview ? Limit(titles).ToList() : new List<ProgressionTitleRow>(),
            careerRows = activePanel == ProgressionFocusedPanelType.Careers || activePanel == ProgressionFocusedPanelType.Overview ? Limit(careers).ToList() : new List<ProgressionCareerRow>(),
            milestoneRows = activePanel == ProgressionFocusedPanelType.Milestones || activePanel == ProgressionFocusedPanelType.Overview ? Limit(milestones).ToList() : new List<ProgressionMilestoneRow>(),
            reputationRows = activePanel == ProgressionFocusedPanelType.Reputation || activePanel == ProgressionFocusedPanelType.Overview ? Limit(reputation).ToList() : new List<ProgressionReputationRow>(),
            accessRows = activePanel == ProgressionFocusedPanelType.Access || activePanel == ProgressionFocusedPanelType.Overview ? Limit(access).ToList() : new List<ProgressionAccessRow>(),
            lastResult = lastResult
        };

        OnSnapshotChanged?.Invoke(currentSnapshot);
        return currentSnapshot;
    }

    public bool SetPanel(ProgressionFocusedPanelType panel, out string feedback) {
        activePanel = panel;
        bool success = Succeed(ProgressionFocusedPanelActionKind.PanelChanged, $"{panel} panel selected.", out feedback);
        RefreshIfNeeded();
        return success;
    }

    public bool SetSearchText(string value, out string feedback) {
        searchText = value ?? string.Empty;
        bool success = Succeed(ProgressionFocusedPanelActionKind.SearchChanged, "Progression panel search changed.", out feedback);
        RefreshIfNeeded();
        return success;
    }

    public bool ToggleTag(string tag, out string feedback) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return Block("No progression tag was selected.", out feedback);
        }

        int index = activeTags.FindIndex(item => string.Equals(item, tag, StringComparison.OrdinalIgnoreCase));
        if(index >= 0) {
            activeTags.RemoveAt(index);
        } else {
            activeTags.Add(tag);
        }

        bool success = Succeed(ProgressionFocusedPanelActionKind.TagChanged, $"Progression tag '{tag}' changed.", out feedback);
        RefreshIfNeeded();
        return success;
    }

    public bool SetTitleActiveOnly(bool value, out string feedback) {
        showOnlyActiveTitles = value;
        return SetFlagsChanged(out feedback);
    }

    public bool SetCareerJoinedOnly(bool value, out string feedback) {
        showOnlyJoinedCareers = value;
        return SetFlagsChanged(out feedback);
    }

    public bool SetHideLockedCareers(bool value, out string feedback) {
        hideLockedCareers = value;
        return SetFlagsChanged(out feedback);
    }

    public bool SetMilestonesCompletedOnly(bool value, out string feedback) {
        showOnlyCompletedMilestones = value;
        return SetFlagsChanged(out feedback);
    }

    public bool SetHideNeutralReputation(bool value, out string feedback) {
        hideNeutralReputation = value;
        return SetFlagsChanged(out feedback);
    }

    public bool SetAccessAccessibleOnly(bool value, out string feedback) {
        showOnlyAccessibleAccess = value;
        return SetFlagsChanged(out feedback);
    }

    public bool ClearFilters(out string feedback) {
        searchText = string.Empty;
        activeTags.Clear();
        showOnlyActiveTitles = false;
        showOnlyJoinedCareers = false;
        hideLockedCareers = false;
        showOnlyCompletedMilestones = false;
        hideNeutralReputation = false;
        showOnlyAccessibleAccess = false;
        bool success = Succeed(ProgressionFocusedPanelActionKind.Cleared, "Progression focused filters cleared.", out feedback);
        RefreshIfNeeded();
        return success;
    }

    bool SetFlagsChanged(out string feedback) {
        bool success = Succeed(ProgressionFocusedPanelActionKind.FlagsChanged, "Progression focused filter flags changed.", out feedback);
        RefreshIfNeeded();
        return success;
    }

    IEnumerable<ProgressionTitleRow> FilterTitles(IEnumerable<ProgressionTitleRow> rows) {
        var filtered = (rows ?? Enumerable.Empty<ProgressionTitleRow>()).Where(row => row != null);
        if(showOnlyActiveTitles) {
            filtered = filtered.Where(row => row.active);
        }

        return ApplyCommonFilters(filtered, row => row.titleId, row => row.displayName, row => row.description, row => row.tags)
            .OrderByDescending(row => row.active)
            .ThenBy(row => row.kind)
            .ThenBy(row => row.displayName);
    }

    IEnumerable<ProgressionCareerRow> FilterCareers(IEnumerable<ProgressionCareerRow> rows) {
        var filtered = (rows ?? Enumerable.Empty<ProgressionCareerRow>()).Where(row => row != null);
        if(showOnlyJoinedCareers) {
            filtered = filtered.Where(row => row.joined);
        }

        if(hideLockedCareers) {
            filtered = filtered.Where(row => row.unlocked || row.joined || row.canJoin);
        }

        return ApplyCommonFilters(filtered, row => row.careerId, row => row.displayName, row => row.description, row => row.tags)
            .OrderByDescending(row => row.joined)
            .ThenByDescending(row => row.unlocked)
            .ThenBy(row => row.category)
            .ThenBy(row => row.displayName);
    }

    IEnumerable<ProgressionMilestoneRow> FilterMilestones(IEnumerable<ProgressionMilestoneRow> rows) {
        var filtered = (rows ?? Enumerable.Empty<ProgressionMilestoneRow>()).Where(row => row != null);
        if(showOnlyCompletedMilestones) {
            filtered = filtered.Where(row => row.completed);
        }

        return ApplyCommonFilters(filtered, row => row.milestoneId, row => row.displayName, row => row.description, row => Array.Empty<string>())
            .OrderByDescending(row => row.completed)
            .ThenBy(row => row.displayName);
    }

    IEnumerable<ProgressionReputationRow> FilterReputation(IEnumerable<ProgressionReputationRow> rows) {
        var filtered = (rows ?? Enumerable.Empty<ProgressionReputationRow>()).Where(row => row != null);
        if(hideNeutralReputation) {
            filtered = filtered.Where(row => row.value != 0);
        }

        return ApplyCommonFilters(filtered, row => row.factionId, row => row.displayName, row => row.description, row => Array.Empty<string>())
            .OrderBy(row => row.value < 0)
            .ThenByDescending(row => Mathf.Abs(row.value))
            .ThenBy(row => row.displayName);
    }

    IEnumerable<ProgressionAccessRow> FilterAccess(IEnumerable<ProgressionAccessRow> rows) {
        var filtered = (rows ?? Enumerable.Empty<ProgressionAccessRow>()).Where(row => row != null);
        if(showOnlyAccessibleAccess) {
            filtered = filtered.Where(row => row.canAccessNow);
        }

        return ApplyCommonFilters(filtered, row => row.profileId, row => row.displayName, row => row.description, row => row.tags)
            .OrderByDescending(row => row.canAccessNow)
            .ThenBy(row => row.category)
            .ThenBy(row => row.displayName);
    }

    IEnumerable<T> ApplyCommonFilters<T>(IEnumerable<T> rows, Func<T, string> id, Func<T, string> name, Func<T, string> description, Func<T, IEnumerable<string>> tags) {
        var filtered = rows ?? Enumerable.Empty<T>();
        if(activeTags != null && activeTags.Any(tag => !string.IsNullOrWhiteSpace(tag))) {
            var selectedTags = activeTags.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList();
            filtered = activeTagMatchMode == MapMarkerFilterMatchMode.All
                ? filtered.Where(row => selectedTags.All(tag => HasTag(tags(row), tag)))
                : filtered.Where(row => selectedTags.Any(tag => HasTag(tags(row), tag)));
        }

        if(!string.IsNullOrWhiteSpace(searchText)) {
            filtered = filtered.Where(row =>
                Contains(id(row), searchText)
                || Contains(name(row), searchText)
                || Contains(description(row), searchText)
                || (tags(row) != null && tags(row).Any(tag => Contains(tag, searchText))));
        }

        return filtered;
    }

    List<ProgressionFocusedPanelRow> BuildPanelRows(
        List<ProgressionTitleRow> titles,
        List<ProgressionCareerRow> careers,
        List<ProgressionMilestoneRow> milestones,
        List<ProgressionReputationRow> reputation,
        List<ProgressionAccessRow> access) {
        return new List<ProgressionFocusedPanelRow> {
            ProgressionFocusedPanelRow.From(ProgressionFocusedPanelType.Overview, "Overview", titles.Count + careers.Count + milestones.Count + reputation.Count + access.Count, activePanel == ProgressionFocusedPanelType.Overview),
            ProgressionFocusedPanelRow.From(ProgressionFocusedPanelType.Titles, "Titles", titles.Count, activePanel == ProgressionFocusedPanelType.Titles),
            ProgressionFocusedPanelRow.From(ProgressionFocusedPanelType.Careers, "Careers", careers.Count, activePanel == ProgressionFocusedPanelType.Careers),
            ProgressionFocusedPanelRow.From(ProgressionFocusedPanelType.Milestones, "Milestones", milestones.Count, activePanel == ProgressionFocusedPanelType.Milestones),
            ProgressionFocusedPanelRow.From(ProgressionFocusedPanelType.Reputation, "Reputation", reputation.Count, activePanel == ProgressionFocusedPanelType.Reputation),
            ProgressionFocusedPanelRow.From(ProgressionFocusedPanelType.Access, "Access", access.Count, activePanel == ProgressionFocusedPanelType.Access)
        };
    }

    IEnumerable<T> Limit<T>(IEnumerable<T> source) {
        if(source == null) {
            return Enumerable.Empty<T>();
        }

        return MaxRows > 0 ? source.Take(MaxRows) : source;
    }

    ProgressionAccessUIManager ResolveSource() {
        if(source != null) {
            return source;
        }

        source = GetComponent<ProgressionAccessUIManager>();
        if(source != null) {
            return source;
        }

        source = FindAnyObjectByType<ProgressionAccessUIManager>();
        if(source != null) {
            return source;
        }

        source = gameObject.AddComponent<ProgressionAccessUIManager>();
        return source;
    }

    ProgressionFocusedPanelSnapshot BuildBlockedSnapshot(string message) {
        return new ProgressionFocusedPanelSnapshot {
            hasSource = false,
            activePanel = activePanel,
            searchText = searchText,
            lastResult = BuildResult(ProgressionFocusedPanelActionKind.Blocked, false, message)
        };
    }

    void RefreshIfNeeded() {
        if(refreshAfterActions) {
            Refresh();
        }
    }

    bool Succeed(ProgressionFocusedPanelActionKind kind, string message, out string feedback) {
        feedback = message;
        lastResult = BuildResult(kind, true, message);
        if(logSuccessfulActions) {
            GameDebug.Step(message, GameDebugCategory.UI, this, "ProgressionFocusedPanelUI");
        }

        OnActionResult?.Invoke(lastResult);
        return true;
    }

    bool Block(string message, out string feedback) {
        feedback = message;
        lastResult = BuildResult(ProgressionFocusedPanelActionKind.Blocked, false, message);
        if(logBlockedActions) {
            GameDebug.Warning(message, GameDebugCategory.UI, this, "ProgressionFocusedPanelUI");
        }

        OnActionResult?.Invoke(lastResult);
        RefreshIfNeeded();
        return false;
    }

    ProgressionFocusedPanelActionResult BuildResult(ProgressionFocusedPanelActionKind kind, bool success, string message) {
        return new ProgressionFocusedPanelActionResult {
            kind = kind,
            success = success,
            message = message
        };
    }

    static bool Contains(string value, string needle) {
        return !string.IsNullOrWhiteSpace(value)
            && !string.IsNullOrWhiteSpace(needle)
            && value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static bool HasTag(IEnumerable<string> tags, string tag) {
        return tags != null
            && !string.IsNullOrWhiteSpace(tag)
            && tags.Any(item => string.Equals(item, tag, StringComparison.OrdinalIgnoreCase));
    }
}

[Serializable]
public class ProgressionFocusedPanelSnapshot {
    [Tooltip("If enabled, a ProgressionAccessUIManager source was found.")]
    public bool hasSource;
    [Tooltip("If enabled, source snapshot has a player.")]
    public bool hasPlayer;
    [Tooltip("Player GameObject name used by the source snapshot.")]
    public string playerName;
    [Tooltip("Currently focused panel.")]
    public ProgressionFocusedPanelType activePanel;
    [Tooltip("Active search text.")]
    public string searchText;
    [Tooltip("Active progression tag filters.")]
    public List<string> activeTags = new List<string>();
    [Tooltip("Source active title count.")]
    public int sourceActiveTitleCount;
    [Tooltip("Source joined career count.")]
    public int sourceJoinedCareerCount;
    [Tooltip("Source completed milestone count.")]
    public int sourceCompletedMilestoneCount;
    [Tooltip("Source known reputation count.")]
    public int sourceKnownReputationCount;
    [Tooltip("Source access pass count.")]
    public int sourceAccessPassedCount;
    [Tooltip("Source access denied count.")]
    public int sourceAccessDeniedCount;
    [Tooltip("Visible title rows after filters.")]
    public int visibleTitleCount;
    [Tooltip("Visible career rows after filters.")]
    public int visibleCareerCount;
    [Tooltip("Visible milestone rows after filters.")]
    public int visibleMilestoneCount;
    [Tooltip("Visible reputation rows after filters.")]
    public int visibleReputationCount;
    [Tooltip("Visible access rows after filters.")]
    public int visibleAccessCount;
    [Tooltip("Available focused panel tabs/rows.")]
    public List<ProgressionFocusedPanelRow> panelRows = new List<ProgressionFocusedPanelRow>();
    [Tooltip("Focused title rows.")]
    public List<ProgressionTitleRow> titleRows = new List<ProgressionTitleRow>();
    [Tooltip("Focused career rows.")]
    public List<ProgressionCareerRow> careerRows = new List<ProgressionCareerRow>();
    [Tooltip("Focused milestone rows.")]
    public List<ProgressionMilestoneRow> milestoneRows = new List<ProgressionMilestoneRow>();
    [Tooltip("Focused reputation rows.")]
    public List<ProgressionReputationRow> reputationRows = new List<ProgressionReputationRow>();
    [Tooltip("Focused access rows.")]
    public List<ProgressionAccessRow> accessRows = new List<ProgressionAccessRow>();
    [Tooltip("Most recent focused panel action result.")]
    public ProgressionFocusedPanelActionResult lastResult;
}

[Serializable]
public class ProgressionFocusedPanelActionResult {
    [Tooltip("Kind of focused panel action that produced this result.")]
    public ProgressionFocusedPanelActionKind kind;
    [Tooltip("If enabled, the action succeeded.")]
    public bool success;
    [Tooltip("Readable result, failure or feedback text.")]
    public string message;
}

[Serializable]
public class ProgressionFocusedPanelRow {
    [Tooltip("Panel type represented by this row.")]
    public ProgressionFocusedPanelType panelType;
    [Tooltip("Panel display name.")]
    public string displayName;
    [Tooltip("Visible row count for this panel.")]
    public int visibleCount;
    [Tooltip("If enabled, this panel is currently selected.")]
    public bool selected;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;

    public static ProgressionFocusedPanelRow From(ProgressionFocusedPanelType panelType, string displayName, int visibleCount, bool selected) {
        return new ProgressionFocusedPanelRow {
            panelType = panelType,
            displayName = displayName,
            visibleCount = Mathf.Max(0, visibleCount),
            selected = selected,
            displayText = $"{displayName} ({Mathf.Max(0, visibleCount)})"
        };
    }
}
