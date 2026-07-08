using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum JobBoardType {
    General,
    PoliceStation,
    ResearchBoard,
    TownBoard,
    GuildBoard,
    FarmBoard,
    TransitBoard,
    ShopBoard
}

[CreateAssetMenu(menuName = "Jobs/Job Board Definition")]
public class JobBoardDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this board. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this board.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad board type used by filters and future UI.")]
    [SerializeField] JobBoardType boardType = JobBoardType.General;
    [Tooltip("Free-form tags used by access rules and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Access")]
    [Tooltip("Optional title, badge or permit required to use this board.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional faction whose reputation gates this board.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Message shown when board access is blocked.")]
    [SerializeField] string lockedMessage = "This board is not available yet.";

    [Header("Offers")]
    [Tooltip("Jobs offered by this board.")]
    [SerializeField] List<JobBoardOffer> offers = new List<JobBoardOffer>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public JobBoardType BoardType => boardType;
    public IReadOnlyList<string> Tags => tags;
    public IReadOnlyList<JobBoardOffer> Offers => offers;

    public bool IsUnlocked(PlayerController player, out string failureMessage) {
        if(requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredFaction != null) {
            int reputation = player?.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0;
            if(reputation < requiredReputation) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more reputation with {requiredFaction.DisplayName}." : lockedMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public List<JobBoardOffer> GetAvailableOffers(PlayerController player, string boardId, PlayerJobLog log) {
        if(!IsUnlocked(player, out _)) {
            return new List<JobBoardOffer>();
        }

        return (offers ?? new List<JobBoardOffer>())
            .Where(o => o != null && o.job != null)
            .Where(o => !o.HiddenWhenLocked || o.CanAccept(player, log, boardId, out _))
            .OrderBy(o => o.SortOrder)
            .ThenBy(o => o.Job.DisplayName)
            .ToList();
    }

    public bool HasTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag) || tags == null) {
            return false;
        }

        foreach(var entry in tags) {
            if(string.Equals(entry, tag, System.StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return false;
    }
}

[System.Serializable]
public class JobBoardOffer {
    [Tooltip("Job offered by this entry.")]
    public JobDefinition job;
    [Tooltip("Optional sort order used by future UI.")]
    public int sortOrder;
    [Tooltip("If enabled, locked jobs are hidden from GetAvailableOffers.")]
    public bool hiddenWhenLocked = true;
    [Tooltip("Optional title, badge or permit required before this offer appears.")]
    public TitleDefinition requiredTitle;
    [Tooltip("Optional faction whose reputation gates this offer.")]
    public ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    public int requiredReputation;
    [Tooltip("Message shown when this offer is locked.")]
    public string lockedMessage = "This job is not available yet.";

    public JobDefinition Job => job;
    public int SortOrder => sortOrder;
    public bool HiddenWhenLocked => hiddenWhenLocked;

    public bool CanAccept(PlayerController player, PlayerJobLog log, string boardId, out string failureMessage) {
        if(job == null) {
            failureMessage = "No job assigned.";
            return false;
        }

        if(requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredFaction != null) {
            int reputation = player?.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0;
            if(reputation < requiredReputation) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more reputation with {requiredFaction.DisplayName}." : lockedMessage;
                return false;
            }
        }

        return job.CanAccept(player, log, boardId, out failureMessage);
    }
}
