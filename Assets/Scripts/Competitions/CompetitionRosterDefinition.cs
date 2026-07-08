using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CompetitionRosterSelectionMode {
    FixedOrder,
    ShuffleAll,
    WeightedRandom
}

public enum CompetitionBracketFormat {
    SingleElimination,
    RoundRobin,
    Gauntlet,
    FreeRun
}

[CreateAssetMenu(menuName = "Competitions/Roster Definition")]
public class CompetitionRosterDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this roster. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future bracket or registration UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation for this roster.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as kanto, frontier, championship, elite-four, qualifier or seasonal.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Owner")]
    [Tooltip("Competition this roster belongs to.")]
    [SerializeField] CompetitionDefinition competition;
    [Tooltip("Optional season that should be active/unlocked before this roster is generated.")]
    [SerializeField] CompetitionSeasonDefinition season;
    [Tooltip("Optional ranking track represented by this roster.")]
    [SerializeField] CompetitionRankingDefinition ranking;
    [Tooltip("Default battle rule set used when an entrant does not provide its own.")]
    [SerializeField] BattleRuleSetDefinition defaultRuleSet;

    [Header("Generation")]
    [Tooltip("How entrants are selected from the candidate list.")]
    [SerializeField] CompetitionRosterSelectionMode selectionMode = CompetitionRosterSelectionMode.FixedOrder;
    [Tooltip("How generated matches are grouped into rounds.")]
    [SerializeField] CompetitionBracketFormat bracketFormat = CompetitionBracketFormat.Gauntlet;
    [Tooltip("If enabled, a player entrant is inserted into the generated bracket.")]
    [SerializeField] bool includePlayer = true;
    [Tooltip("Display name used for the player entrant in saved bracket records.")]
    [SerializeField] string playerDisplayName = "Player";
    [Tooltip("Minimum opponent count selected when enough candidates are available.")]
    [Min(0)]
    [SerializeField] int minOpponentCount = 1;
    [Tooltip("Maximum opponent count selected from the candidate list. 0 means all valid candidates.")]
    [Min(0)]
    [SerializeField] int maxOpponentCount;
    [Tooltip("If enabled, the same entrant definition can be selected more than once by weighted random selection.")]
    [SerializeField] bool allowDuplicateEntrants;
    [Tooltip("Salt added to deterministic bracket seed generation.")]
    [SerializeField] int seedSalt;
    [Tooltip("Candidate entrants available to this roster.")]
    [SerializeField] List<CompetitionEntrantDefinition> entrants = new List<CompetitionEntrantDefinition>();

    [Header("Access")]
    [Tooltip("If enabled, Competition.CanEnter is checked before bracket generation.")]
    [SerializeField] bool requireCompetitionAccess = true;
    [Tooltip("If enabled, Season must be active according to PlayerCompetitionSeasonLog and CalendarEvent.")]
    [SerializeField] bool requireActiveSeason;
    [Tooltip("How additional generation requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Additional activity-style requirements checked before bracket generation.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message shown when generation is blocked and no more specific reason exists.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This roster is not available yet.";

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public CompetitionDefinition Competition => competition;
    public CompetitionSeasonDefinition Season => season;
    public CompetitionRankingDefinition Ranking => ranking;
    public BattleRuleSetDefinition DefaultRuleSet => defaultRuleSet;
    public CompetitionRosterSelectionMode SelectionMode => selectionMode;
    public CompetitionBracketFormat BracketFormat => bracketFormat;
    public bool IncludePlayer => includePlayer;
    public int MinOpponentCount => Mathf.Max(0, minOpponentCount);
    public int MaxOpponentCount => Mathf.Max(0, maxOpponentCount);
    public bool AllowDuplicateEntrants => allowDuplicateEntrants;
    public IReadOnlyList<CompetitionEntrantDefinition> Entrants => entrants != null ? (IReadOnlyList<CompetitionEntrantDefinition>)entrants : Array.Empty<CompetitionEntrantDefinition>();
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();

    public bool CanGenerate(PlayerController player, out string failureMessage) {
        if(requireCompetitionAccess && competition != null && !competition.CanEnter(player, out failureMessage)) {
            return false;
        }

        if(requireActiveSeason && season != null && !(player?.GetComponent<PlayerCompetitionSeasonLog>()?.IsActive(season) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{season.DisplayName} is not active." : lockedMessage;
            return false;
        }

        if(!RequirementsMet(player, out failureMessage)) {
            return false;
        }

        int validCount = GetAvailableEntrants(player).Count;
        if(validCount < MinOpponentCount) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"Not enough entrants are available for {DisplayName}." : lockedMessage;
            return false;
        }

        failureMessage = null;
        return true;
    }

    public List<CompetitionEntrantDefinition> GetAvailableEntrants(PlayerController player) {
        return Entrants
            .Where(entrant => entrant != null && entrant.CanSelect(player, out _))
            .ToList();
    }

    public PlayerCompetitionBracketState GenerateBracket(PlayerController player, int seed, string sourceId = null) {
        if(seed == 0) {
            seed = CreateSeed(player, sourceId);
        }

        var selectedEntrants = SelectEntrants(player, seed);
        var records = new List<PlayerCompetitionBracketEntrantRecord>();
        int slotIndex = 0;

        if(includePlayer) {
            records.Add(CreatePlayerRecord(slotIndex++));
        }

        foreach(var entrant in selectedEntrants) {
            records.Add(entrant.CreateRecord(seed, slotIndex++));
        }

        var state = new PlayerCompetitionBracketState {
            rosterId = Id,
            rosterName = DisplayName,
            competitionId = competition != null ? competition.Id : string.Empty,
            competitionName = competition != null ? competition.DisplayName : string.Empty,
            seasonId = season != null ? season.Id : string.Empty,
            seasonName = season != null ? season.DisplayName : string.Empty,
            rankingId = ranking != null ? ranking.Id : string.Empty,
            rankingName = ranking != null ? ranking.DisplayName : string.Empty,
            defaultRuleSetId = defaultRuleSet != null ? defaultRuleSet.Id : string.Empty,
            defaultRuleSetName = defaultRuleSet != null ? defaultRuleSet.DisplayName : string.Empty,
            bracketFormat = bracketFormat,
            seed = seed,
            generatedTotalHour = GetCurrentTotalHour(),
            sourceId = sourceId,
            active = true,
            entrants = records,
            rounds = BuildRounds(records)
        };

        return state;
    }

    List<CompetitionEntrantDefinition> SelectEntrants(PlayerController player, int seed) {
        var candidates = GetAvailableEntrants(player);
        int targetCount = MaxOpponentCount > 0 ? Mathf.Min(MaxOpponentCount, candidates.Count) : candidates.Count;
        targetCount = Mathf.Max(Mathf.Min(MinOpponentCount, candidates.Count), targetCount);

        if(targetCount <= 0) {
            return new List<CompetitionEntrantDefinition>();
        }

        var random = new System.Random(seed + seedSalt);
        if(selectionMode == CompetitionRosterSelectionMode.FixedOrder) {
            return candidates
                .OrderBy(entrant => entrant.SeededRank)
                .ThenBy(entrant => entrant.DisplayName)
                .Take(targetCount)
                .ToList();
        }

        if(selectionMode == CompetitionRosterSelectionMode.ShuffleAll) {
            return candidates
                .OrderBy(_ => random.Next())
                .Take(targetCount)
                .ToList();
        }

        return SelectWeighted(candidates, targetCount, random);
    }

    List<CompetitionEntrantDefinition> SelectWeighted(List<CompetitionEntrantDefinition> candidates, int targetCount, System.Random random) {
        var selected = new List<CompetitionEntrantDefinition>();
        var pool = candidates.Where(entrant => entrant.SelectionWeight > 0).ToList();
        while(pool.Count > 0 && selected.Count < targetCount) {
            int totalWeight = pool.Sum(entrant => entrant.SelectionWeight);
            int roll = random.Next(0, totalWeight);
            int cursor = 0;
            CompetitionEntrantDefinition picked = null;
            foreach(var entrant in pool) {
                cursor += entrant.SelectionWeight;
                if(roll < cursor) {
                    picked = entrant;
                    break;
                }
            }

            if(picked == null) {
                break;
            }

            selected.Add(picked);
            if(!allowDuplicateEntrants || picked.Unique) {
                pool.Remove(picked);
            }
        }

        return selected;
    }

    List<PlayerCompetitionBracketRoundRecord> BuildRounds(List<PlayerCompetitionBracketEntrantRecord> records) {
        return bracketFormat switch {
            CompetitionBracketFormat.SingleElimination => BuildSingleEliminationOpeningRound(records),
            CompetitionBracketFormat.RoundRobin => BuildRoundRobin(records),
            CompetitionBracketFormat.FreeRun => BuildFreeRun(records),
            _ => BuildGauntlet(records)
        };
    }

    List<PlayerCompetitionBracketRoundRecord> BuildGauntlet(List<PlayerCompetitionBracketEntrantRecord> records) {
        var opponents = records.Where(record => record != null && !record.isPlayer).ToList();
        var rounds = new List<PlayerCompetitionBracketRoundRecord>();
        for(int i = 0; i < opponents.Count; i++) {
            rounds.Add(new PlayerCompetitionBracketRoundRecord {
                roundIndex = i,
                roundName = $"Round {i + 1}",
                matches = new List<PlayerCompetitionBracketMatchRecord> {
                    CreateMatch(i, 0, GetPlayerRecord(records), opponents[i])
                }
            });
        }

        return rounds;
    }

    List<PlayerCompetitionBracketRoundRecord> BuildFreeRun(List<PlayerCompetitionBracketEntrantRecord> records) {
        return new List<PlayerCompetitionBracketRoundRecord> {
            new PlayerCompetitionBracketRoundRecord {
                roundIndex = 0,
                roundName = "Open Matches",
                matches = records
                    .Where(record => record != null && !record.isPlayer)
                    .Select((opponent, index) => CreateMatch(0, index, GetPlayerRecord(records), opponent))
                    .ToList()
            }
        };
    }

    List<PlayerCompetitionBracketRoundRecord> BuildRoundRobin(List<PlayerCompetitionBracketEntrantRecord> records) {
        var matches = new List<PlayerCompetitionBracketMatchRecord>();
        int matchIndex = 0;
        for(int i = 0; i < records.Count; i++) {
            for(int j = i + 1; j < records.Count; j++) {
                matches.Add(CreateMatch(0, matchIndex++, records[i], records[j]));
            }
        }

        return new List<PlayerCompetitionBracketRoundRecord> {
            new PlayerCompetitionBracketRoundRecord {
                roundIndex = 0,
                roundName = "Round Robin",
                matches = matches
            }
        };
    }

    List<PlayerCompetitionBracketRoundRecord> BuildSingleEliminationOpeningRound(List<PlayerCompetitionBracketEntrantRecord> records) {
        var ordered = records.OrderBy(record => record.slotIndex).ToList();
        var matches = new List<PlayerCompetitionBracketMatchRecord>();
        int matchIndex = 0;
        for(int i = 0; i < ordered.Count; i += 2) {
            var first = ordered[i];
            var second = i + 1 < ordered.Count ? ordered[i + 1] : null;
            matches.Add(CreateMatch(0, matchIndex++, first, second));
        }

        return new List<PlayerCompetitionBracketRoundRecord> {
            new PlayerCompetitionBracketRoundRecord {
                roundIndex = 0,
                roundName = "Opening Round",
                matches = matches
            }
        };
    }

    PlayerCompetitionBracketEntrantRecord CreatePlayerRecord(int slotIndex) {
        return new PlayerCompetitionBracketEntrantRecord {
            entrantId = "player",
            entrantName = string.IsNullOrWhiteSpace(playerDisplayName) ? "Player" : playerDisplayName,
            kind = CompetitionEntrantKind.PlayerProxy,
            slotIndex = Mathf.Max(0, slotIndex),
            isPlayer = true
        };
    }

    PlayerCompetitionBracketEntrantRecord GetPlayerRecord(List<PlayerCompetitionBracketEntrantRecord> records) {
        return records.FirstOrDefault(record => record != null && record.isPlayer);
    }

    PlayerCompetitionBracketMatchRecord CreateMatch(int roundIndex, int matchIndex, PlayerCompetitionBracketEntrantRecord first, PlayerCompetitionBracketEntrantRecord second) {
        return new PlayerCompetitionBracketMatchRecord {
            matchId = $"{Id}.r{roundIndex}.m{matchIndex}",
            roundIndex = roundIndex,
            matchIndex = matchIndex,
            firstEntrantId = first != null ? first.entrantId : string.Empty,
            firstEntrantName = first != null ? first.entrantName : "Bye",
            secondEntrantId = second != null ? second.entrantId : string.Empty,
            secondEntrantName = second != null ? second.entrantName : "Bye",
            challengeId = ResolveChallengeId(first, second),
            ruleSetId = ResolveRuleSetId(first, second),
            completed = second == null,
            winnerEntrantId = second == null && first != null ? first.entrantId : string.Empty,
            winnerEntrantName = second == null && first != null ? first.entrantName : string.Empty,
            playerInMatch = (first != null && first.isPlayer) || (second != null && second.isPlayer),
            playerWon = second == null && first != null && first.isPlayer
        };
    }

    string ResolveChallengeId(PlayerCompetitionBracketEntrantRecord first, PlayerCompetitionBracketEntrantRecord second) {
        var opponent = first != null && first.isPlayer ? second : first;
        return opponent != null ? opponent.challengeId : string.Empty;
    }

    string ResolveRuleSetId(PlayerCompetitionBracketEntrantRecord first, PlayerCompetitionBracketEntrantRecord second) {
        var opponent = first != null && first.isPlayer ? second : first;
        if(opponent != null && !string.IsNullOrWhiteSpace(opponent.ruleSetId)) {
            return opponent.ruleSetId;
        }

        return defaultRuleSet != null ? defaultRuleSet.Id : string.Empty;
    }

    int CreateSeed(PlayerController player, string sourceId) {
        int dayHour = TimeSystem.i != null ? TimeSystem.i.Day * 24 + TimeSystem.i.Hour : 0;
        return Mathf.Abs(HashCode.Combine(Id, sourceId ?? string.Empty, player != null ? player.name : string.Empty, dayHour, seedSalt));
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    bool RequirementsMet(PlayerController player, out string failureMessage) {
        var activeRequirements = requirements?.Where(requirement => requirement != null).ToList() ?? new List<ActivityRequirement>();
        if(activeRequirements.Count == 0) {
            failureMessage = null;
            return true;
        }

        if(requirementMatchMode == ConsequenceRequirementMatchMode.Any) {
            foreach(var requirement in activeRequirements) {
                if(requirement.IsMet(player)) {
                    failureMessage = null;
                    return true;
                }
            }

            failureMessage = activeRequirements.FirstOrDefault()?.FailureMessage ?? lockedMessage;
            return false;
        }

        foreach(var requirement in activeRequirements) {
            if(!requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }
}
