using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ShopState{ Menu, Buying, Selling, Busy}

public class ShopController : MonoBehaviour{
    [SerializeField] InventoryUI inventoryUI;
    [SerializeField] WalletUI walletUI;
    [SerializeField] CounterSelectorUI counter;

    ShopState state;
    Inventory inventory;
    int selectedChoice = 0;

    public event Action OnStart;
    public event Action OnFinish;

    public static ShopController i {get; private set;}

    private void Awake(){
        i = this;
    }

    private void Start(){
        inventory = Inventory.GetInventory();
    }

    public IEnumerator StartTrading(Merchant merchant){
        OnStart?.Invoke();
        yield return StartMenuState();
    }

    IEnumerator StartMenuState(){
        state = ShopState.Menu;

        yield return DialogManager.i.ShowDialogText("Welcome to my shop! What would you like to buy?",
                                                    waitForInput: false,
                                                    choices: new List<string> { "Buy", "Sell", "Quit" },
                                                    onChoiceSelected: (choice) => {selectedChoice = choice;});
        
        if(selectedChoice == 0){
            //Buy
        } else if(selectedChoice == 1){

            state = ShopState.Selling;
            inventoryUI.gameObject.SetActive(true);

        } else if(selectedChoice == 2){
            OnFinish?.Invoke();
            yield return DialogManager.i.ShowDialogText("Thank you for visiting! Come again!");
            yield break;
        }
    }

    public void HandleUpdate(){
        if(state == ShopState.Selling){
            inventoryUI.HandleUpdate(OnBackFromSelling, (selectedItem) => StartCoroutine(SellItem(selectedItem)));
        }
    }

    void OnBackFromSelling(){
        inventoryUI.gameObject.SetActive(false);
        StartCoroutine(StartMenuState());
    }

    IEnumerator SellItem(ItemBase item){
        state = ShopState.Busy;

        if(!item.IsSellable){
            yield return DialogManager.i.ShowDialogText("This item can't be sold.");
            state = ShopState.Selling;
            yield break;
        }

        walletUI.Show();
        int countToSell = 1;

        float sellingPrice = Mathf.Round(item.Price * 0.75f);

        int itemCount = inventory.GetItemCount(item);
        if(itemCount > 1){
            yield return DialogManager.i.ShowDialogText($"How many would you like to sell {item.Name}?",
                                                        waitForInput: false, autoClose: false);
            
            yield return counter.ShowSelector(itemCount, sellingPrice,
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

        state = ShopState.Selling;
    }
}
