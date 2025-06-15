using UnityEngine;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;

public class UseItemState : State<GameController>{
    [SerializeField] InventoryUI inventoryUI;
    [SerializeField] PartyScreen partyScreen;

    GameController gameController;
    Inventory inventory;
    
    public bool ItemUsed {get; private set;}

    public static UseItemState i {get; private set;}

    void Awake(){
        i = this;
        inventory = Inventory.GetInventory();
    }

    public override void Enter(GameController owner){
        gameController = owner;
        ItemUsed = false;
        StartCoroutine(UseItem());
    }

    IEnumerator UseItem(){

        var item = inventoryUI.SelectedItem;
        var pokemon = partyScreen.SelectedMember;

        if(item is TmItem){
            yield return HandleTmItems();
        } else {
            if(item is EvolutionItem){
                var evolution = pokemon.CheckForEvolution(item);
                if(evolution != null){
                    yield return EvolutionManager.i.Evolve(pokemon, evolution);
                } else {
                    yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} can't evolve with {item.Name}");
                    gameController.StateMachine.Pop();
                    yield break;
                }
            }

            var usedItem = inventory.UseItem(item, partyScreen.SelectedMember);
            if(usedItem != null){
                ItemUsed = true;
                if(usedItem is RecoveryItem){
                    yield return DialogManager.i.ShowDialogText($"You use {usedItem.Name}!");
                }
            } else {
                if(inventoryUI.SelectedCategory == (int)ItemCategory.Recovery){
                    yield return DialogManager.i.ShowDialogText($"It won't have any effect.");
                }
            }
        }
        gameController.StateMachine.Pop();
    }

    IEnumerator HandleTmItems(){
        var tmItem = inventoryUI.SelectedItem as TmItem;
        if(tmItem == null){
            yield break;
        }

        var pokemon = partyScreen.SelectedMember;

        if(pokemon.HasMove(tmItem.Move)){
            yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} already knows {tmItem.Move.Name}");
            yield break;
        }

        if(!tmItem.CanBeTaught(pokemon)){
            yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} can't learn {tmItem.Move.Name}");
            yield break;
        }

        if(pokemon.Moves.Count < PokemonBase.MaxNumberOfMoves){
            pokemon.LearnMove(tmItem.Move);
            yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} learned {tmItem.Move.Name}");
        } else {
            yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} trying to learn {tmItem.Move.Name}...");
            yield return DialogManager.i.ShowDialogText($"But its is already knew {PokemonBase.MaxNumberOfMoves} moves.");
            
            yield return DialogManager.i.ShowDialogText($"Choose a move you want {pokemon.Base.Name} to forget.",true, false);
            
            MoveForgetState.i.NewMove = tmItem.Move;
            MoveForgetState.i.CurrentMoves = pokemon.Moves;

            yield return gameController.StateMachine.PushAndWait(MoveForgetState.i);
            int moveIndex = MoveForgetState.i.Selection;

            DialogManager.i.CloseDialog();
            if(moveIndex == PokemonBase.MaxNumberOfMoves || moveIndex == -1){
                yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} didn't learn{tmItem.Move.Name}");
            } else {
                var selectedMove = pokemon.Moves[ moveIndex ].Base;
                yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} forgot {selectedMove.Name} and learned {tmItem.Move.Name}");

                pokemon.Moves[ moveIndex ] = new Move(tmItem.Move);
            }
        }
    }
}
