using UnityEngine;

public enum CompetitionInvitationRequirementMode {
    HasInvitation,
    HasUsableInvitation,
    InvitationMatchesRegistration,
    RegistrationHasUsableInvitation,
    InvitationUseCountAtLeast,
    InvitationAvailableUseCountAtLeast
}

[CreateAssetMenu(menuName = "Activities/Requirements/Competition Invitation Requirement")]
public class CompetitionInvitationRequirement : ActivityRequirement {
    [Tooltip("Which invitation condition this requirement checks.")]
    [SerializeField] CompetitionInvitationRequirementMode mode = CompetitionInvitationRequirementMode.HasUsableInvitation;
    [Tooltip("Invitation checked by invitation-specific modes.")]
    [SerializeField] CompetitionInvitationDefinition invitation;
    [Tooltip("Registration checked by registration-related modes.")]
    [SerializeField] CompetitionRegistrationDefinition registration;
    [Tooltip("Optional registration window used when testing whether an invitation matches a scheduled registration.")]
    [SerializeField] CompetitionRegistrationWindowDefinition registrationWindow;
    [Tooltip("Minimum count required by count modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, the selected invitation condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerCompetitionInvitationLog>() : null;
        bool result = mode switch {
            CompetitionInvitationRequirementMode.HasInvitation => log != null && log.HasInvitation(invitation),
            CompetitionInvitationRequirementMode.InvitationMatchesRegistration => invitation != null && invitation.MatchesRegistration(registration, registrationWindow),
            CompetitionInvitationRequirementMode.RegistrationHasUsableInvitation => log != null && log.FindAnyUsableInvitation(registration, registrationWindow, out _) != null,
            CompetitionInvitationRequirementMode.InvitationUseCountAtLeast => log != null && CountInvitationUses(log) >= Mathf.Max(0, requiredCount),
            CompetitionInvitationRequirementMode.InvitationAvailableUseCountAtLeast => log != null && log.GetAvailableUseCount(invitation) >= Mathf.Max(0, requiredCount),
            _ => log != null && log.HasUsableInvitation(invitation, out _)
        };

        return mustBeMet ? result : !result;
    }

    int CountInvitationUses(PlayerCompetitionInvitationLog log) {
        if(log == null || invitation == null) {
            return 0;
        }

        int count = 0;
        foreach(var record in log.UseHistory) {
            if(record != null && record.invitationId == invitation.Id) {
                count++;
            }
        }

        return count;
    }
}
