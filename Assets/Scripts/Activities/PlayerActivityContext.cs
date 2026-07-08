using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PlayerActivityContext {
    static readonly List<ActivityZoneDefinition> activeZones = new List<ActivityZoneDefinition>();

    public static ActivityZoneDefinition CurrentZone => activeZones
        .Where(z => z != null)
        .OrderBy(z => z.Priority)
        .ThenBy(z => activeZones.LastIndexOf(z))
        .LastOrDefault();

    public static IReadOnlyList<ActivityZoneDefinition> ActiveZones => activeZones;

    public static bool IsAllowed(ActivityDefinition activity) {
        return CanPerform(activity, PlayerController.i, out _);
    }

    public static bool IsAllowed(ActivityDefinition activity, PlayerController player) {
        return CanPerform(activity, player, out _);
    }

    public static bool CanPerform(ActivityDefinition activity, out string failureMessage) {
        return CanPerform(activity, PlayerController.i, out failureMessage);
    }

    public static bool CanPerform(ActivityDefinition activity, PlayerController player, out string failureMessage) {
        if(activity == null) {
            failureMessage = "This activity is not configured.";
            return false;
        }

        if(!activity.RequiresActivityZone) {
            failureMessage = null;
            return true;
        }

        var zone = CurrentZone;
        if(zone == null) {
            failureMessage = activity.GetNoValidAreaMessage();
            return false;
        }

        if(zone.Allows(activity, player, out failureMessage)) {
            return true;
        }

        failureMessage ??= activity.GetNoValidAreaMessage();
        return false;
    }

    public static void SetCurrentZone(ActivityZoneDefinition zone) {
        if(zone != null) {
            bool wasActive = activeZones.Contains(zone);
            activeZones.Add(zone);
            if(wasActive) {
                return;
            }

            PublishZoneEvent(zone, active: true);
        }
    }

    public static void ClearCurrentZone(ActivityZoneDefinition zone) {
        if(zone != null) {
            bool removed = activeZones.Remove(zone);
            if(removed && !activeZones.Contains(zone)) {
                PublishZoneEvent(zone, active: false);
            }
        }
    }

    public static void ClearAll() {
        activeZones.Clear();
    }

    public static bool HasActiveZone(ActivityZoneDefinition zone) {
        return zone != null && activeZones.Contains(zone);
    }

    public static bool HasActiveZoneType(ActivityZoneType zoneType) {
        return activeZones.Any(z => z != null && z.ZoneType == zoneType);
    }

    public static bool HasActiveTag(string tag) {
        return activeZones.Any(z => z != null && z.HasTag(tag));
    }

    public static int ModifyExperience(ActivityDefinition activity, int amount) {
        if(amount <= 0) {
            return 0;
        }

        float multiplier = 1f;
        int flatBonus = 0;
        foreach(var modifier in GetActiveModifiers(activity)) {
            multiplier *= modifier.ExperienceMultiplier;
            flatBonus += modifier.FlatExperienceBonus;
        }

        foreach(var state in GetActiveWorldConditionStates(activity)) {
            var condition = state.ResolveDefinition();
            if(condition == null) {
                continue;
            }

            multiplier *= state.ScaleMultiplier(condition.ExperienceMultiplier);
            flatBonus += state.ScaleFlatBonus(condition.FlatExperienceBonus);
        }

        foreach(var companion in GetActiveCompanions()) {
            multiplier *= companion.GetExperienceMultiplier(activity);
            flatBonus += companion.GetFlatExperienceBonus(activity);
        }

        return Mathf.Max(0, Mathf.RoundToInt(amount * multiplier) + flatBonus);
    }

    public static int GetYieldBonus(ActivityDefinition activity) {
        int bonus = 0;
        foreach(var modifier in GetActiveModifiers(activity)) {
            bonus += modifier.YieldBonus;
        }
        foreach(var state in GetActiveWorldConditionStates(activity)) {
            var condition = state.ResolveDefinition();
            if(condition != null) {
                bonus += state.ScaleFlatBonus(condition.YieldBonus);
            }
        }
        foreach(var companion in GetActiveCompanions()) {
            bonus += companion.GetYieldBonus(activity);
        }
        return bonus;
    }

    public static int GetResearchPointBonus(ActivityDefinition activity) {
        int bonus = 0;
        foreach(var modifier in GetActiveModifiers(activity)) {
            bonus += modifier.ResearchPointBonus;
        }
        foreach(var state in GetActiveWorldConditionStates(activity)) {
            var condition = state.ResolveDefinition();
            if(condition != null) {
                bonus += state.ScaleFlatBonus(condition.ResearchPointBonus);
            }
        }
        foreach(var companion in GetActiveCompanions()) {
            bonus += companion.GetResearchPointBonus(activity);
        }
        return bonus;
    }

    public static int GetPokemonCareBonus(ActivityDefinition activity) {
        int bonus = 0;
        foreach(var modifier in GetActiveModifiers(activity)) {
            bonus += modifier.PokemonCareBonus;
        }
        foreach(var state in GetActiveWorldConditionStates(activity)) {
            var condition = state.ResolveDefinition();
            if(condition != null) {
                bonus += state.ScaleFlatBonus(condition.PokemonCareBonus);
            }
        }
        foreach(var companion in GetActiveCompanions()) {
            bonus += companion.GetPokemonCareBonus(activity);
        }
        return bonus;
    }

    public static int ModifyItemCost(ActivityDefinition activity, int amount) {
        return ModifyCost(activity, amount, GetItemCostMultiplier, condition => condition.ItemCostMultiplier, (companion, targetActivity) => companion.GetItemCostMultiplier(targetActivity));
    }

    public static int ModifyToolDurabilityCost(ActivityDefinition activity, int amount) {
        return ModifyCost(activity, amount, GetToolDurabilityCostMultiplier, condition => condition.ToolDurabilityCostMultiplier, (companion, targetActivity) => companion.GetToolDurabilityCostMultiplier(targetActivity));
    }

    public static int ModifyNeedCost(ActivityDefinition activity, int amount) {
        return ModifyCost(activity, amount, GetNeedCostMultiplier, condition => condition.NeedCostMultiplier, (companion, targetActivity) => companion.GetNeedCostMultiplier(targetActivity));
    }

    public static float ModifyEncounterRateMultiplier(float multiplier, PlayerController player = null) {
        float result = Mathf.Max(0f, multiplier);
        var activePlayer = player != null ? player : PlayerController.i;
        var log = activePlayer != null ? activePlayer.GetComponent<PlayerWorldConditionLog>() : null;
        if(log == null) {
            return result;
        }

        foreach(var state in log.GetActiveConditionStates(null, CurrentZone)) {
            var condition = state.ResolveDefinition();
            if(condition != null) {
                result *= state.ScaleMultiplier(condition.EncounterRateMultiplier);
            }
        }

        return Mathf.Max(0f, result);
    }

    static int ModifyCost(
        ActivityDefinition activity,
        int amount,
        System.Func<ActivityZoneModifierDefinition, float> getZoneMultiplier,
        System.Func<WorldConditionDefinition, float> getConditionMultiplier,
        System.Func<CompanionController, ActivityDefinition, float> getCompanionMultiplier
    ) {
        if(amount <= 0) {
            return 0;
        }

        float multiplier = 1f;
        foreach(var modifier in GetActiveModifiers(activity)) {
            multiplier *= getZoneMultiplier(modifier);
        }

        foreach(var state in GetActiveWorldConditionStates(activity)) {
            var condition = state.ResolveDefinition();
            if(condition != null) {
                multiplier *= state.ScaleMultiplier(getConditionMultiplier(condition));
            }
        }

        foreach(var companion in GetActiveCompanions()) {
            multiplier *= getCompanionMultiplier(companion, activity);
        }

        return Mathf.Max(0, Mathf.CeilToInt(amount * multiplier));
    }

    static IEnumerable<CompanionController> GetActiveCompanions() {
        return CompanionController.GetFollowingCompanions(PlayerController.i);
    }

    static IEnumerable<PlayerWorldConditionState> GetActiveWorldConditionStates(ActivityDefinition activity) {
        var log = PlayerController.i != null ? PlayerController.i.GetComponent<PlayerWorldConditionLog>() : null;
        return log != null
            ? log.GetActiveConditionStates(activity, CurrentZone)
            : Enumerable.Empty<PlayerWorldConditionState>();
    }

    static float GetItemCostMultiplier(ActivityZoneModifierDefinition modifier) {
        return modifier.ItemCostMultiplier;
    }

    static float GetToolDurabilityCostMultiplier(ActivityZoneModifierDefinition modifier) {
        return modifier.ToolDurabilityCostMultiplier;
    }

    static float GetNeedCostMultiplier(ActivityZoneModifierDefinition modifier) {
        return modifier.NeedCostMultiplier;
    }

    static IEnumerable<ActivityZoneModifierDefinition> GetActiveModifiers(ActivityDefinition activity) {
        foreach(var zone in activeZones) {
            if(zone == null || zone.Modifiers == null) {
                continue;
            }

            foreach(var modifier in zone.Modifiers) {
                if(modifier != null && modifier.Affects(activity)) {
                    yield return modifier;
                }
            }
        }
    }

    static void PublishZoneEvent(ActivityZoneDefinition zone, bool active) {
        GameEventPublishing.PublishOptional(
            active ? zone.EnteredEvent : zone.ExitedEvent,
            active ? $"activity-zone.entered.{zone.Id}" : $"activity-zone.exited.{zone.Id}",
            active ? $"Entered {zone.DisplayName}." : $"Exited {zone.DisplayName}.",
            GameEventCategory.Activity,
            GameEventImportance.Trace,
            null,
            "PlayerActivityContext",
            GameEventScope.Player,
            showInFeed: zone.ShowZoneEventsInFeed,
            writeToDebugLog: zone.WriteZoneEventsToDebugLog,
            GameEventPublishing.Value("zoneId", zone.Id),
            GameEventPublishing.Value("zoneName", zone.DisplayName),
            GameEventPublishing.Value("zoneType", zone.ZoneType),
            GameEventPublishing.Value("active", active));
    }
}
