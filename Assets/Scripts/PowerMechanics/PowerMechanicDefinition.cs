using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PowerMechanicKind {
    MegaEvolution,
    ZMove,
    Dynamax,
    Gigantamax,
    Custom
}

public enum PowerMechanicSelectionMode {
    AttachToMove,
    SeparateAction,
    PassiveActivation
}

public enum PowerMechanicPokemonMatchMode {
    AnyConfiguredFilter,
    AllConfiguredFilters
}

[CreateAssetMenu(menuName = "Battle/Power Mechanics/Power Mechanic Definition")]
public class PowerMechanicDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this power mechanic. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in battle UI, debug logs and future menus. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation for this mechanic.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as mega, zmove, alola, gym, champion, frontier or reward.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Optional icon used by future battle UI.")]
    [SerializeField] Sprite icon;

    [Header("Type")]
    [Tooltip("Broad mechanic family. Battle rules can allow or block these kinds.")]
    [SerializeField] PowerMechanicKind kind = PowerMechanicKind.MegaEvolution;
    [Tooltip("How the player chooses this mechanic in battle.")]
    [SerializeField] PowerMechanicSelectionMode selectionMode = PowerMechanicSelectionMode.AttachToMove;
    [Tooltip("If enabled, player-side usage is allowed when other requirements pass.")]
    [SerializeField] bool playerCanUse = true;
    [Tooltip("If enabled, trainer/NPC-side usage is allowed when battle rules pass.")]
    [SerializeField] bool opponentCanUse = true;

    [Header("Unlock")]
    [Tooltip("If enabled, the player can use this without a PlayerPowerMechanicLog unlock record.")]
    [SerializeField] bool unlockedByDefault;
    [Tooltip("If enabled, player-side usage requires this mechanic to be unlocked in PlayerPowerMechanicLog.")]
    [SerializeField] bool requirePlayerUnlock = true;
    [Tooltip("Optional title, badge, permit or rank required to use this mechanic.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required to use this mechanic.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional current region required to use this mechanic. Useful for Alola Z-Move or Galar GMax rules.")]
    [SerializeField] WorldRegionDefinition requiredCurrentRegion;
    [Tooltip("Additional reusable activity requirements that must pass for player-side usage.")]
    [SerializeField] List<ActivityRequirement> extraRequirements = new List<ActivityRequirement>();
    [Tooltip("Fallback message shown when access checks fail without a more specific message.")]
    [SerializeField] string lockedMessage = "This power mechanic is not available.";

    [Header("Pokemon Requirements")]
    [Tooltip("How species/type/move filters are combined.")]
    [SerializeField] PowerMechanicPokemonMatchMode pokemonMatchMode = PowerMechanicPokemonMatchMode.AnyConfiguredFilter;
    [Tooltip("Exact Pokemon species that can use this mechanic.")]
    [SerializeField] List<PokemonBase> allowedPokemon = new List<PokemonBase>();
    [Tooltip("Pokemon types that can use this mechanic.")]
    [SerializeField] List<PokemonType> allowedTypes = new List<PokemonType>();
    [Tooltip("Optional move required on the Pokemon.")]
    [SerializeField] MoveBase requiredKnownMove;
    [Tooltip("Optional held item required on the Pokemon, such as a mega stone.")]
    [SerializeField] ItemBase requiredHeldItem;
    [Tooltip("Optional inventory item required on the trainer, such as a key stone, crystal or band.")]
    [SerializeField] ItemBase requiredInventoryItem;
    [Tooltip("If enabled, the inventory item is consumed when the mechanic is used.")]
    [SerializeField] bool consumeInventoryItem;
    [Tooltip("Minimum Pokemon level required.")]
    [Min(1)]
    [SerializeField] int minimumLevel = 1;
    [Tooltip("Minimum friendship required.")]
    [Min(0)]
    [SerializeField] int minimumFriendship;
    [Tooltip("If enabled, fainted Pokemon cannot use this mechanic.")]
    [SerializeField] bool requireHealthyPokemon = true;

    [Header("Move Requirements")]
    [Tooltip("If enabled, this mechanic must be attached to an existing selected move.")]
    [SerializeField] bool requiresSelectedMove;
    [Tooltip("Optional selected move type required before this mechanic can be attached. None ignores type.")]
    [SerializeField] PokemonType requiredSelectedMoveType = PokemonType.None;
    [Tooltip("Optional replacement move used when this mechanic transforms a selected move, such as a Z-Move.")]
    [SerializeField] MoveBase replacementMove;
    [Tooltip("If enabled and Replacement Move is assigned, the original selected move loses PP before the replacement move runs.")]
    [SerializeField] bool consumeSelectedMovePP = true;

    [Header("Battle Rules")]
    [Tooltip("If enabled, the current BattleRuleSetDefinition must allow this mechanic.")]
    [SerializeField] bool requireBattleRulePermission = true;
    [Tooltip("If enabled, this mechanic can be used when no battle rule context is active.")]
    [SerializeField] bool allowWithoutBattleRule = true;
    [Tooltip("Rule sets that explicitly allow this mechanic. Empty means no exact allow-list is enforced here.")]
    [SerializeField] List<BattleRuleSetDefinition> allowedRuleSets = new List<BattleRuleSetDefinition>();
    [Tooltip("Rule sets that explicitly block this mechanic.")]
    [SerializeField] List<BattleRuleSetDefinition> bannedRuleSets = new List<BattleRuleSetDefinition>();
    [Tooltip("Battle rule categories that allow this mechanic. Empty means no category allow-list is enforced here.")]
    [SerializeField] List<BattleRuleCategory> allowedBattleCategories = new List<BattleRuleCategory>();

    [Header("Trainer Charge")]
    [Tooltip("If enabled, player-side usage consumes the trainer's power mechanic charge.")]
    [SerializeField] bool consumesTrainerCharge = true;
    [Tooltip("Charge amount consumed from the trainer log.")]
    [Min(1)]
    [SerializeField] int trainerChargeCost = 1;
    [Tooltip("Cooldown in in-game hours before this charge group can be used again. 0 disables cooldown.")]
    [Min(0)]
    [SerializeField] int cooldownHours;
    [Tooltip("If enabled, charge is shared by mechanic kind, such as all Mega Evolutions sharing one Mega charge.")]
    [SerializeField] bool shareChargeByKind = true;
    [Tooltip("Optional custom charge group id. Empty uses mechanic kind or mechanic id depending on Share Charge By Kind.")]
    [SerializeField] string chargeGroupId = string.Empty;

    [Header("Runtime Effect")]
    [Tooltip("Temporary Pokemon base applied while the mechanic is active. Mega Evolution should assign this.")]
    [SerializeField] PokemonBase temporaryPokemonBase;
    [Tooltip("If enabled, current HP ratio is preserved when applying or removing the temporary base.")]
    [SerializeField] bool preserveHpRatioOnFormChange = true;
    [Tooltip("Number of this Pokemon's turns before the effect expires. 0 means it lasts until battle end.")]
    [Min(0)]
    [SerializeField] int durationTurns;
    [Tooltip("If enabled, this mechanic ends when the Pokemon switches out.")]
    [SerializeField] bool endsOnSwitch;
    [Tooltip("Temporary stat modifiers active while the mechanic is active.")]
    [SerializeField] List<PowerMechanicStatModifier> statModifiers = new List<PowerMechanicStatModifier>();
    [Tooltip("Stat stage boosts applied once when the mechanic activates.")]
    [SerializeField] List<StatBoosts> activationStatBoosts = new List<StatBoosts>();
    [Tooltip("Multiplier applied to move base power while the mechanic is active.")]
    [Min(0f)]
    [SerializeField] float movePowerMultiplier = 1f;
    [Tooltip("Multiplier applied to move accuracy while the mechanic is active.")]
    [Min(0f)]
    [SerializeField] float accuracyMultiplier = 1f;
    [Tooltip("Critical stage bonus while the mechanic is active.")]
    [SerializeField] int critStageBonus;

    [Header("Events")]
    [Tooltip("Optional event published when this mechanic activates.")]
    [SerializeField] GameEventDefinition activatedEvent;
    [Tooltip("Optional event published when this mechanic is blocked.")]
    [SerializeField] GameEventDefinition blockedEvent;
    [Tooltip("Optional event published when this mechanic expires.")]
    [SerializeField] GameEventDefinition expiredEvent;
    [Tooltip("If enabled, mechanic events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, mechanic events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public Sprite Icon => icon;
    public PowerMechanicKind Kind => kind;
    public PowerMechanicSelectionMode SelectionMode => selectionMode;
    public bool PlayerCanUse => playerCanUse;
    public bool OpponentCanUse => opponentCanUse;
    public bool UnlockedByDefault => unlockedByDefault;
    public bool RequirePlayerUnlock => requirePlayerUnlock;
    public TitleDefinition RequiredTitle => requiredTitle;
    public MilestoneDefinition RequiredMilestone => requiredMilestone;
    public WorldRegionDefinition RequiredCurrentRegion => requiredCurrentRegion;
    public IReadOnlyList<ActivityRequirement> ExtraRequirements => extraRequirements != null ? (IReadOnlyList<ActivityRequirement>)extraRequirements : Array.Empty<ActivityRequirement>();
    public string LockedMessage => lockedMessage;
    public PowerMechanicPokemonMatchMode PokemonMatchMode => pokemonMatchMode;
    public IReadOnlyList<PokemonBase> AllowedPokemon => allowedPokemon != null ? (IReadOnlyList<PokemonBase>)allowedPokemon : Array.Empty<PokemonBase>();
    public IReadOnlyList<PokemonType> AllowedTypes => allowedTypes != null ? (IReadOnlyList<PokemonType>)allowedTypes : Array.Empty<PokemonType>();
    public MoveBase RequiredKnownMove => requiredKnownMove;
    public ItemBase RequiredHeldItem => requiredHeldItem;
    public ItemBase RequiredInventoryItem => requiredInventoryItem;
    public bool ConsumeInventoryItem => consumeInventoryItem;
    public int MinimumLevel => Mathf.Max(1, minimumLevel);
    public int MinimumFriendship => Mathf.Max(0, minimumFriendship);
    public bool RequireHealthyPokemon => requireHealthyPokemon;
    public bool RequiresSelectedMove => requiresSelectedMove;
    public PokemonType RequiredSelectedMoveType => requiredSelectedMoveType;
    public MoveBase ReplacementMove => replacementMove;
    public bool ConsumeSelectedMovePP => consumeSelectedMovePP;
    public bool RequireBattleRulePermission => requireBattleRulePermission;
    public bool AllowWithoutBattleRule => allowWithoutBattleRule;
    public IReadOnlyList<BattleRuleSetDefinition> AllowedRuleSets => allowedRuleSets != null ? (IReadOnlyList<BattleRuleSetDefinition>)allowedRuleSets : Array.Empty<BattleRuleSetDefinition>();
    public IReadOnlyList<BattleRuleSetDefinition> BannedRuleSets => bannedRuleSets != null ? (IReadOnlyList<BattleRuleSetDefinition>)bannedRuleSets : Array.Empty<BattleRuleSetDefinition>();
    public IReadOnlyList<BattleRuleCategory> AllowedBattleCategories => allowedBattleCategories != null ? (IReadOnlyList<BattleRuleCategory>)allowedBattleCategories : Array.Empty<BattleRuleCategory>();
    public bool ConsumesTrainerCharge => consumesTrainerCharge;
    public int TrainerChargeCost => Mathf.Max(1, trainerChargeCost);
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public bool ShareChargeByKind => shareChargeByKind;
    public string ChargeGroupKey => !string.IsNullOrWhiteSpace(chargeGroupId) ? chargeGroupId : shareChargeByKind ? $"kind:{kind}" : $"mechanic:{Id}";
    public PokemonBase TemporaryPokemonBase => temporaryPokemonBase;
    public bool PreserveHpRatioOnFormChange => preserveHpRatioOnFormChange;
    public int DurationTurns => Mathf.Max(0, durationTurns);
    public bool EndsOnSwitch => endsOnSwitch;
    public IReadOnlyList<PowerMechanicStatModifier> StatModifiers => statModifiers != null ? (IReadOnlyList<PowerMechanicStatModifier>)statModifiers : Array.Empty<PowerMechanicStatModifier>();
    public IReadOnlyList<StatBoosts> ActivationStatBoosts => activationStatBoosts != null ? (IReadOnlyList<StatBoosts>)activationStatBoosts : Array.Empty<StatBoosts>();
    public float MovePowerMultiplier => Mathf.Max(0f, movePowerMultiplier);
    public float AccuracyMultiplier => Mathf.Max(0f, accuracyMultiplier);
    public int CritStageBonus => critStageBonus;
    public GameEventDefinition ActivatedEvent => activatedEvent;
    public GameEventDefinition BlockedEvent => blockedEvent;
    public GameEventDefinition ExpiredEvent => expiredEvent;
    public bool ShowEventsInFeed => showEventsInFeed;
    public bool WriteEventsToDebugLog => writeEventsToDebugLog;

    public bool CanUse(PowerMechanicUseContext context, out string failureMessage) {
        if(context == null) {
            failureMessage = "Power mechanic context is missing.";
            return false;
        }

        if(context.UserPokemon == null) {
            failureMessage = "No Pokemon selected for this power mechanic.";
            return false;
        }

        if(context.IsPlayerSide && !playerCanUse) {
            failureMessage = "The player cannot use this power mechanic.";
            return false;
        }

        if(!context.IsPlayerSide && !opponentCanUse) {
            failureMessage = "The opponent cannot use this power mechanic.";
            return false;
        }

        if(!CanAccess(context, out failureMessage)) {
            return false;
        }

        if(!CanUsePokemon(context.UserPokemon, out failureMessage)) {
            return false;
        }

        if(!CanUseSelectedMove(context.SelectedMove, out failureMessage)) {
            return false;
        }

        if(!CanUseBattleRule(context, out failureMessage)) {
            return false;
        }

        if(context.IsPlayerSide && context.PlayerLog != null && !context.PlayerLog.CanSpendCharge(this, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool CanUsePokemon(Pokemon pokemon, out string failureMessage) {
        if(pokemon == null || pokemon.Base == null) {
            failureMessage = "Pokemon data is missing.";
            return false;
        }

        if(requireHealthyPokemon && pokemon.HP <= 0) {
            failureMessage = $"{pokemon.NickName} cannot use {DisplayName}.";
            return false;
        }

        if(pokemon.Level < MinimumLevel) {
            failureMessage = $"{pokemon.NickName} must be at least level {MinimumLevel}.";
            return false;
        }

        if(pokemon.Friendship < MinimumFriendship) {
            failureMessage = $"{pokemon.NickName} does not have enough friendship for {DisplayName}.";
            return false;
        }

        if(requiredHeldItem != null && pokemon.HeldItem != requiredHeldItem) {
            failureMessage = $"{pokemon.NickName} must hold {requiredHeldItem.Name}.";
            return false;
        }

        if(!HasPokemonFilter()) {
            failureMessage = null;
            return true;
        }

        bool speciesMatch = allowedPokemon != null && allowedPokemon.Any(species => species != null && species == pokemon.Base);
        bool typeMatch = allowedTypes != null && allowedTypes.Any(type => type != PokemonType.None && pokemon.HasType(type));
        bool moveMatch = requiredKnownMove != null && pokemon.HasMove(requiredKnownMove);

        if(pokemonMatchMode == PowerMechanicPokemonMatchMode.AllConfiguredFilters) {
            bool speciesPass = allowedPokemon == null || allowedPokemon.Count == 0 || speciesMatch;
            bool typePass = allowedTypes == null || allowedTypes.Count == 0 || typeMatch;
            bool movePass = requiredKnownMove == null || moveMatch;
            if(speciesPass && typePass && movePass) {
                failureMessage = null;
                return true;
            }
        } else if(speciesMatch || typeMatch || moveMatch) {
            failureMessage = null;
            return true;
        }

        failureMessage = $"{pokemon.NickName} cannot use {DisplayName}.";
        return false;
    }

    public bool CanUseSelectedMove(Move selectedMove, out string failureMessage) {
        if(requiresSelectedMove && selectedMove == null) {
            failureMessage = $"{DisplayName} must be attached to a selected move.";
            return false;
        }

        if(requiredSelectedMoveType != PokemonType.None && (selectedMove == null || selectedMove.Base == null || selectedMove.Base.Type != requiredSelectedMoveType)) {
            failureMessage = $"{DisplayName} requires a {requiredSelectedMoveType} move.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public Move ResolveBattleMove(Move selectedMove) {
        if(replacementMove == null) {
            return selectedMove;
        }

        return new Move(replacementMove);
    }

    public BattlePowerMechanicRuntimeEffect CreateRuntimeEffect() {
        return new BattlePowerMechanicRuntimeEffect(this);
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasPokemonFilter() {
        return (allowedPokemon != null && allowedPokemon.Any(entry => entry != null))
            || (allowedTypes != null && allowedTypes.Any(type => type != PokemonType.None))
            || requiredKnownMove != null;
    }

    bool CanAccess(PowerMechanicUseContext context, out string failureMessage) {
        if(context.IsPlayerSide) {
            if(requirePlayerUnlock && !unlockedByDefault && (context.PlayerLog == null || !context.PlayerLog.HasUnlocked(this))) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not unlocked." : lockedMessage;
                return false;
            }

            if(requiredTitle != null && !(context.Player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
                return false;
            }

            if(requiredMilestone != null && !(context.Player?.GetComponent<PlayerMilestones>()?.HasMilestone(requiredMilestone) ?? false)) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredMilestone.DisplayName} first." : lockedMessage;
                return false;
            }

            if(requiredCurrentRegion != null) {
                var regionLog = context.Player != null ? context.Player.GetComponent<PlayerWorldRegionLog>() : null;
                if(regionLog == null || !regionLog.IsCurrentRegion(requiredCurrentRegion)) {
                    failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} requires {requiredCurrentRegion.DisplayName}." : lockedMessage;
                    return false;
                }
            }

            if(requiredInventoryItem != null) {
                var inventory = Inventory.GetInventory();
                if(inventory == null || !inventory.HasItemEnough(requiredInventoryItem, 1)) {
                    failureMessage = $"You need {requiredInventoryItem.Name} to use {DisplayName}.";
                    return false;
                }
            }

            foreach(var requirement in ExtraRequirements) {
                if(requirement != null && !requirement.IsMet(context.Player)) {
                    failureMessage = string.IsNullOrWhiteSpace(requirement.FailureMessage) ? lockedMessage : requirement.FailureMessage;
                    return false;
                }
            }
        }

        failureMessage = null;
        return true;
    }

    bool CanUseBattleRule(PowerMechanicUseContext context, out string failureMessage) {
        var ruleSet = context.BattleSystem != null && context.BattleSystem.RuleContext != null
            ? context.BattleSystem.RuleContext.RuleSet
            : null;

        if(ruleSet == null) {
            if(allowWithoutBattleRule) {
                failureMessage = null;
                return true;
            }

            failureMessage = $"{DisplayName} requires an active battle rule.";
            return false;
        }

        if(bannedRuleSets != null && bannedRuleSets.Contains(ruleSet)) {
            failureMessage = $"{DisplayName} is banned by {ruleSet.DisplayName}.";
            return false;
        }

        if(allowedRuleSets != null && allowedRuleSets.Count > 0 && !allowedRuleSets.Contains(ruleSet)) {
            failureMessage = $"{DisplayName} is not allowed by this rule set.";
            return false;
        }

        if(allowedBattleCategories != null && allowedBattleCategories.Count > 0 && !allowedBattleCategories.Contains(ruleSet.Category)) {
            failureMessage = $"{DisplayName} is not allowed in {ruleSet.Category} battles.";
            return false;
        }

        if(requireBattleRulePermission && context.BattleSystem != null) {
            return context.BattleSystem.CanUsePowerMechanicByRule(context.IsPlayerSide, this, out failureMessage);
        }

        failureMessage = null;
        return true;
    }
}

[Serializable]
public class PowerMechanicStatModifier {
    [Tooltip("Stat affected by this runtime modifier.")]
    public Stat stat = Stat.Attack;
    [Tooltip("Flat value added after base stat, nature and normal stat stages.")]
    public int flatBonus;
    [Tooltip("Multiplier applied after flat bonus. 1 means unchanged.")]
    [Min(0f)]
    public float multiplier = 1f;

    public int Apply(Stat targetStat, int value) {
        if(stat != targetStat) {
            return value;
        }

        return Mathf.Max(1, Mathf.FloorToInt((value + flatBonus) * Mathf.Max(0f, multiplier)));
    }
}

public class PowerMechanicUseContext {
    public PlayerController Player { get; }
    public PlayerPowerMechanicLog PlayerLog { get; }
    public BattleSystem BattleSystem { get; }
    public BattleUnit UserUnit { get; }
    public BattleUnit TargetUnit { get; }
    public Pokemon UserPokemon { get; }
    public Move SelectedMove { get; }
    public bool IsPlayerSide { get; }
    public string SourceId { get; }

    public PowerMechanicUseContext(PlayerController player, BattleSystem battleSystem, BattleUnit userUnit, BattleUnit targetUnit, Move selectedMove, bool isPlayerSide, string sourceId) {
        Player = player;
        PlayerLog = player != null ? player.GetComponent<PlayerPowerMechanicLog>() : null;
        BattleSystem = battleSystem;
        UserUnit = userUnit;
        TargetUnit = targetUnit;
        UserPokemon = userUnit != null ? userUnit.Pokemon : null;
        SelectedMove = selectedMove;
        IsPlayerSide = isPlayerSide;
        SourceId = sourceId;
    }
}

[Serializable]
public class BattlePowerMechanicRuntimeEffect {
    [Tooltip("Saved mechanic id.")]
    public string mechanicId;
    [Tooltip("Saved mechanic display name for debug/status messages.")]
    public string mechanicName;
    [Tooltip("Saved mechanic kind.")]
    public PowerMechanicKind kind;
    [Tooltip("Temporary Pokemon base applied by this effect.")]
    public PokemonBase temporaryPokemonBase;
    [Tooltip("If enabled, HP ratio is preserved when the temporary form is applied or removed.")]
    public bool preserveHpRatioOnFormChange;
    [Tooltip("Remaining Pokemon turns before expiration. 0 means battle-end duration.")]
    public int remainingTurns;
    [Tooltip("If enabled, this effect ends when the Pokemon switches out.")]
    public bool endsOnSwitch;
    [Tooltip("Runtime stat modifiers active while this effect remains.")]
    public List<PowerMechanicStatModifier> statModifiers = new List<PowerMechanicStatModifier>();
    [Tooltip("Move power multiplier active while this effect remains.")]
    public float movePowerMultiplier = 1f;
    [Tooltip("Move accuracy multiplier active while this effect remains.")]
    public float accuracyMultiplier = 1f;
    [Tooltip("Critical stage bonus active while this effect remains.")]
    public int critStageBonus;

    public bool HasTurnDuration => remainingTurns > 0;

    public BattlePowerMechanicRuntimeEffect() {
    }

    public BattlePowerMechanicRuntimeEffect(PowerMechanicDefinition definition) {
        if(definition == null) {
            return;
        }

        mechanicId = definition.Id;
        mechanicName = definition.DisplayName;
        kind = definition.Kind;
        temporaryPokemonBase = definition.TemporaryPokemonBase;
        preserveHpRatioOnFormChange = definition.PreserveHpRatioOnFormChange;
        remainingTurns = definition.DurationTurns;
        endsOnSwitch = definition.EndsOnSwitch;
        statModifiers = definition.StatModifiers.Where(modifier => modifier != null).ToList();
        movePowerMultiplier = definition.MovePowerMultiplier;
        accuracyMultiplier = definition.AccuracyMultiplier;
        critStageBonus = definition.CritStageBonus;
    }

    public bool TickTurn() {
        if(remainingTurns <= 0) {
            return false;
        }

        remainingTurns--;
        return remainingTurns == 0;
    }
}
