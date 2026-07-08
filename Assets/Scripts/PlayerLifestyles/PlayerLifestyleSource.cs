using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLifestyleSource : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Identity")]
    [Tooltip("Stable source id used by lifestyle history. Empty uses GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Lifestyle point grants applied by this source.")]
    [SerializeField] List<LifestylePointGrant> grants = new List<LifestylePointGrant>();
    [Tooltip("Optional player override. Empty uses PlayerController.i or the first loaded PlayerController.")]
    [SerializeField] PlayerController playerOverride = null;

    [Header("Signals")]
    [Tooltip("If enabled, lifestyle grants apply once during Start.")]
    [SerializeField] bool applyOnStart = false;
    [Tooltip("If enabled, lifestyle grants apply whenever the component enables.")]
    [SerializeField] bool applyOnEnable = false;
    [Tooltip("If enabled, entering this trigger applies lifestyle grants.")]
    [SerializeField] bool applyOnPlayerTrigger = true;
    [Tooltip("If enabled, interacting with this object applies lifestyle grants.")]
    [SerializeField] bool applyOnInteract = true;
    [Tooltip("If enabled, repeated player triggers can apply this source more than once.")]
    [SerializeField] bool triggerRepeatedly = false;

    [Header("Debug")]
    [Tooltip("If enabled, grant attempts are written to GameDebugLogger.")]
    [SerializeField] bool logAttempts = false;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public IReadOnlyList<LifestylePointGrant> Grants => grants;
    public bool TriggerRepeatedly => triggerRepeatedly;

    void OnEnable() {
        if(applyOnEnable) {
            ApplyGrants();
        }
    }

    void Start() {
        if(applyOnStart) {
            ApplyGrants();
        }
    }

    [ContextMenu("Apply Lifestyle Grants")]
    public void ApplyGrantsFromContextMenu() {
        ApplyGrants();
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(applyOnPlayerTrigger) {
            ApplyGrants(player);
        }
    }

    public IEnumerator Interact(Transform initiator) {
        if(applyOnInteract) {
            var player = initiator != null ? initiator.GetComponent<PlayerController>() : ResolvePlayer();
            ApplyGrants(player);
        }

        yield break;
    }

    public void ApplyGrants() {
        ApplyGrants(ResolvePlayer());
    }

    public void ApplyGrants(PlayerController player) {
        if(player == null) {
            WriteAttemptLog(0, "No player found.");
            return;
        }

        var log = player.GetComponent<PlayerLifestyleLog>() ?? player.gameObject.AddComponent<PlayerLifestyleLog>();
        int applied = 0;
        foreach(var grant in grants) {
            if(grant == null || grant.lifestyle == null || grant.points == 0) {
                continue;
            }

            var state = log.AddPoints(
                grant.lifestyle,
                grant.points,
                string.IsNullOrWhiteSpace(grant.sourceId) ? SourceId : grant.sourceId,
                string.IsNullOrWhiteSpace(grant.sourceName) ? DisplayName : grant.sourceName,
                this);
            if(state != null) {
                applied++;
            }
        }

        WriteAttemptLog(applied, null);
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

    void WriteAttemptLog(int applied, string failure) {
        if(!logAttempts) {
            return;
        }

        bool success = string.IsNullOrWhiteSpace(failure);
        GameDebugLogger.Ensure().Record(
            success ? GameDebugSeverity.Info : GameDebugSeverity.Warning,
            GameDebugCategory.RPG,
            success
                ? $"{DisplayName} applied {applied} lifestyle grant(s)."
                : $"{DisplayName} could not apply lifestyle grants: {failure}",
            this,
            "PlayerLifestyleSource");
    }
}
