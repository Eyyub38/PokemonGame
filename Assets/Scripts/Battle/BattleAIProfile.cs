using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BattleAITier {
    Wild,
    Amateur,
    Skilled,
    Expert,
    Champion,
    Custom
}

public enum BattleAITargetPolicy {
    Random,
    LowestHp,
    HighestDamage,
    Balanced
}

[CreateAssetMenu(menuName = "Battle/AI Profile")]
public class BattleAIProfile : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id for this AI profile. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in editor/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note describing this AI style, such as amateur, veteran or champion.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad difficulty tier used by validators, trainers and future UI filters.")]
    [SerializeField] BattleAITier tier = BattleAITier.Amateur;
    [Tooltip("Free-form tags such as wild, amateur, gym, elite, champion, defensive or aggressive.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Move Scoring")]
    [Tooltip("How strongly raw move power affects AI scoring.")]
    [SerializeField] float damageWeight = 1f;
    [Tooltip("Score added for favorable type effectiveness.")]
    [SerializeField] float typeEffectivenessWeight = 20f;
    [Tooltip("Score added when the user shares the move type.")]
    [SerializeField] float sameTypeAttackBonusWeight = 8f;
    [Tooltip("Score used for status-category moves.")]
    [SerializeField] float statusMoveWeight = 12f;
    [Tooltip("Extra score when a move can likely finish a low-HP target.")]
    [SerializeField] float finishingMoveBonus = 18f;
    [Tooltip("Extra score when a target is already low on HP.")]
    [SerializeField] float lowHpAggressionBonus = 10f;
    [Tooltip("Score added for higher accuracy. 0 ignores accuracy.")]
    [SerializeField] float accuracyWeight = 0.2f;
    [Tooltip("Penalty applied when a usable move has very low remaining PP.")]
    [SerializeField] float lowPpPenalty = 4f;

    [Header("Targeting")]
    [Tooltip("How this AI chooses targets when several player Pokemon are active.")]
    [SerializeField] BattleAITargetPolicy targetPolicy = BattleAITargetPolicy.Balanced;
    [Tooltip("Extra target score for enemies below 35 percent HP.")]
    [SerializeField] float lowHpTargetWeight = 12f;
    [Tooltip("Extra target score when this AI has at least one super-effective move against the target.")]
    [SerializeField] float weaknessTargetWeight = 10f;
    [Tooltip("If enabled, target selection avoids fainted or empty battle units.")]
    [SerializeField] bool ignoreInvalidTargets = true;

    [Header("Decision Quality")]
    [Tooltip("Chance/strength of random decisions. 0 is consistent, 1 is very noisy.")]
    [Range(0f, 1f)]
    [SerializeField] float randomness = 0.35f;
    [Tooltip("If enabled, the AI avoids moves that cannot affect the target.")]
    [SerializeField] bool avoidImmuneMoves = true;
    [Tooltip("If disabled, the AI ignores status moves unless every usable move is status-only.")]
    [SerializeField] bool allowStatusMoves = true;
    [Tooltip("If enabled, this AI may pick a random legal move instead of scoring all moves.")]
    [SerializeField] bool allowRandomMistakes = true;

    [Header("Switching")]
    [Tooltip("If enabled, this AI can choose Switch Pokemon as a battle action.")]
    [SerializeField] bool allowSwitching = false;
    [Tooltip("If enabled, switching is only considered in trainer battles.")]
    [SerializeField] bool switchOnlyTrainerBattles = true;
    [Tooltip("HP percent under which switching can be considered.")]
    [Range(0f, 1f)]
    [SerializeField] float lowHpSwitchThreshold = 0.25f;
    [Tooltip("Chance to switch when the active Pokemon is under the low HP threshold.")]
    [Range(0f, 1f)]
    [SerializeField] float lowHpSwitchChance = 0.35f;
    [Tooltip("Chance to switch when the matchup looks bad and a better party member exists.")]
    [Range(0f, 1f)]
    [SerializeField] float badMatchupSwitchChance = 0.2f;
    [Tooltip("Minimum score improvement required before a matchup switch is considered.")]
    [SerializeField] float minimumSwitchScoreImprovement = 18f;

    [Header("Debug")]
    [Tooltip("If enabled, AI decisions are written to GameDebugLogger.")]
    [SerializeField] bool logDecisions = false;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public BattleAITier Tier => tier;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public float Randomness => Mathf.Clamp01(randomness);
    public bool AllowSwitching => allowSwitching;
    public BattleAITargetPolicy TargetPolicy => targetPolicy;

    public BattleAction ChooseAction(
        BattleUnit userUnit,
        IReadOnlyList<BattleUnit> targetUnits,
        IReadOnlyList<BattleUnit> allyUnits,
        PokemonParty ownParty,
        BattleSystem battleSystem
    ) {
        if(userUnit == null || userUnit.Pokemon == null || userUnit.Pokemon.HP <= 0) {
            return null;
        }

        var targets = GetValidTargetUnits(targetUnits);
        if(targets.Count == 0) {
            return null;
        }

        if(ShouldSwitch(userUnit, targets, allyUnits, ownParty, battleSystem, out var switchPokemon)) {
            LogDecision(userUnit, $"switch -> {switchPokemon.Base.Name}");
            return new BattleAction {
                Type = BattleActionType.SwitchPokemon,
                User = userUnit,
                SelectedPokemon = switchPokemon
            };
        }

        var scoredMove = ChooseMoveTarget(userUnit.Pokemon, userUnit, targets);
        var selectedMove = scoredMove.move ?? userUnit.Pokemon.GetRandomMove() ?? new Move(GlobalSettings.i.BackUpMove);
        var selectedTarget = scoredMove.target ?? PickFallbackTarget(targets);

        LogDecision(userUnit, $"{selectedMove.Base.Name} -> {selectedTarget?.Pokemon?.Base?.Name ?? "none"} score {scoredMove.score:0.0}");
        return new BattleAction {
            Type = BattleActionType.Move,
            SelectedMove = selectedMove,
            User = userUnit,
            Target = selectedMove.Base.Target == MoveTarget.Self ? userUnit : selectedTarget
        };
    }

    public Move ChooseMove(Pokemon user, IReadOnlyList<Pokemon> targets) {
        if(user == null) {
            return null;
        }

        var usableMoves = GetUsableMoves(user);
        if(usableMoves.Count == 0) {
            return null;
        }

        var aliveTargets = targets != null ? targets.Where(t => t != null && t.HP > 0).ToList() : new List<Pokemon>();
        if(aliveTargets.Count == 0 || ShouldMakeRandomMistake()) {
            return usableMoves[UnityEngine.Random.Range(0, usableMoves.Count)];
        }

        Move bestMove = null;
        float bestScore = float.MinValue;
        foreach(var move in usableMoves) {
            foreach(var target in aliveTargets) {
                float score = ScoreMoveAgainstTarget(user, move, target);
                if(score > bestScore) {
                    bestScore = score;
                    bestMove = move;
                }
            }
        }

        return bestMove ?? usableMoves[UnityEngine.Random.Range(0, usableMoves.Count)];
    }

    (Move move, BattleUnit target, float score) ChooseMoveTarget(Pokemon user, BattleUnit userUnit, List<BattleUnit> targets) {
        var usableMoves = GetUsableMoves(user);
        if(usableMoves.Count == 0) {
            return (null, PickFallbackTarget(targets), 0f);
        }

        if(ShouldMakeRandomMistake()) {
            var move = usableMoves[UnityEngine.Random.Range(0, usableMoves.Count)];
            return (move, PickTargetForPolicy(user, move, targets), 0f);
        }

        Move bestMove = null;
        BattleUnit bestTarget = null;
        float bestScore = float.MinValue;

        foreach(var move in usableMoves) {
            if(!allowStatusMoves && move.Base.Category == MoveCategory.Status && usableMoves.Any(candidate => candidate.Base.Category != MoveCategory.Status)) {
                continue;
            }

            var candidateTargets = move.Base.Target == MoveTarget.Self ? new List<BattleUnit> { userUnit } : targets;
            foreach(var targetUnit in candidateTargets) {
                if(targetUnit == null || targetUnit.Pokemon == null) {
                    continue;
                }

                float score = ScoreMoveAgainstTarget(user, move, targetUnit.Pokemon);
                score += ScoreTarget(user, move, targetUnit);
                score += UnityEngine.Random.Range(0f, Randomness * 10f);

                if(score > bestScore) {
                    bestScore = score;
                    bestMove = move;
                    bestTarget = targetUnit;
                }
            }
        }

        return (bestMove, bestTarget, bestScore);
    }

    bool ShouldSwitch(
        BattleUnit userUnit,
        List<BattleUnit> targets,
        IReadOnlyList<BattleUnit> allyUnits,
        PokemonParty ownParty,
        BattleSystem battleSystem,
        out Pokemon switchPokemon
    ) {
        switchPokemon = null;
        if(!allowSwitching || userUnit == null || ownParty == null || battleSystem == null) {
            return false;
        }

        if(switchOnlyTrainerBattles && !battleSystem.IsTrainerBattle) {
            return false;
        }

        if(!battleSystem.CanSwitchByRule(userUnit.IsPlayerUnit, out _)) {
            return false;
        }

        var candidates = GetSwitchCandidates(ownParty, allyUnits, battleSystem);
        if(candidates.Count == 0) {
            return false;
        }

        float hpPercent = userUnit.Pokemon.MaxHp > 0 ? userUnit.Pokemon.HP / (float)userUnit.Pokemon.MaxHp : 1f;
        bool lowHpSwitch = hpPercent <= lowHpSwitchThreshold && UnityEngine.Random.value <= lowHpSwitchChance;

        float currentScore = ScorePokemonMatchup(userUnit.Pokemon, targets);
        var bestCandidate = candidates
            .Select(candidate => new { pokemon = candidate, score = ScorePokemonMatchup(candidate, targets) + UnityEngine.Random.Range(0f, Randomness * 6f) })
            .OrderByDescending(entry => entry.score)
            .FirstOrDefault();

        if(bestCandidate == null) {
            return false;
        }

        bool betterMatchup = bestCandidate.score >= currentScore + minimumSwitchScoreImprovement
            && UnityEngine.Random.value <= badMatchupSwitchChance;

        if(lowHpSwitch || betterMatchup) {
            switchPokemon = bestCandidate.pokemon;
            return switchPokemon != null;
        }

        return false;
    }

    List<Pokemon> GetSwitchCandidates(PokemonParty ownParty, IReadOnlyList<BattleUnit> allyUnits, BattleSystem battleSystem) {
        var activePokemon = allyUnits != null
            ? allyUnits.Where(unit => unit != null).Select(unit => unit.Pokemon).Where(pokemon => pokemon != null).ToList()
            : new List<Pokemon>();

        return ownParty.Pokemons
            .Where(pokemon => pokemon != null && pokemon.HP > 0)
            .Where(pokemon => !activePokemon.Contains(pokemon))
            .Where(pokemon => battleSystem == null || !battleSystem.IsPokemonSelectedToShift(pokemon))
            .ToList();
    }

    float ScorePokemonMatchup(Pokemon candidate, List<BattleUnit> targets) {
        if(candidate == null || candidate.Base == null || targets == null || targets.Count == 0) {
            return 0f;
        }

        float score = 0f;
        foreach(var targetUnit in targets) {
            var target = targetUnit != null ? targetUnit.Pokemon : null;
            if(target == null || target.Base == null || target.HP <= 0) {
                continue;
            }

            var vitalProfile = BattleSystem.i != null ? BattleSystem.i.ActiveVitalProfile : null;
            float offense = candidate.Moves != null && candidate.Moves.Count > 0
                ? candidate.Moves.Where(move => candidate.CanUseMove(move, vitalProfile)).Select(move => ScoreMoveAgainstTarget(candidate, move, target)).DefaultIfEmpty(0f).Max()
                : 0f;
            float danger = EstimateTargetDanger(target, candidate);
            score += offense - danger * 0.6f;
        }

        return score;
    }

    float EstimateTargetDanger(Pokemon target, Pokemon candidate) {
        if(target == null || candidate == null || target.Moves == null) {
            return 0f;
        }

        float danger = 0f;
        var vitalProfile = BattleSystem.i != null ? BattleSystem.i.ActiveVitalProfile : null;
        foreach(var move in target.Moves.Where(move => target.CanUseMove(move, vitalProfile))) {
            if(move == null || move.Base == null || move.Base.Category == MoveCategory.Status) {
                continue;
            }

            var moveType = target.GetMoveType(move, candidate);
            float effectiveness = TypeChart.GetEffectiveness(moveType, candidate.Base.Type1) * TypeChart.GetEffectiveness(moveType, candidate.Base.Type2);
            danger = Mathf.Max(danger, move.Base.Power * effectiveness);
        }

        return danger;
    }

    List<BattleUnit> GetValidTargetUnits(IReadOnlyList<BattleUnit> targetUnits) {
        if(targetUnits == null) {
            return new List<BattleUnit>();
        }

        return targetUnits
            .Where(unit => unit != null && unit.Pokemon != null)
            .Where(unit => !ignoreInvalidTargets || unit.Pokemon.HP > 0)
            .ToList();
    }

    List<Move> GetUsableMoves(Pokemon user) {
        var vitalProfile = BattleSystem.i != null ? BattleSystem.i.ActiveVitalProfile : null;
        return user != null && user.Moves != null
            ? user.Moves.Where(move => move != null && move.Base != null && user.CanUseMove(move, vitalProfile)).ToList()
            : new List<Move>();
    }

    BattleUnit PickTargetForPolicy(Pokemon user, Move move, List<BattleUnit> targets) {
        if(targets == null || targets.Count == 0) {
            return null;
        }

        if(move != null && move.Base.Target == MoveTarget.Self) {
            return null;
        }

        if(targetPolicy == BattleAITargetPolicy.Random) {
            return PickFallbackTarget(targets);
        }

        return targets
            .OrderByDescending(target => ScoreTarget(user, move, target))
            .FirstOrDefault() ?? PickFallbackTarget(targets);
    }

    BattleUnit PickFallbackTarget(List<BattleUnit> targets) {
        if(targets == null || targets.Count == 0) {
            return null;
        }

        return targets[UnityEngine.Random.Range(0, targets.Count)];
    }

    float ScoreMoveAgainstTarget(Pokemon user, Move move, Pokemon target) {
        if(user == null || move == null || move.Base == null || target == null || target.Base == null) {
            return -100f;
        }

        if(move.Base.Category == MoveCategory.Status) {
            return statusMoveWeight + ScoreStatusMove(user, move, target);
        }

        var moveType = user.GetMoveType(move, target);
        float effectiveness = TypeChart.GetEffectiveness(moveType, target.Base.Type1) * TypeChart.GetEffectiveness(moveType, target.Base.Type2);

        if(avoidImmuneMoves && effectiveness <= 0f) {
            return -100f;
        }

        float score = move.Base.Power * damageWeight;
        score += effectiveness * typeEffectivenessWeight;
        score += user.HasType(moveType) ? sameTypeAttackBonusWeight : 0f;
        score += move.Base.AlwaysHits ? accuracyWeight * 100f : move.Base.Accuracy * accuracyWeight;

        if(target.HP <= target.MaxHp * 0.35f) {
            score += lowHpAggressionBonus;
        }

        if(CanLikelyFinishTarget(move, effectiveness, target)) {
            score += finishingMoveBonus;
        }

        if(move.PP <= 2 && move.Base.PP > 5) {
            score -= lowPpPenalty;
        }

        return score;
    }

    float ScoreStatusMove(Pokemon user, Move move, Pokemon target) {
        float score = 0f;
        var effects = move.Base.Effects;
        if(effects == null) {
            return score;
        }

        if(move.Base.Target == MoveTarget.Self) {
            score += user.HP <= user.MaxHp * 0.5f && effects.HealingPercentage > 0 ? 12f : 0f;
            score += effects.Boosts != null && effects.Boosts.Count > 0 ? 8f : 0f;
        } else {
            score += target.HP > target.MaxHp * 0.35f && effects.Status != StatusConditionID.None ? 8f : 0f;
            score += effects.VolatileStatus != StatusConditionID.None ? 7f : 0f;
            score += effects.Boosts != null && effects.Boosts.Count > 0 ? 6f : 0f;
        }

        return score;
    }

    float ScoreTarget(Pokemon user, Move move, BattleUnit targetUnit) {
        if(targetUnit == null || targetUnit.Pokemon == null) {
            return -100f;
        }

        var target = targetUnit.Pokemon;
        if(targetPolicy == BattleAITargetPolicy.Random) {
            return UnityEngine.Random.Range(0f, Randomness * 10f);
        }

        float score = 0f;
        float hpPercent = target.MaxHp > 0 ? target.HP / (float)target.MaxHp : 1f;
        if(targetPolicy == BattleAITargetPolicy.LowestHp || targetPolicy == BattleAITargetPolicy.Balanced) {
            score += (1f - hpPercent) * lowHpTargetWeight;
        }

        if(targetPolicy == BattleAITargetPolicy.HighestDamage || targetPolicy == BattleAITargetPolicy.Balanced) {
            score += move != null ? ScoreMoveAgainstTarget(user, move, target) * 0.25f : weaknessTargetWeight;
        }

        if(move != null && move.Base.Category != MoveCategory.Status) {
            var moveType = user.GetMoveType(move, target);
            float effectiveness = TypeChart.GetEffectiveness(moveType, target.Base.Type1) * TypeChart.GetEffectiveness(moveType, target.Base.Type2);
            if(effectiveness > 1f) {
                score += weaknessTargetWeight;
            }
        }

        return score;
    }

    bool CanLikelyFinishTarget(Move move, float effectiveness, Pokemon target) {
        if(move == null || move.Base == null || target == null || move.Base.Category == MoveCategory.Status) {
            return false;
        }

        float roughDamage = move.Base.Power * Mathf.Max(0f, effectiveness);
        return target.HP <= roughDamage * 0.6f;
    }

    bool ShouldMakeRandomMistake() {
        return allowRandomMistakes && Randomness > 0f && UnityEngine.Random.value < Randomness;
    }

    void LogDecision(BattleUnit userUnit, string message) {
        if(!logDecisions) {
            return;
        }

        GameDebugLogger.Ensure().Record(
            GameDebugSeverity.Trace,
            GameDebugCategory.Battle,
            $"{DisplayName}: {userUnit?.Pokemon?.Base?.Name ?? "AI"} chose {message}.",
            this,
            "BattleAIProfile",
            echoToUnity: false);
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }
}
