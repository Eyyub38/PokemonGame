using System.Collections.Generic;
using UnityEngine;

public class CareerMentor : MonoBehaviour, IPlayerTriggerable {
    [Header("Mentor")]
    [Tooltip("Stable mentor/source id used by career logs. Empty uses GameObject name.")]
    [SerializeField] string mentorId;
    [Tooltip("Name shown in debug/future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName;
    [Tooltip("Career this mentor can unlock or join.")]
    [SerializeField] CareerPathDefinition career;

    [Header("Trigger Actions")]
    [Tooltip("If enabled, triggering this mentor unlocks the assigned career.")]
    [SerializeField] bool unlockCareerOnTrigger = true;
    [Tooltip("If enabled, triggering this mentor joins the assigned career.")]
    [SerializeField] bool joinCareerOnTrigger = true;
    [Tooltip("Career point grants applied when this mentor is triggered.")]
    [SerializeField] List<CareerPointGrant> pointGrants = new List<CareerPointGrant>();
    [Tooltip("If enabled, repeated player triggers can call OnPlayerTriggered more than once.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this mentor can be used.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional faction whose reputation gates this mentor.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum reputation required with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Message shown when mentor access is blocked.")]
    [SerializeField] string lockedMessage = "This mentor is not available right now.";

    [Header("Debug")]
    [Tooltip("If enabled, trigger attempts are written to GameDebug.")]
    [SerializeField] bool logAttempts;

    public string MentorId => string.IsNullOrWhiteSpace(mentorId) ? name : mentorId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public CareerPathDefinition Career => career;
    public IReadOnlyList<CareerPointGrant> PointGrants => pointGrants;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        if(player == null) {
            return;
        }

        if(!CanUse(player, out var failureMessage)) {
            PublishMentorEvent(player, "blocked", failureMessage, GameEventImportance.Warning);
            return;
        }

        var log = player.GetComponent<PlayerCareerLog>() ?? player.gameObject.AddComponent<PlayerCareerLog>();
        if(unlockCareerOnTrigger) {
            log.UnlockCareer(career, MentorId);
        }

        if(joinCareerOnTrigger && career != null && !log.JoinCareer(career, viaMentor: true, MentorId, out failureMessage)) {
            PublishMentorEvent(player, "blocked", failureMessage, GameEventImportance.Warning);
            return;
        }

        log.ApplyPointGrants(pointGrants, MentorId, viaMentor: true);
        PublishMentorEvent(player, "used", $"{DisplayName} updated career progress.", GameEventImportance.Info);
    }

    public bool CanUse(PlayerController player, out string failureMessage) {
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

    void PublishMentorEvent(PlayerController player, string phase, string message, GameEventImportance importance) {
        if(logAttempts) {
            GameDebug.Step(message, GameDebugCategory.Career, player != null ? player : this, "CareerMentor");
        }

        GameEventPublishing.PublishOptional(
            null,
            $"career-mentor.{phase}.{MentorId}",
            message,
            GameEventCategory.Career,
            importance,
            player != null ? player : this,
            "CareerMentor",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: logAttempts,
            GameEventPublishing.Value("mentorId", MentorId),
            GameEventPublishing.Value("mentorName", DisplayName),
            GameEventPublishing.Value("careerId", career != null ? career.Id : string.Empty),
            GameEventPublishing.Value("phase", phase));
    }
}
