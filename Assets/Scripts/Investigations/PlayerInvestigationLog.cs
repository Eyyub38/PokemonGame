using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerInvestigationLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save ids for cases unlocked for the player.")]
    [SerializeField] List<string> unlockedCaseIds = new List<string>();
    [Tooltip("Runtime/save list of currently active cases.")]
    [SerializeField] List<PlayerInvestigationState> activeCases = new List<PlayerInvestigationState>();
    [Tooltip("Runtime/save history of completed cases.")]
    [SerializeField] List<PlayerInvestigationCompletionState> completedCases = new List<PlayerInvestigationCompletionState>();

    public IReadOnlyList<string> UnlockedCaseIds => unlockedCaseIds;
    public IReadOnlyList<PlayerInvestigationState> ActiveCases => activeCases;
    public IReadOnlyList<PlayerInvestigationCompletionState> CompletedCases => completedCases;
    public event Action<InvestigationCaseDefinition> OnCaseUnlocked;
    public event Action<InvestigationCaseDefinition> OnCaseStarted;
    public event Action<InvestigationCaseDefinition, InvestigationClueDefinition> OnClueDiscovered;
    public event Action<InvestigationCaseDefinition> OnCaseCompleted;
    public event Action OnInvestigationLogChanged;

    public bool HasUnlockedCase(InvestigationCaseDefinition investigationCase) {
        return investigationCase != null && (investigationCase.UnlockedByDefault || HasUnlockedCase(investigationCase.Id));
    }

    public bool HasUnlockedCase(string caseId) {
        return !string.IsNullOrWhiteSpace(caseId) && unlockedCaseIds.Contains(caseId);
    }

    public bool UnlockCase(InvestigationCaseDefinition investigationCase, string sourceId = null) {
        if(investigationCase == null || HasUnlockedCase(investigationCase.Id)) {
            return false;
        }

        unlockedCaseIds.Add(investigationCase.Id);
        OnCaseUnlocked?.Invoke(investigationCase);
        OnInvestigationLogChanged?.Invoke();
        investigationCase.PublishUnlocked(GetComponent<PlayerController>(), sourceId);
        return true;
    }

    public bool StartCase(InvestigationCaseDefinition investigationCase, string sourceId, out string failureMessage) {
        if(investigationCase == null) {
            failureMessage = "No investigation case selected.";
            return false;
        }

        if(!investigationCase.CanStart(GetComponent<PlayerController>(), this, out failureMessage)) {
            return false;
        }

        var state = new PlayerInvestigationState {
            caseId = investigationCase.Id,
            caseName = investigationCase.DisplayName,
            category = investigationCase.Category,
            sourceId = sourceId,
            startedDay = GetCurrentDay(),
            startedAbsoluteHour = GetCurrentAbsoluteHour(),
            currentStageIndex = investigationCase.GetStageIndex(null)
        };
        activeCases.Add(state);
        UpdateStage(investigationCase, state);
        OnCaseStarted?.Invoke(investigationCase);
        OnInvestigationLogChanged?.Invoke();
        investigationCase.PublishStarted(GetComponent<PlayerController>(), sourceId);
        failureMessage = null;
        return true;
    }

    public bool DiscoverClue(InvestigationCaseDefinition investigationCase, InvestigationClueDefinition clue, string sourceId, out string failureMessage) {
        if(investigationCase == null || clue == null) {
            failureMessage = "No investigation case or clue selected.";
            return false;
        }

        var state = GetActiveCase(investigationCase);
        if(state == null) {
            if(!StartCase(investigationCase, sourceId, out failureMessage)) {
                return false;
            }

            state = GetActiveCase(investigationCase);
        }

        if(state.HasClue(clue.Id)) {
            failureMessage = $"{clue.DisplayName} is already discovered.";
            return false;
        }

        if(!investigationCase.CanDiscoverClue(GetComponent<PlayerController>(), clue, out failureMessage)) {
            return false;
        }

        int evidencePoints = investigationCase.GetEvidencePointsForClue(clue);
        state.discoveredClueIds.Add(clue.Id);
        state.discoveredClueNames.Add(clue.DisplayName);
        state.evidencePoints += evidencePoints;
        state.lastClueId = clue.Id;
        state.lastClueName = clue.DisplayName;
        state.lastUpdatedDay = GetCurrentDay();
        state.lastUpdatedAbsoluteHour = GetCurrentAbsoluteHour();
        UpdateStage(investigationCase, state);

        OnClueDiscovered?.Invoke(investigationCase, clue);
        OnInvestigationLogChanged?.Invoke();
        clue.PublishDiscovered(GetComponent<PlayerController>(), investigationCase, sourceId, this);

        if(investigationCase.AutoCompleteWhenReady && investigationCase.CanComplete(GetComponent<PlayerController>(), state, out _)) {
            CompleteCase(investigationCase, sourceId, out _);
        }

        failureMessage = null;
        return true;
    }

    public bool CompleteCase(InvestigationCaseDefinition investigationCase, string sourceId, out string failureMessage) {
        if(investigationCase == null) {
            failureMessage = "No investigation case selected.";
            return false;
        }

        var state = GetActiveCase(investigationCase);
        if(state == null) {
            failureMessage = $"{investigationCase.DisplayName} is not active.";
            return false;
        }

        if(!investigationCase.CanComplete(GetComponent<PlayerController>(), state, out failureMessage)) {
            return false;
        }

        investigationCase.ApplyCompletionRewards(GetComponent<PlayerController>());
        activeCases.Remove(state);
        RecordCompletion(investigationCase, state, sourceId);
        OnCaseCompleted?.Invoke(investigationCase);
        OnInvestigationLogChanged?.Invoke();
        investigationCase.PublishCompleted(GetComponent<PlayerController>(), sourceId);
        failureMessage = null;
        return true;
    }

    public bool HasActiveCase(InvestigationCaseDefinition investigationCase) {
        return GetActiveCase(investigationCase) != null;
    }

    public bool HasCompletedCase(InvestigationCaseDefinition investigationCase) {
        return GetCompletedCount(investigationCase) > 0;
    }

    public PlayerInvestigationState GetActiveCase(InvestigationCaseDefinition investigationCase) {
        return investigationCase != null ? activeCases.FirstOrDefault(state => state != null && state.caseId == investigationCase.Id) : null;
    }

    public int GetCompletedCount(InvestigationCaseDefinition investigationCase) {
        if(investigationCase == null) {
            return 0;
        }

        var state = completedCases.FirstOrDefault(entry => entry != null && entry.caseId == investigationCase.Id);
        return state != null ? Mathf.Max(0, state.completedCount) : 0;
    }

    public bool HasDiscoveredClue(InvestigationCaseDefinition investigationCase, InvestigationClueDefinition clue) {
        if(clue == null) {
            return false;
        }

        if(investigationCase != null) {
            if(GetActiveCase(investigationCase)?.HasClue(clue.Id) ?? false) {
                return true;
            }

            return completedCases.Any(state => state != null && state.caseId == investigationCase.Id && state.HasClue(clue.Id));
        }

        return activeCases.Any(state => state != null && state.HasClue(clue.Id))
            || completedCases.Any(state => state != null && state.HasClue(clue.Id));
    }

    public int GetDiscoveredClueCount(InvestigationCaseDefinition investigationCase) {
        var activeState = GetActiveCase(investigationCase);
        if(activeState != null) {
            return activeState.GetDiscoveredClueCount();
        }

        var completedState = investigationCase != null ? completedCases.FirstOrDefault(state => state != null && state.caseId == investigationCase.Id) : null;
        return completedState != null ? completedState.lastClueCount : 0;
    }

    public int GetDiscoveredClueCountWithTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return 0;
        }

        int count = 0;
        foreach(var state in activeCases) {
            if(state == null || state.discoveredClueIds == null) {
                continue;
            }

            foreach(string clueId in state.discoveredClueIds) {
                var clue = ResolveClue(clueId);
                if(clue != null && clue.HasTag(tag)) {
                    count++;
                }
            }
        }

        foreach(var state in completedCases) {
            if(state == null || state.discoveredClueIds == null) {
                continue;
            }

            foreach(string clueId in state.discoveredClueIds) {
                var clue = ResolveClue(clueId);
                if(clue != null && clue.HasTag(tag)) {
                    count++;
                }
            }
        }

        return count;
    }

    public int GetEvidencePoints(InvestigationCaseDefinition investigationCase) {
        return GetActiveCase(investigationCase)?.evidencePoints ?? 0;
    }

    public int GetStageIndex(InvestigationCaseDefinition investigationCase) {
        return GetActiveCase(investigationCase)?.currentStageIndex ?? -1;
    }

    void UpdateStage(InvestigationCaseDefinition investigationCase, PlayerInvestigationState state) {
        if(investigationCase == null || state == null) {
            return;
        }

        var stage = investigationCase.GetStageFor(state);
        state.currentStageIndex = investigationCase.GetStageIndex(state);
        state.currentStageId = stage != null ? stage.id : null;
        state.currentStageName = stage != null ? stage.displayName : null;
    }

    void RecordCompletion(InvestigationCaseDefinition investigationCase, PlayerInvestigationState activeState, string sourceId) {
        var state = completedCases.FirstOrDefault(entry => entry != null && entry.caseId == investigationCase.Id);
        if(state == null) {
            state = new PlayerInvestigationCompletionState {
                caseId = investigationCase.Id,
                caseName = investigationCase.DisplayName,
                category = investigationCase.Category
            };
            completedCases.Add(state);
        }

        state.completedCount++;
        state.lastCompletedDay = GetCurrentDay();
        state.lastCompletedAbsoluteHour = GetCurrentAbsoluteHour();
        state.lastSourceId = sourceId;
        state.lastEvidencePoints = activeState != null ? activeState.evidencePoints : 0;
        state.lastClueCount = activeState != null ? activeState.GetDiscoveredClueCount() : 0;
        state.discoveredClueIds = activeState?.discoveredClueIds?.Distinct().ToList() ?? new List<string>();
    }

    InvestigationClueDefinition ResolveClue(string clueId) {
        if(string.IsNullOrWhiteSpace(clueId)) {
            return null;
        }

        return Resources.LoadAll<InvestigationClueDefinition>("").FirstOrDefault(clue => clue != null && clue.Id == clueId);
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerInvestigationLogSaveData {
            unlockedCaseIds = unlockedCaseIds.Distinct().ToList(),
            activeCases = activeCases.Where(state => state != null).Select(state => state.ToSaveData()).ToList(),
            completedCases = completedCases.Where(state => state != null).Select(state => state.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerInvestigationLogSaveData;
        unlockedCaseIds = saveData?.unlockedCaseIds?.Distinct().ToList() ?? new List<string>();
        activeCases = saveData?.activeCases?.Where(entry => entry != null).Select(entry => new PlayerInvestigationState(entry)).ToList() ?? new List<PlayerInvestigationState>();
        completedCases = saveData?.completedCases?.Where(entry => entry != null).Select(entry => new PlayerInvestigationCompletionState(entry)).ToList() ?? new List<PlayerInvestigationCompletionState>();
        OnInvestigationLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerInvestigationState {
    [Tooltip("Saved case id.")]
    public string caseId;
    [Tooltip("Saved case display name for fallback/debug output.")]
    public string caseName;
    [Tooltip("Saved case category.")]
    public InvestigationCaseCategory category;
    [Tooltip("Source id where this case started.")]
    public string sourceId;
    [Tooltip("In-game day when this case started.")]
    public int startedDay;
    [Tooltip("Absolute in-game hour when this case started.")]
    public int startedAbsoluteHour;
    [Tooltip("Current evidence points.")]
    [Min(0)]
    public int evidencePoints;
    [Tooltip("Current stage index.")]
    public int currentStageIndex = -1;
    [Tooltip("Current stage id.")]
    public string currentStageId;
    [Tooltip("Current stage display name.")]
    public string currentStageName;
    [Tooltip("Discovered clue ids.")]
    public List<string> discoveredClueIds = new List<string>();
    [Tooltip("Discovered clue display names for fallback/debug output.")]
    public List<string> discoveredClueNames = new List<string>();
    [Tooltip("Last clue id discovered for this case.")]
    public string lastClueId;
    [Tooltip("Last clue display name discovered for this case.")]
    public string lastClueName;
    [Tooltip("In-game day when this case last changed.")]
    public int lastUpdatedDay = -1;
    [Tooltip("Absolute in-game hour when this case last changed.")]
    public int lastUpdatedAbsoluteHour = -1;

    public PlayerInvestigationState() {
    }

    public PlayerInvestigationState(PlayerInvestigationStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        caseId = saveData.caseId;
        caseName = saveData.caseName;
        category = saveData.category;
        sourceId = saveData.sourceId;
        startedDay = saveData.startedDay;
        startedAbsoluteHour = saveData.startedAbsoluteHour;
        evidencePoints = Mathf.Max(0, saveData.evidencePoints);
        currentStageIndex = saveData.currentStageIndex;
        currentStageId = saveData.currentStageId;
        currentStageName = saveData.currentStageName;
        discoveredClueIds = saveData.discoveredClueIds?.Distinct().ToList() ?? new List<string>();
        discoveredClueNames = saveData.discoveredClueNames?.ToList() ?? new List<string>();
        lastClueId = saveData.lastClueId;
        lastClueName = saveData.lastClueName;
        lastUpdatedDay = saveData.lastUpdatedDay;
        lastUpdatedAbsoluteHour = saveData.lastUpdatedAbsoluteHour;
    }

    public bool HasClue(string clueId) {
        return !string.IsNullOrWhiteSpace(clueId) && discoveredClueIds.Contains(clueId);
    }

    public int GetDiscoveredClueCount() {
        return discoveredClueIds != null ? discoveredClueIds.Distinct().Count() : 0;
    }

    public PlayerInvestigationStateSaveData ToSaveData() {
        return new PlayerInvestigationStateSaveData {
            caseId = caseId,
            caseName = caseName,
            category = category,
            sourceId = sourceId,
            startedDay = startedDay,
            startedAbsoluteHour = startedAbsoluteHour,
            evidencePoints = evidencePoints,
            currentStageIndex = currentStageIndex,
            currentStageId = currentStageId,
            currentStageName = currentStageName,
            discoveredClueIds = discoveredClueIds?.Distinct().ToList() ?? new List<string>(),
            discoveredClueNames = discoveredClueNames?.ToList() ?? new List<string>(),
            lastClueId = lastClueId,
            lastClueName = lastClueName,
            lastUpdatedDay = lastUpdatedDay,
            lastUpdatedAbsoluteHour = lastUpdatedAbsoluteHour
        };
    }
}

[Serializable]
public class PlayerInvestigationCompletionState {
    [Tooltip("Saved case id.")]
    public string caseId;
    [Tooltip("Saved case display name for fallback/debug output.")]
    public string caseName;
    [Tooltip("Saved case category.")]
    public InvestigationCaseCategory category;
    [Tooltip("Total completions for this case.")]
    [Min(0)]
    public int completedCount;
    [Tooltip("In-game day when this case was last completed.")]
    public int lastCompletedDay = -1;
    [Tooltip("Absolute in-game hour when this case was last completed.")]
    public int lastCompletedAbsoluteHour = -1;
    [Tooltip("Source id that last completed this case.")]
    public string lastSourceId;
    [Tooltip("Evidence points held at last completion.")]
    [Min(0)]
    public int lastEvidencePoints;
    [Tooltip("Discovered clue count held at last completion.")]
    [Min(0)]
    public int lastClueCount;
    [Tooltip("Discovered clue ids held at last completion.")]
    public List<string> discoveredClueIds = new List<string>();

    public PlayerInvestigationCompletionState() {
    }

    public PlayerInvestigationCompletionState(PlayerInvestigationCompletionStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        caseId = saveData.caseId;
        caseName = saveData.caseName;
        category = saveData.category;
        completedCount = Mathf.Max(0, saveData.completedCount);
        lastCompletedDay = saveData.lastCompletedDay;
        lastCompletedAbsoluteHour = saveData.lastCompletedAbsoluteHour;
        lastSourceId = saveData.lastSourceId;
        lastEvidencePoints = Mathf.Max(0, saveData.lastEvidencePoints);
        lastClueCount = Mathf.Max(0, saveData.lastClueCount);
        discoveredClueIds = saveData.discoveredClueIds?.Distinct().ToList() ?? new List<string>();
    }

    public bool HasClue(string clueId) {
        return !string.IsNullOrWhiteSpace(clueId) && discoveredClueIds.Contains(clueId);
    }

    public PlayerInvestigationCompletionStateSaveData ToSaveData() {
        return new PlayerInvestigationCompletionStateSaveData {
            caseId = caseId,
            caseName = caseName,
            category = category,
            completedCount = completedCount,
            lastCompletedDay = lastCompletedDay,
            lastCompletedAbsoluteHour = lastCompletedAbsoluteHour,
            lastSourceId = lastSourceId,
            lastEvidencePoints = lastEvidencePoints,
            lastClueCount = lastClueCount,
            discoveredClueIds = discoveredClueIds?.Distinct().ToList() ?? new List<string>()
        };
    }
}

[Serializable]
public class PlayerInvestigationLogSaveData {
    public List<string> unlockedCaseIds;
    public List<PlayerInvestigationStateSaveData> activeCases;
    public List<PlayerInvestigationCompletionStateSaveData> completedCases;
}

[Serializable]
public class PlayerInvestigationStateSaveData {
    public string caseId;
    public string caseName;
    public InvestigationCaseCategory category;
    public string sourceId;
    public int startedDay;
    public int startedAbsoluteHour;
    public int evidencePoints;
    public int currentStageIndex;
    public string currentStageId;
    public string currentStageName;
    public List<string> discoveredClueIds;
    public List<string> discoveredClueNames;
    public string lastClueId;
    public string lastClueName;
    public int lastUpdatedDay;
    public int lastUpdatedAbsoluteHour;
}

[Serializable]
public class PlayerInvestigationCompletionStateSaveData {
    public string caseId;
    public string caseName;
    public InvestigationCaseCategory category;
    public int completedCount;
    public int lastCompletedDay;
    public int lastCompletedAbsoluteHour;
    public string lastSourceId;
    public int lastEvidencePoints;
    public int lastClueCount;
    public List<string> discoveredClueIds;
}
