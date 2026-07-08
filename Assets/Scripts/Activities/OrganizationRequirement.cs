using UnityEngine;

public enum OrganizationRequirementMode {
    OrganizationUnlocked,
    ActiveMembership,
    PermanentMembership,
    OrganizationPointsAtLeast,
    OrganizationRankAtLeast,
    OrganizationTagActiveMembership,
    OrganizationCanJoin
}

[CreateAssetMenu(menuName = "Activities/Requirements/Organization Requirement")]
public class OrganizationRequirement : ActivityRequirement {
    [Tooltip("Which organization value this requirement checks.")]
    [SerializeField] OrganizationRequirementMode mode = OrganizationRequirementMode.ActiveMembership;
    [Tooltip("Organization checked by organization-specific modes.")]
    [SerializeField] OrganizationDefinition organization;
    [Tooltip("Tag checked by Organization Tag Active Membership mode.")]
    [SerializeField] string tag;
    [Tooltip("Minimum points or rank index required by numeric modes.")]
    [Min(0)]
    [SerializeField] int requiredValue;
    [Tooltip("If enabled, Organization Can Join is evaluated as if the player has an invitation.")]
    [SerializeField] bool evaluateAsInvitation;
    [Tooltip("If enabled, the selected organization condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerOrganizationLog>() : null;
        bool result = mode switch {
            OrganizationRequirementMode.OrganizationUnlocked => log != null && log.HasUnlockedOrganization(organization),
            OrganizationRequirementMode.PermanentMembership => log != null && log.HasPermanentMembership(organization),
            OrganizationRequirementMode.OrganizationPointsAtLeast => log != null && log.GetPoints(organization) >= Mathf.Max(0, requiredValue),
            OrganizationRequirementMode.OrganizationRankAtLeast => log != null && log.HasReachedRank(organization, requiredValue),
            OrganizationRequirementMode.OrganizationTagActiveMembership => log != null && log.HasActiveOrganizationWithTag(tag),
            OrganizationRequirementMode.OrganizationCanJoin => organization != null && organization.CanJoin(player, evaluateAsInvitation, out _),
            _ => log != null && log.HasActiveMembership(organization)
        };

        return mustBeMet ? result : !result;
    }
}
