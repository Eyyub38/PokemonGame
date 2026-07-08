using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum RadialMenuState {
    Closed,
    Opening,
    Open,
    Confirming,
    Closing
}

[Serializable]
public class RadialMenuOption {
    [Tooltip("Stable option id used by callbacks and debug output.")]
    public string id;
    [Tooltip("Short label shown by the option tag/frame, not on the ring segment.")]
    public string label;
    [Tooltip("Optional short description shown by the option tag/frame.")]
    [TextArea]
    public string description;
    [Tooltip("Icon shown inside the radial segment.")]
    public Sprite icon;
    [Tooltip("If disabled, the option remains visible but cannot be confirmed.")]
    public bool disabled;
    [Tooltip("Reason shown when the option is disabled.")]
    public string disabledReason;
    [Tooltip("Optional payload object for screen-specific action handlers.")]
    public UnityEngine.Object payload;
    [Tooltip("Optional priority used by providers before the view lays out options.")]
    public int priority;

    public Action<RadialMenuOption> onSelected;

    public string DisplayLabel => string.IsNullOrWhiteSpace(label) ? id : label;
    public bool CanSelect => !disabled;

    public static RadialMenuOption Create(string id, string label, Sprite icon = null, Action<RadialMenuOption> onSelected = null, bool disabled = false, string disabledReason = null) {
        return new RadialMenuOption {
            id = id,
            label = label,
            icon = icon,
            onSelected = onSelected,
            disabled = disabled,
            disabledReason = disabledReason
        };
    }
}

public interface IRadialMenuProvider {
    IReadOnlyList<RadialMenuOption> BuildRadialOptions(RadialMenuContext context);
    void OnRadialOptionSelected(RadialMenuOption option, RadialMenuContext context);
    void OnRadialMenuClosed(RadialMenuContext context);
}

[Serializable]
public class RadialMenuContext {
    [Tooltip("GameObject/Component that requested the radial menu.")]
    public UnityEngine.Object owner;
    [Tooltip("Transform used as visual anchor for the radial menu.")]
    public Transform anchor;
    [Tooltip("Optional context id such as party-slot-0, inventory-item, world-object or encounter-choice.")]
    public string contextId;
    [Tooltip("Optional context index such as party slot index.")]
    public int index = -1;
    [Tooltip("Optional payload object for provider-specific use.")]
    public UnityEngine.Object payload;

    public static RadialMenuContext From(UnityEngine.Object owner, Transform anchor = null, string contextId = null, int index = -1, UnityEngine.Object payload = null) {
        return new RadialMenuContext {
            owner = owner,
            anchor = anchor,
            contextId = contextId,
            index = index,
            payload = payload
        };
    }
}

[CreateAssetMenu(menuName = "UI/Radial/Radial Menu Theme")]
public class RadialMenuTheme : ScriptableObject {
    [Header("Segment Colors")]
    [Tooltip("Default segment color.")]
    [SerializeField] Color normalColor = new Color(0.12f, 0.14f, 0.18f, 0.92f);
    [Tooltip("Focused segment color.")]
    [SerializeField] Color focusedColor = new Color(0.25f, 0.55f, 0.95f, 1f);
    [Tooltip("Disabled segment color.")]
    [SerializeField] Color disabledColor = new Color(0.18f, 0.18f, 0.18f, 0.55f);
    [Tooltip("Confirmed segment color.")]
    [SerializeField] Color confirmColor = new Color(0.25f, 0.9f, 0.55f, 1f);

    [Header("Icon Colors")]
    [Tooltip("Default icon color.")]
    [SerializeField] Color iconColor = Color.white;
    [Tooltip("Focused icon color.")]
    [SerializeField] Color focusedIconColor = Color.white;
    [Tooltip("Disabled icon color.")]
    [SerializeField] Color disabledIconColor = new Color(1f, 1f, 1f, 0.35f);

    [Header("Timing")]
    [Tooltip("Seconds used by simple view transitions. 0 keeps transitions instant.")]
    [Min(0f)]
    [SerializeField] float transitionDuration = 0.08f;

    public Color NormalColor => normalColor;
    public Color FocusedColor => focusedColor;
    public Color DisabledColor => disabledColor;
    public Color ConfirmColor => confirmColor;
    public Color IconColor => iconColor;
    public Color FocusedIconColor => focusedIconColor;
    public Color DisabledIconColor => disabledIconColor;
    public float TransitionDuration => Mathf.Max(0f, transitionDuration);
}

[CreateAssetMenu(menuName = "UI/Radial/Radial Menu Layout Profile")]
public class RadialMenuLayoutProfile : ScriptableObject {
    [Header("Ring")]
    [Tooltip("Distance from center to segment anchors.")]
    [Min(0f)]
    [SerializeField] float radius = 54f;
    [Tooltip("Extra offset applied to the focused segment.")]
    [Min(0f)]
    [SerializeField] float focusedOffset = 10f;
    [Tooltip("Segment RectTransform size.")]
    [SerializeField] Vector2 segmentSize = new Vector2(34f, 34f);
    [Tooltip("Angle in degrees used by the first option.")]
    [SerializeField] float startAngle = 90f;
    [Tooltip("If enabled, segments are placed clockwise.")]
    [SerializeField] bool clockwise = true;
    [Tooltip("Maximum option count shown by the view. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxVisibleOptions;

    [Header("Tag")]
    [Tooltip("Offset applied to the option tag/frame from the radial center.")]
    [SerializeField] Vector2 tagOffset = new Vector2(0f, -74f);

    public float Radius => Mathf.Max(0f, radius);
    public float FocusedOffset => Mathf.Max(0f, focusedOffset);
    public Vector2 SegmentSize => segmentSize;
    public float StartAngle => startAngle;
    public bool Clockwise => clockwise;
    public int MaxVisibleOptions => Mathf.Max(0, maxVisibleOptions);
    public Vector2 TagOffset => tagOffset;
}

public class RadialMenuController : MonoBehaviour {
    [Header("View")]
    [Tooltip("View that renders the radial ring and option tag.")]
    [SerializeField] RadialMenuView view;
    [Tooltip("Fallback layout used when the view has no explicit profile.")]
    [SerializeField] RadialMenuLayoutProfile layoutProfile;
    [Tooltip("Fallback theme used when the view has no explicit theme.")]
    [SerializeField] RadialMenuTheme theme;

    [Header("Provider")]
    [Tooltip("Optional provider component. It must implement IRadialMenuProvider.")]
    [SerializeField] MonoBehaviour providerOverride;
    [Tooltip("If enabled, the menu closes after a successful confirm.")]
    [SerializeField] bool closeAfterConfirm = true;

    RadialMenuState state = RadialMenuState.Closed;
    IRadialMenuProvider activeProvider;
    RadialMenuContext activeContext;
    readonly List<RadialMenuOption> options = new List<RadialMenuOption>();
    int focusedIndex = -1;

    public RadialMenuState State => state;
    public int FocusedIndex => focusedIndex;
    public IReadOnlyList<RadialMenuOption> Options => options;
    public event Action<RadialMenuState> OnStateChanged;
    public event Action<int, RadialMenuOption> OnFocusChanged;
    public event Action<RadialMenuOption> OnOptionConfirmed;

    void Awake() {
        if(view == null) {
            view = GetComponentInChildren<RadialMenuView>(true);
        }
    }

    public bool OpenFromOverride(Transform anchor = null, string contextId = null, int index = -1, UnityEngine.Object payload = null) {
        var provider = providerOverride as IRadialMenuProvider;
        return Open(provider, RadialMenuContext.From(providerOverride, anchor != null ? anchor : transform, contextId, index, payload));
    }

    public bool Open(IRadialMenuProvider provider, RadialMenuContext context) {
        if(provider == null) {
            Close();
            return false;
        }

        activeProvider = provider;
        activeContext = context ?? RadialMenuContext.From(provider as UnityEngine.Object, transform);
        options.Clear();
        options.AddRange((provider.BuildRadialOptions(activeContext) ?? Array.Empty<RadialMenuOption>())
            .Where(option => option != null)
            .OrderBy(option => option.priority)
            .ThenBy(option => option.DisplayLabel));

        if(options.Count == 0) {
            Close();
            return false;
        }

        focusedIndex = FirstSelectableIndex();
        if(focusedIndex < 0) {
            focusedIndex = 0;
        }

        SetState(RadialMenuState.Opening);
        if(view != null) {
            view.Show(options, focusedIndex, activeContext, layoutProfile, theme);
        }

        SetState(RadialMenuState.Open);
        OnFocusChanged?.Invoke(focusedIndex, GetFocusedOption());
        return true;
    }

    public void Close() {
        if(state == RadialMenuState.Closed) {
            return;
        }

        SetState(RadialMenuState.Closing);
        view?.Hide();
        activeProvider?.OnRadialMenuClosed(activeContext);
        activeProvider = null;
        activeContext = null;
        options.Clear();
        focusedIndex = -1;
        SetState(RadialMenuState.Closed);
    }

    public bool MoveFocus(int delta) {
        if(state != RadialMenuState.Open || options.Count == 0 || delta == 0) {
            return false;
        }

        int next = FindNextSelectable(focusedIndex, delta);
        if(next < 0 || next == focusedIndex) {
            return false;
        }

        focusedIndex = next;
        view?.SetFocus(focusedIndex);
        OnFocusChanged?.Invoke(focusedIndex, GetFocusedOption());
        return true;
    }

    public bool FocusIndex(int index) {
        if(state != RadialMenuState.Open || index < 0 || index >= options.Count) {
            return false;
        }

        focusedIndex = index;
        view?.SetFocus(focusedIndex);
        OnFocusChanged?.Invoke(focusedIndex, GetFocusedOption());
        return true;
    }

    public bool ConfirmFocused() {
        if(state != RadialMenuState.Open) {
            return false;
        }

        var option = GetFocusedOption();
        if(option == null || !option.CanSelect) {
            view?.SetFocus(focusedIndex);
            return false;
        }

        SetState(RadialMenuState.Confirming);
        view?.PlayConfirm(focusedIndex);
        option.onSelected?.Invoke(option);
        activeProvider?.OnRadialOptionSelected(option, activeContext);
        OnOptionConfirmed?.Invoke(option);

        if(closeAfterConfirm) {
            Close();
        } else {
            SetState(RadialMenuState.Open);
        }

        return true;
    }

    public void Cancel() {
        Close();
    }

    RadialMenuOption GetFocusedOption() {
        return focusedIndex >= 0 && focusedIndex < options.Count ? options[focusedIndex] : null;
    }

    int FirstSelectableIndex() {
        for(int i = 0; i < options.Count; i++) {
            if(options[i] != null && options[i].CanSelect) {
                return i;
            }
        }

        return -1;
    }

    int FindNextSelectable(int start, int delta) {
        int count = options.Count;
        for(int step = 1; step <= count; step++) {
            int index = (start + delta * step) % count;
            if(index < 0) {
                index += count;
            }

            if(options[index] != null && options[index].CanSelect) {
                return index;
            }
        }

        return start;
    }

    void SetState(RadialMenuState next) {
        if(state == next) {
            return;
        }

        state = next;
        OnStateChanged?.Invoke(state);
    }
}

public class RadialMenuView : MonoBehaviour {
    [Header("Layout")]
    [Tooltip("RectTransform used as the radial center.")]
    [SerializeField] RectTransform center;
    [Tooltip("Root where segment instances are placed.")]
    [SerializeField] RectTransform segmentRoot;
    [Tooltip("Segment prefab used for each radial option.")]
    [SerializeField] RadialSegmentView segmentPrefab;
    [Tooltip("Option tag/frame view shown when a segment is focused.")]
    [SerializeField] RadialOptionTagView tagView;
    [Tooltip("Default layout profile used by this view.")]
    [SerializeField] RadialMenuLayoutProfile defaultLayout;
    [Tooltip("Default visual theme used by this view.")]
    [SerializeField] RadialMenuTheme defaultTheme;

    readonly List<RadialSegmentView> segments = new List<RadialSegmentView>();
    List<RadialMenuOption> options = new List<RadialMenuOption>();
    RadialMenuLayoutProfile activeLayout;
    RadialMenuTheme activeTheme;

    public IReadOnlyList<RadialSegmentView> Segments => segments;

    void Awake() {
        if(center == null) {
            center = transform as RectTransform;
        }

        if(segmentRoot == null) {
            segmentRoot = center;
        }
    }

    public void Show(IReadOnlyList<RadialMenuOption> menuOptions, int focusedIndex, RadialMenuContext context, RadialMenuLayoutProfile layoutOverride = null, RadialMenuTheme themeOverride = null) {
        activeLayout = layoutOverride != null ? layoutOverride : defaultLayout;
        activeTheme = themeOverride != null ? themeOverride : defaultTheme;
        options = menuOptions != null ? menuOptions.ToList() : new List<RadialMenuOption>();
        if(activeLayout != null && activeLayout.MaxVisibleOptions > 0) {
            options = options.Take(activeLayout.MaxVisibleOptions).ToList();
        }

        if(context != null && context.anchor != null && center != null) {
            center.position = context.anchor.position;
        }

        gameObject.SetActive(true);
        RebuildSegments();
        SetFocus(Mathf.Clamp(focusedIndex, 0, Mathf.Max(0, options.Count - 1)));
    }

    public void Hide() {
        tagView?.Hide();
        foreach(var segment in segments) {
            if(segment != null) {
                segment.gameObject.SetActive(false);
            }
        }

        gameObject.SetActive(false);
    }

    public void SetFocus(int index) {
        for(int i = 0; i < segments.Count; i++) {
            if(segments[i] != null) {
                segments[i].SetFocused(i == index, activeTheme, activeLayout);
            }
        }

        if(index >= 0 && index < options.Count) {
            tagView?.Show(options[index], activeLayout);
        } else {
            tagView?.Hide();
        }
    }

    public void PlayConfirm(int index) {
        if(index >= 0 && index < segments.Count && segments[index] != null) {
            segments[index].SetConfirmed(activeTheme);
        }
    }

    void RebuildSegments() {
        EnsureSegmentCount(options.Count);
        int count = Mathf.Max(1, options.Count);
        float radius = activeLayout != null ? activeLayout.Radius : 54f;
        float startAngle = activeLayout != null ? activeLayout.StartAngle : 90f;
        bool clockwise = activeLayout == null || activeLayout.Clockwise;
        Vector2 size = activeLayout != null ? activeLayout.SegmentSize : new Vector2(34f, 34f);

        for(int i = 0; i < segments.Count; i++) {
            bool active = i < options.Count;
            var segment = segments[i];
            if(segment == null) {
                continue;
            }

            segment.gameObject.SetActive(active);
            if(!active) {
                continue;
            }

            float step = 360f / count;
            float angle = startAngle + (clockwise ? -step * i : step * i);
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            segment.Configure(options[i], i, direction, radius, size, activeTheme);
        }
    }

    void EnsureSegmentCount(int count) {
        if(segmentPrefab == null) {
            return;
        }

        while(segments.Count < count) {
            var instance = Instantiate(segmentPrefab, segmentRoot != null ? segmentRoot : center);
            segments.Add(instance);
        }
    }
}

public class RadialSegmentView : MonoBehaviour {
    [Header("References")]
    [Tooltip("Root RectTransform moved around the radial center.")]
    [SerializeField] RectTransform rectTransform;
    [Tooltip("Background image tinted by focus/disabled state.")]
    [SerializeField] Image background;
    [Tooltip("Icon image shown inside this segment.")]
    [SerializeField] Image icon;
    [Tooltip("Optional button. When assigned, pointer clicks focus/confirm can be wired by the screen.")]
    [SerializeField] Button button;
    [Tooltip("Optional canvas group used to dim disabled segments.")]
    [SerializeField] CanvasGroup canvasGroup;

    Vector2 baseDirection;
    float baseRadius;
    bool disabled;

    public int Index { get; private set; }
    public RadialMenuOption Option { get; private set; }
    public Button Button => button;

    void Awake() {
        if(rectTransform == null) {
            rectTransform = transform as RectTransform;
        }

        if(canvasGroup == null) {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    public void Configure(RadialMenuOption option, int index, Vector2 direction, float radius, Vector2 size, RadialMenuTheme theme) {
        Option = option;
        Index = index;
        baseDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.up;
        baseRadius = Mathf.Max(0f, radius);
        disabled = option != null && option.disabled;

        if(rectTransform != null) {
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = baseDirection * baseRadius;
        }

        if(icon != null) {
            icon.sprite = option != null ? option.icon : null;
            icon.enabled = icon.sprite != null;
            icon.color = disabled ? Resolve(theme, t => t.DisabledIconColor, new Color(1f, 1f, 1f, 0.35f)) : Resolve(theme, t => t.IconColor, Color.white);
        }

        if(background != null) {
            background.color = disabled ? Resolve(theme, t => t.DisabledColor, new Color(0.18f, 0.18f, 0.18f, 0.55f)) : Resolve(theme, t => t.NormalColor, Color.white);
        }

        if(canvasGroup != null) {
            canvasGroup.alpha = disabled ? 0.55f : 1f;
            canvasGroup.interactable = !disabled;
            canvasGroup.blocksRaycasts = !disabled;
        }
    }

    public void SetFocused(bool focused, RadialMenuTheme theme, RadialMenuLayoutProfile layout) {
        float offset = focused && !disabled && layout != null ? layout.FocusedOffset : 0f;
        if(rectTransform != null) {
            rectTransform.anchoredPosition = baseDirection * (baseRadius + offset);
        }

        if(background != null) {
            background.color = disabled
                ? Resolve(theme, t => t.DisabledColor, new Color(0.18f, 0.18f, 0.18f, 0.55f))
                : focused ? Resolve(theme, t => t.FocusedColor, Color.cyan) : Resolve(theme, t => t.NormalColor, Color.white);
        }

        if(icon != null) {
            icon.color = disabled
                ? Resolve(theme, t => t.DisabledIconColor, new Color(1f, 1f, 1f, 0.35f))
                : focused ? Resolve(theme, t => t.FocusedIconColor, Color.white) : Resolve(theme, t => t.IconColor, Color.white);
        }
    }

    public void SetConfirmed(RadialMenuTheme theme) {
        if(background != null && !disabled) {
            background.color = Resolve(theme, t => t.ConfirmColor, Color.green);
        }
    }

    static Color Resolve(RadialMenuTheme theme, Func<RadialMenuTheme, Color> read, Color fallback) {
        return theme != null && read != null ? read(theme) : fallback;
    }
}

public class RadialOptionTagView : MonoBehaviour {
    [Header("References")]
    [Tooltip("Root RectTransform for the label frame.")]
    [SerializeField] RectTransform rectTransform;
    [Tooltip("Optional CanvasGroup used for show/hide.")]
    [SerializeField] CanvasGroup canvasGroup;
    [Tooltip("Text that displays the focused option label.")]
    [SerializeField] Text labelText;
    [Tooltip("Text that displays the focused option description or disabled reason.")]
    [SerializeField] Text descriptionText;

    void Awake() {
        if(rectTransform == null) {
            rectTransform = transform as RectTransform;
        }

        if(canvasGroup == null) {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    public void Show(RadialMenuOption option, RadialMenuLayoutProfile layout) {
        if(option == null) {
            Hide();
            return;
        }

        gameObject.SetActive(true);
        if(rectTransform != null && layout != null) {
            rectTransform.anchoredPosition = layout.TagOffset;
        }

        if(labelText != null) {
            labelText.text = option.DisplayLabel;
        }

        if(descriptionText != null) {
            descriptionText.text = option.disabled && !string.IsNullOrWhiteSpace(option.disabledReason)
                ? option.disabledReason
                : option.description ?? string.Empty;
        }

        if(canvasGroup != null) {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    public void Hide() {
        if(canvasGroup != null) {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        gameObject.SetActive(false);
    }
}

public class RadialMenuInputRouter : MonoBehaviour {
    [Header("Target")]
    [Tooltip("Radial menu controller receiving input.")]
    [SerializeField] RadialMenuController controller;

    [Header("Keyboard Polling")]
    [Tooltip("If enabled, this router polls simple keyboard keys in Update for quick tests.")]
    [SerializeField] bool pollKeyboard = true;
    [Tooltip("Key used to move radial focus counter-clockwise/previous.")]
    [SerializeField] KeyCode previousKey = KeyCode.LeftArrow;
    [Tooltip("Key used to move radial focus clockwise/next.")]
    [SerializeField] KeyCode nextKey = KeyCode.RightArrow;
    [Tooltip("Key used to confirm focused option.")]
    [SerializeField] KeyCode confirmKey = KeyCode.Return;
    [Tooltip("Key used to cancel/close the radial menu.")]
    [SerializeField] KeyCode cancelKey = KeyCode.Escape;

    void Awake() {
        if(controller == null) {
            controller = GetComponent<RadialMenuController>();
        }
    }

    void Update() {
        if(!pollKeyboard || controller == null || controller.State != RadialMenuState.Open) {
            return;
        }

        if(Input.GetKeyDown(previousKey)) {
            controller.MoveFocus(-1);
        }

        if(Input.GetKeyDown(nextKey)) {
            controller.MoveFocus(1);
        }

        if(Input.GetKeyDown(confirmKey)) {
            controller.ConfirmFocused();
        }

        if(Input.GetKeyDown(cancelKey)) {
            controller.Cancel();
        }
    }

    public void MovePrevious() {
        controller?.MoveFocus(-1);
    }

    public void MoveNext() {
        controller?.MoveFocus(1);
    }

    public void Confirm() {
        controller?.ConfirmFocused();
    }

    public void Cancel() {
        controller?.Cancel();
    }

    public void Navigate(Vector2 direction) {
        if(controller == null || direction.sqrMagnitude < 0.01f) {
            return;
        }

        controller.MoveFocus(direction.x < 0f || direction.y < 0f ? -1 : 1);
    }
}
