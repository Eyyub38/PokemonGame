using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DialogGraphPlayer : MonoBehaviour {
    [Header("Runtime")]
    [Tooltip("If enabled, this player survives scene loads.")]
    [SerializeField] bool dontDestroyOnLoad = true;
    [Tooltip("Maximum nodes that can be visited during one graph play before it is stopped as a safety guard.")]
    [Min(1)]
    [SerializeField] int defaultMaxNodesPerPlay = 40;
    [Tooltip("If enabled, missing PlayerDialogGraphLog is added to the player when graph history is written.")]
    [SerializeField] bool createMissingPlayerLog = true;
    [Tooltip("If enabled, graph playback writes useful steps to GameDebug.")]
    [SerializeField] bool writeDebugLog = true;

    Coroutine activeRoutine;

    public static DialogGraphPlayer i { get; private set; }
    public bool IsPlaying { get; private set; }
    public DialogGraphPlaybackContext ActiveContext { get; private set; }
    public DialogGraphPlaybackResult LastResult { get; private set; }

    public event Action<DialogGraphPlaybackContext> OnGraphStarted;
    public event Action<DialogGraphPlaybackResult> OnGraphFinished;
    public event Action<DialogGraphNode> OnNodeStarted;
    public event Action<DialogGraphLinePlayback> OnLineStarted;
    public event Action<DialogGraphChoicePlayback> OnChoiceSelected;

    void Awake() {
        if(i != null && i != this) {
            Destroy(gameObject);
            return;
        }

        i = this;
        if(dontDestroyOnLoad) {
            DontDestroyOnLoad(gameObject);
        }
    }

    public static DialogGraphPlayer Ensure() {
        if(i != null) {
            return i;
        }

        var existing = FindAnyObjectByType<DialogGraphPlayer>();
        if(existing != null) {
            return existing;
        }

        return new GameObject("DialogGraphPlayer").AddComponent<DialogGraphPlayer>();
    }

    public IEnumerator Play(DialogGraphDefinition graph, DialogGraphPlaybackOptions options = null, Action<DialogGraphPlaybackResult> onCompleted = null) {
        if(IsPlaying) {
            var blockedResult = DialogGraphPlaybackResult.Blocked(graph, "Another dialog graph is already playing.");
            LastResult = blockedResult;
            onCompleted?.Invoke(blockedResult);
            yield break;
        }

        activeRoutine = StartCoroutine(RunGraph(graph, options, onCompleted));
        yield return activeRoutine;
    }

    IEnumerator RunGraph(DialogGraphDefinition graph, DialogGraphPlaybackOptions options, Action<DialogGraphPlaybackResult> onCompleted) {
        options ??= new DialogGraphPlaybackOptions();
        var result = DialogGraphPlaybackResult.Create(graph);

        if(graph == null) {
            result.Finish(false, "Dialog graph is missing.");
            Complete(result, onCompleted);
            yield break;
        }

        var startNode = graph.GetStartNode();
        if(startNode == null) {
            result.Finish(false, $"{graph.DisplayName} has no start node.");
            Complete(result, onCompleted);
            yield break;
        }

        IsPlaying = true;
        ActiveContext = DialogGraphPlaybackContext.Create(graph, options);
        PublishGraphEvent(graph, "started", $"{graph.DisplayName} started.", options.Source);
        OnGraphStarted?.Invoke(ActiveContext);

        if(writeDebugLog) {
            GameDebug.Step($"{graph.DisplayName} dialog graph started.", GameDebugCategory.NPC, options.Source, "DialogGraphPlayer");
        }

        var visitedThisPlay = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var currentNode = startNode;
        int maxNodes = Mathf.Max(1, options.MaxNodesPerPlay > 0 ? options.MaxNodesPerPlay : defaultMaxNodesPerPlay);

        for(int step = 0; step < maxNodes && currentNode != null; step++) {
            if(!CanVisitNode(currentNode, visitedThisPlay, out var visitFailure)) {
                result.Finish(false, visitFailure);
                break;
            }

            visitedThisPlay[currentNode.Id] = visitedThisPlay.TryGetValue(currentNode.Id, out var count) ? count + 1 : 1;
            result.visitedNodeIds.Add(currentNode.Id);
            OnNodeStarted?.Invoke(currentNode);
            RecordNode(graph, currentNode, options);
            ApplyEffects(currentNode.OnEnterEffects, options, $"dialog.graph.{graph.Id}.{currentNode.Id}.enter", $"{currentNode.DisplayName} entered.");
            PublishGraphEvent(graph, $"node.{currentNode.Id}", $"{graph.DisplayName} node {currentNode.DisplayName}.", options.Source);

            yield return PlayNodeLines(graph, currentNode, options);

            var availableChoices = currentNode.GetAvailableChoices(ActiveContext.DialogContext);
            if(availableChoices.Count > 0) {
                DialogGraphChoice selectedChoice = null;
                yield return SelectChoice(graph, currentNode, availableChoices, options, choice => selectedChoice = choice);

                if(selectedChoice == null) {
                    result.Finish(false, "No dialog choice was selected.");
                    break;
                }

                result.selectedChoiceIds.Add(selectedChoice.Id);
                RecordChoice(graph, currentNode, selectedChoice, options);
                ApplyEffects(selectedChoice.Effects, options, $"dialog.graph.{graph.Id}.{currentNode.Id}.{selectedChoice.Id}", $"{selectedChoice.DisplayText} selected.");
                OnChoiceSelected?.Invoke(new DialogGraphChoicePlayback(graph, currentNode, selectedChoice, options));
                PublishGraphEvent(graph, $"choice.{selectedChoice.Id}", $"{graph.DisplayName} choice {selectedChoice.DisplayText}.", options.Source);

                ApplyEffects(currentNode.OnExitEffects, options, $"dialog.graph.{graph.Id}.{currentNode.Id}.exit", $"{currentNode.DisplayName} exited.");

                if(selectedChoice.StayOnSameNode) {
                    // Keep current node for another pass.
                } else if(!string.IsNullOrWhiteSpace(selectedChoice.NextNodeId)) {
                    currentNode = graph.GetNode(selectedChoice.NextNodeId);
                    if(currentNode == null) {
                        result.Finish(false, $"Dialog graph node '{selectedChoice.NextNodeId}' could not be found.");
                        break;
                    }
                } else {
                    currentNode = null;
                }
            } else {
                ApplyEffects(currentNode.OnExitEffects, options, $"dialog.graph.{graph.Id}.{currentNode.Id}.exit", $"{currentNode.DisplayName} exited.");

                if(!string.IsNullOrWhiteSpace(currentNode.AutoNextNodeId)) {
                    currentNode = graph.GetNode(currentNode.AutoNextNodeId);
                    if(currentNode == null) {
                        result.Finish(false, $"Dialog graph node '{currentNode.AutoNextNodeId}' could not be found.");
                        break;
                    }
                } else {
                    currentNode = currentNode.EndConversation ? null : null;
                }
            }

            result.success = true;
        }

        if(IsPlaying && result.success && string.IsNullOrWhiteSpace(result.message)) {
            result.Finish(true, $"{graph.DisplayName} completed.");
        } else if(IsPlaying && !result.success && string.IsNullOrWhiteSpace(result.message)) {
            result.Finish(false, $"{graph.DisplayName} stopped.");
        }

        if(result.success && currentNode != null && result.visitedNodeIds.Count >= maxNodes) {
            result.Finish(false, $"{graph.DisplayName} reached the node safety limit ({maxNodes}).");
        }

        PublishGraphEvent(graph, "finished", result.message, options.Source);
        Complete(result, onCompleted);
    }

    IEnumerator PlayNodeLines(DialogGraphDefinition graph, DialogGraphNode node, DialogGraphPlaybackOptions options) {
        if(node.Lines == null || node.Lines.Count == 0) {
            yield break;
        }

        var presentation = options.ResolvePresentation(graph);
        for(int i = 0; i < node.Lines.Count; i++) {
            var line = node.Lines[i];
            if(line == null || string.IsNullOrWhiteSpace(line.Text)) {
                continue;
            }

            var fallbackSpeaker = options.ResolveSpeakerName();
            var speakerName = !string.IsNullOrWhiteSpace(line.SpeakerName) ? line.SpeakerName : fallbackSpeaker;
            var text = line.BuildDisplayText(fallbackSpeaker, presentation);
            OnLineStarted?.Invoke(new DialogGraphLinePlayback(graph, node, line, i, presentation, options));

            yield return DialogPresenter.ShowText(
                text,
                presentation,
                options.Source,
                options.Initiator,
                speakerName,
                options.SpeechBubbleStyle,
                options.SpeechBubbleAnchor);
        }
    }

    IEnumerator SelectChoice(DialogGraphDefinition graph, DialogGraphNode node, List<DialogGraphChoice> choices, DialogGraphPlaybackOptions options, Action<DialogGraphChoice> onSelected) {
        if(choices == null || choices.Count == 0) {
            yield break;
        }

        if(DialogManager.i == null) {
            onSelected?.Invoke(choices[0]);
            yield break;
        }

        int selectedIndex = -1;
        var labels = choices.Select(choice => BuildChoiceLabel(choice)).ToList();
        string prompt = !string.IsNullOrWhiteSpace(node.ChoicePrompt) ? node.ChoicePrompt : graph.DefaultChoicePrompt;
        yield return DialogManager.i.ShowDialogText(
            prompt,
            waitForInput: false,
            autoClose: true,
            choices: labels,
            onChoiceSelected: index => selectedIndex = index);

        if(selectedIndex >= 0 && selectedIndex < choices.Count) {
            onSelected?.Invoke(choices[selectedIndex]);
        }
    }

    string BuildChoiceLabel(DialogGraphChoice choice) {
        if(choice == null) {
            return string.Empty;
        }

        return choice.Intent == DialogChoiceIntent.Neutral
            ? choice.DisplayText
            : $"{choice.DisplayText} [{choice.Intent}]";
    }

    bool CanVisitNode(DialogGraphNode node, Dictionary<string, int> visitedThisPlay, out string failureMessage) {
        failureMessage = string.Empty;
        if(node == null) {
            failureMessage = "Dialog node is missing.";
            return false;
        }

        if(string.IsNullOrWhiteSpace(node.Id)) {
            failureMessage = "Dialog node has no id.";
            return false;
        }

        if(node.MaxVisitsPerPlay <= 0) {
            return true;
        }

        visitedThisPlay.TryGetValue(node.Id, out var count);
        if(count < node.MaxVisitsPerPlay) {
            return true;
        }

        failureMessage = $"{node.DisplayName} reached its visit limit.";
        return false;
    }

    void ApplyEffects(DialogGraphEffects effects, DialogGraphPlaybackOptions options, string fallbackEventId, string fallbackMessage) {
        if(effects == null || !effects.HasAnyEffect) {
            return;
        }

        effects.Apply(options.Player, options.Source, fallbackEventId, fallbackMessage);
    }

    void RecordNode(DialogGraphDefinition graph, DialogGraphNode node, DialogGraphPlaybackOptions options) {
        if(graph == null || node == null || !graph.WritePlayerHistory || options.Player == null) {
            return;
        }

        var log = options.Player.GetComponent<PlayerDialogGraphLog>();
        if(log == null && createMissingPlayerLog) {
            log = options.Player.gameObject.AddComponent<PlayerDialogGraphLog>();
        }

        log?.RecordNode(graph, node, options.ResolveSpeakerId());
    }

    void RecordChoice(DialogGraphDefinition graph, DialogGraphNode node, DialogGraphChoice choice, DialogGraphPlaybackOptions options) {
        if(graph == null || choice == null || !graph.WritePlayerHistory || options.Player == null) {
            return;
        }

        var log = options.Player.GetComponent<PlayerDialogGraphLog>();
        if(log == null && createMissingPlayerLog) {
            log = options.Player.gameObject.AddComponent<PlayerDialogGraphLog>();
        }

        log?.RecordChoice(graph, node, choice, options.ResolveSpeakerId());
    }

    void PublishGraphEvent(DialogGraphDefinition graph, string phase, string message, UnityEngine.Object context) {
        if(graph == null || !graph.PublishEvents) {
            return;
        }

        GameEventBus.Publish(
            $"dialog.graph.{graph.Id}.{phase}",
            message,
            GameEventCategory.Dialogue,
            GameEventImportance.Trace,
            context,
            "DialogGraphPlayer",
            GameEventScope.Scene,
            showInFeed: false,
            writeToDebugLog: writeDebugLog);
    }

    void Complete(DialogGraphPlaybackResult result, Action<DialogGraphPlaybackResult> onCompleted) {
        LastResult = result;
        IsPlaying = false;
        ActiveContext = null;
        activeRoutine = null;
        OnGraphFinished?.Invoke(result);
        onCompleted?.Invoke(result);

        if(writeDebugLog && result != null) {
            if(result.success) {
                GameDebug.Success(result.message, GameDebugCategory.NPC, this, "DialogGraphPlayer");
            } else {
                GameDebug.Warning(result.message, GameDebugCategory.NPC, this, "DialogGraphPlayer");
            }
        }
    }
}

[Serializable]
public class DialogGraphPlaybackOptions {
    [Tooltip("Player used for conditions, logs and rewards. Empty uses PlayerController.i.")]
    public PlayerController Player;
    [Tooltip("Transform that initiated the conversation. Empty uses player transform.")]
    public Transform Initiator;
    [Tooltip("Component that owns this conversation, usually an NPC or source object.")]
    public Component Source;
    [Tooltip("Speaker name used by lines that do not override their own speaker.")]
    public string SpeakerName;
    [Tooltip("Speaker id used by logs, memory and debug output.")]
    public string SpeakerId;
    [Tooltip("Presentation override. If disabled, the graph default is used.")]
    public bool OverridePresentation;
    [Tooltip("Presentation used when Override Presentation is enabled.")]
    public DialogPresentationMode Presentation = DialogPresentationMode.SpeechBubble;
    [Tooltip("Speech bubble style used for speech-bubble presentation.")]
    public SpeechBubbleStyleDefinition SpeechBubbleStyle;
    [Tooltip("Optional speech bubble anchor.")]
    public Transform SpeechBubbleAnchor;
    [Tooltip("Maximum node visits for this playback. 0 uses DialogGraphPlayer default.")]
    public int MaxNodesPerPlay;

    public DialogPresentationMode ResolvePresentation(DialogGraphDefinition graph) {
        return OverridePresentation ? Presentation : graph != null ? graph.DefaultPresentation : Presentation;
    }

    public string ResolveSpeakerName() {
        if(!string.IsNullOrWhiteSpace(SpeakerName)) {
            return SpeakerName;
        }

        if(Source != null) {
            return Source.name;
        }

        return Player != null ? Player.Name : string.Empty;
    }

    public string ResolveSpeakerId() {
        if(!string.IsNullOrWhiteSpace(SpeakerId)) {
            return SpeakerId;
        }

        if(Source != null) {
            return Source.name;
        }

        return ResolveSpeakerName();
    }
}

public class DialogGraphPlaybackContext {
    public DialogGraphDefinition Graph { get; private set; }
    public DialogGraphPlaybackOptions Options { get; private set; }
    public DialogContext DialogContext { get; private set; }

    public static DialogGraphPlaybackContext Create(DialogGraphDefinition graph, DialogGraphPlaybackOptions options) {
        options ??= new DialogGraphPlaybackOptions();
        var player = options.Player != null ? options.Player : PlayerController.i;
        var initiator = options.Initiator != null ? options.Initiator : player != null ? player.transform : null;
        options.Player = player;
        options.Initiator = initiator;

        return new DialogGraphPlaybackContext {
            Graph = graph,
            Options = options,
            DialogContext = new DialogContext(player, initiator, options.Source != null ? options.Source.gameObject : null, options.Source, options.ResolveSpeakerId())
        };
    }
}

public class DialogGraphLinePlayback {
    public DialogGraphDefinition Graph { get; private set; }
    public DialogGraphNode Node { get; private set; }
    public DialogGraphLine Line { get; private set; }
    public int LineIndex { get; private set; }
    public DialogPresentationMode Presentation { get; private set; }
    public DialogGraphPlaybackOptions Options { get; private set; }

    public DialogGraphLinePlayback(DialogGraphDefinition graph, DialogGraphNode node, DialogGraphLine line, int lineIndex, DialogPresentationMode presentation, DialogGraphPlaybackOptions options) {
        Graph = graph;
        Node = node;
        Line = line;
        LineIndex = lineIndex;
        Presentation = presentation;
        Options = options;
    }
}

public class DialogGraphChoicePlayback {
    public DialogGraphDefinition Graph { get; private set; }
    public DialogGraphNode Node { get; private set; }
    public DialogGraphChoice Choice { get; private set; }
    public DialogGraphPlaybackOptions Options { get; private set; }

    public DialogGraphChoicePlayback(DialogGraphDefinition graph, DialogGraphNode node, DialogGraphChoice choice, DialogGraphPlaybackOptions options) {
        Graph = graph;
        Node = node;
        Choice = choice;
        Options = options;
    }
}

[Serializable]
public class DialogGraphPlaybackResult {
    [Tooltip("If enabled, playback finished without a blocking error.")]
    public bool success;
    [Tooltip("Graph id that was played.")]
    public string graphId;
    [Tooltip("Graph display name that was played.")]
    public string graphName;
    [Tooltip("Readable result text.")]
    public string message;
    [Tooltip("Node ids visited during playback.")]
    public List<string> visitedNodeIds = new List<string>();
    [Tooltip("Choice ids selected during playback.")]
    public List<string> selectedChoiceIds = new List<string>();

    public static DialogGraphPlaybackResult Create(DialogGraphDefinition graph) {
        return new DialogGraphPlaybackResult {
            graphId = graph != null ? graph.Id : string.Empty,
            graphName = graph != null ? graph.DisplayName : string.Empty
        };
    }

    public static DialogGraphPlaybackResult Blocked(DialogGraphDefinition graph, string message) {
        var result = Create(graph);
        result.Finish(false, message);
        return result;
    }

    public void Finish(bool succeeded, string resultMessage) {
        success = succeeded;
        message = resultMessage;
    }
}
