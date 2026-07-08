using UnityEngine;
using UnityEngine.UI;

public class DialogEmotionUIBridge : MonoBehaviour {
    [Header("Style")]
    [Tooltip("Emotion style table used to translate dialog line emotions into UI color/icon/animation.")]
    [SerializeField] DialogEmotionStyleDefinition styleDefinition = null;
    [Tooltip("If enabled, this bridge listens to DialogGraphPlayer.Ensure() when enabled.")]
    [SerializeField] bool autoSubscribe = true;
    [Tooltip("If enabled, UI targets are reset when a graph finishes.")]
    [SerializeField] bool clearOnGraphFinished = true;

    [Header("UI Targets")]
    [Tooltip("Optional panel or bubble background image tinted by the active emotion style.")]
    [SerializeField] Image backgroundImage = null;
    [Tooltip("Optional text tinted by the active emotion style.")]
    [SerializeField] Text dialogText = null;
    [Tooltip("Optional text that receives the current speaker name.")]
    [SerializeField] Text speakerNameText = null;
    [Tooltip("Optional image that receives the active emotion icon.")]
    [SerializeField] Image emotionIconImage = null;
    [Tooltip("Optional animator that receives emotion trigger names.")]
    [SerializeField] Animator animator = null;
    [Tooltip("Optional audio source used for emotion style sounds.")]
    [SerializeField] AudioSource audioSource = null;

    [Header("Behavior")]
    [Tooltip("If enabled, DialogGraphLine custom Emotion Color overrides the style background color.")]
    [SerializeField] bool allowLineColorOverride = true;
    [Tooltip("If enabled, background image color is changed when a style is applied.")]
    [SerializeField] bool applyBackgroundColor = true;
    [Tooltip("If enabled, dialog text color is changed when a style is applied.")]
    [SerializeField] bool applyTextColor = false;
    [Tooltip("If enabled, emotion icon object is hidden when the active style has no icon.")]
    [SerializeField] bool hideIconWhenEmpty = true;

    Color initialBackgroundColor;
    Color initialTextColor;
    Sprite initialIcon;
    bool initialIconActive;

    void Awake() {
        if(backgroundImage != null) {
            initialBackgroundColor = backgroundImage.color;
        }

        if(dialogText != null) {
            initialTextColor = dialogText.color;
        }

        if(emotionIconImage != null) {
            initialIcon = emotionIconImage.sprite;
            initialIconActive = emotionIconImage.gameObject.activeSelf;
        }
    }

    void OnEnable() {
        if(autoSubscribe) {
            Subscribe(DialogGraphPlayer.Ensure());
        }
    }

    void OnDisable() {
        if(DialogGraphPlayer.i != null) {
            Unsubscribe(DialogGraphPlayer.i);
        }
    }

    public void Subscribe(DialogGraphPlayer player) {
        if(player == null) {
            return;
        }

        player.OnLineStarted -= HandleLineStarted;
        player.OnGraphFinished -= HandleGraphFinished;
        player.OnLineStarted += HandleLineStarted;
        player.OnGraphFinished += HandleGraphFinished;
    }

    public void Unsubscribe(DialogGraphPlayer player) {
        if(player == null) {
            return;
        }

        player.OnLineStarted -= HandleLineStarted;
        player.OnGraphFinished -= HandleGraphFinished;
    }

    public void ApplyLine(DialogGraphLinePlayback playback) {
        HandleLineStarted(playback);
    }

    void HandleLineStarted(DialogGraphLinePlayback playback) {
        if(playback == null || playback.Line == null) {
            return;
        }

        var style = styleDefinition != null ? styleDefinition.GetStyle(playback.Line.Emotion) : null;
        ApplyStyle(playback, style);
    }

    void ApplyStyle(DialogGraphLinePlayback playback, DialogEmotionStyle style) {
        var line = playback.Line;
        if(backgroundImage != null && applyBackgroundColor) {
            if(allowLineColorOverride && line.UseEmotionColor) {
                backgroundImage.color = line.EmotionColor;
            } else if(style != null) {
                backgroundImage.color = style.BackgroundColor;
            }
        }

        if(dialogText != null && applyTextColor && style != null) {
            dialogText.color = style.TextColor;
        }

        if(speakerNameText != null) {
            speakerNameText.text = !string.IsNullOrWhiteSpace(line.SpeakerName)
                ? line.SpeakerName
                : playback.Options != null ? playback.Options.ResolveSpeakerName() : string.Empty;
        }

        if(emotionIconImage != null) {
            emotionIconImage.sprite = style != null ? style.Icon : null;
            if(hideIconWhenEmpty) {
                emotionIconImage.gameObject.SetActive(emotionIconImage.sprite != null);
            }
        }

        if(animator != null && style != null && !string.IsNullOrWhiteSpace(style.AnimatorTrigger)) {
            animator.SetTrigger(style.AnimatorTrigger);
        }

        if(audioSource != null && style != null && style.Sound != null) {
            audioSource.PlayOneShot(style.Sound);
        }
    }

    void HandleGraphFinished(DialogGraphPlaybackResult result) {
        if(clearOnGraphFinished) {
            ResetTargets();
        }
    }

    public void ResetTargets() {
        if(backgroundImage != null && applyBackgroundColor) {
            backgroundImage.color = initialBackgroundColor;
        }

        if(dialogText != null && applyTextColor) {
            dialogText.color = initialTextColor;
        }

        if(speakerNameText != null) {
            speakerNameText.text = string.Empty;
        }

        if(emotionIconImage != null) {
            emotionIconImage.sprite = initialIcon;
            if(hideIconWhenEmpty) {
                emotionIconImage.gameObject.SetActive(initialIconActive);
            }
        }
    }
}
