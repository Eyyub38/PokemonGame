using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum LifePathCategory {
    General,
    TrainerChampion,
    Ranger,
    CaretakerBreeder,
    FarmerRancher,
    Performer,
    Researcher,
    Explorer,
    MerchantCrafter,
    InvestigatorLaw,
    Custom
}

[CreateAssetMenu(menuName = "Life Paths/Life Path Definition")]
public class LifePathDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this life path. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation of this life path.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad role category used by filters, requirements and future UI.")]
    [SerializeField] LifePathCategory category = LifePathCategory.General;
    [Tooltip("Free-form tags such as battle, ranger, care, research, farm, law, travel or crafting.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Optional icon used by future progression UI.")]
    [SerializeField] Sprite icon = null;

    [Header("Progression")]
    [Tooltip("Life path XP required to earn one perk point. Minimum runtime value is 1.")]
    [Min(1)]
    [SerializeField] int experiencePerPerkPoint = 100;
    [Tooltip("Maximum life path XP stored. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxExperience = 0;
    [Tooltip("Branches belonging to this path, such as Medical Care, Rescue, Mapping or Stage Presence.")]
    [SerializeField] List<LifePathBranchDefinition> branches = new List<LifePathBranchDefinition>();
    [Tooltip("Perks shown under this path. Perks may also reference this path from their own asset.")]
    [SerializeField] List<LifePathPerkDefinition> perks = new List<LifePathPerkDefinition>();

    [Header("Events")]
    [Tooltip("Optional event published when this path gains XP, branch progress, tag progress or unlocks a perk.")]
    [SerializeField] GameEventDefinition changedEvent = null;
    [Tooltip("If enabled, generated life path events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = false;
    [Tooltip("If enabled, generated life path events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog = false;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public LifePathCategory Category => category;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public Sprite Icon => icon;
    public int ExperiencePerPerkPoint => Mathf.Max(1, experiencePerPerkPoint);
    public int MaxExperience => Mathf.Max(0, maxExperience);
    public IReadOnlyList<LifePathBranchDefinition> Branches => branches != null ? (IReadOnlyList<LifePathBranchDefinition>)branches : Array.Empty<LifePathBranchDefinition>();
    public IReadOnlyList<LifePathPerkDefinition> Perks => perks != null ? (IReadOnlyList<LifePathPerkDefinition>)perks : Array.Empty<LifePathPerkDefinition>();

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public int ClampExperience(int value) {
        value = Mathf.Max(0, value);
        return MaxExperience > 0 ? Mathf.Min(MaxExperience, value) : value;
    }

    public int CalculateEarnedPerkPoints(int totalExperience) {
        return Mathf.Max(0, ClampExperience(totalExperience) / ExperiencePerPerkPoint);
    }

    public LifePathBranchDefinition GetBranch(string branchId) {
        if(string.IsNullOrWhiteSpace(branchId)) {
            return null;
        }

        return Branches.FirstOrDefault(branch => branch != null && string.Equals(branch.BranchId, branchId, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasBranch(string branchId) {
        return GetBranch(branchId) != null;
    }

    public bool ContainsPerk(LifePathPerkDefinition perk) {
        return perk != null && (perk.LifePath == this || Perks.Contains(perk));
    }

    public void PublishChanged(PlayerController player, string phase, string message, UnityEngine.Object context, params GameEventValue[] values) {
        GameEventPublishing.PublishOptional(
            changedEvent,
            $"life-path.{phase}.{Id}",
            string.IsNullOrWhiteSpace(message) ? $"{DisplayName} changed." : message,
            GameEventCategory.RPG,
            GameEventImportance.Info,
            context != null ? context : player,
            "LifePathDefinition",
            GameEventScope.Player,
            showEventsInFeed,
            writeEventsToDebugLog,
            values);
    }
}

[Serializable]
public class LifePathBranchDefinition {
    [Tooltip("Stable id for this branch inside the parent life path.")]
    [SerializeField] string branchId = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses Branch Id.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining what this branch tracks.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags used by requirements, filters and future UI.")]
    [SerializeField] List<string> tags = new List<string>();

    public string BranchId => string.IsNullOrWhiteSpace(branchId) ? DisplayName : branchId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? branchId : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }
}

[Serializable]
public class LifePathBranchProgressGrant {
    [Tooltip("Branch id inside the target life path.")]
    public string branchId = string.Empty;
    [Tooltip("Progress added to this branch. Negative values are ignored by PlayerLifePathLog.")]
    [Min(0)]
    public int progress = 1;
}

[Serializable]
public class LifePathTagProgressGrant {
    [Tooltip("Free-form activity or behavior tag to increment, such as grooming, rescue, tracking, trade or stealth.")]
    public string tag = string.Empty;
    [Tooltip("Amount added to this tag counter. Negative values are ignored by PlayerLifePathLog.")]
    [Min(0)]
    public int count = 1;
}

[Serializable]
public class LifePathReward {
    [Tooltip("Life path that receives this reward.")]
    public LifePathDefinition lifePath;
    [Tooltip("Life path XP added by this reward.")]
    [Min(0)]
    public int experience = 0;
    [Tooltip("Branch progress changes applied with this reward.")]
    public List<LifePathBranchProgressGrant> branchProgress = new List<LifePathBranchProgressGrant>();
    [Tooltip("Tag counters incremented with this reward.")]
    public List<LifePathTagProgressGrant> tagProgress = new List<LifePathTagProgressGrant>();
    [Tooltip("Optional perks unlocked directly by this reward, ignoring perk point cost but still recording history.")]
    public List<LifePathPerkDefinition> directPerkUnlocks = new List<LifePathPerkDefinition>();
    [Tooltip("Optional source id saved in life path history. Empty uses the caller fallback.")]
    public string sourceId = string.Empty;
    [Tooltip("Optional source name saved in life path history. Empty uses the caller fallback.")]
    public string sourceName = string.Empty;

    public bool HasAnyPayload {
        get {
            return experience > 0
                || (branchProgress != null && branchProgress.Any(entry => entry != null && entry.progress > 0))
                || (tagProgress != null && tagProgress.Any(entry => entry != null && entry.count > 0))
                || (directPerkUnlocks != null && directPerkUnlocks.Any(perk => perk != null));
        }
    }
}
