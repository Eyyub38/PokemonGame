using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum NPCState{ Idle, Walking, Dialog}

public class NPCController : MonoBehaviour, Interactable, ISavable{
    [Header("NPC Identity")]
    [Tooltip("Display name used by speech bubble/dialog systems. Empty uses the GameObject name.")]
    [SerializeField] string displayName;

    [Header("NPC Dialog")]
    [Tooltip("Fallback dialog shown when no conditional dialog is assigned or matched.")]
    [SerializeField] Dialog dialog;
    [Tooltip("Optional conditional dialog selector. If assigned, this can choose different dialog lines from player/world state.")]
    [SerializeField] ConditionalDialogDefinition conditionalDialog;

    [Header("NPC Dialog Graph")]
    [Tooltip("Optional interactive dialog graph used before the fallback/conditional dialog. Leave empty to use the classic dialog fields.")]
    [SerializeField] DialogGraphDefinition dialogGraph = null;
    [Tooltip("If enabled, Dialog Graph uses this NPC's dialog presentation setting instead of the graph default.")]
    [SerializeField] bool overrideGraphPresentationWithNpcSetting = true;

    [Header("Dialog Presentation")]
    [Tooltip("How this NPC presents its default dialog. Classic keeps the old dialog box; Speech Bubble uses SpeechBubbleDialogManager when available.")]
    [SerializeField] DialogPresentationMode dialogPresentation = DialogPresentationMode.ClassicDialogBox;
    [Tooltip("Speech bubble style used when dialog presentation is Speech Bubble.")]
    [SerializeField] SpeechBubbleStyleDefinition speechBubbleStyle;
    [Tooltip("Optional transform used as the speech bubble anchor. Empty uses this NPC transform plus the style offset.")]
    [SerializeField] Transform speechBubbleAnchor;
 
    [Header("NPC Move Pattern")]
    [Tooltip("Tile movement pattern used while the NPC is idle.")]
    [SerializeField] List<Vector2> movementPattern;
    [Tooltip("Seconds the NPC waits before attempting the next movement pattern step.")]
    [SerializeField] float timeBetweenPattern;

    [Header("Quests")]
    [Tooltip("Quest started by this NPC when available.")]
    [SerializeField] QuestBase questToStart;
    [Tooltip("Quest completed by this NPC when the player meets its requirements.")]
    [SerializeField] QuestBase questToComplete;

    Character character;

    ItemGiver itemGiver;
    PokemonGiver pokemonGiver;
    Merchant merchant;
    Healer healer;
    NPCMemoryProfile memoryProfile;

    NPCState state;
    Quest activeQuest;
    float idleTimer = 0f;
    int currentPattern = 0;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

    void Awake(){
        character = GetComponent<Character>();
        itemGiver = GetComponent<ItemGiver>();
        pokemonGiver = GetComponent<PokemonGiver>();
        healer = GetComponent<Healer>();
        merchant = GetComponent<Merchant>();
        memoryProfile = GetComponent<NPCMemoryProfile>();
    }

    public void SetMovementPattern(IReadOnlyList<Vector2> pattern){
        movementPattern = pattern != null ? new List<Vector2>(pattern) : new List<Vector2>();
        currentPattern = 0;
        idleTimer = 0f;
    }

    public void ApplyGeneratedProfile(
        string generatedName,
        Dialog generatedDialog,
        ConditionalDialogDefinition generatedConditionalDialog,
        IReadOnlyList<Vector2> generatedMovementPattern
    ) {
        if(!string.IsNullOrWhiteSpace(generatedName)) {
            displayName = generatedName;
        }

        if(generatedDialog != null) {
            dialog = generatedDialog;
        }

        if(generatedConditionalDialog != null) {
            conditionalDialog = generatedConditionalDialog;
        }

        if(generatedMovementPattern != null) {
            SetMovementPattern(generatedMovementPattern);
        }
    }

    public IEnumerator Interact(Transform initiator){
        if(state == NPCState.Idle){
            state = NPCState.Dialog;

            character.LookTowards(initiator.position);
            yield return new WaitForEndOfFrame();

            if(memoryProfile != null && memoryProfile.RecordConversationOnInteract) {
                memoryProfile.RecordConversation(initiator.GetComponent<PlayerController>(), "npc-interact");
            }

            if(questToComplete != null){
                var quest = new Quest(questToComplete);
                if (quest.CanBeCompleted()){
                    yield return quest.CompleteQuest(initiator);
                    questToComplete = null;
                }
            }

            if(itemGiver != null && itemGiver.CanBeGiven()){
                yield return itemGiver.GiveItem(initiator.GetComponent<PlayerController>());

            } else if(pokemonGiver != null && pokemonGiver.CanBeGiven()){
                yield return pokemonGiver.GivePokemon(initiator.GetComponent<PlayerController>());

            } else if(questToStart != null){
                activeQuest = new Quest(questToStart);
                yield return activeQuest.StartQuest();
                questToStart = null;

                if(activeQuest.CanBeCompleted()){
                    yield return activeQuest.CompleteQuest(initiator);
                    activeQuest = null;
                }

            } else if(activeQuest != null){
                if(activeQuest.CanBeCompleted()){
                    yield return activeQuest.CompleteQuest(initiator);
                    activeQuest = null;
                } else {
                    yield return DialogManager.i.ShowDialog(activeQuest.Base.InProgressDialog);
                }

            } else if(healer != null){
                yield return healer.Heal(initiator, dialog);

            } else if(merchant != null){
                yield return merchant.Trade();

            } else {
                yield return ShowDefaultDialog(initiator);

            }
            idleTimer = 0f;
            state = NPCState.Idle;
        }
    }

    void Update(){
        if(state == NPCState.Idle){
            idleTimer += Time.deltaTime;
            if(idleTimer > timeBetweenPattern){
                idleTimer = 0f;
                if(movementPattern.Count > 0)
                    StartCoroutine(Walk());
            }
        }
        character.HandleUpdate();
    }

    IEnumerator Walk(){
        state = NPCState.Walking;

        var oldPos = transform.position;

        yield return character.Move(movementPattern[currentPattern]);

        if(oldPos != transform.position){
            currentPattern = (currentPattern + 1) % movementPattern.Count;
        }

        state = NPCState.Idle;
    }

    IEnumerator ShowDefaultDialog(Transform initiator) {
        if(dialogGraph != null) {
            var options = new DialogGraphPlaybackOptions {
                Player = initiator != null ? initiator.GetComponent<PlayerController>() : PlayerController.i,
                Initiator = initiator,
                Source = this,
                SpeakerName = DisplayName,
                SpeakerId = DisplayName,
                OverridePresentation = overrideGraphPresentationWithNpcSetting,
                Presentation = dialogPresentation,
                SpeechBubbleStyle = speechBubbleStyle,
                SpeechBubbleAnchor = speechBubbleAnchor
            };
            yield return DialogGraphPlayer.Ensure().Play(dialogGraph, options);
            yield break;
        }

        var selectedDialog = conditionalDialog != null
            ? conditionalDialog.SelectDialog(DialogContext.FromInteraction(this, initiator, DisplayName))
            : dialog;

        if(selectedDialog != null) {
            yield return DialogPresenter.ShowDialog(selectedDialog, dialogPresentation, this, initiator, DisplayName, speechBubbleStyle, speechBubbleAnchor);
        }
    }

    public object CaptureState(){
        var saveData = new NPCQuestSaveData();

        saveData.activeQuest = activeQuest?.GetSaveData();

        if(questToStart != null){   
            saveData.questToStart = (new Quest(questToStart)).GetSaveData();
        }
        if(questToComplete != null){
            saveData.questToComplete = (new Quest(questToComplete)).GetSaveData();
        }

        return saveData;
    }

    public void RestoreState(object state){
        var saveData = state as NPCQuestSaveData;
        if(saveData != null){
            activeQuest = (saveData.activeQuest != null) ? new Quest(saveData.activeQuest) : null;
            questToStart = (saveData.questToStart != null) ? new Quest(saveData.questToStart).Base : null;
            questToComplete = (saveData.questToComplete != null) ? new Quest(saveData.questToComplete).Base : null;
        }
    }
}

[System.Serializable]
public class NPCQuestSaveData{
    public QuestSaveData activeQuest;
    public QuestSaveData questToStart;
    public QuestSaveData questToComplete;
}
