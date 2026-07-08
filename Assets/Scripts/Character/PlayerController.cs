using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, ISavable{
    [Header("Character Name")]
    [SerializeField] string _name;
    
    [Header("Battle Image")]
    [SerializeField] Sprite battleImage;

    Character character;
    Vector2 input;
    IPlayerTriggerable currentlyInTrigger;
    
    public string Name => _name;
    public Sprite BattleImage => battleImage;
    public Character Character => character;
    public event Action<Vector3> OnMovedTile;

    public static PlayerController i { get; private set; }

    public InputActionAsset actions;
    InputAction move;
    InputAction run;
    InputAction interact;

    private void Awake(){
        i = this;
        character = GetComponent<Character>();
        
        if (actions == null){
            Debug.LogError("PlayerController: actions (InputActionAsset) not found!");
            enabled = false;
            return;
        }
        
        var playerMap = actions.FindActionMap("Player");
        
        move = playerMap.FindAction("Move");
        run = playerMap.FindAction("Run");
        interact = playerMap.FindAction("Interact");
    }
    
    public void HandleUpdate(){
        if(!character.IsMoving) {
            input = move.ReadValue<Vector2>();
            
            input.x = Mathf.RoundToInt(input.x);
            input.y = Mathf.RoundToInt(input.y);
            
            if(input.x != 0 ) input.y = 0;
            bool wasRunning = character.IsRunning;
            character.IsRunning = run.IsPressed();

            if(input != Vector2.zero){
                StartCoroutine( character.Move(input, OnMoveOver));
            }
        }
        
        character.HandleUpdate();

        if(interact.WasPressedThisFrame()){
            StartCoroutine(Interact());
        }
    }

    void OnEnable() {
        if(move != null) move.Enable();
        if(run != null) run.Enable();
        if(interact != null) interact.Enable();
    }
    void OnDisable() {
        move?.Disable();
        run?.Disable();
        interact?.Disable();
    }

    IEnumerator Interact(){
        var facingDir = new Vector3(character.Animator.MoveX, character.Animator.MoveY);
        
        if(facingDir == Vector3.zero){
            facingDir = GetLastFacingDirection();
        }
        
        var interactPos = transform.position + facingDir;

        var collider = Physics2D.OverlapCircle(interactPos, 0.5f, GameLayers.i.InteractableLayer | GameLayers.i.WaterLayer);
        if(collider != null){
            var interactable = collider.GetComponent<Interactable>();
            if(interactable != null){
                yield return interactable.Interact(transform);
            }
        } else {
            yield return TryInteractInAllDirections();
        }
    }

    public Vector3 GetLastFacingDirection(){
        var animator = character.Animator;
        
        if(animator.MoveX != 0){
            return new Vector3(animator.MoveX, 0, 0);
        } else if(animator.MoveY != 0){
            return new Vector3(0, animator.MoveY, 0);
        }
        
        if(animator.LastMoveX != 0){
            return new Vector3(animator.LastMoveX, 0, 0);
        } else if(animator.LastMoveY != 0){
            return new Vector3(0, animator.LastMoveY, 0);
        }
        
        switch(animator.DefaultDirection){
            case FacingDirection.Up: return Vector3.up;
            case FacingDirection.Down: return Vector3.down;
            case FacingDirection.Left: return Vector3.left;
            case FacingDirection.Right: return Vector3.right;
            default: return Vector3.down;
        }
    }

    private IEnumerator TryInteractInAllDirections(){
        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right };
        
        foreach(var dir in directions){
            var interactPos = transform.position + dir;
            var collider = Physics2D.OverlapCircle(interactPos, 0.5f, GameLayers.i.InteractableLayer | GameLayers.i.WaterLayer);
            if(collider != null){
                var interactable = collider.GetComponent<Interactable>();
                if(interactable != null){
                    yield return interactable.Interact(transform);
                    yield break;
                }
            }
        }
    }

    int stepCount = 0;
    private void OnMoveOver() {
        var colliders = Physics2D.OverlapCircleAll(transform.position - new Vector3(0, character.OffSetY), 0.2f, GameLayers.i.TriggerableLayers);
        
        OnMovedTile?.Invoke(transform.position);

        stepCount++;
        if(stepCount >= 256){
            stepCount = 0;
            GetComponent<PokemonParty>().Pokemons.ForEach(p => p.IncreaseFriendship(1));
        }

        IPlayerTriggerable triggerable = null;
        foreach(var collider in colliders) {
            triggerable = collider.GetComponent<IPlayerTriggerable>();
        
            if(triggerable != null) {
                if(triggerable == currentlyInTrigger && !triggerable.TriggerRepeatedly){
                    break;
                }
                triggerable.OnPlayerTriggered(this);
                currentlyInTrigger = triggerable;
                break;
            }
        }

        if(colliders.Count() == 0 || triggerable != currentlyInTrigger){
            currentlyInTrigger = null;
        }
    }

    public object CaptureState(){
        var saveData = new PlayerSaveData(){
            position = new float[] {transform.position.x, transform.position.y},
            pokemons = GetComponent<PokemonParty>().Pokemons.Select( p => p.GetSaveData()).ToList()
        };

        return saveData;
    }

    public void RestoreState(object state){
        var saveData = (PlayerSaveData)state;
        var pos = saveData.position;
        transform.position = new Vector3(pos[0], pos[1]);

        GetComponent<PokemonParty>().Pokemons = saveData.pokemons.Select( s => new Pokemon(s)).ToList();
    }
}

[Serializable]
public class PlayerSaveData{
    public float[] position;
    public List<PokemonSaveData> pokemons;
}
