using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AssignmentSourceType {
    General,
    PoliceStation,
    ProfessorLab,
    ResearchBoard,
    OrganizationDesk,
    NoticeBoard,
    NPC,
    Custom
}

public enum AssignmentSourceTriggerMode {
    RevealOnly,
    AutoAcceptFirstAvailable,
    AutoCompleteFirstReady
}

public class AssignmentSource : MonoBehaviour, IPlayerTriggerable {
    [Header("Source")]
    [Tooltip("Stable source id used by assignment logs. Empty uses GameObject name.")]
    [SerializeField] string sourceId;
    [Tooltip("Name shown in debug/future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName;
    [Tooltip("Broad source type used by filters and future UI.")]
    [SerializeField] AssignmentSourceType sourceType = AssignmentSourceType.General;
    [Tooltip("Assignments offered by this source.")]
    [SerializeField] List<AssignmentDefinition> assignments = new List<AssignmentDefinition>();

    [Header("Trigger")]
    [Tooltip("What this source does when the player triggers it without a UI.")]
    [SerializeField] AssignmentSourceTriggerMode triggerMode = AssignmentSourceTriggerMode.RevealOnly;
    [Tooltip("If enabled, triggering this source unlocks its listed assignments.")]
    [SerializeField] bool unlockAssignmentsOnTrigger = true;
    [Tooltip("If enabled, repeated player triggers can call OnPlayerTriggered more than once.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this source can be used.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional organization membership required before this source can be used.")]
    [SerializeField] OrganizationDefinition requiredOrganization;
    [Tooltip("Minimum organization rank index required for Required Organization.")]
    [Min(0)]
    [SerializeField] int requiredOrganizationRankIndex;
    [Tooltip("Optional career required before this source can be used.")]
    [SerializeField] CareerPathDefinition requiredCareer;
    [Tooltip("Minimum career rank index required for Required Career.")]
    [Min(0)]
    [SerializeField] int requiredCareerRankIndex;
    [Tooltip("Message shown when source access is blocked.")]
    [SerializeField] string lockedMessage = "This assignment source is not available right now.";

    [Header("Debug")]
    [Tooltip("If enabled, source attempts are written to GameDebug.")]
    [SerializeField] bool logAttempts;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public AssignmentSourceType SourceType => sourceType;
    public IReadOnlyList<AssignmentDefinition> Assignments => assignments;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        if(player == null) {
            return;
        }

        if(!CanUse(player, out var failureMessage)) {
            PublishSourceEvent(player, "blocked", failureMessage, GameEventImportance.Warning);
            return;
        }

        var log = player.GetComponent<PlayerAssignmentLog>() ?? player.gameObject.AddComponent<PlayerAssignmentLog>();
        if(unlockAssignmentsOnTrigger) {
            foreach(var assignment in assignments) {
                log.UnlockAssignment(assignment, SourceId);
            }
        }

        if(triggerMode == AssignmentSourceTriggerMode.AutoAcceptFirstAvailable) {
            TryAcceptFirstAvailable(player, out _);
        } else if(triggerMode == AssignmentSourceTriggerMode.AutoCompleteFirstReady) {
            TryCompleteFirstReady(player, out _);
        } else {
            PublishSourceEvent(player, "revealed", $"{DisplayName} has {GetAvailableAssignments(player).Count} available assignment(s).", GameEventImportance.Info);
        }
    }

    public bool CanUse(PlayerController player, out string failureMessage) {
        if(requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredOrganization != null && !(player?.GetComponent<PlayerOrganizationLog>()?.HasReachedRank(requiredOrganization, requiredOrganizationRankIndex) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more progress with {requiredOrganization.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredCareer != null && !(player?.GetComponent<PlayerCareerLog>()?.HasReachedRank(requiredCareer, requiredCareerRankIndex) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more progress in {requiredCareer.DisplayName}." : lockedMessage;
            return false;
        }

        failureMessage = null;
        return true;
    }

    public List<AssignmentDefinition> GetAvailableAssignments(PlayerController player) {
        if(player == null || !CanUse(player, out _)) {
            return new List<AssignmentDefinition>();
        }

        var log = player.GetComponent<PlayerAssignmentLog>();
        return (assignments ?? new List<AssignmentDefinition>())
            .Where(assignment => assignment != null && assignment.CanAccept(player, log, SourceId, out _))
            .OrderByDescending(assignment => assignment.Priority)
            .ThenBy(assignment => assignment.DisplayName)
            .ToList();
    }

    public List<AssignmentDefinition> GetCompletableAssignments(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerAssignmentLog>() : null;
        if(log == null) {
            return new List<AssignmentDefinition>();
        }

        return log.ActiveAssignments
            .Where(state => state != null && state.sourceId == SourceId)
            .Select(state => ResolveAssignment(state.assignmentId))
            .Where(assignment => assignment != null && assignment.CanComplete(player, log.GetActiveAssignment(assignment, SourceId), out _))
            .ToList();
    }

    public bool TryAccept(PlayerController player, AssignmentDefinition assignment, out string failureMessage) {
        if(!CanUse(player, out failureMessage)) {
            return false;
        }

        if(assignment == null || !assignments.Contains(assignment)) {
            failureMessage = "This assignment is not available from this source.";
            return false;
        }

        var log = player.GetComponent<PlayerAssignmentLog>() ?? player.gameObject.AddComponent<PlayerAssignmentLog>();
        bool accepted = log.Accept(assignment, SourceId, out failureMessage);
        if(accepted) {
            PublishSourceEvent(player, "accepted", $"{assignment.DisplayName} accepted.", GameEventImportance.Info);
        }

        return accepted;
    }

    public bool TryComplete(PlayerController player, AssignmentDefinition assignment, out string failureMessage) {
        if(!CanUse(player, out failureMessage)) {
            return false;
        }

        if(assignment == null || !assignments.Contains(assignment)) {
            failureMessage = "This assignment is not available from this source.";
            return false;
        }

        var log = player.GetComponent<PlayerAssignmentLog>() ?? player.gameObject.AddComponent<PlayerAssignmentLog>();
        bool completed = log.Complete(assignment, SourceId, out failureMessage);
        if(completed) {
            PublishSourceEvent(player, "completed", $"{assignment.DisplayName} completed.", GameEventImportance.Success);
        }

        return completed;
    }

    public bool TryAcceptFirstAvailable(PlayerController player, out string failureMessage) {
        var assignment = GetAvailableAssignments(player).FirstOrDefault();
        if(assignment == null) {
            failureMessage = "No assignment is available right now.";
            PublishSourceEvent(player, "empty", failureMessage, GameEventImportance.Trace);
            return false;
        }

        return TryAccept(player, assignment, out failureMessage);
    }

    public bool TryCompleteFirstReady(PlayerController player, out string failureMessage) {
        var assignment = GetCompletableAssignments(player).FirstOrDefault();
        if(assignment == null) {
            failureMessage = "No assignment is ready to complete right now.";
            PublishSourceEvent(player, "empty", failureMessage, GameEventImportance.Trace);
            return false;
        }

        return TryComplete(player, assignment, out failureMessage);
    }

    AssignmentDefinition ResolveAssignment(string assignmentId) {
        if(string.IsNullOrWhiteSpace(assignmentId)) {
            return null;
        }

        return Resources.LoadAll<AssignmentDefinition>("").FirstOrDefault(assignment => assignment != null && assignment.Id == assignmentId);
    }

    void PublishSourceEvent(PlayerController player, string phase, string message, GameEventImportance importance) {
        if(logAttempts) {
            GameDebug.Step(message, GameDebugCategory.Assignment, player != null ? player : this, "AssignmentSource");
        }

        GameEventPublishing.PublishOptional(
            null,
            $"assignment-source.{phase}.{SourceId}",
            message,
            GameEventCategory.Assignment,
            importance,
            player != null ? player : this,
            "AssignmentSource",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: logAttempts,
            GameEventPublishing.Value("sourceId", SourceId),
            GameEventPublishing.Value("sourceName", DisplayName),
            GameEventPublishing.Value("sourceType", sourceType),
            GameEventPublishing.Value("phase", phase));
    }
}
