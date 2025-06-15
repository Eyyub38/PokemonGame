using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrainerController : MonoBehaviour, Interactable, ISavable{
    [Header("Trainer Name")]
    [SerializeField] string _name;

    [Header("Trainer Battle Image")]
    [SerializeField] Sprite battleImage;

    [Header("Trainer Dialog")]
    [SerializeField] Dialog dialog;
    [SerializeField] Dialog dialogAfterBattle;

    [Header("Trainer Emote")]
    [SerializeField] GameObject exclamation;
    
    [Header("Trainer FoV")]
    [SerializeField] GameObject fov;

    [Header("Trainer Music")]
    [SerializeField] AudioClip trainerAppearsClip;

    bool battleLost = false;
    Character character;

    public string Name => _name;
    public Sprite BattleImage => battleImage;

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
        yield return DialogManager.i.ShowDialog( dialog);

        GameController.i.StateMachine.Pop();
        
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

    public void BattleLost(){
        fov.gameObject.SetActive(false);
        battleLost = true;
    }

    public IEnumerator Interact(Transform initiator){
        character.LookTowards(initiator.position);
        if(!battleLost){
            AudioManager.i.PlayMusic(trainerAppearsClip);
    
            yield return DialogManager.i.ShowDialog(dialog);
            GameController.i.StartTrainerBattle(this);
        } else {
            yield return DialogManager.i.ShowDialog(dialogAfterBattle);
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