using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GDEUtills.StateMachine;

public class ShopBuyingState : State<GameController>{
    [SerializeField] WalletUI walletUI;
    [SerializeField] CounterSelectorUI countSelectorUI;
    [SerializeField] ShopUI shopUI;
    [SerializeField] Vector2 cameraOffset;

    GameController gameController;
    Inventory inventory;
    bool browseItems = false;

    public List<ItemBase> AvailableItems {get; set;}

    public static ShopBuyingState i {get; private set;}

    void Awake(){
        i = this;
    }

    void Start(){
        inventory = Inventory.GetInventory();
    }

    public override void Enter(GameController owner){
        gameController = owner;
        browseItems = false;
        StartCoroutine(StartBuyingState());
    }

    public override void Execute(){
        if(browseItems){
            shopUI.HandleUpdate();
        }
    }

    IEnumerator StartBuyingState(){
        yield return GameController.i.MoveCamera(cameraOffset);
        walletUI.Show();
        shopUI.Show(AvailableItems, (item) => StartCoroutine(BuyItem(item)), () => StartCoroutine(OnBackFromBuying()));
        browseItems = true;
    }

    IEnumerator BuyItem(ItemBase item){
        browseItems = false;
        yield return DialogManager.i.ShowDialogText($"How many would you like to buy {item.Name}?",
                                                    waitForInput: false, autoClose: false);

        int countToBuy = 1;
        yield return countSelectorUI.ShowSelector( 100, item.Price, 
                                                (selectedCount) => countToBuy = selectedCount );

        DialogManager.i.CloseDialog();

        float totalPrice = item.Price * countToBuy;
        if(Wallet.i.HasMoney(totalPrice)){
            int selectedChoice = 0;
            yield return DialogManager.i.ShowDialogText($"That will cost you ${totalPrice}. Would you like to buy it",
                                                    waitForInput: false,
                                                    choices: new List<string> { "Ok", "No, thanks" },
                                                    onChoiceSelected: (choiceIndex) => selectedChoice = choiceIndex);
            
            if(selectedChoice == 0){
                inventory.AddItem(item, countToBuy);
                Wallet.i.TakeMoney(totalPrice);
                yield return DialogManager.i.ShowDialogText($"Thanks for shhopping with us.");
            }
        } else {
            yield return DialogManager.i.ShowDialogText("You don't have enough money!");
        }
        browseItems = true;
    }

    IEnumerator OnBackFromBuying(){
        yield return GameController.i.MoveCamera(-cameraOffset);
        shopUI.Close();
        walletUI.Close();
        gameController.StateMachine.Pop();
    }
}
