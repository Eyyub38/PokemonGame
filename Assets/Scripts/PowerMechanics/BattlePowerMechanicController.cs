using UnityEngine;

public class BattlePowerMechanicController : MonoBehaviour {
    [Tooltip("Battle system controlled by this power mechanic controller. Empty uses this GameObject or BattleSystem.i.")]
    [SerializeField] BattleSystem battleSystemOverride;
    [Tooltip("If enabled, successful and blocked uses are written to GameDebug.")]
    [SerializeField] bool writeDebugLogs;

    BattleSystem battleSystem;

    public bool TryUse(PowerMechanicDefinition mechanic, BattleUnit userUnit, BattleUnit targetUnit, Move selectedMove, bool isPlayerSide, string sourceId, out string failureMessage) {
        battleSystem = ResolveBattleSystem();
        var player = ResolvePlayer();
        var context = new PowerMechanicUseContext(player, battleSystem, userUnit, targetUnit, selectedMove, isPlayerSide, sourceId);

        if(mechanic == null) {
            failureMessage = "Power mechanic is missing.";
            return false;
        }

        if(!mechanic.CanUse(context, out failureMessage)) {
            context.PlayerLog?.RecordUse(mechanic, context.UserPokemon, GetRuleId(), sourceId, blocked: true, failureMessage);
            PublishMechanicEvent(mechanic, context.UserPokemon, "blocked", failureMessage, mechanic.BlockedEvent, GameEventImportance.Warning, sourceId);
            WriteDebug($"Power mechanic blocked: {mechanic.DisplayName} - {failureMessage}", warning: true);
            return false;
        }

        ApplyMechanic(mechanic, userUnit);
        if(isPlayerSide && mechanic.ConsumeInventoryItem && mechanic.RequiredInventoryItem != null) {
            Inventory.GetInventory()?.RemoveItem(mechanic.RequiredInventoryItem, 1);
        }

        battleSystem?.RecordPowerMechanicUse(isPlayerSide, mechanic);
        context.PlayerLog?.RecordUse(mechanic, context.UserPokemon, GetRuleId(), sourceId, blocked: false);
        PublishMechanicEvent(mechanic, context.UserPokemon, "activated", $"{context.UserPokemon.NickName} used {mechanic.DisplayName}.", mechanic.ActivatedEvent, GameEventImportance.Success, sourceId);
        WriteDebug($"Power mechanic activated: {mechanic.DisplayName}");
        failureMessage = null;
        return true;
    }

    void ApplyMechanic(PowerMechanicDefinition mechanic, BattleUnit userUnit) {
        if(mechanic == null || userUnit == null || userUnit.Pokemon == null) {
            return;
        }

        userUnit.Pokemon.ApplyPowerMechanicEffect(mechanic.CreateRuntimeEffect());

        if(mechanic.ActivationStatBoosts != null && mechanic.ActivationStatBoosts.Count > 0) {
            userUnit.Pokemon.ApplyBoosts(new System.Collections.Generic.List<StatBoosts>(mechanic.ActivationStatBoosts), userUnit.Pokemon);
        }

        userUnit.RefreshPokemonVisual();
    }

    BattleSystem ResolveBattleSystem() {
        if(battleSystemOverride != null) {
            return battleSystemOverride;
        }

        battleSystemOverride = GetComponent<BattleSystem>();
        if(battleSystemOverride == null) {
            battleSystemOverride = BattleSystem.i != null ? BattleSystem.i : FindAnyObjectByType<BattleSystem>();
        }

        return battleSystemOverride;
    }

    PlayerController ResolvePlayer() {
        var system = ResolveBattleSystem();
        if(system != null && system.PlayerParty != null) {
            var player = system.PlayerParty.GetComponent<PlayerController>();
            if(player != null) {
                return player;
            }
        }

        return PlayerController.i != null ? PlayerController.i : FindAnyObjectByType<PlayerController>();
    }

    string GetRuleId() {
        var system = ResolveBattleSystem();
        return system != null && system.RuleContext != null && system.RuleContext.RuleSet != null
            ? system.RuleContext.RuleSet.Id
            : string.Empty;
    }

    void PublishMechanicEvent(PowerMechanicDefinition mechanic, Pokemon pokemon, string phase, string message, GameEventDefinition eventDefinition, GameEventImportance importance, string sourceId) {
        if(mechanic == null) {
            return;
        }

        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"power-mechanic.{phase}.{mechanic.Id}",
            message,
            GameEventCategory.Battle,
            importance,
            this,
            "BattlePowerMechanicController",
            GameEventScope.Battle,
            showInFeed: mechanic.ShowEventsInFeed,
            writeToDebugLog: mechanic.WriteEventsToDebugLog,
            GameEventPublishing.Value("mechanicId", mechanic.Id),
            GameEventPublishing.Value("mechanicName", mechanic.DisplayName),
            GameEventPublishing.Value("kind", mechanic.Kind),
            GameEventPublishing.Value("pokemonInstanceId", pokemon != null ? pokemon.InstanceId : string.Empty),
            GameEventPublishing.Value("pokemonName", pokemon != null ? pokemon.NickName : string.Empty),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", sourceId));
    }

    void WriteDebug(string message, bool warning = false) {
        if(!writeDebugLogs) {
            return;
        }

        if(warning) {
            GameDebug.Warning(message, GameDebugCategory.Battle, this, "BattlePowerMechanicController");
        } else {
            GameDebug.Step(message, GameDebugCategory.Battle, this, "BattlePowerMechanicController");
        }
    }
}
