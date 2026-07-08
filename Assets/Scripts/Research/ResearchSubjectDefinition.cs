using UnityEngine;

[CreateAssetMenu(menuName = "Research/Subject Definition")]
public class ResearchSubjectDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this research subject. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this research subject.")]
    [TextArea][SerializeField] string description;
    [Header("Activity")]
    [Tooltip("Activity definition that gates costs, requirements, XP and rewards for studying this subject.")]
    [SerializeField] ActivityDefinition activity;
    [Header("Progress")]
    [Tooltip("Research points required to complete this subject.")]
    [Min(1)]
    [SerializeField] int requiredResearchPoints = 10;
    [Tooltip("Research points gained each time this subject is studied before bonuses.")]
    [Min(1)]
    [SerializeField] int pointsPerStudy = 3;
    [Header("Presentation")]
    [Tooltip("Optional icon for research UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Optional Pokemon related to this research subject.")]
    [SerializeField] PokemonBase relatedPokemon;
    [Header("Events")]
    [Tooltip("Optional event published when this subject gains progress but is not complete yet.")]
    [SerializeField] GameEventDefinition studyEvent;
    [Tooltip("Optional event published when this subject becomes completed.")]
    [SerializeField] GameEventDefinition completedEvent;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public ActivityDefinition Activity => activity;
    public int RequiredResearchPoints => Mathf.Max(1, requiredResearchPoints);
    public int PointsPerStudy => Mathf.Max(1, pointsPerStudy);
    public Sprite Icon => icon;
    public PokemonBase RelatedPokemon => relatedPokemon;
    public GameEventDefinition StudyEvent => studyEvent;
    public GameEventDefinition CompletedEvent => completedEvent;
}
