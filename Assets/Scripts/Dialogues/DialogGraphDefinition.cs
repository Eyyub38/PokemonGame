using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum DialogLineEmotion {
    Neutral,
    Happy,
    Sad,
    Angry,
    Worried,
    Surprised,
    Thinking,
    Shy,
    Confident,
    Suspicious,
    Excited,
    Serious
}

public enum DialogChoiceIntent {
    Neutral,
    Friendly,
    Direct,
    Curious,
    Helpful,
    Sarcastic,
    Cautious,
    Assertive,
    Apologetic,
    Accept,
    Decline,
    Bargain,
    Flirt,
    Threaten,
    Custom
}

[CreateAssetMenu(menuName = "Dialogues/Dialog Graph")]
public class DialogGraphDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id used by saves, logs and UI. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in editor/debug output. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer notes describing this conversation graph.")]
    [TextArea]
    [SerializeField] string description = string.Empty;

    [Header("Start")]
    [Tooltip("Node id used when the conversation starts. Empty uses the first node in the list.")]
    [SerializeField] string startNodeId = string.Empty;
    [Tooltip("Fallback presentation used by graph runners unless a runner overrides it.")]
    [SerializeField] DialogPresentationMode defaultPresentation = DialogPresentationMode.SpeechBubble;
    [Tooltip("Prompt shown above choices when a node has selectable responses.")]
    [SerializeField] string defaultChoicePrompt = "Choose a response.";

    [Header("Nodes")]
    [Tooltip("Conversation nodes. Each node can show lines, offer choices and move to another node.")]
    [SerializeField] List<DialogGraphNode> nodes = new List<DialogGraphNode>();

    [Header("Events")]
    [Tooltip("If enabled, graph start/end/node/choice activity is sent to GameEventBus and GameDebug.")]
    [SerializeField] bool publishEvents = true;
    [Tooltip("If enabled, player dialog graph history is written when possible.")]
    [SerializeField] bool writePlayerHistory = true;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public string StartNodeId => startNodeId;
    public DialogPresentationMode DefaultPresentation => defaultPresentation;
    public string DefaultChoicePrompt => string.IsNullOrWhiteSpace(defaultChoicePrompt) ? "Choose a response." : defaultChoicePrompt;
    public IReadOnlyList<DialogGraphNode> Nodes => nodes;
    public bool PublishEvents => publishEvents;
    public bool WritePlayerHistory => writePlayerHistory;

    public DialogGraphNode GetStartNode() {
        if(!string.IsNullOrWhiteSpace(startNodeId)) {
            var start = GetNode(startNodeId);
            if(start != null) {
                return start;
            }
        }

        return nodes.FirstOrDefault(node => node != null);
    }

    public DialogGraphNode GetNode(string nodeId) {
        if(string.IsNullOrWhiteSpace(nodeId) || nodes == null) {
            return null;
        }

        return nodes.FirstOrDefault(node => node != null && string.Equals(node.Id, nodeId, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasNode(string nodeId) {
        return GetNode(nodeId) != null;
    }
}

[Serializable]
public class DialogGraphNode {
    [Header("Identity")]
    [Tooltip("Stable node id used by choices, saves and debug logs.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Editor/debug label for this node.")]
    [SerializeField] string displayName = string.Empty;

    [Header("Content")]
    [Tooltip("Lines shown before this node offers choices or exits.")]
    [SerializeField] List<DialogGraphLine> lines = new List<DialogGraphLine>();
    [Tooltip("Choices shown after the lines. Empty means the node can auto-advance or end.")]
    [SerializeField] List<DialogGraphChoice> choices = new List<DialogGraphChoice>();
    [Tooltip("Prompt shown above this node's choices. Empty uses graph default prompt.")]
    [SerializeField] string choicePrompt = string.Empty;

    [Header("Flow")]
    [Tooltip("Optional node id used when this node has no choices and should continue automatically.")]
    [SerializeField] string autoNextNodeId = string.Empty;
    [Tooltip("If enabled, the conversation ends after this node unless a choice or auto-next node routes elsewhere.")]
    [SerializeField] bool endConversation = true;
    [Tooltip("Maximum times this node can be visited during one play session. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxVisitsPerPlay = 1;

    [Header("Effects")]
    [Tooltip("Effects applied when this node starts.")]
    [SerializeField] DialogGraphEffects onEnterEffects = new DialogGraphEffects();
    [Tooltip("Effects applied when this node exits.")]
    [SerializeField] DialogGraphEffects onExitEffects = new DialogGraphEffects();

    public string Id => !string.IsNullOrWhiteSpace(id) ? id : (!string.IsNullOrWhiteSpace(displayName) ? displayName : "node");
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? id : displayName;
    public IReadOnlyList<DialogGraphLine> Lines => lines;
    public IReadOnlyList<DialogGraphChoice> Choices => choices;
    public string ChoicePrompt => choicePrompt;
    public string AutoNextNodeId => autoNextNodeId;
    public bool EndConversation => endConversation;
    public int MaxVisitsPerPlay => Mathf.Max(0, maxVisitsPerPlay);
    public DialogGraphEffects OnEnterEffects => onEnterEffects;
    public DialogGraphEffects OnExitEffects => onExitEffects;

    public List<DialogGraphChoice> GetAvailableChoices(DialogContext context) {
        if(choices == null || choices.Count == 0) {
            return new List<DialogGraphChoice>();
        }

        return choices
            .Where(choice => choice != null && choice.IsAvailable(context))
            .OrderByDescending(choice => choice.Priority)
            .ThenBy(choice => choices.IndexOf(choice))
            .ToList();
    }
}

[Serializable]
public class DialogGraphLine {
    [Header("Text")]
    [Tooltip("Optional speaker name override. Empty uses the runner speaker name.")]
    [SerializeField] string speakerName = string.Empty;
    [Tooltip("Line text shown to the player.")]
    [TextArea]
    [SerializeField] string text = string.Empty;

    [Header("Presentation Hints")]
    [Tooltip("Emotional tone for UI color, icon or animation. Current backend publishes it for UI listeners.")]
    [SerializeField] DialogLineEmotion emotion = DialogLineEmotion.Neutral;
    [Tooltip("Optional color hint for UI. Leave Use Emotion Color disabled if the UI should pick its own palette.")]
    [SerializeField] Color emotionColor = Color.white;
    [Tooltip("If enabled, UI listeners may use Emotion Color for this line.")]
    [SerializeField] bool useEmotionColor = false;
    [Tooltip("If enabled, the speaker name is prepended when shown through the classic dialog box.")]
    [SerializeField] bool prefixSpeakerNameInClassic = true;

    public string SpeakerName => speakerName;
    public string Text => text;
    public DialogLineEmotion Emotion => emotion;
    public Color EmotionColor => emotionColor;
    public bool UseEmotionColor => useEmotionColor;
    public bool PrefixSpeakerNameInClassic => prefixSpeakerNameInClassic;

    public string BuildDisplayText(string fallbackSpeakerName, DialogPresentationMode presentationMode) {
        string line = text ?? string.Empty;
        string speaker = !string.IsNullOrWhiteSpace(speakerName) ? speakerName : fallbackSpeakerName;
        if(presentationMode == DialogPresentationMode.ClassicDialogBox && prefixSpeakerNameInClassic && !string.IsNullOrWhiteSpace(speaker)) {
            return $"{speaker}: {line}";
        }

        return line;
    }
}

[Serializable]
public class DialogGraphChoice {
    [Header("Identity")]
    [Tooltip("Stable choice id used by logs and history.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Text shown in the response list.")]
    [SerializeField] string displayText = string.Empty;
    [Tooltip("Intent used by roleplay UI, relationship systems and analytics.")]
    [SerializeField] DialogChoiceIntent intent = DialogChoiceIntent.Neutral;
    [Tooltip("Higher priority choices appear first when several choices are available.")]
    [SerializeField] int priority = 0;

    [Header("Flow")]
    [Tooltip("Node id opened after this choice is selected. Empty ends the conversation unless Stay On Same Node is enabled.")]
    [SerializeField] string nextNodeId = string.Empty;
    [Tooltip("If enabled, selecting this choice reruns the current node.")]
    [SerializeField] bool stayOnSameNode = false;

    [Header("Availability")]
    [Tooltip("How this choice's conditions are evaluated.")]
    [SerializeField] DialogConditionMatchMode matchMode = DialogConditionMatchMode.All;
    [Tooltip("If enabled, the final availability result is inverted.")]
    [SerializeField] bool invertConditions = false;
    [Tooltip("Conditions that decide whether this response is visible.")]
    [SerializeField] List<DialogCondition> conditions = new List<DialogCondition>();

    [Header("Effects")]
    [Tooltip("Effects applied after this response is selected.")]
    [SerializeField] DialogGraphEffects effects = new DialogGraphEffects();

    public string Id => string.IsNullOrWhiteSpace(id) ? displayText : id;
    public string DisplayText => displayText;
    public DialogChoiceIntent Intent => intent;
    public int Priority => priority;
    public string NextNodeId => nextNodeId;
    public bool StayOnSameNode => stayOnSameNode;
    public DialogGraphEffects Effects => effects;

    public bool IsAvailable(DialogContext context) {
        bool result;
        if(conditions == null || conditions.Count == 0) {
            result = true;
        } else if(matchMode == DialogConditionMatchMode.Any) {
            result = conditions.Any(condition => condition == null || condition.Evaluate(context));
        } else {
            result = conditions.All(condition => condition == null || condition.Evaluate(context));
        }

        return invertConditions ? !result : result;
    }
}

[Serializable]
public class DialogGraphEffects {
    [Header("Rewards And Changes")]
    [Tooltip("Faction reputation changes applied when this effect runs.")]
    [SerializeField] List<ReputationChange> reputationChanges = new List<ReputationChange>();
    [Tooltip("Relationship changes applied when this effect runs.")]
    [SerializeField] List<RelationshipChange> relationshipChanges = new List<RelationshipChange>();
    [Tooltip("Milestones completed when this effect runs.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Titles, permits, badges or licenses granted when this effect runs.")]
    [SerializeField] List<TitleGrant> titleGrants = new List<TitleGrant>();
    [Tooltip("Recipes learned when this effect runs.")]
    [SerializeField] List<RecipeGrant> recipeGrants = new List<RecipeGrant>();
    [Tooltip("Life Path XP, branch progress, tag counters or perk unlocks awarded when this effect runs.")]
    [SerializeField] List<LifePathReward> lifePathRewards = new List<LifePathReward>();

    [Header("Events")]
    [Tooltip("Optional custom event published when this effect runs. Empty uses a generated runtime event if Publish Event is enabled.")]
    [SerializeField] GameEventDefinition eventDefinition = null;
    [Tooltip("If enabled, an event is published when this effect runs.")]
    [SerializeField] bool publishEvent = false;
    [Tooltip("Event id used when Event Definition is empty.")]
    [SerializeField] string eventId = string.Empty;
    [Tooltip("Message used by event/debug output. Empty uses a generated message.")]
    [SerializeField] string eventMessage = string.Empty;
    [Tooltip("If enabled, this effect writes a debug breadcrumb.")]
    [SerializeField] bool writeDebug = false;

    public IReadOnlyList<ReputationChange> ReputationChanges => reputationChanges;
    public IReadOnlyList<RelationshipChange> RelationshipChanges => relationshipChanges;
    public IReadOnlyList<MilestoneDefinition> MilestonesToComplete => milestonesToComplete;
    public IReadOnlyList<TitleGrant> TitleGrants => titleGrants;
    public IReadOnlyList<RecipeGrant> RecipeGrants => recipeGrants;
    public IReadOnlyList<LifePathReward> LifePathRewards => lifePathRewards;
    public bool PublishEvent => publishEvent;
    public GameEventDefinition EventDefinition => eventDefinition;
    public string EventId => eventId;
    public string EventMessage => eventMessage;
    public bool WriteDebug => writeDebug;

    public bool HasAnyEffect {
        get {
            return (reputationChanges != null && reputationChanges.Any(change => change != null && change.faction != null && change.amount != 0))
                || (relationshipChanges != null && relationshipChanges.Any(change => change != null && change.subject != null && change.amount != 0))
                || (milestonesToComplete != null && milestonesToComplete.Any(milestone => milestone != null))
                || (titleGrants != null && titleGrants.Any(grant => grant != null && grant.title != null))
                || (recipeGrants != null && recipeGrants.Any(grant => grant != null && grant.recipe != null))
                || (lifePathRewards != null && lifePathRewards.Any(reward => reward != null && reward.lifePath != null && reward.HasAnyPayload))
                || publishEvent
                || writeDebug;
        }
    }

    public void Apply(PlayerController player, UnityEngine.Object context, string fallbackEventId, string fallbackMessage) {
        if(player != null) {
            player.GetComponent<PlayerReputation>()?.ApplyChanges(reputationChanges);
            player.GetComponent<PlayerRelationships>()?.ApplyChanges(relationshipChanges);
            player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);
            player.GetComponent<PlayerTitles>()?.ApplyGrants(titleGrants, context);
            player.GetComponent<PlayerRecipeBook>()?.ApplyGrants(recipeGrants, context);
            player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(lifePathRewards, fallbackEventId, fallbackMessage, context);
        }

        string message = !string.IsNullOrWhiteSpace(eventMessage) ? eventMessage : fallbackMessage;
        if(writeDebug && !string.IsNullOrWhiteSpace(message)) {
            GameDebug.Step(message, GameDebugCategory.NPC, context, "DialogGraphEffects");
        }

        if(publishEvent) {
            if(eventDefinition != null) {
                GameEventBus.Publish(
                    eventDefinition,
                    message,
                    context,
                    "DialogGraphEffects",
                    GameEventScope.Player,
                    new[] {
                        new GameEventValue { key = "eventId", value = !string.IsNullOrWhiteSpace(eventId) ? eventId : fallbackEventId }
                    });
            } else {
                GameEventBus.Publish(
                    !string.IsNullOrWhiteSpace(eventId) ? eventId : fallbackEventId,
                    message,
                    GameEventCategory.Dialogue,
                    GameEventImportance.Info,
                    context,
                    "DialogGraphEffects",
                    GameEventScope.Player);
            }
        } else if(eventDefinition != null) {
            GameEventBus.Publish(eventDefinition, message, context, "DialogGraphEffects", GameEventScope.Player);
        } else if(!string.IsNullOrWhiteSpace(eventId)) {
            GameEventBus.Publish(eventId, message, GameEventCategory.Dialogue, GameEventImportance.Info, context, "DialogGraphEffects", GameEventScope.Player);
        }
    }
}
