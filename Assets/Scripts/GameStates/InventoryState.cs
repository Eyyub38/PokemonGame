using UnityEngine;
using GDEUtills.StateMachine;
using System.Collections.Generic;
using System;

public class InventoryState : State<GameController>{
    [SerializeField] InventoryUI inventoryUI;

    GameController gameController;
    
    public static InventoryState i { get; private set; }

    void Awake(){
        i = this;
    }
    
    public override void Enter(GameController owner){
        gameController = owner;
        inventoryUI.gameObject.SetActive(true);
        inventoryUI.OnSelected += OnItemSelected;
        inventoryUI.OnBack += OnBack;
    }

    public override void Execute(){
        inventoryUI.HandleUpdate();
    }
    
    void OnItemSelected(int selectedItem){
        gameController.StateMachine.Push(GamePartyState.i);
    }

    void OnBack(){
        gameController.StateMachine.Pop();
    }

    public override void Exit(){
        inventoryUI.gameObject.SetActive(false);
        inventoryUI.OnSelected -= OnItemSelected;
        inventoryUI.OnBack -= OnBack;
    }
}
