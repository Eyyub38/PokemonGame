using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerServicePackageLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum service package history records kept in memory/save data. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRecords = 160;
    [Tooltip("Runtime/save history of successful and optionally blocked package uses.")]
    [SerializeField] List<PlayerServicePackageRecord> records = new List<PlayerServicePackageRecord>();

    public IReadOnlyList<PlayerServicePackageRecord> Records => records;
    public event Action<ServicePackageDefinition, PlayerServicePackageRecord> OnPackageRecorded;

    public bool CanUse(
        ServicePackageDefinition package,
        string sourceId,
        ConsequenceChainRepeatMode repeatMode,
        int cooldownHours,
        int maxUseCount,
        out string failureMessage
    ) {
        if(package == null) {
            failureMessage = "No service package selected.";
            return false;
        }

        string normalizedSourceId = NormalizeSourceId(sourceId);
        int totalSuccessfulUses = GetUseCount(package, includeBlocked: false);
        if(maxUseCount > 0 && totalSuccessfulUses >= maxUseCount) {
            failureMessage = $"{package.DisplayName} has reached its maximum use count.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OnceEver && totalSuccessfulUses > 0) {
            failureMessage = $"{package.DisplayName} has already been used.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OncePerSource && GetUseCount(package, normalizedSourceId, includeBlocked: false) > 0) {
            failureMessage = $"{package.DisplayName} has already been used from this source.";
            return false;
        }

        var lastSourceUse = GetLastUse(package, normalizedSourceId, includeBlocked: false);
        if(repeatMode == ConsequenceChainRepeatMode.Daily && lastSourceUse != null && lastSourceUse.day == GetCurrentDay()) {
            failureMessage = $"{package.DisplayName} can only be used once per day from this source.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.CooldownHours && lastSourceUse != null) {
            int elapsed = GetCurrentAbsoluteHour() - lastSourceUse.absoluteHour;
            int cooldown = Mathf.Max(0, cooldownHours);
            if(elapsed < cooldown) {
                failureMessage = $"{package.DisplayName} will be available again in {cooldown - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public PlayerServicePackageRecord RecordUse(ServicePackageDefinition package, ServicePackageUseResult result) {
        if(package == null) {
            return null;
        }

        var record = new PlayerServicePackageRecord {
            recordId = Guid.NewGuid().ToString("N"),
            packageId = package.Id,
            packageName = package.DisplayName,
            category = package.Category,
            sourceId = NormalizeSourceId(result != null ? result.sourceId : null),
            sourceName = result != null ? result.sourceName : null,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour(),
            blocked = result != null && result.blocked,
            failureMessage = result != null ? result.failureMessage : null,
            packagePricePaid = result != null ? Mathf.Max(0f, result.packagePricePaid) : 0f,
            individualPricePaid = result != null ? Mathf.Max(0f, result.individualPricePaid) : 0f,
            appliedServiceCount = result != null ? Mathf.Max(0, result.appliedServiceCount) : 0,
            blockedServiceCount = result != null ? Mathf.Max(0, result.blockedServiceCount) : 0,
            purchasedOfferCount = result != null ? Mathf.Max(0, result.purchasedOfferCount) : 0,
            blockedOfferCount = result != null ? Mathf.Max(0, result.blockedOfferCount) : 0,
            affectedPokemonCount = result != null ? Mathf.Max(0, result.affectedPokemonCount) : 0,
            appliedGrantCount = result != null ? Mathf.Max(0, result.appliedGrantCount) : 0,
            appliedChainCount = result != null ? Mathf.Max(0, result.appliedChainCount) : 0,
            failedChainCount = result != null ? Mathf.Max(0, result.failedChainCount) : 0,
            messages = result != null && result.messages != null
                ? result.messages.Where(message => !string.IsNullOrWhiteSpace(message)).ToList()
                : new List<string>(),
            tags = package.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };

        records.Add(record);
        TrimHistory();
        OnPackageRecorded?.Invoke(package, record);
        return record;
    }

    public bool HasUsed(ServicePackageDefinition package = null, string sourceId = null, bool includeBlocked = false) {
        return GetUseCount(package, sourceId, includeBlocked: includeBlocked) > 0;
    }

    public int GetUseCount(ServicePackageDefinition package = null, string sourceId = null, bool includeBlocked = false) {
        string packageId = package != null ? package.Id : null;
        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        return records.Count(record => Matches(record, packageId, normalizedSourceId, null, null, includeBlocked));
    }

    public int GetCategoryUseCount(ServicePackageCategory category, bool includeBlocked = false) {
        return records.Count(record => record != null
            && record.category == category
            && (includeBlocked || !record.blocked));
    }

    public int GetTaggedUseCount(string requiredTag, string sourceId = null, bool includeBlocked = false) {
        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        return records.Count(record => Matches(record, null, normalizedSourceId, null, requiredTag, includeBlocked));
    }

    public int GetTodayUseCount(ServicePackageDefinition package = null, string sourceId = null, bool includeBlocked = false) {
        string packageId = package != null ? package.Id : null;
        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        int day = GetCurrentDay();
        return records.Count(record => Matches(record, packageId, normalizedSourceId, null, null, includeBlocked) && record.day == day);
    }

    public PlayerServicePackageRecord GetLastUse(ServicePackageDefinition package = null, string sourceId = null, bool includeBlocked = false) {
        string packageId = package != null ? package.Id : null;
        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        return records
            .Where(record => Matches(record, packageId, normalizedSourceId, null, null, includeBlocked))
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.day)
            .FirstOrDefault();
    }

    public int GetHoursSinceLastUse(ServicePackageDefinition package = null, string sourceId = null, bool includeBlocked = false) {
        var lastUse = GetLastUse(package, sourceId, includeBlocked);
        return lastUse != null ? Mathf.Max(0, GetCurrentAbsoluteHour() - lastUse.absoluteHour) : -1;
    }

    public void ClearHistoryBefore(int absoluteHour) {
        records.RemoveAll(record => record != null && record.absoluteHour < absoluteHour);
    }

    bool Matches(PlayerServicePackageRecord record, string packageId, string sourceId, ServicePackageCategory? category, string requiredTag, bool includeBlocked) {
        return record != null
            && (includeBlocked || !record.blocked)
            && (string.IsNullOrWhiteSpace(packageId) || record.packageId == packageId)
            && (string.IsNullOrWhiteSpace(sourceId) || record.sourceId == sourceId)
            && (!category.HasValue || record.category == category.Value)
            && (string.IsNullOrWhiteSpace(requiredTag) || record.HasTag(requiredTag));
    }

    void TrimHistory() {
        if(maxHistoryRecords <= 0 || records.Count <= maxHistoryRecords) {
            return;
        }

        records = records
            .Where(record => record != null)
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.day)
            .Take(maxHistoryRecords)
            .OrderBy(record => record.absoluteHour)
            .ThenBy(record => record.day)
            .ToList();
    }

    static string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? "service-package" : sourceId;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        TrimHistory();
        return new PlayerServicePackageLogSaveData {
            records = records.Where(record => record != null).Select(record => record.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerServicePackageLogSaveData;
        records = saveData?.records?.Where(record => record != null).Select(record => record.Clone()).ToList()
            ?? new List<PlayerServicePackageRecord>();
        TrimHistory();
    }
}

[Serializable]
public class PlayerServicePackageRecord {
    [Tooltip("Unique runtime/save id for this service package record.")]
    public string recordId;
    [Tooltip("Saved service package id.")]
    public string packageId;
    [Tooltip("Saved service package display name for fallback/debug output.")]
    public string packageName;
    [Tooltip("Service package category at the time of use.")]
    public ServicePackageCategory category;
    [Tooltip("Source id used by package repeat rules.")]
    public string sourceId;
    [Tooltip("Source display name for debug/future UI.")]
    public string sourceName;
    [Tooltip("In-game day when this package was used or blocked.")]
    public int day;
    [Tooltip("Absolute in-game hour when this package was used or blocked.")]
    public int absoluteHour;
    [Tooltip("Whether this record is a blocked attempt instead of a successful package use.")]
    public bool blocked;
    [Tooltip("Failure message stored for blocked attempts.")]
    public string failureMessage;
    [Tooltip("Money paid directly to the package price.")]
    public float packagePricePaid;
    [Tooltip("Money paid by included services or learnable offers.")]
    public float individualPricePaid;
    [Tooltip("Number of included services successfully applied.")]
    public int appliedServiceCount;
    [Tooltip("Number of included services that were blocked.")]
    public int blockedServiceCount;
    [Tooltip("Number of included learnable offers successfully purchased.")]
    public int purchasedOfferCount;
    [Tooltip("Number of included learnable offers that were blocked.")]
    public int blockedOfferCount;
    [Tooltip("Number of Pokemon affected by included services.")]
    public int affectedPokemonCount;
    [Tooltip("Number of grants that changed player state.")]
    public int appliedGrantCount;
    [Tooltip("Number of consequence chain steps applied by the package or included services.")]
    public int appliedChainCount;
    [Tooltip("Number of consequence chains that failed or were blocked.")]
    public int failedChainCount;
    [Tooltip("Extra debug messages collected while applying this package.")]
    public List<string> messages = new List<string>();
    [Tooltip("Package tags copied at use time.")]
    public List<string> tags = new List<string>();

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public PlayerServicePackageRecord Clone() {
        return new PlayerServicePackageRecord {
            recordId = recordId,
            packageId = packageId,
            packageName = packageName,
            category = category,
            sourceId = sourceId,
            sourceName = sourceName,
            day = day,
            absoluteHour = absoluteHour,
            blocked = blocked,
            failureMessage = failureMessage,
            packagePricePaid = packagePricePaid,
            individualPricePaid = individualPricePaid,
            appliedServiceCount = appliedServiceCount,
            blockedServiceCount = blockedServiceCount,
            purchasedOfferCount = purchasedOfferCount,
            blockedOfferCount = blockedOfferCount,
            affectedPokemonCount = affectedPokemonCount,
            appliedGrantCount = appliedGrantCount,
            appliedChainCount = appliedChainCount,
            failedChainCount = failedChainCount,
            messages = messages != null ? messages.ToList() : new List<string>(),
            tags = tags != null ? tags.ToList() : new List<string>()
        };
    }
}

[Serializable]
public class PlayerServicePackageLogSaveData {
    public List<PlayerServicePackageRecord> records = new List<PlayerServicePackageRecord>();
}
