using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Competitions/Match Resolver Definition")]
public class CompetitionMatchResolverDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this match resolver. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug logs or future bracket UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining how this resolver should simulate matches.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as casual, league, frontier, championship, deterministic or upset-heavy.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Safety")]
    [Tooltip("If disabled, matches containing the player are never resolved automatically.")]
    [SerializeField] bool allowPlayerMatchResolution;
    [Tooltip("If enabled, a match with only one entrant is resolved as a bye.")]
    [SerializeField] bool resolveByes = true;
    [Tooltip("If enabled, already completed matches are ignored instead of treated as invalid.")]
    [SerializeField] bool ignoreCompletedMatches = true;

    [Header("Power")]
    [Tooltip("Base power used when no specific kind rule exists.")]
    [Min(1)]
    [SerializeField] int defaultPower = 50;
    [Tooltip("Minimum final power after all modifiers.")]
    [Min(1)]
    [SerializeField] int minimumPower = 1;
    [Tooltip("Power added for lower seeded ranks. 0 disables seeded-rank power.")]
    [SerializeField] float seededRankWeight = 1f;
    [Tooltip("Baseline used by seeded rank power. Lower ranks gain roughly (baseline - rank) * weight.")]
    [Min(0)]
    [SerializeField] int seededRankBaseline = 100;
    [Tooltip("Power rules per entrant kind.")]
    [SerializeField] List<CompetitionEntrantKindPowerRule> kindPowerRules = new List<CompetitionEntrantKindPowerRule>();
    [Tooltip("Power modifiers applied when an entrant has matching tags.")]
    [SerializeField] List<CompetitionEntrantTagPowerRule> tagPowerRules = new List<CompetitionEntrantTagPowerRule>();

    [Header("Randomness")]
    [Tooltip("Salt mixed into deterministic match rolls.")]
    [SerializeField] int seedSalt;
    [Tooltip("Minimum random power variance added before calculating win chance.")]
    [SerializeField] int randomVarianceMin = -5;
    [Tooltip("Maximum random power variance added before calculating win chance.")]
    [SerializeField] int randomVarianceMax = 5;
    [Tooltip("Minimum chance any entrant can have to win a simulated match.")]
    [Range(0f, 1f)]
    [SerializeField] float minimumWinChance = 0.05f;
    [Tooltip("Maximum chance any entrant can have to win a simulated match.")]
    [Range(0f, 1f)]
    [SerializeField] float maximumWinChance = 0.95f;
    [Tooltip("If enabled, equal-power ties prefer the lower seeded rank. If disabled, ties use the deterministic roll.")]
    [SerializeField] bool preferLowerSeedOnTie = true;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public bool AllowPlayerMatchResolution => allowPlayerMatchResolution;
    public bool ResolveByes => resolveByes;
    public bool IgnoreCompletedMatches => ignoreCompletedMatches;
    public int DefaultPower => Mathf.Max(1, defaultPower);
    public int MinimumPower => Mathf.Max(1, minimumPower);
    public IReadOnlyList<CompetitionEntrantKindPowerRule> KindPowerRules => kindPowerRules != null ? (IReadOnlyList<CompetitionEntrantKindPowerRule>)kindPowerRules : Array.Empty<CompetitionEntrantKindPowerRule>();
    public IReadOnlyList<CompetitionEntrantTagPowerRule> TagPowerRules => tagPowerRules != null ? (IReadOnlyList<CompetitionEntrantTagPowerRule>)tagPowerRules : Array.Empty<CompetitionEntrantTagPowerRule>();

    public bool TryResolve(
        CompetitionRosterDefinition roster,
        PlayerCompetitionBracketState state,
        PlayerCompetitionBracketMatchRecord match,
        string sourceId,
        out CompetitionMatchResolutionResult result,
        out string failureMessage
    ) {
        result = null;
        if(!CanResolve(state, match, out failureMessage)) {
            return false;
        }

        var first = state.GetEntrant(match.firstEntrantId);
        var second = state.GetEntrant(match.secondEntrantId);
        if((first == null || second == null) && resolveByes) {
            var byeWinner = first ?? second;
            if(byeWinner == null) {
                failureMessage = "No entrant is available to win this bye.";
                return false;
            }

            result = CompetitionMatchResolutionResult.Create(match, byeWinner.entrantId, Id, 0, 0);
            failureMessage = null;
            return true;
        }

        if(first == null || second == null) {
            failureMessage = "Both entrants are required to resolve this match.";
            return false;
        }

        var random = new System.Random(CreateSeed(state, match, sourceId));
        int firstPower = CalculatePower(roster, first, random);
        int secondPower = CalculatePower(roster, second, random);
        string winnerId = ResolveWinner(first, second, firstPower, secondPower, random);

        result = CompetitionMatchResolutionResult.Create(match, winnerId, Id, firstPower, secondPower);
        failureMessage = null;
        return true;
    }

    public bool CanResolve(PlayerCompetitionBracketState state, PlayerCompetitionBracketMatchRecord match, out string failureMessage) {
        if(state == null) {
            failureMessage = "A bracket state is required.";
            return false;
        }

        if(match == null) {
            failureMessage = "A match is required.";
            return false;
        }

        if(match.completed) {
            failureMessage = ignoreCompletedMatches ? null : "Match is already completed.";
            return false;
        }

        if(match.playerInMatch && !allowPlayerMatchResolution) {
            failureMessage = "Player matches cannot be auto-resolved by this resolver.";
            return false;
        }

        bool hasFirst = !string.IsNullOrWhiteSpace(match.firstEntrantId);
        bool hasSecond = !string.IsNullOrWhiteSpace(match.secondEntrantId);
        if(resolveByes && (hasFirst || hasSecond)) {
            failureMessage = null;
            return true;
        }

        if(!hasFirst || !hasSecond) {
            failureMessage = "Match does not have two entrants.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    int CalculatePower(CompetitionRosterDefinition roster, PlayerCompetitionBracketEntrantRecord entrant, System.Random random) {
        if(entrant == null) {
            return MinimumPower;
        }

        int power = GetKindPower(entrant.kind);
        if(entrant.seededRank > 0 && seededRankWeight != 0f) {
            power += Mathf.RoundToInt(Mathf.Max(0, seededRankBaseline - entrant.seededRank) * seededRankWeight);
        }

        foreach(var rule in TagPowerRules) {
            if(rule != null && rule.Matches(entrant)) {
                power += rule.PowerModifier;
            }
        }

        power += random.Next(Mathf.Min(randomVarianceMin, randomVarianceMax), Mathf.Max(randomVarianceMin, randomVarianceMax) + 1);
        return Mathf.Max(MinimumPower, power);
    }

    int GetKindPower(CompetitionEntrantKind kind) {
        var rule = KindPowerRules.FirstOrDefault(entry => entry != null && entry.Kind == kind);
        return rule != null ? rule.Power : DefaultPower;
    }

    string ResolveWinner(PlayerCompetitionBracketEntrantRecord first, PlayerCompetitionBracketEntrantRecord second, int firstPower, int secondPower, System.Random random) {
        if(preferLowerSeedOnTie && firstPower == secondPower && first.seededRank > 0 && second.seededRank > 0 && first.seededRank != second.seededRank) {
            return first.seededRank < second.seededRank ? first.entrantId : second.entrantId;
        }

        float chance = firstPower / Mathf.Max(1f, firstPower + secondPower);
        float minChance = Mathf.Min(minimumWinChance, maximumWinChance);
        float maxChance = Mathf.Max(minimumWinChance, maximumWinChance);
        chance = Mathf.Clamp(chance, minChance, maxChance);
        return random.NextDouble() <= chance ? first.entrantId : second.entrantId;
    }

    int CreateSeed(PlayerCompetitionBracketState state, PlayerCompetitionBracketMatchRecord match, string sourceId) {
        unchecked {
            int hash = 17;
            hash = hash * 31 + Id.GetHashCode();
            hash = hash * 31 + (state != null ? state.seed : 0);
            hash = hash * 31 + (match != null && match.matchId != null ? match.matchId.GetHashCode() : 0);
            hash = hash * 31 + (sourceId != null ? sourceId.GetHashCode() : 0);
            hash = hash * 31 + seedSalt;
            return Mathf.Abs(hash);
        }
    }
}

[Serializable]
public class CompetitionEntrantKindPowerRule {
    [Tooltip("Entrant kind matched by this power rule.")]
    [SerializeField] CompetitionEntrantKind kind = CompetitionEntrantKind.Trainer;
    [Tooltip("Base power used by entrants of this kind.")]
    [Min(1)]
    [SerializeField] int power = 50;

    public CompetitionEntrantKind Kind => kind;
    public int Power => Mathf.Max(1, power);
}

[Serializable]
public class CompetitionEntrantTagPowerRule {
    [Tooltip("Entrant tag matched by this power modifier.")]
    [SerializeField] string tag = string.Empty;
    [Tooltip("Power added when the entrant has the selected tag. Negative values weaken the entrant.")]
    [SerializeField] int powerModifier;

    public string Tag => tag;
    public int PowerModifier => powerModifier;

    public bool Matches(PlayerCompetitionBracketEntrantRecord entrant) {
        return entrant != null
            && !string.IsNullOrWhiteSpace(tag)
            && entrant.tags != null
            && entrant.tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }
}

public class CompetitionMatchResolutionResult {
    public string MatchId { get; private set; }
    public string WinnerEntrantId { get; private set; }
    public string ResolverId { get; private set; }
    public int FirstPower { get; private set; }
    public int SecondPower { get; private set; }

    public static CompetitionMatchResolutionResult Create(PlayerCompetitionBracketMatchRecord match, string winnerEntrantId, string resolverId, int firstPower, int secondPower) {
        return new CompetitionMatchResolutionResult {
            MatchId = match != null ? match.matchId : string.Empty,
            WinnerEntrantId = winnerEntrantId,
            ResolverId = resolverId,
            FirstPower = firstPower,
            SecondPower = secondPower
        };
    }
}
