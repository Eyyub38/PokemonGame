using System.Collections;
using UnityEngine;

public class ConditionalDialogSelector : MonoBehaviour {
    [Header("Dialog")]
    [Tooltip("Conditional dialog definition evaluated when SelectDialog or ShowDialog is called.")]
    [SerializeField] ConditionalDialogDefinition conditionalDialog;
    [Tooltip("Fallback dialog used when the conditional definition is missing or has no matching dialog.")]
    [SerializeField] Dialog fallbackDialog;
    [Tooltip("Optional id used by logs and future UI when this speaker needs a stable key.")]
    [SerializeField] string speakerId;

    [Header("Presentation")]
    [Tooltip("How this selector presents dialog when ShowDialog is called.")]
    [SerializeField] DialogPresentationMode presentationMode = DialogPresentationMode.ClassicDialogBox;
    [Tooltip("Style used when presentation mode is Speech Bubble.")]
    [SerializeField] SpeechBubbleStyleDefinition speechBubbleStyle;
    [Tooltip("Optional transform used as the bubble anchor. Empty uses this transform plus the style offset.")]
    [SerializeField] Transform speechBubbleAnchor;

    public ConditionalDialogDefinition ConditionalDialog => conditionalDialog;
    public Dialog FallbackDialog => fallbackDialog;
    public string SpeakerId => speakerId;

    public Dialog SelectDialog(Transform initiator = null) {
        var context = DialogContext.FromInteraction(this, initiator, speakerId);
        var selectedDialog = conditionalDialog != null ? conditionalDialog.SelectDialog(context) : null;
        return selectedDialog ?? fallbackDialog;
    }

    public IEnumerator ShowDialog(Transform initiator = null) {
        var selectedDialog = SelectDialog(initiator);
        yield return DialogPresenter.ShowDialog(selectedDialog, presentationMode, this, initiator, speakerId, speechBubbleStyle, speechBubbleAnchor);
    }
}
