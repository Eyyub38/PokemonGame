using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum InvestigationSourceType {
    General,
    PoliceDesk,
    ProfessorLab,
    EvidenceObject,
    Witness,
    SceneLocation,
    ResearchStation,
    NoticeBoard,
    NPC,
    Custom
}

public enum InvestigationSourceTriggerMode {
    RevealOnly,
    AutoStartFirstAvailable,
    DiscoverClue,
    CompleteFirstReady
}

public class InvestigationSource : MonoBehaviour, IPlayerTriggerable {
    [Header("Source")]
    [Tooltip("Stable source id used by investigation logs. Empty uses GameObject name.")]
    [SerializeField] string sourceId;
    [Tooltip("Name shown in debug and future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName;
    [Tooltip("Broad source type used by filters and future UI.")]
    [SerializeField] InvestigationSourceType sourceType = InvestigationSourceType.General;
    [Tooltip("Cases offered or completed by this source.")]
    [SerializeField] List<InvestigationCaseDefinition> cases = new List<InvestigationCaseDefinition>();
    [Tooltip("Case used by Discover Clue mode.")]
    [SerializeField] InvestigationCaseDefinition clueCase;
    [Tooltip("Clue discovered by this source when Trigger Mode is Discover Clue.")]
    [SerializeField] InvestigationClueDefinition clue;

    [Header("Trigger")]
    [Tooltip("What this source does when the player triggers it without a UI.")]
    [SerializeField] InvestigationSourceTriggerMode triggerMode = InvestigationSourceTriggerMode.RevealOnly;
    [Tooltip("If enabled, triggering this source unlocks its listed cases.")]
    [SerializeField] bool unlockCasesOnTrigger = true;
    [Tooltip("If enabled, repeated player triggers can call OnPlayerTriggered more than once.")]
    [SerializeField] bool triggerRepeatedly = true;
    [Tooltip("If enabled, a PlayerInvestigationLog component is added to the player when missing.")]
    [SerializeField] bool installLogIfMissing = true;

    [Header("Access")]
    [Tooltip("Optional access profile required before this source can be used.")]
    [SerializeField] AccessProfileDefinition requiredAccessProfile;
    [Tooltip("Message shown when source access is blocked.")]
    [SerializeField] string lockedMessage = "This investigation source is not available right now.";

    [Header("Debug")]
    [Tooltip("If enabled, source attempts are written to GameDebug.")]
    [SerializeField] bool logAttempts;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public InvestigationSourceType SourceType => sourceType;
    public IReadOnlyList<InvestigationCaseDefinition> Cases => cases;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        if(player == null) {
            return;
        }

        if(!CanUse(player, out var failureMessage)) {
            PublishSourceEvent(player, "blocked", failureMessage, GameEventImportance.Warning);
            return;
        }

        var log = GetOrInstallLog(player);
        if(unlockCasesOnTrigger) {
            foreach(var investigationCase in cases) {
                log?.UnlockCase(investigationCase, SourceId);
            }
        }

        if(triggerMode == InvestigationSourceTriggerMode.AutoStartFirstAvailable) {
            TryStartFirstAvailable(player, out _);
        } else if(triggerMode == InvestigationSourceTriggerMode.DiscoverClue) {
            TryDiscoverClue(player, clueCase, clue, out _);
        } else if(triggerMode == InvestigationSourceTriggerMode.CompleteFirstReady) {
            TryCompleteFirstReady(player, out _);
        } else {
            PublishSourceEvent(player, "revealed", $"{DisplayName} has {GetStartableCases(player).Count} startable case(s).", GameEventImportance.Info);
        }
    }

    public bool CanUse(PlayerController player, out string failureMessage) {
        if(requiredAccessProfile != null && !requiredAccessProfile.CanAccess(player, out failureMessage)) {
            if(string.IsNullOrWhiteSpace(failureMessage)) {
                failureMessage = lockedMessage;
            }
            return false;
        }

        failureMessage = null;
        return true;
    }

    public List<InvestigationCaseDefinition> GetStartableCases(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerInvestigationLog>() : null;
        return (cases ?? new List<InvestigationCaseDefinition>())
            .Where(investigationCase => investigationCase != null && investigationCase.CanStart(player, log, out _))
            .OrderByDescending(investigationCase => investigationCase.Priority)
            .ThenBy(investigationCase => investigationCase.DisplayName)
            .ToList();
    }

    public List<InvestigationCaseDefinition> GetCompletableCases(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerInvestigationLog>() : null;
        if(log == null) {
            return new List<InvestigationCaseDefinition>();
        }

        return (cases ?? new List<InvestigationCaseDefinition>())
            .Where(investigationCase => investigationCase != null && investigationCase.CanComplete(player, log.GetActiveCase(investigationCase), out _))
            .OrderByDescending(investigationCase => investigationCase.Priority)
            .ThenBy(investigationCase => investigationCase.DisplayName)
            .ToList();
    }

    public bool TryStart(PlayerController player, InvestigationCaseDefinition investigationCase, out string failureMessage) {
        if(!CanUse(player, out failureMessage)) {
            return false;
        }

        if(investigationCase == null || !cases.Contains(investigationCase)) {
            failureMessage = "This investigation case is not available from this source.";
            return false;
        }

        bool started = GetOrInstallLog(player)?.StartCase(investigationCase, SourceId, out failureMessage) ?? false;
        if(started) {
            PublishSourceEvent(player, "started", $"{investigationCase.DisplayName} started.", GameEventImportance.Info);
        }

        return started;
    }

    public bool TryDiscoverClue(PlayerController player, InvestigationCaseDefinition investigationCase, InvestigationClueDefinition investigationClue, out string failureMessage) {
        if(!CanUse(player, out failureMessage)) {
            return false;
        }

        if(investigationCase == null || investigationClue == null) {
            failureMessage = "No investigation case or clue selected.";
            return false;
        }

        bool discovered = GetOrInstallLog(player)?.DiscoverClue(investigationCase, investigationClue, SourceId, out failureMessage) ?? false;
        if(discovered) {
            PublishSourceEvent(player, "clue", $"{investigationClue.DisplayName} discovered.", GameEventImportance.Success);
        }

        return discovered;
    }

    public bool TryComplete(PlayerController player, InvestigationCaseDefinition investigationCase, out string failureMessage) {
        if(!CanUse(player, out failureMessage)) {
            return false;
        }

        if(investigationCase == null || !cases.Contains(investigationCase)) {
            failureMessage = "This investigation case is not available from this source.";
            return false;
        }

        bool completed = GetOrInstallLog(player)?.CompleteCase(investigationCase, SourceId, out failureMessage) ?? false;
        if(completed) {
            PublishSourceEvent(player, "completed", $"{investigationCase.DisplayName} completed.", GameEventImportance.Success);
        }

        return completed;
    }

    public bool TryStartFirstAvailable(PlayerController player, out string failureMessage) {
        var investigationCase = GetStartableCases(player).FirstOrDefault();
        if(investigationCase == null) {
            failureMessage = "No investigation case is available right now.";
            PublishSourceEvent(player, "empty", failureMessage, GameEventImportance.Trace);
            return false;
        }

        return TryStart(player, investigationCase, out failureMessage);
    }

    public bool TryCompleteFirstReady(PlayerController player, out string failureMessage) {
        var investigationCase = GetCompletableCases(player).FirstOrDefault();
        if(investigationCase == null) {
            failureMessage = "No investigation case is ready to complete right now.";
            PublishSourceEvent(player, "empty", failureMessage, GameEventImportance.Trace);
            return false;
        }

        return TryComplete(player, investigationCase, out failureMessage);
    }

    PlayerInvestigationLog GetOrInstallLog(PlayerController player) {
        if(player == null) {
            return null;
        }

        var log = player.GetComponent<PlayerInvestigationLog>();
        if(log == null && installLogIfMissing) {
            log = player.gameObject.AddComponent<PlayerInvestigationLog>();
        }
        return log;
    }

    void PublishSourceEvent(PlayerController player, string phase, string message, GameEventImportance importance) {
        if(logAttempts) {
            GameDebug.Step(message, GameDebugCategory.Investigation, player != null ? player : this, "InvestigationSource");
        }

        GameEventPublishing.PublishOptional(
            null,
            $"investigation-source.{phase}.{SourceId}",
            message,
            GameEventCategory.Investigation,
            importance,
            player != null ? player : this,
            "InvestigationSource",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: logAttempts,
            GameEventPublishing.Value("sourceId", SourceId),
            GameEventPublishing.Value("sourceName", DisplayName),
            GameEventPublishing.Value("sourceType", sourceType),
            GameEventPublishing.Value("phase", phase));
    }
}
