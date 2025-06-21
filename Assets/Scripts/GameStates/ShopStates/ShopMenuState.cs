using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GDEUtills.StateMachine;

public class ShopMenuState : State<GameController>{
    GameController gameController;

    public List<ItemBase> AvailableItems {get; set;}

    public static ShopMenuState i {get; private set;}

    void Awake(){
        i = this;
    }

    public override void Enter(GameController owner){
        gameController = owner;

        StartCoroutine(StartMenuState());
    }

    IEnumerator StartMenuState(){
        int selectedChoice = 0;
        
        yield return DialogManager.i.ShowDialogText("Welcome to my shop! What would you like to buy?",
                                                    waitForInput: false,
                                                    choices: new List<string> { "Buy", "Sell", "Quit" },
                                                    onChoiceSelected: (choice) => selectedChoice = choice);
        
        if(selectedChoice == 0){
            ShopBuyingState.i.AvailableItems = AvailableItems;
            yield return gameController.StateMachine.PushAndWait(ShopBuyingState.i);
        } else if(selectedChoice == 1){
            yield return gameController.StateMachine.PushAndWait(ShopSellingState.i);
        } else if(selectedChoice == 2){}
        
        gameController.StateMachine.Pop();
    }
}
