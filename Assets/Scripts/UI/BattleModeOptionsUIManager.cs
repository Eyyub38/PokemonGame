using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BattleModeOptionsUIActionResultKind {
    None,
    Refreshed,
    Previewed,
    ModeSelected,
    ModeCleared,
    ChallengePreferenceChanged,
    Blocked
}

public class BattleModeOptionsUIManager : MonoBehaviour {
    const string ClassicOptionId = "classic";

    [Header("Player")]
    [Tooltip("Player whose battle mode settings are shown. Empty uses PlayerController.i or the first PlayerController in the scene.")]
    [SerializeField] PlayerController playerOverride = null;
    [Tooltip("If enabled, missing PlayerBattleModeSettings is created when UI actions need it.")]
    [SerializeField] bool createMissingSettingsForActions = true;

    [Header("Context")]
    [Tooltip("Optional battle challenge context used to show allowed/default battle modes.")]
    [SerializeField] BattleChallengeDefinition challenge = null;
    [Tooltip("Optional negotiator context used by trainer/challenge selection UI. Forced modes from this negotiator are shown as forced.")]
    [SerializeField] BattleRuleNegotiator negotiator = null;
    [Tooltip("If enabled, selecting a mode is blocked when the active challenge/negotiator would not allow it.")]
    [SerializeField] bool respectChallengeContextWhenSelecting = true;

    [Header("Mode Pool")]
    [Tooltip("Battle modes explicitly shown by this UI. Empty can still read Resources when Include Resource Modes is enabled.")]
    [SerializeField] List<BattleModeDefinition> modePool = new List<BattleModeDefinition>();
    [Tooltip("If enabled, all BattleModeDefinition assets in Resources are added to the selectable pool.")]
    [SerializeField] bool includeResourceModes = true;
    [Tooltip("If enabled, a built-in Classic Current Behavior row is shown. Selecting it clears the saved preference.")]
    [SerializeField] bool includeClassicClearOption = true;
    [Tooltip("Display name for the built-in classic/clear-preference row.")]
    [SerializeField] string classicOptionName = "Classic Battle";
    [Tooltip("Description for the built-in classic/clear-preference row.")]
    [TextArea]
    [SerializeField] string classicOptionDescription = "Use the current classic four-move BattleSystem behavior.";

    [Header("Visibility")]
    [Tooltip("If enabled, inaccessible modes remain visible with failure text.")]
    [SerializeField] bool includeUnavailableModes = true;
    [Tooltip("If enabled, modes that cannot run and cannot fall back remain visible with failure text.")]
    [SerializeField] bool includeUnsupportedModes = true;
    [Tooltip("If enabled, modes outside the active challenge context remain visible with failure text.")]
    [SerializeField] bool includeChallengeBlockedModes = true;
    [Tooltip("Optional tag filter. Empty shows every tag.")]
    [SerializeField] string requiredTag = string.Empty;
    [Tooltip("Maximum rows copied into the snapshot. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxRows = 20;

    [Header("Snapshot")]
    [Tooltip("If enabled, Refresh is called when this component starts.")]
    [SerializeField] bool refreshOnStart = true;
    [Tooltip("If enabled, Refresh is called after every successful or blocked action.")]
    [SerializeField] bool refreshAfterActions = true;
    [Tooltip("Source id written to debug/result rows.")]
    [SerializeField] string uiSourceId = "ui:battle-mode-options";

    [Header("Debug")]
    [Tooltip("If enabled, successful UI backend actions are written to GameDebug.")]
    [SerializeField] bool logSuccessfulActions;
    [Tooltip("If enabled, blocked UI backend actions are written to GameDebug.")]
    [SerializeField] bool logBlockedActions = true;

    BattleModeOptionsUIScreenSnapshot currentSnapshot = new BattleModeOptionsUIScreenSnapshot();
    BattleModeOptionsUIActionResult lastResult = new BattleModeOptionsUIActionResult();

    public BattleModeOptionsUIScreenSnapshot CurrentSnapshot => currentSnapshot;
    public BattleModeOptionsUIActionResult LastResult => lastResult;
    public BattleChallengeDefinition Challenge => challenge;
    public BattleRuleNegotiator Negotiator => negotiator;
    public IReadOnlyList<BattleModeDefinition> ModePool => modePool;
    public bool IncludeResourceModes => includeResourceModes;
    public bool CreateMissingSettingsForActions => createMissingSettingsForActions;
    public bool RespectChallengeContextWhenSelecting => respectChallengeContextWhenSelecting;
    public event Action<BattleModeOptionsUIScreenSnapshot> OnSnapshotChanged;
    public event Action<BattleModeOptionsUIActionResult> OnActionResult;

    void Start() {
        if(refreshOnStart) {
            Refresh();
        }
    }

    [ContextMenu("Refresh Battle Mode Options Snapshot")]
    public BattleModeOptionsUIScreenSnapshot RefreshFromContextMenu() {
        return Refresh();
    }

    public BattleModeOptionsUIScreenSnapshot Refresh() {
        var player = ResolvePlayer();
        var settings = player != null ? player.GetComponent<PlayerBattleModeSettings>() : null;
        var resolvedChallenge = ResolveChallenge();
        var forcedMode = ResolveForcedMode();
        var resolvedMode = ResolveResolvedMode(player, settings, resolvedChallenge, forcedMode);
        var rows = BuildRows(player, settings, resolvedChallenge, forcedMode, resolvedMode).ToList();

        currentSnapshot = new BattleModeOptionsUIScreenSnapshot {
            hasPlayer = player != null,
            playerName = player != null ? player.name : string.Empty,
            sourceId = ResolveSourceId(),
            hasSettings = settings != null,
            selectedModeId = settings != null && settings.SelectedBattleMode != null ? settings.SelectedBattleMode.Id : string.Empty,
            selectedModeName = settings != null && settings.SelectedBattleMode != null ? settings.SelectedBattleMode.DisplayName : classicOptionName,
            preferSelectedModeForChallenges = settings == null || settings.PreferSelectedModeForChallenges,
            challengeId = resolvedChallenge != null ? resolvedChallenge.Id : string.Empty,
            challengeName = resolvedChallenge != null ? resolvedChallenge.DisplayName : string.Empty,
            negotiatorName = negotiator != null ? negotiator.name : string.Empty,
            forcedModeId = forcedMode != null ? forcedMode.Id : string.Empty,
            forcedModeName = forcedMode != null ? forcedMode.DisplayName : string.Empty,
            resolvedModeId = resolvedMode != null ? resolvedMode.Id : string.Empty,
            resolvedModeName = resolvedMode != null ? resolvedMode.DisplayName : classicOptionName,
            optionCount = rows.Count,
            selectableCount = rows.Count(row => row != null && row.canSelect),
            lockedCount = rows.Count(row => row != null && !row.canSelect),
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0,
            absoluteHour = GetCurrentAbsoluteHour(),
            rows = rows,
            lastResult = lastResult
        };

        OnSnapshotChanged?.Invoke(currentSnapshot);
        return currentSnapshot;
    }

    public bool TrySelectMode(string optionId, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to select a battle mode.", out feedback);
        }

        var settings = GetSettings(player, createMissingSettingsForActions);
        if(settings == null) {
            return Block("PlayerBattleModeSettings is missing.", out feedback);
        }

        if(IsClassicOption(optionId)) {
            if(settings.SetBattleMode(null, out feedback)) {
                return Succeed(BattleModeOptionsUIActionResultKind.ModeCleared, "Battle mode preference cleared. Classic battle will be used.", out feedback);
            }
            return Block(feedback, out feedback);
        }

        var mode = FindMode(optionId);
        if(mode == null) {
            return Block($"Battle mode '{optionId}' could not be found.", out feedback);
        }

        if(!CanSelectMode(player, mode, out feedback)) {
            return Block(feedback, out feedback);
        }

        if(settings.SetBattleMode(mode, out feedback)) {
            return Succeed(BattleModeOptionsUIActionResultKind.ModeSelected, $"{mode.DisplayName} selected.", out feedback);
        }

        return Block(feedback, out feedback);
    }

    public bool TryClearMode(out string feedback) {
        return TrySelectMode(ClassicOptionId, out feedback);
    }

    public bool TrySetPreferSelectedModeForChallenges(bool prefer, out string feedback) {
        var player = ResolvePlayer();
        if(player == null) {
            return Block("A player is required to change battle mode challenge preferences.", out feedback);
        }

        var settings = GetSettings(player, createMissingSettingsForActions);
        if(settings == null) {
            return Block("PlayerBattleModeSettings is missing.", out feedback);
        }

        settings.SetPreferSelectedModeForChallenges(prefer);
        return Succeed(
            BattleModeOptionsUIActionResultKind.ChallengePreferenceChanged,
            prefer ? "Preferred battle mode will be used for allowed challenges." : "Preferred battle mode will not override challenge defaults.",
            out feedback);
    }

    public bool TryPreviewMode(string optionId, out BattleModeOptionPreviewResult preview, out string feedback) {
        var player = ResolvePlayer();
        var settings = player != null ? player.GetComponent<PlayerBattleModeSettings>() : null;
        var resolvedChallenge = ResolveChallenge();
        var forcedMode = ResolveForcedMode();
        BattleModeDefinition mode = IsClassicOption(optionId) ? null : FindMode(optionId);

        if(!IsClassicOption(optionId) && mode == null) {
            preview = BattleModeOptionPreviewResult.Blocked(optionId, $"Battle mode '{optionId}' could not be found.");
            feedback = preview.message;
            SetLastResult(BattleModeOptionsUIActionResultKind.Blocked, false, feedback);
            return false;
        }

        var row = mode == null
            ? BattleModeOptionRow.FromClassic(classicOptionName, classicOptionDescription, settings, resolvedChallenge, forcedMode, ResolveResolvedMode(player, settings, resolvedChallenge, forcedMode))
            : BattleModeOptionRow.FromMode(mode, player, settings, resolvedChallenge, forcedMode, ResolveResolvedMode(player, settings, resolvedChallenge, forcedMode), CanSelectMode(player, mode, out var failure) ? null : failure);

        preview = BattleModeOptionPreviewResult.FromRow(row);
        feedback = preview.message;
        return preview.canSelect
            ? Succeed(BattleModeOptionsUIActionResultKind.Previewed, feedback, out feedback)
            : Block(feedback, out feedback);
    }

    public BattleModeOptionRow FindRow(string optionId) {
        return currentSnapshot?.rows?
            .FirstOrDefault(row => row != null && string.Equals(row.optionId, NormalizeOptionId(optionId), StringComparison.OrdinalIgnoreCase));
    }

    IEnumerable<BattleModeOptionRow> BuildRows(
        PlayerController player,
        PlayerBattleModeSettings settings,
        BattleChallengeDefinition resolvedChallenge,
        BattleModeDefinition forcedMode,
        BattleModeDefinition resolvedMode) {
        var rows = new List<BattleModeOptionRow>();
        if(includeClassicClearOption) {
            rows.Add(BattleModeOptionRow.FromClassic(classicOptionName, classicOptionDescription, settings, resolvedChallenge, forcedMode, resolvedMode));
        }

        rows.AddRange(ResolveModePool(resolvedChallenge, forcedMode, player)
            .Where(mode => mode != null)
            .Distinct()
            .Where(mode => string.IsNullOrWhiteSpace(requiredTag) || mode.HasTag(requiredTag))
            .Select(mode => BattleModeOptionRow.FromMode(mode, player, settings, resolvedChallenge, forcedMode, resolvedMode, CanSelectMode(player, mode, out var failure) ? null : failure))
            .Where(ShouldShowRow)
            .OrderByDescending(row => row.isForcedByNegotiator)
            .ThenByDescending(row => row.isResolvedForContext)
            .ThenByDescending(row => row.isSelectedPreference)
            .ThenByDescending(row => row.isDefaultForChallenge)
            .ThenBy(row => row.displayName));

        return maxRows > 0 ? rows.Take(maxRows) : rows;
    }

    bool ShouldShowRow(BattleModeOptionRow row) {
        if(row == null) {
            return false;
        }

        if(row.isClassicClearOption) {
            return includeClassicClearOption;
        }

        if(!includeUnavailableModes && !row.canAccess) {
            return false;
        }

        if(!includeUnsupportedModes && !row.canRunInCurrentBattleSystem) {
            return false;
        }

        if(!includeChallengeBlockedModes && !row.allowedByChallenge) {
            return false;
        }

        return true;
    }

    bool CanSelectMode(PlayerController player, BattleModeDefinition mode, out string failureMessage) {
        if(mode == null) {
            failureMessage = null;
            return true;
        }

        if(!mode.CanAccess(player, out failureMessage)) {
            return false;
        }

        if(!mode.CanRunWithCurrentBattleSystem(out failureMessage, out _)) {
            return false;
        }

        if(respectChallengeContextWhenSelecting) {
            var forcedMode = ResolveForcedMode();
            if(forcedMode != null && forcedMode != mode) {
                failureMessage = $"{forcedMode.DisplayName} is forced for this battle context.";
                return false;
            }

            var resolvedChallenge = ResolveChallenge();
            if(resolvedChallenge != null && !resolvedChallenge.IsBattleModeAllowed(player, mode)) {
                failureMessage = $"{mode.DisplayName} is not allowed for {resolvedChallenge.DisplayName}.";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    IEnumerable<BattleModeDefinition> ResolveModePool(BattleChallengeDefinition resolvedChallenge, BattleModeDefinition forcedMode, PlayerController player) {
        if(forcedMode != null) {
            yield return forcedMode;
            yield break;
        }

        var basePool = new List<BattleModeDefinition>();
        if(modePool != null) {
            basePool.AddRange(modePool.Where(mode => mode != null));
        }

        if(includeResourceModes) {
            basePool.AddRange(Resources.LoadAll<BattleModeDefinition>("").Where(mode => mode != null));
        }

        if(resolvedChallenge != null) {
            if(resolvedChallenge.DefaultBattleMode != null) {
                basePool.Add(resolvedChallenge.DefaultBattleMode);
            }

            if(resolvedChallenge.AllowedBattleModes != null && resolvedChallenge.AllowedBattleModes.Count > 0) {
                basePool.AddRange(resolvedChallenge.AllowedBattleModes.Where(mode => mode != null));
            }
        }

        foreach(var mode in basePool
            .Where(mode => resolvedChallenge == null || resolvedChallenge.AllowedBattleModes == null || resolvedChallenge.AllowedBattleModes.Count == 0 || resolvedChallenge.AllowedBattleModes.Contains(mode) || resolvedChallenge.DefaultBattleMode == mode)
            .Distinct()) {
            yield return mode;
        }
    }

    BattleModeDefinition FindMode(string optionId) {
        if(string.IsNullOrWhiteSpace(optionId)) {
            return null;
        }

        string id = NormalizeOptionId(optionId);
        return ResolveModePool(ResolveChallenge(), ResolveForcedMode(), ResolvePlayer())
            .FirstOrDefault(mode => mode != null && string.Equals(mode.Id, id, StringComparison.OrdinalIgnoreCase))
            ?? Resources.LoadAll<BattleModeDefinition>("").FirstOrDefault(mode => mode != null && string.Equals(mode.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    PlayerBattleModeSettings GetSettings(PlayerController player, bool createIfMissing) {
        if(player == null) {
            return null;
        }

        var settings = player.GetComponent<PlayerBattleModeSettings>();
        return settings != null || !createIfMissing ? settings : player.gameObject.AddComponent<PlayerBattleModeSettings>();
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        if(PlayerController.i != null) {
            return PlayerController.i;
        }

        return FindAnyObjectByType<PlayerController>();
    }

    BattleChallengeDefinition ResolveChallenge() {
        return challenge != null ? challenge : negotiator != null ? negotiator.Challenge : null;
    }

    BattleModeDefinition ResolveForcedMode() {
        return negotiator != null ? negotiator.ForcedBattleMode : null;
    }

    BattleModeDefinition ResolveResolvedMode(PlayerController player, PlayerBattleModeSettings settings, BattleChallengeDefinition resolvedChallenge, BattleModeDefinition forcedMode) {
        if(forcedMode != null) {
            return forcedMode;
        }

        if(resolvedChallenge != null) {
            return resolvedChallenge.ResolveBattleMode(player, null);
        }

        return settings != null ? settings.SelectedBattleMode : null;
    }

    bool IsClassicOption(string optionId) {
        string id = NormalizeOptionId(optionId);
        return string.IsNullOrWhiteSpace(id)
            || string.Equals(id, ClassicOptionId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, "none", StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, "default", StringComparison.OrdinalIgnoreCase);
    }

    string NormalizeOptionId(string optionId) {
        return string.IsNullOrWhiteSpace(optionId) ? ClassicOptionId : optionId.Trim();
    }

    string ResolveSourceId() {
        return string.IsNullOrWhiteSpace(uiSourceId) ? "ui:battle-mode-options" : uiSourceId;
    }

    bool Succeed(BattleModeOptionsUIActionResultKind kind, string message, out string feedback) {
        feedback = message;
        SetLastResult(kind, true, message);
        if(logSuccessfulActions) {
            GameDebug.Success(message, GameDebugCategory.BattleRule, this, "BattleModeOptionsUIManager");
        }
        return true;
    }

    bool Block(string message, out string feedback) {
        feedback = string.IsNullOrWhiteSpace(message) ? "Battle mode option action was blocked." : message;
        SetLastResult(BattleModeOptionsUIActionResultKind.Blocked, false, feedback);
        if(logBlockedActions) {
            GameDebug.Warning(feedback, GameDebugCategory.BattleRule, this, "BattleModeOptionsUIManager");
        }
        return false;
    }

    void SetLastResult(BattleModeOptionsUIActionResultKind kind, bool success, string message) {
        lastResult = new BattleModeOptionsUIActionResult {
            kind = kind,
            success = success,
            message = message,
            day = TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1,
            hour = TimeSystem.i != null ? Mathf.Clamp(TimeSystem.i.Hour, 0, 23) : 0,
            absoluteHour = GetCurrentAbsoluteHour()
        };

        OnActionResult?.Invoke(lastResult);
        if(refreshAfterActions) {
            Refresh();
        }
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }
}

[Serializable]
public class BattleModeOptionsUIScreenSnapshot {
    [Tooltip("If enabled, a player was resolved for this snapshot.")]
    public bool hasPlayer;
    [Tooltip("Resolved player object name.")]
    public string playerName;
    [Tooltip("Source id used by this UI manager.")]
    public string sourceId;
    [Tooltip("If enabled, PlayerBattleModeSettings was found on the player.")]
    public bool hasSettings;
    [Tooltip("Saved preferred battle mode id. Empty means classic/current behavior.")]
    public string selectedModeId;
    [Tooltip("Saved preferred battle mode display name or classic fallback name.")]
    public string selectedModeName;
    [Tooltip("If enabled, battle challenges can use the saved preference when allowed.")]
    public bool preferSelectedModeForChallenges;
    [Tooltip("Resolved battle challenge id for this options context.")]
    public string challengeId;
    [Tooltip("Resolved battle challenge display name for this options context.")]
    public string challengeName;
    [Tooltip("Resolved negotiator object name for this options context.")]
    public string negotiatorName;
    [Tooltip("Forced battle mode id, if any.")]
    public string forcedModeId;
    [Tooltip("Forced battle mode display name, if any.")]
    public string forcedModeName;
    [Tooltip("Battle mode id that would currently resolve for this context.")]
    public string resolvedModeId;
    [Tooltip("Battle mode display name that would currently resolve for this context.")]
    public string resolvedModeName;
    [Tooltip("Visible option row count.")]
    public int optionCount;
    [Tooltip("Rows that can be selected right now.")]
    public int selectableCount;
    [Tooltip("Rows that are visible but blocked.")]
    public int lockedCount;
    [Tooltip("Current in-game day.")]
    public int day;
    [Tooltip("Current in-game hour.")]
    public int hour;
    [Tooltip("Current absolute in-game hour.")]
    public int absoluteHour;
    [Tooltip("Visible battle mode option rows.")]
    public List<BattleModeOptionRow> rows = new List<BattleModeOptionRow>();
    [Tooltip("Most recent UI backend action result.")]
    public BattleModeOptionsUIActionResult lastResult;
}

[Serializable]
public class BattleModeOptionsUIActionResult {
    [Tooltip("Kind of UI backend action that produced this result.")]
    public BattleModeOptionsUIActionResultKind kind;
    [Tooltip("If enabled, the action succeeded.")]
    public bool success;
    [Tooltip("Readable result, failure or feedback text.")]
    public string message;
    [Tooltip("In-game day when the result was produced.")]
    public int day;
    [Tooltip("In-game hour when the result was produced.")]
    public int hour;
    [Tooltip("Absolute in-game hour when the result was produced.")]
    public int absoluteHour;
}

[Serializable]
public class BattleModeOptionRow {
    [Tooltip("Option id used by UI select/preview actions. Classic option uses 'classic'.")]
    public string optionId;
    [Tooltip("Battle mode definition id. Empty for the built-in classic/clear option.")]
    public string modeId;
    [Tooltip("Display name shown by UI.")]
    public string displayName;
    [Tooltip("Description shown by UI.")]
    public string description;
    [Tooltip("Broad battle mode kind used by UI filters.")]
    public BattleModeKind kind;
    [Tooltip("Backend key used by future battle routing.")]
    public string battleSystemKey;
    [Tooltip("Free-form battle mode tags.")]
    public List<string> tags = new List<string>();
    [Tooltip("If enabled, this row clears the saved preference and uses classic/current behavior.")]
    public bool isClassicClearOption;
    [Tooltip("If enabled, this row is the currently saved player preference.")]
    public bool isSelectedPreference;
    [Tooltip("If enabled, this row would resolve for the current challenge/negotiator context.")]
    public bool isResolvedForContext;
    [Tooltip("If enabled, this row is the challenge default.")]
    public bool isDefaultForChallenge;
    [Tooltip("If enabled, this row is forced by the negotiator.")]
    public bool isForcedByNegotiator;
    [Tooltip("If enabled, this row is allowed by the active challenge context.")]
    public bool allowedByChallenge;
    [Tooltip("If enabled, the player passes this mode's access checks.")]
    public bool canAccess;
    [Tooltip("If enabled, the current battle system can run this mode or fall back safely.")]
    public bool canRunInCurrentBattleSystem;
    [Tooltip("If enabled, selecting this row is currently valid.")]
    public bool canSelect;
    [Tooltip("If enabled, this mode is implemented in the current BattleSystem.")]
    public bool implementedInCurrentBattleSystem;
    [Tooltip("If enabled, this mode can fall back to classic battle when unsupported.")]
    public bool allowFallbackToClassic;
    [Tooltip("Failure reason shown when Can Select is false.")]
    public string failureMessage;
    [Tooltip("Fallback message shown when a non-implemented mode can still use classic battle.")]
    public string fallbackMessage;
    [Tooltip("If enabled, this mode uses the old four-move selection limit.")]
    public bool usesFourMoveLimit;
    [Tooltip("If enabled, this mode expects a known-move command palette.")]
    public bool usesKnownMovePalette;
    [Tooltip("If enabled, this mode expects action points.")]
    public bool usesActionPoints;
    [Tooltip("If enabled, this mode expects stamina.")]
    public bool usesStamina;
    [Tooltip("If enabled, this mode expects elemental modifiers.")]
    public bool usesElementModifiers;
    [Tooltip("Suggested maximum visible action count for future battle UI.")]
    public int suggestedVisibleActionCount;
    [Tooltip("Short text useful for placeholder UI rows.")]
    public string displayText;

    public static BattleModeOptionRow FromClassic(
        string displayName,
        string description,
        PlayerBattleModeSettings settings,
        BattleChallengeDefinition challenge,
        BattleModeDefinition forcedMode,
        BattleModeDefinition resolvedMode) {
        bool forcedBlocked = forcedMode != null;
        return new BattleModeOptionRow {
            optionId = "classic",
            modeId = string.Empty,
            displayName = string.IsNullOrWhiteSpace(displayName) ? "Classic Battle" : displayName,
            description = description,
            kind = BattleModeKind.ClassicFourMove,
            battleSystemKey = "classic",
            tags = new List<string> { "classic" },
            isClassicClearOption = true,
            isSelectedPreference = settings == null || settings.SelectedBattleMode == null,
            isResolvedForContext = resolvedMode == null,
            isDefaultForChallenge = challenge != null && challenge.DefaultBattleMode == null,
            isForcedByNegotiator = false,
            allowedByChallenge = challenge == null || challenge.IsBattleModeAllowed(null, null),
            canAccess = true,
            canRunInCurrentBattleSystem = true,
            canSelect = !forcedBlocked,
            implementedInCurrentBattleSystem = true,
            allowFallbackToClassic = true,
            failureMessage = forcedBlocked ? $"{forcedMode.DisplayName} is forced for this battle context." : string.Empty,
            fallbackMessage = string.Empty,
            usesFourMoveLimit = true,
            suggestedVisibleActionCount = 4,
            displayText = forcedBlocked ? $"{displayName} - forced mode active" : $"{displayName} - current behavior"
        };
    }

    public static BattleModeOptionRow FromMode(
        BattleModeDefinition mode,
        PlayerController player,
        PlayerBattleModeSettings settings,
        BattleChallengeDefinition challenge,
        BattleModeDefinition forcedMode,
        BattleModeDefinition resolvedMode,
        string selectFailure) {
        string accessFailure = null;
        string runFailure = null;
        string fallbackMessage = null;
        bool canAccess = mode != null && mode.CanAccess(player, out accessFailure);
        bool canRun = mode != null && mode.CanRunWithCurrentBattleSystem(out runFailure, out fallbackMessage);
        bool allowedByChallenge = challenge == null || challenge.IsBattleModeAllowed(player, mode);
        bool forced = forcedMode != null && forcedMode == mode;
        string failure = !string.IsNullOrWhiteSpace(selectFailure)
            ? selectFailure
            : !canAccess ? accessFailure : !canRun ? runFailure : !allowedByChallenge ? $"{mode.DisplayName} is not allowed for {challenge.DisplayName}." : string.Empty;

        return new BattleModeOptionRow {
            optionId = mode != null ? mode.Id : string.Empty,
            modeId = mode != null ? mode.Id : string.Empty,
            displayName = mode != null ? mode.DisplayName : string.Empty,
            description = mode != null ? mode.Description : string.Empty,
            kind = mode != null ? mode.Kind : BattleModeKind.ClassicFourMove,
            battleSystemKey = mode != null ? mode.BattleSystemKey : "classic",
            tags = mode != null ? mode.Tags.ToList() : new List<string>(),
            isClassicClearOption = false,
            isSelectedPreference = settings != null && settings.SelectedBattleMode == mode,
            isResolvedForContext = resolvedMode == mode,
            isDefaultForChallenge = challenge != null && challenge.DefaultBattleMode == mode,
            isForcedByNegotiator = forced,
            allowedByChallenge = allowedByChallenge,
            canAccess = canAccess,
            canRunInCurrentBattleSystem = canRun,
            canSelect = string.IsNullOrWhiteSpace(failure),
            implementedInCurrentBattleSystem = mode != null && mode.ImplementedInCurrentBattleSystem,
            allowFallbackToClassic = mode != null && mode.AllowFallbackToClassic,
            failureMessage = failure,
            fallbackMessage = fallbackMessage,
            usesFourMoveLimit = mode != null && mode.UsesFourMoveLimit,
            usesKnownMovePalette = mode != null && mode.UsesKnownMovePalette,
            usesActionPoints = mode != null && mode.UsesActionPoints,
            usesStamina = mode != null && mode.UsesStamina,
            usesElementModifiers = mode != null && mode.UsesElementModifiers,
            suggestedVisibleActionCount = mode != null ? mode.SuggestedVisibleActionCount : 0,
            displayText = mode != null ? $"{mode.DisplayName} - {mode.Kind}" : string.Empty
        };
    }
}

[Serializable]
public class BattleModeOptionPreviewResult {
    [Tooltip("Option id that was previewed.")]
    public string optionId;
    [Tooltip("Display name of the previewed option.")]
    public string displayName;
    [Tooltip("If enabled, this option can be selected.")]
    public bool canSelect;
    [Tooltip("Readable preview, failure or fallback message.")]
    public string message;
    [Tooltip("If enabled, this option is implemented directly in the current BattleSystem.")]
    public bool implementedInCurrentBattleSystem;
    [Tooltip("If enabled, this option would fall back to classic battle.")]
    public bool willFallbackToClassic;
    [Tooltip("Suggested maximum visible action count for future battle UI.")]
    public int suggestedVisibleActionCount;

    public static BattleModeOptionPreviewResult FromRow(BattleModeOptionRow row) {
        string message = row == null
            ? "Battle mode option is unavailable."
            : row.canSelect
                ? !string.IsNullOrWhiteSpace(row.fallbackMessage) ? row.fallbackMessage : $"{row.displayName} can be selected."
                : row.failureMessage;

        return new BattleModeOptionPreviewResult {
            optionId = row != null ? row.optionId : string.Empty,
            displayName = row != null ? row.displayName : string.Empty,
            canSelect = row != null && row.canSelect,
            message = message,
            implementedInCurrentBattleSystem = row != null && row.implementedInCurrentBattleSystem,
            willFallbackToClassic = row != null && !row.implementedInCurrentBattleSystem && row.allowFallbackToClassic,
            suggestedVisibleActionCount = row != null ? row.suggestedVisibleActionCount : 0
        };
    }

    public static BattleModeOptionPreviewResult Blocked(string optionId, string message) {
        return new BattleModeOptionPreviewResult {
            optionId = optionId,
            displayName = string.Empty,
            canSelect = false,
            message = message
        };
    }
}
