using UnityEngine;

public enum SceneSpawnRequirementMode {
    HasSpawned,
    NeverSpawned,
    SpawnCountAtLeast,
    SpawnerSpawnCountAtLeast,
    EntrySpawnCountAtLeast,
    HoursSinceLastSpawnAtLeast,
    HoursSinceLastSpawnAtMost
}

[CreateAssetMenu(menuName = "Activities/Requirements/Scene Spawn Requirement")]
public class SceneSpawnRequirement : ActivityRequirement {
    [Tooltip("Which scene spawn history check this requirement performs.")]
    [SerializeField] SceneSpawnRequirementMode mode = SceneSpawnRequirementMode.HasSpawned;
    [Tooltip("Scene spawn profile checked by this requirement.")]
    [SerializeField] SceneSpawnProfileDefinition profile = null;
    [Tooltip("Optional spawner/source id filter. Empty checks all spawners except Spawner Spawn Count At Least.")]
    [SerializeField] string spawnerId = string.Empty;
    [Tooltip("Optional entry id filter for entry/count/time modes.")]
    [SerializeField] string entryId = string.Empty;
    [Tooltip("Minimum count required by count modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("Minimum/maximum in-game hours used by Hours Since Last Spawn modes.")]
    [Min(0)]
    [SerializeField] int requiredHours = 1;
    [Tooltip("If enabled, blocked attempts are counted too. If disabled, only successful spawns count.")]
    [SerializeField] bool includeBlockedAttempts;
    [Tooltip("If enabled, the selected condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerSceneSpawnLog>() : null;
        bool result = mode switch {
            SceneSpawnRequirementMode.NeverSpawned => log == null || !log.HasSpawned(profile, spawnerId, entryId, includeBlockedAttempts),
            SceneSpawnRequirementMode.SpawnCountAtLeast => log != null && log.GetSpawnCount(profile, includeBlocked: includeBlockedAttempts) >= Mathf.Max(0, requiredCount),
            SceneSpawnRequirementMode.SpawnerSpawnCountAtLeast => log != null && log.GetSpawnCount(profile, spawnerId, includeBlocked: includeBlockedAttempts) >= Mathf.Max(0, requiredCount),
            SceneSpawnRequirementMode.EntrySpawnCountAtLeast => log != null && log.GetSpawnCount(profile, spawnerId, entryId, includeBlockedAttempts) >= Mathf.Max(0, requiredCount),
            SceneSpawnRequirementMode.HoursSinceLastSpawnAtLeast => HasHoursSinceLastSpawn(log, atLeast: true),
            SceneSpawnRequirementMode.HoursSinceLastSpawnAtMost => HasHoursSinceLastSpawn(log, atLeast: false),
            _ => log != null && log.HasSpawned(profile, spawnerId, entryId, includeBlockedAttempts)
        };

        return mustBeMet ? result : !result;
    }

    bool HasHoursSinceLastSpawn(PlayerSceneSpawnLog log, bool atLeast) {
        if(log == null || profile == null) {
            return false;
        }

        int hours = log.GetHoursSinceLastSpawn(profile, spawnerId, entryId, includeBlockedAttempts);
        if(hours < 0) {
            return false;
        }

        int required = Mathf.Max(0, requiredHours);
        return atLeast ? hours >= required : hours <= required;
    }
}
