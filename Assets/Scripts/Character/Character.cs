using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Character : MonoBehaviour{
    CharacterAnimator animator;
    PokemonAnimator pokemonAnimator;
    public float movingSpeed = 5f;
    public float runningSpeed = 2.5f;

    public bool IsMoving{ get; private set; }
    public bool IsRunning {get; set;} = false;
    public CharacterAnimator Animator {get => animator;}
    public float OffSetY {get; private set;} = 0.3f;
    
    private void Awake(){
        animator = GetComponent<CharacterAnimator>();    
        SetPositionAndSnapToTile(transform.position);
    }
    
    public void HandleUpdate(){
        animator.IsMoving = IsMoving;
        animator.IsRunning = IsRunning;
        
        if (pokemonAnimator != null){
            pokemonAnimator.MoveX = animator.MoveX;
            pokemonAnimator.MoveY = animator.MoveY;
            pokemonAnimator.IsSurfing = animator.IsSurfing;
        }
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

        var collusionLayer = GameLayers.i.SolidObjectsLayer | GameLayers.i.InteractableLayer | GameLayers.i.PlayerLayer;
        if(animator.IsSurfing == false){
            collusionLayer = collusionLayer | GameLayers.i.WaterLayer;
        }
        if(Physics2D.BoxCast(transform.position + dir , new Vector2(0.2f, 0.2f), 0f, dir, diff.magnitude - 1, collusionLayer) == true){
            return false;
        }
        return true;
    }
    
    public IEnumerator Move(Vector2 moveVector, Action OnMoveOver = null){
        animator.MoveX = Mathf.Clamp(moveVector.x, -1f, 1f);
        animator.MoveY = Mathf.Clamp(moveVector.y, -1f, 1f);
        animator.IsMoving = true;

        var targetPos = transform.position;
        targetPos.x += moveVector.x;
        targetPos.y += moveVector.y;

        var ledge = CheckForLedge(targetPos);

        if(ledge != null){
            if(ledge.TryToJump(this, moveVector)) {
                yield break;
            }
        }

        if(!IsPathClear(targetPos)){
            animator.IsMoving = false;
            animator.MoveX = 0f;
            animator.MoveY = 0f;
            yield break;
        }
        if(animator.IsSurfing && Physics2D.OverlapCircle(targetPos, 0.3f, GameLayers.i.WaterLayer) == null){
            animator.IsSurfing = false;
            ClearPokemonAnimator();
        }

        IsMoving = true;
        float currentSpeed = IsRunning ? movingSpeed * runningSpeed : movingSpeed;

        int maxIterations = 1000;
        int iterations = 0;

        while((targetPos - transform.position).sqrMagnitude > 0.01f && iterations < maxIterations){
            var oldPos = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, currentSpeed * Time.deltaTime);
            
            if(Vector3.Distance(oldPos, transform.position) < 0.001f){
                Debug.LogWarning($"Character not moving! Old pos: {oldPos}, New pos: {transform.position}, Target: {targetPos}, Speed: {currentSpeed}");
                break;
            }
            
            iterations++;
            yield return null;
        }
        
        if(iterations >= maxIterations){
            Debug.LogWarning($"Move function reached maximum iterations ({maxIterations}), forcing position. Current: {transform.position}, Target: {targetPos}");
        }
        
        transform.position = targetPos;
        
        IsMoving = false;
        animator.IsMoving = false;
        animator.MoveX = 0f;
        animator.MoveY = 0f;

        OnMoveOver?.Invoke();
    }

    public void LookTowards(Vector3 targetPos){
        var xDiff = Mathf.Floor(targetPos.x) - Mathf.Floor(transform.position.x);
        var yDiff = Mathf.Floor(targetPos.y) - Mathf.Floor(transform.position.y);

        if(xDiff == 0 || yDiff == 0){
            animator.MoveX = Mathf.Clamp(xDiff, -1f, 1f);
            animator.MoveY = Mathf.Clamp(yDiff, -1f, 1f);
        } else {
            Debug.Log($"Error in Look Towards: You cannot ask the character to look diagonally!!!");
        }
    }

    public void SetPositionAndSnapToTile(Vector3 pos){
        pos.x = Mathf.Floor(pos.x) + 0.5f;
        pos.y = Mathf.Floor(pos.y) + 0.5f + OffSetY;
    
        transform.position = pos;
    }

    Ledge CheckForLedge(Vector3 targetPos){
        var collider = Physics2D.OverlapCircle(targetPos, 0.15f, GameLayers.i.LedgesLayer);
        return collider?.GetComponent<Ledge>();
    }

    public void SetPokemonAnimator(PokemonAnimator animator){
        pokemonAnimator = animator;
    }

    void ClearPokemonAnimator(){
        if (pokemonAnimator != null){
            pokemonAnimator.StopSurfing();
            Destroy(pokemonAnimator.gameObject);
            pokemonAnimator = null;
        }
    }
}