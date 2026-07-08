using UnityEngine;

public class LifePathUIManager : MonoBehaviour {
    [Tooltip("Optional player override. Empty uses PlayerController.i or the first loaded PlayerController.")]
    [SerializeField] PlayerController playerOverride = null;
    [Tooltip("If enabled, snapshot is refreshed during Start.")]
    [SerializeField] bool refreshOnStart = true;

    public LifePathSnapshot CurrentSnapshot { get; private set; } = new LifePathSnapshot();
    public PlayerLifePathLog CurrentLog => ResolvePlayer()?.GetComponent<PlayerLifePathLog>();

    void Start() {
        if(refreshOnStart) {
            RefreshSnapshot();
        }
    }

    [ContextMenu("Refresh Life Path Snapshot")]
    public void RefreshSnapshotFromContextMenu() {
        RefreshSnapshot();
    }

    public LifePathSnapshot RefreshSnapshot() {
        var log = CurrentLog;
        CurrentSnapshot = log != null ? log.GetSnapshot() : new LifePathSnapshot();
        return CurrentSnapshot;
    }

    public bool TryUnlockPerk(LifePathPerkDefinition perk, out string failureMessage) {
        var log = CurrentLog;
        if(log == null) {
            failureMessage = "Player life path log is missing.";
            return false;
        }

        bool unlocked = log.UnlockPerk(perk, "life-path-ui", this, out failureMessage);
        RefreshSnapshot();
        return unlocked;
    }

    public int GetAvailablePerkPoints(LifePathDefinition lifePath) {
        return CurrentLog != null ? CurrentLog.GetAvailablePerkPoints(lifePath) : 0;
    }

    public int GetBranchProgress(LifePathDefinition lifePath, string branchId) {
        return CurrentLog != null ? CurrentLog.GetBranchProgress(lifePath, branchId) : 0;
    }

    public int GetTagProgress(LifePathDefinition lifePath, string tag) {
        return CurrentLog != null ? CurrentLog.GetTagProgress(lifePath, tag) : 0;
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
}
