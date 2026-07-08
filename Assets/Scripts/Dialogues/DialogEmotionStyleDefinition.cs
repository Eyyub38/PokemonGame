using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogues/Dialog Emotion Style")]
public class DialogEmotionStyleDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Name shown in editor/debug output. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Default style used when a line emotion has no exact match.")]
    [SerializeField] DialogEmotionStyle fallbackStyle = new DialogEmotionStyle();
    [Tooltip("Styles mapped by Dialog Line Emotion.")]
    [SerializeField] List<DialogEmotionStyle> styles = new List<DialogEmotionStyle>();

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public DialogEmotionStyle FallbackStyle => fallbackStyle;
    public IReadOnlyList<DialogEmotionStyle> Styles => styles;

    public DialogEmotionStyle GetStyle(DialogLineEmotion emotion) {
        var style = styles.FirstOrDefault(entry => entry != null && entry.Emotion == emotion);
        return style ?? fallbackStyle;
    }
}

[Serializable]
public class DialogEmotionStyle {
    [Tooltip("Emotion this style represents.")]
    [SerializeField] DialogLineEmotion emotion = DialogLineEmotion.Neutral;
    [Tooltip("Color applied to dialog panel or speech bubble background when enabled by the UI bridge.")]
    [SerializeField] Color backgroundColor = Color.white;
    [Tooltip("Color applied to dialog text when enabled by the UI bridge.")]
    [SerializeField] Color textColor = Color.white;
    [Tooltip("Optional icon shown near the dialog text.")]
    [SerializeField] Sprite icon = null;
    [Tooltip("Animator trigger fired when this emotion appears.")]
    [SerializeField] string animatorTrigger = string.Empty;
    [Tooltip("Optional UI sound played when this emotion appears.")]
    [SerializeField] AudioClip sound = null;

    public DialogLineEmotion Emotion => emotion;
    public Color BackgroundColor => backgroundColor;
    public Color TextColor => textColor;
    public Sprite Icon => icon;
    public string AnimatorTrigger => animatorTrigger;
    public AudioClip Sound => sound;
}
