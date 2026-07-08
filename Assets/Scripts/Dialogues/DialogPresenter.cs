using System.Collections;
using UnityEngine;

public static class DialogPresenter {
    public static IEnumerator ShowDialog(
        Dialog dialog,
        DialogPresentationMode presentationMode,
        Component source,
        Transform initiator,
        string speakerName = null,
        SpeechBubbleStyleDefinition speechBubbleStyle = null,
        Transform speechBubbleAnchor = null
    ) {
        if(dialog == null) {
            yield break;
        }

        if(presentationMode == DialogPresentationMode.SpeechBubble && SpeechBubbleDialogManager.i != null) {
            var options = SpeechBubbleDialogOptions.ForSpeaker(source, initiator, speakerName, speechBubbleStyle, speechBubbleAnchor);
            yield return SpeechBubbleDialogManager.i.ShowDialog(dialog, options);
            yield break;
        }

        if(DialogManager.i != null) {
            yield return DialogManager.i.ShowDialog(dialog);
        }
    }

    public static IEnumerator ShowText(
        string text,
        DialogPresentationMode presentationMode,
        Component source,
        Transform initiator,
        string speakerName = null,
        SpeechBubbleStyleDefinition speechBubbleStyle = null,
        Transform speechBubbleAnchor = null
    ) {
        if(string.IsNullOrWhiteSpace(text)) {
            yield break;
        }

        if(presentationMode == DialogPresentationMode.SpeechBubble && SpeechBubbleDialogManager.i != null) {
            var options = SpeechBubbleDialogOptions.ForSpeaker(source, initiator, speakerName, speechBubbleStyle, speechBubbleAnchor);
            yield return SpeechBubbleDialogManager.i.ShowText(text, options);
            yield break;
        }

        if(DialogManager.i != null) {
            yield return DialogManager.i.ShowDialogText(text);
        }
    }
}
