using UnityEngine;

public static class GameEventPublishing {
    public static GameEventValue Value(string key, object value) {
        return new GameEventValue(key, value != null ? value.ToString() : string.Empty);
    }

    public static GameEventRecord PublishOptional(
        GameEventDefinition definition,
        string fallbackId,
        string fallbackMessage,
        GameEventCategory fallbackCategory,
        GameEventImportance fallbackImportance,
        UnityEngine.Object context,
        string source,
        GameEventScope scope,
        bool showInFeed,
        bool writeToDebugLog,
        params GameEventValue[] values
    ) {
        if(definition != null) {
            return GameEventBus.Publish(definition, fallbackMessage, context, source, scope, values);
        }

        return GameEventBus.Publish(
            fallbackId,
            fallbackMessage,
            fallbackCategory,
            fallbackImportance,
            context,
            source,
            scope,
            showInFeed,
            writeToDebugLog,
            values);
    }
}
