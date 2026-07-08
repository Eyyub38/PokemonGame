using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrainerController : MonoBehaviour, Interactable, ISavable{
    [Header("Trainer Name")]
    [Tooltip("Trainer display name used in battle and dialog.")]
    [SerializeField] string _name;

    [Header("Trainer Battle Image")]
    [Tooltip("Sprite shown for this trainer in battle UI.")]
    [SerializeField] Sprite battleImage;

    [Header("Trainer Dialog")]
    [Tooltip("Fallback dialog shown before battle.")]
    [SerializeField] Dialog dialog;
    [Tooltip("Fallback dialog shown after this trainer has lost.")]
    [SerializeField] Dialog dialogAfterBattle;
    [Tooltip("Optional conditional dialog used before battle.")]
    [SerializeField] ConditionalDialogDefinition conditionalDialog;
    [Tooltip("Optional conditional dialog used after this trainer has lost.")]
    [SerializeField] ConditionalDialogDefinition conditionalDialogAfterBattle;

    [Header("Dialog Presentation")]
    [Tooltip("How this trainer presents dialog. Classic keeps the old dialog box; Speech Bubble uses SpeechBubbleDialogManager when available.")]
    [SerializeField] DialogPresentationMode dialogPresentation = DialogPresentationMode.ClassicDialogBox;
    [Tooltip("Speech bubble style used when dialog presentation is Speech Bubble.")]
    [SerializeField] SpeechBubbleStyleDefinition speechBubbleStyle;
    [Tooltip("Optional transform used as the speech bubble anchor. Empty uses this trainer transform plus the style offset.")]
    [SerializeField] Transform speechBubbleAnchor;

    [Header("Trainer Emote")]
    [Tooltip("Emote object briefly shown when the trainer notices the player.")]
    [SerializeField] GameObject exclamation;
    
    [Header("Trainer Battle")]
    [Tooltip("Trainer field-of-view object that detects the player.")]
    [SerializeField] GameObject fov;
    [Tooltip("How many Pokemon this trainer can use in battle.")]
    [Min(1)]
    [SerializeField] int battleUnitCount = 1;
    [Tooltip("Optional AI profile override used by this trainer. Empty uses BattleSystem's default trainer AI.")]
    [SerializeField] BattleAIProfile battleAIProfile;

    [Header("Trainer Music")]
    [Tooltip("Music played when this trainer starts an encounter.")]
    [SerializeField] AudioClip trainerAppearsClip;

    bool battleLost = false;
    Character character;

    public string Name => _name;
    public Sprite BattleImage => battleImage;
    public int BattleUnitCount => battleUnitCount;
    public BattleAIProfile BattleAIProfile => battleAIProfile;

    private void Awake(){
        character = GetComponent<Character>();
    }

    private void Start(){
        SetFovDirection(character.Animator.DefaultDirection);
    }

    public IEnumerator TriggerTrainerBattle(PlayerController player){
        GameController.i.StateMachine.Push(CutsceneState.i);
        AudioManager.i.PlayMusic(trainerAppearsClip);

        exclamation.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        exclamation.gameObject.SetActive(false);

        var diff = player.transform.position - transform.position;
        var moveVec = diff - diff.normalized;
        moveVec = new Vector3(Mathf.Round(moveVec.x), Mathf.Round(moveVec.y));

        yield return character.Move( moveVec);
        yield return ShowTrainerDialog(player.transform, beforeBattle: true);

        GameController.i.StateMachine.Pop();

        if(!TryPrepareBattleRules(player, player.transform, out var failureMessage)){
            yield return DialogPresenter.ShowText(failureMessage, dialogPresentation, this, player.transform, Name, speechBubbleStyle, speechBubbleAnchor);
            yield break;
        }

        GameController.i.StartTrainerBattle(this);
    }

    public void SetFovDirection(FacingDirection dir){
        float angle = 0f;

        if(dir == FacingDirection.Right){
            angle = 90f;
        } else if(dir == FacingDirection.Left){
            angle = 270f;
        } else if(dir == FacingDirection.Up){
            angle = 180f;
        }

        fov.transform.eulerAngles = new Vector3( 0f, 0f, angle);
    }

    public void ApplyGeneratedProfile(
        string trainerName,
        Sprite generatedBattleImage,
        Dialog generatedDialog,
        Dialog generatedAfterBattleDialog,
        ConditionalDialogDefinition generatedConditionalDialog,
        ConditionalDialogDefinition generatedConditionalAfterBattleDialog,
        int generatedBattleUnitCount,
        BattleAIProfile generatedAIProfile = null
    ) {
        if(!string.IsNullOrWhiteSpace(trainerName)) {
            _name = trainerName;
        }

        if(generatedBattleImage != null) {
            battleImage = generatedBattleImage;
        }

        if(generatedDialog != null) {
            dialog = generatedDialog;
        }

        if(generatedAfterBattleDialog != null) {
            dialogAfterBattle = generatedAfterBattleDialog;
        }

        if(generatedConditionalDialog != null) {
            conditionalDialog = generatedConditionalDialog;
        }

        if(generatedConditionalAfterBattleDialog != null) {
            conditionalDialogAfterBattle = generatedConditionalAfterBattleDialog;
        }

        if(generatedBattleUnitCount > 0) {
            battleUnitCount = Mathf.Max(1, generatedBattleUnitCount);
        }

        if(generatedAIProfile != null) {
            battleAIProfile = generatedAIProfile;
        }
    }

    public void BattleLost(){
        fov.gameObject.SetActive(false);
        battleLost = true;
    }

    public IEnumerator Interact(Transform initiator){
        character.LookTowards(initiator.position);
        if(!battleLost){
            AudioManager.i.PlayMusic(trainerAppearsClip);
    
            yield return ShowTrainerDialog(initiator, beforeBattle: true);
            var player = initiator != null ? initiator.GetComponent<PlayerController>() : null;
            if(!TryPrepareBattleRules(player, initiator, out var failureMessage)){
                yield return DialogPresenter.ShowText(failureMessage, dialogPresentation, this, initiator, Name, speechBubbleStyle, speechBubbleAnchor);
                yield break;
            }

            GameController.i.StartTrainerBattle(this);
        } else {
            yield return ShowTrainerDialog(initiator, beforeBattle: false);
        }
    }

    bool TryPrepareBattleRules(PlayerController player, Transform initiator, out string failureMessage) {
        var negotiator = GetComponent<BattleRuleNegotiator>();
        if(negotiator == null) {
            failureMessage = null;
            return true;
        }

        if(player == null && initiator != null) {
            player = initiator.GetComponent<PlayerController>();
        }

        return negotiator.TryPrepareBattle(player, out failureMessage);
    }

    IEnumerator ShowTrainerDialog(Transform initiator, bool beforeBattle) {
        var definition = beforeBattle ? conditionalDialog : conditionalDialogAfterBattle;
        var fallback = beforeBattle ? dialog : dialogAfterBattle;
        var selectedDialog = definition != null
            ? definition.SelectDialog(DialogContext.FromInteraction(this, initiator, Name))
            : fallback;

        if(selectedDialog != null) {
            yield return DialogPresenter.ShowDialog(selectedDialog, dialogPresentation, this, initiator, Name, speechBubbleStyle, speechBubbleAnchor);
        }
    }

    public object CaptureState(){
        return battleLost;
    }

    public void RestoreState(object state){
        battleLost = (bool)state;

        if(battleLost){
            fov.gameObject.SetActive(false);
        }
    }
}
