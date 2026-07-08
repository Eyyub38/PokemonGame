using UnityEngine;

public class PersonalityProfile : MonoBehaviour {
    [Tooltip("Current personality id used by dialog, companion and behavior systems.")]
    [SerializeField] PersonalityID personalityId = PersonalityID.Balanced;
    [Tooltip("If enabled, Balanced is replaced with a random personality during Awake.")]
    [SerializeField] bool randomizeOnAwake;

    public PersonalityID PersonalityID => personalityId;
    public Personality Personality => PersonalityDB.Personalities[personalityId];

    void Awake() {
        if(randomizeOnAwake && personalityId == PersonalityID.Balanced) {
            personalityId = PersonalityDB.GetRandomPersonalityID();
        }
    }

    public int GetTrait(PersonalityTrait trait) {
        return Personality.GetTrait(trait);
    }

    public void SetPersonality(PersonalityID personality) {
        personalityId = personality;
    }
}
