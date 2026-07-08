using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Skill Definition")]
public class PlayerSkillDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this skill. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this skill.")]
    [TextArea][SerializeField] string description;
    [Header("Progression")]
    [Tooltip("Highest level this skill can reach.")]
    [Min(1)]
    [SerializeField] int maxLevel = 10;
    [Tooltip("Optional labels used by future systems to group/filter skills.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("XP multipliers this skill grants per level for different progression sources.")]
    [SerializeField] List<PlayerSkillExperienceBonus> experienceBonuses = new List<PlayerSkillExperienceBonus>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public int MaxLevel => Mathf.Max(1, maxLevel);
    public IReadOnlyList<string> Tags => tags;

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag) && tags.Contains(tag);
    }

    public float GetExperienceMultiplier(PlayerExperienceSource source, int level) {
        float multiplier = 1f;
        foreach(var bonus in experienceBonuses) {
            if(bonus.source == source) {
                multiplier += Mathf.Max(0, level) * bonus.bonusPerLevel;
            }
        }
        return multiplier;
    }
}

[System.Serializable]
public class PlayerSkillExperienceBonus {
    [Tooltip("Progression source affected by this bonus.")]
    public PlayerExperienceSource source;
    [Tooltip("Additional multiplier gained per skill level. 0.05 means +5% per level.")]
    public float bonusPerLevel = 0.05f;
}

[CreateAssetMenu(menuName = "RPG/Skill Tree Definition")]
public class PlayerSkillTreeDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this skill tree. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this skill tree.")]
    [TextArea][SerializeField] string description;
    [Header("Skills")]
    [Tooltip("Skills that belong to this tree.")]
    [SerializeField] List<PlayerSkillDefinition> skills = new List<PlayerSkillDefinition>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<PlayerSkillDefinition> Skills => skills;
}
