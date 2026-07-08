using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BattleRuleCategory {
    Casual,
    Trainer,
    Tournament,
    Gym,
    Contest,
    Research,
    Police,
    Club,
    Custom
}

public enum BattleRuleItemRule {
    Allowed,
    PlayerForbidden,
    BothForbidden,
    LimitedCount
}

public enum BattleRuleSwitchRule {
    Allowed,
    PlayerForbidden,
    BothForbidden,
    LimitedCount
}

public enum BattleRulePowerMechanicRule {
    Allowed,
    PlayerForbidden,
    OpponentForbidden,
    BothForbidden,
    LimitedCount
}

public enum BattleRuleWinCondition {
    StandardDefeatAll,
    TurnLimitScore,
    SurviveTurns,
    CaptureOnly,
    Custom
}

public enum BattleRuleTurnLimitOutcome {
    ContinueBattle,
    PlayerWins,
    PlayerLoses
}

[CreateAssetMenu(menuName = "Battle Rules/Rule Set Definition")]
public class BattleRuleSetDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this battle rule set. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in future challenge/rule selection UI. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer or player-facing explanation of this rule set.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad category used by filters, validation and future UI styling.")]
    [SerializeField] BattleRuleCategory category = BattleRuleCategory.Casual;
    [Tooltip("Free-form tags used by dialog, activities and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Party Size")]
    [Tooltip("Minimum number of valid Pokemon required in the party. 0 disables this check.")]
    [Min(0)]
    [SerializeField] int minPokemon;
    [Tooltip("Maximum number of valid Pokemon allowed in the party. 0 disables this check.")]
    [Min(0)]
    [SerializeField] int maxPokemon;
    [Tooltip("Exact number of valid Pokemon required. 0 disables this check and uses min/max instead.")]
    [Min(0)]
    [SerializeField] int exactPokemon;
    [Tooltip("If enabled, only Pokemon with HP above 0 count toward size and level rules.")]
    [SerializeField] bool countOnlyHealthyPokemon = true;

    [Header("Pokemon Rules")]
    [Tooltip("Allowed Pokemon types. Empty means every type is allowed unless banned below.")]
    [SerializeField] List<PokemonType> allowedTypes = new List<PokemonType>();
    [Tooltip("Pokemon types that are always blocked by this rule set.")]
    [SerializeField] List<PokemonType> bannedTypes = new List<PokemonType>();
    [Tooltip("If enabled, a dual-type Pokemon passes allowed type rules when either type matches.")]
    [SerializeField] bool allowDualTypeIfAnyAllowedTypeMatches = true;
    [Tooltip("Minimum Pokemon level allowed. 0 disables the lower level check.")]
    [Min(0)]
    [SerializeField] int minLevel;
    [Tooltip("Maximum Pokemon level allowed. 0 disables the upper level check.")]
    [Min(0)]
    [SerializeField] int maxLevel;
    [Tooltip("Species that must be present in the party for this rule set.")]
    [SerializeField] List<PokemonBase> requiredPokemon = new List<PokemonBase>();
    [Tooltip("Species that cannot be used with this rule set.")]
    [SerializeField] List<PokemonBase> bannedPokemon = new List<PokemonBase>();

    [Header("Battle Limits")]
    [Tooltip("Optional vital profile used by this rule set for battle HP cap, stamina max/cost percentage and core-health damage. Empty uses move profile/default formulas.")]
    [SerializeField] PokemonVitalProfileDefinition vitalProfile;
    [Tooltip("If enabled, Pokemon entering battle under this rule spend core stamina to refill battle stamina.")]
    [SerializeField] bool spendCoreStaminaOnBattleEntry = true;
    [Tooltip("If enabled, battle HP is capped by the Pokemon's core health ratio under this rule set.")]
    [SerializeField] bool capBattleHpByCoreHealth = true;
    [Tooltip("How item usage is restricted during battle.")]
    [SerializeField] BattleRuleItemRule itemRule = BattleRuleItemRule.Allowed;
    [Tooltip("Maximum player item uses when Item Rule is Limited Count.")]
    [Min(0)]
    [SerializeField] int maxPlayerItemUses;
    [Tooltip("How voluntary switching is restricted during battle.")]
    [SerializeField] BattleRuleSwitchRule switchRule = BattleRuleSwitchRule.Allowed;
    [Tooltip("Maximum player voluntary switches when Switch Rule is Limited Count.")]
    [Min(0)]
    [SerializeField] int maxPlayerSwitches;
    [Tooltip("Maximum number of full turns before the rule set is considered time-limited. 0 disables this limit.")]
    [Min(0)]
    [SerializeField] int turnLimit;
    [Tooltip("What happens when Turn Limit is reached. Continue Battle only reports the limit to scripts/UI.")]
    [SerializeField] BattleRuleTurnLimitOutcome turnLimitOutcome = BattleRuleTurnLimitOutcome.ContinueBattle;
    [Tooltip("Maximum real-time seconds for future timer UI. 0 means no time limit is enforced by scripts yet.")]
    [Min(0)]
    [SerializeField] int secondsLimit;
    [Tooltip("Win condition metadata used by future scoring and UI.")]
    [SerializeField] BattleRuleWinCondition winCondition = BattleRuleWinCondition.StandardDefeatAll;
    [Tooltip("If disabled, the player cannot run away while this rule set is active.")]
    [SerializeField] bool allowRun = true;
    [Tooltip("If disabled, Pokeball capture attempts are blocked while this rule set is active.")]
    [SerializeField] bool allowCapture = true;

    [Header("Power Mechanics")]
    [Tooltip("How Mega Evolution, Z-Move, Dynamax, Gigantamax and custom mechanics are restricted.")]
    [SerializeField] BattleRulePowerMechanicRule powerMechanicRule = BattleRulePowerMechanicRule.Allowed;
    [Tooltip("Maximum player power mechanic uses when Power Mechanic Rule is Limited Count. 0 means no player uses.")]
    [Min(0)]
    [SerializeField] int maxPlayerPowerMechanicUses = 1;
    [Tooltip("Maximum opponent power mechanic uses when Power Mechanic Rule is Limited Count. 0 means no opponent uses.")]
    [Min(0)]
    [SerializeField] int maxOpponentPowerMechanicUses = 1;
    [Tooltip("Maximum player uses per mechanic kind. 0 disables this per-kind limit.")]
    [Min(0)]
    [SerializeField] int maxPlayerPowerMechanicUsesPerKind = 1;
    [Tooltip("Maximum opponent uses per mechanic kind. 0 disables this per-kind limit.")]
    [Min(0)]
    [SerializeField] int maxOpponentPowerMechanicUsesPerKind = 1;
    [Tooltip("Power mechanic kinds allowed by this battle rule. Empty means every kind is allowed unless banned.")]
    [SerializeField] List<PowerMechanicKind> allowedPowerMechanicKinds = new List<PowerMechanicKind>();
    [Tooltip("Power mechanic kinds banned by this battle rule.")]
    [SerializeField] List<PowerMechanicKind> bannedPowerMechanicKinds = new List<PowerMechanicKind>();
    [Tooltip("Exact power mechanics allowed by this battle rule. Empty means every mechanic is allowed unless banned.")]
    [SerializeField] List<PowerMechanicDefinition> allowedPowerMechanics = new List<PowerMechanicDefinition>();
    [Tooltip("Exact power mechanics banned by this battle rule.")]
    [SerializeField] List<PowerMechanicDefinition> bannedPowerMechanics = new List<PowerMechanicDefinition>();

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this rule can be selected.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this rule can be selected.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this rule set.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum reputation required with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Message shown when access or validation fails and no more specific reason exists.")]
    [SerializeField] string lockedMessage = "This battle rule is not available yet.";

    [Header("Events")]
    [Tooltip("Optional event published when this rule set is accepted for battle.")]
    [SerializeField] GameEventDefinition acceptedEvent;
    [Tooltip("Optional event published when this rule set blocks a challenge.")]
    [SerializeField] GameEventDefinition rejectedEvent;
    [Tooltip("Optional event published when a battle using this rule set ends.")]
    [SerializeField] GameEventDefinition completedEvent;
    [Tooltip("If enabled, rule events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, rule events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public BattleRuleCategory Category => category;
    public IReadOnlyList<string> Tags => tags;
    public int MinPokemon => Mathf.Max(0, minPokemon);
    public int MaxPokemon => Mathf.Max(0, maxPokemon);
    public int ExactPokemon => Mathf.Max(0, exactPokemon);
    public bool CountOnlyHealthyPokemon => countOnlyHealthyPokemon;
    public IReadOnlyList<PokemonType> AllowedTypes => allowedTypes;
    public IReadOnlyList<PokemonType> BannedTypes => bannedTypes;
    public int MinLevel => Mathf.Max(0, minLevel);
    public int MaxLevel => Mathf.Max(0, maxLevel);
    public IReadOnlyList<PokemonBase> RequiredPokemon => requiredPokemon;
    public IReadOnlyList<PokemonBase> BannedPokemon => bannedPokemon;
    public PokemonVitalProfileDefinition VitalProfile => vitalProfile;
    public bool SpendCoreStaminaOnBattleEntry => spendCoreStaminaOnBattleEntry;
    public bool CapBattleHpByCoreHealth => capBattleHpByCoreHealth;
    public BattleRuleItemRule ItemRule => itemRule;
    public int MaxPlayerItemUses => Mathf.Max(0, maxPlayerItemUses);
    public BattleRuleSwitchRule SwitchRule => switchRule;
    public int MaxPlayerSwitches => Mathf.Max(0, maxPlayerSwitches);
    public int TurnLimit => Mathf.Max(0, turnLimit);
    public BattleRuleTurnLimitOutcome TurnLimitOutcome => turnLimitOutcome;
    public int SecondsLimit => Mathf.Max(0, secondsLimit);
    public BattleRuleWinCondition WinCondition => winCondition;
    public bool AllowRun => allowRun;
    public bool AllowCapture => allowCapture;
    public BattleRulePowerMechanicRule PowerMechanicRule => powerMechanicRule;
    public int MaxPlayerPowerMechanicUses => Mathf.Max(0, maxPlayerPowerMechanicUses);
    public int MaxOpponentPowerMechanicUses => Mathf.Max(0, maxOpponentPowerMechanicUses);
    public int MaxPlayerPowerMechanicUsesPerKind => Mathf.Max(0, maxPlayerPowerMechanicUsesPerKind);
    public int MaxOpponentPowerMechanicUsesPerKind => Mathf.Max(0, maxOpponentPowerMechanicUsesPerKind);
    public IReadOnlyList<PowerMechanicKind> AllowedPowerMechanicKinds => allowedPowerMechanicKinds;
    public IReadOnlyList<PowerMechanicKind> BannedPowerMechanicKinds => bannedPowerMechanicKinds;
    public IReadOnlyList<PowerMechanicDefinition> AllowedPowerMechanics => allowedPowerMechanics;
    public IReadOnlyList<PowerMechanicDefinition> BannedPowerMechanics => bannedPowerMechanics;

    public bool CanAccess(PlayerController player, out string failureMessage) {
        if(requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredMilestone != null && !(player?.GetComponent<PlayerMilestones>()?.HasMilestone(requiredMilestone) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredMilestone.DisplayName} first." : lockedMessage;
            return false;
        }

        if(requiredFaction != null) {
            int reputation = player?.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0;
            if(reputation < requiredReputation) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more reputation with {requiredFaction.DisplayName}." : lockedMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public bool ValidateParty(PokemonParty party, out BattleRuleValidationReport report) {
        report = new BattleRuleValidationReport(this);
        if(party == null || party.Pokemons == null) {
            report.AddIssue("Party is missing.");
            return false;
        }

        var candidates = GetCountedPokemon(party).ToList();
        ValidatePartySize(candidates, report);
        ValidateRequiredPokemon(candidates, report);

        foreach(var pokemon in candidates) {
            ValidatePokemon(pokemon, report);
        }

        return report.IsValid;
    }

    public bool CanUsePokemon(Pokemon pokemon, out string failureMessage) {
        var report = new BattleRuleValidationReport(this);
        ValidatePokemon(pokemon, report);
        failureMessage = report.FirstIssue;
        return report.IsValid;
    }

    public bool CanUseItem(bool isPlayer, int usedCount, ItemBase item, out string failureMessage) {
        if(item is PokeballItem && !allowCapture) {
            failureMessage = "Capture attempts are not allowed under these rules.";
            return false;
        }

        if(itemRule == BattleRuleItemRule.BothForbidden || (isPlayer && itemRule == BattleRuleItemRule.PlayerForbidden)) {
            failureMessage = "Items are not allowed under these rules.";
            return false;
        }

        if(isPlayer && itemRule == BattleRuleItemRule.LimitedCount && usedCount >= MaxPlayerItemUses) {
            failureMessage = $"You have already used {MaxPlayerItemUses} item(s).";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool CanUsePowerMechanic(bool isPlayer, PowerMechanicDefinition mechanic, int totalUsedCount, int kindUsedCount, out string failureMessage) {
        if(mechanic == null) {
            failureMessage = "Power mechanic is missing.";
            return false;
        }

        if(powerMechanicRule == BattleRulePowerMechanicRule.BothForbidden
            || (isPlayer && powerMechanicRule == BattleRulePowerMechanicRule.PlayerForbidden)
            || (!isPlayer && powerMechanicRule == BattleRulePowerMechanicRule.OpponentForbidden)) {
            failureMessage = "Power mechanics are not allowed under these rules.";
            return false;
        }

        if(bannedPowerMechanics != null && bannedPowerMechanics.Contains(mechanic)) {
            failureMessage = $"{mechanic.DisplayName} is banned under these rules.";
            return false;
        }

        if(allowedPowerMechanics != null && allowedPowerMechanics.Count > 0 && !allowedPowerMechanics.Contains(mechanic)) {
            failureMessage = $"{mechanic.DisplayName} is not allowed under these rules.";
            return false;
        }

        if(bannedPowerMechanicKinds != null && bannedPowerMechanicKinds.Contains(mechanic.Kind)) {
            failureMessage = $"{mechanic.Kind} mechanics are banned under these rules.";
            return false;
        }

        if(allowedPowerMechanicKinds != null && allowedPowerMechanicKinds.Count > 0 && !allowedPowerMechanicKinds.Contains(mechanic.Kind)) {
            failureMessage = $"{mechanic.Kind} mechanics are not allowed under these rules.";
            return false;
        }

        if(powerMechanicRule == BattleRulePowerMechanicRule.LimitedCount) {
            int totalLimit = isPlayer ? MaxPlayerPowerMechanicUses : MaxOpponentPowerMechanicUses;
            if(totalUsedCount >= totalLimit) {
                failureMessage = $"Power mechanic use limit reached ({totalLimit}).";
                return false;
            }
        }

        int kindLimit = isPlayer ? MaxPlayerPowerMechanicUsesPerKind : MaxOpponentPowerMechanicUsesPerKind;
        if(kindLimit > 0 && kindUsedCount >= kindLimit) {
            failureMessage = $"{mechanic.Kind} use limit reached ({kindLimit}).";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool CanSwitch(bool isPlayer, int switchCount, out string failureMessage) {
        if(switchRule == BattleRuleSwitchRule.BothForbidden || (isPlayer && switchRule == BattleRuleSwitchRule.PlayerForbidden)) {
            failureMessage = "Switching is not allowed under these rules.";
            return false;
        }

        if(isPlayer && switchRule == BattleRuleSwitchRule.LimitedCount && switchCount >= MaxPlayerSwitches) {
            failureMessage = $"You have already switched {MaxPlayerSwitches} time(s).";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool CanRunAway(out string failureMessage) {
        if(allowRun) {
            failureMessage = null;
            return true;
        }

        failureMessage = "Running is not allowed under these rules.";
        return false;
    }

    public bool IsTurnLimitReached(int completedTurns) {
        return TurnLimit > 0 && completedTurns >= TurnLimit;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public void PublishAccepted(PlayerController player, string sourceId = null) {
        PublishRuleEvent(acceptedEvent, "accepted", $"{DisplayName} accepted.", GameEventImportance.Info, player, sourceId, won: null);
    }

    public void PublishRejected(PlayerController player, string reason, string sourceId = null) {
        PublishRuleEvent(rejectedEvent, "rejected", string.IsNullOrWhiteSpace(reason) ? $"{DisplayName} rejected." : reason, GameEventImportance.Warning, player, sourceId, won: null);
    }

    public void PublishCompleted(PlayerController player, bool won, string sourceId = null) {
        PublishRuleEvent(completedEvent, "completed", $"{DisplayName} completed.", won ? GameEventImportance.Success : GameEventImportance.Info, player, sourceId, won);
    }

    IEnumerable<Pokemon> GetCountedPokemon(PokemonParty party) {
        return (party?.Pokemons ?? new List<Pokemon>())
            .Where(p => p != null)
            .Where(p => !countOnlyHealthyPokemon || p.HP > 0);
    }

    void ValidatePartySize(List<Pokemon> candidates, BattleRuleValidationReport report) {
        int count = candidates.Count;
        if(ExactPokemon > 0 && count != ExactPokemon) {
            report.AddIssue($"This rule requires exactly {ExactPokemon} Pokemon.");
            return;
        }

        if(MinPokemon > 0 && count < MinPokemon) {
            report.AddIssue($"This rule requires at least {MinPokemon} Pokemon.");
        }

        if(MaxPokemon > 0 && count > MaxPokemon) {
            report.AddIssue($"This rule allows at most {MaxPokemon} Pokemon.");
        }
    }

    void ValidateRequiredPokemon(List<Pokemon> candidates, BattleRuleValidationReport report) {
        if(requiredPokemon == null) {
            return;
        }

        foreach(var required in requiredPokemon) {
            if(required != null && !candidates.Any(p => p.Base == required)) {
                report.AddIssue($"{required.Name} is required for this rule.");
            }
        }
    }

    void ValidatePokemon(Pokemon pokemon, BattleRuleValidationReport report) {
        if(pokemon == null || pokemon.Base == null) {
            report.AddIssue("A party slot has no Pokemon data.");
            return;
        }

        if(bannedPokemon != null && bannedPokemon.Contains(pokemon.Base)) {
            report.AddIssue($"{pokemon.Base.Name} is banned by this rule.");
        }

        if(MinLevel > 0 && pokemon.Level < MinLevel) {
            report.AddIssue($"{pokemon.Base.Name} is below level {MinLevel}.");
        }

        if(MaxLevel > 0 && pokemon.Level > MaxLevel) {
            report.AddIssue($"{pokemon.Base.Name} is above level {MaxLevel}.");
        }

        if(HasBannedType(pokemon.Base)) {
            report.AddIssue($"{pokemon.Base.Name} has a banned type.");
        }

        if(allowedTypes != null && allowedTypes.Count > 0 && !HasAllowedType(pokemon.Base)) {
            report.AddIssue($"{pokemon.Base.Name} does not match the allowed type list.");
        }
    }

    bool HasBannedType(PokemonBase pokemonBase) {
        if(bannedTypes == null || pokemonBase == null) {
            return false;
        }

        return bannedTypes.Contains(pokemonBase.Type1) || bannedTypes.Contains(pokemonBase.Type2);
    }

    bool HasAllowedType(PokemonBase pokemonBase) {
        if(allowedTypes == null || allowedTypes.Count == 0 || pokemonBase == null) {
            return true;
        }

        if(allowDualTypeIfAnyAllowedTypeMatches) {
            return allowedTypes.Contains(pokemonBase.Type1) || allowedTypes.Contains(pokemonBase.Type2);
        }

        bool type1Allowed = pokemonBase.Type1 == PokemonType.None || allowedTypes.Contains(pokemonBase.Type1);
        bool type2Allowed = pokemonBase.Type2 == PokemonType.None || allowedTypes.Contains(pokemonBase.Type2);
        return type1Allowed && type2Allowed;
    }

    void PublishRuleEvent(GameEventDefinition eventDefinition, string phase, string message, GameEventImportance importance, PlayerController player, string sourceId, bool? won) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"battle-rule.{phase}.{Id}",
            message,
            GameEventCategory.BattleRule,
            importance,
            player != null ? player : this,
            "BattleRuleSetDefinition",
            GameEventScope.Battle,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("ruleId", Id),
            GameEventPublishing.Value("ruleName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("won", won.HasValue ? won.Value.ToString() : string.Empty));
    }
}

[Serializable]
public class BattleRuleValidationReport {
    [Tooltip("Rule set that produced this validation report.")]
    public BattleRuleSetDefinition ruleSet;
    [Tooltip("Validation issues found while checking this rule set.")]
    public List<string> issues = new List<string>();

    public bool IsValid => issues == null || issues.Count == 0;
    public string FirstIssue => IsValid ? null : issues[0];

    public BattleRuleValidationReport(BattleRuleSetDefinition ruleSet) {
        this.ruleSet = ruleSet;
    }

    public void AddIssue(string issue) {
        if(string.IsNullOrWhiteSpace(issue)) {
            return;
        }

        issues.Add(issue);
    }
}
