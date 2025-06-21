using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GDEUtills.StateMachine;
using System;

public class ShopSellingState : State<GameController>{
    [SerializeField] WalletUI walletUI;
    [SerializeField] CounterSelectorUI countSelectorUI;

    GameController gameController;
    Inventory inventory;

    public static ShopSellingState i {get; private set;}

    void Awake(){
        i = this;
    }

    void Start(){
        inventory = Inventory.GetInventory();
    }


    public override void Enter(GameController owner){
        gameController = owner;
        StartCoroutine(StartSellingState());
    }

    IEnumerator StartSellingState(){
        yield return gameController.StateMachine.PushAndWait(InventoryState.i);

        var selectedItem = InventoryState.i.SelectedItem;

        if(selectedItem != null){
            yield return SellItem(selectedItem);
            StartCoroutine(StartSellingState());
        } else {
            gameController.StateMachine.Pop();
        }   
    }

    IEnumerator SellItem(ItemBase item){

        if(!item.IsSellable){
            yield return DialogManager.i.ShowDialogText("This item can't be sold.");
            yield break;
        }

        walletUI.Show();
        int countToSell = 1;

        float sellingPrice = Mathf.Round(item.Price * 0.75f);

        int itemCount = inventory.GetItemCount(item);
        if(itemCount > 1){
            yield return DialogManager.i.ShowDialogText($"How many would you like to sell {item.Name}?",
                                                        waitForInput: false, autoClose: false);
            
            yield return countSelectorUI.ShowSelector(itemCount, sellingPrice,
                                              (selectedCount) => countToSell = selectedCount);

            DialogManager.i.CloseDialog();
        }

        sellingPrice = sellingPrice * countToSell;

        int selectedChoice = 0;
        yield return DialogManager.i.ShowDialogText($"Would you like to sell {item.Name} for ${sellingPrice}",
                                                    waitForInput: false,
                                                    choices: new List<string> { "Ok", "No, thanks" },
                                                    onChoiceSelected: (choiceIndex) => selectedChoice = choiceIndex);
        if(selectedChoice == 0){
            inventory.RemoveItem(item);
            Wallet.i.AddMoney(sellingPrice);
            yield return DialogManager.i.ShowDialogText($"You sold {item.Name} for ${sellingPrice}");
        }

        walletUI.Close();
    }
}
