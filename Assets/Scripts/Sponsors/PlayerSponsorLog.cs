using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerSponsorLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save sponsor agreements owned by this player.")]
    [SerializeField] List<PlayerSponsorRecord> sponsors = new List<PlayerSponsorRecord>();
    [Tooltip("Runtime/save history of sponsor benefits used by shops, competitions or assignments.")]
    [SerializeField] List<PlayerSponsorBenefitRecord> benefitHistory = new List<PlayerSponsorBenefitRecord>();

    public IReadOnlyList<PlayerSponsorRecord> Sponsors => sponsors;
    public IReadOnlyList<PlayerSponsorBenefitRecord> BenefitHistory => benefitHistory;
    public event Action<SponsorDefinition, PlayerSponsorRecord> OnSponsorGranted;
    public event Action<SponsorDefinition, PlayerSponsorBenefitRecord> OnSponsorBenefitUsed;
    public event Action OnSponsorLogChanged;

    public bool CanGrant(SponsorDefinition sponsor, out string failureMessage) {
        if(sponsor == null) {
            failureMessage = "A sponsor definition is required.";
            return false;
        }

        var record = GetRecord(sponsor);
        if(sponsor.GrantMode == SponsorGrantMode.OnceEver && record != null) {
            failureMessage = $"{sponsor.DisplayName} was already granted.";
            return false;
        }

        if(sponsor.GrantMode == SponsorGrantMode.RefreshExistingOnly && record == null) {
            failureMessage = $"{sponsor.DisplayName} cannot be refreshed because it is not owned.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public PlayerSponsorRecord RecordGrant(SponsorDefinition sponsor, string sourceId = null) {
        if(sponsor == null) {
            return null;
        }

        var record = GetRecord(sponsor);
        if(record == null) {
            record = new PlayerSponsorRecord {
                sponsorId = sponsor.Id,
                sponsorName = sponsor.DisplayName,
                kind = sponsor.Kind.ToString(),
                sourceId = sourceId
            };
            sponsors.Add(record);
        }

        record.grantCount++;
        record.sponsorName = sponsor.DisplayName;
        record.kind = sponsor.Kind.ToString();
        record.lastGrantedTotalHour = GetCurrentTotalHour();
        record.sourceId = sourceId;
        record.sponsorPoints += sponsor.SponsorPointsOnGrant;

        if(sponsor.Expires && (record.expiresTotalHour < 0 || sponsor.RefreshExpirationOnGrant)) {
            record.expiresTotalHour = GetCurrentTotalHour() + sponsor.DefaultDurationHours;
        } else if(!sponsor.Expires) {
            record.expiresTotalHour = -1;
        }

        OnSponsorGranted?.Invoke(sponsor, record);
        OnSponsorLogChanged?.Invoke();
        return record;
    }

    public bool HasSponsor(SponsorDefinition sponsor) {
        return GetRecord(sponsor) != null;
    }

    public bool HasActiveSponsor(SponsorDefinition sponsor, out string failureMessage) {
        var record = GetRecord(sponsor);
        if(record == null) {
            failureMessage = $"{sponsor?.DisplayName ?? "Sponsor"} is not owned.";
            return false;
        }

        return record.IsActive(GetCurrentTotalHour(), out failureMessage);
    }

    public bool HasActiveSponsorWithTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return false;
        }

        foreach(var sponsor in ResolveActiveSponsors()) {
            if(sponsor != null && sponsor.HasTag(tag)) {
                return true;
            }
        }

        return false;
    }

    public int GetSponsorPoints(SponsorDefinition sponsor) {
        var record = GetRecord(sponsor);
        return record != null ? Mathf.Max(0, record.sponsorPoints) : 0;
    }

    public int GetBenefitUseCount(SponsorDefinition sponsor = null, SponsorBenefitType? benefitType = null, string targetId = null) {
        string sponsorId = sponsor != null ? sponsor.Id : null;
        return benefitHistory.Count(record => record != null
            && (string.IsNullOrWhiteSpace(sponsorId) || record.sponsorId == sponsorId)
            && (!benefitType.HasValue || record.benefitType == benefitType.Value.ToString())
            && (string.IsNullOrWhiteSpace(targetId) || record.targetId == targetId));
    }

    public bool TryGetBestBuyPriceSponsor(ShopCatalogDefinition catalog, ShopCatalogEntry entry, out SponsorDefinition sponsor, out float multiplier) {
        return TryGetBestShopSponsor(catalog, entry, SponsorBenefitType.ShopBuyPrice, out sponsor, out multiplier);
    }

    public bool TryGetBestSellPriceSponsor(ShopCatalogDefinition catalog, out SponsorDefinition sponsor, out float multiplier) {
        return TryGetBestShopSponsor(catalog, null, SponsorBenefitType.ShopSellPrice, out sponsor, out multiplier);
    }

    public float GetBestBuyPriceMultiplier(ShopCatalogDefinition catalog, ShopCatalogEntry entry) {
        return TryGetBestBuyPriceSponsor(catalog, entry, out _, out float multiplier) ? multiplier : 1f;
    }

    public float GetBestSellPriceMultiplier(ShopCatalogDefinition catalog) {
        return TryGetBestSellPriceSponsor(catalog, out _, out float multiplier) ? multiplier : 1f;
    }

    public PlayerSponsorBenefitRecord RecordBenefitUse(SponsorDefinition sponsor, SponsorBenefitType benefitType, string targetId, string targetName, float multiplier, string sourceId = null) {
        if(sponsor == null) {
            return null;
        }

        var ownerRecord = GetRecord(sponsor);
        if(ownerRecord != null) {
            ownerRecord.benefitUseCount++;
        }

        var record = new PlayerSponsorBenefitRecord {
            sponsorId = sponsor.Id,
            sponsorName = sponsor.DisplayName,
            kind = sponsor.Kind.ToString(),
            benefitType = benefitType.ToString(),
            targetId = targetId,
            targetName = targetName,
            multiplier = multiplier,
            usedTotalHour = GetCurrentTotalHour(),
            sourceId = sourceId
        };

        benefitHistory.Add(record);
        OnSponsorBenefitUsed?.Invoke(sponsor, record);
        OnSponsorLogChanged?.Invoke();
        return record;
    }

    bool TryGetBestShopSponsor(ShopCatalogDefinition catalog, ShopCatalogEntry entry, SponsorBenefitType benefitType, out SponsorDefinition bestSponsor, out float bestMultiplier) {
        bestSponsor = null;
        bestMultiplier = 1f;

        foreach(var sponsor in ResolveActiveSponsors()) {
            if(sponsor == null || !sponsor.AppliesToShop(catalog, entry)) {
                continue;
            }

            float multiplier = benefitType == SponsorBenefitType.ShopSellPrice
                ? sponsor.SellPriceMultiplier
                : sponsor.BuyPriceMultiplier;

            if(benefitType == SponsorBenefitType.ShopSellPrice) {
                if(multiplier > bestMultiplier) {
                    bestMultiplier = multiplier;
                    bestSponsor = sponsor;
                }
            } else if(multiplier < bestMultiplier) {
                bestMultiplier = multiplier;
                bestSponsor = sponsor;
            }
        }

        if(bestSponsor == null) {
            bestMultiplier = 1f;
            return false;
        }

        bestMultiplier = Mathf.Max(0f, bestMultiplier);
        return true;
    }

    IEnumerable<SponsorDefinition> ResolveActiveSponsors() {
        int currentHour = GetCurrentTotalHour();
        foreach(var record in sponsors) {
            if(record == null || !record.IsActive(currentHour, out _)) {
                continue;
            }

            var sponsor = ResolveSponsor(record.sponsorId);
            if(sponsor != null) {
                yield return sponsor;
            }
        }
    }

    PlayerSponsorRecord GetRecord(SponsorDefinition sponsor) {
        string sponsorId = sponsor != null ? sponsor.Id : string.Empty;
        return string.IsNullOrWhiteSpace(sponsorId)
            ? null
            : sponsors.FirstOrDefault(record => record != null && record.sponsorId == sponsorId);
    }

    SponsorDefinition ResolveSponsor(string sponsorId) {
        if(string.IsNullOrWhiteSpace(sponsorId)) {
            return null;
        }

        return Resources.LoadAll<SponsorDefinition>("").FirstOrDefault(sponsor => sponsor != null && sponsor.Id == sponsorId);
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    public object CaptureState() {
        return new PlayerSponsorLogSaveData {
            sponsors = sponsors.Where(record => record != null).Select(record => record.Clone()).ToList(),
            benefitHistory = benefitHistory.Where(record => record != null).Select(record => record.Clone()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerSponsorLogSaveData;
        sponsors = saveData?.sponsors?.Where(record => record != null).Select(record => record.Clone()).ToList() ?? new List<PlayerSponsorRecord>();
        benefitHistory = saveData?.benefitHistory?.Where(record => record != null).Select(record => record.Clone()).ToList() ?? new List<PlayerSponsorBenefitRecord>();
        OnSponsorLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerSponsorRecord {
    [Tooltip("Saved sponsor id.")]
    public string sponsorId;
    [Tooltip("Saved sponsor display name.")]
    public string sponsorName;
    [Tooltip("Saved sponsor kind.")]
    public string kind;
    [Tooltip("How many times this sponsor has been granted or refreshed.")]
    [Min(0)]
    public int grantCount;
    [Tooltip("Total sponsor points earned with this sponsor.")]
    [Min(0)]
    public int sponsorPoints;
    [Tooltip("How many sponsor benefits have been consumed from this sponsor.")]
    [Min(0)]
    public int benefitUseCount;
    [Tooltip("Last in-game total hour when this sponsor was granted.")]
    public int lastGrantedTotalHour = -1;
    [Tooltip("In-game total hour when this sponsor expires. -1 means no expiration.")]
    public int expiresTotalHour = -1;
    [Tooltip("Short source id that last granted this sponsor.")]
    public string sourceId;

    public bool IsActive(int currentTotalHour, out string failureMessage) {
        if(expiresTotalHour >= 0 && currentTotalHour >= expiresTotalHour) {
            failureMessage = $"{sponsorName} sponsorship has expired.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public PlayerSponsorRecord Clone() {
        return new PlayerSponsorRecord {
            sponsorId = sponsorId,
            sponsorName = sponsorName,
            kind = kind,
            grantCount = grantCount,
            sponsorPoints = sponsorPoints,
            benefitUseCount = benefitUseCount,
            lastGrantedTotalHour = lastGrantedTotalHour,
            expiresTotalHour = expiresTotalHour,
            sourceId = sourceId
        };
    }
}

[Serializable]
public class PlayerSponsorBenefitRecord {
    [Tooltip("Saved sponsor id.")]
    public string sponsorId;
    [Tooltip("Saved sponsor display name.")]
    public string sponsorName;
    [Tooltip("Saved sponsor kind.")]
    public string kind;
    [Tooltip("Benefit type used by this record.")]
    public string benefitType;
    [Tooltip("Target id affected by this benefit, such as a shop id or competition id.")]
    public string targetId;
    [Tooltip("Readable target name affected by this benefit.")]
    public string targetName;
    [Tooltip("Multiplier applied when the benefit was used.")]
    public float multiplier = 1f;
    [Tooltip("In-game total hour when this benefit was used.")]
    public int usedTotalHour = -1;
    [Tooltip("Short source id that used this benefit.")]
    public string sourceId;

    public PlayerSponsorBenefitRecord Clone() {
        return new PlayerSponsorBenefitRecord {
            sponsorId = sponsorId,
            sponsorName = sponsorName,
            kind = kind,
            benefitType = benefitType,
            targetId = targetId,
            targetName = targetName,
            multiplier = multiplier,
            usedTotalHour = usedTotalHour,
            sourceId = sourceId
        };
    }
}

[Serializable]
public class PlayerSponsorLogSaveData {
    [Tooltip("Saved sponsor agreements.")]
    public List<PlayerSponsorRecord> sponsors = new List<PlayerSponsorRecord>();
    [Tooltip("Saved sponsor benefit history.")]
    public List<PlayerSponsorBenefitRecord> benefitHistory = new List<PlayerSponsorBenefitRecord>();
}
