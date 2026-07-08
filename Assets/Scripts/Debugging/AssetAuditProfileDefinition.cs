using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum AssetAuditRuleKind {
    MissingObjectReferences,
    MissingScriptsInPrefabs,
    MissingMetaFiles,
    OrphanMetaFiles,
    InvalidMetaGuids,
    DuplicateMetaGuids,
    CountAssetsAtLeast,
    CountAssetsAtMost,
    CountAssetsExactly,
    AssetExists
}

public enum AssetAuditTargetType {
    AnyAsset,
    ScriptableObject,
    Prefab,
    Scene,
    Sprite,
    Texture,
    AudioClip,
    Material,
    AnimationClip,
    AnimatorController,
    CustomTypeName
}

public enum AssetAuditSeverity {
    Info,
    Warning,
    Error
}

[CreateAssetMenu(menuName = "Debugging/Asset Audit/Profile Definition")]
public class AssetAuditProfileDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id for this asset audit profile. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug logs. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining what this profile is meant to inspect.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as release, content, scene, meta, sprites or shop.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Rules")]
    [Tooltip("Editable asset audit rules. Each rule scans project assets, meta files or serialized references.")]
    [SerializeField] List<AssetAuditRule> rules = new List<AssetAuditRule>();
    [Tooltip("If enabled, successful rules are included in the generated report results.")]
    [SerializeField] bool includePassedRulesInReport = true;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public IReadOnlyList<AssetAuditRule> Rules => rules != null ? (IReadOnlyList<AssetAuditRule>)rules : Array.Empty<AssetAuditRule>();
    public bool IncludePassedRulesInReport => includePassedRulesInReport;

    public AssetAuditReport Run(UnityEngine.Object context = null) {
        var report = new AssetAuditReport(Id, DisplayName, context != null ? context.name : null);

        if(rules == null || rules.Count == 0) {
            report.AddIssue(AssetAuditSeverity.Info, "No asset audit rules are defined.", "AssetAudit/Profile", null);
            return report;
        }

        foreach(var rule in rules) {
            if(rule == null) {
                report.AddIssue(AssetAuditSeverity.Warning, "Profile contains a null rule slot.", DisplayName, null);
                continue;
            }

            if(!rule.Enabled) {
                continue;
            }

            var result = rule.Evaluate();
            if(result.Passed && !includePassedRulesInReport) {
                report.passedRuleCount++;
                report.totalRuleCount++;
                continue;
            }

            report.AddResult(result);
        }

        return report;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag) && Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
    }
}

[Serializable]
public class AssetAuditRule {
    [Tooltip("If disabled, this rule is skipped.")]
    [SerializeField] bool enabled = true;
    [Tooltip("Stable id for this rule inside the profile. Empty uses the rule kind and target type.")]
    [SerializeField] string ruleId = string.Empty;
    [Tooltip("Short note explaining what this rule protects against.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("What this rule checks.")]
    [SerializeField] AssetAuditRuleKind kind = AssetAuditRuleKind.MissingObjectReferences;
    [Tooltip("Severity used when the rule fails.")]
    [SerializeField] AssetAuditSeverity severity = AssetAuditSeverity.Warning;
    [Tooltip("Asset type scanned by editor-backed rules. Meta rules scan files instead.")]
    [SerializeField] AssetAuditTargetType targetType = AssetAuditTargetType.AnyAsset;
    [Tooltip("Type name used when Target Type is Custom Type Name. Accepts class name or AssetDatabase filter type.")]
    [SerializeField] string customTypeName = string.Empty;
    [Tooltip("Project folders searched by this rule. Empty uses Assets.")]
    [SerializeField] List<string> searchFolders = new List<string> { "Assets" };
    [Tooltip("Path fragments ignored by this rule.")]
    [SerializeField] List<string> ignoredPathContains = new List<string>();
    [Tooltip("Optional path fragment required for a candidate to match.")]
    [SerializeField] string pathContains = string.Empty;
    [Tooltip("Optional file extension required for a candidate to match. Include the dot, such as .prefab or .asset.")]
    [SerializeField] string requiredExtension = string.Empty;
    [Tooltip("Expected count used by count and exists rules.")]
    [Min(0)]
    [SerializeField] int threshold = 1;
    [Tooltip("Maximum failing samples stored in this rule result.")]
    [Min(1)]
    [SerializeField] int maxSamples = 25;
    [Tooltip("Optional custom failure text. Empty generates a standard message.")]
    [TextArea]
    [SerializeField] string failureMessage = string.Empty;

    public bool Enabled => enabled;
    public string RuleId => string.IsNullOrWhiteSpace(ruleId) ? $"{kind}/{targetType}" : ruleId;
    public string Description => description;
    public AssetAuditRuleKind Kind => kind;
    public AssetAuditSeverity Severity => severity;
    public AssetAuditTargetType TargetType => targetType;
    public string CustomTypeName => customTypeName;
    public IReadOnlyList<string> SearchFolders => searchFolders != null ? (IReadOnlyList<string>)searchFolders : Array.Empty<string>();
    public IReadOnlyList<string> IgnoredPathContains => ignoredPathContains != null ? (IReadOnlyList<string>)ignoredPathContains : Array.Empty<string>();
    public string PathContains => pathContains;
    public string RequiredExtension => requiredExtension;
    public int Threshold => Mathf.Max(0, threshold);
    public int MaxSamples => Mathf.Max(1, maxSamples);

    public AssetAuditRuleResult Evaluate() {
        return kind switch {
            AssetAuditRuleKind.MissingMetaFiles => EvaluateMissingMetaFiles(),
            AssetAuditRuleKind.OrphanMetaFiles => EvaluateOrphanMetaFiles(),
            AssetAuditRuleKind.InvalidMetaGuids => EvaluateInvalidMetaGuids(),
            AssetAuditRuleKind.DuplicateMetaGuids => EvaluateDuplicateMetaGuids(),
            AssetAuditRuleKind.CountAssetsAtLeast => EvaluateAssetCount(count => count >= Threshold, $"at least {Threshold}"),
            AssetAuditRuleKind.CountAssetsAtMost => EvaluateAssetCount(count => count <= Threshold, $"at most {Threshold}"),
            AssetAuditRuleKind.CountAssetsExactly => EvaluateAssetCount(count => count == Threshold, $"exactly {Threshold}"),
            AssetAuditRuleKind.AssetExists => EvaluateAssetCount(count => count > 0, "one or more"),
            AssetAuditRuleKind.MissingScriptsInPrefabs => EvaluateMissingScriptsInPrefabs(),
            _ => EvaluateMissingObjectReferences()
        };
    }

    AssetAuditRuleResult EvaluateMissingMetaFiles() {
        var files = EnumerateProjectFiles(includeMetaFiles: false);
        var missing = files
            .Where(path => !File.Exists($"{path}.meta"))
            .Select(ToProjectPath)
            .Take(MaxSamples)
            .ToList();
        int totalMissing = files.Count(path => !File.Exists($"{path}.meta"));
        return BuildResult(totalMissing == 0, totalMissing, "no missing meta files", missing, $"{totalMissing} asset file(s) have no .meta file.");
    }

    AssetAuditRuleResult EvaluateOrphanMetaFiles() {
        var metaFiles = EnumerateProjectFiles(includeMetaFiles: true).Where(path => path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)).ToList();
        var orphaned = metaFiles
            .Where(path => !File.Exists(path.Substring(0, path.Length - 5)) && !Directory.Exists(path.Substring(0, path.Length - 5)))
            .Select(ToProjectPath)
            .Take(MaxSamples)
            .ToList();
        int totalOrphaned = metaFiles.Count(path => !File.Exists(path.Substring(0, path.Length - 5)) && !Directory.Exists(path.Substring(0, path.Length - 5)));
        return BuildResult(totalOrphaned == 0, totalOrphaned, "no orphan meta files", orphaned, $"{totalOrphaned} .meta file(s) have no matching asset or folder.");
    }

    AssetAuditRuleResult EvaluateInvalidMetaGuids() {
        var metaFiles = EnumerateProjectFiles(includeMetaFiles: true).Where(path => path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)).ToList();
        var invalid = new List<string>();
        int totalInvalid = 0;

        foreach(var path in metaFiles) {
            string guid = TryReadGuid(path);
            if(string.IsNullOrWhiteSpace(guid) || !Regex.IsMatch(guid, "^[0-9a-fA-F]{32}$")) {
                totalInvalid++;
                if(invalid.Count < MaxSamples) {
                    invalid.Add(ToProjectPath(path));
                }
            }
        }

        return BuildResult(totalInvalid == 0, totalInvalid, "all meta GUIDs valid", invalid, $"{totalInvalid} .meta file(s) have missing or invalid GUIDs.");
    }

    AssetAuditRuleResult EvaluateDuplicateMetaGuids() {
        var metaFiles = EnumerateProjectFiles(includeMetaFiles: true).Where(path => path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)).ToList();
        var groups = metaFiles
            .Select(path => new { path, guid = TryReadGuid(path) })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.guid) && Regex.IsMatch(entry.guid, "^[0-9a-fA-F]{32}$"))
            .GroupBy(entry => entry.guid, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .ToList();

        var samples = groups
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(entry => ToProjectPath(entry.path)).Take(4))}")
            .Take(MaxSamples)
            .ToList();

        return BuildResult(groups.Count == 0, groups.Count, "no duplicate meta GUIDs", samples, $"{groups.Count} duplicate GUID group(s) found.");
    }

    AssetAuditRuleResult EvaluateAssetCount(Func<int, bool> predicate, string expectedText) {
        var paths = FindAssetPaths();
        bool passed = predicate(paths.Count);
        string message = passed
            ? $"{RuleId} passed. Matched {paths.Count}; expected {expectedText}."
            : string.IsNullOrWhiteSpace(failureMessage)
                ? $"{RuleId} failed. Matched {paths.Count}; expected {expectedText}."
                : failureMessage;
        return new AssetAuditRuleResult(RuleId, description, severity, kind, targetType, Threshold, paths.Count, passed, message, paths.Take(MaxSamples).ToList());
    }

    AssetAuditRuleResult EvaluateMissingObjectReferences() {
#if UNITY_EDITOR
        var paths = FindAssetPaths();
        var samples = new List<string>();
        int issueCount = 0;

        foreach(var path in paths) {
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path)) {
                if(asset == null) {
                    continue;
                }

                issueCount += CountMissingObjectReferences(asset, path, samples);
            }
        }

        return BuildResult(issueCount == 0, issueCount, "no missing object references", samples, $"{issueCount} missing serialized object reference(s) found.");
#else
        return BuildEditorOnlyResult();
#endif
    }

    AssetAuditRuleResult EvaluateMissingScriptsInPrefabs() {
#if UNITY_EDITOR
        var paths = FindAssetPaths(AssetAuditTargetType.Prefab);
        int issueCount = 0;
        var samples = new List<string>();

        foreach(var path in paths) {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(root == null) {
                continue;
            }

            int count = CountMissingScripts(root);
            if(count <= 0) {
                continue;
            }

            issueCount += count;
            if(samples.Count < MaxSamples) {
                samples.Add($"{path} ({count} missing script component(s))");
            }
        }

        return BuildResult(issueCount == 0, issueCount, "no missing scripts in prefabs", samples, $"{issueCount} missing script component(s) found in prefabs.");
#else
        return BuildEditorOnlyResult();
#endif
    }

#if UNITY_EDITOR
    int CountMissingObjectReferences(UnityEngine.Object asset, string assetPath, List<string> samples) {
        int count = 0;
        SerializedObject serializedObject;
        try {
            serializedObject = new SerializedObject(asset);
        } catch(Exception) {
            return 0;
        }

        var property = serializedObject.GetIterator();
        bool enterChildren = true;
        while(property.NextVisible(enterChildren)) {
            enterChildren = false;
            if(property.propertyType != SerializedPropertyType.ObjectReference) {
                continue;
            }

            if(property.objectReferenceValue == null && property.objectReferenceEntityIdValue != default) {
                count++;
                if(samples.Count < MaxSamples) {
                    samples.Add($"{assetPath} :: {asset.name}.{property.propertyPath}");
                }
            }
        }

        return count;
    }

    int CountMissingScripts(GameObject root) {
        int count = 0;
        foreach(var transform in root.GetComponentsInChildren<Transform>(true)) {
            if(transform != null) {
                count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
            }
        }

        return count;
    }
#endif

    AssetAuditRuleResult BuildEditorOnlyResult() {
        return new AssetAuditRuleResult(
            RuleId,
            description,
            AssetAuditSeverity.Info,
            kind,
            targetType,
            Threshold,
            0,
            true,
            $"{RuleId} skipped because this audit requires Unity Editor APIs.",
            new List<string>());
    }

    AssetAuditRuleResult BuildResult(bool passed, int issueCount, string expectedText, List<string> samples, string defaultFailure) {
        string message = passed
            ? $"{RuleId} passed. Expected {expectedText}."
            : string.IsNullOrWhiteSpace(failureMessage) ? $"{RuleId} failed. {defaultFailure}" : failureMessage;

        return new AssetAuditRuleResult(RuleId, description, severity, kind, targetType, 0, issueCount, passed, message, samples ?? new List<string>());
    }

    List<string> FindAssetPaths() {
        return FindAssetPaths(targetType);
    }

    List<string> FindAssetPaths(AssetAuditTargetType overrideTargetType) {
#if UNITY_EDITOR
        string filter = BuildAssetDatabaseFilter(overrideTargetType);
        string[] folders = ResolveSearchFoldersForAssetDatabase();
        return AssetDatabase.FindAssets(filter, folders)
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(MatchesPathFilters)
            .Distinct()
            .OrderBy(path => path)
            .ToList();
#else
        return new List<string>();
#endif
    }

    string BuildAssetDatabaseFilter(AssetAuditTargetType selectedTargetType) {
        return selectedTargetType switch {
            AssetAuditTargetType.ScriptableObject => "t:ScriptableObject",
            AssetAuditTargetType.Prefab => "t:Prefab",
            AssetAuditTargetType.Scene => "t:Scene",
            AssetAuditTargetType.Sprite => "t:Sprite",
            AssetAuditTargetType.Texture => "t:Texture",
            AssetAuditTargetType.AudioClip => "t:AudioClip",
            AssetAuditTargetType.Material => "t:Material",
            AssetAuditTargetType.AnimationClip => "t:AnimationClip",
            AssetAuditTargetType.AnimatorController => "t:AnimatorController",
            AssetAuditTargetType.CustomTypeName => string.IsNullOrWhiteSpace(customTypeName) ? string.Empty : $"t:{customTypeName}",
            _ => string.Empty
        };
    }

    string[] ResolveSearchFoldersForAssetDatabase() {
        var folders = ResolveSearchFolders()
            .Where(folder => folder.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return folders.Length > 0 ? folders : new[] { "Assets" };
    }

    IEnumerable<string> ResolveSearchFolders() {
        var folders = searchFolders?.Where(folder => !string.IsNullOrWhiteSpace(folder)).ToList() ?? new List<string>();
        if(folders.Count == 0) {
            folders.Add("Assets");
        }

        return folders.Select(folder => folder.Replace('\\', '/').TrimEnd('/'));
    }

    bool MatchesPathFilters(string path) {
        if(string.IsNullOrWhiteSpace(path)) {
            return false;
        }

        string normalized = path.Replace('\\', '/');
        if(!string.IsNullOrWhiteSpace(pathContains) && normalized.IndexOf(pathContains, StringComparison.OrdinalIgnoreCase) < 0) {
            return false;
        }

        if(!string.IsNullOrWhiteSpace(requiredExtension)
            && !normalized.EndsWith(requiredExtension, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        foreach(var ignored in ignoredPathContains ?? new List<string>()) {
            if(!string.IsNullOrWhiteSpace(ignored) && normalized.IndexOf(ignored, StringComparison.OrdinalIgnoreCase) >= 0) {
                return false;
            }
        }

        return true;
    }

    List<string> EnumerateProjectFiles(bool includeMetaFiles) {
        var results = new List<string>();
        string projectRoot = GetProjectRoot();

        foreach(var folder in ResolveSearchFolders()) {
            string absoluteFolder = Path.GetFullPath(Path.Combine(projectRoot, folder));
            if(!Directory.Exists(absoluteFolder)) {
                continue;
            }

            foreach(var path in Directory.GetFiles(absoluteFolder, "*", SearchOption.AllDirectories)) {
                if(!includeMetaFiles && path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                string projectPath = ToProjectPath(path);
                if(MatchesPathFilters(projectPath)) {
                    results.Add(path);
                }
            }
        }

        return results.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    string TryReadGuid(string metaPath) {
        try {
            foreach(var line in File.ReadLines(metaPath)) {
                var match = Regex.Match(line, @"^\s*guid:\s*([A-Za-z0-9]+)\s*$");
                if(match.Success) {
                    return match.Groups[1].Value;
                }
            }
        } catch(IOException) {
            return string.Empty;
        } catch(UnauthorizedAccessException) {
            return string.Empty;
        }

        return string.Empty;
    }

    string ToProjectPath(string absolutePath) {
        string projectRoot = GetProjectRoot().Replace('\\', '/').TrimEnd('/');
        string normalized = absolutePath.Replace('\\', '/');
        return normalized.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase)
            ? normalized.Substring(projectRoot.Length + 1)
            : normalized;
    }

    string GetProjectRoot() {
        return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    }
}

[Serializable]
public class AssetAuditReport {
    [Tooltip("Id of the audit profile that produced this report.")]
    public string profileId;
    [Tooltip("Display name of the audit profile that produced this report.")]
    public string profileName;
    [Tooltip("Optional Unity object or runner that requested the audit.")]
    public string contextName;
    [Tooltip("Local timestamp when this report was generated.")]
    public string generatedAt;
    [Tooltip("Unity frame count when this report was generated.")]
    public int frame;
    [Tooltip("Number of enabled rules evaluated.")]
    public int totalRuleCount;
    [Tooltip("Number of rules that passed.")]
    public int passedRuleCount;
    [Tooltip("Number of failed info-level rules.")]
    public int infoCount;
    [Tooltip("Number of failed warning-level rules.")]
    public int warningCount;
    [Tooltip("Number of failed error-level rules.")]
    public int errorCount;
    [Tooltip("Detailed rule results and issues.")]
    public List<AssetAuditRuleResult> results = new List<AssetAuditRuleResult>();

    public bool HasErrors => errorCount > 0;
    public bool HasWarnings => warningCount > 0;

    public AssetAuditReport(string profileId, string profileName, string contextName) {
        this.profileId = profileId;
        this.profileName = profileName;
        this.contextName = contextName;
        generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        frame = Time.frameCount;
    }

    public void AddResult(AssetAuditRuleResult result) {
        if(result == null) {
            return;
        }

        totalRuleCount++;
        if(result.Passed) {
            passedRuleCount++;
        } else {
            if(result.Severity == AssetAuditSeverity.Error) errorCount++;
            else if(result.Severity == AssetAuditSeverity.Warning) warningCount++;
            else infoCount++;
        }

        results.Add(result);
    }

    public void AddIssue(AssetAuditSeverity severity, string message, string context, List<string> sampleMatches) {
        AddResult(new AssetAuditRuleResult(
            context,
            string.Empty,
            severity,
            AssetAuditRuleKind.AssetExists,
            AssetAuditTargetType.AnyAsset,
            0,
            sampleMatches != null ? sampleMatches.Count : 0,
            false,
            message,
            sampleMatches ?? new List<string>()));
    }

    public string BuildSummary() {
        return $"Asset audit '{profileName}' finished. Errors={errorCount}, Warnings={warningCount}, Info={infoCount}, Passed={passedRuleCount}/{totalRuleCount}";
    }
}

[Serializable]
public class AssetAuditRuleResult {
    [Tooltip("Rule id that produced this result.")]
    public string ruleId;
    [Tooltip("Designer note copied from the rule.")]
    public string description;
    [Tooltip("Severity used when this result fails.")]
    public AssetAuditSeverity severity;
    [Tooltip("Rule kind used by this result.")]
    public AssetAuditRuleKind kind;
    [Tooltip("Target type scanned by this result.")]
    public AssetAuditTargetType targetType;
    [Tooltip("Configured threshold for this rule.")]
    public int threshold;
    [Tooltip("Number of issues or matching assets found.")]
    public int matchedCount;
    [Tooltip("Whether this rule passed.")]
    public bool passed;
    [Tooltip("Human-readable result message.")]
    public string message;
    [Tooltip("Small sample of matching assets or problems for quick inspection.")]
    public List<string> sampleMatches;

    public bool Passed => passed;
    public AssetAuditSeverity Severity => severity;
    public string Message => message;

    public AssetAuditRuleResult(
        string ruleId,
        string description,
        AssetAuditSeverity severity,
        AssetAuditRuleKind kind,
        AssetAuditTargetType targetType,
        int threshold,
        int matchedCount,
        bool passed,
        string message,
        List<string> sampleMatches
    ) {
        this.ruleId = ruleId;
        this.description = description;
        this.severity = severity;
        this.kind = kind;
        this.targetType = targetType;
        this.threshold = threshold;
        this.matchedCount = matchedCount;
        this.passed = passed;
        this.message = message;
        this.sampleMatches = sampleMatches ?? new List<string>();
    }
}
