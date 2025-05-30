using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour{
    const float offSetY = 0.3f;

    [SerializeField] string _name;
    [SerializeField] Sprite battleImage;

    private Character character;
    private Vector2 input;
    
    public Action OnEncountered;
    public Action<Collider2D> OnEnterTrainersView;

    public string Name => _name;
    public Sprite BattleImage => battleImage;

    private void Awake(){
        character = GetComponent<Character>();
    }
    
    public void HandleUpdate(){
        if(!character.IsMoving){
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");
            
            if(input.x != 0 ) input.y = 0;

            if(input != Vector2.zero){
                StartCoroutine(character.Move(input, OnMoveOver));
            }
        }
        character.HandleUpdate();

        if(Input.GetKeyDown(KeyCode.Return)){
            Interact();
        }
    }

    void Interact(){
        var facingDir = new Vector3(character.Animator.MoveX, character.Animator.MoveY);
        var interactPos = transform.position + facingDir;

        var collider = Physics2D.OverlapCircle(interactPos, 0.3f, GameLayers.i.InteractableLayer);
        if(collider != null){
            collider.GetComponent<Interactable>()?.Interact(transform);
        }
    }

    private void OnMoveOver(){
        CheckForEncounters();
        CheckIfInTrainersView();
    }

    private void CheckForEncounters(){
        if(Physics2D.OverlapCircle(transform.position - new Vector3(0, offSetY), 0.2f, GameLayers.i.GrassLayer) != null){
            if(UnityEngine.Random.Range(1,101) <= 10){
                character.Animator.IsMoving = false;
                OnEncountered();
            }
        }
    }

    private void CheckIfInTrainersView(){
        var collider = Physics2D.OverlapCircle(transform.position, 0.2f, GameLayers.i.FovLayer);
        if( collider != null){
            character.Animator.IsMoving = false;
            OnEnterTrainersView?.Invoke(collider);
        }
    }
}