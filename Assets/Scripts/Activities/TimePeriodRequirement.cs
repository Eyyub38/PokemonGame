using UnityEngine;

[CreateAssetMenu(menuName = "Activities/Requirements/Time Period Requirement")]
public class TimePeriodRequirement : ActivityRequirement {
    [Tooltip("Required day period. None means this requirement is always met.")]
    [SerializeField] DayPeriod requiredPeriod = DayPeriod.None;

    public override bool IsMet(PlayerController player) {
        if(requiredPeriod == DayPeriod.None) {
            return true;
        }

        return TimeSystem.i != null && TimeSystem.i.CurrentPeriod == requiredPeriod;
    }
}
