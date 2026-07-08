using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerLoyaltyLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save loyalty memberships owned by this player.")]
    [SerializeField] List<PlayerLoyaltyRecord> memberships = new List<PlayerLoyaltyRecord>();
    [Tooltip("Runtime/save point gain history.")]
    [SerializeField] List<PlayerLoyaltyPointRecord> pointHistory = new List<PlayerLoyaltyPointRecord>();
    [Tooltip("Maximum point history records kept in memory/save data. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxPointHistoryRecords = 240;

    public IReadOnlyList<PlayerLoyaltyRecord> Memberships => memberships;
    public IReadOnlyList<PlayerLoyaltyPointRecord> PointHistory => pointHistory;
    public event Action<LoyaltyProgramDefinition, PlayerLoyaltyRecord> OnMembershipJoined;
    public event Action<LoyaltyProgramDefinition, PlayerLoyaltyPointRecord> OnPointsGained;
    public event Action OnLoyaltyChanged;

    public bool CanJoin(LoyaltyProgramDefinition program, out string failureMessage) {
        if(program == null) {
            failureMessage = "A loyalty program definition is required.";
            return false;
        }

        var record = GetRecord(program);
        if(program.GrantMode == LoyaltyProgramGrantMode.OnceEver && record != null) {
            failureMessage = $"{program.DisplayName} was already joined.";
            return false;
        }

        if(program.GrantMode == LoyaltyProgramGrantMode.RefreshExistingOnly && record == null) {
            failureMessage = $"{program.DisplayName} cannot be refreshed because it is not owned.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public PlayerLoyaltyRecord RecordJoin(LoyaltyProgramDefinition program, string sourceId = null, float moneyPaid = 0f) {
        if(program == null) {
            return null;
        }

        var record = GetRecord(program);
        bool isNew = record == null;
        if(record == null) {
            record = new PlayerLoyaltyRecord {
                programId = program.Id,
                programName = program.DisplayName,
                kind = program.Kind,
                sourceId = NormalizeSourceId(sourceId)
            };
            memberships.Add(record);
        }

        record.programName = program.DisplayName;
        record.kind = program.Kind;
        record.joinCount++;
        record.lastJoinedTotalHour = GetCurrentTotalHour();
        record.sourceId = NormalizeSourceId(sourceId);
        record.moneyPaidToJoin += Mathf.Max(0f, moneyPaid);

        int pointsToAdd = isNew ? program.StartingPoints : program.RefreshPoints;
        if(pointsToAdd > 0) {
            record.points += pointsToAdd;
            record.lifetimePoints += pointsToAdd;
        }

        if(program.Expires && (record.expiresTotalHour < 0 || program.RefreshExpirationOnGrant)) {
            record.expiresTotalHour = GetCurrentTotalHour() + program.DefaultDurationHours;
        } else if(!program.Expires) {
            record.expiresTotalHour = -1;
        }

        program.ApplyTierRewards(GetComponent<PlayerController>(), record, this);
        OnMembershipJoined?.Invoke(program, record);
        OnLoyaltyChanged?.Invoke();
        return record;
    }

    public bool HasProgram(LoyaltyProgramDefinition program) {
        return GetRecord(program) != null;
    }

    public bool HasActiveProgram(LoyaltyProgramDefinition program, out string failureMessage) {
        var record = GetRecord(program);
        if(record == null) {
            failureMessage = $"{program?.DisplayName ?? "Loyalty program"} is not owned.";
            return false;
        }

        return record.IsActive(GetCurrentTotalHour(), out failureMessage);
    }

    public bool HasActiveProgramWithTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return false;
        }

        return ResolveActivePrograms().Any(program => program != null && program.HasTag(tag));
    }

    public int GetPoints(LoyaltyProgramDefinition program) {
        var record = GetRecord(program);
        return record != null ? Mathf.Max(0, record.points) : 0;
    }

    public string GetCurrentTierId(LoyaltyProgramDefinition program) {
        var record = GetRecord(program);
        return record != null ? record.CurrentTierId : string.Empty;
    }

    public bool HasUnlockedTier(LoyaltyProgramDefinition program, string tierId) {
        var record = GetRecord(program);
        return record != null && record.HasUnlockedTier(tierId);
    }

    public int GetPointGainCount(LoyaltyProgramDefinition program = null, LoyaltyPointSourceKind? sourceKind = null, string sourceId = null) {
        string programId = program != null ? program.Id : null;
        string normalizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? null : NormalizeSourceId(sourceId);
        return pointHistory.Count(record => record != null
            && (string.IsNullOrWhiteSpace(programId) || record.programId == programId)
            && (!sourceKind.HasValue || record.sourceKind == sourceKind.Value)
            && (string.IsNullOrWhiteSpace(normalizedSourceId) || record.sourceId == normalizedSourceId));
    }

    public PlayerLoyaltyPointRecord AddPoints(LoyaltyProgramDefinition program, int points, LoyaltyPointSourceKind sourceKind, string sourceId, string targetId = null, string targetName = null, float moneyValue = 0f) {
        if(program == null || points <= 0) {
            return null;
        }

        var membership = GetRecord(program);
        if(membership == null) {
            if(!program.AutoJoinOnFirstPointGain) {
                return null;
            }

            var player = GetComponent<PlayerController>();
            if(program.JoinCost > 0f || !program.CanJoin(player, this, out _)) {
                return null;
            }

            membership = RecordJoin(program, sourceId);
        }

        if(membership == null || !membership.IsActive(GetCurrentTotalHour(), out _)) {
            return null;
        }

        membership.points += Mathf.Max(0, points);
        membership.lifetimePoints += Mathf.Max(0, points);
        membership.lastPointGainTotalHour = GetCurrentTotalHour();
        program.ApplyTierRewards(GetComponent<PlayerController>(), membership, this);

        var record = new PlayerLoyaltyPointRecord {
            programId = program.Id,
            programName = program.DisplayName,
            sourceKind = sourceKind,
            sourceId = NormalizeSourceId(sourceId),
            targetId = targetId,
            targetName = targetName,
            points = Mathf.Max(0, points),
            moneyValue = Mathf.Max(0f, moneyValue),
            totalPointsAfter = membership.points,
            tierIdAfter = membership.CurrentTierId,
            gainedTotalHour = GetCurrentTotalHour()
        };

        pointHistory.Add(record);
        TrimPointHistory();
        program.PublishPointsGained(GetComponent<PlayerController>(), record.sourceId, record.points, record.tierIdAfter);
        OnPointsGained?.Invoke(program, record);
        OnLoyaltyChanged?.Invoke();
        return record;
    }

    public void RecordShopPurchase(ShopCatalogDefinition catalog, ShopCatalogEntry entry, string shopId, float moneySpent, int bundles) {
        foreach(var program in ResolveEligiblePrograms(catalog, entry, includeAutoJoinPrograms: true)) {
            var membership = GetRecord(program);
            int currentPoints = membership != null ? membership.points : 0;
            int points = program.CalculateShopPurchasePoints(moneySpent, bundles, currentPoints);
            AddPoints(program, points, LoyaltyPointSourceKind.ShopPurchase, $"shop:{shopId}", entry != null ? entry.OfferId : null, entry != null ? entry.DisplayName : null, moneySpent);
        }
    }

    public void RecordShopSell(ShopCatalogDefinition catalog, string shopId, ItemBase item, int count, float moneyGained) {
        foreach(var program in ResolveEligiblePrograms(catalog, null, includeAutoJoinPrograms: true)) {
            var membership = GetRecord(program);
            int currentPoints = membership != null ? membership.points : 0;
            int points = program.CalculateShopSellPoints(moneyGained, currentPoints);
            AddPoints(program, points, LoyaltyPointSourceKind.ShopSell, $"shop:{shopId}", item != null ? item.name : null, item != null ? item.Name : null, moneyGained);
        }
    }

    public bool TryGetBestBuyPriceProgram(ShopCatalogDefinition catalog, ShopCatalogEntry entry, out LoyaltyProgramDefinition program, out float multiplier) {
        program = null;
        multiplier = 1f;

        foreach(var candidate in ResolveActivePrograms()) {
            if(candidate == null || !candidate.AppliesToShop(catalog, entry)) {
                continue;
            }

            var record = GetRecord(candidate);
            float candidateMultiplier = candidate.GetBuyPriceMultiplier(record != null ? record.points : 0);
            if(candidateMultiplier < multiplier) {
                multiplier = candidateMultiplier;
                program = candidate;
            }
        }

        if(program == null) {
            multiplier = 1f;
            return false;
        }

        multiplier = Mathf.Max(0f, multiplier);
        return true;
    }

    public bool TryGetBestSellPriceProgram(ShopCatalogDefinition catalog, out LoyaltyProgramDefinition program, out float multiplier) {
        program = null;
        multiplier = 1f;

        foreach(var candidate in ResolveActivePrograms()) {
            if(candidate == null || !candidate.AppliesToShop(catalog, null)) {
                continue;
            }

            var record = GetRecord(candidate);
            float candidateMultiplier = candidate.GetSellPriceMultiplier(record != null ? record.points : 0);
            if(candidateMultiplier > multiplier) {
                multiplier = candidateMultiplier;
                program = candidate;
            }
        }

        if(program == null) {
            multiplier = 1f;
            return false;
        }

        multiplier = Mathf.Max(0f, multiplier);
        return true;
    }

    public float GetBestBuyPriceMultiplier(ShopCatalogDefinition catalog, ShopCatalogEntry entry) {
        return TryGetBestBuyPriceProgram(catalog, entry, out _, out float multiplier) ? multiplier : 1f;
    }

    public float GetBestSellPriceMultiplier(ShopCatalogDefinition catalog) {
        return TryGetBestSellPriceProgram(catalog, out _, out float multiplier) ? multiplier : 1f;
    }

    IEnumerable<LoyaltyProgramDefinition> ResolveEligiblePrograms(ShopCatalogDefinition catalog, ShopCatalogEntry entry, bool includeAutoJoinPrograms) {
        var active = ResolveActivePrograms().Where(program => program != null && program.AppliesToShop(catalog, entry));
        if(!includeAutoJoinPrograms) {
            return active;
        }

        var activeIds = new HashSet<string>(active.Select(program => program.Id));
        var autoJoin = Resources.LoadAll<LoyaltyProgramDefinition>("")
            .Where(program => program != null
                && program.AutoJoinOnFirstPointGain
                && !activeIds.Contains(program.Id)
                && program.AppliesToShop(catalog, entry));

        return active.Concat(autoJoin);
    }

    IEnumerable<LoyaltyProgramDefinition> ResolveActivePrograms() {
        int currentHour = GetCurrentTotalHour();
        foreach(var record in memberships) {
            if(record == null || !record.IsActive(currentHour, out _)) {
                continue;
            }

            var program = ResolveProgram(record.programId);
            if(program != null) {
                yield return program;
            }
        }
    }

    PlayerLoyaltyRecord GetRecord(LoyaltyProgramDefinition program) {
        string programId = program != null ? program.Id : string.Empty;
        return string.IsNullOrWhiteSpace(programId)
            ? null
            : memberships.FirstOrDefault(record => record != null && record.programId == programId);
    }

    LoyaltyProgramDefinition ResolveProgram(string programId) {
        if(string.IsNullOrWhiteSpace(programId)) {
            return null;
        }

        return Resources.LoadAll<LoyaltyProgramDefinition>("").FirstOrDefault(program => program != null && program.Id == programId);
    }

    void TrimPointHistory() {
        if(maxPointHistoryRecords <= 0 || pointHistory.Count <= maxPointHistoryRecords) {
            return;
        }

        pointHistory = pointHistory
            .Where(record => record != null)
            .OrderByDescending(record => record.gainedTotalHour)
            .Take(maxPointHistoryRecords)
            .OrderBy(record => record.gainedTotalHour)
            .ToList();
    }

    static string NormalizeSourceId(string sourceId) {
        return string.IsNullOrWhiteSpace(sourceId) ? "loyalty" : sourceId;
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        TrimPointHistory();
        return new PlayerLoyaltyLogSaveData {
            memberships = memberships.Where(record => record != null).Select(record => record.Clone()).ToList(),
            pointHistory = pointHistory.Where(record => record != null).Select(record => record.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerLoyaltyLogSaveData;
        memberships = saveData?.memberships?.Where(record => record != null).Select(record => record.Clone()).ToList() ?? new List<PlayerLoyaltyRecord>();
        pointHistory = saveData?.pointHistory?.Where(record => record != null).Select(record => record.Clone()).ToList() ?? new List<PlayerLoyaltyPointRecord>();
        TrimPointHistory();
        OnLoyaltyChanged?.Invoke();
    }
}

[Serializable]
public class PlayerLoyaltyRecord {
    [Tooltip("Saved loyalty program id.")]
    public string programId;
    [Tooltip("Saved loyalty program display name.")]
    public string programName;
    [Tooltip("Saved loyalty program kind.")]
    public LoyaltyProgramKind kind;
    [Tooltip("How many times this membership has been joined or refreshed.")]
    [Min(0)]
    public int joinCount;
    [Tooltip("Current spendable/trackable points for this program.")]
    [Min(0)]
    public int points;
    [Tooltip("Lifetime points earned for analytics and requirements.")]
    [Min(0)]
    public int lifetimePoints;
    [Tooltip("Total money paid to join or refresh this membership.")]
    [Min(0f)]
    public float moneyPaidToJoin;
    [Tooltip("Last in-game total hour when this membership was joined or refreshed.")]
    public int lastJoinedTotalHour = -1;
    [Tooltip("Last in-game total hour when points were gained.")]
    public int lastPointGainTotalHour = -1;
    [Tooltip("In-game total hour when this membership expires. -1 means no expiration.")]
    public int expiresTotalHour = -1;
    [Tooltip("Short source id that last joined or refreshed this membership.")]
    public string sourceId;
    [Tooltip("Currently active tier id based on points.")]
    public string currentTierId;
    [Tooltip("Currently active tier display name based on points.")]
    public string currentTierName;
    [Tooltip("Tier ids whose one-time rewards were already applied.")]
    public List<string> unlockedTierIds = new List<string>();

    public string CurrentTierId => currentTierId;
    public string CurrentTierName => currentTierName;

    public bool IsActive(int currentTotalHour, out string failureMessage) {
        if(expiresTotalHour >= 0 && currentTotalHour >= expiresTotalHour) {
            failureMessage = $"{programName} membership has expired.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool HasUnlockedTier(string tierId) {
        return !string.IsNullOrWhiteSpace(tierId)
            && unlockedTierIds != null
            && unlockedTierIds.Any(entry => string.Equals(entry, tierId, StringComparison.OrdinalIgnoreCase));
    }

    public PlayerLoyaltyRecord Clone() {
        return new PlayerLoyaltyRecord {
            programId = programId,
            programName = programName,
            kind = kind,
            joinCount = joinCount,
            points = points,
            lifetimePoints = lifetimePoints,
            moneyPaidToJoin = moneyPaidToJoin,
            lastJoinedTotalHour = lastJoinedTotalHour,
            lastPointGainTotalHour = lastPointGainTotalHour,
            expiresTotalHour = expiresTotalHour,
            sourceId = sourceId,
            currentTierId = currentTierId,
            currentTierName = currentTierName,
            unlockedTierIds = unlockedTierIds != null ? unlockedTierIds.ToList() : new List<string>()
        };
    }
}

[Serializable]
public class PlayerLoyaltyPointRecord {
    [Tooltip("Saved loyalty program id.")]
    public string programId;
    [Tooltip("Saved loyalty program display name.")]
    public string programName;
    [Tooltip("Source kind that granted points.")]
    public LoyaltyPointSourceKind sourceKind;
    [Tooltip("Short source id that granted points.")]
    public string sourceId;
    [Tooltip("Target id affected by this point gain, such as a shop offer id.")]
    public string targetId;
    [Tooltip("Readable target name affected by this point gain.")]
    public string targetName;
    [Tooltip("Points gained by this record.")]
    [Min(0)]
    public int points;
    [Tooltip("Money value used to calculate this point gain.")]
    [Min(0f)]
    public float moneyValue;
    [Tooltip("Program points after this gain.")]
    [Min(0)]
    public int totalPointsAfter;
    [Tooltip("Current tier id after this gain.")]
    public string tierIdAfter;
    [Tooltip("In-game total hour when this point gain happened.")]
    public int gainedTotalHour;

    public PlayerLoyaltyPointRecord Clone() {
        return new PlayerLoyaltyPointRecord {
            programId = programId,
            programName = programName,
            sourceKind = sourceKind,
            sourceId = sourceId,
            targetId = targetId,
            targetName = targetName,
            points = points,
            moneyValue = moneyValue,
            totalPointsAfter = totalPointsAfter,
            tierIdAfter = tierIdAfter,
            gainedTotalHour = gainedTotalHour
        };
    }
}

[Serializable]
public class PlayerLoyaltyLogSaveData {
    public List<PlayerLoyaltyRecord> memberships = new List<PlayerLoyaltyRecord>();
    public List<PlayerLoyaltyPointRecord> pointHistory = new List<PlayerLoyaltyPointRecord>();
}
