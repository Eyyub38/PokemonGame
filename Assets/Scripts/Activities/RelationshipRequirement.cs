using UnityEngine;

[CreateAssetMenu(menuName = "Activities/Requirements/Relationship Requirement")]
public class RelationshipRequirement : ActivityRequirement {
    [Tooltip("Relationship subject to check.")]
    [SerializeField] RelationshipSubjectDefinition subject;
    [Tooltip("Minimum relationship value required.")]
    [SerializeField] int minimumValue;

    public override bool IsMet(PlayerController player) {
        if(player == null || subject == null) {
            return false;
        }

        var relationships = player.GetComponent<PlayerRelationships>();
        return relationships != null && relationships.GetRelationship(subject) >= minimumValue;
    }
}
