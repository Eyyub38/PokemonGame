using UnityEngine;
using GDEUtills.StateMachine;
using System.Collections.Generic;

public class GameMenuState : State<GameController>{
    [SerializeField] MenuController menuController;

    GameController gameController;
    
    public static GameMenuState i { get; private set; }

    void Awake(){
        i = this;
    }

    public override void Enter(GameController owner){
        gameController = owner;
        menuController.gameObject.SetActive(true);
        menuController.OnSelected += OnMenuItemSelected;
        menuController.OnBack += OnBack;
    }

    public override void Execute(){
        menuController.HandleUpdate();
    }
    
    public override void Exit(){
        menuController.gameObject.SetActive(false);
        menuController.OnSelected -= OnMenuItemSelected;
        menuController.OnBack -= OnBack;
    }

    void OnMenuItemSelected(int selection){
        if(selection == 0){
            gameController.StateMachine.Push(PartyState.i);
        } else if(selection == 1){
            gameController.StateMachine.Push(InventoryState.i);
        }
    }

    void OnBack(){
        gameController.StateMachine.Pop();
    }
}
