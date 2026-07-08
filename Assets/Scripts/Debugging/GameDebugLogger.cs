using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public enum GameDebugSeverity {
    Trace,
    Info,
    Success,
    Warning,
    Error,
    Exception
}

public enum GameDebugCategory {
    General,
    Battle,
    Activity,
    Farming,
    Resource,
    PokemonCare,
    Research,
    Quest,
    RPG,
    Inventory,
    SaveLoad,
    UI,
    Input,
    Scene,
    NPC,
    Validation,
    Crafting,
    Shop,
    Encounter,
    Job,
    Transit,
    Customization,
    PokeNav,
    Map,
    Rumor,
    Calendar,
    BattleRule,
    Contest,
    Career,
    Organization,
    Assignment,
    Access,
    Law,
    Investigation,
    Risk,
    Consequence,
    WorldTrigger,
    SceneObject,
    SceneSpawn,
    WorldDiscovery,
    LocationVisit,
    Chronicle,
    Navigation,
    AreaProfile
}

public class GameDebugLogger : MonoBehaviour {
    [Header("Log Capture")]
    [Tooltip("If enabled, Unity Debug.Log/Warning/Error entries are copied into the debug logger.")]
    [SerializeField] bool captureUnityLogs = true;
    [Tooltip("If enabled, debug entries are written to Application.persistentDataPath/DebugLogs.")]
    [SerializeField] bool writeToFile = true;
    [Tooltip("If enabled, custom GameDebug entries are also echoed into Unity's Console.")]
    [SerializeField] bool echoCustomLogsToUnity = true;
    [Tooltip("Maximum number of recent debug entries kept in memory.")]
    [Min(1)]
    [SerializeField] int maxMemoryEntries = 300;
    [Tooltip("Maximum number of recent breadcrumb steps attached to warnings/errors.")]
    [Min(1)]
    [SerializeField] int maxBreadcrumbs = 30;

    static readonly object fileLock = new object();
    static int suppressUnityCaptureDepth;

    readonly List<GameDebugEntry> entries = new List<GameDebugEntry>();
    readonly Queue<string> breadcrumbs = new Queue<string>();
    StreamWriter writer;

    public static GameDebugLogger i { get; private set; }
    public IReadOnlyList<GameDebugEntry> Entries => entries;
    public string CurrentLogPath { get; private set; }

    void Awake() {
        if(i != null && i != this) {
            Destroy(gameObject);
            return;
        }

        i = this;
        DontDestroyOnLoad(gameObject);
        OpenWriter();
    }

    void OnEnable() {
        if(captureUnityLogs) {
            Application.logMessageReceived += HandleUnityLog;
        }
    }

    void OnDisable() {
        Application.logMessageReceived -= HandleUnityLog;
    }

    void OnDestroy() {
        if(i == this) {
            i = null;
        }

        CloseWriter();
    }

    public static GameDebugLogger Ensure() {
        if(i != null) {
            return i;
        }

        var logger = FindAnyObjectByType<GameDebugLogger>();
        if(logger != null) {
            i = logger;
            return i;
        }

        var go = new GameObject("GameDebugLogger");
        return go.AddComponent<GameDebugLogger>();
    }

    public void Record(
        GameDebugSeverity severity,
        GameDebugCategory category,
        string message,
        UnityEngine.Object context = null,
        string source = null,
        string stackTrace = null,
        bool echoToUnity = true
    ) {
        var entry = new GameDebugEntry() {
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            frame = Time.frameCount,
            severity = severity,
            category = category,
            source = source,
            contextName = context != null ? context.name : null,
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            message = message,
            stackTrace = stackTrace,
            breadcrumbs = breadcrumbs.ToList()
        };

        entries.Add(entry);
        while(entries.Count > Mathf.Max(1, maxMemoryEntries)) {
            entries.RemoveAt(0);
        }

        WriteEntry(entry);

        if(echoCustomLogsToUnity && echoToUnity) {
            EchoToUnity(entry, context);
        }
    }

    public void AddBreadcrumb(string message, GameDebugCategory category = GameDebugCategory.General) {
        string breadcrumb = $"{DateTime.Now:HH:mm:ss.fff} [{category}] {message}";
        breadcrumbs.Enqueue(breadcrumb);
        while(breadcrumbs.Count > Mathf.Max(1, maxBreadcrumbs)) {
            breadcrumbs.Dequeue();
        }

        Record(GameDebugSeverity.Trace, category, message, source: "Breadcrumb", echoToUnity: false);
    }

    public string BuildRecentReport(int maxEntries = 50) {
        var builder = new StringBuilder();
        builder.AppendLine($"Debug report generated at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        builder.AppendLine($"Log file: {CurrentLogPath}");
        builder.AppendLine();

        foreach(var entry in entries.Skip(Mathf.Max(0, entries.Count - maxEntries))) {
            builder.AppendLine(entry.ToLine());
            if(!string.IsNullOrWhiteSpace(entry.stackTrace)) {
                builder.AppendLine(entry.stackTrace);
            }
        }

        return builder.ToString();
    }

    void HandleUnityLog(string condition, string stackTrace, LogType type) {
        if(suppressUnityCaptureDepth > 0) {
            return;
        }

        var severity = type switch {
            LogType.Warning => GameDebugSeverity.Warning,
            LogType.Error => GameDebugSeverity.Error,
            LogType.Assert => GameDebugSeverity.Error,
            LogType.Exception => GameDebugSeverity.Exception,
            _ => GameDebugSeverity.Info
        };

        Record(severity, GameDebugCategory.General, condition, source: $"Unity.{type}", stackTrace: stackTrace, echoToUnity: false);
    }

    void EchoToUnity(GameDebugEntry entry, UnityEngine.Object context) {
        string line = entry.ToLine();
        suppressUnityCaptureDepth++;
        try {
            if(entry.severity == GameDebugSeverity.Error || entry.severity == GameDebugSeverity.Exception) {
                Debug.LogError(line, context);
            } else if(entry.severity == GameDebugSeverity.Warning) {
                Debug.LogWarning(line, context);
            } else {
                Debug.Log(line, context);
            }
        } finally {
            suppressUnityCaptureDepth--;
        }
    }

    void OpenWriter() {
        if(!writeToFile) {
            return;
        }

        try {
            string directory = Path.Combine(Application.persistentDataPath, "DebugLogs");
            Directory.CreateDirectory(directory);
            CurrentLogPath = Path.Combine(directory, $"session_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            writer = new StreamWriter(CurrentLogPath, append: false, Encoding.UTF8) {
                AutoFlush = true
            };
            writer.WriteLine($"PokemonProject debug session started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"Unity version: {Application.unityVersion}");
            writer.WriteLine($"Platform: {Application.platform}");
            writer.WriteLine();
        } catch(Exception ex) {
            writer = null;
            Debug.LogWarning($"GameDebugLogger could not open log file: {ex.Message}", this);
        }
    }

    void CloseWriter() {
        lock(fileLock) {
            writer?.Flush();
            writer?.Dispose();
            writer = null;
        }
    }

    void WriteEntry(GameDebugEntry entry) {
        if(writer == null) {
            return;
        }

        lock(fileLock) {
            writer.WriteLine(entry.ToLine());
            if(entry.breadcrumbs != null && entry.breadcrumbs.Count > 0 && ShouldWriteBreadcrumbs(entry.severity)) {
                writer.WriteLine("  Recent steps:");
                foreach(var breadcrumb in entry.breadcrumbs) {
                    writer.WriteLine($"    - {breadcrumb}");
                }
            }

            if(!string.IsNullOrWhiteSpace(entry.stackTrace)) {
                writer.WriteLine(entry.stackTrace);
            }
        }
    }

    bool ShouldWriteBreadcrumbs(GameDebugSeverity severity) {
        return severity == GameDebugSeverity.Warning
            || severity == GameDebugSeverity.Error
            || severity == GameDebugSeverity.Exception;
    }
}

[Serializable]
public class GameDebugEntry {
    public string timestamp;
    public int frame;
    public GameDebugSeverity severity;
    public GameDebugCategory category;
    public string source;
    public string contextName;
    public string sceneName;
    public string message;
    public string stackTrace;
    public List<string> breadcrumbs = new List<string>();

    public string ToLine() {
        string origin = string.IsNullOrWhiteSpace(source) ? category.ToString() : $"{category}/{source}";
        string context = string.IsNullOrWhiteSpace(contextName) ? "" : $" Context={contextName}";
        return $"{timestamp} Frame={frame} Scene={sceneName} [{severity}] [{origin}]{context} {message}";
    }
}
