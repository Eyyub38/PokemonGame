using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RuntimeUIScreenInputMode {
    Unchanged,
    Player,
    UI,
    Disabled
}

[Serializable]
public class RuntimeUIScreenBinding {
    [Tooltip("Stable id used by scripts/buttons to open this screen, such as PokeNav, PauseMenu or Bag.")]
    [SerializeField] string screenId;
    [Tooltip("Root GameObject for this UI screen. It is enabled/disabled by RuntimeUIScreenManager.")]
    [SerializeField] GameObject root;
    [Tooltip("If enabled, OpenExclusive closes other registered screens before opening this one.")]
    [SerializeField] bool closesOtherScreens = true;
    [Tooltip("Input map mode applied when this screen opens. Use UI for menus, Player for HUD-only screens.")]
    [SerializeField] RuntimeUIScreenInputMode inputModeOnOpen = RuntimeUIScreenInputMode.UI;

    public string ScreenId => screenId;
    public GameObject Root => root;
    public bool ClosesOtherScreens => closesOtherScreens;
    public RuntimeUIScreenInputMode InputModeOnOpen => inputModeOnOpen;
    public bool IsOpen => root != null && root.activeSelf;

    public RuntimeUIScreenBinding() {
    }

    public RuntimeUIScreenBinding(string screenId, GameObject root, bool closesOtherScreens, RuntimeUIScreenInputMode inputModeOnOpen) {
        this.screenId = screenId;
        this.root = root;
        this.closesOtherScreens = closesOtherScreens;
        this.inputModeOnOpen = inputModeOnOpen;
    }
}

public class RuntimeUIScreenManager : MonoBehaviour {
    [Header("Screen Registry")]
    [Tooltip("All UI roots this manager can open/close. Screen ids should be unique.")]
    [SerializeField] List<RuntimeUIScreenBinding> screens = new List<RuntimeUIScreenBinding>();
    [Tooltip("If enabled, all registered roots are closed in Awake before the default screen opens.")]
    [SerializeField] bool closeAllOnAwake = true;
    [Tooltip("Optional screen id opened after Awake. Empty leaves every screen closed.")]
    [SerializeField] string defaultScreenId;

    [Header("Input")]
    [Tooltip("Input map controller used when screens request Player/UI/Disabled input mode. Empty uses InputMapController.Instance or GameController.i.InputMaps.")]
    [SerializeField] InputMapController inputMapController;
    [Tooltip("Input mode restored when the final UI screen closes.")]
    [SerializeField] RuntimeUIScreenInputMode fallbackInputMode = RuntimeUIScreenInputMode.Player;

    [Header("Debug")]
    [Tooltip("If enabled, open/close actions are written to GameDebug.")]
    [SerializeField] bool logScreenChanges;

    readonly Stack<string> navigationStack = new Stack<string>();

    public IReadOnlyList<RuntimeUIScreenBinding> Screens => screens;
    public string CurrentScreenId => navigationStack.Count > 0 ? navigationStack.Peek() : string.Empty;
    public event Action<string> OnScreenOpened;
    public event Action<string> OnScreenClosed;

    void Awake() {
        if(inputMapController == null) {
            inputMapController = InputMapController.Instance != null
                ? InputMapController.Instance
                : GameController.i != null ? GameController.i.InputMaps : null;
        }

        if(closeAllOnAwake) {
            CloseAll(applyFallbackInput: false);
        }

        if(!string.IsNullOrWhiteSpace(defaultScreenId)) {
            OpenExclusive(defaultScreenId);
        }
    }

    public bool Open(string screenId) {
        return OpenInternal(screenId, exclusive: false);
    }

    public void ConfigureStartup(string defaultScreenId, bool closeAllOnAwake, RuntimeUIScreenInputMode fallbackInputMode) {
        this.defaultScreenId = defaultScreenId;
        this.closeAllOnAwake = closeAllOnAwake;
        this.fallbackInputMode = fallbackInputMode;
    }

    public void ClearRegisteredScreens() {
        screens.Clear();
    }

    public void RegisterScreen(string screenId, GameObject root, bool closesOtherScreens = true, RuntimeUIScreenInputMode inputModeOnOpen = RuntimeUIScreenInputMode.UI) {
        if(string.IsNullOrWhiteSpace(screenId) || root == null) {
            return;
        }

        var existing = FindScreen(screenId);
        if(existing != null) {
            screens.Remove(existing);
        }

        screens.Add(new RuntimeUIScreenBinding(screenId, root, closesOtherScreens, inputModeOnOpen));
    }

    public bool OpenExclusive(string screenId) {
        return OpenInternal(screenId, exclusive: true);
    }

    public bool Toggle(string screenId) {
        var screen = FindScreen(screenId);
        if(screen == null || screen.Root == null) {
            LogMissing(screenId);
            return false;
        }

        return screen.IsOpen ? Close(screenId) : Open(screenId);
    }

    public bool Close(string screenId) {
        var screen = FindScreen(screenId);
        if(screen == null || screen.Root == null) {
            LogMissing(screenId);
            return false;
        }

        if(!screen.Root.activeSelf) {
            return true;
        }

        screen.Root.SetActive(false);
        RemoveFromStack(screenId);
        OnScreenClosed?.Invoke(screenId);
        Log($"Closed UI screen '{screenId}'.");

        if(navigationStack.Count == 0) {
            ApplyInputMode(fallbackInputMode);
        }

        return true;
    }

    public void CloseAll(bool applyFallbackInput = true) {
        foreach(var screen in screens) {
            if(screen?.Root != null) {
                screen.Root.SetActive(false);
            }
        }

        navigationStack.Clear();
        if(applyFallbackInput) {
            ApplyInputMode(fallbackInputMode);
        }
    }

    public bool Back() {
        if(navigationStack.Count == 0) {
            return false;
        }

        return Close(navigationStack.Peek());
    }

    RuntimeUIScreenBinding FindScreen(string screenId) {
        if(string.IsNullOrWhiteSpace(screenId)) {
            return null;
        }

        return screens.FirstOrDefault(screen =>
            screen != null && string.Equals(screen.ScreenId, screenId, StringComparison.OrdinalIgnoreCase));
    }

    bool OpenInternal(string screenId, bool exclusive) {
        var screen = FindScreen(screenId);
        if(screen == null || screen.Root == null) {
            LogMissing(screenId);
            return false;
        }

        if(exclusive || screen.ClosesOtherScreens) {
            foreach(var other in screens) {
                if(other?.Root != null && other != screen) {
                    other.Root.SetActive(false);
                    RemoveFromStack(other.ScreenId);
                }
            }
        }

        screen.Root.SetActive(true);
        PushScreen(screenId);
        ApplyInputMode(screen.InputModeOnOpen);
        OnScreenOpened?.Invoke(screenId);
        Log($"Opened UI screen '{screenId}'.");
        return true;
    }

    void PushScreen(string screenId) {
        RemoveFromStack(screenId);
        navigationStack.Push(screenId);
    }

    void RemoveFromStack(string screenId) {
        if(navigationStack.Count == 0) {
            return;
        }

        var kept = navigationStack.Where(id => !string.Equals(id, screenId, StringComparison.OrdinalIgnoreCase)).Reverse().ToList();
        navigationStack.Clear();
        foreach(var id in kept) {
            navigationStack.Push(id);
        }
    }

    void ApplyInputMode(RuntimeUIScreenInputMode mode) {
        if(inputMapController == null || mode == RuntimeUIScreenInputMode.Unchanged) {
            return;
        }

        switch(mode) {
            case RuntimeUIScreenInputMode.Player:
                inputMapController.EnablePlayer();
                break;
            case RuntimeUIScreenInputMode.UI:
                inputMapController.EnableUI();
                break;
            case RuntimeUIScreenInputMode.Disabled:
                inputMapController.DisableAll();
                break;
        }
    }

    void LogMissing(string screenId) {
        GameDebug.Warning($"UI screen '{screenId}' is not registered or has no root.", GameDebugCategory.UI, this, "RuntimeUIScreenManager");
    }

    void Log(string message) {
        if(logScreenChanges) {
            GameDebug.Step(message, GameDebugCategory.UI, this, "RuntimeUIScreenManager");
        }
    }
}
