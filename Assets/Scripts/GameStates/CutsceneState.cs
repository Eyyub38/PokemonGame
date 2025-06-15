using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GDEUtills.StateMachine;

public class CutsceneState : State<GameController>{
    GameController gameController;

    public static CutsceneState i { get; private set; }

    private void Awake(){
        i = this;
    }

    public override void Enter(GameController owner){
        gameController = owner;
    }

    public override void Execute(){
        PlayerController.i.Character.HandleUpdate();
    }
}
