using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class Ledge : MonoBehaviour{
    [SerializeField] int xDir;
    [SerializeField] int yDir;

    private void Awake(){
        GetComponent<SpriteRenderer>().enabled = false;
    }

    public bool TryToJump(Character chacarter, Vector2 moveDir){
        if(moveDir.x == xDir && moveDir.y == yDir){
            StartCoroutine(Jump(chacarter));
           return true; 
        }
        return false;
    }

    IEnumerator Jump(Character character){
        character.Animator.IsJumping = true;
        
        GameController.i.PauseGame(true);
        
        var landingPoint = character.transform.position + new Vector3(xDir,yDir) * 2;
        yield return character.transform.DOJump(landingPoint, 0.3f, 1, 0.5f).WaitForCompletion();
        
        GameController.i.PauseGame(false);
        
        character.Animator.IsJumping = false;
    }
}
