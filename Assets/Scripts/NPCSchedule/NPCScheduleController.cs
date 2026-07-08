using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NPCScheduleController : MonoBehaviour {
    [Tooltip("Schedule asset that decides where and when this NPC appears.")]
    [SerializeField] NPCScheduleDefinition schedule;
    [Tooltip("Scene waypoint lookup used by schedule entries.")]
    [SerializeField] List<NPCScheduleWaypoint> waypoints = new List<NPCScheduleWaypoint>();
    [Tooltip("Root GameObject hidden when a schedule entry marks the NPC invisible. Empty uses this GameObject/renderers.")]
    [SerializeField] GameObject visualRoot;
    [Tooltip("Collider enabled/disabled with NPC visibility. Empty uses this GameObject's Collider2D.")]
    [SerializeField] Collider2D interactionCollider;
    [Tooltip("If enabled, applies the schedule once during Start.")]
    [SerializeField] bool applyOnStart = true;
    [Tooltip("If enabled, schedule changes are written to the debug log.")]
    [SerializeField] bool logScheduleChanges;

    Character character;
    CharacterAnimator characterAnimator;
    NPCController npcController;
    string activeEntryId;

    public NPCScheduleDefinition Schedule => schedule;
    public string ActiveEntryId => activeEntryId;

    void Awake() {
        character = GetComponent<Character>();
        characterAnimator = GetComponent<CharacterAnimator>();
        npcController = GetComponent<NPCController>();
        if(visualRoot == null) {
            visualRoot = gameObject;
        }
        if(interactionCollider == null) {
            interactionCollider = GetComponent<Collider2D>();
        }
    }

    void Start() {
        if(applyOnStart) {
            ApplySchedule();
        }
    }

    void OnEnable() {
        if(TimeSystem.i != null) {
            TimeSystem.i.OnTimeChanged += ApplySchedule;
            TimeSystem.i.OnDayChanged += ApplySchedule;
        }

        if(WorldEventManager.i != null) {
            WorldEventManager.i.OnWorldEventsChanged += ApplySchedule;
        }
    }

    void OnDisable() {
        if(TimeSystem.i != null) {
            TimeSystem.i.OnTimeChanged -= ApplySchedule;
            TimeSystem.i.OnDayChanged -= ApplySchedule;
        }

        if(WorldEventManager.i != null) {
            WorldEventManager.i.OnWorldEventsChanged -= ApplySchedule;
        }
    }

    [ContextMenu("Apply Schedule Now")]
    public void ApplySchedule() {
        if(schedule == null) {
            return;
        }

        var entry = schedule.GetEntryForNow(PlayerController.i);
        if(entry == null) {
            SetVisible(false);
            activeEntryId = null;
            return;
        }

        if(activeEntryId == entry.Id) {
            return;
        }

        activeEntryId = entry.Id;
        ApplyEntry(entry);
    }

    void ApplyEntry(NPCScheduleEntry entry) {
        SetVisible(entry.Visible);

        var waypoint = GetWaypoint(entry.WaypointKey);
        if(waypoint != null && character != null) {
            character.SetPositionAndSnapToTile(waypoint.position);
        }

        if(characterAnimator != null) {
            characterAnimator.SetFacingDirection(entry.FacingDirection);
        }

        if(npcController != null) {
            npcController.SetMovementPattern(entry.MovementPattern);
        }

        if(logScheduleChanges) {
            GameDebug.Step($"{name} applied NPC schedule entry {entry.Id}.", GameDebugCategory.Scene, this, "NPCScheduleController");
        }
    }

    Transform GetWaypoint(string key) {
        if(string.IsNullOrWhiteSpace(key)) {
            return null;
        }

        var waypoint = waypoints.FirstOrDefault(w => w != null && w.key == key);
        if(waypoint == null || waypoint.target == null) {
            GameDebug.Warning($"{name} schedule waypoint '{key}' is missing.", GameDebugCategory.Scene, this, "NPCScheduleController");
            return null;
        }

        return waypoint.target;
    }

    void SetVisible(bool visible) {
        if(visualRoot != null && visualRoot != gameObject) {
            visualRoot.SetActive(visible);
        } else {
            foreach(var renderer in GetComponentsInChildren<Renderer>(true)) {
                renderer.enabled = visible;
            }
        }

        if(interactionCollider != null) {
            interactionCollider.enabled = visible;
        }

        if(npcController != null) {
            npcController.enabled = visible;
        }
    }
}

[System.Serializable]
public class NPCScheduleWaypoint {
    [Tooltip("Key referenced by NPC schedule entries.")]
    public string key;
    [Tooltip("Scene transform where the NPC moves for this key.")]
    public Transform target;
}
