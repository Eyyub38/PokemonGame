using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Pokemon Care/Care Facility Definition")]
public class PokemonCareFacilityDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this care facility. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing explanation for this care facility.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Free-form tags used by requirements, validators and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Access")]
    [Tooltip("Optional activity used to gate admission costs, area rules and requirements.")]
    [SerializeField] ActivityDefinition admissionActivity;
    [Tooltip("Optional activity used to gate release costs, area rules and requirements.")]
    [SerializeField] ActivityDefinition releaseActivity;

    [Header("Capacity")]
    [Tooltip("Maximum number of Pokemon that can stay in one facility instance at the same time.")]
    [Min(1)]
    [SerializeField] int capacity = 1;
    [Tooltip("If enabled, fainted Pokemon can be admitted.")]
    [SerializeField] bool allowFaintedPokemon;
    [Tooltip("If enabled, the same Pokemon can have multiple active stays in this facility type.")]
    [SerializeField] bool allowDuplicatePokemon;

    [Header("Care Timing")]
    [Tooltip("Default in-game hours between timed care applications when a rule does not override the interval.")]
    [Min(0)]
    [SerializeField] int defaultCareIntervalHours = 12;
    [Tooltip("Minimum in-game hours before the Pokemon can be released. 0 means no minimum.")]
    [Min(0)]
    [SerializeField] int minimumStayHours;
    [Tooltip("Extra bonus added to care actions applied by this facility.")]
    [SerializeField] int careBonus;
    [Tooltip("Rules that decide which care actions the facility can apply and when.")]
    [SerializeField] List<PokemonCareFacilityCareRule> careRules = new List<PokemonCareFacilityCareRule>();

    [Header("Events")]
    [Tooltip("Optional event published when a Pokemon is admitted. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition admittedEvent;
    [Tooltip("Optional event published when facility care is processed. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition careProcessedEvent;
    [Tooltip("Optional event published when a Pokemon is released. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition releasedEvent;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : System.Array.Empty<string>();
    public ActivityDefinition AdmissionActivity => admissionActivity;
    public ActivityDefinition ReleaseActivity => releaseActivity;
    public int Capacity => Mathf.Max(1, capacity);
    public bool AllowFaintedPokemon => allowFaintedPokemon;
    public bool AllowDuplicatePokemon => allowDuplicatePokemon;
    public int DefaultCareIntervalHours => Mathf.Max(0, defaultCareIntervalHours);
    public int MinimumStayHours => Mathf.Max(0, minimumStayHours);
    public int CareBonus => careBonus;
    public IReadOnlyList<PokemonCareFacilityCareRule> CareRules => careRules != null ? (IReadOnlyList<PokemonCareFacilityCareRule>)careRules : System.Array.Empty<PokemonCareFacilityCareRule>();
    public GameEventDefinition AdmittedEvent => admittedEvent;
    public GameEventDefinition CareProcessedEvent => careProcessedEvent;
    public GameEventDefinition ReleasedEvent => releasedEvent;

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, System.StringComparison.OrdinalIgnoreCase));
    }

    public bool CanAdmit(PlayerController player, Pokemon pokemon, PlayerPokemonCareFacilityLog log, string facilityInstanceId, out string failureMessage) {
        if(pokemon == null) {
            failureMessage = "No Pokemon selected.";
            return false;
        }

        if(!allowFaintedPokemon && pokemon.HP <= 0) {
            failureMessage = $"{pokemon.NickName} cannot enter {DisplayName} right now.";
            return false;
        }

        if(log == null) {
            failureMessage = "Pokemon care facility log is missing.";
            return false;
        }

        if(log.GetActiveStayCount(this, facilityInstanceId) >= Capacity) {
            failureMessage = $"{DisplayName} has no free care slot.";
            return false;
        }

        if(!allowDuplicatePokemon && log.HasActiveStay(pokemon, this)) {
            failureMessage = $"{pokemon.NickName} is already staying in {DisplayName}.";
            return false;
        }

        if(admissionActivity != null && !admissionActivity.CanPerform(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool CanRelease(PlayerController player, PlayerPokemonCareFacilityStay stay, out string failureMessage) {
        if(stay == null) {
            failureMessage = "No Pokemon is staying here.";
            return false;
        }

        int stayedHours = Mathf.Max(0, GetCurrentAbsoluteHour() - stay.enteredAbsoluteHour);
        if(stayedHours < MinimumStayHours) {
            failureMessage = $"{stay.pokemonName} needs {MinimumStayHours - stayedHours} more hour(s) before leaving {DisplayName}.";
            return false;
        }

        if(releaseActivity != null && !releaseActivity.CanPerform(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public int ProcessDueCare(Pokemon pokemon, PlayerPokemonCareFacilityStay stay, string sourceId) {
        if(pokemon == null || stay == null) {
            return 0;
        }

        int appliedCount = 0;
        int currentHour = GetCurrentAbsoluteHour();
        foreach(var rule in CareRules) {
            if(rule != null && rule.TryApplyTimedCare(pokemon, stay, this, sourceId, currentHour)) {
                appliedCount++;
            }
        }

        if(appliedCount > 0) {
            stay.lastProcessedAbsoluteHour = currentHour;
        }

        return appliedCount;
    }

    public int ApplyAdmissionCare(Pokemon pokemon, PlayerPokemonCareFacilityStay stay, string sourceId) {
        return ApplyPhaseCare(pokemon, stay, sourceId, PokemonCareFacilityCarePhase.Admission);
    }

    public int ApplyReleaseCare(Pokemon pokemon, PlayerPokemonCareFacilityStay stay, string sourceId) {
        return ApplyPhaseCare(pokemon, stay, sourceId, PokemonCareFacilityCarePhase.Release);
    }

    int ApplyPhaseCare(Pokemon pokemon, PlayerPokemonCareFacilityStay stay, string sourceId, PokemonCareFacilityCarePhase phase) {
        if(pokemon == null || stay == null) {
            return 0;
        }

        int appliedCount = 0;
        int currentHour = GetCurrentAbsoluteHour();
        foreach(var rule in CareRules) {
            if(rule != null && rule.TryApplyPhaseCare(pokemon, stay, this, sourceId, phase, currentHour)) {
                appliedCount++;
            }
        }

        return appliedCount;
    }

    public void PublishAdmitted(PlayerController player, Pokemon pokemon, string facilityInstanceId) {
        PublishFacilityEvent(admittedEvent, "admitted", player, pokemon, facilityInstanceId, 0);
    }

    public void PublishCareProcessed(PlayerController player, Pokemon pokemon, string facilityInstanceId, int appliedCount) {
        PublishFacilityEvent(careProcessedEvent, "care-processed", player, pokemon, facilityInstanceId, appliedCount);
    }

    public void PublishReleased(PlayerController player, Pokemon pokemon, string facilityInstanceId, int appliedCount) {
        PublishFacilityEvent(releasedEvent, "released", player, pokemon, facilityInstanceId, appliedCount);
    }

    void PublishFacilityEvent(GameEventDefinition eventDefinition, string phase, PlayerController player, Pokemon pokemon, string facilityInstanceId, int appliedCount) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"pokemon-care.facility.{phase}.{Id}.{pokemon?.InstanceId}",
            $"{pokemon?.NickName ?? "Pokemon"} {phase} at {DisplayName}.",
            GameEventCategory.PokemonCare,
            GameEventImportance.Info,
            player,
            "PokemonCareFacilityDefinition",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("facilityId", Id),
            GameEventPublishing.Value("facilityName", DisplayName),
            GameEventPublishing.Value("facilityInstanceId", facilityInstanceId),
            GameEventPublishing.Value("pokemonId", pokemon?.InstanceId),
            GameEventPublishing.Value("pokemonName", pokemon?.NickName),
            GameEventPublishing.Value("appliedCareActions", appliedCount));
    }

    int GetCurrentAbsoluteHour() {
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }
}

public enum PokemonCareFacilityCarePhase {
    Admission,
    TimedTick,
    Release
}

[System.Serializable]
public class PokemonCareFacilityCareRule {
    [Tooltip("Care action applied by this facility rule.")]
    public PokemonCareActionDefinition careAction;
    [Tooltip("If enabled, this rule runs when the Pokemon is admitted.")]
    public bool applyOnAdmission;
    [Tooltip("If enabled, this rule can run when timed care is processed.")]
    public bool applyOnTimedTick = true;
    [Tooltip("If enabled, this rule runs when the Pokemon is released.")]
    public bool applyOnRelease;
    [Tooltip("Hours between timed applications for this rule. 0 uses the facility default interval.")]
    [Min(0)]
    public int intervalHours;
    [Tooltip("Maximum times this rule can apply during one stay. 0 means unlimited.")]
    [Min(0)]
    public int maxUsesPerStay;
    [Tooltip("Extra bonus added on top of the facility bonus for this rule.")]
    public int careBonus;
    [Tooltip("If enabled, PokemonCareActionDefinition.CanApply must pass before this rule can run.")]
    public bool requireActionEligibility = true;

    public bool TryApplyPhaseCare(Pokemon pokemon, PlayerPokemonCareFacilityStay stay, PokemonCareFacilityDefinition facility, string sourceId, PokemonCareFacilityCarePhase phase, int currentAbsoluteHour) {
        if(phase == PokemonCareFacilityCarePhase.Admission && !applyOnAdmission) {
            return false;
        }

        if(phase == PokemonCareFacilityCarePhase.Release && !applyOnRelease) {
            return false;
        }

        return TryApply(pokemon, stay, facility, sourceId, currentAbsoluteHour);
    }

    public bool TryApplyTimedCare(Pokemon pokemon, PlayerPokemonCareFacilityStay stay, PokemonCareFacilityDefinition facility, string sourceId, int currentAbsoluteHour) {
        if(!applyOnTimedTick || !IsDue(stay, facility, currentAbsoluteHour)) {
            return false;
        }

        return TryApply(pokemon, stay, facility, sourceId, currentAbsoluteHour);
    }

    bool IsDue(PlayerPokemonCareFacilityStay stay, PokemonCareFacilityDefinition facility, int currentAbsoluteHour) {
        int interval = intervalHours > 0 ? intervalHours : facility.DefaultCareIntervalHours;
        if(interval <= 0) {
            return true;
        }

        var useState = stay.GetActionUse(careAction);
        int baselineHour = useState != null && useState.lastUsedAbsoluteHour >= 0
            ? useState.lastUsedAbsoluteHour
            : stay.enteredAbsoluteHour;
        return currentAbsoluteHour - baselineHour >= interval;
    }

    bool TryApply(Pokemon pokemon, PlayerPokemonCareFacilityStay stay, PokemonCareFacilityDefinition facility, string sourceId, int currentAbsoluteHour) {
        if(careAction == null || pokemon == null || stay == null || facility == null) {
            return false;
        }

        var useState = stay.GetOrCreateActionUse(careAction);
        if(maxUsesPerStay > 0 && useState.uses >= maxUsesPerStay) {
            return false;
        }

        if(requireActionEligibility && !careAction.CanApply(pokemon, out _)) {
            return false;
        }

        if(!careAction.TryApply(pokemon, facility.CareBonus + careBonus, sourceId, out _)) {
            return false;
        }

        useState.uses++;
        useState.lastUsedAbsoluteHour = currentAbsoluteHour;
        stay.totalCareActionsApplied++;
        return true;
    }
}
