using System;
using UnityEngine;

public enum RadialMenuContextKind {
    Generic,
    PartySlot,
    InventoryItem,
    WorldInteraction,
    EncounterChoice
}

public class RadialMenuOpenBridge : MonoBehaviour {
    [Header("Radial")]
    [Tooltip("Controller that opens/closes the radial menu. Empty searches in children, parent or scene at runtime.")]
    [SerializeField] RadialMenuController controller;
    [Tooltip("Provider component used to build options. It must implement IRadialMenuProvider.")]
    [SerializeField] MonoBehaviour provider;
    [Tooltip("Optional visual anchor used by the radial menu. Empty uses this transform.")]
    [SerializeField] Transform anchor;

    [Header("Default Context")]
    [Tooltip("Default context kind used when OpenDefault is called.")]
    [SerializeField] RadialMenuContextKind defaultContextKind = RadialMenuContextKind.Generic;
    [Tooltip("Optional context id such as party-slot, inventory-item, world-target or encounter-choice.")]
    [SerializeField] string defaultContextId = string.Empty;
    [Tooltip("Optional index such as party slot index or inventory item index.")]
    [SerializeField] int defaultIndex = -1;
    [Tooltip("Optional payload object passed to the provider.")]
    [SerializeField] UnityEngine.Object defaultPayload;

    [Header("Behavior")]
    [Tooltip("If enabled, calling Open closes any currently open radial menu first.")]
    [SerializeField] bool closeBeforeOpen = true;
    [Tooltip("If enabled, this bridge logs successful opens and failed open attempts.")]
    [SerializeField] bool logDebugMessages;

    public RadialMenuController Controller => controller;
    public MonoBehaviour Provider => provider;
    public Transform Anchor => anchor;
    public RadialMenuContextKind DefaultContextKind => defaultContextKind;
    public int DefaultIndex => defaultIndex;
    public UnityEngine.Object DefaultPayload => defaultPayload;
    public event Action<RadialMenuContext> OnRadialOpenRequested;
    public event Action<RadialMenuContext> OnRadialOpened;
    public event Action<RadialMenuContext, string> OnRadialOpenFailed;

    void Awake() {
        ResolveController();
        if(anchor == null) {
            anchor = transform;
        }
    }

    public bool OpenDefault() {
        return Open(defaultContextKind, defaultContextId, defaultIndex, defaultPayload, anchor);
    }

    public bool OpenPartySlot(int slotIndex) {
        return Open(RadialMenuContextKind.PartySlot, $"party-slot-{slotIndex}", slotIndex, null, anchor);
    }

    public bool OpenInventoryItem(int itemIndex, UnityEngine.Object payload = null) {
        return Open(RadialMenuContextKind.InventoryItem, $"inventory-item-{itemIndex}", itemIndex, payload, anchor);
    }

    public bool OpenWorldInteraction(UnityEngine.Object payload = null) {
        return Open(RadialMenuContextKind.WorldInteraction, "world-interaction", -1, payload, anchor);
    }

    public bool OpenEncounterChoice(UnityEngine.Object payload = null) {
        return Open(RadialMenuContextKind.EncounterChoice, "encounter-choice", -1, payload, anchor);
    }

    public bool Open(RadialMenuContextKind kind, string contextId, int index = -1, UnityEngine.Object payload = null, Transform overrideAnchor = null) {
        var resolvedController = ResolveController();
        if(resolvedController == null) {
            return Fail(null, "No RadialMenuController was found.");
        }

        var resolvedProvider = ResolveProvider();
        if(resolvedProvider == null) {
            return Fail(null, "Provider is missing or does not implement IRadialMenuProvider.");
        }

        var context = RadialMenuContext.From(
            this,
            overrideAnchor != null ? overrideAnchor : anchor != null ? anchor : transform,
            BuildContextId(kind, contextId),
            index,
            payload);

        OnRadialOpenRequested?.Invoke(context);
        if(closeBeforeOpen && resolvedController.State != RadialMenuState.Closed) {
            resolvedController.Close();
        }

        bool opened = resolvedController.Open(resolvedProvider, context);
        if(opened) {
            OnRadialOpened?.Invoke(context);
            if(logDebugMessages) {
                GameDebug.Step($"Radial menu opened: {context.contextId}.", GameDebugCategory.UI, this, "RadialMenuOpenBridge");
            }
            return true;
        }

        return Fail(context, "Radial controller rejected the context or provider produced no options.");
    }

    public void Close() {
        ResolveController()?.Close();
    }

    public void SetProvider(MonoBehaviour nextProvider) {
        provider = nextProvider;
    }

    public void SetController(RadialMenuController nextController) {
        controller = nextController;
    }

    RadialMenuController ResolveController() {
        if(controller != null) {
            return controller;
        }

        controller = GetComponentInChildren<RadialMenuController>(true)
            ?? GetComponentInParent<RadialMenuController>()
            ?? FindAnyObjectByType<RadialMenuController>();
        return controller;
    }

    IRadialMenuProvider ResolveProvider() {
        if(provider is IRadialMenuProvider radialProvider) {
            return radialProvider;
        }

        provider = GetComponent<MonoBehaviour>();
        if(provider is IRadialMenuProvider selfProvider) {
            return selfProvider;
        }

        foreach(var behaviour in GetComponents<MonoBehaviour>()) {
            if(behaviour is IRadialMenuProvider componentProvider) {
                provider = behaviour;
                return componentProvider;
            }
        }

        return null;
    }

    string BuildContextId(RadialMenuContextKind kind, string contextId) {
        if(!string.IsNullOrWhiteSpace(contextId)) {
            return contextId;
        }

        return kind switch {
            RadialMenuContextKind.PartySlot => "party-slot",
            RadialMenuContextKind.InventoryItem => "inventory-item",
            RadialMenuContextKind.WorldInteraction => "world-interaction",
            RadialMenuContextKind.EncounterChoice => "encounter-choice",
            _ => "radial-context"
        };
    }

    bool Fail(RadialMenuContext context, string reason) {
        OnRadialOpenFailed?.Invoke(context, reason);
        if(logDebugMessages) {
            GameDebugLogger.Ensure().Record(GameDebugSeverity.Warning, GameDebugCategory.UI, reason, this, "RadialMenuOpenBridge");
        }
        return false;
    }
}
