using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class MoveActorAction : CutsceneAction{
    [SerializeField] Character character;
    [SerializeField] List<Vector2> movePatterns;

    public override IEnumerator Play(){
        foreach(var moveVec in movePatterns){
            yield return character.Move(moveVec);
        }
    }
}
