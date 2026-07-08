using UnityEngine;

public enum SurvivalNeedState {
    Critical,
    Low,
    Normal,
    High
}

[System.Serializable]
public class SurvivalNeed {
    [Tooltip("Need definition that controls limits and hourly rules.")]
    [SerializeField] SurvivalNeedDefinition definition;
    [Tooltip("Current runtime/save value for this need.")]
    [SerializeField] int currentValue;

    public SurvivalNeedDefinition Definition => definition;
    public string Id => definition != null ? definition.Id : "";
    public string DisplayName => definition != null ? definition.DisplayName : "Need";
    public int MaxValue => definition != null ? definition.MaxValue : 100;
    public int CurrentValue => currentValue;
    public float Normalized => MaxValue <= 0 ? 0f : currentValue / (float)MaxValue;

    public SurvivalNeedState State {
        get {
            if(definition == null) return SurvivalNeedState.Normal;
            if(currentValue <= definition.CriticalThreshold) return SurvivalNeedState.Critical;
            if(currentValue <= definition.LowThreshold) return SurvivalNeedState.Low;
            if(currentValue >= Mathf.RoundToInt(MaxValue * 0.85f)) return SurvivalNeedState.High;
            return SurvivalNeedState.Normal;
        }
    }

    public SurvivalNeed(SurvivalNeedDefinition definition) {
        this.definition = definition;
        currentValue = definition != null ? definition.MaxValue : 100;
    }

    public void Change(int amount) {
        currentValue = Mathf.Clamp(currentValue + amount, 0, MaxValue);
    }

    public void Set(int value) {
        currentValue = Mathf.Clamp(value, 0, MaxValue);
    }
}
