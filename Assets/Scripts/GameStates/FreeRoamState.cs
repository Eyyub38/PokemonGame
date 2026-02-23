using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using GDEUtills.StateMachine;

public class FreeRoamState : State<GameController>{
    GameController gameController;
    [SerializeField] InputActionAsset inputActions;
    [SerializeField] string playerMapName = "Player";
    [SerializeField] string menuActionName = "Menu";
    
    InputAction menuAction;
    
    public static FreeRoamState i { get; private set; }

    void Awake(){
        i = this;
    }

    public override void Enter(GameController owner){
        gameController = owner;
        
        gameController.InputMaps.EnablePlayer();
        var map = inputActions.FindActionMap(playerMapName);
        menuAction = inputActions.FindAction(menuActionName);
        menuAction.Enable();
    }

    public override void Execute(){
        PlayerController.i.HandleUpdate();
        if(menuAction.WasPressedThisFrame()){
            gameController.StateMachine.Push(GameMenuState.i);
        }
    }
    
    public override void Exit(){
        menuAction?.Disable();
    }
}
