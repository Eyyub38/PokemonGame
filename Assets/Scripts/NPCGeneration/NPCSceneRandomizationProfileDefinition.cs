using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum NPCSceneRandomizationSeedMode {
    StableSceneAndProfile,
    FixedSeed,
    FreshRandom,
    DailyStable
}

[CreateAssetMenu(menuName = "NPC Generation/NPC Scene Randomization Profile")]
public class NPCSceneRandomizationProfileDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this scene randomization profile. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in editor/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining which route, town or scene chunk this profile is meant to randomize.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as route, city, market, trainer, civilian, night or event.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Selection")]
    [Tooltip("Maximum eligible slots randomized in one run. 0 means all eligible slots.")]
    [Min(0)]
    [SerializeField] int maxSlotsToRandomize = 0;
    [Tooltip("If enabled, slots marked as fixed/special can still be randomized by this profile.")]
    [SerializeField] bool allowFixedSpecialSlots = false;
    [Tooltip("If enabled, a slot-level pool override is used before the selected rule's pool.")]
    [SerializeField] bool preferSlotPoolOverride = true;

    [Header("Requirements")]
    [Tooltip("How profile-level requirements are evaluated before scene NPC randomization can run.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Requirements checked before any slot is randomized by this profile.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();

    [Header("Rules")]
    [Tooltip("Weighted rules that match scene slots and choose NPC variant pools.")]
    [SerializeField] List<NPCSceneRandomizationRule> rules = new List<NPCSceneRandomizationRule>();

    [Header("Events")]
    [Tooltip("Optional event published when this profile successfully applies at least one NPC variant.")]
    [SerializeField] GameEventDefinition completedEvent = null;
    [Tooltip("Optional event published when this profile cannot apply any NPC variant.")]
    [SerializeField] GameEventDefinition blockedEvent = null;
    [Tooltip("If enabled, scene NPC randomization events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = false;
    [Tooltip("If enabled, scene NPC randomization events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog = false;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public int MaxSlotsToRandomize => Mathf.Max(0, maxSlotsToRandomize);
    public bool AllowFixedSpecialSlots => allowFixedSpecialSlots;
    public bool PreferSlotPoolOverride => preferSlotPoolOverride;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public IReadOnlyList<NPCSceneRandomizationRule> Rules => rules != null ? (IReadOnlyList<NPCSceneRandomizationRule>)rules : Array.Empty<NPCSceneRandomizationRule>();
    public bool ShowEventsInFeed => showEventsInFeed;
    public bool WriteEventsToDebugLog => writeEventsToDebugLog;

    public bool CanUse(PlayerController player, out string failureMessage) {
        return ConsequenceChainDefinition.RequirementsMet(player, requirements, requirementMatchMode, out failureMessage);
    }

    public List<NPCSceneRandomizationAssignment> BuildAssignments(
        IReadOnlyList<NPCSceneRandomizationSlot> slots,
        PlayerController player,
        int seed,
        out List<string> messages
    ) {
        messages = new List<string>();
        var assignments = new List<NPCSceneRandomizationAssignment>();
        if(slots == null || slots.Count == 0) {
            messages.Add("No NPC randomization slots were found.");
            return assignments;
        }

        var random = new System.Random(seed);
        var ruleUseCounts = new Dictionary<NPCSceneRandomizationRule, int>();
        var orderedSlots = slots
            .Where(slot => slot != null)
            .OrderBy(slot => slot.SortOrder)
            .ThenBy(slot => slot.SlotId)
            .ToList();

        foreach(var slot in orderedSlots) {
            if(MaxSlotsToRandomize > 0 && assignments.Count >= MaxSlotsToRandomize) {
                break;
            }

            if(!slot.CanBeRandomized(allowFixedSpecialSlots, out string slotFailure)) {
                if(!string.IsNullOrWhiteSpace(slotFailure)) {
                    messages.Add($"{slot.SlotId}: {slotFailure}");
                }
                continue;
            }

            var validRules = Rules
                .Where(rule => rule != null && rule.CanApply(slot, player, GetRuleUseCount(ruleUseCounts, rule), out _))
                .Where(rule => rule.ResolvePool(slot, preferSlotPoolOverride) != null)
                .ToList();

            var selectedRule = PickRule(validRules, random);
            if(selectedRule == null) {
                messages.Add($"{slot.SlotId}: no matching randomization rule.");
                continue;
            }

            var pool = selectedRule.ResolvePool(slot, preferSlotPoolOverride);
            int assignmentSeed = Hash(seed, slot.SlotId, selectedRule.RuleId, pool.Id);
            assignments.Add(new NPCSceneRandomizationAssignment(slot, selectedRule, pool, assignmentSeed));
            ruleUseCounts[selectedRule] = GetRuleUseCount(ruleUseCounts, selectedRule) + 1;
        }

        return assignments;
    }

    public NPCSceneRandomizationRule GetRule(string ruleId) {
        if(string.IsNullOrWhiteSpace(ruleId)) {
            return null;
        }

        return Rules.FirstOrDefault(rule => rule != null && rule.RuleId == ruleId);
    }

    public void PublishCompleted(NPCSceneRandomizationRunResult result, PlayerController player, UnityEngine.Object context) {
        PublishResult(completedEvent, "completed", result, player, context, GameEventImportance.Info);
    }

    public void PublishBlocked(NPCSceneRandomizationRunResult result, PlayerController player, UnityEngine.Object context) {
        PublishResult(blockedEvent, "blocked", result, player, context, GameEventImportance.Warning);
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    NPCSceneRandomizationRule PickRule(List<NPCSceneRandomizationRule> candidates, System.Random random) {
        if(candidates == null || candidates.Count == 0 || random == null) {
            return null;
        }

        int totalWeight = candidates.Sum(rule => rule.Weight);
        if(totalWeight <= 0) {
            return candidates[0];
        }

        int roll = random.Next(0, totalWeight);
        foreach(var rule in candidates) {
            roll -= rule.Weight;
            if(roll < 0) {
                return rule;
            }
        }

        return candidates[candidates.Count - 1];
    }

    int GetRuleUseCount(Dictionary<NPCSceneRandomizationRule, int> ruleUseCounts, NPCSceneRandomizationRule rule) {
        return rule != null && ruleUseCounts.TryGetValue(rule, out int count) ? count : 0;
    }

    void PublishResult(
        GameEventDefinition definition,
        string phase,
        NPCSceneRandomizationRunResult result,
        PlayerController player,
        UnityEngine.Object context,
        GameEventImportance importance
    ) {
        GameEventPublishing.PublishOptional(
            definition,
            $"npc-scene-randomization.{phase}.{Id}",
            phase == "completed"
                ? $"{DisplayName} randomized {result?.appliedSlots ?? 0} NPC slot(s)."
                : $"{DisplayName} randomization blocked: {result?.failureMessage}",
            GameEventCategory.NPC,
            importance,
            context != null ? context : player != null ? player : this,
            "NPCSceneRandomizationProfile",
            GameEventScope.Scene,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("profileId", Id),
            GameEventPublishing.Value("profileName", DisplayName),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("controllerId", result != null ? result.controllerId : string.Empty),
            GameEventPublishing.Value("slotsFound", result != null ? result.slotsFound : 0),
            GameEventPublishing.Value("assignments", result != null ? result.assignments : 0),
            GameEventPublishing.Value("appliedSlots", result != null ? result.appliedSlots : 0),
            GameEventPublishing.Value("blocked", result != null && result.blocked),
            GameEventPublishing.Value("failureMessage", result != null ? result.failureMessage : string.Empty));
    }

    public static int Hash(params object[] values) {
        unchecked {
            int hash = 23;
            if(values == null) {
                return hash;
            }

            foreach(var value in values) {
                string text = value != null ? value.ToString() : string.Empty;
                for(int i = 0; i < text.Length; i++) {
                    hash = hash * 31 + text[i];
                }
                hash = hash * 31 + 17;
            }

            return hash;
        }
    }
}

[Serializable]
public class NPCSceneRandomizationRule {
    [Header("Identity")]
    [Tooltip("Stable id for this rule inside the scene randomization profile. Empty uses display name or variant pool id.")]
    [SerializeField] string ruleId = string.Empty;
    [Tooltip("Editor/debug label for this rule.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("If disabled, this rule is ignored.")]
    [SerializeField] bool enabled = true;
    [Tooltip("Relative chance for this rule when multiple rules match a slot.")]
    [Min(0)]
    [SerializeField] int weight = 10;
    [Tooltip("Maximum slots this rule can apply to in one run. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxApplications = 0;

    [Header("Pool")]
    [Tooltip("NPC variant pool applied by this rule. A slot-level override can replace this if the profile allows it.")]
    [SerializeField] NPCVariantPoolDefinition variantPool = null;

    [Header("Slot Filters")]
    [Tooltip("Only slots with this tag can match. Empty means no required tag.")]
    [SerializeField] string requiredSlotTag = string.Empty;
    [Tooltip("Slots with this tag cannot match. Empty means no blocked tag.")]
    [SerializeField] string blockedSlotTag = string.Empty;
    [Tooltip("Allowed slot role hints. Empty means any role hint.")]
    [SerializeField] List<NPCVariantRole> allowedRoles = new List<NPCVariantRole>();
    [Tooltip("If enabled, this rule only matches slots that have a TrainerController.")]
    [SerializeField] bool requireTrainerController = false;
    [Tooltip("If enabled, this rule only matches slots that have an NPCController.")]
    [SerializeField] bool requireNpcController = false;

    [Header("Requirements")]
    [Tooltip("How rule-level requirements are evaluated before this rule can match a slot.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Requirements checked before this rule can be used.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();

    public string RuleId => !string.IsNullOrWhiteSpace(ruleId) ? ruleId : !string.IsNullOrWhiteSpace(displayName) ? displayName : variantPool != null ? variantPool.Id : "npc-scene-rule";
    public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName : RuleId;
    public bool Enabled => enabled;
    public int Weight => Mathf.Max(0, weight);
    public int MaxApplications => Mathf.Max(0, maxApplications);
    public NPCVariantPoolDefinition VariantPool => variantPool;
    public string RequiredSlotTag => requiredSlotTag;
    public string BlockedSlotTag => blockedSlotTag;
    public IReadOnlyList<NPCVariantRole> AllowedRoles => allowedRoles != null ? (IReadOnlyList<NPCVariantRole>)allowedRoles : Array.Empty<NPCVariantRole>();
    public bool RequireTrainerController => requireTrainerController;
    public bool RequireNpcController => requireNpcController;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();

    public bool CanApply(NPCSceneRandomizationSlot slot, PlayerController player, int currentApplications, out string failureMessage) {
        if(!enabled) {
            failureMessage = "Rule is disabled.";
            return false;
        }

        if(slot == null) {
            failureMessage = "Slot is missing.";
            return false;
        }

        if(MaxApplications > 0 && currentApplications >= MaxApplications) {
            failureMessage = "Rule reached max applications.";
            return false;
        }

        if(!string.IsNullOrWhiteSpace(requiredSlotTag) && !slot.HasTag(requiredSlotTag)) {
            failureMessage = "Required slot tag is missing.";
            return false;
        }

        if(!string.IsNullOrWhiteSpace(blockedSlotTag) && slot.HasTag(blockedSlotTag)) {
            failureMessage = "Blocked slot tag is present.";
            return false;
        }

        if(allowedRoles != null && allowedRoles.Count > 0 && !allowedRoles.Contains(slot.RoleHint)) {
            failureMessage = "Slot role is not allowed.";
            return false;
        }

        if(requireTrainerController && !slot.HasTrainerController) {
            failureMessage = "Slot has no TrainerController.";
            return false;
        }

        if(requireNpcController && !slot.HasNpcController) {
            failureMessage = "Slot has no NPCController.";
            return false;
        }

        if(!ConsequenceChainDefinition.RequirementsMet(player, requirements, requirementMatchMode, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public NPCVariantPoolDefinition ResolvePool(NPCSceneRandomizationSlot slot, bool preferSlotPoolOverride) {
        if(preferSlotPoolOverride && slot != null && slot.PoolOverride != null) {
            return slot.PoolOverride;
        }

        return variantPool != null ? variantPool : slot != null ? slot.PoolOverride : null;
    }
}

public class NPCSceneRandomizationSlot : MonoBehaviour {
    [Header("Identity")]
    [Tooltip("Stable slot id used by scene randomization saves. Empty uses GameObject name and position.")]
    [SerializeField] string slotId = string.Empty;
    [Tooltip("Editor/debug label for this slot. Empty uses GameObject name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Sort order used when a scene profile walks through eligible slots.")]
    [SerializeField] int sortOrder = 0;
    [Tooltip("Role hint used by profile rules to decide which pools can target this slot.")]
    [SerializeField] NPCVariantRole roleHint = NPCVariantRole.Civilian;
    [Tooltip("Free-form tags such as street, market, route, trainer, night, child, adult or worker.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Rules")]
    [Tooltip("If disabled, scene randomization controllers ignore this slot.")]
    [SerializeField] bool randomizationEnabled = true;
    [Tooltip("If enabled, this slot is treated as a fixed/story/special NPC unless a profile explicitly allows fixed slots.")]
    [SerializeField] bool fixedSpecialNpc = false;
    [Tooltip("Optional pool override used before the selected profile rule's pool when the profile allows it.")]
    [SerializeField] NPCVariantPoolDefinition poolOverride = null;

    public string SlotId {
        get {
            if(!string.IsNullOrWhiteSpace(slotId)) {
                return slotId;
            }

            var pos = transform.position;
            return $"{name}:{Mathf.RoundToInt(pos.x * 10f)}:{Mathf.RoundToInt(pos.y * 10f)}";
        }
    }

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public int SortOrder => sortOrder;
    public NPCVariantRole RoleHint => roleHint;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public bool RandomizationEnabled => randomizationEnabled;
    public bool FixedSpecialNpc => fixedSpecialNpc;
    public NPCVariantPoolDefinition PoolOverride => poolOverride;
    public bool HasTrainerController => GetComponent<TrainerController>() != null;
    public bool HasNpcController => GetComponent<NPCController>() != null;

    public bool CanBeRandomized(bool allowFixedSpecialSlots, out string failureMessage) {
        if(!randomizationEnabled) {
            failureMessage = "Slot randomization is disabled.";
            return false;
        }

        if(fixedSpecialNpc && !allowFixedSpecialSlots) {
            failureMessage = "Slot is marked as fixed/special.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    public NPCSceneRandomizationRecord ApplyAssignment(NPCVariantPoolDefinition pool, int seed, bool forceApplyWhenRandomizerDisabled) {
        var record = new NPCSceneRandomizationRecord {
            slotId = SlotId,
            slotName = DisplayName,
            poolId = pool != null ? pool.Id : string.Empty,
            seed = seed,
            applied = false
        };

        if(pool == null) {
            record.message = "Variant pool is missing.";
            return record;
        }

        var randomizer = GetComponent<NPCVariantRandomizer>() ?? gameObject.AddComponent<NPCVariantRandomizer>();
        record.applied = randomizer.GenerateAndApplyExternal(pool, seed, rememberGeneratedProfile: true, forceApplyWhenDisabled: forceApplyWhenRandomizerDisabled);
        record.variantId = randomizer.GeneratedVariantId;
        record.displayName = randomizer.GeneratedDisplayName;
        record.message = record.applied ? "Applied." : "NPCVariantRandomizer did not apply.";
        return record;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }
}

public class NPCSceneRandomizationController : MonoBehaviour, ISavable {
    [Header("Identity")]
    [Tooltip("Stable controller id used in logs and save records. Empty uses GameObject name.")]
    [SerializeField] string controllerId = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName = string.Empty;

    [Header("Profile")]
    [Tooltip("Scene randomization profile that maps eligible slots to NPC variant pools.")]
    [SerializeField] NPCSceneRandomizationProfileDefinition profile = null;
    [Tooltip("Optional player override used for requirement checks. Empty uses PlayerController.i or first loaded PlayerController.")]
    [SerializeField] PlayerController playerOverride = null;

    [Header("Signals")]
    [Tooltip("If enabled, randomization runs during Start.")]
    [SerializeField] bool randomizeOnStart = true;
    [Tooltip("If enabled, randomization runs whenever this component enables.")]
    [SerializeField] bool randomizeOnEnable = false;
    [Tooltip("If enabled, a saved run is reused instead of rerolling when records already exist.")]
    [SerializeField] bool keepSavedRun = true;
    [Tooltip("If enabled, saved records are reapplied when RestoreState is called.")]
    [SerializeField] bool applySavedRecordsOnRestore = true;

    [Header("Slot Search")]
    [Tooltip("Optional root searched for NPCSceneRandomizationSlot children. Empty can search the whole loaded scene.")]
    [SerializeField] Transform slotRoot = null;
    [Tooltip("Manual slots included before child/scene search results.")]
    [SerializeField] List<NPCSceneRandomizationSlot> manualSlots = new List<NPCSceneRandomizationSlot>();
    [Tooltip("If enabled, child slots under Slot Root or this transform are included.")]
    [SerializeField] bool includeChildSlots = true;
    [Tooltip("If enabled and no Slot Root is assigned, the whole loaded scene is searched for slots.")]
    [SerializeField] bool searchWholeSceneWhenNoRoot = true;
    [Tooltip("If enabled, inactive slots can be found and randomized.")]
    [SerializeField] bool includeInactiveSlots = true;

    [Header("Seed")]
    [Tooltip("How this controller chooses the base randomization seed.")]
    [SerializeField] NPCSceneRandomizationSeedMode seedMode = NPCSceneRandomizationSeedMode.StableSceneAndProfile;
    [Tooltip("Fixed seed used when Seed Mode is Fixed Seed.")]
    [SerializeField] int fixedSeed = 0;
    [Tooltip("Extra stable salt added to generated seeds so two controllers can use the same profile differently.")]
    [SerializeField] int seedSalt = 0;
    [Tooltip("Optional stable key used in stable seed modes. Empty uses Controller Id.")]
    [SerializeField] string stableKey = string.Empty;

    [Header("Apply")]
    [Tooltip("If enabled, slot assignment can apply even when the target NPCVariantRandomizer has its own randomization flag disabled.")]
    [SerializeField] bool forceApplyWhenRandomizerDisabled = true;

    [Header("Debug")]
    [Tooltip("If enabled, randomization attempts are written to GameDebugLogger.")]
    [SerializeField] bool logAttempts = false;

    [Header("Runtime")]
    [Tooltip("Whether this controller has generated or restored a run in the current save/session.")]
    [SerializeField] bool hasRun = false;
    [Tooltip("Seed used by the latest randomization run.")]
    [SerializeField] int lastSeed = 0;
    [Tooltip("Saved/runtime records produced by the latest randomization run.")]
    [SerializeField] List<NPCSceneRandomizationRecord> records = new List<NPCSceneRandomizationRecord>();

    public string ControllerId => string.IsNullOrWhiteSpace(controllerId) ? name : controllerId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public NPCSceneRandomizationProfileDefinition Profile => profile;
    public IReadOnlyList<NPCSceneRandomizationSlot> ManualSlots => manualSlots != null ? (IReadOnlyList<NPCSceneRandomizationSlot>)manualSlots : Array.Empty<NPCSceneRandomizationSlot>();
    public bool RandomizeOnStart => randomizeOnStart;
    public bool RandomizeOnEnable => randomizeOnEnable;
    public bool KeepSavedRun => keepSavedRun;
    public bool IncludeChildSlots => includeChildSlots;
    public bool SearchWholeSceneWhenNoRoot => searchWholeSceneWhenNoRoot;
    public bool IncludeInactiveSlots => includeInactiveSlots;
    public IReadOnlyList<NPCSceneRandomizationRecord> Records => records != null ? (IReadOnlyList<NPCSceneRandomizationRecord>)records : Array.Empty<NPCSceneRandomizationRecord>();
    public bool HasRun => hasRun;
    public int LastSeed => lastSeed;

    void OnEnable() {
        if(randomizeOnEnable) {
            RunRandomization();
        }
    }

    void Start() {
        if(randomizeOnStart) {
            RunRandomization();
        }
    }

    [ContextMenu("Run NPC Scene Randomization")]
    public void RunRandomizationFromContextMenu() {
        RunRandomization(forceReroll: true);
    }

    public NPCSceneRandomizationRunResult RunRandomization(bool forceReroll = false) {
        var result = new NPCSceneRandomizationRunResult(
            profile != null ? profile.Id : string.Empty,
            profile != null ? profile.DisplayName : string.Empty,
            ControllerId,
            DisplayName);

        var player = ResolvePlayer();
        if(profile == null) {
            return HandleBlocked(result, player, "NPC scene randomization profile is missing.");
        }

        var slots = ResolveSlots();
        result.slotsFound = slots.Count;
        if(slots.Count == 0) {
            return HandleBlocked(result, player, "No NPC scene randomization slots were found.");
        }

        if(keepSavedRun && hasRun && records != null && records.Count > 0 && !forceReroll) {
            ApplySavedRecords(slots, result);
            WriteAttemptLog(result);
            return result;
        }

        if(!profile.CanUse(player, out string requirementFailure)) {
            return HandleBlocked(result, player, requirementFailure);
        }

        lastSeed = ResolveSeed();
        var assignments = profile.BuildAssignments(slots, player, lastSeed, out var messages);
        result.messages.AddRange(messages);
        result.assignments = assignments.Count;
        if(assignments.Count == 0) {
            return HandleBlocked(result, player, "No NPC randomization assignments were selected.");
        }

        records = new List<NPCSceneRandomizationRecord>();
        foreach(var assignment in assignments) {
            var record = assignment.Apply(forceApplyWhenRandomizerDisabled);
            record.ruleId = assignment.Rule.RuleId;
            records.Add(record);
            result.records.Add(record);
            if(record.applied) {
                result.appliedSlots++;
            } else {
                result.skippedSlots++;
                if(!string.IsNullOrWhiteSpace(record.message)) {
                    result.messages.Add($"{record.slotId}: {record.message}");
                }
            }
        }

        hasRun = result.appliedSlots > 0;
        if(result.appliedSlots <= 0) {
            return HandleBlocked(result, player, "Assignments were selected but no NPC slot was applied.");
        }

        profile.PublishCompleted(result, player, this);
        WriteAttemptLog(result);
        return result;
    }

    void ApplySavedRecords(List<NPCSceneRandomizationSlot> slots, NPCSceneRandomizationRunResult result) {
        if(records == null || records.Count == 0) {
            return;
        }

        var slotById = slots
            .Where(slot => slot != null)
            .GroupBy(slot => slot.SlotId)
            .ToDictionary(group => group.Key, group => group.First());

        foreach(var record in records) {
            if(record == null || string.IsNullOrWhiteSpace(record.slotId)) {
                result.skippedSlots++;
                continue;
            }

            if(!slotById.TryGetValue(record.slotId, out var slot) || slot == null) {
                result.skippedSlots++;
                result.messages.Add($"{record.slotId}: saved slot no longer exists.");
                continue;
            }

            var pool = ResolvePoolForSavedRecord(slot, record);
            var appliedRecord = slot.ApplyAssignment(pool, record.seed, forceApplyWhenRandomizerDisabled);
            appliedRecord.ruleId = record.ruleId;
            result.records.Add(appliedRecord);
            if(appliedRecord.applied) {
                result.appliedSlots++;
            } else {
                result.skippedSlots++;
            }
        }
    }

    NPCVariantPoolDefinition ResolvePoolForSavedRecord(NPCSceneRandomizationSlot slot, NPCSceneRandomizationRecord record) {
        var rule = profile != null ? profile.GetRule(record.ruleId) : null;
        var pool = rule != null ? rule.ResolvePool(slot, profile.PreferSlotPoolOverride) : null;
        return pool != null ? pool : slot != null ? slot.PoolOverride : null;
    }

    NPCSceneRandomizationRunResult HandleBlocked(NPCSceneRandomizationRunResult result, PlayerController player, string failureMessage) {
        result.blocked = true;
        result.failureMessage = string.IsNullOrWhiteSpace(failureMessage) ? "NPC scene randomization was blocked." : failureMessage;
        profile?.PublishBlocked(result, player, this);
        WriteAttemptLog(result);
        return result;
    }

    List<NPCSceneRandomizationSlot> ResolveSlots() {
        var results = new List<NPCSceneRandomizationSlot>();
        var seen = new HashSet<EntityId>();

        AddSlots(manualSlots, results, seen);

        if(includeChildSlots) {
            var root = slotRoot != null ? slotRoot : transform;
            AddSlots(root.GetComponentsInChildren<NPCSceneRandomizationSlot>(includeInactiveSlots), results, seen);
        }

        if(searchWholeSceneWhenNoRoot && slotRoot == null) {
            var inactiveMode = includeInactiveSlots ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
            AddSlots(FindObjectsByType<NPCSceneRandomizationSlot>(inactiveMode), results, seen);
        }

        return results
            .Where(slot => slot != null && slot.gameObject.scene == gameObject.scene)
            .OrderBy(slot => slot.SortOrder)
            .ThenBy(slot => slot.SlotId)
            .ToList();
    }

    void AddSlots(IEnumerable<NPCSceneRandomizationSlot> source, List<NPCSceneRandomizationSlot> results, HashSet<EntityId> seen) {
        if(source == null) {
            return;
        }

        foreach(var slot in source) {
            if(slot == null) {
                continue;
            }

            var id = slot.GetEntityId();
            if(seen.Add(id)) {
                results.Add(slot);
            }
        }
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        if(PlayerController.i != null) {
            return PlayerController.i;
        }

        return FindAnyObjectByType<PlayerController>();
    }

    int ResolveSeed() {
        if(seedMode == NPCSceneRandomizationSeedMode.FixedSeed) {
            return fixedSeed;
        }

        if(seedMode == NPCSceneRandomizationSeedMode.FreshRandom) {
            return UnityEngine.Random.Range(1, int.MaxValue);
        }

        string key = string.IsNullOrWhiteSpace(stableKey) ? ControllerId : stableKey;
        string sceneName = SceneManager.GetActiveScene().name;
        int day = seedMode == NPCSceneRandomizationSeedMode.DailyStable && TimeSystem.i != null ? TimeSystem.i.Day : 0;
        return NPCSceneRandomizationProfileDefinition.Hash(sceneName, key, profile != null ? profile.Id : string.Empty, seedSalt, day);
    }

    void WriteAttemptLog(NPCSceneRandomizationRunResult result) {
        if(!logAttempts || result == null) {
            return;
        }

        GameDebugLogger.Ensure().Record(
            result.blocked ? GameDebugSeverity.Warning : GameDebugSeverity.Info,
            GameDebugCategory.NPC,
            result.blocked ? $"{DisplayName} NPC randomization blocked: {result.failureMessage}" : $"{DisplayName} randomized {result.appliedSlots} NPC slot(s).",
            this,
            "NPCSceneRandomizationController");
    }

    public object CaptureState() {
        return new NPCSceneRandomizationControllerSaveData {
            hasRun = hasRun,
            lastSeed = lastSeed,
            records = records != null ? records.Select(record => record != null ? new NPCSceneRandomizationRecord(record) : null).ToList() : new List<NPCSceneRandomizationRecord>()
        };
    }

    public void RestoreState(object state) {
        var saveData = state as NPCSceneRandomizationControllerSaveData;
        if(saveData == null) {
            return;
        }

        hasRun = saveData.hasRun;
        lastSeed = saveData.lastSeed;
        records = saveData.records != null
            ? saveData.records.Where(record => record != null).Select(record => new NPCSceneRandomizationRecord(record)).ToList()
            : new List<NPCSceneRandomizationRecord>();

        if(applySavedRecordsOnRestore && hasRun && records.Count > 0) {
            var result = new NPCSceneRandomizationRunResult(
                profile != null ? profile.Id : string.Empty,
                profile != null ? profile.DisplayName : string.Empty,
                ControllerId,
                DisplayName);
            ApplySavedRecords(ResolveSlots(), result);
            WriteAttemptLog(result);
        }
    }
}

public class NPCSceneRandomizationAssignment {
    public NPCSceneRandomizationSlot Slot { get; }
    public NPCSceneRandomizationRule Rule { get; }
    public NPCVariantPoolDefinition Pool { get; }
    public int Seed { get; }

    public NPCSceneRandomizationAssignment(NPCSceneRandomizationSlot slot, NPCSceneRandomizationRule rule, NPCVariantPoolDefinition pool, int seed) {
        Slot = slot;
        Rule = rule;
        Pool = pool;
        Seed = seed;
    }

    public NPCSceneRandomizationRecord Apply(bool forceApplyWhenRandomizerDisabled) {
        if(Slot == null) {
            return new NPCSceneRandomizationRecord {
                ruleId = Rule != null ? Rule.RuleId : string.Empty,
                poolId = Pool != null ? Pool.Id : string.Empty,
                seed = Seed,
                applied = false,
                message = "Slot is missing."
            };
        }

        var record = Slot.ApplyAssignment(Pool, Seed, forceApplyWhenRandomizerDisabled);
        record.ruleId = Rule != null ? Rule.RuleId : string.Empty;
        return record;
    }
}

[Serializable]
public class NPCSceneRandomizationRunResult {
    [Tooltip("Profile id used by this randomization run.")]
    public string profileId;
    [Tooltip("Profile display name used by this randomization run.")]
    public string profileName;
    [Tooltip("Controller id that ran randomization.")]
    public string controllerId;
    [Tooltip("Controller display name that ran randomization.")]
    public string controllerName;
    [Tooltip("Number of scene slots found before filtering.")]
    public int slotsFound;
    [Tooltip("Number of assignments selected by the profile.")]
    public int assignments;
    [Tooltip("Number of slots successfully applied.")]
    public int appliedSlots;
    [Tooltip("Number of slots skipped or failed.")]
    public int skippedSlots;
    [Tooltip("Whether the run was blocked.")]
    public bool blocked;
    [Tooltip("Human-readable failure message when blocked.")]
    public string failureMessage;
    [Tooltip("Messages collected during assignment and application.")]
    public List<string> messages = new List<string>();
    [Tooltip("Records produced by this run.")]
    public List<NPCSceneRandomizationRecord> records = new List<NPCSceneRandomizationRecord>();

    public NPCSceneRandomizationRunResult(string profileId, string profileName, string controllerId, string controllerName) {
        this.profileId = profileId;
        this.profileName = profileName;
        this.controllerId = controllerId;
        this.controllerName = controllerName;
    }
}

[Serializable]
public class NPCSceneRandomizationRecord {
    [Tooltip("Slot id that received this generated NPC variant.")]
    public string slotId;
    [Tooltip("Slot display name saved for debug/fallback output.")]
    public string slotName;
    [Tooltip("Rule id used for this slot.")]
    public string ruleId;
    [Tooltip("Variant pool id used for this slot.")]
    public string poolId;
    [Tooltip("Seed used to generate this slot.")]
    public int seed;
    [Tooltip("Generated variant id.")]
    public string variantId;
    [Tooltip("Generated display name.")]
    public string displayName;
    [Tooltip("Whether the generated variant was applied successfully.")]
    public bool applied;
    [Tooltip("Debug/failure message for this slot.")]
    public string message;

    public NPCSceneRandomizationRecord() {
    }

    public NPCSceneRandomizationRecord(NPCSceneRandomizationRecord other) {
        if(other == null) {
            return;
        }

        slotId = other.slotId;
        slotName = other.slotName;
        ruleId = other.ruleId;
        poolId = other.poolId;
        seed = other.seed;
        variantId = other.variantId;
        displayName = other.displayName;
        applied = other.applied;
        message = other.message;
    }
}

[Serializable]
public class NPCSceneRandomizationControllerSaveData {
    public bool hasRun;
    public int lastSeed;
    public List<NPCSceneRandomizationRecord> records = new List<NPCSceneRandomizationRecord>();
}
