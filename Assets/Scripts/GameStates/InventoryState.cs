using System;
using UnityEngine;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;

public class InventoryState : State<GameController>{
    [SerializeField] InventoryUI inventoryUI;

    GameController gameController;
    Inventory inventory;
    
    public ItemBase SelectedItem { get; private set; }
    public BattleSystem BattleSystem { get; set; }

    public static InventoryState i { get; private set; }

    void Awake(){
        i = this;
    }

    void Start(){
        inventory = Inventory.GetInventory();
    }
    
    public override void Enter(GameController owner){
        gameController = owner;
        gameController.InputMaps.EnableUI();

        SelectedItem = null;
        if(BattleSystem != null){
            inventoryUI = BattleSystem.InventoryUI;
        }

        inventoryUI.gameObject.SetActive(true);
        inventoryUI.OnSelected += OnItemSelected;
        inventoryUI.OnBack += OnBack;
    }

    public override void Execute(){
        inventoryUI.HandleUpdate();
    }

    public override void Exit(){
        inventoryUI.gameObject.SetActive(false);
        BattleSystem = null;
        inventoryUI.OnSelected -= OnItemSelected;
        inventoryUI.OnBack -= OnBack;
    }
    
    void OnItemSelected(int selectedItem){
        SelectedItem = inventoryUI.SelectedItem;
        if(gameController.StateMachine.GetPrevState() != ShopSellingState.i){
            StartCoroutine(SelectPokemonAndUseItem());
            
        } else {
            gameController.StateMachine.Pop();
        }
    }

    void OnBack(){
        gameController.StateMachine.Pop();
    }

    IEnumerator SelectPokemonAndUseItem(){
        var prevState = gameController.StateMachine.GetPrevState();
        if(prevState == BattleState.i){
            if(!SelectedItem.CanUseInBattle){
                yield return DialogManager.i.ShowDialogText($"{SelectedItem.Name} can't be used in battle.");
                yield break;
            }

            if(BattleSystem != null && !BattleSystem.CanUseBattleItem(true, SelectedItem, out var failureMessage)){
                yield return DialogManager.i.ShowDialogText(string.IsNullOrWhiteSpace(failureMessage) ? "That item is blocked by the current battle rules." : failureMessage);
                yield break;
            }

        } else {
            if(!SelectedItem.CanUseInOutsideBattle){
                yield return DialogManager.i.ShowDialogText($"{SelectedItem.Name} can't be used outside battle.");
                yield break;
            }
        }

        if(SelectedItem is PokeballItem){
            if (prevState != BattleState.i) {
                inventory.UseItem(SelectedItem, null);
            }
            gameController.StateMachine.Pop();
            yield break;
        }
        
        if(SelectedItem is TmItem){
            PartyState.i.SelectedItem = SelectedItem;
        }
        
        yield return gameController.StateMachine.PushAndWait(PartyState.i);

        if(prevState == BattleState.i){
            if(UseItemState.i.ItemUsed){
                gameController.StateMachine.Pop();
            }
        }
    }
}
