using System;
using System.Linq;
using UnityEngine;

public class PlayerBattleModeSettings : MonoBehaviour, ISavable {
    [Tooltip("Currently selected preferred battle mode. Empty means the classic/current BattleSystem behavior is used.")]
    [SerializeField] BattleModeDefinition selectedBattleMode;
    [Tooltip("If enabled, battle challenges can use the selected player preference when the challenge allows it.")]
    [SerializeField] bool preferSelectedModeForChallenges = true;
    [Tooltip("If enabled, battle mode changes are written to GameDebug.")]
    [SerializeField] bool writeDebugLogs;

    public BattleModeDefinition SelectedBattleMode => selectedBattleMode;
    public bool PreferSelectedModeForChallenges => preferSelectedModeForChallenges;
    public event Action<BattleModeDefinition> OnBattleModeChanged;

    public bool SetBattleMode(BattleModeDefinition mode, out string failureMessage) {
        var player = GetComponent<PlayerController>();
        if(mode != null && !mode.CanAccess(player, out failureMessage)) {
            return false;
        }

        selectedBattleMode = mode;
        OnBattleModeChanged?.Invoke(selectedBattleMode);
        if(writeDebugLogs) {
            GameDebug.Step(
                selectedBattleMode != null ? $"Battle mode selected: {selectedBattleMode.DisplayName}." : "Battle mode cleared.",
                GameDebugCategory.BattleRule,
                this,
                "PlayerBattleModeSettings");
        }

        failureMessage = null;
        return true;
    }

    public bool SetBattleModeById(string modeId, out string failureMessage) {
        if(string.IsNullOrWhiteSpace(modeId)) {
            return SetBattleMode(null, out failureMessage);
        }

        var mode = ResolveMode(modeId);
        if(mode == null) {
            failureMessage = $"Battle mode '{modeId}' was not found.";
            return false;
        }

        return SetBattleMode(mode, out failureMessage);
    }

    public void SetPreferSelectedModeForChallenges(bool prefer) {
        if(preferSelectedModeForChallenges == prefer) {
            return;
        }

        preferSelectedModeForChallenges = prefer;
        OnBattleModeChanged?.Invoke(selectedBattleMode);
        if(writeDebugLogs) {
            GameDebug.Step(
                preferSelectedModeForChallenges ? "Battle mode preference enabled for challenges." : "Battle mode preference disabled for challenges.",
                GameDebugCategory.BattleRule,
                this,
                "PlayerBattleModeSettings");
        }
    }

    public BattleModeDefinition ResolvePreferredMode(PlayerController player, BattleChallengeDefinition challenge = null) {
        if(selectedBattleMode == null || (challenge != null && !preferSelectedModeForChallenges)) {
            return null;
        }

        if(!selectedBattleMode.CanAccess(player, out _)) {
            return null;
        }

        return selectedBattleMode;
    }

    BattleModeDefinition ResolveMode(string modeId) {
        return string.IsNullOrWhiteSpace(modeId)
            ? null
            : Resources.LoadAll<BattleModeDefinition>("").FirstOrDefault(mode => mode != null && mode.Id == modeId);
    }

    public object CaptureState() {
        return new PlayerBattleModeSettingsSaveData {
            selectedBattleModeId = selectedBattleMode != null ? selectedBattleMode.Id : string.Empty,
            selectedBattleModeName = selectedBattleMode != null ? selectedBattleMode.DisplayName : string.Empty,
            preferSelectedModeForChallenges = preferSelectedModeForChallenges
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerBattleModeSettingsSaveData;
        preferSelectedModeForChallenges = saveData?.preferSelectedModeForChallenges ?? preferSelectedModeForChallenges;
        selectedBattleMode = ResolveMode(saveData?.selectedBattleModeId);
        OnBattleModeChanged?.Invoke(selectedBattleMode);
    }
}

[Serializable]
public class PlayerBattleModeSettingsSaveData {
    [Tooltip("Saved id of the selected battle mode.")]
    public string selectedBattleModeId;
    [Tooltip("Saved display name of the selected battle mode for fallback/debug output.")]
    public string selectedBattleModeName;
    [Tooltip("Saved setting for whether challenge battles should use the player's preferred mode.")]
    public bool preferSelectedModeForChallenges;
}
