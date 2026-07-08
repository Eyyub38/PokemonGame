using UnityEngine;

[CreateAssetMenu(menuName = "Dialogues/Speech Bubble Style Definition")]
public class SpeechBubbleStyleDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id used by validation/debug systems. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in editor/debug output. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer notes for when this bubble style should be used.")]
    [TextArea]
    [SerializeField] string description;

    [Header("Anchor")]
    [Tooltip("World-space offset added above the speaker when no custom anchor transform is assigned.")]
    [SerializeField] Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);
    [Tooltip("If enabled, future UI presenters can keep the bubble clamped inside the screen.")]
    [SerializeField] bool clampToScreen = true;

    [Header("Typing")]
    [Tooltip("Default typing speed for this style. 0 uses the manager default.")]
    [Min(0)]
    [SerializeField] int lettersPerSecond;
    [Tooltip("Fast-forward multiplier while the advance button is held. 0 uses the manager default.")]
    [Min(0)]
    [SerializeField] int fastForwardMultiplier;

    [Header("Timing")]
    [Tooltip("If enabled, the bubble waits for player input after a line finishes. If disabled, it advances by timer.")]
    [SerializeField] bool waitForAdvance = true;
    [Tooltip("Minimum seconds a completed line stays visible before auto-advance can close it.")]
    [Min(0f)]
    [SerializeField] float minimumVisibleSeconds = 0.8f;
    [Tooltip("Extra seconds added per character when auto-advancing.")]
    [Min(0f)]
    [SerializeField] float secondsPerCharacter = 0.025f;
    [Tooltip("Small delay after closing a bubble before the next line opens.")]
    [Min(0f)]
    [SerializeField] float closeDelaySeconds = 0.05f;

    [Header("Display Hints")]
    [Tooltip("If enabled, future bubble UI can show the speaker name.")]
    [SerializeField] bool showSpeakerName = true;
    [Tooltip("If enabled, future bubble UI can show an advance icon when the line has finished typing.")]
    [SerializeField] bool showAdvanceIndicator = true;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public Vector3 WorldOffset => worldOffset;
    public bool ClampToScreen => clampToScreen;
    public int LettersPerSecond => lettersPerSecond;
    public int FastForwardMultiplier => fastForwardMultiplier;
    public bool WaitForAdvance => waitForAdvance;
    public float MinimumVisibleSeconds => minimumVisibleSeconds;
    public float SecondsPerCharacter => secondsPerCharacter;
    public float CloseDelaySeconds => closeDelaySeconds;
    public bool ShowSpeakerName => showSpeakerName;
    public bool ShowAdvanceIndicator => showAdvanceIndicator;
}
