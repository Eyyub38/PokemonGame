using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokeNavKnowledgeDetailType {
    None,
    Pokedex,
    Region,
    KnowledgeEntry
}

public enum PokeNavKnowledgeDetailActionKind {
    None,
    Refreshed,
    SelectionChanged,
    RegionDiscovered,
    EntryDiscovered,
    PokemonKnowledgeRecorded,
    Blocked
}

public class PokeNavKnowledgeDetailUIManager : MonoBehaviour {
    [Header("Player")]
    [Tooltip("Player whose PokeNav state is used. Empty uses PlayerController.i or the first PlayerController in the scene.")]
    [SerializeField] PlayerController playerOverride;
    [Tooltip("If enabled, missing PlayerPokeNavLog components are created when actions need them.")]
    [SerializeField] bool createMissingLogForActions = true;

    [Header("Selection")]
    [Tooltip("Current detail type shown by this backend.")]
    [SerializeField] PokeNavKnowledgeDetailType selectedType = PokeNavKnowledgeDetailType.None;
    [Tooltip("Selected Pokedex entry id, region id or knowledge entry id depending on Selected Type.")]
    [SerializeField] string selectedId = string.Empty;
    [Tooltip("Minimum knowledge level used when manually recording selected Pokemon knowledge from this detail panel.")]
    [SerializeField] PokemonKnowledgeLevel manualKnowledgeLevel = PokemonKnowledgeLevel.Seen;

    [Header("Snapshot")]
    [Tooltip("If enabled, Refresh is called when this component starts.")]
    [SerializeField] bool refreshOnStart = true;
    [Tooltip("If enabled, Refresh is called after selection/actions.")]
    [SerializeField] bool refreshAfterActions = true;
    [Tooltip("Maximum rows copied per detail list. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxRowsPerList = 80;
    [Tooltip("Source id written into PokeNav discovery/knowledge actions.")]
    [SerializeField] string uiSourceId = "ui:pokenav-detail";

    [Header("Debug")]
    [Tooltip("If enabled, successful detail backend actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulActions;
    [Tooltip("If enabled, blocked detail backend actions are written to GameDebug.")]
    [SerializeField] bool logBlockedActions = true;

    PokeNavKnowledgeDetailSnapshot currentSnapshot = new PokeNavKnowledgeDetailSnapshot();
    PokeNavKnowledgeDetailActionResult lastResult = new PokeNavKnowledgeDetailActionResult();

    public PokeNavKnowledgeDetailSnapshot CurrentSnapshot => currentSnapshot;
    public PokeNavKnowledgeDetailActionResult LastResult => lastResult;
    public PokeNavKnowledgeDetailType SelectedType => selectedType;
    public string SelectedId => selectedId;
    public int MaxRowsPerList => Mathf.Max(0, maxRowsPerList);
    public event Action<PokeNavKnowledgeDetailSnapshot> OnSnapshotChanged;
    public event Action<PokeNavKnowledgeDetailActionResult> OnActionResult;

    void Start() {
        if(refreshOnStart) {
            Refresh();
        }
    }

    [ContextMenu("Refresh PokeNav Detail Snapshot")]
    public PokeNavKnowledgeDetailSnapshot RefreshFromContextMenu() {
        return Refresh();
    }

    public PokeNavKnowledgeDetailSnapshot Refresh() {
        var player = ResolvePlayer();
        var log = player != null ? player.GetComponent<PlayerPokeNavLog>() : null;

        currentSnapshot = selectedType switch {
            PokeNavKnowledgeDetailType.Pokedex => BuildPokedexSnapshot(log),
            PokeNavKnowledgeDetailType.Region => BuildRegionSnapshot(player, log),
            PokeNavKnowledgeDetailType.KnowledgeEntry => BuildEntrySnapshot(player, log),
            _ => BuildEmptySnapshot("No PokeNav detail selected.")
        };

        currentSnapshot.hasPlayer = player != null;
        currentSnapshot.playerName = player != null ? player.name : string.Empty;
        currentSnapshot.selectedType = selectedType;
        currentSnapshot.selectedId = selectedId;
        currentSnapshot.day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
        currentSnapshot.hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0;
        currentSnapshot.absoluteHour = GetCurrentAbsoluteHour();
        currentSnapshot.lastResult = lastResult;
        OnSnapshotChanged?.Invoke(currentSnapshot);
        return currentSnapshot;
    }

    public bool Select(PokeNavKnowledgeDetailType type, string id, out string feedback) {
        selectedType = type;
        selectedId = id ?? string.Empty;
        bool success = Succeed(PokeNavKnowledgeDetailActionKind.SelectionChanged, "PokeNav detail selection changed.", out feedback);
        RefreshIfNeeded();
        return success;
    }

    public bool SelectPokedex(string entryId, out string feedback) {
        return Select(PokeNavKnowledgeDetailType.Pokedex, entryId, out feedback);
    }

    public bool SelectRegion(string regionId, out string feedback) {
        return Select(PokeNavKnowledgeDetailType.Region, regionId, out feedback);
    }

    public bool SelectKnowledgeEntry(string entryId, out string feedback) {
        return Select(PokeNavKnowledgeDetailType.KnowledgeEntry, entryId, out feedback);
    }

    public bool TryDiscoverSelectedRegion(out string feedback) {
        if(selectedType != PokeNavKnowledgeDetailType.Region) {
            return Block("Selected detail is not a region.", out feedback);
        }

        var player = ResolvePlayer();
        var region = FindResourceById<RegionInfoDefinition>(selectedId, item => item.Id);
        var log = GetOrCreateLog(player);
        feedback = null;
        if(region != null && log != null && log.DiscoverRegion(region, out feedback)) {
            bool success = Succeed(PokeNavKnowledgeDetailActionKind.RegionDiscovered, $"{region.DisplayName} discovered.", out feedback);
            RefreshIfNeeded();
            return success;
        }

        return Block(string.IsNullOrWhiteSpace(feedback) ? $"Region '{selectedId}' could not be discovered." : feedback, out feedback);
    }

    public bool TryDiscoverSelectedEntry(out string feedback) {
        if(selectedType != PokeNavKnowledgeDetailType.KnowledgeEntry) {
            return Block("Selected detail is not a knowledge entry.", out feedback);
        }

        var player = ResolvePlayer();
        var entry = FindResourceById<PokeNavEntryDefinition>(selectedId, item => item.Id);
        var log = GetOrCreateLog(player);
        feedback = null;
        if(entry != null && log != null && log.DiscoverEntry(entry, out feedback)) {
            bool success = Succeed(PokeNavKnowledgeDetailActionKind.EntryDiscovered, $"{entry.DisplayName} discovered.", out feedback);
            RefreshIfNeeded();
            return success;
        }

        return Block(string.IsNullOrWhiteSpace(feedback) ? $"Entry '{selectedId}' could not be discovered." : feedback, out feedback);
    }

    public bool TryRecordSelectedPokemonKnowledge(out string feedback) {
        if(selectedType != PokeNavKnowledgeDetailType.Pokedex) {
            return Block("Selected detail is not a Pokedex entry.", out feedback);
        }

        var entry = FindResourceById<PokedexEntryDefinition>(selectedId, item => item.Id);
        var log = GetOrCreateLog(ResolvePlayer());
        if(entry == null || entry.Pokemon == null || log == null) {
            return Block("Selected Pokedex entry cannot record Pokemon knowledge.", out feedback);
        }

        bool changed = log.RecordPokemonKnowledge(entry.Pokemon, manualKnowledgeLevel, ResolveSourceId());
        bool success = Succeed(PokeNavKnowledgeDetailActionKind.PokemonKnowledgeRecorded, changed ? $"{entry.DisplayName} knowledge updated." : $"{entry.DisplayName} knowledge was already known.", out feedback);
        RefreshIfNeeded();
        return success;
    }

    PokeNavKnowledgeDetailSnapshot BuildPokedexSnapshot(PlayerPokeNavLog log) {
        var entry = FindResourceById<PokedexEntryDefinition>(selectedId, item => item.Id);
        if(entry == null) {
            return BuildEmptySnapshot($"Pokedex entry '{selectedId}' could not be found.");
        }

        var level = log != null ? log.GetPokemonKnowledgeLevel(entry.Pokemon) : PokemonKnowledgeLevel.Unknown;
        var snapshot = BuildBaseSnapshot(entry.Id, entry.DisplayName, entry.Classification, entry.GetBestNote(level), level >= PokemonKnowledgeLevel.Seen, string.Empty);
        snapshot.pokemonId = entry.Pokemon != null ? entry.Pokemon.name : string.Empty;
        snapshot.pokemonName = entry.Pokemon != null ? entry.Pokemon.Name : entry.DisplayName;
        snapshot.knowledgeLevel = level;
        snapshot.tags = entry.Tags != null ? entry.Tags.ToList() : new List<string>();
        snapshot.habitatRows = Limit(entry.GetVisibleHabitats(level).Select(PokeNavHabitatDetailRow.FromHabitat)).ToList();
        snapshot.careHintRows = Limit((entry.CareHints ?? Array.Empty<PokedexCareHint>()).Where(hint => hint != null && level >= hint.minimumKnowledgeToReveal).Select(PokeNavCareHintDetailRow.FromHint)).ToList();
        snapshot.linkRows = BuildPokedexLinks(entry);
        snapshot.summaryText = $"{snapshot.pokemonName} - {level}";
        return snapshot;
    }

    PokeNavKnowledgeDetailSnapshot BuildRegionSnapshot(PlayerController player, PlayerPokeNavLog log) {
        var region = FindResourceById<RegionInfoDefinition>(selectedId, item => item.Id);
        if(region == null) {
            return BuildEmptySnapshot($"Region '{selectedId}' could not be found.");
        }

        bool visible = log != null ? log.HasDiscoveredRegion(region) : region.VisibleByDefault;
        bool canDiscover = region.CanDiscover(player, out var failure);
        var snapshot = BuildBaseSnapshot(region.Id, region.DisplayName, region.RegionType.ToString(), visible || canDiscover ? region.Description : string.Empty, visible, failure);
        snapshot.regionId = region.Id;
        snapshot.regionName = region.DisplayName;
        snapshot.sceneName = region.SceneName;
        snapshot.tags = region.Tags != null ? region.Tags.ToList() : new List<string>();
        snapshot.pokemonRows = Limit(region.GetListedPokemon().Select(PokeNavRelatedPokemonDetailRow.FromPokemon)).ToList();
        snapshot.linkRows = BuildRegionLinks(region);
        snapshot.summaryText = $"{region.DisplayName} - {region.RegionType}";
        return snapshot;
    }

    PokeNavKnowledgeDetailSnapshot BuildEntrySnapshot(PlayerController player, PlayerPokeNavLog log) {
        var entry = FindResourceById<PokeNavEntryDefinition>(selectedId, item => item.Id);
        if(entry == null) {
            return BuildEmptySnapshot($"PokeNav entry '{selectedId}' could not be found.");
        }

        bool visible = log != null ? log.HasDiscoveredEntry(entry) : entry.VisibleByDefault;
        bool canDiscover = entry.CanDiscover(player, out var failure);
        var snapshot = BuildBaseSnapshot(entry.Id, entry.DisplayName, entry.EntryType.ToString(), visible || canDiscover ? entry.Body : string.Empty, visible, failure);
        snapshot.relatedPokemonName = entry.RelatedPokemon != null ? entry.RelatedPokemon.Name : string.Empty;
        snapshot.regionId = entry.RelatedRegion != null ? entry.RelatedRegion.Id : string.Empty;
        snapshot.regionName = entry.RelatedRegion != null ? entry.RelatedRegion.DisplayName : string.Empty;
        snapshot.tags = entry.Tags != null ? entry.Tags.ToList() : new List<string>();
        snapshot.linkRows = BuildEntryLinks(entry);
        snapshot.summaryText = $"{entry.DisplayName} - {entry.EntryType}";
        return snapshot;
    }

    PokeNavKnowledgeDetailSnapshot BuildBaseSnapshot(string id, string title, string subtitle, string body, bool visible, string blockedReason) {
        return new PokeNavKnowledgeDetailSnapshot {
            detailId = id,
            title = title,
            subtitle = subtitle,
            body = body,
            visible = visible,
            canDiscover = string.IsNullOrWhiteSpace(blockedReason),
            blockedReason = blockedReason ?? string.Empty
        };
    }

    PokeNavKnowledgeDetailSnapshot BuildEmptySnapshot(string message) {
        return new PokeNavKnowledgeDetailSnapshot {
            title = "PokeNav Detail",
            body = message,
            blockedReason = message,
            canDiscover = false
        };
    }

    List<PokeNavDetailLinkRow> BuildPokedexLinks(PokedexEntryDefinition entry) {
        var rows = new List<PokeNavDetailLinkRow>();
        if(entry == null) {
            return rows;
        }

        foreach(var research in entry.RelatedResearch ?? Array.Empty<ResearchSubjectDefinition>()) {
            if(research != null) {
                rows.Add(PokeNavDetailLinkRow.From("Research", research.Id, research.DisplayName, string.Empty));
            }
        }

        return Limit(rows).ToList();
    }

    List<PokeNavDetailLinkRow> BuildRegionLinks(RegionInfoDefinition region) {
        var rows = new List<PokeNavDetailLinkRow>();
        if(region == null) {
            return rows;
        }

        rows.AddRange((region.ActivityZones ?? Array.Empty<ActivityZoneDefinition>()).Where(zone => zone != null).Select(zone => PokeNavDetailLinkRow.From("Activity Zone", zone.Id, zone.DisplayName, string.Empty)));
        rows.AddRange((region.Shops ?? Array.Empty<ShopCatalogDefinition>()).Where(shop => shop != null).Select(shop => PokeNavDetailLinkRow.From("Shop", shop.Id, shop.DisplayName, string.Empty)));
        rows.AddRange((region.TransitStops ?? Array.Empty<TransitStopDefinition>()).Where(stop => stop != null).Select(stop => PokeNavDetailLinkRow.From("Transit Stop", stop.Id, stop.DisplayName, string.Empty)));
        rows.AddRange((region.JobBoards ?? Array.Empty<JobBoardDefinition>()).Where(board => board != null).Select(board => PokeNavDetailLinkRow.From("Job Board", board.Id, board.DisplayName, string.Empty)));
        rows.AddRange((region.EncounterTables ?? Array.Empty<EncounterTableDefinition>()).Where(table => table != null).Select(table => PokeNavDetailLinkRow.From("Encounter Table", table.Id, table.DisplayName, string.Empty)));
        return Limit(rows).ToList();
    }

    List<PokeNavDetailLinkRow> BuildEntryLinks(PokeNavEntryDefinition entry) {
        var rows = new List<PokeNavDetailLinkRow>();
        if(entry == null) {
            return rows;
        }

        if(entry.RelatedRegion != null) rows.Add(PokeNavDetailLinkRow.From("Region", entry.RelatedRegion.Id, entry.RelatedRegion.DisplayName, string.Empty));
        if(entry.RelatedPokemon != null) rows.Add(PokeNavDetailLinkRow.From("Pokemon", entry.RelatedPokemon.name, entry.RelatedPokemon.Name, string.Empty));
        if(entry.RelatedActivity != null) rows.Add(PokeNavDetailLinkRow.From("Activity", entry.RelatedActivity.Id, entry.RelatedActivity.DisplayName, string.Empty));
        if(entry.RelatedShop != null) rows.Add(PokeNavDetailLinkRow.From("Shop", entry.RelatedShop.Id, entry.RelatedShop.DisplayName, string.Empty));
        if(entry.RelatedTransitRoute != null) rows.Add(PokeNavDetailLinkRow.From("Transit Route", entry.RelatedTransitRoute.Id, entry.RelatedTransitRoute.DisplayName, string.Empty));
        return Limit(rows).ToList();
    }

    IEnumerable<T> Limit<T>(IEnumerable<T> source) {
        if(source == null) {
            return Enumerable.Empty<T>();
        }

        return MaxRowsPerList > 0 ? source.Take(MaxRowsPerList) : source;
    }

    T FindResourceById<T>(string id, Func<T, string> getId) where T : UnityEngine.Object {
        if(string.IsNullOrWhiteSpace(id) || getId == null) {
            return null;
        }

        return Resources.LoadAll<T>("").FirstOrDefault(asset => asset != null && string.Equals(getId(asset), id, StringComparison.OrdinalIgnoreCase));
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

    PlayerPokeNavLog GetOrCreateLog(PlayerController player) {
        if(player == null) {
            return null;
        }

        var log = player.GetComponent<PlayerPokeNavLog>();
        return log != null || !createMissingLogForActions ? log : player.gameObject.AddComponent<PlayerPokeNavLog>();
    }

    string ResolveSourceId() {
        return string.IsNullOrWhiteSpace(uiSourceId) ? "ui:pokenav-detail" : uiSourceId;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    void RefreshIfNeeded() {
        if(refreshAfterActions) {
            Refresh();
        }
    }

    bool Succeed(PokeNavKnowledgeDetailActionKind kind, string message, out string feedback) {
        feedback = message;
        lastResult = BuildResult(kind, true, message);
        if(logSuccessfulActions) {
            GameDebug.Step(message, GameDebugCategory.UI, this, "PokeNavDetailUI");
        }

        OnActionResult?.Invoke(lastResult);
        return true;
    }

    bool Block(string message, out string feedback) {
        feedback = message;
        lastResult = BuildResult(PokeNavKnowledgeDetailActionKind.Blocked, false, message);
        if(logBlockedActions) {
            GameDebug.Warning(message, GameDebugCategory.UI, this, "PokeNavDetailUI");
        }

        OnActionResult?.Invoke(lastResult);
        RefreshIfNeeded();
        return false;
    }

    PokeNavKnowledgeDetailActionResult BuildResult(PokeNavKnowledgeDetailActionKind kind, bool success, string message) {
        return new PokeNavKnowledgeDetailActionResult {
            kind = kind,
            success = success,
            message = message,
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0,
            absoluteHour = GetCurrentAbsoluteHour()
        };
    }
}

[Serializable]
public class PokeNavKnowledgeDetailSnapshot {
    [Tooltip("If enabled, a player was found for this snapshot.")]
    public bool hasPlayer;
    [Tooltip("Player GameObject name used by this snapshot.")]
    public string playerName;
    [Tooltip("Selected detail type.")]
    public PokeNavKnowledgeDetailType selectedType;
    [Tooltip("Selected detail id.")]
    public string selectedId;
    [Tooltip("Resolved detail id.")]
    public string detailId;
    [Tooltip("Detail title.")]
    public string title;
    [Tooltip("Detail subtitle/category.")]
    public string subtitle;
    [Tooltip("Detail body text visible to UI.")]
    public string body;
    [Tooltip("If enabled, this detail is already visible/discovered.")]
    public bool visible;
    [Tooltip("If enabled, this detail can be discovered now.")]
    public bool canDiscover;
    [Tooltip("Reason shown when discovery/detail access is blocked.")]
    public string blockedReason;
    [Tooltip("Related Pokemon asset id/name.")]
    public string pokemonId;
    [Tooltip("Related Pokemon display name.")]
    public string pokemonName;
    [Tooltip("Related Pokemon knowledge level.")]
    public PokemonKnowledgeLevel knowledgeLevel;
    [Tooltip("Related region id.")]
    public string regionId;
    [Tooltip("Related region display name.")]
    public string regionName;
    [Tooltip("Related scene name.")]
    public string sceneName;
    [Tooltip("Related Pokemon name for generic knowledge entries.")]
    public string relatedPokemonName;
    [Tooltip("Free-form tags copied from the selected content.")]
    public List<string> tags = new List<string>();
    [Tooltip("Habitat rows for Pokedex details.")]
    public List<PokeNavHabitatDetailRow> habitatRows = new List<PokeNavHabitatDetailRow>();
    [Tooltip("Care hint rows for Pokedex details.")]
    public List<PokeNavCareHintDetailRow> careHintRows = new List<PokeNavCareHintDetailRow>();
    [Tooltip("Related Pokemon rows for region details.")]
    public List<PokeNavRelatedPokemonDetailRow> pokemonRows = new List<PokeNavRelatedPokemonDetailRow>();
    [Tooltip("Generic linked content rows.")]
    public List<PokeNavDetailLinkRow> linkRows = new List<PokeNavDetailLinkRow>();
    [Tooltip("Short summary text useful for placeholder UI.")]
    public string summaryText;
    [Tooltip("In-game day when this snapshot was built.")]
    public int day;
    [Tooltip("In-game hour when this snapshot was built.")]
    public int hour;
    [Tooltip("Absolute in-game hour when this snapshot was built.")]
    public int absoluteHour;
    [Tooltip("Most recent backend action result.")]
    public PokeNavKnowledgeDetailActionResult lastResult;
}

[Serializable]
public class PokeNavKnowledgeDetailActionResult {
    [Tooltip("Kind of detail backend action that produced this result.")]
    public PokeNavKnowledgeDetailActionKind kind;
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
public class PokeNavHabitatDetailRow {
    [Tooltip("Related region id.")]
    public string regionId;
    [Tooltip("Related region display name.")]
    public string regionName;
    [Tooltip("Related encounter table id.")]
    public string encounterTableId;
    [Tooltip("Related encounter table display name.")]
    public string encounterTableName;
    [Tooltip("Encounter source type.")]
    public EncounterSourceType sourceType;
    [Tooltip("Minimum knowledge level required to show this habitat.")]
    public PokemonKnowledgeLevel minimumKnowledgeToReveal;
    [Tooltip("Habitat note.")]
    public string note;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;

    public static PokeNavHabitatDetailRow FromHabitat(PokedexHabitatInfo habitat) {
        return new PokeNavHabitatDetailRow {
            regionId = habitat.region != null ? habitat.region.Id : string.Empty,
            regionName = habitat.region != null ? habitat.region.DisplayName : string.Empty,
            encounterTableId = habitat.encounterTable != null ? habitat.encounterTable.Id : string.Empty,
            encounterTableName = habitat.encounterTable != null ? habitat.encounterTable.DisplayName : string.Empty,
            sourceType = habitat.sourceType,
            minimumKnowledgeToReveal = habitat.minimumKnowledgeToReveal,
            note = habitat.note,
            displayText = $"{(habitat.region != null ? habitat.region.DisplayName : "Unknown")} - {habitat.sourceType}"
        };
    }
}

[Serializable]
public class PokeNavCareHintDetailRow {
    [Tooltip("Minimum knowledge level required to show this care hint.")]
    public PokemonKnowledgeLevel minimumKnowledgeToReveal;
    [Tooltip("Related care action id.")]
    public string careActionId;
    [Tooltip("Related care action display name.")]
    public string careActionName;
    [Tooltip("Care hint note.")]
    public string note;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;

    public static PokeNavCareHintDetailRow FromHint(PokedexCareHint hint) {
        return new PokeNavCareHintDetailRow {
            minimumKnowledgeToReveal = hint.minimumKnowledgeToReveal,
            careActionId = hint.careAction != null ? hint.careAction.Id : string.Empty,
            careActionName = hint.careAction != null ? hint.careAction.DisplayName : string.Empty,
            note = hint.note,
            displayText = !string.IsNullOrWhiteSpace(hint.note) ? hint.note : hint.careAction != null ? hint.careAction.DisplayName : "Care hint"
        };
    }
}

[Serializable]
public class PokeNavRelatedPokemonDetailRow {
    [Tooltip("Pokemon asset id/name.")]
    public string pokemonId;
    [Tooltip("Pokemon display name.")]
    public string pokemonName;
    [Tooltip("Pokemon primary type.")]
    public PokemonType type1;
    [Tooltip("Pokemon secondary type.")]
    public PokemonType type2;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;

    public static PokeNavRelatedPokemonDetailRow FromPokemon(PokemonBase pokemon) {
        return new PokeNavRelatedPokemonDetailRow {
            pokemonId = pokemon != null ? pokemon.name : string.Empty,
            pokemonName = pokemon != null ? pokemon.Name : string.Empty,
            type1 = pokemon != null ? pokemon.Type1 : PokemonType.None,
            type2 = pokemon != null ? pokemon.Type2 : PokemonType.None,
            displayText = pokemon != null ? $"{pokemon.Name} [{pokemon.Type1}/{pokemon.Type2}]" : "Unknown Pokemon"
        };
    }
}

[Serializable]
public class PokeNavDetailLinkRow {
    [Tooltip("Linked content type.")]
    public string linkType;
    [Tooltip("Linked content id.")]
    public string linkId;
    [Tooltip("Linked content display name.")]
    public string displayName;
    [Tooltip("Optional note.")]
    public string note;
    [Tooltip("Short text useful for placeholder UI.")]
    public string displayText;

    public static PokeNavDetailLinkRow From(string linkType, string linkId, string displayName, string note) {
        return new PokeNavDetailLinkRow {
            linkType = linkType,
            linkId = linkId,
            displayName = displayName,
            note = note,
            displayText = $"{linkType}: {displayName}"
        };
    }
}
