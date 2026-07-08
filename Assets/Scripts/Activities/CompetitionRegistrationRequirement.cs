using UnityEngine;

public enum CompetitionRegistrationRequirementMode {
    RegistrationMade,
    RegistrationCountAtLeast,
    RegistrationNotMade,
    RosterRegistered,
    RosterRegistrationCountAtLeast,
    CanRegister,
    RegisteredWithTag
}

[CreateAssetMenu(menuName = "Activities/Requirements/Competition Registration Requirement")]
public class CompetitionRegistrationRequirement : ActivityRequirement {
    [Tooltip("Which registration log value this requirement checks.")]
    [SerializeField] CompetitionRegistrationRequirementMode mode = CompetitionRegistrationRequirementMode.RegistrationMade;
    [Tooltip("Registration checked by registration-specific modes.")]
    [SerializeField] CompetitionRegistrationDefinition registration;
    [Tooltip("Roster checked by roster-specific modes.")]
    [SerializeField] CompetitionRosterDefinition roster;
    [Tooltip("Tag checked by Registered With Tag mode.")]
    [SerializeField] string tag;
    [Tooltip("Minimum count required by count modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, the selected registration condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerCompetitionRegistrationLog>() : null;
        bool result = mode switch {
            CompetitionRegistrationRequirementMode.RegistrationCountAtLeast => log != null && log.GetRegistrationCount(registration) >= Mathf.Max(0, requiredCount),
            CompetitionRegistrationRequirementMode.RegistrationNotMade => log != null && !log.HasRegistered(registration),
            CompetitionRegistrationRequirementMode.RosterRegistered => log != null && log.HasRegisteredRoster(roster),
            CompetitionRegistrationRequirementMode.RosterRegistrationCountAtLeast => log != null && log.GetRosterRegistrationCount(roster) >= Mathf.Max(0, requiredCount),
            CompetitionRegistrationRequirementMode.CanRegister => registration != null && registration.CanRegister(player, out _),
            CompetitionRegistrationRequirementMode.RegisteredWithTag => HasRegisteredWithTag(log),
            _ => log != null && log.HasRegistered(registration)
        };

        return mustBeMet ? result : !result;
    }

    bool HasRegisteredWithTag(PlayerCompetitionRegistrationLog log) {
        if(log == null || string.IsNullOrWhiteSpace(tag)) {
            return false;
        }

        int required = Mathf.Max(0, requiredCount);
        foreach(var candidate in Resources.LoadAll<CompetitionRegistrationDefinition>("")) {
            if(candidate != null && candidate.HasTag(tag) && log.GetRegistrationCount(candidate) >= required) {
                return true;
            }
        }

        return false;
    }
}
