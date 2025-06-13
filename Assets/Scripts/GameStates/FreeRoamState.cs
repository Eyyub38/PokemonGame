using UnityEngine;
using System.Collections.Generic;
using GDEUtills.StateMachine;

public class FreeRoamState : State<GameController>{
    GameController gameController;
    public static FreeRoamState i { get; private set; }

    void Awake(){
        i = this;
    }

    public override void Enter(GameController owner){
        gameController = owner;
    }

    public override void Execute(){
        PlayerController.i.HandleUpdate();
        if(Input.GetKeyDown(KeyCode.Tab)){
            gameController.StateMachine.Push(GameMenuState.i);
        }
    }
}
