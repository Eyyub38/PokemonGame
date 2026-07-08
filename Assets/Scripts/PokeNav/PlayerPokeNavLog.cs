using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerPokeNavLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save Pokemon knowledge levels manually discovered by PokeNav systems.")]
    [SerializeField] List<PokeNavPokemonKnowledgeState> pokemonKnowledge = new List<PokeNavPokemonKnowledgeState>();
    [Tooltip("Runtime/save ids for discovered generic PokeNav knowledge entries.")]
    [SerializeField] List<string> discoveredEntryIds = new List<string>();
    [Tooltip("Runtime/save ids for discovered region cards.")]
    [SerializeField] List<string> discoveredRegionIds = new List<string>();
    [Tooltip("Runtime/save ids for unlocked social posts.")]
    [SerializeField] List<string> unlockedPostIds = new List<string>();
    [Tooltip("Runtime/save ids for social posts marked as read.")]
    [SerializeField] List<string> readPostIds = new List<string>();

    public IReadOnlyList<PokeNavPokemonKnowledgeState> PokemonKnowledge => pokemonKnowledge;
    public IReadOnlyList<string> DiscoveredEntryIds => discoveredEntryIds;
    public IReadOnlyList<string> DiscoveredRegionIds => discoveredRegionIds;
    public IReadOnlyList<string> UnlockedPostIds => unlockedPostIds;
    public IReadOnlyList<string> ReadPostIds => readPostIds;
    public event Action<PokemonBase, PokemonKnowledgeLevel> OnPokemonKnowledgeChanged;
    public event Action<PokeNavEntryDefinition> OnEntryDiscovered;
    public event Action<RegionInfoDefinition> OnRegionDiscovered;
    public event Action<SocialPostDefinition> OnPostUnlocked;
    public event Action<SocialPostDefinition> OnPostRead;

    public PokemonKnowledgeLevel GetPokemonKnowledgeLevel(PokemonBase pokemon) {
        if(pokemon == null) {
            return PokemonKnowledgeLevel.Unknown;
        }

        var manual = GetState(pokemon.name)?.knowledgeLevel ?? PokemonKnowledgeLevel.Unknown;
        var encounter = GetEncounterKnowledge(pokemon);
        var research = GetResearchKnowledge(pokemon);
        return Max(manual, encounter, research);
    }

    public bool RecordPokemonKnowledge(PokemonBase pokemon, PokemonKnowledgeLevel level, string source = null) {
        if(pokemon == null || level <= PokemonKnowledgeLevel.Unknown) {
            return false;
        }

        var state = GetOrCreateState(pokemon);
        if(level <= state.knowledgeLevel) {
            state.timesUpdated++;
            return false;
        }

        state.knowledgeLevel = level;
        state.timesUpdated++;
        state.lastSource = source;
        state.lastUpdatedDay = GetCurrentDay();
        state.lastUpdatedAbsoluteHour = GetCurrentAbsoluteHour();
        OnPokemonKnowledgeChanged?.Invoke(pokemon, level);
        PublishPokeNavEvent("pokemon-knowledge", pokemon.name, pokemon.Name, $"{pokemon.Name} knowledge updated to {level}.", GameEventImportance.Info);
        return true;
    }

    public void RecordPokemonEncounter(PokemonBase pokemon, EncounterSourceType sourceType, EncounterTableDefinition table, PokemonKnowledgeLevel level) {
        if(pokemon == null) {
            return;
        }

        string source = table != null ? $"{sourceType}:{table.Id}" : sourceType.ToString();
        RecordPokemonKnowledge(pokemon, level, source);
    }

    public bool HasDiscoveredEntry(PokeNavEntryDefinition entry) {
        return entry != null && (entry.VisibleByDefault || discoveredEntryIds.Contains(entry.Id));
    }

    public bool DiscoverEntry(PokeNavEntryDefinition entry, out string failureMessage) {
        if(entry == null) {
            failureMessage = "No PokeNav entry selected.";
            return false;
        }

        var player = GetComponent<PlayerController>();
        if(!entry.CanDiscover(player, out failureMessage)) {
            return false;
        }

        if(!discoveredEntryIds.Contains(entry.Id)) {
            discoveredEntryIds.Add(entry.Id);
            OnEntryDiscovered?.Invoke(entry);
            PublishPokeNavEvent("entry-discovered", entry.Id, entry.DisplayName, $"{entry.DisplayName} added to PokeNav.", GameEventImportance.Success);
        }

        failureMessage = null;
        return true;
    }

    public bool HasDiscoveredRegion(RegionInfoDefinition region) {
        return region != null && (region.VisibleByDefault || discoveredRegionIds.Contains(region.Id));
    }

    public bool DiscoverRegion(RegionInfoDefinition region, out string failureMessage) {
        if(region == null) {
            failureMessage = "No region selected.";
            return false;
        }

        var player = GetComponent<PlayerController>();
        if(!region.CanDiscover(player, out failureMessage)) {
            return false;
        }

        if(!discoveredRegionIds.Contains(region.Id)) {
            discoveredRegionIds.Add(region.Id);
            OnRegionDiscovered?.Invoke(region);
            PublishPokeNavEvent("region-discovered", region.Id, region.DisplayName, $"{region.DisplayName} discovered.", GameEventImportance.Success);
        }

        failureMessage = null;
        return true;
    }

    public bool HasUnlockedPost(SocialPostDefinition post) {
        return post != null && (post.VisibleByDefault || unlockedPostIds.Contains(post.Id));
    }

    public bool UnlockPost(SocialPostDefinition post, bool publish = true) {
        if(post == null || unlockedPostIds.Contains(post.Id)) {
            return false;
        }

        unlockedPostIds.Add(post.Id);
        OnPostUnlocked?.Invoke(post);
        if(publish) {
            post.Publish(GetComponent<PlayerController>(), "unlocked");
        }
        return true;
    }

    public bool MarkPostRead(SocialPostDefinition post, bool read = true) {
        if(post == null) {
            return false;
        }

        bool changed;
        if(read) {
            changed = !readPostIds.Contains(post.Id);
            if(changed) {
                readPostIds.Add(post.Id);
                OnPostRead?.Invoke(post);
            }
        } else {
            changed = readPostIds.Remove(post.Id);
        }

        return changed;
    }

    public bool IsPostRead(SocialPostDefinition post) {
        return post != null && readPostIds.Contains(post.Id);
    }

    public List<SocialPostDefinition> GetAvailablePosts(IEnumerable<SocialPostDefinition> posts, bool includeRead = true) {
        var player = GetComponent<PlayerController>();
        return (posts ?? Enumerable.Empty<SocialPostDefinition>())
            .Where(post => post != null && post.CanShow(player, this, out _))
            .Where(post => includeRead || !IsPostRead(post))
            .OrderByDescending(post => post.Pinned)
            .ThenByDescending(post => post.Priority)
            .ThenBy(post => post.Title)
            .ToList();
    }

    public List<PokedexEntryDefinition> GetKnownPokedexEntries(PokemonKnowledgeLevel minimumLevel = PokemonKnowledgeLevel.Seen) {
        return Resources.LoadAll<PokedexEntryDefinition>("")
            .Where(entry => entry != null && entry.Pokemon != null)
            .Where(entry => GetPokemonKnowledgeLevel(entry.Pokemon) >= minimumLevel)
            .OrderBy(entry => entry.DisplayName)
            .ToList();
    }

    PokeNavPokemonKnowledgeState GetState(string pokemonId) {
        if(string.IsNullOrWhiteSpace(pokemonId)) {
            return null;
        }

        return pokemonKnowledge.FirstOrDefault(state => state != null && state.pokemonId == pokemonId);
    }

    PokeNavPokemonKnowledgeState GetOrCreateState(PokemonBase pokemon) {
        var state = GetState(pokemon.name);
        if(state != null) {
            return state;
        }

        state = new PokeNavPokemonKnowledgeState {
            pokemonId = pokemon.name,
            pokemonName = pokemon.Name,
            knowledgeLevel = PokemonKnowledgeLevel.Unknown
        };
        pokemonKnowledge.Add(state);
        return state;
    }

    PokemonKnowledgeLevel GetEncounterKnowledge(PokemonBase pokemon) {
        var encounterLog = GetComponent<PlayerEncounterLog>();
        if(encounterLog == null || pokemon == null) {
            return PokemonKnowledgeLevel.Unknown;
        }

        if(encounterLog.GetCount(pokemon, EncounterSourceType.Any, EncounterLogCountType.Captured) > 0
            || encounterLog.GetCount(pokemon, EncounterSourceType.Any, EncounterLogCountType.StealthCaptured) > 0) {
            return PokemonKnowledgeLevel.Caught;
        }

        if(encounterLog.GetCount(pokemon, EncounterSourceType.Any, EncounterLogCountType.BattleStarted) > 0) {
            return PokemonKnowledgeLevel.Battled;
        }

        if(encounterLog.GetCount(pokemon, EncounterSourceType.Any, EncounterLogCountType.Seen) > 0) {
            return PokemonKnowledgeLevel.Seen;
        }

        return PokemonKnowledgeLevel.Unknown;
    }

    PokemonKnowledgeLevel GetResearchKnowledge(PokemonBase pokemon) {
        var researchLog = GetComponent<PlayerResearchLog>();
        if(researchLog == null || pokemon == null) {
            return PokemonKnowledgeLevel.Unknown;
        }

        foreach(var subject in Resources.LoadAll<ResearchSubjectDefinition>("")) {
            if(subject != null && subject.RelatedPokemon == pokemon && researchLog.IsCompleted(subject)) {
                return PokemonKnowledgeLevel.Researched;
            }
        }

        return PokemonKnowledgeLevel.Unknown;
    }

    PokemonKnowledgeLevel Max(params PokemonKnowledgeLevel[] levels) {
        PokemonKnowledgeLevel result = PokemonKnowledgeLevel.Unknown;
        foreach(var level in levels) {
            if(level > result) {
                result = level;
            }
        }

        return result;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    void PublishPokeNavEvent(string phase, string targetId, string targetName, string message, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            null,
            $"pokenav.{phase}.{targetId}",
            message,
            GameEventCategory.PokeNav,
            importance,
            this,
            "PlayerPokeNavLog",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("targetId", targetId),
            GameEventPublishing.Value("targetName", targetName));
    }

    public object CaptureState() {
        return new PlayerPokeNavLogSaveData {
            pokemonKnowledge = pokemonKnowledge.Where(state => state != null).Select(state => state.ToSaveData()).ToList(),
            discoveredEntryIds = discoveredEntryIds.Distinct().ToList(),
            discoveredRegionIds = discoveredRegionIds.Distinct().ToList(),
            unlockedPostIds = unlockedPostIds.Distinct().ToList(),
            readPostIds = readPostIds.Distinct().ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerPokeNavLogSaveData;
        pokemonKnowledge = saveData?.pokemonKnowledge?.Where(s => s != null).Select(s => new PokeNavPokemonKnowledgeState(s)).ToList() ?? new List<PokeNavPokemonKnowledgeState>();
        discoveredEntryIds = saveData?.discoveredEntryIds?.Distinct().ToList() ?? new List<string>();
        discoveredRegionIds = saveData?.discoveredRegionIds?.Distinct().ToList() ?? new List<string>();
        unlockedPostIds = saveData?.unlockedPostIds?.Distinct().ToList() ?? new List<string>();
        readPostIds = saveData?.readPostIds?.Distinct().ToList() ?? new List<string>();
    }
}

[Serializable]
public class PokeNavPokemonKnowledgeState {
    [Tooltip("Saved Pokemon asset id/name.")]
    public string pokemonId;
    [Tooltip("Saved Pokemon display name for fallback/debug output.")]
    public string pokemonName;
    [Tooltip("Best manually recorded PokeNav knowledge level.")]
    public PokemonKnowledgeLevel knowledgeLevel;
    [Tooltip("Last source that updated this knowledge state.")]
    public string lastSource;
    [Tooltip("In-game day when this state was last updated.")]
    public int lastUpdatedDay = -1;
    [Tooltip("Absolute in-game hour when this state was last updated.")]
    public int lastUpdatedAbsoluteHour = -1;
    [Tooltip("How many update attempts touched this state.")]
    [Min(0)]
    public int timesUpdated;

    public PokeNavPokemonKnowledgeState() {
    }

    public PokeNavPokemonKnowledgeState(PokeNavPokemonKnowledgeSaveData saveData) {
        if(saveData == null) {
            return;
        }

        pokemonId = saveData.pokemonId;
        pokemonName = saveData.pokemonName;
        knowledgeLevel = saveData.knowledgeLevel;
        lastSource = saveData.lastSource;
        lastUpdatedDay = saveData.lastUpdatedDay;
        lastUpdatedAbsoluteHour = saveData.lastUpdatedAbsoluteHour;
        timesUpdated = Mathf.Max(0, saveData.timesUpdated);
    }

    public PokeNavPokemonKnowledgeSaveData ToSaveData() {
        return new PokeNavPokemonKnowledgeSaveData {
            pokemonId = pokemonId,
            pokemonName = pokemonName,
            knowledgeLevel = knowledgeLevel,
            lastSource = lastSource,
            lastUpdatedDay = lastUpdatedDay,
            lastUpdatedAbsoluteHour = lastUpdatedAbsoluteHour,
            timesUpdated = timesUpdated
        };
    }
}

[Serializable]
public class PlayerPokeNavLogSaveData {
    public List<PokeNavPokemonKnowledgeSaveData> pokemonKnowledge;
    public List<string> discoveredEntryIds;
    public List<string> discoveredRegionIds;
    public List<string> unlockedPostIds;
    public List<string> readPostIds;
}

[Serializable]
public class PokeNavPokemonKnowledgeSaveData {
    public string pokemonId;
    public string pokemonName;
    public PokemonKnowledgeLevel knowledgeLevel;
    public string lastSource;
    public int lastUpdatedDay;
    public int lastUpdatedAbsoluteHour;
    public int timesUpdated;
}
