using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerTitles : MonoBehaviour, ISavable {
    [Tooltip("Runtime/save list of title, badge and permit states.")]
    [SerializeField] List<PlayerTitleState> titles = new List<PlayerTitleState>();

    public IReadOnlyList<PlayerTitleState> Titles => titles;
    public event Action<TitleDefinition> OnTitleGranted;
    public event Action<string> OnTitleRevoked;
    public event Action<string> OnTitleExpired;

    void OnEnable() {
        if(TimeSystem.i != null) {
            TimeSystem.i.OnTimeChanged += RemoveExpiredTitles;
            TimeSystem.i.OnDayChanged += RemoveExpiredTitles;
        }
    }

    void OnDisable() {
        if(TimeSystem.i != null) {
            TimeSystem.i.OnTimeChanged -= RemoveExpiredTitles;
            TimeSystem.i.OnDayChanged -= RemoveExpiredTitles;
        }
    }

    public bool HasTitle(TitleDefinition title) {
        return title != null && HasTitle(title.Id);
    }

    public bool HasTitle(string titleId) {
        RemoveExpiredTitles();
        return GetState(titleId) != null;
    }

    public bool HasTitleWithTag(string tag) {
        RemoveExpiredTitles();
        return !string.IsNullOrWhiteSpace(tag) && titles.Any(t => t != null && t.MatchesTag(tag));
    }

    public bool HasTitleKind(TitleKind kind) {
        RemoveExpiredTitles();
        return titles.Any(t => t != null && t.kind == kind);
    }

    public bool Grant(TitleGrant grant, UnityEngine.Object context = null) {
        if(grant == null || grant.title == null) {
            return false;
        }

        return Grant(grant.title, grant.ResolveDurationHours(), grant.source, grant.refreshExisting, context);
    }

    public bool Grant(TitleDefinition title, int durationHours = -1, string source = null, bool refreshExisting = true, UnityEngine.Object context = null) {
        if(title == null) {
            return false;
        }

        RemoveExpiredTitles();
        int now = GetCurrentTotalHour();
        bool permanent = durationHours < 0 || !title.CanBeTemporary;
        int expiresAt = permanent ? -1 : now + Mathf.Max(1, durationHours);
        var state = GetState(title.Id);

        if(state != null) {
            if(!refreshExisting) {
                return false;
            }

            state.permanent = state.permanent || permanent;
            state.expiresAtHour = state.permanent ? -1 : Mathf.Max(state.expiresAtHour, expiresAt);
            state.source = string.IsNullOrWhiteSpace(source) ? state.source : source;
        } else {
            titles.Add(new PlayerTitleState(title, now, expiresAt, source, permanent));
        }

        OnTitleGranted?.Invoke(title);
        PublishTitleEvent(title, "granted", title.GrantedEvent, context);
        return true;
    }

    public void ApplyGrants(IEnumerable<TitleGrant> grants, UnityEngine.Object context = null) {
        if(grants == null) {
            return;
        }

        foreach(var grant in grants) {
            Grant(grant, context);
        }
    }

    public bool Revoke(TitleDefinition title, UnityEngine.Object context = null) {
        return title != null && Revoke(title.Id, context);
    }

    public bool Revoke(string titleId, UnityEngine.Object context = null) {
        if(string.IsNullOrWhiteSpace(titleId)) {
            return false;
        }

        var state = GetState(titleId);
        if(state == null) {
            return false;
        }

        titles.Remove(state);
        OnTitleRevoked?.Invoke(titleId);
        PublishTitleEvent(state.ToDefinition(), "revoked", state.definition != null ? state.definition.RevokedEvent : null, context, state);
        return true;
    }

    public PlayerTitleState GetState(string titleId) {
        if(string.IsNullOrWhiteSpace(titleId)) {
            return null;
        }

        return titles.FirstOrDefault(t => t != null && t.titleId == titleId);
    }

    public int GetRemainingHours(TitleDefinition title) {
        if(title == null) {
            return 0;
        }

        var state = GetState(title.Id);
        if(state == null || state.permanent) {
            return state != null ? -1 : 0;
        }

        return Mathf.Max(0, state.expiresAtHour - GetCurrentTotalHour());
    }

    void RemoveExpiredTitles() {
        int now = GetCurrentTotalHour();
        for(int i = titles.Count - 1; i >= 0; i--) {
            var state = titles[i];
            if(state == null || state.permanent || state.expiresAtHour < 0 || state.expiresAtHour > now) {
                continue;
            }

            titles.RemoveAt(i);
            OnTitleExpired?.Invoke(state.titleId);
            PublishTitleEvent(state.ToDefinition(), "expired", state.definition != null ? state.definition.ExpiredEvent : null, this, state);
        }
    }

    int GetCurrentTotalHour() {
        if(TimeSystem.i == null) {
            return 0;
        }

        return Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour);
    }

    void PublishTitleEvent(TitleDefinition title, string phase, GameEventDefinition eventDefinition, UnityEngine.Object context, PlayerTitleState state = null) {
        string titleId = title != null ? title.Id : state?.titleId;
        string titleName = title != null ? title.DisplayName : titleId;
        var titleKind = title != null ? title.Kind : state != null ? state.kind : TitleKind.Title;

        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"title.{phase}.{titleId}",
            $"{titleName} {phase}.",
            GameEventCategory.RPG,
            phase == "granted" ? GameEventImportance.Success : GameEventImportance.Info,
            context != null ? context : this,
            "PlayerTitles",
            GameEventScope.Player,
            showInFeed: title == null || title.ShowEventsInFeed,
            writeToDebugLog: title != null && title.WriteEventsToDebugLog,
            GameEventPublishing.Value("titleId", titleId),
            GameEventPublishing.Value("titleName", titleName),
            GameEventPublishing.Value("titleKind", titleKind),
            GameEventPublishing.Value("phase", phase));
    }

    public object CaptureState() {
        RemoveExpiredTitles();
        return titles.Select(t => t.ToSaveData()).ToList();
    }

    public void RestoreState(object state) {
        var saveData = state as List<PlayerTitleSaveData>;
        titles = saveData?.Select(s => new PlayerTitleState(s)).ToList() ?? new List<PlayerTitleState>();
        RemoveExpiredTitles();
    }
}

[Serializable]
public class PlayerTitleState {
    [Tooltip("Saved title id.")]
    public string titleId;
    [Tooltip("Saved title display name for fallback/debug output.")]
    public string displayName;
    [Tooltip("Saved title kind for fallback/debug output.")]
    public TitleKind kind;
    [Tooltip("In-game total hour when this title was acquired.")]
    public int acquiredAtHour;
    [Tooltip("In-game total hour when this title expires. -1 means permanent.")]
    public int expiresAtHour = -1;
    [Tooltip("If enabled, this title never expires.")]
    public bool permanent = true;
    [Tooltip("Short source/reason for this title grant.")]
    public string source;
    [Tooltip("Runtime definition reference. Not required for save restore, but useful while active.")]
    public TitleDefinition definition;

    public PlayerTitleState() {
    }

    public PlayerTitleState(TitleDefinition title, int acquiredAtHour, int expiresAtHour, string source, bool permanent) {
        definition = title;
        titleId = title.Id;
        displayName = title.DisplayName;
        kind = title.Kind;
        this.acquiredAtHour = acquiredAtHour;
        this.expiresAtHour = expiresAtHour;
        this.permanent = permanent;
        this.source = source;
    }

    public PlayerTitleState(PlayerTitleSaveData saveData) {
        if(saveData == null) {
            return;
        }

        titleId = saveData.titleId;
        displayName = saveData.displayName;
        kind = saveData.kind;
        acquiredAtHour = saveData.acquiredAtHour;
        expiresAtHour = saveData.expiresAtHour;
        permanent = saveData.permanent;
        source = saveData.source;
        definition = ResolveDefinition(titleId);
    }

    public bool MatchesTag(string tag) {
        return definition != null && definition.HasTag(tag);
    }

    public TitleDefinition ToDefinition() {
        if(definition == null) {
            definition = ResolveDefinition(titleId);
        }
        return definition;
    }

    public PlayerTitleSaveData ToSaveData() {
        return new PlayerTitleSaveData {
            titleId = titleId,
            displayName = displayName,
            kind = kind,
            acquiredAtHour = acquiredAtHour,
            expiresAtHour = expiresAtHour,
            permanent = permanent,
            source = source
        };
    }

    static TitleDefinition ResolveDefinition(string titleId) {
        if(string.IsNullOrWhiteSpace(titleId)) {
            return null;
        }

        return Resources.LoadAll<TitleDefinition>("").FirstOrDefault(t => t != null && t.Id == titleId);
    }
}

[Serializable]
public class PlayerTitleSaveData {
    public string titleId;
    public string displayName;
    public TitleKind kind;
    public int acquiredAtHour;
    public int expiresAtHour;
    public bool permanent;
    public string source;
}
