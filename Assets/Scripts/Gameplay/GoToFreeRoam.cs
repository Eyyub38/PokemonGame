using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GoToFreeRoam : MonoBehaviour{
    IEnumerator Start(){
        yield return null;
        GameController.i.StateMachine.ChangeState(FreeRoamState.i);
    }
}
