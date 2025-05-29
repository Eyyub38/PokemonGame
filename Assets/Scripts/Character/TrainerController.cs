using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrainerController : MonoBehaviour{
    [SerializeField] string _name;
    [SerializeField] Sprite battleImage;
    [SerializeField] Dialog dialog;
    [SerializeField] GameObject exclamation;
    [SerializeField] GameObject fov;

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
        exclamation.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        exclamation.gameObject.SetActive(false);

        var diff = player.transform.position - transform.position;
        var moveVec = diff - diff.normalized;
        moveVec = new Vector3(Mathf.Round(moveVec.x), Mathf.Round(moveVec.y));

        yield return character.Move( moveVec);
        StartCoroutine(DialogManager.i.ShowDialog( dialog, ()=> {
            GameController.Instance.StartTrainerBattle( this );
        }));
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
}