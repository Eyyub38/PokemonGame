using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class Cutscene : MonoBehaviour, IPlayerTriggerable{

    [SerializeReference]
    [SerializeField] List<CutsceneAction> actions;

    public bool TriggerRepeatedly => false;

    public IEnumerator Play(){
        GameController.i.StateMachine.Push(CutsceneState.i);

        foreach(var action in actions){
            
            if(action.WaitForCompletion){
                yield return action.Play();
            } else {
                StartCoroutine(action.Play());
            }
        }

        GameController.i.StateMachine.Pop();
    }

    public void AddAction(CutsceneAction action){
        
        #if UNITY_EDITOR
        Undo.RegisterCompleteObjectUndo(this, "Add Action to Cutscene");
        #endif

        action.Name = action.GetType().ToString();
        actions.Add(action);
    }

    public void OnPlayerTriggered(PlayerController player){
        player.Character.Animator.IsMoving = false;
        StartCoroutine(Play());
    }
}
