using UnityEngine;

[CreateAssetMenu(menuName = "Activities/Requirements/Reputation Requirement")]
public class ReputationRequirement : ActivityRequirement {
    [Tooltip("Faction reputation to check.")]
    [SerializeField] ReputationFactionDefinition faction;
    [Tooltip("Minimum reputation value required.")]
    [SerializeField] int minimumValue;

    public override bool IsMet(PlayerController player) {
        if(player == null || faction == null) {
            return false;
        }

        var reputation = player.GetComponent<PlayerReputation>();
        return reputation != null && reputation.GetReputation(faction) >= minimumValue;
    }
}
