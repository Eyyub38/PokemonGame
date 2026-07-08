using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class JobBoard : MonoBehaviour {
    [Header("Board")]
    [Tooltip("Board definition that controls access and available jobs.")]
    [SerializeField] JobBoardDefinition boardDefinition;
    [Tooltip("Optional save/id override for this board instance. Empty uses board definition id or GameObject name.")]
    [SerializeField] string boardInstanceId;
    [Tooltip("If enabled, the GameObject name is used when no explicit board id exists.")]
    [SerializeField] bool fallbackToGameObjectName = true;

    public JobBoardDefinition BoardDefinition => boardDefinition;
    public string BoardId {
        get {
            if(!string.IsNullOrWhiteSpace(boardInstanceId)) {
                return boardInstanceId;
            }

            if(boardDefinition != null) {
                return boardDefinition.Id;
            }

            return fallbackToGameObjectName ? name : "job-board";
        }
    }

    public bool CanUse(PlayerController player, out string failureMessage) {
        if(boardDefinition == null) {
            failureMessage = "No job board definition assigned.";
            return false;
        }

        return boardDefinition.IsUnlocked(player, out failureMessage);
    }

    public List<JobBoardOffer> GetAvailableOffers(PlayerController player) {
        if(boardDefinition == null || player == null) {
            return new List<JobBoardOffer>();
        }

        var log = player.GetComponent<PlayerJobLog>();
        return boardDefinition.GetAvailableOffers(player, BoardId, log);
    }

    public bool CanAccept(PlayerController player, JobBoardOffer offer, out string failureMessage) {
        if(!CanUse(player, out failureMessage)) {
            return false;
        }

        if(player == null) {
            failureMessage = "A player is required to accept jobs.";
            return false;
        }

        var log = player.GetComponent<PlayerJobLog>();
        if(log == null) {
            failureMessage = "The player has no job log.";
            return false;
        }

        return offer != null && offer.CanAccept(player, log, BoardId, out failureMessage);
    }

    public bool TryAccept(PlayerController player, JobBoardOffer offer, out string failureMessage) {
        if(!CanAccept(player, offer, out failureMessage)) {
            return false;
        }

        bool accepted = player.GetComponent<PlayerJobLog>().Accept(offer.Job, BoardId, out failureMessage);
        if(accepted) {
            PublishBoardEvent("accepted", player, offer.Job);
        }

        return accepted;
    }

    public bool TryComplete(PlayerController player, JobDefinition job, out string failureMessage) {
        if(!CanUse(player, out failureMessage)) {
            return false;
        }

        if(player == null) {
            failureMessage = "A player is required to complete jobs.";
            return false;
        }

        var log = player.GetComponent<PlayerJobLog>();
        if(log == null) {
            failureMessage = "The player has no job log.";
            return false;
        }

        bool completed = log.Complete(job, BoardId, out failureMessage);
        if(completed) {
            PublishBoardEvent("completed", player, job);
        }

        return completed;
    }

    public List<JobDefinition> GetCompletableJobs(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerJobLog>() : null;
        if(log == null) {
            return new List<JobDefinition>();
        }

        return log.ActiveJobs
            .Where(j => j != null && j.boardId == BoardId)
            .Select(j => ResolveJob(j.jobId))
            .Where(j => j != null && j.IsCompleted(player, log.GetActiveJob(j, BoardId), out _))
            .ToList();
    }

    JobDefinition ResolveJob(string jobId) {
        if(string.IsNullOrWhiteSpace(jobId)) {
            return null;
        }

        return Resources.LoadAll<JobDefinition>("").FirstOrDefault(j => j != null && j.Id == jobId);
    }

    void PublishBoardEvent(string phase, PlayerController player, JobDefinition job) {
        GameEventPublishing.PublishOptional(
            null,
            $"job-board.{phase}.{BoardId}.{job?.Id}",
            $"{job?.DisplayName ?? "Job"} {phase} at {boardDefinition?.DisplayName ?? BoardId}.",
            GameEventCategory.Job,
            GameEventImportance.Info,
            player != null ? player : this,
            "JobBoard",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: false,
            GameEventPublishing.Value("boardId", BoardId),
            GameEventPublishing.Value("boardName", boardDefinition != null ? boardDefinition.DisplayName : name),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("jobId", job != null ? job.Id : string.Empty),
            GameEventPublishing.Value("jobName", job != null ? job.DisplayName : string.Empty));
    }
}
