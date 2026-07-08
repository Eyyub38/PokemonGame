using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerServiceLog : MonoBehaviour, ISavable {
    [Tooltip("Maximum service history records kept in memory/save data. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxHistoryRecords = 240;
    [Tooltip("Runtime/save history of successful and optionally blocked service uses.")]
    [SerializeField] List<PlayerServiceRecord> records = new List<PlayerServiceRecord>();

    public IReadOnlyList<PlayerServiceRecord> Records => records;
    public event Action<ServiceDefinition, PlayerServiceRecord> OnServiceRecorded;

    public bool CanUse(
        ServiceDefinition service,
        string providerId,
        ConsequenceChainRepeatMode repeatMode,
        int cooldownHours,
        int maxUseCount,
        out string failureMessage
    ) {
        if(service == null) {
            failureMessage = "No service selected.";
            return false;
        }

        string normalizedProviderId = NormalizeProviderId(providerId);
        int totalSuccessfulUses = GetUseCount(service, includeBlocked: false);
        if(maxUseCount > 0 && totalSuccessfulUses >= maxUseCount) {
            failureMessage = $"{service.DisplayName} has reached its maximum use count.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OnceEver && totalSuccessfulUses > 0) {
            failureMessage = $"{service.DisplayName} has already been used.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.OncePerSource && GetUseCount(service, normalizedProviderId, includeBlocked: false) > 0) {
            failureMessage = $"{service.DisplayName} has already been used from this provider.";
            return false;
        }

        var lastProviderUse = GetLastUse(service, normalizedProviderId, includeBlocked: false);
        if(repeatMode == ConsequenceChainRepeatMode.Daily && lastProviderUse != null && lastProviderUse.day == GetCurrentDay()) {
            failureMessage = $"{service.DisplayName} can only be used once per day from this provider.";
            return false;
        }

        if(repeatMode == ConsequenceChainRepeatMode.CooldownHours && lastProviderUse != null) {
            int elapsed = GetCurrentAbsoluteHour() - lastProviderUse.absoluteHour;
            int cooldown = Mathf.Max(0, cooldownHours);
            if(elapsed < cooldown) {
                failureMessage = $"{service.DisplayName} will be available again in {cooldown - elapsed} hour(s).";
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public PlayerServiceRecord RecordUse(ServiceDefinition service, ServiceUseResult result) {
        if(service == null) {
            return null;
        }

        var record = new PlayerServiceRecord {
            recordId = Guid.NewGuid().ToString("N"),
            serviceId = service.Id,
            serviceName = service.DisplayName,
            category = service.Category,
            providerId = NormalizeProviderId(result != null ? result.providerId : null),
            providerName = result != null ? result.providerName : null,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour(),
            blocked = result != null && result.blocked,
            failureMessage = result != null ? result.failureMessage : null,
            moneyPaid = result != null ? result.moneyPaid : 0f,
            moneyRewarded = result != null ? result.moneyRewarded : 0f,
            affectedPokemonCount = result != null ? Mathf.Max(0, result.affectedPokemonCount) : 0,
            appliedChainCount = result != null ? Mathf.Max(0, result.appliedChainCount) : 0,
            failedChainCount = result != null ? Mathf.Max(0, result.failedChainCount) : 0,
            messages = result != null && result.messages != null
                ? result.messages.Where(message => !string.IsNullOrWhiteSpace(message)).ToList()
                : new List<string>()
        };

        records.Add(record);
        TrimHistory();
        OnServiceRecorded?.Invoke(service, record);
        return record;
    }

    public int GetUseCount(ServiceDefinition service, string providerId = null, bool includeBlocked = false) {
        if(service == null) {
            return 0;
        }

        string normalizedProviderId = string.IsNullOrWhiteSpace(providerId) ? null : NormalizeProviderId(providerId);
        return records.Count(record => record != null
            && record.serviceId == service.Id
            && (includeBlocked || !record.blocked)
            && (string.IsNullOrWhiteSpace(normalizedProviderId) || record.providerId == normalizedProviderId));
    }

    public int GetCategoryUseCount(PlayerServiceCategory category, bool includeBlocked = false) {
        return records.Count(record => record != null
            && record.category == category
            && (includeBlocked || !record.blocked));
    }

    public int GetTodayUseCount(ServiceDefinition service, string providerId = null, bool includeBlocked = false) {
        if(service == null) {
            return 0;
        }

        string normalizedProviderId = string.IsNullOrWhiteSpace(providerId) ? null : NormalizeProviderId(providerId);
        int day = GetCurrentDay();
        return records.Count(record => record != null
            && record.serviceId == service.Id
            && record.day == day
            && (includeBlocked || !record.blocked)
            && (string.IsNullOrWhiteSpace(normalizedProviderId) || record.providerId == normalizedProviderId));
    }

    public bool HasUsed(ServiceDefinition service, string providerId = null, bool includeBlocked = false) {
        return GetUseCount(service, providerId, includeBlocked) > 0;
    }

    public PlayerServiceRecord GetLastUse(ServiceDefinition service, string providerId = null, bool includeBlocked = false) {
        if(service == null) {
            return null;
        }

        string normalizedProviderId = string.IsNullOrWhiteSpace(providerId) ? null : NormalizeProviderId(providerId);
        return records
            .Where(record => record != null
                && record.serviceId == service.Id
                && (includeBlocked || !record.blocked)
                && (string.IsNullOrWhiteSpace(normalizedProviderId) || record.providerId == normalizedProviderId))
            .OrderByDescending(record => record.absoluteHour)
            .ThenByDescending(record => record.day)
            .FirstOrDefault();
    }

    public int GetHoursSinceLastUse(ServiceDefinition service, string providerId = null, bool includeBlocked = false) {
        var lastUse = GetLastUse(service, providerId, includeBlocked);
        return lastUse != null ? Mathf.Max(0, GetCurrentAbsoluteHour() - lastUse.absoluteHour) : -1;
    }

    public void ClearHistoryBefore(int absoluteHour) {
        records.RemoveAll(record => record != null && record.absoluteHour < absoluteHour);
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

    static string NormalizeProviderId(string providerId) {
        return string.IsNullOrWhiteSpace(providerId) ? "service" : providerId;
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        TrimHistory();
        return new PlayerServiceLogSaveData {
            records = records.Where(record => record != null).Select(record => record.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerServiceLogSaveData;
        records = saveData?.records?.Where(record => record != null).Select(record => new PlayerServiceRecord(record)).ToList()
            ?? new List<PlayerServiceRecord>();
        TrimHistory();
    }
}

[Serializable]
public class PlayerServiceRecord {
    [Tooltip("Unique runtime/save id for this service record.")]
    public string recordId;
    [Tooltip("Saved service id.")]
    public string serviceId;
    [Tooltip("Saved service display name for fallback/debug output.")]
    public string serviceName;
    [Tooltip("Service category at the time of use.")]
    public PlayerServiceCategory category;
    [Tooltip("Provider/source id used by repeat rules.")]
    public string providerId;
    [Tooltip("Provider/source display name for debug/future UI.")]
    public string providerName;
    [Tooltip("In-game day when this service was used or blocked.")]
    public int day;
    [Tooltip("Absolute in-game hour when this service was used or blocked.")]
    public int absoluteHour;
    [Tooltip("Whether this record is a blocked attempt instead of a successful use.")]
    public bool blocked;
    [Tooltip("Failure message stored for blocked attempts.")]
    public string failureMessage;
    [Tooltip("Money paid by this service use.")]
    public float moneyPaid;
    [Tooltip("Money rewarded by this service use.")]
    public float moneyRewarded;
    [Tooltip("Number of Pokemon affected by this service.")]
    public int affectedPokemonCount;
    [Tooltip("Number of consequence chain steps applied after this service.")]
    public int appliedChainCount;
    [Tooltip("Number of consequence chains that failed or were blocked.")]
    public int failedChainCount;
    [Tooltip("Extra debug messages collected while applying this service.")]
    public List<string> messages = new List<string>();

    public PlayerServiceRecord() {
    }

    public PlayerServiceRecord(PlayerServiceRecordSaveData saveData) {
        recordId = saveData.recordId;
        serviceId = saveData.serviceId;
        serviceName = saveData.serviceName;
        category = saveData.category;
        providerId = saveData.providerId;
        providerName = saveData.providerName;
        day = saveData.day;
        absoluteHour = saveData.absoluteHour;
        blocked = saveData.blocked;
        failureMessage = saveData.failureMessage;
        moneyPaid = saveData.moneyPaid;
        moneyRewarded = saveData.moneyRewarded;
        affectedPokemonCount = saveData.affectedPokemonCount;
        appliedChainCount = saveData.appliedChainCount;
        failedChainCount = saveData.failedChainCount;
        messages = saveData.messages ?? new List<string>();
    }

    public PlayerServiceRecordSaveData ToSaveData() {
        return new PlayerServiceRecordSaveData {
            recordId = recordId,
            serviceId = serviceId,
            serviceName = serviceName,
            category = category,
            providerId = providerId,
            providerName = providerName,
            day = day,
            absoluteHour = absoluteHour,
            blocked = blocked,
            failureMessage = failureMessage,
            moneyPaid = moneyPaid,
            moneyRewarded = moneyRewarded,
            affectedPokemonCount = affectedPokemonCount,
            appliedChainCount = appliedChainCount,
            failedChainCount = failedChainCount,
            messages = messages != null ? new List<string>(messages) : new List<string>()
        };
    }
}

[Serializable]
public class PlayerServiceLogSaveData {
    public List<PlayerServiceRecordSaveData> records = new List<PlayerServiceRecordSaveData>();
}

[Serializable]
public class PlayerServiceRecordSaveData {
    public string recordId;
    public string serviceId;
    public string serviceName;
    public PlayerServiceCategory category;
    public string providerId;
    public string providerName;
    public int day;
    public int absoluteHour;
    public bool blocked;
    public string failureMessage;
    public float moneyPaid;
    public float moneyRewarded;
    public int affectedPokemonCount;
    public int appliedChainCount;
    public int failedChainCount;
    public List<string> messages = new List<string>();
}
