using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ConditionalSceneObjectAvailabilityMode {
    RequirementsOnly,
    SceneObjectStateOnly,
    SceneObjectStateAndRequirements,
    SceneObjectStateOrRequirements
}

public class ConditionalSceneObject : MonoBehaviour, IPlayerTriggerable, Interactable {
    [Header("Identity")]
    [Tooltip("Optional logical scene object state read from PlayerSceneObjectLog.")]
    [SerializeField] SceneObjectDefinition sceneObject = null;
    [Tooltip("Stable source id used by logs and consequence chains. Empty uses GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName = string.Empty;

    [Header("Availability")]
    [Tooltip("How this component combines saved scene object state with live requirements.")]
    [SerializeField] ConditionalSceneObjectAvailabilityMode availabilityMode = ConditionalSceneObjectAvailabilityMode.SceneObjectStateAndRequirements;
    [Tooltip("If enabled, final availability is inverted.")]
    [SerializeField] bool invertAvailability;
    [Tooltip("Optional access profile checked as part of live requirements.")]
    [SerializeField] AccessProfileDefinition accessProfile = null;
    [Tooltip("How requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Optional reusable requirements checked as part of live availability.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message/debug reason used when this object blocks interaction.")]
    [SerializeField] string blockedMessage = "This is not available right now.";

    [Header("Evaluation")]
    [Tooltip("Optional player override. Empty uses PlayerController.i or first loaded player.")]
    [SerializeField] PlayerController playerOverride = null;
    [Tooltip("If enabled, availability is evaluated during Start.")]
    [SerializeField] bool evaluateOnStart = true;
    [Tooltip("If enabled, availability is evaluated during OnEnable.")]
    [SerializeField] bool evaluateOnEnable = true;
    [Tooltip("If enabled, availability refreshes whenever GameEventBus publishes an event.")]
    [SerializeField] bool refreshOnGameEvents = true;
    [Tooltip("If enabled, GameEventBus history is replayed when this component enables.")]
    [SerializeField] bool replayEventHistoryOnEnable;
    [Tooltip("If enabled, availability refreshes when TimeSystem time changes.")]
    [SerializeField] bool refreshOnTimeChanged = true;
    [Tooltip("If enabled, availability refreshes when TimeSystem day changes.")]
    [SerializeField] bool refreshOnDayChanged = true;
    [Tooltip("If enabled, availability chains can run on the first evaluation. If disabled, they only run after a state change.")]
    [SerializeField] bool runAvailabilityChainsOnInitialEvaluation;

    [Header("Targets")]
    [Tooltip("If enabled, target GameObjects are set active/inactive. Avoid using this on the same GameObject when it must later re-enable itself.")]
    [SerializeField] bool setTargetGameObjectsActive;
    [Tooltip("GameObjects toggled when Set Target GameObjects Active is enabled. Empty uses this GameObject.")]
    [SerializeField] List<GameObject> targetGameObjects = new List<GameObject>();
    [Tooltip("If enabled, renderers are enabled/disabled based on availability.")]
    [SerializeField] bool toggleRenderers = true;
    [Tooltip("Renderers toggled by this component. Empty uses child renderers.")]
    [SerializeField] List<Renderer> targetRenderers = new List<Renderer>();
    [Tooltip("If enabled, 2D colliders are enabled/disabled based on availability.")]
    [SerializeField] bool toggleColliders = true;
    [Tooltip("Colliders toggled by this component. Empty uses child colliders.")]
    [SerializeField] List<Collider2D> targetColliders = new List<Collider2D>();
    [Tooltip("If enabled, selected behaviours are enabled/disabled based on availability.")]
    [SerializeField] bool toggleBehaviours;
    [Tooltip("Behaviours toggled by this component. The ConditionalSceneObject itself is never disabled by this list.")]
    [SerializeField] List<Behaviour> targetBehaviours = new List<Behaviour>();

    [Header("Interaction Forwarding")]
    [Tooltip("If enabled, repeated player triggers can call this object more than once.")]
    [SerializeField] bool triggerRepeatedly = true;
    [Tooltip("Optional MonoBehaviour that implements IPlayerTriggerable. Called when this object is available and player enters trigger.")]
    [SerializeField] MonoBehaviour triggerForwardTarget = null;
    [Tooltip("Optional MonoBehaviour that implements Interactable. Called when this object is available and player interacts.")]
    [SerializeField] MonoBehaviour interactForwardTarget = null;
    [Tooltip("If enabled, scene object interaction history is recorded when trigger succeeds.")]
    [SerializeField] bool recordInteractionOnTrigger;
    [Tooltip("If enabled, scene object interaction history is recorded when manual interaction succeeds.")]
    [SerializeField] bool recordInteractionOnInteract = true;

    [Header("Consequences")]
    [Tooltip("Consequence chains applied when this object becomes available.")]
    [SerializeField] List<ConsequenceChainDefinition> becameAvailableChains = new List<ConsequenceChainDefinition>();
    [Tooltip("Consequence chains applied when this object becomes unavailable.")]
    [SerializeField] List<ConsequenceChainDefinition> becameUnavailableChains = new List<ConsequenceChainDefinition>();
    [Tooltip("Consequence chains applied when player successfully triggers/interacts with this object.")]
    [SerializeField] List<ConsequenceChainDefinition> successfulInteractionChains = new List<ConsequenceChainDefinition>();
    [Tooltip("Consequence chains applied when player tries to use this object while unavailable.")]
    [SerializeField] List<ConsequenceChainDefinition> blockedInteractionChains = new List<ConsequenceChainDefinition>();

    [Header("Debug")]
    [Tooltip("If enabled, availability and interaction attempts are written to GameEventBus/GameDebugLogger.")]
    [SerializeField] bool logAttempts;

    bool? lastAvailability;
    bool timeSubscribed;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public SceneObjectDefinition SceneObject => sceneObject;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements;
    public IReadOnlyList<ConsequenceChainDefinition> BecameAvailableChains => becameAvailableChains;
    public IReadOnlyList<ConsequenceChainDefinition> BecameUnavailableChains => becameUnavailableChains;
    public IReadOnlyList<ConsequenceChainDefinition> SuccessfulInteractionChains => successfulInteractionChains;
    public IReadOnlyList<ConsequenceChainDefinition> BlockedInteractionChains => blockedInteractionChains;
    public bool TriggerRepeatedly => triggerRepeatedly;

    void OnEnable() {
        if(refreshOnGameEvents) {
            GameEventBus.Subscribe(HandleGameEvent, replayEventHistoryOnEnable);
        }

        SubscribeTime();
        if(evaluateOnEnable) {
            RefreshAvailability();
        }
    }

    void Start() {
        SubscribeTime();
        if(evaluateOnStart) {
            RefreshAvailability();
        }
    }

    void OnDisable() {
        if(refreshOnGameEvents) {
            GameEventBus.Unsubscribe(HandleGameEvent);
        }

        UnsubscribeTime();
    }

    [ContextMenu("Refresh Availability")]
    public void RefreshAvailability() {
        bool available = IsAvailable(out var failureMessage);
        ApplyAvailability(available, failureMessage);
    }

    public bool IsAvailable(out string failureMessage) {
        var player = ResolvePlayer();
        bool stateAvailable = sceneObject == null || (player != null && (player.GetComponent<PlayerSceneObjectLog>()?.IsAvailable(sceneObject) ?? sceneObject.IsAvailableState(sceneObject.DefaultState)));
        bool requirementsAvailable = AreRequirementsMet(player, out failureMessage);

        bool available = availabilityMode switch {
            ConditionalSceneObjectAvailabilityMode.RequirementsOnly => requirementsAvailable,
            ConditionalSceneObjectAvailabilityMode.SceneObjectStateOnly => stateAvailable,
            ConditionalSceneObjectAvailabilityMode.SceneObjectStateOrRequirements => stateAvailable || requirementsAvailable,
            _ => stateAvailable && requirementsAvailable
        };

        if(invertAvailability) {
            available = !available;
        }

        if(available) {
            failureMessage = null;
        } else if(string.IsNullOrWhiteSpace(failureMessage)) {
            failureMessage = string.IsNullOrWhiteSpace(blockedMessage) ? "This is not available right now." : blockedMessage;
        }

        return available;
    }

    public void OnPlayerTriggered(PlayerController player) {
        if(!IsAvailable(out var failureMessage)) {
            HandleBlockedInteraction(player, failureMessage);
            return;
        }

        RecordInteraction(player, recordInteractionOnTrigger);
        ApplyConsequenceChains(player, successfulInteractionChains, "triggered");
        if(triggerForwardTarget != null && triggerForwardTarget != this && triggerForwardTarget is IPlayerTriggerable triggerable) {
            triggerable.OnPlayerTriggered(player);
        }
    }

    public IEnumerator Interact(Transform initiator) {
        var player = initiator != null ? initiator.GetComponent<PlayerController>() : ResolvePlayer();
        if(!IsAvailable(out var failureMessage)) {
            HandleBlockedInteraction(player, failureMessage);
            yield break;
        }

        RecordInteraction(player, recordInteractionOnInteract);
        ApplyConsequenceChains(player, successfulInteractionChains, "interacted");
        if(interactForwardTarget != null && interactForwardTarget != this && interactForwardTarget is Interactable interactable) {
            yield return interactable.Interact(initiator);
        }
    }

    bool AreRequirementsMet(PlayerController player, out string failureMessage) {
        if(accessProfile != null) {
            if(player == null) {
                failureMessage = "A player is required for access checks.";
                return false;
            }

            if(!accessProfile.CanAccess(player, out failureMessage)) {
                return false;
            }
        }

        return ConsequenceChainDefinition.RequirementsMet(player, requirements, requirementMatchMode, out failureMessage);
    }

    void ApplyAvailability(bool available, string failureMessage) {
        bool wasInitialized = lastAvailability.HasValue;
        bool changed = !lastAvailability.HasValue || lastAvailability.Value != available;
        lastAvailability = available;

        ApplyTargets(available);
        if(changed && (wasInitialized || runAvailabilityChainsOnInitialEvaluation)) {
            var player = ResolvePlayer();
            ApplyConsequenceChains(player, available ? becameAvailableChains : becameUnavailableChains, available ? "became-available" : "became-unavailable");
            PublishAvailabilityEvent(player, available, failureMessage);
        }
    }

    void ApplyTargets(bool available) {
        if(setTargetGameObjectsActive) {
            var objects = targetGameObjects != null && targetGameObjects.Count > 0 ? targetGameObjects : new List<GameObject> { gameObject };
            foreach(var target in objects) {
                if(target != null && target.activeSelf != available) {
                    target.SetActive(available);
                }
            }
        }

        if(toggleRenderers) {
            foreach(var renderer in ResolveRenderers()) {
                if(renderer != null) {
                    renderer.enabled = available;
                }
            }
        }

        if(toggleColliders) {
            foreach(var collider in ResolveColliders()) {
                if(collider != null) {
                    collider.enabled = available;
                }
            }
        }

        if(toggleBehaviours && targetBehaviours != null) {
            foreach(var behaviour in targetBehaviours) {
                if(behaviour != null && behaviour != this) {
                    behaviour.enabled = available;
                }
            }
        }
    }

    IEnumerable<Renderer> ResolveRenderers() {
        return targetRenderers != null && targetRenderers.Count > 0
            ? targetRenderers.Where(renderer => renderer != null)
            : GetComponentsInChildren<Renderer>(includeInactive: true);
    }

    IEnumerable<Collider2D> ResolveColliders() {
        return targetColliders != null && targetColliders.Count > 0
            ? targetColliders.Where(collider => collider != null)
            : GetComponentsInChildren<Collider2D>(includeInactive: true);
    }

    void HandleBlockedInteraction(PlayerController player, string failureMessage) {
        string message = string.IsNullOrWhiteSpace(failureMessage) ? blockedMessage : failureMessage;
        ApplyConsequenceChains(player, blockedInteractionChains, "blocked");
        PublishInteractionEvent(player, "blocked", message, GameEventImportance.Warning);
    }

    void RecordInteraction(PlayerController player, bool shouldRecord) {
        if(!shouldRecord || player == null || sceneObject == null) {
            return;
        }

        var log = player.GetComponent<PlayerSceneObjectLog>() ?? player.gameObject.AddComponent<PlayerSceneObjectLog>();
        log.RecordInteraction(sceneObject, SourceId, this);
    }

    void ApplyConsequenceChains(PlayerController player, IEnumerable<ConsequenceChainDefinition> chains, string phase) {
        if(player == null || chains == null) {
            return;
        }

        var context = new ConsequenceChainContext {
            SourceId = $"{SourceId}:{phase}",
            SourceName = DisplayName,
            ContextObject = this
        };

        foreach(var chain in chains) {
            chain?.Apply(player, context, this);
        }
    }

    void PublishAvailabilityEvent(PlayerController player, bool available, string failureMessage) {
        if(!logAttempts) {
            return;
        }

        GameEventPublishing.PublishOptional(
            null,
            $"conditional-scene-object.{(available ? "available" : "unavailable")}.{SourceId}",
            available ? $"{DisplayName} is available." : $"{DisplayName} is unavailable: {failureMessage}",
            GameEventCategory.SceneObject,
            available ? GameEventImportance.Info : GameEventImportance.Trace,
            this,
            "ConditionalSceneObject",
            GameEventScope.Scene,
            showInFeed: false,
            writeToDebugLog: logAttempts,
            GameEventPublishing.Value("sourceId", SourceId),
            GameEventPublishing.Value("sceneObjectId", sceneObject != null ? sceneObject.Id : string.Empty),
            GameEventPublishing.Value("available", available),
            GameEventPublishing.Value("player", player != null ? player.name : string.Empty));
    }

    void PublishInteractionEvent(PlayerController player, string phase, string message, GameEventImportance importance) {
        if(!logAttempts && importance < GameEventImportance.Warning) {
            return;
        }

        GameEventPublishing.PublishOptional(
            null,
            $"conditional-scene-object.{phase}.{SourceId}",
            message,
            GameEventCategory.SceneObject,
            importance,
            this,
            "ConditionalSceneObject",
            GameEventScope.Scene,
            showInFeed: false,
            writeToDebugLog: logAttempts,
            GameEventPublishing.Value("sourceId", SourceId),
            GameEventPublishing.Value("sceneObjectId", sceneObject != null ? sceneObject.Id : string.Empty),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("player", player != null ? player.name : string.Empty));
    }

    void HandleGameEvent(GameEventRecord record) {
        RefreshAvailability();
    }

    void HandleTimeChanged() {
        RefreshAvailability();
    }

    void SubscribeTime() {
        if(timeSubscribed || TimeSystem.i == null) {
            return;
        }

        if(refreshOnTimeChanged) {
            TimeSystem.i.OnTimeChanged += HandleTimeChanged;
        }

        if(refreshOnDayChanged) {
            TimeSystem.i.OnDayChanged += HandleTimeChanged;
        }

        timeSubscribed = refreshOnTimeChanged || refreshOnDayChanged;
    }

    void UnsubscribeTime() {
        if(!timeSubscribed || TimeSystem.i == null) {
            timeSubscribed = false;
            return;
        }

        TimeSystem.i.OnTimeChanged -= HandleTimeChanged;
        TimeSystem.i.OnDayChanged -= HandleTimeChanged;
        timeSubscribed = false;
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
}
