using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Character : MonoBehaviour{
    CharacterAnimator animator;
    public float movingSpeed;

    public bool IsMoving{ get; private set; }
    public CharacterAnimator Animator => animator;
    public float OffSetY {get; private set;} = 0.3f;
    
    private void Awake(){
        animator = GetComponent<CharacterAnimator>();    
        SetPositionAndSnapToTile(transform.position);
    }
    
    public void HandleUpdate(){
        animator.IsMoving = IsMoving;
    }

    private bool IsWalkable(Vector3 targetPos){
        if(Physics2D.OverlapCircle(targetPos, 0.2f, GameLayers.i.SolidObjectsLayer | GameLayers.i.InteractableLayer) != null){
            return false;
        }
        
        return true;
    }

    private bool IsPathClear(Vector3 targetPos){
        var diff = targetPos - transform.position;
        var dir = diff.normalized;

        if(Physics2D.BoxCast(transform.position + dir, new Vector2(0.2f,0.2f), 0f, dir, diff.magnitude - 1, GameLayers.i.SolidObjectsLayer | GameLayers.i.InteractableLayer | GameLayers.i.PlayerLayer) == true){
            return false;
        }
        return true;
    }
    
    public IEnumerator Move(Vector2 moveVector, Action OnMoveOver = null){
        animator.MoveX = Mathf.Clamp(moveVector.x, -1f, 1f);
        animator.MoveY = Mathf.Clamp(moveVector.y, -1f, 1f);

        var targetPos = transform.position;
        targetPos.x += moveVector.x;
        targetPos.y += moveVector.y;

        if(!IsPathClear(targetPos)){
            yield break;
        }

        IsMoving = true;

        while((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon){
            transform.position = Vector3.MoveTowards(transform.position, targetPos, movingSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;
        
        IsMoving = false;

        OnMoveOver?.Invoke();
    }

    public void LookTowards(Vector3 targetPos){
        var xDiff = Mathf.Floor(targetPos.x) - Mathf.Floor(transform.position.x);
        var yDiff = Mathf.Floor(targetPos.y) - Mathf.Floor(transform.position.y);

        if(xDiff == 0 || yDiff == 0){
            animator.MoveX = Mathf.Clamp(xDiff, -1f, 1f);
            animator.MoveY = Mathf.Clamp(yDiff, -1f, 1f);
        } else {
            Debug.Log($"Error in Look Towards: You cannto ask the character to look diagonally!!!");
        }
    }

    public void SetPositionAndSnapToTile(Vector3 pos){
        pos.x = Mathf.Floor(pos.x) + 0.5f;
        pos.x = Mathf.Floor(pos.x) + 0.5f + OffSetY;
    
        transform.position = pos;
    }
}