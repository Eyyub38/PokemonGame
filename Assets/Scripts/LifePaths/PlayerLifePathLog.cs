using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum LifePathRecordKind {
    Experience,
    BranchProgress,
    TagProgress,
    PerkUnlocked
}

public class PlayerLifePathLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum life path history records kept in save data. 0 keeps all records.")]
    [Min(0)]
    [SerializeField] int maxRecords = 160;
    [Tooltip("Runtime/save state for every life path the player has touched.")]
    [SerializeField] List<PlayerLifePathState> lifePaths = new List<PlayerLifePathState>();
    [Tooltip("Runtime/save unlocked perk records.")]
    [SerializeField] List<PlayerLifePathPerkState> unlockedPerks = new List<PlayerLifePathPerkState>();
    [Tooltip("Runtime/save history of life path changes.")]
    [SerializeField] List<PlayerLifePathRecord> records = new List<PlayerLifePathRecord>();
    bool ensuringDefaultPerks;

    public IReadOnlyList<PlayerLifePathState> LifePaths => lifePaths;
    public IReadOnlyList<PlayerLifePathPerkState> UnlockedPerks => unlockedPerks;
    public IReadOnlyList<PlayerLifePathRecord> Records => records;

    public event Action<PlayerLifePathState, int> OnLifePathExperienceChanged;
    public event Action<PlayerLifePathState, string, int> OnBranchProgressChanged;
    public event Action<PlayerLifePathState, string, int> OnTagProgressChanged;
    public event Action<LifePathPerkDefinition> OnPerkUnlocked;
    public event Action OnLifePathLogChanged;

    public PlayerLifePathState GetState(LifePathDefinition lifePath) {
        return lifePath != null ? GetState(lifePath.Id) : null;
    }

    public PlayerLifePathState GetState(string lifePathId) {
        if(string.IsNullOrWhiteSpace(lifePathId)) {
            return null;
        }

        return lifePaths.FirstOrDefault(state => state != null && state.lifePathId == lifePathId);
    }

    public int GetTotalExperience(LifePathDefinition lifePath) {
        return GetState(lifePath)?.totalExperience ?? 0;
    }

    public int GetEarnedPerkPoints(LifePathDefinition lifePath) {
        if(lifePath == null) {
            return 0;
        }

        var state = GetState(lifePath);
        if(state == null) {
            return 0;
        }

        return Mathf.Max(state.earnedPerkPoints, lifePath.CalculateEarnedPerkPoints(state.totalExperience));
    }

    public int GetSpentPerkPoints(LifePathDefinition lifePath) {
        return GetState(lifePath)?.spentPerkPoints ?? 0;
    }

    public int GetAvailablePerkPoints(LifePathDefinition lifePath) {
        return Mathf.Max(0, GetEarnedPerkPoints(lifePath) - GetSpentPerkPoints(lifePath));
    }

    public int GetBranchProgress(LifePathDefinition lifePath, string branchId) {
        if(lifePath == null || string.IsNullOrWhiteSpace(branchId)) {
            return 0;
        }

        var state = GetState(lifePath);
        return state?.GetBranchProgress(branchId) ?? 0;
    }

    public int GetTagProgress(LifePathDefinition lifePath, string tag) {
        if(lifePath == null || string.IsNullOrWhiteSpace(tag)) {
            return 0;
        }

        var state = GetState(lifePath);
        return state?.GetTagProgress(tag) ?? 0;
    }

    public bool HasPathExperience(LifePathDefinition lifePath, int minimumExperience = 1) {
        return lifePath != null && GetTotalExperience(lifePath) >= Mathf.Max(0, minimumExperience);
    }

    public bool HasBranchProgress(LifePathDefinition lifePath, string branchId, int minimumProgress = 1) {
        return lifePath != null && GetBranchProgress(lifePath, branchId) >= Mathf.Max(0, minimumProgress);
    }

    public bool HasTagProgress(LifePathDefinition lifePath, string tag, int minimumCount = 1) {
        return lifePath != null && GetTagProgress(lifePath, tag) >= Mathf.Max(0, minimumCount);
    }

    public bool HasPerk(LifePathPerkDefinition perk) {
        return perk != null && HasPerk(perk.Id);
    }

    public bool HasPerk(string perkId) {
        return !string.IsNullOrWhiteSpace(perkId)
            && unlockedPerks.Any(perk => perk != null && perk.perkId == perkId);
    }

    public bool HasAnyPathWithTag(string tag, int minimumExperience = 1) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return false;
        }

        return GetStatesWithDefinitions()
            .Any(pair => pair.definition != null
                && pair.definition.HasTag(tag)
                && pair.state.totalExperience >= Mathf.Max(0, minimumExperience));
    }

    public PlayerLifePathState GetDominantPath() {
        return lifePaths
            .Where(state => state != null && state.totalExperience > 0)
            .OrderByDescending(state => state.totalExperience)
            .ThenBy(state => state.lifePathName)
            .FirstOrDefault();
    }

    public bool DominantPathIs(LifePathDefinition lifePath) {
        var dominant = GetDominantPath();
        return lifePath != null && dominant != null && dominant.lifePathId == lifePath.Id;
    }

    public bool DominantPathHasTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return false;
        }

        var dominant = GetDominantPath();
        if(dominant == null) {
            return false;
        }

        var definition = ResolveLifePath(dominant.lifePathId);
        return definition != null && definition.HasTag(tag);
    }

    public void ApplyRewards(IEnumerable<LifePathReward> rewards, string fallbackSourceId = null, string fallbackSourceName = null, UnityEngine.Object context = null) {
        if(rewards == null) {
            return;
        }

        foreach(var reward in rewards) {
            ApplyReward(reward, fallbackSourceId, fallbackSourceName, context);
        }
    }

    public bool ApplyReward(LifePathReward reward, string fallbackSourceId = null, string fallbackSourceName = null, UnityEngine.Object context = null) {
        if(reward == null || reward.lifePath == null || !reward.HasAnyPayload) {
            return false;
        }

        string sourceId = string.IsNullOrWhiteSpace(reward.sourceId) ? fallbackSourceId : reward.sourceId;
        string sourceName = string.IsNullOrWhiteSpace(reward.sourceName) ? fallbackSourceName : reward.sourceName;
        bool changed = false;

        if(reward.experience > 0) {
            changed |= AddExperience(reward.lifePath, reward.experience, sourceId, sourceName, context) != null;
        } else {
            EnsureState(reward.lifePath);
        }

        if(reward.branchProgress != null) {
            foreach(var branch in reward.branchProgress) {
                if(branch != null && branch.progress > 0 && !string.IsNullOrWhiteSpace(branch.branchId)) {
                    changed |= AddBranchProgress(reward.lifePath, branch.branchId, branch.progress, sourceId, sourceName, context) != null;
                }
            }
        }

        if(reward.tagProgress != null) {
            foreach(var tag in reward.tagProgress) {
                if(tag != null && tag.count > 0 && !string.IsNullOrWhiteSpace(tag.tag)) {
                    changed |= AddTagProgress(reward.lifePath, tag.tag, tag.count, sourceId, sourceName, context) != null;
                }
            }
        }

        if(reward.directPerkUnlocks != null) {
            foreach(var perk in reward.directPerkUnlocks) {
                if(perk != null && UnlockPerk(perk, sourceId, context, ignoreCost: true, ignoreRequirements: true, out _)) {
                    changed = true;
                }
            }
        }

        return changed;
    }

    public PlayerLifePathState AddExperience(LifePathDefinition lifePath, int amount, string sourceId = null, string sourceName = null, UnityEngine.Object context = null) {
        if(lifePath == null || amount <= 0) {
            return null;
        }

        var state = EnsureState(lifePath);
        int before = state.totalExperience;
        state.totalExperience = lifePath.ClampExperience(state.totalExperience + amount);
        int delta = state.totalExperience - before;
        if(delta <= 0) {
            return state;
        }

        int oldEarned = state.earnedPerkPoints;
        state.earnedPerkPoints = Mathf.Max(state.earnedPerkPoints, lifePath.CalculateEarnedPerkPoints(state.totalExperience));
        state.lastDelta = delta;
        state.lastSourceId = sourceId;
        state.lastSourceName = sourceName;
        state.lastChangedHour = GetCurrentTotalHour();
        RecordChange(LifePathRecordKind.Experience, state, delta, sourceId, sourceName, context: context);
        OnLifePathExperienceChanged?.Invoke(state, delta);
        OnLifePathLogChanged?.Invoke();

        string message = oldEarned != state.earnedPerkPoints
            ? $"{lifePath.DisplayName} gained {delta} XP and earned a perk point."
            : $"{lifePath.DisplayName} gained {delta} XP.";
        lifePath.PublishChanged(GetComponent<PlayerController>(), "experience", message, context != null ? context : this,
            GameEventPublishing.Value("lifePathId", lifePath.Id),
            GameEventPublishing.Value("lifePathName", lifePath.DisplayName),
            GameEventPublishing.Value("delta", delta),
            GameEventPublishing.Value("totalExperience", state.totalExperience),
            GameEventPublishing.Value("earnedPerkPoints", state.earnedPerkPoints),
            GameEventPublishing.Value("availablePerkPoints", Mathf.Max(0, state.earnedPerkPoints - state.spentPerkPoints)),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("sourceName", sourceName));
        return state;
    }

    public PlayerLifePathState AddBranchProgress(LifePathDefinition lifePath, string branchId, int amount, string sourceId = null, string sourceName = null, UnityEngine.Object context = null) {
        if(lifePath == null || string.IsNullOrWhiteSpace(branchId) || amount <= 0) {
            return null;
        }

        var state = EnsureState(lifePath);
        int after = state.AddBranchProgress(branchId, amount);
        state.lastSourceId = sourceId;
        state.lastSourceName = sourceName;
        state.lastChangedHour = GetCurrentTotalHour();
        RecordChange(LifePathRecordKind.BranchProgress, state, amount, sourceId, sourceName, branchId: branchId, context: context);
        OnBranchProgressChanged?.Invoke(state, branchId, amount);
        OnLifePathLogChanged?.Invoke();
        lifePath.PublishChanged(GetComponent<PlayerController>(), "branch", $"{lifePath.DisplayName} branch {branchId} gained {amount} progress.", context != null ? context : this,
            GameEventPublishing.Value("lifePathId", lifePath.Id),
            GameEventPublishing.Value("branchId", branchId),
            GameEventPublishing.Value("delta", amount),
            GameEventPublishing.Value("branchProgress", after),
            GameEventPublishing.Value("sourceId", sourceId));
        return state;
    }

    public PlayerLifePathState AddTagProgress(LifePathDefinition lifePath, string tag, int amount, string sourceId = null, string sourceName = null, UnityEngine.Object context = null) {
        if(lifePath == null || string.IsNullOrWhiteSpace(tag) || amount <= 0) {
            return null;
        }

        var state = EnsureState(lifePath);
        int after = state.AddTagProgress(tag, amount);
        state.lastSourceId = sourceId;
        state.lastSourceName = sourceName;
        state.lastChangedHour = GetCurrentTotalHour();
        RecordChange(LifePathRecordKind.TagProgress, state, amount, sourceId, sourceName, tag: tag, context: context);
        OnTagProgressChanged?.Invoke(state, tag, amount);
        OnLifePathLogChanged?.Invoke();
        lifePath.PublishChanged(GetComponent<PlayerController>(), "tag", $"{lifePath.DisplayName} tag {tag} gained {amount} progress.", context != null ? context : this,
            GameEventPublishing.Value("lifePathId", lifePath.Id),
            GameEventPublishing.Value("tag", tag),
            GameEventPublishing.Value("delta", amount),
            GameEventPublishing.Value("tagProgress", after),
            GameEventPublishing.Value("sourceId", sourceId));
        return state;
    }

    public bool CanUnlockPerk(LifePathPerkDefinition perk, out string failureMessage) {
        return CanUnlockPerk(perk, false, out failureMessage);
    }

    public bool CanUnlockPerk(LifePathPerkDefinition perk, bool ignoreCost, out string failureMessage) {
        if(perk == null) {
            failureMessage = "No perk selected.";
            return false;
        }

        var player = GetComponent<PlayerController>();
        if(!perk.CanUnlock(player, this, out failureMessage)) {
            return false;
        }

        if(!ignoreCost && GetAvailablePerkPoints(perk.LifePath) < perk.PerkPointCost) {
            failureMessage = $"{perk.LifePath.DisplayName} needs {perk.PerkPointCost} available perk point(s).";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool UnlockPerk(LifePathPerkDefinition perk, string sourceId, UnityEngine.Object context, out string failureMessage) {
        return UnlockPerk(perk, sourceId, context, ignoreCost: false, ignoreRequirements: false, out failureMessage);
    }

    public bool UnlockPerk(LifePathPerkDefinition perk, string sourceId, UnityEngine.Object context, bool ignoreCost, bool ignoreRequirements, out string failureMessage) {
        if(perk == null) {
            failureMessage = "No perk selected.";
            return false;
        }

        var lifePath = perk.LifePath;
        if(lifePath == null) {
            failureMessage = $"{perk.DisplayName} has no life path.";
            return false;
        }

        var state = EnsureState(lifePath);
        if(HasPerk(perk)) {
            failureMessage = null;
            return false;
        }

        if(!ignoreRequirements && !CanUnlockPerk(perk, ignoreCost, out failureMessage)) {
            return false;
        }

        if(!ignoreCost && perk.PerkPointCost > 0) {
            state.spentPerkPoints += perk.PerkPointCost;
        }

        var record = new PlayerLifePathPerkState {
            perkId = perk.Id,
            perkName = perk.DisplayName,
            lifePathId = lifePath.Id,
            lifePathName = lifePath.DisplayName,
            branchId = perk.BranchId,
            perkPointCost = ignoreCost ? 0 : perk.PerkPointCost,
            unlockedAtHour = GetCurrentTotalHour(),
            sourceId = sourceId
        };
        unlockedPerks.Add(record);
        RecordChange(LifePathRecordKind.PerkUnlocked, state, 0, sourceId, perk.DisplayName, perkId: perk.Id, branchId: perk.BranchId, context: context);
        perk.ApplyUnlockEffects(GetComponent<PlayerController>(), sourceId, context != null ? context : this);
        OnPerkUnlocked?.Invoke(perk);
        OnLifePathLogChanged?.Invoke();
        lifePath.PublishChanged(GetComponent<PlayerController>(), "perk", $"{perk.DisplayName} unlocked.", context != null ? context : this,
            GameEventPublishing.Value("lifePathId", lifePath.Id),
            GameEventPublishing.Value("perkId", perk.Id),
            GameEventPublishing.Value("perkName", perk.DisplayName),
            GameEventPublishing.Value("availablePerkPoints", GetAvailablePerkPoints(lifePath)),
            GameEventPublishing.Value("sourceId", sourceId));

        failureMessage = null;
        return true;
    }

    public LifePathSnapshot GetSnapshot() {
        EnsureDefaultPerks();
        var rows = GetStatesWithDefinitions()
            .Select(pair => LifePathSnapshotRow.From(pair.state, pair.definition, this))
            .Where(row => row != null)
            .OrderByDescending(row => row.totalExperience)
            .ThenBy(row => row.displayName)
            .ToList();

        var perkRows = unlockedPerks
            .Where(perk => perk != null)
            .Select(perk => new LifePathPerkSnapshot(perk))
            .ToList();

        return new LifePathSnapshot(rows, perkRows, records.Where(record => record != null).Select(record => new PlayerLifePathRecord(record)).ToList());
    }

    PlayerLifePathState EnsureState(LifePathDefinition lifePath) {
        var state = GetState(lifePath);
        if(state != null) {
            state.lifePathName = lifePath.DisplayName;
            state.category = lifePath.Category;
            state.earnedPerkPoints = Mathf.Max(state.earnedPerkPoints, lifePath.CalculateEarnedPerkPoints(state.totalExperience));
            return state;
        }

        state = new PlayerLifePathState {
            lifePathId = lifePath.Id,
            lifePathName = lifePath.DisplayName,
            category = lifePath.Category,
            totalExperience = 0,
            earnedPerkPoints = 0,
            spentPerkPoints = 0,
            lastChangedHour = -1
        };
        lifePaths.Add(state);
        return state;
    }

    void EnsureDefaultPerks() {
        if(ensuringDefaultPerks) {
            return;
        }

        ensuringDefaultPerks = true;
        foreach(var path in Resources.LoadAll<LifePathDefinition>("")) {
            if(path != null) {
                EnsureDefaultPerks(path);
            }
        }
        ensuringDefaultPerks = false;
    }

    void EnsureDefaultPerks(LifePathDefinition lifePath) {
        if(lifePath == null || lifePath.Perks == null) {
            return;
        }

        foreach(var perk in lifePath.Perks) {
            if(perk != null && perk.UnlockedByDefault && !HasPerk(perk)) {
                UnlockPerk(perk, "default", this, ignoreCost: true, ignoreRequirements: true, out _);
            }
        }
    }

    IEnumerable<(PlayerLifePathState state, LifePathDefinition definition)> GetStatesWithDefinitions() {
        foreach(var state in lifePaths) {
            if(state == null) {
                continue;
            }

            yield return (state, ResolveLifePath(state.lifePathId));
        }
    }

    LifePathDefinition ResolveLifePath(string lifePathId) {
        if(string.IsNullOrWhiteSpace(lifePathId)) {
            return null;
        }

        return Resources.LoadAll<LifePathDefinition>("").FirstOrDefault(path => path != null && path.Id == lifePathId);
    }

    void RecordChange(LifePathRecordKind kind, PlayerLifePathState state, int delta, string sourceId, string sourceName, string branchId = null, string tag = null, string perkId = null, UnityEngine.Object context = null) {
        records.Add(new PlayerLifePathRecord {
            kind = kind,
            lifePathId = state.lifePathId,
            lifePathName = state.lifePathName,
            category = state.category,
            delta = delta,
            resultingExperience = state.totalExperience,
            earnedPerkPoints = state.earnedPerkPoints,
            spentPerkPoints = state.spentPerkPoints,
            branchId = branchId,
            tag = tag,
            perkId = perkId,
            sourceId = sourceId,
            sourceName = sourceName,
            recordedAtHour = GetCurrentTotalHour(),
            frame = Time.frameCount
        });
        TrimRecords();
    }

    void TrimRecords() {
        if(maxRecords <= 0) {
            return;
        }

        while(records.Count > maxRecords) {
            records.RemoveAt(0);
        }
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerLifePathLogSaveData {
            lifePaths = lifePaths.Where(state => state != null).Select(state => new PlayerLifePathState(state)).ToList(),
            unlockedPerks = unlockedPerks.Where(perk => perk != null).Select(perk => new PlayerLifePathPerkState(perk)).ToList(),
            records = records.Where(record => record != null).Select(record => new PlayerLifePathRecord(record)).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerLifePathLogSaveData;
        lifePaths = saveData?.lifePaths?.Where(entry => entry != null).Select(entry => new PlayerLifePathState(entry)).ToList()
            ?? new List<PlayerLifePathState>();
        unlockedPerks = saveData?.unlockedPerks?.Where(entry => entry != null).Select(entry => new PlayerLifePathPerkState(entry)).ToList()
            ?? new List<PlayerLifePathPerkState>();
        records = saveData?.records?.Where(entry => entry != null).Select(entry => new PlayerLifePathRecord(entry)).ToList()
            ?? new List<PlayerLifePathRecord>();
        OnLifePathLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerLifePathState {
    [Tooltip("Saved life path definition id.")]
    public string lifePathId;
    [Tooltip("Saved life path display name for fallback/debug output.")]
    public string lifePathName;
    [Tooltip("Saved life path category.")]
    public LifePathCategory category;
    [Tooltip("Total XP earned in this life path.")]
    [Min(0)]
    public int totalExperience;
    [Tooltip("Total perk points earned from this life path.")]
    [Min(0)]
    public int earnedPerkPoints;
    [Tooltip("Perk points spent in this life path.")]
    [Min(0)]
    public int spentPerkPoints;
    [Tooltip("Branch progress values saved for this life path.")]
    public List<LifePathProgressValue> branchProgress = new List<LifePathProgressValue>();
    [Tooltip("Activity/behavior tag counters saved for this life path.")]
    public List<LifePathProgressValue> tagProgress = new List<LifePathProgressValue>();
    [Tooltip("Most recent XP delta.")]
    public int lastDelta;
    [Tooltip("Source id of the most recent change.")]
    public string lastSourceId;
    [Tooltip("Source display name of the most recent change.")]
    public string lastSourceName;
    [Tooltip("Total in-game hour when this life path last changed.")]
    public int lastChangedHour;

    public PlayerLifePathState() {
    }

    public PlayerLifePathState(PlayerLifePathState other) {
        lifePathId = other.lifePathId;
        lifePathName = other.lifePathName;
        category = other.category;
        totalExperience = Mathf.Max(0, other.totalExperience);
        earnedPerkPoints = Mathf.Max(0, other.earnedPerkPoints);
        spentPerkPoints = Mathf.Max(0, other.spentPerkPoints);
        branchProgress = other.branchProgress?.Where(value => value != null).Select(value => new LifePathProgressValue(value)).ToList() ?? new List<LifePathProgressValue>();
        tagProgress = other.tagProgress?.Where(value => value != null).Select(value => new LifePathProgressValue(value)).ToList() ?? new List<LifePathProgressValue>();
        lastDelta = other.lastDelta;
        lastSourceId = other.lastSourceId;
        lastSourceName = other.lastSourceName;
        lastChangedHour = other.lastChangedHour;
    }

    public int GetBranchProgress(string branchId) {
        return GetValue(branchProgress, branchId);
    }

    public int GetTagProgress(string tag) {
        return GetValue(tagProgress, tag);
    }

    public int AddBranchProgress(string branchId, int amount) {
        return AddValue(branchProgress, branchId, amount);
    }

    public int AddTagProgress(string tag, int amount) {
        return AddValue(tagProgress, tag, amount);
    }

    int GetValue(List<LifePathProgressValue> values, string key) {
        if(values == null || string.IsNullOrWhiteSpace(key)) {
            return 0;
        }

        return values.FirstOrDefault(value => value != null && string.Equals(value.id, key, StringComparison.OrdinalIgnoreCase))?.value ?? 0;
    }

    int AddValue(List<LifePathProgressValue> values, string key, int amount) {
        if(values == null || string.IsNullOrWhiteSpace(key) || amount <= 0) {
            return 0;
        }

        var value = values.FirstOrDefault(entry => entry != null && string.Equals(entry.id, key, StringComparison.OrdinalIgnoreCase));
        if(value == null) {
            value = new LifePathProgressValue { id = key, value = 0 };
            values.Add(value);
        }

        value.value = Mathf.Max(0, value.value + amount);
        return value.value;
    }
}

[Serializable]
public class LifePathProgressValue {
    [Tooltip("Branch id or tag id.")]
    public string id;
    [Tooltip("Saved progress/count value.")]
    [Min(0)]
    public int value;

    public LifePathProgressValue() {
    }

    public LifePathProgressValue(LifePathProgressValue other) {
        id = other.id;
        value = Mathf.Max(0, other.value);
    }
}

[Serializable]
public class PlayerLifePathPerkState {
    [Tooltip("Saved perk id.")]
    public string perkId;
    [Tooltip("Saved perk display name.")]
    public string perkName;
    [Tooltip("Life path id this perk belongs to.")]
    public string lifePathId;
    [Tooltip("Life path display name this perk belongs to.")]
    public string lifePathName;
    [Tooltip("Branch id this perk belongs to, if any.")]
    public string branchId;
    [Tooltip("Perk points spent when this perk was unlocked.")]
    [Min(0)]
    public int perkPointCost;
    [Tooltip("Total in-game hour when this perk unlocked.")]
    public int unlockedAtHour;
    [Tooltip("Source id that unlocked this perk.")]
    public string sourceId;

    public PlayerLifePathPerkState() {
    }

    public PlayerLifePathPerkState(PlayerLifePathPerkState other) {
        perkId = other.perkId;
        perkName = other.perkName;
        lifePathId = other.lifePathId;
        lifePathName = other.lifePathName;
        branchId = other.branchId;
        perkPointCost = Mathf.Max(0, other.perkPointCost);
        unlockedAtHour = other.unlockedAtHour;
        sourceId = other.sourceId;
    }
}

[Serializable]
public class PlayerLifePathRecord {
    [Tooltip("Kind of life path change recorded.")]
    public LifePathRecordKind kind;
    [Tooltip("Life path id affected by this record.")]
    public string lifePathId;
    [Tooltip("Life path display name affected by this record.")]
    public string lifePathName;
    [Tooltip("Life path category affected by this record.")]
    public LifePathCategory category;
    [Tooltip("Delta applied by this record.")]
    public int delta;
    [Tooltip("Total XP after this record.")]
    public int resultingExperience;
    [Tooltip("Earned perk points after this record.")]
    public int earnedPerkPoints;
    [Tooltip("Spent perk points after this record.")]
    public int spentPerkPoints;
    [Tooltip("Branch id affected by this record, if any.")]
    public string branchId;
    [Tooltip("Tag affected by this record, if any.")]
    public string tag;
    [Tooltip("Perk id affected by this record, if any.")]
    public string perkId;
    [Tooltip("Source id that caused this record.")]
    public string sourceId;
    [Tooltip("Source display name that caused this record.")]
    public string sourceName;
    [Tooltip("Total in-game hour when this record was created.")]
    public int recordedAtHour;
    [Tooltip("Unity frame when this record was created.")]
    public int frame;

    public PlayerLifePathRecord() {
    }

    public PlayerLifePathRecord(PlayerLifePathRecord other) {
        kind = other.kind;
        lifePathId = other.lifePathId;
        lifePathName = other.lifePathName;
        category = other.category;
        delta = other.delta;
        resultingExperience = other.resultingExperience;
        earnedPerkPoints = other.earnedPerkPoints;
        spentPerkPoints = other.spentPerkPoints;
        branchId = other.branchId;
        tag = other.tag;
        perkId = other.perkId;
        sourceId = other.sourceId;
        sourceName = other.sourceName;
        recordedAtHour = other.recordedAtHour;
        frame = other.frame;
    }
}

[Serializable]
public class PlayerLifePathLogSaveData {
    public List<PlayerLifePathState> lifePaths = new List<PlayerLifePathState>();
    public List<PlayerLifePathPerkState> unlockedPerks = new List<PlayerLifePathPerkState>();
    public List<PlayerLifePathRecord> records = new List<PlayerLifePathRecord>();
}

[Serializable]
public class LifePathSnapshot {
    public List<LifePathSnapshotRow> paths = new List<LifePathSnapshotRow>();
    public List<LifePathPerkSnapshot> unlockedPerks = new List<LifePathPerkSnapshot>();
    public List<PlayerLifePathRecord> records = new List<PlayerLifePathRecord>();

    public LifePathSnapshot() {
    }

    public LifePathSnapshot(List<LifePathSnapshotRow> paths, List<LifePathPerkSnapshot> unlockedPerks, List<PlayerLifePathRecord> records) {
        this.paths = paths ?? new List<LifePathSnapshotRow>();
        this.unlockedPerks = unlockedPerks ?? new List<LifePathPerkSnapshot>();
        this.records = records ?? new List<PlayerLifePathRecord>();
    }
}

[Serializable]
public class LifePathSnapshotRow {
    public string lifePathId;
    public string displayName;
    public LifePathCategory category;
    public int totalExperience;
    public int earnedPerkPoints;
    public int spentPerkPoints;
    public int availablePerkPoints;
    public string description;
    public List<LifePathProgressValue> branches = new List<LifePathProgressValue>();
    public List<LifePathProgressValue> tags = new List<LifePathProgressValue>();

    public static LifePathSnapshotRow From(PlayerLifePathState state, LifePathDefinition definition, PlayerLifePathLog log) {
        if(state == null) {
            return null;
        }

        int earned = definition != null ? log.GetEarnedPerkPoints(definition) : state.earnedPerkPoints;
        return new LifePathSnapshotRow {
            lifePathId = state.lifePathId,
            displayName = definition != null ? definition.DisplayName : state.lifePathName,
            category = definition != null ? definition.Category : state.category,
            totalExperience = state.totalExperience,
            earnedPerkPoints = earned,
            spentPerkPoints = state.spentPerkPoints,
            availablePerkPoints = Mathf.Max(0, earned - state.spentPerkPoints),
            description = definition != null ? definition.Description : string.Empty,
            branches = state.branchProgress?.Select(value => new LifePathProgressValue(value)).ToList() ?? new List<LifePathProgressValue>(),
            tags = state.tagProgress?.Select(value => new LifePathProgressValue(value)).ToList() ?? new List<LifePathProgressValue>()
        };
    }
}

[Serializable]
public class LifePathPerkSnapshot {
    public string perkId;
    public string perkName;
    public string lifePathId;
    public string lifePathName;
    public string branchId;
    public int perkPointCost;
    public int unlockedAtHour;
    public string sourceId;

    public LifePathPerkSnapshot() {
    }

    public LifePathPerkSnapshot(PlayerLifePathPerkState state) {
        perkId = state.perkId;
        perkName = state.perkName;
        lifePathId = state.lifePathId;
        lifePathName = state.lifePathName;
        branchId = state.branchId;
        perkPointCost = state.perkPointCost;
        unlockedAtHour = state.unlockedAtHour;
        sourceId = state.sourceId;
    }
}
