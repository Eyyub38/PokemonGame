using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum EncounterSourceOutcomeMode {
    StartBattle,
    TryStealthCapture,
    TryStealthCaptureOnly,
    RecordSeenOnly
}

public enum EncounterSourceAttemptStatus {
    Blocked,
    NoEncounter,
    EncounterRolled,
    StartedBattle,
    Captured,
    SeenOnly
}

[CreateAssetMenu(menuName = "Encounters/Encounter Source Profile")]
public class EncounterSourceProfileDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this encounter source profile. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining where and how this encounter source should be used.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as grass, route, tree, cave, rare, event or stealth.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Encounter")]
    [Tooltip("Encounter table rolled by this source. The table decides which Pokemon can appear.")]
    [SerializeField] EncounterTableDefinition encounterTable = null;
    [Tooltip("Optional source override. Any uses the encounter table source type.")]
    [SerializeField] EncounterSourceType sourceOverride = EncounterSourceType.Any;
    [Tooltip("Multiplier applied to the encounter table's base chance before world-condition modifiers.")]
    [Min(0f)]
    [SerializeField] float chanceMultiplier = 1f;
    [Tooltip("If enabled, active world conditions can multiply this source's encounter chance.")]
    [SerializeField] bool applyWorldConditionEncounterRate = true;
    [Tooltip("Fallback battle trigger used when the encounter table is missing or does not provide a suitable battle trigger.")]
    [SerializeField] BattleTrigger fallbackBattleTrigger = BattleTrigger.LongGrass;

    [Header("Area Gate")]
    [Tooltip("If enabled, the source only works when the player is inside an active activity zone of Required Zone Type.")]
    [SerializeField] bool requireActiveZoneType = false;
    [Tooltip("Required active activity zone type when Require Active Zone Type is enabled.")]
    [SerializeField] ActivityZoneType requiredZoneType = ActivityZoneType.Wild;
    [Tooltip("Optional active activity zone tag required by this source. Empty means no tag requirement.")]
    [SerializeField] string requiredActiveZoneTag = string.Empty;
    [Tooltip("Extra modular requirements that must pass before this source can roll.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();

    [Header("Outcome")]
    [Tooltip("What happens after the encounter table successfully rolls a Pokemon.")]
    [SerializeField] EncounterSourceOutcomeMode outcomeMode = EncounterSourceOutcomeMode.StartBattle;
    [Tooltip("Optional stealth capture profile used by stealth outcome modes or interact overrides.")]
    [SerializeField] StealthCaptureProfileDefinition stealthCaptureProfile = null;
    [Tooltip("If enabled, player movement animation is stopped before starting a battle or stealth attempt.")]
    [SerializeField] bool stopPlayerMovement = true;

    [Header("Messages")]
    [Tooltip("Optional text shown by EncounterSource before it rolls this profile through Interact.")]
    [TextArea]
    [SerializeField] string interactionText = string.Empty;
    [Tooltip("Message returned when the profile is blocked by player, zone or requirement checks.")]
    [TextArea]
    [SerializeField] string blockedMessage = "This encounter is unavailable right now.";
    [Tooltip("Message returned when the table does not roll an encounter.")]
    [TextArea]
    [SerializeField] string noEncounterMessage = "Nothing appeared.";
    [Tooltip("Message returned by Record Seen Only outcome. {pokemon} is replaced with the Pokemon name.")]
    [TextArea]
    [SerializeField] string seenOnlyMessage = "{pokemon} was spotted.";
    [Tooltip("Message returned after a battle encounter is started. {pokemon} is replaced with the Pokemon name.")]
    [TextArea]
    [SerializeField] string battleStartedMessage = "A wild {pokemon} appeared.";

    [Header("Events")]
    [Tooltip("Optional event published when this source is blocked.")]
    [SerializeField] GameEventDefinition blockedEvent = null;
    [Tooltip("Optional event published when this source resolves with a Pokemon, capture, battle or seen-only outcome.")]
    [SerializeField] GameEventDefinition resolvedEvent = null;
    [Tooltip("If enabled, source-level events can appear in the notification feed.")]
    [SerializeField] bool showSourceEventsInFeed = false;
    [Tooltip("If enabled, source-level events are written to the debug log.")]
    [SerializeField] bool writeSourceEventsToDebugLog = false;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public EncounterTableDefinition EncounterTable => encounterTable;
    public EncounterSourceType SourceOverride => sourceOverride;
    public float ChanceMultiplier => Mathf.Max(0f, chanceMultiplier);
    public bool ApplyWorldConditionEncounterRate => applyWorldConditionEncounterRate;
    public BattleTrigger FallbackBattleTrigger => fallbackBattleTrigger;
    public bool RequireActiveZoneType => requireActiveZoneType;
    public ActivityZoneType RequiredZoneType => requiredZoneType;
    public string RequiredActiveZoneTag => requiredActiveZoneTag;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public EncounterSourceOutcomeMode OutcomeMode => outcomeMode;
    public StealthCaptureProfileDefinition StealthCaptureProfile => stealthCaptureProfile;
    public bool StopPlayerMovement => stopPlayerMovement;
    public string InteractionText => interactionText;
    public string BlockedMessage => string.IsNullOrWhiteSpace(blockedMessage) ? "This encounter is unavailable right now." : blockedMessage;
    public string NoEncounterMessage => string.IsNullOrWhiteSpace(noEncounterMessage) ? "Nothing appeared." : noEncounterMessage;
    public string SeenOnlyMessage => seenOnlyMessage;
    public string BattleStartedMessage => battleStartedMessage;
    public bool ShowSourceEventsInFeed => showSourceEventsInFeed;
    public bool WriteSourceEventsToDebugLog => writeSourceEventsToDebugLog;

    public EncounterSourceType ResolveSourceType() {
        if(sourceOverride != EncounterSourceType.Any) {
            return sourceOverride;
        }

        return encounterTable != null ? encounterTable.SourceType : EncounterSourceType.Special;
    }

    public BattleTrigger ResolveBattleTrigger() {
        return encounterTable != null ? encounterTable.BattleTrigger : fallbackBattleTrigger;
    }

    public bool CanAttempt(PlayerController player, out string failureMessage) {
        if(player == null) {
            failureMessage = "No player was provided for this encounter source.";
            return false;
        }

        if(encounterTable == null) {
            failureMessage = "No encounter table is assigned.";
            return false;
        }

        if(requireActiveZoneType && !PlayerActivityContext.HasActiveZoneType(requiredZoneType)) {
            failureMessage = BlockedMessage;
            return false;
        }

        if(!string.IsNullOrWhiteSpace(requiredActiveZoneTag) && !PlayerActivityContext.HasActiveTag(requiredActiveZoneTag)) {
            failureMessage = BlockedMessage;
            return false;
        }

        if(requirements != null) {
            foreach(var requirement in requirements) {
                if(requirement == null) {
                    continue;
                }

                if(!requirement.IsMet(player)) {
                    failureMessage = string.IsNullOrWhiteSpace(requirement.FailureMessage) ? BlockedMessage : requirement.FailureMessage;
                    return false;
                }
            }
        }

        failureMessage = null;
        return true;
    }

    public EncounterSourceAttemptResult Execute(PlayerController player, UnityEngine.Object context, bool preferStealthCapture = false) {
        var result = EncounterSourceAttemptResult.Create(this);
        result.sourceType = ResolveSourceType();

        if(!CanAttempt(player, out string failureMessage)) {
            result.status = EncounterSourceAttemptStatus.Blocked;
            result.startResult = EncounterStartResult.Blocked;
            result.message = string.IsNullOrWhiteSpace(failureMessage) ? BlockedMessage : failureMessage;
            PublishSourceEvent(blockedEvent, "blocked", result, player, context, GameEventImportance.Warning);
            return result;
        }

        if(!EncounterSystem.TryRoll(
            player,
            encounterTable,
            result.sourceType,
            ChanceMultiplier,
            context,
            out var pokemon,
            out var entry,
            applyWorldConditionEncounterRate)) {
            result.status = EncounterSourceAttemptStatus.NoEncounter;
            result.startResult = EncounterStartResult.NoEncounter;
            result.message = NoEncounterMessage;
            return result;
        }

        result.status = EncounterSourceAttemptStatus.EncounterRolled;
        result.pokemon = pokemon;
        result.selectedEntry = entry;

        if(stopPlayerMovement && player.Character != null && player.Character.Animator != null) {
            player.Character.Animator.IsMoving = false;
        }

        var mode = preferStealthCapture && stealthCaptureProfile != null
            ? EncounterSourceOutcomeMode.TryStealthCapture
            : outcomeMode;

        switch(mode) {
            case EncounterSourceOutcomeMode.RecordSeenOnly:
                RecordSeenOnly(player, result);
                break;
            case EncounterSourceOutcomeMode.TryStealthCapture:
                TryStealthCapture(player, context, result, allowBattleOnFailure: true);
                break;
            case EncounterSourceOutcomeMode.TryStealthCaptureOnly:
                TryStealthCapture(player, context, result, allowBattleOnFailure: false);
                break;
            default:
                StartBattle(player, context, result);
                break;
        }

        PublishSourceEvent(resolvedEvent, "resolved", result, player, context, GameEventImportance.Info);
        return result;
    }

    void RecordSeenOnly(PlayerController player, EncounterSourceAttemptResult result) {
        player.GetComponent<PlayerEncounterLog>()?.RecordSeen(result.pokemon, result.sourceType, encounterTable);
        result.status = EncounterSourceAttemptStatus.SeenOnly;
        result.startResult = EncounterStartResult.NoEncounter;
        result.message = FormatPokemonMessage(SeenOnlyMessage, result.pokemon);
    }

    void TryStealthCapture(PlayerController player, UnityEngine.Object context, EncounterSourceAttemptResult result, bool allowBattleOnFailure) {
        if(stealthCaptureProfile == null) {
            if(!allowBattleOnFailure) {
                result.startResult = EncounterStartResult.Blocked;
                result.status = EncounterSourceAttemptStatus.Blocked;
                result.message = "No stealth capture profile is assigned.";
                return;
            }

            StartBattle(player, context, result);
            return;
        }

        result.startResult = EncounterSystem.TryStealthCapture(
            player,
            result.pokemon,
            result.sourceType,
            encounterTable,
            ResolveBattleTrigger(),
            stealthCaptureProfile,
            context,
            out var captureResult,
            allowBattleOnFailure);
        result.captureResult = captureResult;
        result.message = captureResult != null && !string.IsNullOrWhiteSpace(captureResult.message)
            ? captureResult.message
            : FormatPokemonMessage(BattleStartedMessage, result.pokemon);
        result.status = MapStartResult(result.startResult);
    }

    void StartBattle(PlayerController player, UnityEngine.Object context, EncounterSourceAttemptResult result) {
        result.startResult = EncounterSystem.StartBattle(player, result.pokemon, result.sourceType, encounterTable, ResolveBattleTrigger(), context);
        result.status = MapStartResult(result.startResult);
        result.message = FormatPokemonMessage(BattleStartedMessage, result.pokemon);
    }

    EncounterSourceAttemptStatus MapStartResult(EncounterStartResult startResult) {
        return startResult switch {
            EncounterStartResult.StartedBattle => EncounterSourceAttemptStatus.StartedBattle,
            EncounterStartResult.Captured => EncounterSourceAttemptStatus.Captured,
            EncounterStartResult.Blocked => EncounterSourceAttemptStatus.Blocked,
            _ => EncounterSourceAttemptStatus.NoEncounter
        };
    }

    void PublishSourceEvent(
        GameEventDefinition definition,
        string phase,
        EncounterSourceAttemptResult result,
        PlayerController player,
        UnityEngine.Object context,
        GameEventImportance importance
    ) {
        GameEventPublishing.PublishOptional(
            definition,
            $"encounter-source.{phase}.{Id}",
            result != null && !string.IsNullOrWhiteSpace(result.message) ? result.message : DisplayName,
            GameEventCategory.Encounter,
            importance,
            context != null ? context : player,
            "EncounterSourceProfile",
            GameEventScope.Scene,
            showInFeed: showSourceEventsInFeed,
            writeToDebugLog: writeSourceEventsToDebugLog,
            GameEventPublishing.Value("profileId", Id),
            GameEventPublishing.Value("profileName", DisplayName),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceType", result != null ? result.sourceType : ResolveSourceType()),
            GameEventPublishing.Value("status", result != null ? result.status : EncounterSourceAttemptStatus.Blocked),
            GameEventPublishing.Value("pokemon", result != null && result.pokemon != null && result.pokemon.Base != null ? result.pokemon.Base.Name : string.Empty));
    }

    string FormatPokemonMessage(string template, Pokemon pokemon) {
        string pokemonName = pokemon != null && pokemon.Base != null ? pokemon.Base.Name : "Pokemon";
        return string.IsNullOrWhiteSpace(template) ? pokemonName : template.Replace("{pokemon}", pokemonName);
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }
}

public class EncounterSource : MonoBehaviour, Interactable, IPlayerTriggerable {
    [Header("Profile")]
    [Tooltip("Data profile that defines this source's encounter table, gates, outcome and messages.")]
    [SerializeField] EncounterSourceProfileDefinition profile = null;

    [Header("Activation")]
    [Tooltip("If enabled, touching this object can execute the encounter source.")]
    [SerializeField] bool triggerOnTouch = true;
    [Tooltip("If enabled, interacting with this object can execute the encounter source.")]
    [SerializeField] bool interactOnUse = true;
    [Tooltip("If enabled, this trigger can fire repeatedly while the player remains in or revisits it.")]
    [SerializeField] bool triggerRepeatedly = true;
    [Tooltip("Seconds of real time before this source can execute again.")]
    [Min(0f)]
    [SerializeField] float realTimeCooldownSeconds = 0.25f;
    [Tooltip("If enabled, interaction requests stealth capture when the profile has a stealth capture profile.")]
    [SerializeField] bool preferStealthOnInteract = true;

    [Header("Messages")]
    [Tooltip("If enabled, interaction text from the profile is shown before an Interact roll.")]
    [SerializeField] bool showInteractionText = true;
    [Tooltip("If enabled, blocked, no-encounter and capture result messages are shown after Interact.")]
    [SerializeField] bool showResultMessages = true;

    [Header("Lifecycle")]
    [Tooltip("If enabled, this GameObject is disabled after a successful stealth capture.")]
    [SerializeField] bool disableAfterCapture = false;
    [Tooltip("If enabled, this GameObject is disabled after a battle starts.")]
    [SerializeField] bool disableAfterBattleStarted = false;

    float lastAttemptTime = -999f;

    public EncounterSourceProfileDefinition Profile => profile;
    public bool TriggerOnTouch => triggerOnTouch;
    public bool InteractOnUse => interactOnUse;
    public bool TriggerRepeatedly => triggerRepeatedly;
    public float RealTimeCooldownSeconds => Mathf.Max(0f, realTimeCooldownSeconds);
    public bool PreferStealthOnInteract => preferStealthOnInteract;

    public void OnPlayerTriggered(PlayerController player) {
        if(!triggerOnTouch) {
            return;
        }

        Execute(player, preferStealthCapture: false);
    }

    public IEnumerator Interact(Transform initiator) {
        if(!interactOnUse) {
            yield break;
        }

        var player = initiator != null ? initiator.GetComponent<PlayerController>() : null;
        if(showInteractionText && profile != null && !string.IsNullOrWhiteSpace(profile.InteractionText) && DialogManager.i != null) {
            yield return DialogManager.i.ShowDialogText(profile.InteractionText);
        }

        var result = Execute(player, preferStealthOnInteract);
        if(showResultMessages && result != null && !string.IsNullOrWhiteSpace(result.message) && DialogManager.i != null) {
            yield return DialogManager.i.ShowDialogText(result.message);
        }
    }

    public EncounterSourceAttemptResult Execute(PlayerController player, bool preferStealthCapture = false) {
        if(Time.time < lastAttemptTime + RealTimeCooldownSeconds) {
            return EncounterSourceAttemptResult.Blocked(profile, "Encounter source is cooling down.");
        }

        lastAttemptTime = Time.time;
        var result = profile != null
            ? profile.Execute(player, this, preferStealthCapture)
            : EncounterSourceAttemptResult.Blocked(null, "Encounter source has no profile.");

        if(result != null) {
            if(disableAfterCapture && result.status == EncounterSourceAttemptStatus.Captured) {
                gameObject.SetActive(false);
            } else if(disableAfterBattleStarted && result.status == EncounterSourceAttemptStatus.StartedBattle) {
                gameObject.SetActive(false);
            }
        }

        return result;
    }
}

public class EncounterSourceAttemptResult {
    [Tooltip("Encounter source profile id that produced this result.")]
    public string profileId;
    [Tooltip("Encounter source profile display name.")]
    public string profileName;
    [Tooltip("Source type used by this attempt.")]
    public EncounterSourceType sourceType;
    [Tooltip("High-level status of this encounter source attempt.")]
    public EncounterSourceAttemptStatus status;
    [Tooltip("Battle/capture start result produced by EncounterSystem.")]
    public EncounterStartResult startResult;
    [Tooltip("Pokemon produced by the encounter table, when any.")]
    public Pokemon pokemon;
    [Tooltip("Encounter table entry that produced the Pokemon, when any.")]
    public EncounterTableEntry selectedEntry;
    [Tooltip("Stealth capture result, when a stealth profile was used.")]
    public EncounterCaptureResult captureResult;
    [Tooltip("Human-readable result message for UI/debug usage.")]
    public string message;

    public static EncounterSourceAttemptResult Create(EncounterSourceProfileDefinition profile) {
        return new EncounterSourceAttemptResult {
            profileId = profile != null ? profile.Id : string.Empty,
            profileName = profile != null ? profile.DisplayName : string.Empty,
            sourceType = profile != null ? profile.ResolveSourceType() : EncounterSourceType.Special,
            status = EncounterSourceAttemptStatus.NoEncounter,
            startResult = EncounterStartResult.NoEncounter
        };
    }

    public static EncounterSourceAttemptResult Blocked(EncounterSourceProfileDefinition profile, string message) {
        var result = Create(profile);
        result.status = EncounterSourceAttemptStatus.Blocked;
        result.startResult = EncounterStartResult.Blocked;
        result.message = message;
        return result;
    }
}
