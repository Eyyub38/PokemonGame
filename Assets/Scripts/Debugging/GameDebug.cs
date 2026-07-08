using System;
using UnityEngine;

public static class GameDebug {
    public static void Step(string message, GameDebugCategory category = GameDebugCategory.General, UnityEngine.Object context = null, string source = null) {
        var logger = GameDebugLogger.Ensure();
        logger.AddBreadcrumb(message, category);
        logger.Record(GameDebugSeverity.Info, category, message, context, source);
    }

    public static void Success(string message, GameDebugCategory category = GameDebugCategory.General, UnityEngine.Object context = null, string source = null) {
        var logger = GameDebugLogger.Ensure();
        logger.AddBreadcrumb($"SUCCESS: {message}", category);
        logger.Record(GameDebugSeverity.Success, category, message, context, source);
    }

    public static void Warning(string message, GameDebugCategory category = GameDebugCategory.General, UnityEngine.Object context = null, string source = null) {
        GameDebugLogger.Ensure().Record(GameDebugSeverity.Warning, category, message, context, source);
    }

    public static void Error(string message, GameDebugCategory category = GameDebugCategory.General, UnityEngine.Object context = null, string source = null) {
        GameDebugLogger.Ensure().Record(GameDebugSeverity.Error, category, message, context, source, Environment.StackTrace);
    }

    public static void Exception(Exception exception, GameDebugCategory category = GameDebugCategory.General, UnityEngine.Object context = null, string source = null) {
        if(exception == null) {
            return;
        }

        GameDebugLogger.Ensure().Record(
            GameDebugSeverity.Exception,
            category,
            exception.Message,
            context,
            source ?? exception.GetType().Name,
            exception.ToString());
    }

    public static bool GuardNotNull(object value, string name, GameDebugCategory category = GameDebugCategory.Validation, UnityEngine.Object context = null, string source = null) {
        if(value != null) {
            return true;
        }

        Error($"{name} is null.", category, context, source);
        return false;
    }

    public static bool GuardUnityObject(UnityEngine.Object value, string name, GameDebugCategory category = GameDebugCategory.Validation, UnityEngine.Object context = null, string source = null) {
        if(value != null) {
            return true;
        }

        Error($"{name} is missing or destroyed.", category, context, source);
        return false;
    }

    public static T GuardComponent<T>(Component owner, GameDebugCategory category = GameDebugCategory.Validation, string source = null) where T : Component {
        if(owner == null) {
            Error($"Cannot get component {typeof(T).Name}; owner is null.", category, null, source);
            return null;
        }

        var component = owner.GetComponent<T>();
        if(component == null) {
            Error($"{owner.name} is missing required component {typeof(T).Name}.", category, owner, source);
        }

        return component;
    }

    public static GameDebugLoopGuard LoopGuard(string loopName, int maxIterations, GameDebugCategory category = GameDebugCategory.Validation, UnityEngine.Object context = null) {
        return new GameDebugLoopGuard(loopName, maxIterations, category, context);
    }

    public static string BuildRecentReport(int maxEntries = 50) {
        return GameDebugLogger.Ensure().BuildRecentReport(maxEntries);
    }
}

public class GameDebugLoopGuard {
    readonly string loopName;
    readonly int maxIterations;
    readonly GameDebugCategory category;
    readonly UnityEngine.Object context;
    int iterations;

    public GameDebugLoopGuard(string loopName, int maxIterations, GameDebugCategory category, UnityEngine.Object context) {
        this.loopName = string.IsNullOrWhiteSpace(loopName) ? "Loop" : loopName;
        this.maxIterations = Mathf.Max(1, maxIterations);
        this.category = category;
        this.context = context;
    }

    public bool Tick() {
        iterations++;
        if(iterations <= maxIterations) {
            return true;
        }

        GameDebug.Error($"{loopName} exceeded {maxIterations} iterations. Possible infinite loop.", category, context, "LoopGuard");
        return false;
    }
}
