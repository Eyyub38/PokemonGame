using UnityEngine;

public enum CompetitionRegistrationWindowRequirementMode {
    WindowOpen,
    WindowClosed,
    RegistrationHasOpenWindow,
    RegistrationCanRegister,
    OpenWindowCountAtLeast
}

[CreateAssetMenu(menuName = "Activities/Requirements/Competition Registration Window Requirement")]
public class CompetitionRegistrationWindowRequirement : ActivityRequirement {
    [Tooltip("Which registration window condition this requirement checks.")]
    [SerializeField] CompetitionRegistrationWindowRequirementMode mode = CompetitionRegistrationWindowRequirementMode.WindowOpen;
    [Tooltip("Window checked by Window Open and Window Closed modes.")]
    [SerializeField] CompetitionRegistrationWindowDefinition window;
    [Tooltip("Registration checked by registration-wide window modes.")]
    [SerializeField] CompetitionRegistrationDefinition registration;
    [Tooltip("Minimum open windows required by Open Window Count At Least mode.")]
    [Min(1)]
    [SerializeField] int requiredOpenWindowCount = 1;
    [Tooltip("If enabled, the selected condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        bool result = mode switch {
            CompetitionRegistrationWindowRequirementMode.WindowClosed => window != null && !window.IsOpen(player, registration, out _),
            CompetitionRegistrationWindowRequirementMode.RegistrationHasOpenWindow => registration != null && registration.GetOpenWindows(player).Count > 0,
            CompetitionRegistrationWindowRequirementMode.RegistrationCanRegister => registration != null && registration.CanRegister(player, out _),
            CompetitionRegistrationWindowRequirementMode.OpenWindowCountAtLeast => registration != null && registration.GetOpenWindows(player).Count >= Mathf.Max(1, requiredOpenWindowCount),
            _ => window != null && window.IsOpen(player, registration, out _)
        };

        return mustBeMet ? result : !result;
    }
}
