using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerOrganizationLog : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save ids for organizations unlocked for the player.")]
    [SerializeField] List<string> unlockedOrganizationIds = new List<string>();
    [Tooltip("Runtime/save membership and point state for organizations.")]
    [SerializeField] List<PlayerOrganizationState> organizations = new List<PlayerOrganizationState>();

    public IReadOnlyList<string> UnlockedOrganizationIds => unlockedOrganizationIds;
    public IReadOnlyList<PlayerOrganizationState> Organizations => organizations;
    public event Action<OrganizationDefinition> OnOrganizationUnlocked;
    public event Action<OrganizationDefinition> OnOrganizationJoined;
    public event Action<OrganizationDefinition> OnOrganizationExpired;
    public event Action<OrganizationDefinition, int> OnOrganizationPointsChanged;
    public event Action<OrganizationDefinition, OrganizationRankDefinition> OnOrganizationRankReached;
    public event Action OnOrganizationLogChanged;

    void OnEnable() {
        if(TimeSystem.i != null) {
            TimeSystem.i.OnTimeChanged += RemoveExpiredMemberships;
            TimeSystem.i.OnDayChanged += RemoveExpiredMemberships;
        }
    }

    void OnDisable() {
        if(TimeSystem.i != null) {
            TimeSystem.i.OnTimeChanged -= RemoveExpiredMemberships;
            TimeSystem.i.OnDayChanged -= RemoveExpiredMemberships;
        }
    }

    public bool HasUnlockedOrganization(OrganizationDefinition organization) {
        return organization != null && (organization.UnlockedByDefault || HasUnlockedOrganization(organization.Id));
    }

    public bool HasUnlockedOrganization(string organizationId) {
        return !string.IsNullOrWhiteSpace(organizationId) && unlockedOrganizationIds.Contains(organizationId);
    }

    public bool UnlockOrganization(OrganizationDefinition organization, string source = null) {
        if(organization == null || HasUnlockedOrganization(organization.Id)) {
            return false;
        }

        unlockedOrganizationIds.Add(organization.Id);
        OnOrganizationUnlocked?.Invoke(organization);
        OnOrganizationLogChanged?.Invoke();
        organization.PublishUnlocked(GetComponent<PlayerController>(), source);
        return true;
    }

    public bool JoinOrganization(OrganizationDefinition organization, bool viaInvitation, int durationHours, string source, bool refreshExisting, int initialPoints, out string failureMessage) {
        if(organization == null) {
            failureMessage = "No organization assigned.";
            return false;
        }

        RemoveExpiredMemberships();
        var player = GetComponent<PlayerController>();
        if(!organization.CanJoin(player, viaInvitation, out failureMessage)) {
            return false;
        }

        var state = GetOrCreateState(organization);
        int resolvedDuration = durationHours < 0 ? -1 : organization.ResolveDurationHours(false, durationHours);
        bool permanent = resolvedDuration < 0;
        int expiresAt = permanent ? -1 : GetCurrentTotalHour() + Mathf.Max(1, resolvedDuration);

        if(state.active && !refreshExisting) {
            if(initialPoints > 0) {
                AddPoints(organization, initialPoints, source, autoJoinIfAllowed: false);
            }

            failureMessage = null;
            return true;
        }

        bool wasActive = state.active;
        state.active = true;
        state.permanent = state.permanent || permanent;
        state.expiresAtHour = state.permanent ? -1 : Mathf.Max(state.expiresAtHour, expiresAt);
        state.joinedAtHour = state.joinedAtHour < 0 ? GetCurrentTotalHour() : state.joinedAtHour;
        state.lastSource = source;

        organization.ApplyJoinRewards(player, source);
        if(initialPoints + organization.JoinOrganizationPoints > 0) {
            AddPoints(organization, initialPoints + organization.JoinOrganizationPoints, source, autoJoinIfAllowed: false);
        } else {
            ApplyReachedRanks(organization, state, source);
        }

        if(!wasActive) {
            OnOrganizationJoined?.Invoke(organization);
            organization.PublishJoined(player, source);
        }

        OnOrganizationLogChanged?.Invoke();
        failureMessage = null;
        return true;
    }

    public bool GrantMembership(OrganizationMembershipGrant grant, string fallbackSource, out string failureMessage) {
        if(grant == null || grant.organization == null) {
            failureMessage = "No organization membership grant assigned.";
            return false;
        }

        string source = string.IsNullOrWhiteSpace(grant.source) ? fallbackSource : grant.source;
        int duration = grant.grantPermanently ? -1 : grant.durationHours;
        return JoinOrganization(grant.organization, grant.countsAsInvitation, duration, source, grant.refreshExisting, grant.initialPoints, out failureMessage);
    }

    public void ApplyMembershipGrants(IEnumerable<OrganizationMembershipGrant> grants, string fallbackSource = null) {
        if(grants == null) {
            return;
        }

        foreach(var grant in grants) {
            GrantMembership(grant, fallbackSource, out _);
        }
    }

    public void ApplyPointGrants(IEnumerable<OrganizationPointGrant> grants, string fallbackSource = null) {
        if(grants == null) {
            return;
        }

        foreach(var grant in grants) {
            if(grant != null) {
                AddPoints(grant.organization, grant.points, string.IsNullOrWhiteSpace(grant.source) ? fallbackSource : grant.source, grant.autoJoinIfAllowed);
            }
        }
    }

    public bool AddPoints(OrganizationDefinition organization, int points, string source = null, bool autoJoinIfAllowed = true) {
        if(organization == null || points <= 0) {
            return false;
        }

        RemoveExpiredMemberships();
        var state = GetOrCreateState(organization);
        if(!state.active && autoJoinIfAllowed && organization.AutoJoinOnPointGain) {
            JoinOrganization(organization, viaInvitation: true, durationHours: organization.PermanentByDefault ? -1 : organization.DefaultDurationHours, source, refreshExisting: true, initialPoints: 0, out _);
        }

        if(!state.active) {
            return false;
        }

        state.points += points;
        state.totalPointsEarned += points;
        state.lastPointGain = points;
        state.lastPointGainHour = GetCurrentTotalHour();
        state.lastSource = source;
        ApplyReachedRanks(organization, state, source);
        OnOrganizationPointsChanged?.Invoke(organization, state.points);
        OnOrganizationLogChanged?.Invoke();
        PublishPointEvent(organization, state, points, source);
        return true;
    }

    public bool ExpireOrganization(OrganizationDefinition organization, string source = null) {
        var state = GetState(organization);
        if(state == null || !state.active) {
            return false;
        }

        state.active = false;
        state.expiresAtHour = GetCurrentTotalHour();
        state.lastSource = source;
        OnOrganizationExpired?.Invoke(organization);
        OnOrganizationLogChanged?.Invoke();
        organization.PublishExpired(GetComponent<PlayerController>(), source);
        return true;
    }

    public bool HasActiveMembership(OrganizationDefinition organization) {
        RemoveExpiredMemberships();
        return GetState(organization)?.active ?? false;
    }

    public bool HasPermanentMembership(OrganizationDefinition organization) {
        RemoveExpiredMemberships();
        var state = GetState(organization);
        return state != null && state.active && state.permanent;
    }

    public bool HasActiveExclusiveMembership(string exclusiveGroup, OrganizationDefinition exceptOrganization = null) {
        if(string.IsNullOrWhiteSpace(exclusiveGroup)) {
            return false;
        }

        RemoveExpiredMemberships();
        foreach(var state in organizations) {
            if(state == null || !state.active || state.organizationId == exceptOrganization?.Id) {
                continue;
            }

            var organization = ResolveOrganization(state.organizationId);
            if(organization != null && organization.ExclusiveGroup == exclusiveGroup) {
                return true;
            }
        }

        return false;
    }

    public bool HasActiveOrganizationWithTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag)) {
            return false;
        }

        RemoveExpiredMemberships();
        foreach(var state in organizations) {
            if(state == null || !state.active) {
                continue;
            }

            var organization = ResolveOrganization(state.organizationId);
            if(organization != null && organization.HasTag(tag)) {
                return true;
            }
        }

        return false;
    }

    public int GetPoints(OrganizationDefinition organization) {
        return GetState(organization)?.points ?? 0;
    }

    public int GetRankIndex(OrganizationDefinition organization) {
        var state = GetState(organization);
        if(state == null) {
            return -1;
        }

        var rank = organization.GetRankForPoints(state.points);
        return organization.GetRankIndex(rank);
    }

    public bool HasReachedRank(OrganizationDefinition organization, int rankIndex) {
        return GetRankIndex(organization) >= Mathf.Max(0, rankIndex);
    }

    PlayerOrganizationState GetOrCreateState(OrganizationDefinition organization) {
        var state = GetState(organization);
        if(state != null) {
            return state;
        }

        state = new PlayerOrganizationState {
            organizationId = organization.Id,
            organizationName = organization.DisplayName,
            category = organization.Category,
            currentRankIndex = -1,
            joinedAtHour = -1,
            expiresAtHour = -1,
            lastPointGainHour = -1
        };
        organizations.Add(state);
        return state;
    }

    PlayerOrganizationState GetState(OrganizationDefinition organization) {
        return organization != null ? organizations.FirstOrDefault(state => state != null && state.organizationId == organization.Id) : null;
    }

    void ApplyReachedRanks(OrganizationDefinition organization, PlayerOrganizationState state, string source) {
        var player = GetComponent<PlayerController>();
        foreach(var rank in organization.GetRanksReached(state.points)) {
            if(rank == null || state.claimedRankIds.Contains(rank.Id)) {
                continue;
            }

            state.claimedRankIds.Add(rank.Id);
            state.currentRankIndex = Mathf.Max(state.currentRankIndex, organization.GetRankIndex(rank));
            state.currentRankId = rank.Id;
            state.currentRankName = rank.DisplayName;
            organization.ApplyRankRewards(player, rank, source);
            organization.PublishRankUp(player, rank, state.points, source);
            OnOrganizationRankReached?.Invoke(organization, rank);
        }

        var currentRank = organization.GetRankForPoints(state.points);
        if(currentRank != null) {
            state.currentRankIndex = organization.GetRankIndex(currentRank);
            state.currentRankId = currentRank.Id;
            state.currentRankName = currentRank.DisplayName;
        }
    }

    void RemoveExpiredMemberships() {
        int now = GetCurrentTotalHour();
        foreach(var state in organizations) {
            if(state == null || !state.active || state.permanent || state.expiresAtHour < 0 || state.expiresAtHour > now) {
                continue;
            }

            state.active = false;
            var organization = ResolveOrganization(state.organizationId);
            OnOrganizationExpired?.Invoke(organization);
            organization?.PublishExpired(GetComponent<PlayerController>(), state.lastSource);
        }
    }

    OrganizationDefinition ResolveOrganization(string organizationId) {
        if(string.IsNullOrWhiteSpace(organizationId)) {
            return null;
        }

        return Resources.LoadAll<OrganizationDefinition>("").FirstOrDefault(organization => organization != null && organization.Id == organizationId);
    }

    int GetCurrentTotalHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    void PublishPointEvent(OrganizationDefinition organization, PlayerOrganizationState state, int points, string source) {
        GameEventPublishing.PublishOptional(
            null,
            $"organization.points.{organization.Id}",
            $"{organization.DisplayName} gained {points} point(s).",
            GameEventCategory.Organization,
            GameEventImportance.Info,
            this,
            "PlayerOrganizationLog",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: false,
            GameEventPublishing.Value("organizationId", organization.Id),
            GameEventPublishing.Value("organizationName", organization.DisplayName),
            GameEventPublishing.Value("pointsAdded", points),
            GameEventPublishing.Value("totalPoints", state.points),
            GameEventPublishing.Value("rank", state.currentRankName),
            GameEventPublishing.Value("source", source));
    }

    public object CaptureState() {
        RemoveExpiredMemberships();
        return new PlayerOrganizationLogSaveData {
            unlockedOrganizationIds = unlockedOrganizationIds.Distinct().ToList(),
            organizations = organizations.Where(state => state != null).Select(state => state.ToSaveData()).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerOrganizationLogSaveData;
        unlockedOrganizationIds = saveData?.unlockedOrganizationIds?.Distinct().ToList() ?? new List<string>();
        organizations = saveData?.organizations?.Where(s => s != null).Select(s => new PlayerOrganizationState(s)).ToList() ?? new List<PlayerOrganizationState>();
        RemoveExpiredMemberships();
        OnOrganizationLogChanged?.Invoke();
    }
}

[Serializable]
public class PlayerOrganizationState {
    [Tooltip("Saved organization id.")]
    public string organizationId;
    [Tooltip("Saved organization display name for fallback/debug output.")]
    public string organizationName;
    [Tooltip("Saved organization category.")]
    public OrganizationCategory category;
    [Tooltip("If enabled, this membership is currently active.")]
    public bool active;
    [Tooltip("If enabled, this membership does not expire.")]
    public bool permanent;
    [Tooltip("Current organization points.")]
    [Min(0)]
    public int points;
    [Tooltip("Total organization points ever earned.")]
    [Min(0)]
    public int totalPointsEarned;
    [Tooltip("Current organization rank index.")]
    public int currentRankIndex = -1;
    [Tooltip("Current organization rank id.")]
    public string currentRankId;
    [Tooltip("Current organization rank display name.")]
    public string currentRankName;
    [Tooltip("Rank ids whose rewards have already been granted.")]
    public List<string> claimedRankIds = new List<string>();
    [Tooltip("In-game total hour this organization was joined.")]
    public int joinedAtHour = -1;
    [Tooltip("In-game total hour this membership expires. -1 means permanent.")]
    public int expiresAtHour = -1;
    [Tooltip("Last organization points gained in one action.")]
    [Min(0)]
    public int lastPointGain;
    [Tooltip("In-game total hour of the last point gain.")]
    public int lastPointGainHour = -1;
    [Tooltip("Short source id that last changed this membership.")]
    public string lastSource;

    public PlayerOrganizationState() {
    }

    public PlayerOrganizationState(PlayerOrganizationStateSaveData saveData) {
        if(saveData == null) {
            return;
        }

        organizationId = saveData.organizationId;
        organizationName = saveData.organizationName;
        category = saveData.category;
        active = saveData.active;
        permanent = saveData.permanent;
        points = Mathf.Max(0, saveData.points);
        totalPointsEarned = Mathf.Max(0, saveData.totalPointsEarned);
        currentRankIndex = saveData.currentRankIndex;
        currentRankId = saveData.currentRankId;
        currentRankName = saveData.currentRankName;
        claimedRankIds = saveData.claimedRankIds?.Distinct().ToList() ?? new List<string>();
        joinedAtHour = saveData.joinedAtHour;
        expiresAtHour = saveData.expiresAtHour;
        lastPointGain = Mathf.Max(0, saveData.lastPointGain);
        lastPointGainHour = saveData.lastPointGainHour;
        lastSource = saveData.lastSource;
    }

    public PlayerOrganizationStateSaveData ToSaveData() {
        return new PlayerOrganizationStateSaveData {
            organizationId = organizationId,
            organizationName = organizationName,
            category = category,
            active = active,
            permanent = permanent,
            points = points,
            totalPointsEarned = totalPointsEarned,
            currentRankIndex = currentRankIndex,
            currentRankId = currentRankId,
            currentRankName = currentRankName,
            claimedRankIds = claimedRankIds?.Distinct().ToList() ?? new List<string>(),
            joinedAtHour = joinedAtHour,
            expiresAtHour = expiresAtHour,
            lastPointGain = lastPointGain,
            lastPointGainHour = lastPointGainHour,
            lastSource = lastSource
        };
    }
}

[Serializable]
public class PlayerOrganizationLogSaveData {
    public List<string> unlockedOrganizationIds;
    public List<PlayerOrganizationStateSaveData> organizations;
}

[Serializable]
public class PlayerOrganizationStateSaveData {
    public string organizationId;
    public string organizationName;
    public OrganizationCategory category;
    public bool active;
    public bool permanent;
    public int points;
    public int totalPointsEarned;
    public int currentRankIndex;
    public string currentRankId;
    public string currentRankName;
    public List<string> claimedRankIds;
    public int joinedAtHour;
    public int expiresAtHour;
    public int lastPointGain;
    public int lastPointGainHour;
    public string lastSource;
}
