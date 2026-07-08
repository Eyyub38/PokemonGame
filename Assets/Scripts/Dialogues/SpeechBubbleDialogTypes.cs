using System;
using UnityEngine;

public enum DialogPresentationMode {
    ClassicDialogBox,
    SpeechBubble
}

[Serializable]
public class SpeechBubbleDialogOptions {
    [Tooltip("Character or object that is speaking.")]
    public Transform speaker;
    [Tooltip("Optional custom anchor point for the bubble. Empty uses the speaker transform plus the style offset.")]
    public Transform anchor;
    [Tooltip("Name passed to future bubble UI. Empty uses the speaker GameObject name.")]
    public string speakerName;
    [Tooltip("Optional style asset that defines typing/timing/display hints.")]
    public SpeechBubbleStyleDefinition style;
    [Tooltip("Object that requested this dialog. Used by debug/event systems.")]
    public Component source;
    [Tooltip("If enabled, this line waits for player input when possible.")]
    public bool waitForAdvance = true;
    [Tooltip("If enabled, the bubble closes after each line finishes.")]
    public bool autoClose = true;
    [Tooltip("If enabled, this request can interrupt a currently running bubble dialog.")]
    public bool interruptCurrent;
    [Tooltip("If enabled, speech bubble activity is sent to debug/event systems.")]
    public bool publishEvent = true;

    public static SpeechBubbleDialogOptions ForSpeaker(Component source, Transform initiator, string speakerName, SpeechBubbleStyleDefinition style, Transform anchor) {
        return new SpeechBubbleDialogOptions {
            speaker = source != null ? source.transform : initiator,
            anchor = anchor,
            speakerName = speakerName,
            style = style,
            source = source,
            waitForAdvance = style == null || style.WaitForAdvance,
            autoClose = true
        };
    }
}

public class SpeechBubbleDialogRequest {
    public string Id { get; private set; }
    public Transform Speaker { get; private set; }
    public Transform Anchor { get; private set; }
    public string SpeakerName { get; private set; }
    public SpeechBubbleStyleDefinition Style { get; private set; }
    public Component Source { get; private set; }
    public string FullText { get; private set; }
    public string VisibleText { get; private set; }
    public int LineIndex { get; private set; }
    public int LineCount { get; private set; }
    public bool IsLineComplete { get; private set; }
    public bool ShowAdvanceIndicator { get; private set; }

    public Vector3 AnchorPosition {
        get {
            if(Anchor != null) {
                return Anchor.position;
            }

            var speakerPosition = Speaker != null ? Speaker.position : Vector3.zero;
            var offset = Style != null ? Style.WorldOffset : Vector3.up;
            return speakerPosition + offset;
        }
    }

    public SpeechBubbleDialogRequest(SpeechBubbleDialogOptions options, string fullText, int lineIndex, int lineCount) {
        options ??= new SpeechBubbleDialogOptions();

        Id = Guid.NewGuid().ToString("N");
        Speaker = options.speaker;
        Anchor = options.anchor;
        SpeakerName = !string.IsNullOrWhiteSpace(options.speakerName)
            ? options.speakerName
            : options.speaker != null ? options.speaker.name : string.Empty;
        Style = options.style;
        Source = options.source;
        FullText = fullText ?? string.Empty;
        VisibleText = string.Empty;
        LineIndex = lineIndex;
        LineCount = lineCount;
    }

    public void SetVisibleText(string visibleText, bool isLineComplete, bool showAdvanceIndicator) {
        VisibleText = visibleText ?? string.Empty;
        IsLineComplete = isLineComplete;
        ShowAdvanceIndicator = showAdvanceIndicator;
    }
}
