using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum NewGameSetupCategory {
    General,
    Trainer,
    Researcher,
    Farmer,
    Caretaker,
    Ranger,
    Merchant,
    Performer,
    Investigator,
    Challenge,
    Custom
}

[CreateAssetMenu(menuName = "New Game/Setup Definition")]
public class NewGameSetupDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id saved into PlayerNewGameSetupLog. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future New Game UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer/player-facing summary of this starting setup.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad setup group used by future New Game UI filters.")]
    [SerializeField] NewGameSetupCategory category = NewGameSetupCategory.General;
    [Tooltip("Free-form tags such as beginner, hard-mode, professor, farm, ranger, classic or command-palette.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Optional icon used by future New Game UI.")]
    [SerializeField] Sprite icon = null;

    [Header("Selection")]
    [Tooltip("If enabled, this setup can replace a previously applied new-game setup when force replace is used.")]
    [SerializeField] bool allowReplacingExistingSetup;
    [Tooltip("Reusable requirements checked before this setup can be applied.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message shown when this setup is blocked.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This starting setup is not available.";

    [Header("Origin")]
    [Tooltip("Origin package applied by this new-game setup.")]
    [SerializeField] PlayerOriginDefinition origin = null;
    [Tooltip("If enabled, the setup can replace the player's existing origin when allowed by the origin.")]
    [SerializeField] bool forceReplaceOrigin;

    [Header("Customization")]
    [Tooltip("Customization preset applied to the player at game start.")]
    [SerializeField] CustomizationPresetDefinition customizationPreset = null;
    [Tooltip("Additional customization parts unlocked and equipped at game start.")]
    [SerializeField] List<CustomizationPartDefinition> customizationParts = new List<CustomizationPartDefinition>();
    [Tooltip("If enabled, preset default parts replace current parts.")]
    [SerializeField] bool replaceCustomizationParts = true;
    [Tooltip("If enabled, preset parts and explicit parts are unlocked before equipping.")]
    [SerializeField] bool unlockCustomizationBeforeApply = true;

    [Header("Battle")]
    [Tooltip("Preferred battle mode selected at game start. Empty uses classic/current behavior.")]
    [SerializeField] BattleModeDefinition battleMode = null;
    [Tooltip("If enabled, challenge battles can use this selected battle mode when allowed.")]
    [SerializeField] bool preferBattleModeForChallenges = true;
    [Tooltip("Battle rule sets unlocked at game start.")]
    [SerializeField] List<BattleRuleSetDefinition> unlockedBattleRuleSets = new List<BattleRuleSetDefinition>();

    [Header("Lifestyle")]
    [Tooltip("Lifestyle points granted at game start.")]
    [SerializeField] List<LifestylePointGrant> lifestyleGrants = new List<LifestylePointGrant>();

    [Header("Events")]
    [Tooltip("Optional event published when this setup is applied. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition appliedEvent = null;
    [Tooltip("Optional event published when this setup is blocked. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition blockedEvent = null;
    [Tooltip("If enabled, setup events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, setup events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public NewGameSetupCategory Category => category;
    public IReadOnlyList<string> Tags => tags != null ? tags : Array.Empty<string>();
    public Sprite Icon => icon;
    public bool AllowReplacingExistingSetup => allowReplacingExistingSetup;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? requirements : Array.Empty<ActivityRequirement>();
    public PlayerOriginDefinition Origin => origin;
    public CustomizationPresetDefinition CustomizationPreset => customizationPreset;
    public IReadOnlyList<CustomizationPartDefinition> CustomizationParts => customizationParts != null ? customizationParts : Array.Empty<CustomizationPartDefinition>();
    public BattleModeDefinition BattleMode => battleMode;
    public IReadOnlyList<BattleRuleSetDefinition> UnlockedBattleRuleSets => unlockedBattleRuleSets != null ? unlockedBattleRuleSets : Array.Empty<BattleRuleSetDefinition>();
    public IReadOnlyList<LifestylePointGrant> LifestyleGrants => lifestyleGrants != null ? lifestyleGrants : Array.Empty<LifestylePointGrant>();

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public bool CanApply(PlayerController player, PlayerNewGameSetupLog log, bool forceReplace, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to apply a new-game setup.";
            return false;
        }

        if(log != null && log.HasAppliedSetup && !forceReplace) {
            failureMessage = $"Player already has new-game setup {log.SetupName}.";
            return false;
        }

        if(log != null && log.HasAppliedSetup && forceReplace && !allowReplacingExistingSetup) {
            failureMessage = $"{DisplayName} cannot replace an existing setup.";
            return false;
        }

        foreach(var requirement in Requirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = string.IsNullOrWhiteSpace(requirement.FailureMessage) ? lockedMessage : requirement.FailureMessage;
                return false;
            }
        }

        if(battleMode != null && !battleMode.CanAccess(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public NewGameSetupApplyResult Apply(PlayerController player, string sourceId = null, string sourceName = null, UnityEngine.Object context = null, bool forceReplace = false) {
        var result = new NewGameSetupApplyResult(Id, DisplayName, category, ResolveSourceId(sourceId), string.IsNullOrWhiteSpace(sourceName) ? DisplayName : sourceName);
        var log = player != null ? player.GetComponent<PlayerNewGameSetupLog>() ?? player.gameObject.AddComponent<PlayerNewGameSetupLog>() : null;

        if(!CanApply(player, log, forceReplace, out var failureMessage)) {
            result.blocked = true;
            result.failureMessage = failureMessage;
            log?.RecordBlocked(this, result);
            PublishSetupEvent(blockedEvent, "blocked", result, context, GameEventImportance.Warning);
            return result;
        }

        ApplyOrigin(player, result, context);
        ApplyCustomization(player, result);
        ApplyBattlePreferences(player, result);
        ApplyLifestyles(player, result, context);

        log?.RecordApplied(this, result, forceReplace);
        PublishSetupEvent(appliedEvent, "applied", result, context, GameEventImportance.Success);
        return result;
    }

    void ApplyOrigin(PlayerController player, NewGameSetupApplyResult result, UnityEngine.Object context) {
        if(origin == null) {
            return;
        }

        var originResult = origin.Apply(player, result.sourceId, result.sourceName, context != null ? context : this, forceReplaceOrigin);
        result.originApplied = originResult != null && !originResult.blocked;
        result.originId = origin.Id;
        result.originName = origin.DisplayName;
        if(originResult != null) {
            result.pokemonGranted = originResult.pokemonGrants;
            result.itemsGranted = originResult.itemGrants;
            result.moneyGranted = originResult.moneyGranted;
            if(originResult.blocked && !string.IsNullOrWhiteSpace(originResult.failureMessage)) {
                result.messages.Add(originResult.failureMessage);
            }
        }
    }

    void ApplyCustomization(PlayerController player, NewGameSetupApplyResult result) {
        if(customizationPreset == null && CustomizationParts.Count == 0) {
            return;
        }

        var customization = player.GetComponent<PlayerCustomization>() ?? player.gameObject.AddComponent<PlayerCustomization>();
        if(customizationPreset != null) {
            if(unlockCustomizationBeforeApply) {
                customization.UnlockPreset(customizationPreset, Id);
            }

            if(customization.ApplyPreset(customizationPreset, replaceCustomizationParts, unlockCustomizationBeforeApply, out var failure)) {
                result.customizationPresetApplied = true;
                result.customizationPresetId = customizationPreset.Id;
                result.customizationPresetName = customizationPreset.DisplayName;
            } else {
                result.messages.Add(failure);
            }
        }

        foreach(var part in CustomizationParts) {
            if(part == null) {
                result.skippedCustomizationParts++;
                continue;
            }

            if(unlockCustomizationBeforeApply) {
                customization.UnlockPart(part, Id);
            }

            if(customization.EquipPart(part, out var failure)) {
                result.customizationPartsEquipped++;
            } else {
                result.messages.Add(failure);
            }
        }
    }

    void ApplyBattlePreferences(PlayerController player, NewGameSetupApplyResult result) {
        if(battleMode != null) {
            var settings = player.GetComponent<PlayerBattleModeSettings>() ?? player.gameObject.AddComponent<PlayerBattleModeSettings>();
            if(settings.SetBattleMode(battleMode, out var failure)) {
                settings.SetPreferSelectedModeForChallenges(preferBattleModeForChallenges);
                result.battleModeId = battleMode.Id;
                result.battleModeName = battleMode.DisplayName;
                result.battleModeApplied = true;
            } else {
                result.messages.Add(failure);
            }
        }

        if(UnlockedBattleRuleSets.Count == 0) {
            return;
        }

        var ruleLog = player.GetComponent<PlayerBattleRuleLog>() ?? player.gameObject.AddComponent<PlayerBattleRuleLog>();
        foreach(var ruleSet in UnlockedBattleRuleSets) {
            if(ruleSet != null && ruleLog.UnlockRuleSet(ruleSet, Id)) {
                result.battleRuleSetsUnlocked++;
            }
        }
    }

    void ApplyLifestyles(PlayerController player, NewGameSetupApplyResult result, UnityEngine.Object context) {
        if(LifestyleGrants.Count == 0) {
            return;
        }

        var log = player.GetComponent<PlayerLifestyleLog>() ?? player.gameObject.AddComponent<PlayerLifestyleLog>();
        log.ApplyGrants(LifestyleGrants, Id, DisplayName, context != null ? context : this);
        result.lifestyleGrantsApplied = LifestyleGrants.Count(grant => grant != null && grant.lifestyle != null && grant.points != 0);
    }

    string ResolveSourceId(string sourceId) {
        return !string.IsNullOrWhiteSpace(sourceId) ? sourceId : $"new-game:{Id}";
    }

    void PublishSetupEvent(GameEventDefinition eventDefinition, string phase, NewGameSetupApplyResult result, UnityEngine.Object context, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"new-game.setup.{phase}.{Id}",
            $"{DisplayName} new-game setup {phase}.",
            GameEventCategory.RPG,
            importance,
            context != null ? context : this,
            "NewGameSetupDefinition",
            GameEventScope.Player,
            showEventsInFeed,
            writeEventsToDebugLog,
            GameEventPublishing.Value("setupId", Id),
            GameEventPublishing.Value("setupName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("blocked", result != null && result.blocked),
            GameEventPublishing.Value("sourceId", result?.sourceId),
            GameEventPublishing.Value("sourceName", result?.sourceName));
    }
}

public class PlayerNewGameSetupLog : MonoBehaviour, ISavable {
    [Tooltip("Saved/current new-game setup id.")]
    [SerializeField] string setupId = string.Empty;
    [Tooltip("Saved/current new-game setup display name.")]
    [SerializeField] string setupName = string.Empty;
    [Tooltip("Saved/current new-game setup category.")]
    [SerializeField] NewGameSetupCategory category = NewGameSetupCategory.General;
    [Tooltip("Saved/current new-game setup tags.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Total in-game hour when this setup was applied.")]
    [SerializeField] int appliedAtHour = -1;
    [Tooltip("Runtime/save history of new-game setup attempts.")]
    [SerializeField] List<NewGameSetupRecord> records = new List<NewGameSetupRecord>();

    public string SetupId => setupId;
    public string SetupName => setupName;
    public NewGameSetupCategory Category => category;
    public IReadOnlyList<string> Tags => tags;
    public int AppliedAtHour => appliedAtHour;
    public IReadOnlyList<NewGameSetupRecord> Records => records;
    public bool HasAppliedSetup => !string.IsNullOrWhiteSpace(setupId);
    public event Action<NewGameSetupRecord> OnSetupApplied;
    public event Action<NewGameSetupRecord> OnSetupBlocked;
    public event Action OnSetupChanged;

    public NewGameSetupApplyResult ApplySetup(NewGameSetupDefinition setup, string sourceId = null, string sourceName = null, UnityEngine.Object context = null, bool forceReplace = false) {
        if(setup == null) {
            var result = new NewGameSetupApplyResult(string.Empty, string.Empty, NewGameSetupCategory.General, sourceId, sourceName) {
                blocked = true,
                failureMessage = "No new-game setup selected."
            };
            RecordBlocked(null, result);
            return result;
        }

        return setup.Apply(GetComponent<PlayerController>(), sourceId, sourceName, context != null ? context : this, forceReplace);
    }

    public NewGameSetupRecord RecordApplied(NewGameSetupDefinition setup, NewGameSetupApplyResult result, bool replacedExisting) {
        if(setup == null || result == null) {
            return null;
        }

        setupId = setup.Id;
        setupName = setup.DisplayName;
        category = setup.Category;
        tags = setup.Tags?.Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
        appliedAtHour = GetCurrentTotalHour();

        var record = CreateRecord(setup, result, replacedExisting ? "replaced" : "applied");
        records.Add(record);
        OnSetupApplied?.Invoke(record);
        OnSetupChanged?.Invoke();
        return record;
    }

    public NewGameSetupRecord RecordBlocked(NewGameSetupDefinition setup, NewGameSetupApplyResult result) {
        var record = CreateRecord(setup, result, "blocked");
        records.Add(record);
        OnSetupBlocked?.Invoke(record);
        OnSetupChanged?.Invoke();
        return record;
    }

    NewGameSetupRecord CreateRecord(NewGameSetupDefinition setup, NewGameSetupApplyResult result, string status) {
        return new NewGameSetupRecord {
            setupId = setup != null ? setup.Id : result?.setupId,
            setupName = setup != null ? setup.DisplayName : result?.setupName,
            category = setup != null ? setup.Category : result != null ? result.category : NewGameSetupCategory.General,
            status = status,
            sourceId = result?.sourceId,
            sourceName = result?.sourceName,
            originId = result?.originId,
            battleModeId = result?.battleModeId,
            customizationPresetId = result?.customizationPresetId,
            failureMessage = result?.failureMessage,
            recordedAtHour = GetCurrentTotalHour(),
            frame = Time.frameCount
        };
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerNewGameSetupLogSaveData {
            setupId = setupId,
            setupName = setupName,
            category = category,
            tags = tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            appliedAtHour = appliedAtHour,
            records = records.Where(record => record != null).Select(record => new NewGameSetupRecord(record)).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerNewGameSetupLogSaveData;
        if(saveData == null) {
            return;
        }

        setupId = saveData.setupId;
        setupName = saveData.setupName;
        category = saveData.category;
        tags = saveData.tags?.Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
        appliedAtHour = saveData.appliedAtHour;
        records = saveData.records?.Where(record => record != null).Select(record => new NewGameSetupRecord(record)).ToList() ?? new List<NewGameSetupRecord>();
        OnSetupChanged?.Invoke();
    }
}

public class NewGameSetupSource : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Setup")]
    [Tooltip("New-game setup applied by this source.")]
    [SerializeField] NewGameSetupDefinition setup = null;
    [Tooltip("Player used by context-menu actions. Empty uses PlayerController.i.")]
    [SerializeField] PlayerController playerOverride = null;
    [Tooltip("If enabled, this source can replace an existing setup when the setup allows it.")]
    [SerializeField] bool forceReplace;
    [Tooltip("If enabled, trigger volumes may run this source repeatedly.")]
    [SerializeField] bool triggerRepeatedly;
    [Tooltip("Specific source id saved into setup/origin/lifestyle history. Empty uses this object name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Specific source name saved into setup/origin/lifestyle history. Empty uses this object name.")]
    [SerializeField] string sourceName = string.Empty;
    [Tooltip("If enabled, results are written to GameDebug.")]
    [SerializeField] bool writeDebugLog;

    public NewGameSetupDefinition Setup => setup;
    public bool ForceReplace => forceReplace;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public IEnumerator Interact(Transform initiator) {
        TryApply(initiator != null ? initiator.GetComponent<PlayerController>() : ResolvePlayer(), out _);
        yield break;
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(triggerRepeatedly) {
            TryApply(player != null ? player : ResolvePlayer(), out _);
        }
    }

    [ContextMenu("Apply New Game Setup")]
    public void ApplyFromContextMenu() {
        TryApply(ResolvePlayer(), out _);
    }

    public bool TryApply(PlayerController player, out string feedback) {
        if(setup == null) {
            feedback = "New-game setup is missing.";
            WriteDebug(feedback, true);
            return false;
        }

        if(player == null) {
            feedback = "Player is missing.";
            WriteDebug(feedback, true);
            return false;
        }

        var result = setup.Apply(player, ResolveSourceId(), ResolveSourceName(), this, forceReplace);
        feedback = result.blocked
            ? result.failureMessage
            : $"{setup.DisplayName} applied.";
        WriteDebug(feedback, result.blocked);
        return !result.blocked;
    }

    string ResolveSourceId() {
        return !string.IsNullOrWhiteSpace(sourceId) ? sourceId : name;
    }

    string ResolveSourceName() {
        return !string.IsNullOrWhiteSpace(sourceName) ? sourceName : name;
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        return PlayerController.i != null ? PlayerController.i : FindAnyObjectByType<PlayerController>();
    }

    void WriteDebug(string message, bool warning) {
        if(!writeDebugLog || string.IsNullOrWhiteSpace(message)) {
            return;
        }

        if(warning) {
            GameDebug.Warning(message, GameDebugCategory.RPG, this, "NewGameSetupSource");
        } else {
            GameDebug.Success(message, GameDebugCategory.RPG, this, "NewGameSetupSource");
        }
    }
}

[Serializable]
public class NewGameSetupApplyResult {
    [Tooltip("Setup id.")]
    public string setupId;
    [Tooltip("Setup display name.")]
    public string setupName;
    [Tooltip("Setup category.")]
    public NewGameSetupCategory category;
    [Tooltip("Source id that requested setup application.")]
    public string sourceId;
    [Tooltip("Source display name that requested setup application.")]
    public string sourceName;
    [Tooltip("If true, this setup attempt was blocked.")]
    public bool blocked;
    [Tooltip("Failure message for blocked attempts.")]
    public string failureMessage;
    [Tooltip("Origin id applied by this setup.")]
    public string originId;
    [Tooltip("Origin display name applied by this setup.")]
    public string originName;
    [Tooltip("True when the origin package was applied.")]
    public bool originApplied;
    [Tooltip("Battle mode id applied by this setup.")]
    public string battleModeId;
    [Tooltip("Battle mode display name applied by this setup.")]
    public string battleModeName;
    [Tooltip("True when battle mode preference was applied.")]
    public bool battleModeApplied;
    [Tooltip("Customization preset id applied by this setup.")]
    public string customizationPresetId;
    [Tooltip("Customization preset display name applied by this setup.")]
    public string customizationPresetName;
    [Tooltip("True when customization preset was applied.")]
    public bool customizationPresetApplied;
    [Tooltip("Explicit customization parts equipped.")]
    public int customizationPartsEquipped;
    [Tooltip("Explicit customization parts skipped.")]
    public int skippedCustomizationParts;
    [Tooltip("Battle rule sets unlocked.")]
    public int battleRuleSetsUnlocked;
    [Tooltip("Lifestyle grants applied.")]
    public int lifestyleGrantsApplied;
    [Tooltip("Pokemon granted by the origin package.")]
    public int pokemonGranted;
    [Tooltip("Items granted by the origin package.")]
    public int itemsGranted;
    [Tooltip("Money granted by the origin package.")]
    public float moneyGranted;
    [Tooltip("Additional messages collected while applying this setup.")]
    public List<string> messages = new List<string>();

    public NewGameSetupApplyResult(string setupId, string setupName, NewGameSetupCategory category, string sourceId, string sourceName) {
        this.setupId = setupId;
        this.setupName = setupName;
        this.category = category;
        this.sourceId = sourceId;
        this.sourceName = sourceName;
    }
}

[Serializable]
public class NewGameSetupRecord {
    [Tooltip("Setup id.")]
    public string setupId;
    [Tooltip("Setup display name.")]
    public string setupName;
    [Tooltip("Setup category.")]
    public NewGameSetupCategory category;
    [Tooltip("Status such as applied, replaced or blocked.")]
    public string status;
    [Tooltip("Source id that requested this setup.")]
    public string sourceId;
    [Tooltip("Source display name that requested this setup.")]
    public string sourceName;
    [Tooltip("Origin id applied by this setup.")]
    public string originId;
    [Tooltip("Battle mode id applied by this setup.")]
    public string battleModeId;
    [Tooltip("Customization preset id applied by this setup.")]
    public string customizationPresetId;
    [Tooltip("Failure message for blocked attempts.")]
    public string failureMessage;
    [Tooltip("Total in-game hour when this record was created.")]
    public int recordedAtHour;
    [Tooltip("Unity frame when this record was created.")]
    public int frame;

    public NewGameSetupRecord() {
    }

    public NewGameSetupRecord(NewGameSetupRecord other) {
        setupId = other.setupId;
        setupName = other.setupName;
        category = other.category;
        status = other.status;
        sourceId = other.sourceId;
        sourceName = other.sourceName;
        originId = other.originId;
        battleModeId = other.battleModeId;
        customizationPresetId = other.customizationPresetId;
        failureMessage = other.failureMessage;
        recordedAtHour = other.recordedAtHour;
        frame = other.frame;
    }
}

[Serializable]
public class PlayerNewGameSetupLogSaveData {
    public string setupId;
    public string setupName;
    public NewGameSetupCategory category;
    public List<string> tags;
    public int appliedAtHour;
    public List<NewGameSetupRecord> records;
}
