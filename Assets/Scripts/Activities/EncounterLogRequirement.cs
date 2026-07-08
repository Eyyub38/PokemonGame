using UnityEngine;

[CreateAssetMenu(menuName = "Activities/Requirements/Encounter Log Requirement")]
public class EncounterLogRequirement : ActivityRequirement {
    [Tooltip("Pokemon whose encounter history is checked.")]
    [SerializeField] PokemonBase pokemon;
    [Tooltip("Source filter for the encounter count. Any accepts all sources.")]
    [SerializeField] EncounterSourceType sourceType = EncounterSourceType.Any;
    [Tooltip("Which count is checked: seen, battle-started, captured or stealth-captured.")]
    [SerializeField] EncounterLogCountType countType = EncounterLogCountType.Seen;
    [Tooltip("Minimum count required.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, count must be at least Required Count. If disabled, it must be lower.")]
    [SerializeField] bool mustMeetCount = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerEncounterLog>() : null;
        int count = log != null ? log.GetCount(pokemon, sourceType, countType) : 0;
        bool result = count >= Mathf.Max(0, requiredCount);
        return mustMeetCount ? result : !result;
    }
}
