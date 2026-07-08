using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ShopStockLimitPeriod {
    None = 0,
    Daily = 1,
    Total = 2,
    Weekly = 3
}

public class PlayerShopLedger : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save list of shop offer purchases used for limited stock and analytics.")]
    [SerializeField] List<ShopPurchaseState> purchases = new List<ShopPurchaseState>();

    public IReadOnlyList<ShopPurchaseState> Purchases => purchases;
    public event Action OnLedgerChanged;

    public int GetPurchasedCount(string shopId, string offerId, ShopStockLimitPeriod period) {
        if(period == ShopStockLimitPeriod.None || string.IsNullOrWhiteSpace(shopId) || string.IsNullOrWhiteSpace(offerId)) {
            return 0;
        }

        int day = GetCurrentPeriodKey(period);
        var state = purchases.FirstOrDefault(p => p != null
            && p.shopId == shopId
            && p.offerId == offerId
            && p.period == period
            && p.day == day);

        return state != null ? Mathf.Max(0, state.count) : 0;
    }

    public void RecordPurchase(string shopId, string offerId, ShopStockLimitPeriod period, int count) {
        if(period == ShopStockLimitPeriod.None || count <= 0 || string.IsNullOrWhiteSpace(shopId) || string.IsNullOrWhiteSpace(offerId)) {
            return;
        }

        int day = GetCurrentPeriodKey(period);
        var state = purchases.FirstOrDefault(p => p != null
            && p.shopId == shopId
            && p.offerId == offerId
            && p.period == period
            && p.day == day);

        if(state == null) {
            state = new ShopPurchaseState {
                shopId = shopId,
                offerId = offerId,
                period = period,
                day = day
            };
            purchases.Add(state);
        }

        state.count += count;
        OnLedgerChanged?.Invoke();
    }

    public void RecordReturn(string shopId, string offerId, ShopStockLimitPeriod period, int count) {
        if(period == ShopStockLimitPeriod.None || count <= 0 || string.IsNullOrWhiteSpace(shopId) || string.IsNullOrWhiteSpace(offerId)) {
            return;
        }

        int day = GetCurrentPeriodKey(period);
        var state = purchases.FirstOrDefault(p => p != null
            && p.shopId == shopId
            && p.offerId == offerId
            && p.period == period
            && p.day == day);

        if(state == null) {
            return;
        }

        state.count = Mathf.Max(0, state.count - count);
        if(state.count <= 0) {
            purchases.Remove(state);
        }
        OnLedgerChanged?.Invoke();
    }

    public int RestoreStock(string shopId, string offerId, ShopStockLimitPeriod period, int bundleCount = 0) {
        if(period == ShopStockLimitPeriod.None || string.IsNullOrWhiteSpace(shopId) || string.IsNullOrWhiteSpace(offerId)) {
            return 0;
        }

        int day = GetCurrentPeriodKey(period);
        var state = purchases.FirstOrDefault(p => p != null
            && p.shopId == shopId
            && p.offerId == offerId
            && p.period == period
            && p.day == day);

        if(state == null || state.count <= 0) {
            return 0;
        }

        int restored = bundleCount <= 0 ? state.count : Mathf.Min(state.count, Mathf.Max(1, bundleCount));
        state.count = Mathf.Max(0, state.count - restored);
        if(state.count <= 0) {
            purchases.Remove(state);
        }

        OnLedgerChanged?.Invoke();
        return restored;
    }

    public int ClearPurchases(string shopId, ShopStockLimitPeriod period = ShopStockLimitPeriod.None) {
        int removedCount = 0;
        for(int i = purchases.Count - 1; i >= 0; i--) {
            var purchase = purchases[i];
            if(purchase == null) {
                purchases.RemoveAt(i);
                continue;
            }

            if(!string.IsNullOrWhiteSpace(shopId) && purchase.shopId != shopId) {
                continue;
            }

            if(period != ShopStockLimitPeriod.None && purchase.period != period) {
                continue;
            }

            removedCount += Mathf.Max(0, purchase.count);
            purchases.RemoveAt(i);
        }

        if(removedCount > 0) {
            OnLedgerChanged?.Invoke();
        }

        return removedCount;
    }

    public void ClearDailyHistoryBefore(int day) {
        int removed = purchases.RemoveAll(p => p != null && p.period == ShopStockLimitPeriod.Daily && p.day < day);
        if(removed > 0) {
            OnLedgerChanged?.Invoke();
        }
    }

    public void ClearWeeklyHistoryBefore(int weekIndex) {
        int removed = purchases.RemoveAll(p => p != null && p.period == ShopStockLimitPeriod.Weekly && p.day < weekIndex);
        if(removed > 0) {
            OnLedgerChanged?.Invoke();
        }
    }

    int GetCurrentPeriodKey(ShopStockLimitPeriod period) {
        return period switch {
            ShopStockLimitPeriod.Daily => GetCurrentDay(),
            ShopStockLimitPeriod.Weekly => GetCurrentWeekIndex(),
            _ => -1
        };
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day) : 0;
    }

    int GetCurrentWeekIndex() {
        return Mathf.Max(0, (GetCurrentDay() - 1) / 7);
    }

    public object CaptureState() {
        return new PlayerShopLedgerSaveData {
            purchases = purchases.Where(p => p != null).Select(p => new ShopPurchaseSaveData {
                shopId = p.shopId,
                offerId = p.offerId,
                period = p.period,
                day = p.day,
                count = p.count
            }).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerShopLedgerSaveData;
        purchases = saveData?.purchases?.Where(p => p != null).Select(p => new ShopPurchaseState {
            shopId = p.shopId,
            offerId = p.offerId,
            period = p.period,
            day = p.day,
            count = Mathf.Max(0, p.count)
        }).ToList() ?? new List<ShopPurchaseState>();
        OnLedgerChanged?.Invoke();
    }
}

[Serializable]
public class ShopPurchaseState {
    [Tooltip("Shop/catalog id that recorded this purchase.")]
    public string shopId;
    [Tooltip("Offer id purchased from the shop.")]
    public string offerId;
    [Tooltip("Stock period this purchase belongs to.")]
    public ShopStockLimitPeriod period;
    [Tooltip("Period key for limited stock. Daily uses in-game day, Weekly uses week index and Total uses -1.")]
    public int day = -1;
    [Tooltip("Number of bundles purchased in this period.")]
    [Min(0)]
    public int count;
}

[Serializable]
public class PlayerShopLedgerSaveData {
    public List<ShopPurchaseSaveData> purchases;
}

[Serializable]
public class ShopPurchaseSaveData {
    public string shopId;
    public string offerId;
    public ShopStockLimitPeriod period;
    public int day;
    public int count;
}
