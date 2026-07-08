using System.Collections;
using UnityEngine;

public class ResearchTarget : MonoBehaviour, Interactable {
    [Tooltip("Research subject studied through this target.")]
    [SerializeField] ResearchSubjectDefinition subject;
    [Tooltip("If disabled, this target cannot be studied again after the subject is completed.")]
    [SerializeField] bool canRepeatAfterCompleted = true;

    public IEnumerator Interact(Transform initiator) {
        var player = initiator.GetComponent<PlayerController>();

        if(subject == null) {
            yield return DialogManager.i.ShowDialogText("There is nothing to study here.");
            yield break;
        }

        var activity = subject.Activity;
        if(activity == null) {
            yield return DialogManager.i.ShowDialogText($"{subject.DisplayName} has no activity configured.");
            yield break;
        }

        if(!activity.CanPerform(player, out var failureMessage)) {
            yield return DialogManager.i.ShowDialogText(failureMessage);
            yield break;
        }

        var log = GetOrCreateResearchLog(player);
        if(log == null) {
            yield break;
        }

        bool wasCompleted = log.IsCompleted(subject);
        if(wasCompleted && !canRepeatAfterCompleted) {
            yield return DialogManager.i.ShowDialogText($"{subject.DisplayName} has already been fully researched.");
            yield break;
        }

        if(!activity.TryPayCosts(player, out failureMessage)) {
            yield return DialogManager.i.ShowDialogText(failureMessage);
            yield break;
        }

        int bonus = GetResearchBonus(player);
        var entry = log.AddProgress(subject, subject.PointsPerStudy + bonus + PlayerActivityContext.GetResearchPointBonus(activity));
        bool completedNow = entry != null && entry.completed && !wasCompleted;
        int experienceReward = (activity?.BaseExperience ?? 10) + bonus * 3;
        experienceReward = PlayerActivityContext.ModifyExperience(activity, experienceReward);
        if(WorldEventManager.i != null) {
            experienceReward = WorldEventManager.i.ModifyExperience(activity, experienceReward);
            WorldEventManager.i.ApplyActivityReputation(player, activity);
        } else {
            player?.GetComponent<PlayerReputation>()?.ApplyChanges(activity?.ReputationChanges);
        }

        player?.GetComponent<PlayerProgression>()?.AddExperience(
            experienceReward,
            activity?.ExperienceSource ?? PlayerExperienceSource.Research);
        activity?.ApplyRelationshipRewards(player);
        activity?.RecordCompletion(player);
        activity?.CompleteMilestones(player);
        activity?.ApplyLifePathRewards(player);
        activity?.ApplyOutcomes(player);
        PublishResearchEvent(player, entry, completedNow, experienceReward);

        string progress = entry.completed ? "Research complete." : $"Research progress: {entry.points}/{subject.RequiredResearchPoints}.";
        yield return DialogManager.i.ShowDialogText($"{subject.DisplayName}: {progress}");
    }

    PlayerResearchLog GetOrCreateResearchLog(PlayerController player) {
        if(player == null) {
            return null;
        }

        var log = player.GetComponent<PlayerResearchLog>();
        if(log == null) {
            log = player.gameObject.AddComponent<PlayerResearchLog>();
        }
        return log;
    }

    int GetResearchBonus(PlayerController player) {
        var skill = subject.Activity?.BonusSkill;
        if(player == null || skill == null) {
            return 0;
        }

        return player.GetComponent<PlayerProgression>()?.GetSkillLevel(skill) ?? 0;
    }

    void PublishResearchEvent(PlayerController player, ResearchEntry entry, bool completedNow, int experienceReward) {
        var eventDefinition = completedNow ? subject.CompletedEvent : subject.StudyEvent;
        string fallbackId = completedNow ? $"research.completed.{subject.Id}" : $"research.studied.{subject.Id}";
        string fallbackMessage = completedNow ? $"{subject.DisplayName} research completed." : $"{subject.DisplayName} studied.";

        GameEventPublishing.PublishOptional(
            eventDefinition,
            fallbackId,
            fallbackMessage,
            GameEventCategory.Research,
            completedNow ? GameEventImportance.Success : GameEventImportance.Info,
            player,
            "ResearchTarget",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("subjectId", subject.Id),
            GameEventPublishing.Value("subjectName", subject.DisplayName),
            GameEventPublishing.Value("activityId", subject.Activity != null ? subject.Activity.Id : null),
            GameEventPublishing.Value("points", entry != null ? entry.points : 0),
            GameEventPublishing.Value("requiredPoints", subject.RequiredResearchPoints),
            GameEventPublishing.Value("completed", entry != null && entry.completed),
            GameEventPublishing.Value("experience", experienceReward));
    }
}
