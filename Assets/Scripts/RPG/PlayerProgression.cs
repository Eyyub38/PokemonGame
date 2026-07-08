using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PlayerExperienceSource {
    Battle,
    Quest,
    Exploration,
    Farming,
    Gathering,
    Research,
    Companion,
    Survival,
    Contest,
    Career
}

public class PlayerProgression : MonoBehaviour, ISavable {
    [Header("Definitions")]
    [Tooltip("Skill tree used to initialize and organize player skills.")]
    [SerializeField] PlayerSkillTreeDefinition skillTree;

    [Header("Level")]
    [Tooltip("Current trainer level.")]
    [Min(1)]
    [SerializeField] int level = 1;
    [Tooltip("Current accumulated trainer experience.")]
    [Min(0)]
    [SerializeField] int experience;
    [Tooltip("Unspent skill points available to upgrade skills.")]
    [Min(0)]
    [SerializeField] int skillPoints;

    [Header("Skill Progress")]
    [Tooltip("Runtime/save list of player skill levels. Usually initialized from the skill tree.")]
    [SerializeField] List<PlayerSkillLevel> skills = new List<PlayerSkillLevel>();

    public int Level => level;
    public int Experience => experience;
    public int SkillPoints => skillPoints;
    public PlayerSkillTreeDefinition SkillTree => skillTree;
    public IReadOnlyList<PlayerSkillLevel> Skills => skills;
    public event Action OnProgressionChanged;
    public event Action<int> OnLevelUp;

    void Awake() {
        EnsureSkillsFromTree();
    }

    public int ExperienceForNextLevel => GetExperienceForLevel(level + 1);

    public int GetSkillLevel(PlayerSkillDefinition definition) {
        if(definition == null) {
            return 0;
        }

        return GetSkillLevel(definition.Id);
    }

    public int GetSkillLevel(string skillId) {
        if(string.IsNullOrWhiteSpace(skillId)) {
            return 0;
        }

        return skills.FirstOrDefault(s => s.SkillId == skillId)?.level ?? 0;
    }

    public int GetHighestSkillLevelWithTag(string tag) {
        if(skillTree == null || string.IsNullOrWhiteSpace(tag)) {
            return 0;
        }

        int bestLevel = 0;
        foreach(var definition in skillTree.Skills) {
            if(definition != null && definition.HasTag(tag)) {
                bestLevel = Mathf.Max(bestLevel, GetSkillLevel(definition));
            }
        }
        return bestLevel;
    }

    public float GetNormalizedExperience() {
        int currentLevelExp = GetExperienceForLevel(level);
        int nextLevelExp = ExperienceForNextLevel;
        return Mathf.Clamp01((experience - currentLevelExp) / (float)(nextLevelExp - currentLevelExp));
    }

    public void AddExperience(int amount, PlayerExperienceSource source = PlayerExperienceSource.Exploration) {
        if(amount <= 0) {
            return;
        }

        experience += Mathf.RoundToInt(amount * GetSourceMultiplier(source));
        bool leveledUp = false;

        while(experience >= ExperienceForNextLevel) {
            level++;
            skillPoints++;
            leveledUp = true;
            OnLevelUp?.Invoke(level);
        }

        if(leveledUp && DialogManager.i != null && !DialogManager.i.IsShowing) {
            StartCoroutine(DialogManager.i.ShowDialogText($"Your trainer level increased to {level}!", waitForInput: true));
        }

        OnProgressionChanged?.Invoke();
    }

    public bool SpendSkillPoint(PlayerSkillDefinition definition) {
        if(definition == null || skillPoints <= 0) {
            return false;
        }

        EnsureSkillsFromTree();
        var skill = GetOrCreateSkill(definition.Id);
        if(skill.level >= definition.MaxLevel) {
            return false;
        }

        skill.level++;
        skillPoints--;
        OnProgressionChanged?.Invoke();
        return true;
    }

    float GetSourceMultiplier(PlayerExperienceSource source) {
        if(skillTree == null) {
            return 1f;
        }

        float multiplier = 1f;
        foreach(var definition in skillTree.Skills) {
            if(definition == null) {
                continue;
            }

            multiplier += definition.GetExperienceMultiplier(source, GetSkillLevel(definition)) - 1f;
        }

        return Mathf.Max(0.1f, multiplier);
    }

    int GetExperienceForLevel(int targetLevel) {
        targetLevel = Mathf.Max(1, targetLevel);
        return Mathf.FloorToInt(50f * targetLevel * targetLevel * 0.8f);
    }

    void EnsureSkillsFromTree() {
        if(skillTree == null) {
            return;
        }

        foreach(var definition in skillTree.Skills) {
            if(definition != null) {
                GetOrCreateSkill(definition.Id);
            }
        }
    }

    PlayerSkillLevel GetOrCreateSkill(string skillId) {
        var skill = skills.FirstOrDefault(s => s.SkillId == skillId);
        if(skill != null) {
            return skill;
        }

        skill = new PlayerSkillLevel() { SkillId = skillId, level = 0 };
        skills.Add(skill);
        return skill;
    }

    public object CaptureState() {
        return new PlayerProgressionSaveData() {
            level = level,
            experience = experience,
            skillPoints = skillPoints,
            skills = skills.Select(s => new PlayerSkillSaveData() {
                skillId = s.SkillId,
                level = s.level
            }).ToList()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as PlayerProgressionSaveData;
        if(saveData == null) {
            return;
        }

        level = Mathf.Max(1, saveData.level);
        experience = Mathf.Max(0, saveData.experience);
        skillPoints = Mathf.Max(0, saveData.skillPoints);
        skills = saveData.skills?.Select(s => new PlayerSkillLevel() {
            SkillId = s.skillId,
            level = Mathf.Max(0, s.level)
        }).ToList() ?? new List<PlayerSkillLevel>();

        EnsureSkillsFromTree();
        OnProgressionChanged?.Invoke();
    }
}

[Serializable]
public class PlayerSkillLevel {
    [Tooltip("Saved skill id.")]
    [SerializeField] string skillId;
    [Tooltip("Current level for this skill.")]
    [Min(0)]
    public int level;

    public string SkillId {
        get => skillId;
        set => skillId = value;
    }
}

[Serializable]
public class PlayerProgressionSaveData {
    public int level;
    public int experience;
    public int skillPoints;
    public List<PlayerSkillSaveData> skills;
}

[Serializable]
public class PlayerSkillSaveData {
    public string skillId;
    public int level;
}
