using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerRumorLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save ids for rumors that have been unlocked but not necessarily heard.")]
    [SerializeField] List<string> unlockedRumorIds = new List<string>();
    [Tooltip("Runtime/save history of rumors the player has heard.")]
    [SerializeField] List<PlayerRumorState> heardRumors = new List<PlayerRumorState>();
    [Tooltip("Runtime/save ids for rumors the player has marked read in future UI.")]
    [SerializeField] List<string> readRumorIds = new List<string>();
    [Tooltip("Runtime/save ids for rumors the player has dismissed in future UI.")]
    [SerializeField] List<string> dismissedRumorIds = new List<string>();

    public IReadOnlyList<string> UnlockedRumorIds => unlockedRumorIds;
    public IReadOnlyList<PlayerRumorState> HeardRumors => heardRumors;
    public IReadOnlyList<string> ReadRumorIds => readRumorIds;
    public IReadOnlyList<string> DismissedRumorIds => dismissedRumorIds;
    public event Action<RumorDefinition> OnRumorUnlocked;
    public event Action<RumorDefinition> OnRumorHeard;
    public event Action OnRumorLogChanged;

    public bool HasUnlockedRumor(RumorDefinition rumor) {
        return rumor != null && (rumor.UnlockedByDefault || HasUnlockedRumor(rumor.Id));
    }

    public bool HasUnlockedRumor(string rumorId) {
        return !string.IsNullOrWhiteSpace(rumorId) && unlockedRumorIds.Contains(rumorId);
    }

    public bool UnlockRumor(RumorDefinition rumor, string source = null) {
        if(rumor == null || HasUnlockedRumor(rumor.Id)) {
            return false;
        }

        unlockedRumorIds.Add(rumor.Id);
        OnRumorUnlocked?.Invoke(rumor);
        OnRumorLogChanged?.Invoke();
        PublishLogEvent("unlocked", rumor.Id, rumor.Title, source, GameEventImportance.Success);
        return true;
    }

    public bool CanHear(RumorDefinition rumor, string sourceId, RumorRepeatMode repeatMode, int cooldownHours, int maxHeardCount, out string failureMessage) {
        if(rumor == null) {
            failureMessage = "No rumor selected.";
            return false;
        }

        int totalCount = GetHeardCount(rumor);
        if(maxHeardCount > 0 && totalCount >= maxHeardCount) {
            failureMessage = $"{rumor.Title} has already been heard enough times.";
            return false;
        }

        var sourceState = GetState(rumor.Id, sourceId);
        if(repeatMode == RumorRepeatMode.OnceEver && totalCount > 0) {
            failureMessage = $"{rumor.Title} has already been heard.";
            return false;
        }

        if(repeatMode == RumorRepeatMode.OncePerSource && sourceState != null && sourceState.heardCount > 0) {
            failureMessage = $"{rumor.Title} has already been heard from this source.";
            return false;
        }

        if(repeatMode == RumorRepeatMode.Daily && sourceState != null && sourceState.lastHeardDay == GetCurrentDay()) {
            failureMessage = $"{rumor.Title} can only be heard once per day here.";
            return false;
        }

        if(repeatMode == RumorRepeatMode.CooldownHours && sourceState != null && sourceState.lastHeardAbsoluteHour >= 0) {
            int elapsed = GetCurrentAbsoluteHour() - sourceState.lastHeardAbsoluteHour;
            if(elapsed < Mathf.Max(0, cooldownHours)) {
                failureMessage = $"{rumor.Title} will be available again in {cooldownHours - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public void RecordHeard(RumorDefinition rumor, string sourceId, string sourceName) {
        if(rumor == null) {
            return;
        }

        sourceId = string.IsNullOrWhiteSpace(sourceId) ? "rumor-source" : sourceId;
        var state = GetState(rumor.Id, sourceId);
        if(state == null) {
            state = new PlayerRumorState {
                rumorId = rumor.Id,
                rumorTitle = rumor.Title,
                sourceId = sourceId,
                sourceName = sourceName,
                firstHeardDay = GetCurrentDay(),
                firstHeardAbsoluteHour = GetCurrentAbsoluteHour()
            };
            heardRumors.Add(state);
        }

        state.heardCount++;
        state.lastHeardDay = GetCurrentDay();
        state.lastHeardAbsoluteHour = GetCurrentAbsoluteHour();
        state.sourceName = sourceName;
        OnRumorHeard?.Invoke(rumor);
        OnRumorLogChanged?.Invoke();
    }

    public bool HasHeardRumor(RumorDefinition rumor, string sourceId = null) {
        return rumor != null && GetHeardCount(rumor, sourceId) > 0;
    }

    public int GetHeardCount(RumorDefinition rumor, string sourceId = null) {
        return rumor != null ? GetHeardCount(rumor.Id, sourceId) : 0;
    }

    public int GetHeardCount(string rumorId, string sourceId = null) {
        if(string.IsNullOrWhiteSpace(rumorId)) {
            return 0;
        }

        return heardRumors
            .Where(state => state != null && state.rumorId == rumorId)
            .Where(state => string.IsNullOrWhiteSpace(sourceId) || state.sourceId == sourceId)
            .Sum(state => Mathf.Max(0, state.heardCount));
    }

    public bool HasHeardRumorWithTag(string tag) {
        return GetHeardCountWithTag(tag) > 0;
    }

    public int GetHeardCountWithTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return 0;
        }

        int count = 0;
        foreach(var state in heardRumors) {
            if(state == null || state.heardCount <= 0) {
                continue;
            }

            var rumor = ResolveRumor(state.rumorId);
            if(rumor != null && rumor.HasTag(tag)) {
                count += state.heardCount;
            }
        }

        return count;
    }

    public bool IsRumorRead(RumorDefinition rumor) {
        return rumor != null && readRumorIds.Contains(rumor.Id);
    }

    public void SetRumorRead(RumorDefinition rumor, bool read = true) {
        if(rumor == null) {
            return;
        }

        if(read && !readRumorIds.Contains(rumor.Id)) {
            readRumorIds.Add(rumor.Id);
        } else if(!read) {
            readRumorIds.Remove(rumor.Id);
        }

        OnRumorLogChanged?.Invoke();
    }

    public bool IsRumorDismissed(RumorDefinition rumor) {
        return rumor != null && dismissedRumorIds.Contains(rumor.Id);
    }

    public void SetRumorDismissed(RumorDefinition rumor, bool dismissed = true) {
        if(rumor == null) {
            return;
        }

        if(dismissed && !dismissedRumorIds.Contains(rumor.Id)) {
            dismissedRumorIds.Add(rumor.Id);
        } else if(!dismissed) {
            dismissedRumorIds.Remove(rumor.Id);
        }

        OnRumorLogChanged?.Invoke();
    }

    PlayerRumorState GetState(string rumorId, string sourceId) {
        if(string.IsNullOrWhiteSpace(rumorId)) {
            return null;
        }

        sourceId = string.IsNullOrWhiteSpace(sourceId) ? "rumor-source" : sourceId;
        return heardRumors.FirstOrDefault(state => state != null && state.rumorId == rumorId && state.sourceId == sourceId);
    }

    RumorDefinition ResolveRumor(string rumorId) {
        if(string.IsNullOrWhiteSpace(rumorId)) {
            return null;
        }

        return Resources.LoadAll<RumorDefinition>("").FirstOrDefault(rumor => rumor != null && rumor.Id == rumorId);
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    void PublishLogEvent(string phase, string rumorId, string rumorTitle, string source, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            null,
            $"rumor.{phase}.{rumorId}",
            $"{rumorTitle} {phase}.",
            GameEventCategory.Rumor,
            importance,
            this,
            "PlayerRumorLog",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("rumorId", rumorId),
            GameEventPublishing.Value("rumorTitle", rumorTitle),
            GameEventPublishing.Value("source", source));
    }

    public object CaptureState() {
        return new PlayerRumorLogSaveData {
            unlockedRumorIds = unlockedRumorIds.Distinct().ToList(),
            heardRumors = heardRumors.Where(state => state != null).Select(state => state.ToSaveData()).ToList(),
            readRumorIds = readRumorIds.Distinct().ToList(),
            dismissedRumorIds = dismissedRumorIds.Distinct().ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerRumorLogSaveData;
        unlockedRumorIds = saveData?.unlockedRumorIds?.Distinct().ToList() ?? new List<string>();
        heardRumors = saveData?.heardRumors?.Where(s => s != null).Select(s => new PlayerRumorState(s)).ToList() ?? new List<PlayerRumorState>();
        readRumorIds = saveData?.readRumorIds?.Distinct().ToList() ?? new List<string>();
        dismissedRumorIds = saveData?.dismissedRumorIds?.Distinct().ToList() ?? new List<string>();
        OnRumorLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerRumorState {
    [Tooltip("Saved rumor id.")]
    public string rumorId;
    [Tooltip("Saved rumor title for fallback/debug output.")]
    public string rumorTitle;
    [Tooltip("Source id where this rumor was heard.")]
    public string sourceId;
    [Tooltip("Source name where this rumor was heard.")]
    public string sourceName;
    [Tooltip("Number of times this rumor was heard from this source.")]
    [Min(0)]
    public int heardCount;
    [Tooltip("In-game day when this rumor was first heard from this source.")]
    public int firstHeardDay = -1;
    [Tooltip("Absolute in-game hour when this rumor was first heard from this source.")]
    public int firstHeardAbsoluteHour = -1;
    [Tooltip("In-game day when this rumor was last heard from this source.")]
    public int lastHeardDay = -1;
    [Tooltip("Absolute in-game hour when this rumor was last heard from this source.")]
    public int lastHeardAbsoluteHour = -1;

    public PlayerRumorState() {
    }

    public PlayerRumorState(PlayerRumorStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        rumorId = saveData.rumorId;
        rumorTitle = saveData.rumorTitle;
        sourceId = saveData.sourceId;
        sourceName = saveData.sourceName;
        heardCount = Mathf.Max(0, saveData.heardCount);
        firstHeardDay = saveData.firstHeardDay;
        firstHeardAbsoluteHour = saveData.firstHeardAbsoluteHour;
        lastHeardDay = saveData.lastHeardDay;
        lastHeardAbsoluteHour = saveData.lastHeardAbsoluteHour;
    }

    public PlayerRumorStateSaveData ToSaveData() {
        return new PlayerRumorStateSaveData {
            rumorId = rumorId,
            rumorTitle = rumorTitle,
            sourceId = sourceId,
            sourceName = sourceName,
            heardCount = heardCount,
            firstHeardDay = firstHeardDay,
            firstHeardAbsoluteHour = firstHeardAbsoluteHour,
            lastHeardDay = lastHeardDay,
            lastHeardAbsoluteHour = lastHeardAbsoluteHour
        };
    }
}

[Serializable]
public class PlayerRumorLogSaveData {
    public List<string> unlockedRumorIds;
    public List<PlayerRumorStateSaveData> heardRumors;
    public List<string> readRumorIds;
    public List<string> dismissedRumorIds;
}

[Serializable]
public class PlayerRumorStateSaveData {
    public string rumorId;
    public string rumorTitle;
    public string sourceId;
    public string sourceName;
    public int heardCount;
    public int firstHeardDay;
    public int firstHeardAbsoluteHour;
    public int lastHeardDay;
    public int lastHeardAbsoluteHour;
}
