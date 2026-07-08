using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifePathRewardSource : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Identity")]
    [Tooltip("Stable source id saved in life path history. Empty uses GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Life path rewards applied by this source.")]
    [SerializeField] List<LifePathReward> rewards = new List<LifePathReward>();
    [Tooltip("Optional player override. Empty uses PlayerController.i or the first loaded PlayerController.")]
    [SerializeField] PlayerController playerOverride = null;

    [Header("Signals")]
    [Tooltip("If enabled, rewards apply once during Start.")]
    [SerializeField] bool applyOnStart = false;
    [Tooltip("If enabled, rewards apply whenever the component enables.")]
    [SerializeField] bool applyOnEnable = false;
    [Tooltip("If enabled, entering this trigger applies rewards.")]
    [SerializeField] bool applyOnPlayerTrigger = true;
    [Tooltip("If enabled, interacting with this object applies rewards.")]
    [SerializeField] bool applyOnInteract = true;
    [Tooltip("If enabled, repeated player triggers can apply this source more than once.")]
    [SerializeField] bool triggerRepeatedly = false;

    [Header("Debug")]
    [Tooltip("If enabled, reward attempts are written to GameDebugLogger.")]
    [SerializeField] bool logAttempts = false;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public IReadOnlyList<LifePathReward> Rewards => rewards;
    public bool TriggerRepeatedly => triggerRepeatedly;

    void OnEnable() {
        if(applyOnEnable) {
            ApplyRewards();
        }
    }

    void Start() {
        if(applyOnStart) {
            ApplyRewards();
        }
    }

    [ContextMenu("Apply Life Path Rewards")]
    public void ApplyRewardsFromContextMenu() {
        ApplyRewards();
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(applyOnPlayerTrigger) {
            ApplyRewards(player);
        }
    }

    public IEnumerator Interact(Transform initiator) {
        if(applyOnInteract) {
            var player = initiator != null ? initiator.GetComponent<PlayerController>() : ResolvePlayer();
            ApplyRewards(player);
        }

        yield break;
    }

    public void ApplyRewards() {
        ApplyRewards(ResolvePlayer());
    }

    public void ApplyRewards(PlayerController player) {
        if(player == null) {
            WriteAttemptLog(false, 0, "No player found.");
            return;
        }

        var log = player.GetComponent<PlayerLifePathLog>() ?? player.gameObject.AddComponent<PlayerLifePathLog>();
        int applied = 0;
        foreach(var reward in rewards) {
            if(log.ApplyReward(reward, SourceId, DisplayName, this)) {
                applied++;
            }
        }

        WriteAttemptLog(true, applied, null);
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

    void WriteAttemptLog(bool success, int applied, string failure) {
        if(!logAttempts) {
            return;
        }

        GameDebugLogger.Ensure().Record(
            success ? GameDebugSeverity.Info : GameDebugSeverity.Warning,
            GameDebugCategory.RPG,
            success
                ? $"{DisplayName} applied {applied} life path reward(s)."
                : $"{DisplayName} could not apply life path rewards: {failure}",
            this,
            "LifePathRewardSource");
    }
}
