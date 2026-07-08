using UnityEngine;

public enum EncounterStartResult {
    StartedBattle,
    Captured,
    NoEncounter,
    Blocked
}

public static class EncounterSystem {
    public static WeatherConditionID GetCurrentWeather() {
        var scene = GameController.i != null ? GameController.i.CurrentScene : null;
        var mapArea = scene != null ? scene.GetComponent<MapArea>() : null;
        return mapArea != null ? mapArea.Weather : WeatherConditionID.None;
    }

    public static bool TryRoll(
        PlayerController player,
        EncounterTableDefinition table,
        EncounterSourceType sourceType,
        float chanceMultiplier,
        UnityEngine.Object context,
        out Pokemon pokemon,
        out EncounterTableEntry entry,
        bool applyWorldConditionEncounterRate = true
    ) {
        pokemon = null;
        entry = null;

        if(table == null) {
            Publish("encounter.blocked.no-table", "Encounter table is missing.", GameEventImportance.Warning, player, context, sourceType, null, null, showInFeed: false);
            return false;
        }

        float effectiveChanceMultiplier = applyWorldConditionEncounterRate
            ? PlayerActivityContext.ModifyEncounterRateMultiplier(chanceMultiplier, player)
            : chanceMultiplier;

        if(!table.ShouldAttempt(effectiveChanceMultiplier)) {
            PublishNoEncounter(player, table, sourceType, context, "Encounter chance failed.");
            return false;
        }

        if(!table.RollPokemon(player, out pokemon, out entry)) {
            PublishNoEncounter(player, table, sourceType, context, table.NoValidEncounterMessage);
            return false;
        }

        return pokemon != null;
    }

    public static EncounterStartResult StartBattle(
        PlayerController player,
        Pokemon pokemon,
        EncounterSourceType sourceType,
        EncounterTableDefinition table,
        BattleTrigger battleTrigger,
        UnityEngine.Object context
    ) {
        if(player == null || pokemon == null || GameController.i == null) {
            Publish("encounter.blocked.no-player-or-pokemon", "Encounter could not start a battle.", GameEventImportance.Warning, player, context, sourceType, table, pokemon, showInFeed: false);
            return EncounterStartResult.Blocked;
        }

        var log = player.GetComponent<PlayerEncounterLog>();
        log?.RecordSeen(pokemon, sourceType, table);
        log?.RecordBattleStarted(pokemon, sourceType, table);

        Publish(
            $"encounter.started.{pokemon.Base.name}",
            $"A wild {pokemon.Base.Name} appeared.",
            GameEventImportance.Success,
            player,
            context,
            sourceType,
            table,
            pokemon,
            showInFeed: table == null || table.ShowEventsInFeed);

        GameController.i.StartWildBattle(pokemon, battleTrigger);
        return EncounterStartResult.StartedBattle;
    }

    public static EncounterStartResult TryStealthCapture(
        PlayerController player,
        Pokemon pokemon,
        EncounterSourceType sourceType,
        EncounterTableDefinition table,
        BattleTrigger battleTrigger,
        StealthCaptureProfileDefinition captureProfile,
        UnityEngine.Object context,
        out EncounterCaptureResult captureResult,
        bool allowBattleOnFailure = true
    ) {
        captureResult = null;
        if(captureProfile == null) {
            return StartBattle(player, pokemon, sourceType, table, battleTrigger, context);
        }

        captureResult = captureProfile.TryCapture(player, pokemon, sourceType);

        if(captureResult.success) {
            player?.GetComponent<PlayerEncounterLog>()?.RecordSeen(pokemon, sourceType, table);
            player?.GetComponent<PlayerEncounterLog>()?.RecordCaptured(pokemon, sourceType, table, stealth: true);
            Publish(
                $"encounter.stealth-captured.{pokemon.Base.name}",
                captureResult.message,
                GameEventImportance.Success,
                player,
                context,
                sourceType,
                table,
                pokemon,
                showInFeed: true,
                GameEventPublishing.Value("chance", captureResult.chancePercent));
            return EncounterStartResult.Captured;
        }

        Publish(
            $"encounter.stealth-failed.{pokemon?.Base?.name ?? "unknown"}",
            captureResult.message,
            GameEventImportance.Info,
            player,
            context,
            sourceType,
            table,
            pokemon,
            showInFeed: true,
            GameEventPublishing.Value("chance", captureResult.chancePercent));

        if(captureResult.shouldStartBattle && allowBattleOnFailure) {
            return StartBattle(player, pokemon, sourceType, table, battleTrigger, context);
        }

        if(!allowBattleOnFailure) {
            captureResult.shouldStartBattle = false;
        }

        player?.GetComponent<PlayerEncounterLog>()?.RecordSeen(pokemon, sourceType, table);
        return EncounterStartResult.NoEncounter;
    }

    public static void PublishNoEncounter(PlayerController player, EncounterTableDefinition table, EncounterSourceType sourceType, UnityEngine.Object context, string message) {
        GameEventPublishing.PublishOptional(
            table != null ? table.NoEncounterEvent : null,
            $"encounter.none.{(table != null ? table.Id : "missing")}",
            message,
            GameEventCategory.Encounter,
            GameEventImportance.Trace,
            context != null ? context : player,
            "EncounterSystem",
            GameEventScope.Scene,
            showInFeed: table != null && table.ShowEventsInFeed,
            writeToDebugLog: table != null && table.WriteEventsToDebugLog,
            GameEventPublishing.Value("tableId", table != null ? table.Id : string.Empty),
            GameEventPublishing.Value("sourceType", sourceType));
    }

    static void Publish(
        string id,
        string message,
        GameEventImportance importance,
        PlayerController player,
        UnityEngine.Object context,
        EncounterSourceType sourceType,
        EncounterTableDefinition table,
        Pokemon pokemon,
        bool showInFeed,
        params GameEventValue[] extraValues
    ) {
        GameEventPublishing.PublishOptional(
            table != null && importance == GameEventImportance.Success ? table.EncounterStartedEvent : null,
            id,
            message,
            GameEventCategory.Encounter,
            importance,
            context != null ? context : player,
            "EncounterSystem",
            GameEventScope.Scene,
            showInFeed: showInFeed,
            writeToDebugLog: table != null && table.WriteEventsToDebugLog,
            MergeValues(
                extraValues,
                GameEventPublishing.Value("sourceType", sourceType),
                GameEventPublishing.Value("tableId", table != null ? table.Id : string.Empty),
                GameEventPublishing.Value("tableName", table != null ? table.DisplayName : string.Empty),
                GameEventPublishing.Value("pokemon", pokemon != null && pokemon.Base != null ? pokemon.Base.Name : string.Empty),
                GameEventPublishing.Value("level", pokemon != null ? pokemon.Level : 0)));
    }

    static GameEventValue[] MergeValues(GameEventValue[] extras, params GameEventValue[] baseValues) {
        if(extras == null || extras.Length == 0) {
            return baseValues;
        }

        var values = new GameEventValue[baseValues.Length + extras.Length];
        baseValues.CopyTo(values, 0);
        extras.CopyTo(values, baseValues.Length);
        return values;
    }
}
