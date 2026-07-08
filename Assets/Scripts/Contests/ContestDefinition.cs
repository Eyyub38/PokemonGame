using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ContestCategory {
    Beauty,
    Coolness,
    Cuteness,
    Smartness,
    Toughness,
    Care,
    Performance,
    Cooking,
    Fishing,
    Racing,
    BattleStyle,
    Custom
}

public enum ContestDifficulty {
    Beginner,
    Amateur,
    Skilled,
    Expert,
    Master,
    Champion,
    Custom
}

public enum ContestEntryMode {
    FirstHealthyPokemon,
    SelectedPokemon,
    WholeParty
}

public enum ContestScoreSource {
    Flat,
    PokemonLevel,
    PokemonFriendship,
    PokemonMood,
    PlayerLevel,
    PlayerSkillLevel,
    PlayerSkillTagLevel,
    PartyHighestLevel,
    ItemCount,
    Reputation,
    BattleChallengeWins,
    RandomRange
}

[CreateAssetMenu(menuName = "Contests/Contest Definition")]
public class ContestDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this contest. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in future contest UI. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer or player-facing explanation of this contest.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad contest category used by filters, scoring and future UI styling.")]
    [SerializeField] ContestCategory category = ContestCategory.Performance;
    [Tooltip("Difficulty tier used by filters, rewards and future UI.")]
    [SerializeField] ContestDifficulty difficulty = ContestDifficulty.Beginner;
    [Tooltip("How a Pokemon entry is chosen for this contest.")]
    [SerializeField] ContestEntryMode entryMode = ContestEntryMode.SelectedPokemon;
    [Tooltip("Free-form tags used by access checks, dialog and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Entry Rules")]
    [Tooltip("If enabled, the player can enter this contest without unlocking it first.")]
    [SerializeField] bool unlockedByDefault = true;
    [Tooltip("Minimum trainer level required. 0 disables this check.")]
    [Min(0)]
    [SerializeField] int minTrainerLevel;
    [Tooltip("Minimum Pokemon level required. 0 disables this check.")]
    [Min(0)]
    [SerializeField] int minPokemonLevel;
    [Tooltip("Maximum Pokemon level allowed. 0 disables this check.")]
    [Min(0)]
    [SerializeField] int maxPokemonLevel;
    [Tooltip("Allowed Pokemon types. Empty means all types are allowed unless banned below.")]
    [SerializeField] List<PokemonType> allowedTypes = new List<PokemonType>();
    [Tooltip("Pokemon types blocked from entering this contest.")]
    [SerializeField] List<PokemonType> bannedTypes = new List<PokemonType>();
    [Tooltip("Optional mood that must be at or above Required Mood Value.")]
    [SerializeField] PokemonMoodDefinition requiredMood;
    [Tooltip("Minimum value required for Required Mood.")]
    [SerializeField] int requiredMoodValue;
    [Tooltip("Items consumed when the contest starts.")]
    [SerializeField] List<ContestItemCost> entryCosts = new List<ContestItemCost>();
    [Tooltip("Message shown when entry fails and no more specific reason exists.")]
    [SerializeField] string lockedMessage = "This contest is not available yet.";

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this contest can be entered.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this contest can be entered.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this contest.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum reputation required with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional player skill required before this contest can be entered.")]
    [SerializeField] PlayerSkillDefinition requiredSkill;
    [Tooltip("Minimum level required for Required Skill.")]
    [Min(0)]
    [SerializeField] int requiredSkillLevel;
    [Tooltip("Optional calendar event that must be active for this contest.")]
    [SerializeField] CalendarEventDefinition requiredActiveCalendarEvent;

    [Header("Scoring")]
    [Tooltip("Base score added before criteria are evaluated.")]
    [SerializeField] int baseScore;
    [Tooltip("Score criteria evaluated when the contest is completed.")]
    [SerializeField] List<ContestScoreCriterion> scoreCriteria = new List<ContestScoreCriterion>();
    [Tooltip("Rank thresholds. Higher Min Score ranks should represent better results.")]
    [SerializeField] List<ContestRankDefinition> rankThresholds = new List<ContestRankDefinition>();

    [Header("Career Rewards")]
    [Tooltip("Career points awarded whenever this contest completes, regardless of rank.")]
    [SerializeField] List<CareerPointGrant> participationCareerPointRewards = new List<CareerPointGrant>();
    [Tooltip("Life Path XP, branch progress, tag counters or perk unlocks awarded whenever this contest completes, regardless of rank.")]
    [SerializeField] List<LifePathReward> participationLifePathRewards = new List<LifePathReward>();

    [Header("Organization Rewards")]
    [Tooltip("Organization memberships granted whenever this contest completes, regardless of rank.")]
    [SerializeField] List<OrganizationMembershipGrant> participationOrganizationMembershipRewards = new List<OrganizationMembershipGrant>();
    [Tooltip("Organization points awarded whenever this contest completes, regardless of rank.")]
    [SerializeField] List<OrganizationPointGrant> participationOrganizationPointRewards = new List<OrganizationPointGrant>();

    [Header("Events")]
    [Tooltip("Optional event published when this contest starts.")]
    [SerializeField] GameEventDefinition startedEvent;
    [Tooltip("Optional event published when this contest completes.")]
    [SerializeField] GameEventDefinition completedEvent;
    [Tooltip("If enabled, contest events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, contest events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public ContestCategory Category => category;
    public ContestDifficulty Difficulty => difficulty;
    public ContestEntryMode EntryMode => entryMode;
    public IReadOnlyList<string> Tags => tags;
    public bool UnlockedByDefault => unlockedByDefault;
    public int MinTrainerLevel => Mathf.Max(0, minTrainerLevel);
    public int MinPokemonLevel => Mathf.Max(0, minPokemonLevel);
    public int MaxPokemonLevel => Mathf.Max(0, maxPokemonLevel);
    public IReadOnlyList<PokemonType> AllowedTypes => allowedTypes;
    public IReadOnlyList<PokemonType> BannedTypes => bannedTypes;
    public IReadOnlyList<ContestItemCost> EntryCosts => entryCosts;
    public IReadOnlyList<ContestScoreCriterion> ScoreCriteria => scoreCriteria;
    public IReadOnlyList<ContestRankDefinition> RankThresholds => rankThresholds;
    public IReadOnlyList<CareerPointGrant> ParticipationCareerPointRewards => participationCareerPointRewards;
    public IReadOnlyList<LifePathReward> ParticipationLifePathRewards => participationLifePathRewards;
    public IReadOnlyList<OrganizationMembershipGrant> ParticipationOrganizationMembershipRewards => participationOrganizationMembershipRewards;
    public IReadOnlyList<OrganizationPointGrant> ParticipationOrganizationPointRewards => participationOrganizationPointRewards;

    public bool CanEnter(PlayerController player, Pokemon selectedPokemon, out string failureMessage) {
        var log = player != null ? player.GetComponent<PlayerContestLog>() : null;
        if(!unlockedByDefault && !(log?.HasUnlockedContest(this) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not unlocked." : lockedMessage;
            return false;
        }

        if(!PassesAccess(player, out failureMessage)) {
            return false;
        }

        var entryPokemon = ResolveEntryPokemon(player, selectedPokemon);
        if(entryMode != ContestEntryMode.WholeParty && !PassesPokemonRules(entryPokemon, out failureMessage)) {
            return false;
        }

        if(entryMode == ContestEntryMode.WholeParty && !PassesWholePartyRules(player, out failureMessage)) {
            return false;
        }

        if(!CanPayEntryCosts(out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool TryRunContest(PlayerController player, Pokemon selectedPokemon, out ContestRunResult result, out string failureMessage) {
        result = null;
        if(!CanEnter(player, selectedPokemon, out failureMessage)) {
            PublishContestEvent(startedEvent, "blocked", failureMessage, GameEventImportance.Warning, player, null);
            return false;
        }

        if(!TryPayEntryCosts(out failureMessage)) {
            PublishContestEvent(startedEvent, "blocked", failureMessage, GameEventImportance.Warning, player, null);
            return false;
        }

        var entryPokemon = ResolveEntryPokemon(player, selectedPokemon);
        PublishContestEvent(startedEvent, "started", $"{DisplayName} started.", GameEventImportance.Info, player, entryPokemon);

        int score = CalculateScore(player, entryPokemon);
        var rank = ResolveRank(score);
        int rankIndex = GetRankIndex(rank);
        bool won = rank != null && rank.CountsAsWin;

        result = new ContestRunResult {
            contestId = Id,
            contestName = DisplayName,
            category = category,
            difficulty = difficulty,
            pokemonName = entryPokemon != null && entryPokemon.Base != null ? entryPokemon.Base.Name : string.Empty,
            score = score,
            rankIndex = rankIndex,
            rankName = rank != null ? rank.DisplayName : string.Empty,
            won = won
        };

        rank?.ApplyRewards(player, this, result);
        player?.GetComponent<PlayerCareerLog>()?.ApplyPointGrants(participationCareerPointRewards, $"contest:{Id}");
        player?.GetComponent<PlayerLifePathLog>()?.ApplyRewards(participationLifePathRewards, $"contest:{Id}", DisplayName, this);
        var organizationLog = player?.GetComponent<PlayerOrganizationLog>();
        organizationLog?.ApplyMembershipGrants(participationOrganizationMembershipRewards, $"contest:{Id}");
        organizationLog?.ApplyPointGrants(participationOrganizationPointRewards, $"contest:{Id}");
        player?.GetComponent<PlayerContestLog>()?.RecordAttempt(this, result);
        PublishContestEvent(completedEvent, "completed", $"{DisplayName} completed with {score} points.", won ? GameEventImportance.Success : GameEventImportance.Info, player, entryPokemon, result);
        failureMessage = null;
        return true;
    }

    public int CalculateScore(PlayerController player, Pokemon selectedPokemon) {
        int score = baseScore;
        foreach(var criterion in scoreCriteria) {
            if(criterion != null) {
                score += criterion.Calculate(player, selectedPokemon);
            }
        }

        return Mathf.Max(0, score);
    }

    public ContestRankDefinition ResolveRank(int score) {
        return GetOrderedRanks()
            .Where(rank => rank != null && score >= rank.MinScore)
            .OrderByDescending(rank => rank.MinScore)
            .FirstOrDefault();
    }

    public int GetRankIndex(ContestRankDefinition rank) {
        if(rank == null) {
            return -1;
        }

        var ordered = GetOrderedRanks();
        return ordered.IndexOf(rank);
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    Pokemon ResolveEntryPokemon(PlayerController player, Pokemon selectedPokemon) {
        if(entryMode == ContestEntryMode.FirstHealthyPokemon || selectedPokemon == null) {
            return player != null ? player.GetComponent<PokemonParty>()?.GetHealthyPokemon() : null;
        }

        return selectedPokemon;
    }

    bool PassesAccess(PlayerController player, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to enter this contest.";
            return false;
        }

        if(MinTrainerLevel > 0 && (player.GetComponent<PlayerProgression>()?.Level ?? 0) < MinTrainerLevel) {
            failureMessage = $"Trainer level {MinTrainerLevel} is required.";
            return false;
        }

        if(requiredTitle != null && !(player.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredMilestone != null && !(player.GetComponent<PlayerMilestones>()?.HasMilestone(requiredMilestone) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredMilestone.DisplayName} first." : lockedMessage;
            return false;
        }

        if(requiredFaction != null) {
            int reputation = player.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0;
            if(reputation < requiredReputation) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more reputation with {requiredFaction.DisplayName}." : lockedMessage;
                return false;
            }
        }

        if(requiredSkill != null && (player.GetComponent<PlayerProgression>()?.GetSkillLevel(requiredSkill) ?? 0) < requiredSkillLevel) {
            failureMessage = $"You need {requiredSkill.DisplayName} level {requiredSkillLevel}.";
            return false;
        }

        if(requiredActiveCalendarEvent != null && !requiredActiveCalendarEvent.IsActiveNow()) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{requiredActiveCalendarEvent.Title} is not active right now." : lockedMessage;
            return false;
        }

        failureMessage = null;
        return true;
    }

    bool PassesWholePartyRules(PlayerController player, out string failureMessage) {
        var party = player != null ? player.GetComponent<PokemonParty>() : null;
        if(party == null || party.Pokemons == null || party.Pokemons.Count == 0) {
            failureMessage = "The party is empty.";
            return false;
        }

        foreach(var pokemon in party.Pokemons.Where(p => p != null && p.HP > 0)) {
            if(!PassesPokemonRules(pokemon, out failureMessage)) {
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    bool PassesPokemonRules(Pokemon pokemon, out string failureMessage) {
        if(pokemon == null || pokemon.Base == null) {
            failureMessage = "A Pokemon is required for this contest.";
            return false;
        }

        if(MinPokemonLevel > 0 && pokemon.Level < MinPokemonLevel) {
            failureMessage = $"{pokemon.Base.Name} must be at least level {MinPokemonLevel}.";
            return false;
        }

        if(MaxPokemonLevel > 0 && pokemon.Level > MaxPokemonLevel) {
            failureMessage = $"{pokemon.Base.Name} must be level {MaxPokemonLevel} or below.";
            return false;
        }

        if(bannedTypes != null && (bannedTypes.Contains(pokemon.Base.Type1) || bannedTypes.Contains(pokemon.Base.Type2))) {
            failureMessage = $"{pokemon.Base.Name} has a banned type.";
            return false;
        }

        if(allowedTypes != null && allowedTypes.Count > 0 && !allowedTypes.Contains(pokemon.Base.Type1) && !allowedTypes.Contains(pokemon.Base.Type2)) {
            failureMessage = $"{pokemon.Base.Name} does not match the allowed type list.";
            return false;
        }

        if(requiredMood != null && pokemon.GetMoodValue(requiredMood) < requiredMoodValue) {
            failureMessage = $"{pokemon.Base.Name} needs more {requiredMood.DisplayName}.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    bool CanPayEntryCosts(out string failureMessage) {
        var inventory = Inventory.GetInventory();
        foreach(var cost in entryCosts) {
            if(cost == null || cost.item == null || cost.count <= 0) {
                continue;
            }

            if(inventory == null || !inventory.HasItemEnough(cost.item, cost.count)) {
                failureMessage = $"You need {cost.count} {cost.item.Name} to enter {DisplayName}.";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    bool TryPayEntryCosts(out string failureMessage) {
        if(!CanPayEntryCosts(out failureMessage)) {
            return false;
        }

        var inventory = Inventory.GetInventory();
        foreach(var cost in entryCosts) {
            if(cost != null && cost.item != null && cost.count > 0) {
                inventory?.RemoveItem(cost.item, cost.count);
            }
        }

        failureMessage = null;
        return true;
    }

    List<ContestRankDefinition> GetOrderedRanks() {
        return (rankThresholds ?? new List<ContestRankDefinition>())
            .Where(rank => rank != null)
            .OrderBy(rank => rank.MinScore)
            .ToList();
    }

    void PublishContestEvent(GameEventDefinition eventDefinition, string phase, string message, GameEventImportance importance, PlayerController player, Pokemon pokemon, ContestRunResult result = null) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"contest.{phase}.{Id}",
            message,
            GameEventCategory.Contest,
            importance,
            player != null ? player : this,
            "ContestDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("contestId", Id),
            GameEventPublishing.Value("contestName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("difficulty", difficulty),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("pokemon", pokemon != null && pokemon.Base != null ? pokemon.Base.Name : string.Empty),
            GameEventPublishing.Value("score", result != null ? result.score.ToString() : string.Empty),
            GameEventPublishing.Value("rank", result != null ? result.rankName : string.Empty),
            GameEventPublishing.Value("won", result != null ? result.won.ToString() : string.Empty));
    }
}

[Serializable]
public class ContestItemCost {
    [Tooltip("Item consumed by this contest entry cost.")]
    public ItemBase item;
    [Tooltip("Item count consumed.")]
    [Min(1)]
    public int count = 1;
}

[Serializable]
public class ContestScoreCriterion {
    [Tooltip("Editor/debug label for this score criterion.")]
    public string displayName;
    [Tooltip("Which runtime value this criterion converts into score.")]
    public ContestScoreSource source = ContestScoreSource.Flat;
    [Tooltip("Score multiplier applied to the source value.")]
    public float weight = 1f;
    [Tooltip("Flat score used by Flat source.")]
    public int flatScore;
    [Tooltip("Minimum random score used by Random Range source.")]
    public int minRandom;
    [Tooltip("Maximum random score used by Random Range source.")]
    public int maxRandom;
    [Tooltip("Player skill used by Player Skill Level source.")]
    public PlayerSkillDefinition skill;
    [Tooltip("Skill tag used by Player Skill Tag Level source.")]
    public string skillTag;
    [Tooltip("Pokemon mood used by Pokemon Mood source.")]
    public PokemonMoodDefinition mood;
    [Tooltip("Item counted by Item Count source.")]
    public ItemBase item;
    [Tooltip("Faction checked by Reputation source.")]
    public ReputationFactionDefinition faction;
    [Tooltip("Battle challenge checked by Battle Challenge Wins source.")]
    public BattleChallengeDefinition battleChallenge;
    [Tooltip("Optional battle rule filter for Battle Challenge Wins source.")]
    public BattleRuleSetDefinition battleRuleSet;

    public int Calculate(PlayerController player, Pokemon pokemon) {
        float value = source switch {
            ContestScoreSource.PokemonLevel => pokemon != null ? pokemon.Level : 0,
            ContestScoreSource.PokemonFriendship => pokemon != null ? pokemon.Friendship : 0,
            ContestScoreSource.PokemonMood => pokemon != null ? pokemon.GetMoodValue(mood) : 0,
            ContestScoreSource.PlayerLevel => player != null ? player.GetComponent<PlayerProgression>()?.Level ?? 0 : 0,
            ContestScoreSource.PlayerSkillLevel => player != null ? player.GetComponent<PlayerProgression>()?.GetSkillLevel(skill) ?? 0 : 0,
            ContestScoreSource.PlayerSkillTagLevel => player != null ? player.GetComponent<PlayerProgression>()?.GetHighestSkillLevelWithTag(skillTag) ?? 0 : 0,
            ContestScoreSource.PartyHighestLevel => GetHighestPartyLevel(player),
            ContestScoreSource.ItemCount => Inventory.GetInventory()?.GetItemCount(item) ?? 0,
            ContestScoreSource.Reputation => player != null ? player.GetComponent<PlayerReputation>()?.GetReputation(faction) ?? 0 : 0,
            ContestScoreSource.BattleChallengeWins => player != null ? player.GetComponent<PlayerBattleRuleLog>()?.GetWinCount(battleChallenge, battleRuleSet) ?? 0 : 0,
            ContestScoreSource.RandomRange => UnityEngine.Random.Range(Mathf.Min(minRandom, maxRandom), Mathf.Max(minRandom, maxRandom) + 1),
            _ => flatScore
        };

        return Mathf.RoundToInt(value * weight);
    }

    int GetHighestPartyLevel(PlayerController player) {
        var party = player != null ? player.GetComponent<PokemonParty>() : null;
        if(party == null || party.Pokemons == null) {
            return 0;
        }

        return party.Pokemons.Where(p => p != null).Select(p => p.Level).DefaultIfEmpty(0).Max();
    }
}

[Serializable]
public class ContestRankDefinition {
    [Tooltip("Stable rank id used by save/debug output.")]
    public string id;
    [Tooltip("Name shown for this result rank.")]
    public string displayName;
    [Tooltip("Minimum score required to receive this rank.")]
    [Min(0)]
    public int minScore;
    [Tooltip("If enabled, reaching this rank counts as winning the contest.")]
    public bool countsAsWin;
    [Tooltip("Trainer XP granted when this rank is reached.")]
    [Min(0)]
    public int trainerExperience;
    [Tooltip("Progression source used for trainer XP from this rank.")]
    public PlayerExperienceSource experienceSource = PlayerExperienceSource.Contest;
    [Tooltip("Items granted when this rank is reached.")]
    public List<ContestItemReward> itemRewards = new List<ContestItemReward>();
    [Tooltip("Faction reputation changes applied when this rank is reached.")]
    public List<ReputationChange> reputationChanges = new List<ReputationChange>();
    [Tooltip("Milestones completed when this rank is reached.")]
    public List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Titles, medals, permits or ranks granted when this rank is reached.")]
    public List<TitleGrant> titleGrants = new List<TitleGrant>();
    [Tooltip("Career points awarded when this contest rank is reached.")]
    public List<CareerPointGrant> careerPointRewards = new List<CareerPointGrant>();
    [Tooltip("Life Path XP, branch progress, tag counters or perk unlocks awarded when this contest rank is reached.")]
    public List<LifePathReward> lifePathRewards = new List<LifePathReward>();
    [Tooltip("Organization memberships granted when this contest rank is reached.")]
    public List<OrganizationMembershipGrant> organizationMembershipRewards = new List<OrganizationMembershipGrant>();
    [Tooltip("Organization points awarded when this contest rank is reached.")]
    public List<OrganizationPointGrant> organizationPointRewards = new List<OrganizationPointGrant>();

    public string Id => string.IsNullOrWhiteSpace(id) ? DisplayName : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
    public int MinScore => Mathf.Max(0, minScore);
    public bool CountsAsWin => countsAsWin;

    public void ApplyRewards(PlayerController player, ContestDefinition contest, ContestRunResult result) {
        if(player == null) {
            return;
        }

        if(trainerExperience > 0) {
            player.GetComponent<PlayerProgression>()?.AddExperience(trainerExperience, experienceSource);
        }

        var inventory = Inventory.GetInventory();
        foreach(var reward in itemRewards) {
            if(reward != null && reward.item != null) {
                int count = reward.RollCount();
                if(count > 0) {
                    inventory?.AddItem(reward.item, count);
                }
            }
        }

        player.GetComponent<PlayerReputation>()?.ApplyChanges(reputationChanges);
        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);
        player.GetComponent<PlayerTitles>()?.ApplyGrants(titleGrants, contest);
        player.GetComponent<PlayerCareerLog>()?.ApplyPointGrants(careerPointRewards, $"contest-rank:{contest.Id}:{Id}");
        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(lifePathRewards, $"contest-rank:{contest.Id}:{Id}", $"{contest.DisplayName} {DisplayName}", contest);
        var organizationLog = player.GetComponent<PlayerOrganizationLog>();
        organizationLog?.ApplyMembershipGrants(organizationMembershipRewards, $"contest-rank:{contest.Id}:{Id}");
        organizationLog?.ApplyPointGrants(organizationPointRewards, $"contest-rank:{contest.Id}:{Id}");
    }
}

[Serializable]
public class ContestItemReward {
    [Tooltip("Item granted by this reward.")]
    public ItemBase item;
    [Tooltip("Minimum item count granted.")]
    [Min(0)]
    public int minCount = 1;
    [Tooltip("Maximum item count granted.")]
    [Min(0)]
    public int maxCount = 1;

    public int RollCount() {
        int min = Mathf.Max(0, minCount);
        int max = Mathf.Max(min, maxCount);
        return UnityEngine.Random.Range(min, max + 1);
    }
}

[Serializable]
public class ContestRunResult {
    [Tooltip("Contest id that produced this result.")]
    public string contestId;
    [Tooltip("Contest display name at the time of completion.")]
    public string contestName;
    [Tooltip("Contest category at the time of completion.")]
    public ContestCategory category;
    [Tooltip("Contest difficulty at the time of completion.")]
    public ContestDifficulty difficulty;
    [Tooltip("Pokemon name used for this entry.")]
    public string pokemonName;
    [Tooltip("Final score awarded by the contest.")]
    public int score;
    [Tooltip("Rank index from the contest rank list. Higher means better.")]
    public int rankIndex = -1;
    [Tooltip("Rank name awarded by the contest.")]
    public string rankName;
    [Tooltip("Whether the awarded rank counted as a win.")]
    public bool won;
}
