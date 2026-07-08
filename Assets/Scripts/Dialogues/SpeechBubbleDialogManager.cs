using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpeechBubbleDialogManager : MonoBehaviour {
    [Header("Lifetime")]
    [Tooltip("If enabled, this manager stays alive between scene loads.")]
    [SerializeField] bool dontDestroyOnLoad = true;

    [Header("Input")]
    [Tooltip("Input actions used to advance or fast-forward speech bubbles. Empty allows timed auto-advance.")]
    [SerializeField] InputActionAsset actions;
    [Tooltip("Action map containing the advance action.")]
    [SerializeField] string actionMapName = "UI";
    [Tooltip("Action name used to advance or skip typing.")]
    [SerializeField] string advanceActionName = "Select";

    [Header("Typing Defaults")]
    [Tooltip("Default typing speed used when the style does not override it.")]
    [Min(1)]
    [SerializeField] int lettersPerSecond = 28;
    [Tooltip("Default speed multiplier while the advance button is held.")]
    [Min(1)]
    [SerializeField] int fastForwardMultiplier = 8;
    [Tooltip("If enabled, typing and auto-advance use unscaled time.")]
    [SerializeField] bool useUnscaledTime;

    [Header("Timing Defaults")]
    [Tooltip("Minimum seconds a completed line stays visible when not waiting for input.")]
    [Min(0f)]
    [SerializeField] float minimumVisibleSeconds = 0.8f;
    [Tooltip("Extra seconds added per character when auto-advancing.")]
    [Min(0f)]
    [SerializeField] float secondsPerCharacter = 0.025f;
    [Tooltip("Small delay after closing a bubble before the next line opens.")]
    [Min(0f)]
    [SerializeField] float closeDelaySeconds = 0.05f;

    [Header("Fallback")]
    [Tooltip("If enabled and no UI presenter is listening, speech bubble dialog uses the classic DialogManager instead.")]
    [SerializeField] bool fallbackToClassicDialogWhenNoPresenter = true;

    [Header("Debug")]
    [Tooltip("If enabled, speech bubble dialog emits low-level events for debugging.")]
    [SerializeField] bool publishDebugEvents;

    InputAction advanceAction;
    Coroutine activeRoutine;

    public static SpeechBubbleDialogManager i { get; private set; }
    public bool IsShowing { get; private set; }
    public SpeechBubbleDialogRequest ActiveRequest { get; private set; }
    public bool HasPresenter => OnBubbleOpened != null || OnBubbleTextChanged != null || OnBubbleClosed != null;

    public event Action OnDialogStarted;
    public event Action OnDialogFinished;
    public event Action<SpeechBubbleDialogRequest> OnBubbleOpened;
    public event Action<SpeechBubbleDialogRequest> OnBubbleTextChanged;
    public event Action<SpeechBubbleDialogRequest> OnBubbleClosed;

    void Awake() {
        if(i != null && i != this) {
            Destroy(gameObject);
            return;
        }

        i = this;
        if(dontDestroyOnLoad) {
            DontDestroyOnLoad(gameObject);
        }

        BindInput();
    }

    void OnEnable() {
        advanceAction?.Enable();
    }

    void OnDisable() {
        advanceAction?.Disable();
    }

    void BindInput() {
        if(actions == null) {
            return;
        }

        var map = actions.FindActionMap(actionMapName, throwIfNotFound: false);
        advanceAction = map?.FindAction(advanceActionName, throwIfNotFound: false);
    }

    public IEnumerator ShowDialog(Dialog dialog, SpeechBubbleDialogOptions options = null) {
        if(dialog == null || dialog.Lines == null || dialog.Lines.Count == 0) {
            yield break;
        }

        options ??= new SpeechBubbleDialogOptions();
        if(fallbackToClassicDialogWhenNoPresenter && !HasPresenter && DialogManager.i != null) {
            yield return DialogManager.i.ShowDialog(dialog);
            yield break;
        }

        if(IsShowing && !options.interruptCurrent) {
            yield break;
        }

        if(activeRoutine != null) {
            StopCoroutine(activeRoutine);
            CloseActiveBubble();
            IsShowing = false;
            OnDialogFinished?.Invoke();
        }

        activeRoutine = StartCoroutine(RunDialog(dialog, options));
        yield return activeRoutine;
    }

    public IEnumerator ShowText(string text, SpeechBubbleDialogOptions options = null) {
        var dialog = new Dialog(new[] { text });
        yield return ShowDialog(dialog, options);
    }

    IEnumerator RunDialog(Dialog dialog, SpeechBubbleDialogOptions options) {
        IsShowing = true;
        OnDialogStarted?.Invoke();

        for(int i = 0; i < dialog.Lines.Count; i++) {
            yield return RunLine(dialog.Lines[i], i, dialog.Lines.Count, options);
        }

        IsShowing = false;
        ActiveRequest = null;
        activeRoutine = null;
        OnDialogFinished?.Invoke();
    }

    IEnumerator RunLine(string line, int lineIndex, int lineCount, SpeechBubbleDialogOptions options) {
        ActiveRequest = new SpeechBubbleDialogRequest(options, line, lineIndex, lineCount);
        PublishBubbleEvent("opened", ActiveRequest);
        OnBubbleOpened?.Invoke(ActiveRequest);

        yield return TypeLine(ActiveRequest, options);
        yield return WaitForAdvanceOrTimer(ActiveRequest, options);

        if(options.autoClose) {
            CloseActiveBubble();
            yield return WaitSeconds(GetCloseDelay(options.style));
        }
    }

    IEnumerator TypeLine(SpeechBubbleDialogRequest request, SpeechBubbleDialogOptions options) {
        string fullText = request.FullText ?? string.Empty;
        int speed = GetLettersPerSecond(options.style);

        for(int i = 0; i < fullText.Length; i++) {
            if(WasAdvancePressedThisFrame()) {
                request.SetVisibleText(fullText, true, ShouldShowAdvanceIndicator(options.style, options));
                OnBubbleTextChanged?.Invoke(request);
                yield break;
            }

            request.SetVisibleText(fullText.Substring(0, i + 1), i == fullText.Length - 1, false);
            OnBubbleTextChanged?.Invoke(request);

            float currentSpeed = IsAdvanceHeld() ? speed * GetFastForwardMultiplier(options.style) : speed;
            yield return WaitSeconds(1f / Mathf.Max(1f, currentSpeed));
        }

        request.SetVisibleText(fullText, true, ShouldShowAdvanceIndicator(options.style, options));
        OnBubbleTextChanged?.Invoke(request);
    }

    IEnumerator WaitForAdvanceOrTimer(SpeechBubbleDialogRequest request, SpeechBubbleDialogOptions options) {
        bool waitForInput = options.waitForAdvance && advanceAction != null;
        if(waitForInput) {
            while(IsAdvanceHeld()) {
                yield return null;
            }

            while(!WasAdvancePressedThisFrame()) {
                yield return null;
            }
            yield break;
        }

        float waitTime = GetMinimumVisibleSeconds(options.style) + (request.FullText?.Length ?? 0) * GetSecondsPerCharacter(options.style);
        yield return WaitSeconds(waitTime);
    }

    void CloseActiveBubble() {
        if(ActiveRequest == null) {
            return;
        }

        PublishBubbleEvent("closed", ActiveRequest);
        OnBubbleClosed?.Invoke(ActiveRequest);
        ActiveRequest = null;
    }

    bool WasAdvancePressedThisFrame() {
        return advanceAction != null && advanceAction.WasPressedThisFrame();
    }

    bool IsAdvanceHeld() {
        return advanceAction != null && advanceAction.IsPressed();
    }

    int GetLettersPerSecond(SpeechBubbleStyleDefinition style) {
        return style != null && style.LettersPerSecond > 0 ? style.LettersPerSecond : lettersPerSecond;
    }

    int GetFastForwardMultiplier(SpeechBubbleStyleDefinition style) {
        return style != null && style.FastForwardMultiplier > 0 ? style.FastForwardMultiplier : fastForwardMultiplier;
    }

    float GetMinimumVisibleSeconds(SpeechBubbleStyleDefinition style) {
        return style != null ? style.MinimumVisibleSeconds : minimumVisibleSeconds;
    }

    float GetSecondsPerCharacter(SpeechBubbleStyleDefinition style) {
        return style != null ? style.SecondsPerCharacter : secondsPerCharacter;
    }

    float GetCloseDelay(SpeechBubbleStyleDefinition style) {
        return style != null ? style.CloseDelaySeconds : closeDelaySeconds;
    }

    bool ShouldShowAdvanceIndicator(SpeechBubbleStyleDefinition style, SpeechBubbleDialogOptions options) {
        return options.waitForAdvance && advanceAction != null && (style == null || style.ShowAdvanceIndicator);
    }

    IEnumerator WaitSeconds(float seconds) {
        seconds = Mathf.Max(0f, seconds);
        if(seconds <= 0f) {
            yield break;
        }

        float elapsed = 0f;
        while(elapsed < seconds) {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    void PublishBubbleEvent(string phase, SpeechBubbleDialogRequest request) {
        if(!publishDebugEvents || request == null) {
            return;
        }

        GameEventBus.Publish(
            $"dialog.speech-bubble.{phase}",
            $"{request.SpeakerName} speech bubble {phase}.",
            GameEventCategory.Dialogue,
            GameEventImportance.Trace,
            request.Source,
            "SpeechBubbleDialogManager",
            GameEventScope.Scene,
            showInFeed: false,
            writeToDebugLog: true,
            values: new[] {
                GameEventPublishing.Value("phase", phase),
                GameEventPublishing.Value("speakerName", request.SpeakerName),
                GameEventPublishing.Value("lineIndex", request.LineIndex),
                GameEventPublishing.Value("lineCount", request.LineCount)
            });
    }
}
