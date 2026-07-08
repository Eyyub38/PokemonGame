using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokemonAbilityPointSource {
    General,
    Battle,
    Training,
    Care,
    Research,
    Assignment,
    Contest,
    Travel,
    Quest,
    Manual
}

public enum PokemonAbilityTreeEffectKind {
    CustomFlag,
    StatModifier,
    GrowthTraining,
    PassiveTrait,
    Technique,
    Friendship,
    CareBonus,
    AssignmentBonus,
    BattleHint
}

public enum PokemonAbilityTreeSourceAction {
    GrantPoints,
    UnlockNode,
    GrantPointsAndUnlockNode
}

public enum PokemonAbilityTreeTarget {
    PartySlot,
    FirstPartyPokemon,
    FirstHealthyPokemon,
    AllPartyPokemon
}

[CreateAssetMenu(menuName = "Pokemon/Ability Tree/Tree Definition")]
public class PokemonAbilityTreeDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id saved into Pokemon ability tree state. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug output or future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining the intended role, such as battle, care, travel, research or species mastery.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as fire, starter, care, ranger, travel, stealth, battle or support.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Eligibility")]
    [Tooltip("Optional Pokemon species/base required to use this tree. Empty allows any species.")]
    [SerializeField] PokemonBase requiredPokemonBase = null;
    [Tooltip("Optional Pokemon type required to use this tree. None ignores type.")]
    [SerializeField] PokemonType requiredType = PokemonType.None;
    [Tooltip("Minimum Pokemon level required before nodes in this tree can be unlocked. 0 ignores level.")]
    [Min(0)]
    [SerializeField] int minimumLevel;
    [Tooltip("Minimum friendship required before nodes in this tree can be unlocked. 0 ignores friendship.")]
    [Range(0, 255)]
    [SerializeField] int minimumFriendship;
    [Tooltip("Reusable player requirements checked before nodes in this tree can be unlocked.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message shown when the tree is blocked.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This ability tree is not available for this Pokemon.";

    [Header("Nodes")]
    [Tooltip("Editable ability nodes in this tree. Connections are defined by prerequisite node ids.")]
    [SerializeField] List<PokemonAbilityTreeNodeDefinition> nodes = new List<PokemonAbilityTreeNodeDefinition>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? tags : Array.Empty<string>();
    public PokemonBase RequiredPokemonBase => requiredPokemonBase;
    public PokemonType RequiredType => requiredType;
    public int MinimumLevel => Mathf.Max(0, minimumLevel);
    public int MinimumFriendship => Mathf.Clamp(minimumFriendship, 0, 255);
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? requirements : Array.Empty<ActivityRequirement>();
    public string LockedMessage => lockedMessage;
    public IReadOnlyList<PokemonAbilityTreeNodeDefinition> Nodes => nodes != null ? nodes : Array.Empty<PokemonAbilityTreeNodeDefinition>();

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public PokemonAbilityTreeNodeDefinition GetNode(string nodeId) {
        return Nodes.FirstOrDefault(node => node != null && string.Equals(node.NodeId, nodeId, StringComparison.OrdinalIgnoreCase));
    }

    public bool CanUseTree(Pokemon pokemon, PlayerController player, out string failureMessage) {
        if(pokemon == null) {
            failureMessage = "No Pokemon selected.";
            return false;
        }

        if(requiredPokemonBase != null && pokemon.OriginalBase != requiredPokemonBase && pokemon.Base != requiredPokemonBase) {
            failureMessage = lockedMessage;
            return false;
        }

        if(requiredType != PokemonType.None && !pokemon.HasType(requiredType)) {
            failureMessage = lockedMessage;
            return false;
        }

        if(minimumLevel > 0 && pokemon.Level < minimumLevel) {
            failureMessage = $"{pokemon.NickName} must reach level {minimumLevel}.";
            return false;
        }

        if(minimumFriendship > 0 && pokemon.Friendship < minimumFriendship) {
            failureMessage = $"{pokemon.NickName} needs more friendship.";
            return false;
        }

        foreach(var requirement in Requirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = string.IsNullOrWhiteSpace(requirement.FailureMessage) ? lockedMessage : requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }
}

[Serializable]
public class PokemonAbilityTreeNodeDefinition {
    [Header("Identity")]
    [Tooltip("Stable node id unique inside its tree.")]
    [SerializeField] string nodeId = string.Empty;
    [Tooltip("Readable node name shown in future UI. Empty uses Node Id.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining what this node unlocks.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as passive, active, battle, care, travel or support.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Unlock")]
    [Tooltip("Ability points required to unlock this node.")]
    [Min(0)]
    [SerializeField] int pointCost = 1;
    [Tooltip("Prerequisite node ids that must already be unlocked.")]
    [SerializeField] List<string> prerequisiteNodeIds = new List<string>();
    [Tooltip("Minimum Pokemon level required for this node. 0 ignores level.")]
    [Min(0)]
    [SerializeField] int minimumLevel;
    [Tooltip("Minimum friendship required for this node. 0 ignores friendship.")]
    [Range(0, 255)]
    [SerializeField] int minimumFriendship;
    [Tooltip("Required passive growth trait ids. Empty ignores traits.")]
    [SerializeField] List<string> requiredGrowthTraitIds = new List<string>();
    [Tooltip("Required known move/technique asset ids. Empty ignores known techniques.")]
    [SerializeField] List<string> requiredKnownTechniqueIds = new List<string>();
    [Tooltip("Reusable player requirements for titles, regions, research, quests, reputation or other gates.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message shown when this node is blocked.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This ability is not available yet.";

    [Header("Effects")]
    [Tooltip("Effects applied once when this node is unlocked. Future systems can also read these as passive flags.")]
    [SerializeField] List<PokemonAbilityTreeEffect> effects = new List<PokemonAbilityTreeEffect>();

    public string NodeId => !string.IsNullOrWhiteSpace(nodeId) ? nodeId : displayName;
    public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName : NodeId;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? tags : Array.Empty<string>();
    public int PointCost => Mathf.Max(0, pointCost);
    public IReadOnlyList<string> PrerequisiteNodeIds => prerequisiteNodeIds != null ? prerequisiteNodeIds : Array.Empty<string>();
    public int MinimumLevel => Mathf.Max(0, minimumLevel);
    public int MinimumFriendship => Mathf.Clamp(minimumFriendship, 0, 255);
    public IReadOnlyList<string> RequiredGrowthTraitIds => requiredGrowthTraitIds != null ? requiredGrowthTraitIds : Array.Empty<string>();
    public IReadOnlyList<string> RequiredKnownTechniqueIds => requiredKnownTechniqueIds != null ? requiredKnownTechniqueIds : Array.Empty<string>();
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? requirements : Array.Empty<ActivityRequirement>();
    public string LockedMessage => lockedMessage;
    public IReadOnlyList<PokemonAbilityTreeEffect> Effects => effects != null ? effects : Array.Empty<PokemonAbilityTreeEffect>();

    public bool CanUnlock(Pokemon pokemon, PlayerController player, PokemonAbilityTreeDefinition tree, out string failureMessage) {
        if(pokemon == null) {
            failureMessage = "No Pokemon selected.";
            return false;
        }

        if(tree == null) {
            failureMessage = "Ability tree is missing.";
            return false;
        }

        if(pokemon.HasUnlockedAbilityNode(tree, NodeId)) {
            failureMessage = $"{pokemon.NickName} already unlocked {DisplayName}.";
            return false;
        }

        if(pokemon.GetAbilityPoints(tree) < PointCost) {
            failureMessage = $"{pokemon.NickName} needs {PointCost} ability point(s).";
            return false;
        }

        foreach(var prerequisiteId in PrerequisiteNodeIds.Where(id => !string.IsNullOrWhiteSpace(id))) {
            if(!pokemon.HasUnlockedAbilityNode(tree, prerequisiteId)) {
                failureMessage = lockedMessage;
                return false;
            }
        }

        if(minimumLevel > 0 && pokemon.Level < minimumLevel) {
            failureMessage = $"{pokemon.NickName} must reach level {minimumLevel}.";
            return false;
        }

        if(minimumFriendship > 0 && pokemon.Friendship < minimumFriendship) {
            failureMessage = $"{pokemon.NickName} needs more friendship.";
            return false;
        }

        foreach(var traitId in RequiredGrowthTraitIds.Where(id => !string.IsNullOrWhiteSpace(id))) {
            if(!pokemon.HasPassiveTrait(traitId)) {
                failureMessage = $"{pokemon.NickName} lacks a required trait.";
                return false;
            }
        }

        foreach(var techniqueId in RequiredKnownTechniqueIds.Where(id => !string.IsNullOrWhiteSpace(id))) {
            if(pokemon.TechniqueMemory == null || !pokemon.TechniqueMemory.HasMoveId(techniqueId)) {
                failureMessage = $"{pokemon.NickName} lacks a required technique.";
                return false;
            }
        }

        foreach(var requirement in Requirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = string.IsNullOrWhiteSpace(requirement.FailureMessage) ? lockedMessage : requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public void ApplyEffects(Pokemon pokemon, PokemonAbilityTreeDefinition tree) {
        foreach(var effect in Effects) {
            effect?.Apply(pokemon, tree, this);
        }
    }
}

[Serializable]
public class PokemonAbilityTreeEffect {
    [Tooltip("Broad effect kind. Custom Flag is passive data only unless another system reads Effect Id/tags.")]
    [SerializeField] PokemonAbilityTreeEffectKind kind = PokemonAbilityTreeEffectKind.CustomFlag;
    [Tooltip("Stable effect id saved into the Pokemon ability state. Empty uses the effect kind.")]
    [SerializeField] string effectId = string.Empty;
    [Tooltip("Readable effect name saved for future UI/debugging.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Tags future systems can query, such as fire, stealth, care, travel or battle.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Stat affected by Stat Modifier or Growth Training effects.")]
    [SerializeField] Stat stat = Stat.Attack;
    [Tooltip("Flat stat modifier value for Stat Modifier effects.")]
    [SerializeField] int flatStatBonus;
    [Tooltip("Multiplier stat modifier for Stat Modifier effects. 0.10 means +10 percent.")]
    [SerializeField] float statMultiplierBonus;
    [Tooltip("Training points granted by Growth Training effects.")]
    [Min(0)]
    [SerializeField] int trainingPoints;
    [Tooltip("Passive trait applied by Passive Trait effects.")]
    [SerializeField] PokemonPassiveTraitDefinition passiveTrait = null;
    [Tooltip("Move/technique remembered by Technique effects.")]
    [SerializeField] MoveBase technique = null;
    [Tooltip("Friendship amount granted by Friendship effects.")]
    [SerializeField] int friendshipAmount;
    [Tooltip("Generic numeric value for custom/battle/care/assignment systems.")]
    [SerializeField] float value;

    public PokemonAbilityTreeEffectKind Kind => kind;
    public string EffectId => !string.IsNullOrWhiteSpace(effectId) ? effectId : kind.ToString();
    public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName : EffectId;
    public IReadOnlyList<string> Tags => tags != null ? tags : Array.Empty<string>();
    public Stat Stat => stat;
    public int FlatStatBonus => flatStatBonus;
    public float StatMultiplierBonus => statMultiplierBonus;
    public int TrainingPoints => Mathf.Max(0, trainingPoints);
    public PokemonPassiveTraitDefinition PassiveTrait => passiveTrait;
    public MoveBase Technique => technique;
    public int FriendshipAmount => friendshipAmount;
    public float Value => value;

    public void Apply(Pokemon pokemon, PokemonAbilityTreeDefinition tree, PokemonAbilityTreeNodeDefinition node) {
        if(pokemon == null) {
            return;
        }

        string sourceId = $"{tree?.Id}:{node?.NodeId}:{EffectId}";
        switch(kind) {
            case PokemonAbilityTreeEffectKind.StatModifier:
                pokemon.AddAbilityTreeStatModifier(this, sourceId);
                break;
            case PokemonAbilityTreeEffectKind.GrowthTraining:
                pokemon.GainGrowthTraining(stat, TrainingPoints, null, PokemonGrowthSource.Training, sourceId, DisplayName);
                break;
            case PokemonAbilityTreeEffectKind.PassiveTrait:
                pokemon.ApplyPassiveTrait(passiveTrait, sourceId);
                break;
            case PokemonAbilityTreeEffectKind.Technique:
                pokemon.RememberTechnique(technique, PokemonTechniqueLearnSource.Training, sourceId, DisplayName);
                break;
            case PokemonAbilityTreeEffectKind.Friendship:
                pokemon.IncreaseFriendship(friendshipAmount);
                break;
        }
    }

    public PokemonAbilityTreeEffectRecord ToRecord(PokemonAbilityTreeDefinition tree, PokemonAbilityTreeNodeDefinition node) {
        return new PokemonAbilityTreeEffectRecord {
            treeId = tree != null ? tree.Id : string.Empty,
            nodeId = node != null ? node.NodeId : string.Empty,
            effectId = EffectId,
            effectName = DisplayName,
            kind = kind,
            tags = Tags.ToList(),
            stat = stat,
            flatStatBonus = flatStatBonus,
            statMultiplierBonus = statMultiplierBonus,
            value = value
        };
    }
}

[Serializable]
public class PokemonAbilityTreeState {
    [Tooltip("Ability points saved per ability tree id.")]
    public List<PokemonAbilityTreePointState> treePoints = new List<PokemonAbilityTreePointState>();
    [Tooltip("Unlocked ability tree nodes.")]
    public List<PokemonAbilityTreeUnlockRecord> unlockedNodes = new List<PokemonAbilityTreeUnlockRecord>();
    [Tooltip("Applied ability effects cached for future UI/debug and passive queries.")]
    public List<PokemonAbilityTreeEffectRecord> appliedEffects = new List<PokemonAbilityTreeEffectRecord>();
    [Tooltip("Ability point gain/spend/unlock history.")]
    public List<PokemonAbilityTreeHistoryRecord> history = new List<PokemonAbilityTreeHistoryRecord>();

    public int GetPoints(string treeId) {
        return treePoints?.FirstOrDefault(state => state != null && string.Equals(state.treeId, treeId, StringComparison.OrdinalIgnoreCase))?.points ?? 0;
    }

    public int AddPoints(string treeId, string treeName, int amount, PokemonAbilityPointSource source, string sourceId, string sourceName) {
        if(string.IsNullOrWhiteSpace(treeId) || amount == 0) {
            return 0;
        }

        treePoints ??= new List<PokemonAbilityTreePointState>();
        var state = treePoints.FirstOrDefault(entry => entry != null && string.Equals(entry.treeId, treeId, StringComparison.OrdinalIgnoreCase));
        if(state == null) {
            state = new PokemonAbilityTreePointState { treeId = treeId, treeName = treeName };
            treePoints.Add(state);
        }

        int before = state.points;
        state.points = Mathf.Max(0, state.points + amount);
        int applied = state.points - before;
        if(applied != 0) {
            AddHistory(treeId, treeName, string.Empty, string.Empty, applied > 0 ? "points-gained" : "points-spent", applied, source, sourceId, sourceName);
        }
        return applied;
    }

    public bool HasUnlocked(string treeId, string nodeId) {
        return !string.IsNullOrWhiteSpace(treeId)
            && !string.IsNullOrWhiteSpace(nodeId)
            && unlockedNodes != null
            && unlockedNodes.Any(record => record != null
                && string.Equals(record.treeId, treeId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(record.nodeId, nodeId, StringComparison.OrdinalIgnoreCase));
    }

    public bool Unlock(PokemonAbilityTreeDefinition tree, PokemonAbilityTreeNodeDefinition node, PokemonAbilityPointSource source, string sourceId, string sourceName) {
        if(tree == null || node == null || HasUnlocked(tree.Id, node.NodeId)) {
            return false;
        }

        AddPoints(tree.Id, tree.DisplayName, -node.PointCost, source, sourceId, sourceName);
        unlockedNodes ??= new List<PokemonAbilityTreeUnlockRecord>();
        unlockedNodes.Add(new PokemonAbilityTreeUnlockRecord {
            treeId = tree.Id,
            treeName = tree.DisplayName,
            nodeId = node.NodeId,
            nodeName = node.DisplayName,
            pointCost = node.PointCost,
            source = source,
            sourceId = sourceId,
            sourceName = sourceName,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour()
        });

        appliedEffects ??= new List<PokemonAbilityTreeEffectRecord>();
        foreach(var effect in node.Effects) {
            if(effect != null) {
                appliedEffects.Add(effect.ToRecord(tree, node));
            }
        }

        AddHistory(tree.Id, tree.DisplayName, node.NodeId, node.DisplayName, "node-unlocked", -node.PointCost, source, sourceId, sourceName);
        return true;
    }

    public float GetEffectValue(PokemonAbilityTreeEffectKind kind, string tag = null) {
        return appliedEffects != null
            ? appliedEffects
                .Where(effect => effect != null
                    && effect.kind == kind
                    && (string.IsNullOrWhiteSpace(tag) || (effect.tags != null && effect.tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase)))))
                .Sum(effect => effect.value)
            : 0f;
    }

    void AddHistory(string treeId, string treeName, string nodeId, string nodeName, string operation, int pointsDelta, PokemonAbilityPointSource source, string sourceId, string sourceName) {
        history ??= new List<PokemonAbilityTreeHistoryRecord>();
        history.Add(new PokemonAbilityTreeHistoryRecord {
            treeId = treeId,
            treeName = treeName,
            nodeId = nodeId,
            nodeName = nodeName,
            operation = operation,
            pointsDelta = pointsDelta,
            source = source,
            sourceId = sourceId,
            sourceName = sourceName,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour()
        });

        if(history.Count > 100) {
            history.RemoveAt(0);
        }
    }

    int GetCurrentDay() {
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }
}

[Serializable]
public class PokemonAbilityTreePointState {
    [Tooltip("Ability tree id.")]
    public string treeId;
    [Tooltip("Ability tree display name saved for fallback/debug output.")]
    public string treeName;
    [Tooltip("Unspent ability points for this tree.")]
    public int points;
}

[Serializable]
public class PokemonAbilityTreeUnlockRecord {
    [Tooltip("Ability tree id.")]
    public string treeId;
    [Tooltip("Ability tree display name saved for fallback/debug output.")]
    public string treeName;
    [Tooltip("Unlocked node id.")]
    public string nodeId;
    [Tooltip("Unlocked node display name.")]
    public string nodeName;
    [Tooltip("Ability point cost paid for this node.")]
    public int pointCost;
    [Tooltip("Source category that unlocked this node.")]
    public PokemonAbilityPointSource source;
    [Tooltip("Specific source id.")]
    public string sourceId;
    [Tooltip("Specific source display name.")]
    public string sourceName;
    [Tooltip("In-game day when this node was unlocked.")]
    public int day;
    [Tooltip("Absolute in-game hour when this node was unlocked.")]
    public int absoluteHour;
}

[Serializable]
public class PokemonAbilityTreeEffectRecord {
    [Tooltip("Ability tree id that granted this effect.")]
    public string treeId;
    [Tooltip("Node id that granted this effect.")]
    public string nodeId;
    [Tooltip("Effect id.")]
    public string effectId;
    [Tooltip("Effect display name.")]
    public string effectName;
    [Tooltip("Effect kind.")]
    public PokemonAbilityTreeEffectKind kind;
    [Tooltip("Effect tags for future passive queries.")]
    public List<string> tags = new List<string>();
    [Tooltip("Affected stat for stat-like effects.")]
    public Stat stat;
    [Tooltip("Flat stat bonus for stat modifier effects.")]
    public int flatStatBonus;
    [Tooltip("Multiplier stat bonus for stat modifier effects.")]
    public float statMultiplierBonus;
    [Tooltip("Generic numeric value for custom systems.")]
    public float value;
}

[Serializable]
public class PokemonAbilityTreeHistoryRecord {
    [Tooltip("Ability tree id.")]
    public string treeId;
    [Tooltip("Ability tree display name.")]
    public string treeName;
    [Tooltip("Ability node id, if relevant.")]
    public string nodeId;
    [Tooltip("Ability node display name, if relevant.")]
    public string nodeName;
    [Tooltip("Operation such as points-gained, points-spent or node-unlocked.")]
    public string operation;
    [Tooltip("Ability point delta.")]
    public int pointsDelta;
    [Tooltip("Source category for this operation.")]
    public PokemonAbilityPointSource source;
    [Tooltip("Specific source id.")]
    public string sourceId;
    [Tooltip("Specific source display name.")]
    public string sourceName;
    [Tooltip("In-game day when this operation happened.")]
    public int day;
    [Tooltip("Absolute in-game hour when this operation happened.")]
    public int absoluteHour;
}

public class PokemonAbilityTreeSource : MonoBehaviour, Interactable, IPlayerTriggerable {
    [Header("Definition")]
    [Tooltip("Ability tree affected by this source.")]
    [SerializeField] PokemonAbilityTreeDefinition tree;
    [Tooltip("Node id unlocked by this source when the action includes Unlock Node.")]
    [SerializeField] string nodeId = string.Empty;
    [Tooltip("Player used by context-menu/start actions. Empty uses PlayerController.i.")]
    [SerializeField] PlayerController playerOverride;

    [Header("Action")]
    [Tooltip("Action performed by this source.")]
    [SerializeField] PokemonAbilityTreeSourceAction action = PokemonAbilityTreeSourceAction.GrantPoints;
    [Tooltip("Ability points granted before optional unlock is attempted.")]
    [Min(0)]
    [SerializeField] int points = 1;
    [Tooltip("Source category saved into ability tree history.")]
    [SerializeField] PokemonAbilityPointSource pointSource = PokemonAbilityPointSource.Training;
    [Tooltip("Specific source id saved into ability tree history. Empty uses this object name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Specific source name saved into ability tree history. Empty uses this object name.")]
    [SerializeField] string sourceName = string.Empty;

    [Header("Targeting")]
    [Tooltip("Which Pokemon receive points or unlocks.")]
    [SerializeField] PokemonAbilityTreeTarget target = PokemonAbilityTreeTarget.PartySlot;
    [Tooltip("Party slot used when Target is Party Slot.")]
    [Min(0)]
    [SerializeField] int partySlotIndex;
    [Tooltip("If enabled, trigger volumes may run this source repeatedly.")]
    [SerializeField] bool triggerRepeatedly;
    [Tooltip("If enabled, results are written to GameDebug.")]
    [SerializeField] bool writeDebugLog;

    public PokemonAbilityTreeDefinition Tree => tree;
    public string NodeId => nodeId;
    public PokemonAbilityTreeSourceAction Action => action;
    public PokemonAbilityTreeTarget Target => target;
    public int PartySlotIndex => Mathf.Max(0, partySlotIndex);
    public bool TriggerRepeatedly => triggerRepeatedly;

    public IEnumerator Interact(Transform initiator) {
        TryApply(initiator != null ? initiator.GetComponent<PlayerController>() : ResolvePlayer(), out _);
        yield break;
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(triggerRepeatedly) {
            TryApply(player != null ? player : ResolvePlayer(), out _);
        }
    }

    [ContextMenu("Apply Pokemon Ability Tree Source")]
    public void ApplyFromContextMenu() {
        TryApply(ResolvePlayer(), out _);
    }

    public bool TryApply(PlayerController player, out string feedback) {
        feedback = null;
        if(tree == null) {
            feedback = "Ability tree is missing.";
            WriteDebug(feedback, true);
            return false;
        }

        var party = player != null ? player.GetComponent<PokemonParty>() : null;
        if(party == null || party.Pokemons == null) {
            feedback = "Pokemon party is missing.";
            WriteDebug(feedback, true);
            return false;
        }

        int successCount = 0;
        string lastFailure = null;
        foreach(var pokemon in ResolveTargets(party)) {
            if(pokemon == null) {
                continue;
            }

            if(!tree.CanUseTree(pokemon, player, out lastFailure)) {
                continue;
            }

            if(action == PokemonAbilityTreeSourceAction.GrantPoints || action == PokemonAbilityTreeSourceAction.GrantPointsAndUnlockNode) {
                pokemon.GainAbilityPoints(tree, points, pointSource, ResolveSourceId(), ResolveSourceName());
            }

            bool success = action == PokemonAbilityTreeSourceAction.GrantPoints;
            if(action == PokemonAbilityTreeSourceAction.UnlockNode || action == PokemonAbilityTreeSourceAction.GrantPointsAndUnlockNode) {
                success = pokemon.TryUnlockAbilityNode(tree, nodeId, player, pointSource, ResolveSourceId(), ResolveSourceName(), out lastFailure);
            }

            if(success) {
                successCount++;
            }
        }

        if(successCount > 0) {
            party.PartyUpdated();
            feedback = $"{tree.DisplayName} updated for {successCount} Pokemon.";
            WriteDebug(feedback, false);
            return true;
        }

        feedback = string.IsNullOrWhiteSpace(lastFailure) ? "No Pokemon ability tree changes were applied." : lastFailure;
        WriteDebug(feedback, true);
        return false;
    }

    IEnumerable<Pokemon> ResolveTargets(PokemonParty party) {
        switch(target) {
            case PokemonAbilityTreeTarget.AllPartyPokemon:
                return party.Pokemons.Where(pokemon => pokemon != null);
            case PokemonAbilityTreeTarget.FirstPartyPokemon:
                return party.Pokemons.Where(pokemon => pokemon != null).Take(1);
            case PokemonAbilityTreeTarget.FirstHealthyPokemon:
                var healthy = party.GetHealthyPokemon();
                return healthy != null ? new[] { healthy } : Enumerable.Empty<Pokemon>();
            default:
                return partySlotIndex >= 0 && partySlotIndex < party.Pokemons.Count && party.Pokemons[partySlotIndex] != null
                    ? new[] { party.Pokemons[partySlotIndex] }
                    : Enumerable.Empty<Pokemon>();
        }
    }

    string ResolveSourceId() {
        return !string.IsNullOrWhiteSpace(sourceId) ? sourceId : name;
    }

    string ResolveSourceName() {
        return !string.IsNullOrWhiteSpace(sourceName) ? sourceName : name;
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        return PlayerController.i != null ? PlayerController.i : FindAnyObjectByType<PlayerController>();
    }

    void WriteDebug(string message, bool warning) {
        if(!writeDebugLog || string.IsNullOrWhiteSpace(message)) {
            return;
        }

        if(warning) {
            GameDebug.Warning(message, GameDebugCategory.General, this, "PokemonAbilityTreeSource");
        } else {
            GameDebug.Success(message, GameDebugCategory.General, this, "PokemonAbilityTreeSource");
        }
    }
}
