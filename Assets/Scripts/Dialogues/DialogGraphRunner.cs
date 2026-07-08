using System.Collections;
using UnityEngine;

public class DialogGraphRunner : MonoBehaviour, Interactable, IPlayerTriggerable {
    [Header("Graph")]
    [Tooltip("Conversation graph played by this source.")]
    [SerializeField] DialogGraphDefinition graph = null;
    [Tooltip("If enabled, this runner overrides the graph default presentation.")]
    [SerializeField] bool overridePresentation = false;
    [Tooltip("Presentation used when Override Presentation is enabled.")]
    [SerializeField] DialogPresentationMode presentation = DialogPresentationMode.SpeechBubble;

    [Header("Speaker")]
    [Tooltip("Speaker name used by graph lines that do not define their own speaker. Empty uses NPCController display name or GameObject name.")]
    [SerializeField] string speakerName = string.Empty;
    [Tooltip("Stable speaker id used by dialog graph logs. Empty uses NPCMemoryProfile id, NPCController display name or GameObject name.")]
    [SerializeField] string speakerId = string.Empty;
    [Tooltip("Speech bubble style used when presentation resolves to Speech Bubble.")]
    [SerializeField] SpeechBubbleStyleDefinition speechBubbleStyle = null;
    [Tooltip("Optional transform used as speech bubble anchor.")]
    [SerializeField] Transform speechBubbleAnchor = null;

    [Header("Trigger")]
    [Tooltip("If enabled, interacting with this object starts the graph.")]
    [SerializeField] bool playOnInteract = true;
    [Tooltip("If enabled, entering this trigger starts the graph.")]
    [SerializeField] bool playOnTrigger = false;
    [Tooltip("Controls IPlayerTriggerable repeat behavior.")]
    [SerializeField] bool triggerRepeatedly = false;

    [Header("Safety")]
    [Tooltip("Maximum nodes that can be visited from this runner before playback stops. 0 uses DialogGraphPlayer default.")]
    [Min(0)]
    [SerializeField] int maxNodesPerPlay = 0;
    [Tooltip("If enabled, blocked graph starts show DialogManager feedback.")]
    [SerializeField] bool showBlockedFeedback = true;
    [Tooltip("If enabled, runner starts/results are written to GameDebug.")]
    [SerializeField] bool writeToDebug = true;

    public bool TriggerRepeatedly => triggerRepeatedly;
    public DialogGraphDefinition Graph => graph;

    public void OnPlayerTriggered(PlayerController player) {
        if(playOnTrigger && player != null) {
            StartCoroutine(Play(player.transform));
        }
    }

    public IEnumerator Interact(Transform initiator) {
        if(!playOnInteract) {
            yield break;
        }

        yield return Play(initiator);
    }

    [ContextMenu("Play Dialog Graph")]
    public void PlayFromContextMenu() {
        StartCoroutine(Play(PlayerController.i != null ? PlayerController.i.transform : null));
    }

    public IEnumerator Play(Transform initiator) {
        var player = initiator != null ? initiator.GetComponent<PlayerController>() : PlayerController.i;
        if(graph == null) {
            yield return ShowBlocked("No dialog graph is assigned.");
            yield break;
        }

        if(player == null) {
            yield return ShowBlocked("A player is required to play this dialog graph.");
            yield break;
        }

        if(writeToDebug) {
            GameDebug.Step($"{ResolveSpeakerName()} started dialog graph {graph.DisplayName}.", GameDebugCategory.NPC, this, "DialogGraphRunner");
        }

        var options = new DialogGraphPlaybackOptions {
            Player = player,
            Initiator = initiator != null ? initiator : player.transform,
            Source = this,
            SpeakerName = ResolveSpeakerName(),
            SpeakerId = ResolveSpeakerId(),
            OverridePresentation = overridePresentation,
            Presentation = presentation,
            SpeechBubbleStyle = speechBubbleStyle,
            SpeechBubbleAnchor = speechBubbleAnchor,
            MaxNodesPerPlay = maxNodesPerPlay
        };

        DialogGraphPlaybackResult result = null;
        yield return DialogGraphPlayer.Ensure().Play(graph, options, playbackResult => result = playbackResult);

        if(writeToDebug && result != null && !result.success) {
            GameDebug.Warning(result.message, GameDebugCategory.NPC, this, "DialogGraphRunner");
        }
    }

    string ResolveSpeakerName() {
        if(!string.IsNullOrWhiteSpace(speakerName)) {
            return speakerName;
        }

        var npc = GetComponent<NPCController>();
        if(npc != null) {
            return npc.DisplayName;
        }

        var companion = GetComponent<CompanionController>();
        if(companion != null) {
            return companion.CompanionName;
        }

        return name;
    }

    string ResolveSpeakerId() {
        if(!string.IsNullOrWhiteSpace(speakerId)) {
            return speakerId;
        }

        var memoryProfile = GetComponent<NPCMemoryProfile>();
        if(memoryProfile != null) {
            return memoryProfile.NpcId;
        }

        return ResolveSpeakerName();
    }

    IEnumerator ShowBlocked(string message) {
        if(writeToDebug) {
            GameDebug.Warning(message, GameDebugCategory.NPC, this, "DialogGraphRunner");
        }

        if(showBlockedFeedback && DialogManager.i != null && !string.IsNullOrWhiteSpace(message)) {
            yield return DialogManager.i.ShowDialogText(message);
        }
    }
}
