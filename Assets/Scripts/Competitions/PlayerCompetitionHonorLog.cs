using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerCompetitionHonorLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save ids for unique honors already earned by this player.")]
    [SerializeField] List<string> earnedHonorIds = new List<string>();
    [Tooltip("Runtime/save history of earned honors and Hall of Fame style records.")]
    [SerializeField] List<PlayerCompetitionHonorRecord> honorHistory = new List<PlayerCompetitionHonorRecord>();

    public IReadOnlyList<string> EarnedHonorIds => earnedHonorIds;
    public IReadOnlyList<PlayerCompetitionHonorRecord> HonorHistory => honorHistory;
    public event Action<CompetitionHonorDefinition, PlayerCompetitionHonorRecord> OnHonorRecorded;
    public event Action OnCompetitionHonorLogChanged;

    public bool HasHonor(CompetitionHonorDefinition honor) {
        return honor != null && HasHonor(honor.Id);
    }

    public bool HasHonor(string honorId) {
        return !string.IsNullOrWhiteSpace(honorId) && earnedHonorIds.Contains(honorId);
    }

    public int GetHonorCount(CompetitionHonorKind kind) {
        return honorHistory.Count(record => record != null && record.kind == kind);
    }

    public int GetHonorCountWithTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return 0;
        }

        int count = 0;
        foreach(var record in honorHistory) {
            if(record == null) {
                continue;
            }

            var honor = ResolveHonor(record.honorId);
            if(honor != null && honor.HasTag(tag)) {
                count++;
            }
        }

        return count;
    }

    public bool RecordHonor(CompetitionHonorDefinition honor, string sourceId = null, CompetitionDefinition sourceCompetition = null, CompetitionRankingDefinition sourceRanking = null) {
        if(honor == null) {
            return false;
        }

        if(honor.Unique && HasHonor(honor)) {
            return false;
        }

        if(!earnedHonorIds.Contains(honor.Id)) {
            earnedHonorIds.Add(honor.Id);
        }

        var record = new PlayerCompetitionHonorRecord {
            honorId = honor.Id,
            honorName = honor.DisplayName,
            kind = honor.Kind,
            worldRegionId = honor.WorldRegion != null ? honor.WorldRegion.Id : string.Empty,
            worldRegionName = honor.WorldRegion != null ? honor.WorldRegion.DisplayName : string.Empty,
            sourceCompetitionId = sourceCompetition != null ? sourceCompetition.Id : string.Empty,
            sourceCompetitionName = sourceCompetition != null ? sourceCompetition.DisplayName : string.Empty,
            sourceRankingId = sourceRanking != null ? sourceRanking.Id : string.Empty,
            sourceRankingName = sourceRanking != null ? sourceRanking.DisplayName : string.Empty,
            sourceId = sourceId,
            earnedTotalHour = GetCurrentTotalHour(),
            partySnapshot = honor.CapturePartySnapshot ? CapturePartySnapshot() : new List<PlayerCompetitionHonorPokemonRecord>()
        };

        honorHistory.Add(record);
        OnHonorRecorded?.Invoke(honor, record);
        OnCompetitionHonorLogChanged?.Invoke();
        PublishLogEvent(honor, record);
        return true;
    }

    List<PlayerCompetitionHonorPokemonRecord> CapturePartySnapshot() {
        var party = GetComponent<PokemonParty>();
        if(party == null || party.Pokemons == null) {
            return new List<PlayerCompetitionHonorPokemonRecord>();
        }

        return party.Pokemons
            .Where(pokemon => pokemon != null)
            .Select((pokemon, index) => new PlayerCompetitionHonorPokemonRecord {
                slot = index,
                pokemonName = pokemon.Base != null ? pokemon.Base.Name : string.Empty,
                nickname = pokemon.NickName,
                level = pokemon.Level,
                isShiny = pokemon.IsShiny,
                pokeballName = pokemon.Pokeball != null ? pokemon.Pokeball.Name : string.Empty
            })
            .ToList();
    }

    CompetitionHonorDefinition ResolveHonor(string honorId) {
        if(string.IsNullOrWhiteSpace(honorId)) {
            return null;
        }

        return Resources.LoadAll<CompetitionHonorDefinition>("").FirstOrDefault(honor => honor != null && honor.Id == honorId);
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    void PublishLogEvent(CompetitionHonorDefinition honor, PlayerCompetitionHonorRecord record) {
        GameEventPublishing.PublishOptional(
            null,
            $"competition-honor-log.recorded.{honor.Id}",
            $"{honor.DisplayName} recorded.",
            GameEventCategory.BattleRule,
            GameEventImportance.Success,
            this,
            "PlayerCompetitionHonorLog",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: false,
            GameEventPublishing.Value("honorId", honor.Id),
            GameEventPublishing.Value("honorName", honor.DisplayName),
            GameEventPublishing.Value("kind", honor.Kind),
            GameEventPublishing.Value("sourceCompetition", record.sourceCompetitionId),
            GameEventPublishing.Value("sourceRanking", record.sourceRankingId),
            GameEventPublishing.Value("sourceId", record.sourceId));
    }

    public object CaptureState() {
        return new PlayerCompetitionHonorLogSaveData {
            earnedHonorIds = earnedHonorIds.Distinct().ToList(),
            honorHistory = honorHistory.Where(record => record != null).Select(record => record.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerCompetitionHonorLogSaveData;
        earnedHonorIds = saveData?.earnedHonorIds?.Distinct().ToList() ?? new List<string>();
        honorHistory = saveData?.honorHistory?.Where(record => record != null).Select(record => record.Clone()).ToList() ?? new List<PlayerCompetitionHonorRecord>();
        OnCompetitionHonorLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerCompetitionHonorRecord {
    [Tooltip("Saved honor id.")]
    public string honorId;
    [Tooltip("Saved honor display name for fallback/debug output.")]
    public string honorName;
    [Tooltip("Saved honor kind.")]
    public CompetitionHonorKind kind;
    [Tooltip("Saved world region id connected to this honor.")]
    public string worldRegionId;
    [Tooltip("Saved world region display name.")]
    public string worldRegionName;
    [Tooltip("Competition id that caused this honor.")]
    public string sourceCompetitionId;
    [Tooltip("Competition display name that caused this honor.")]
    public string sourceCompetitionName;
    [Tooltip("Ranking id that caused this honor.")]
    public string sourceRankingId;
    [Tooltip("Ranking display name that caused this honor.")]
    public string sourceRankingName;
    [Tooltip("Short source id that caused this honor.")]
    public string sourceId;
    [Tooltip("In-game total hour when this honor was earned.")]
    public int earnedTotalHour;
    [Tooltip("Party snapshot recorded when this honor was earned.")]
    public List<PlayerCompetitionHonorPokemonRecord> partySnapshot = new List<PlayerCompetitionHonorPokemonRecord>();

    public PlayerCompetitionHonorRecord Clone() {
        return new PlayerCompetitionHonorRecord {
            honorId = honorId,
            honorName = honorName,
            kind = kind,
            worldRegionId = worldRegionId,
            worldRegionName = worldRegionName,
            sourceCompetitionId = sourceCompetitionId,
            sourceCompetitionName = sourceCompetitionName,
            sourceRankingId = sourceRankingId,
            sourceRankingName = sourceRankingName,
            sourceId = sourceId,
            earnedTotalHour = earnedTotalHour,
            partySnapshot = partySnapshot?.Where(entry => entry != null).Select(entry => entry.Clone()).ToList() ?? new List<PlayerCompetitionHonorPokemonRecord>()
        };
    }
}

[Serializable]
public class PlayerCompetitionHonorPokemonRecord {
    [Tooltip("Party slot index at the time of recording.")]
    public int slot;
    [Tooltip("Pokemon species/base name.")]
    public string pokemonName;
    [Tooltip("Pokemon nickname shown at the time of recording.")]
    public string nickname;
    [Tooltip("Pokemon level shown at the time of recording.")]
    public int level;
    [Tooltip("Whether this Pokemon was shiny at the time of recording.")]
    public bool isShiny;
    [Tooltip("Pokeball name used by this Pokemon.")]
    public string pokeballName;

    public PlayerCompetitionHonorPokemonRecord Clone() {
        return new PlayerCompetitionHonorPokemonRecord {
            slot = slot,
            pokemonName = pokemonName,
            nickname = nickname,
            level = level,
            isShiny = isShiny,
            pokeballName = pokeballName
        };
    }
}

[Serializable]
public class PlayerCompetitionHonorLogSaveData {
    public List<string> earnedHonorIds = new List<string>();
    public List<PlayerCompetitionHonorRecord> honorHistory = new List<PlayerCompetitionHonorRecord>();
}
