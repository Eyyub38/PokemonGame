using UnityEngine;
using System.Linq;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;

public class ActionSelectionState : State<BattleSystem>{
    [SerializeField] ActionSelectionUI actionSelectionUI;

    BattleSystem battleSystem;

    public static ActionSelectionState i { get; private set;}
    public ActionSelectionUI ActionSelectionUI => actionSelectionUI;

    void Awake(){
        i = this;
    }

    public override void Enter(BattleSystem owner){
        battleSystem = owner;
        actionSelectionUI.gameObject.SetActive(true);
        actionSelectionUI.OnSelected += OnActionSelected;

        battleSystem.DialogBox.SetDialog("Choose an Action");
    }

    public override void Execute(){
        actionSelectionUI.HandleUpdate();
    }

    public override void Exit(){
        actionSelectionUI.gameObject.SetActive(false);
        actionSelectionUI.OnSelected -= OnActionSelected;
    }

    void OnActionSelected(int selectedAction){
        if(selectedAction == 0){
            battleSystem.SelectedAction = BattleAction.Move;
            MoveSelectionState.i.Moves = battleSystem.PlayerUnit.Pokemon.Moves;
            actionSelectionUI.gameObject.SetActive(false);
            battleSystem.StateMachine.Push(MoveSelectionState.i);
        } else if(selectedAction == 1){
            StartCoroutine(GoToPartyState());
        } else if(selectedAction == 2){
            StartCoroutine(GoToInventoryState());
        } else if(selectedAction == 3){
            battleSystem.SelectedAction = BattleAction.Run;
            battleSystem.StateMachine.ChangeState(RunTurnState.i);
        }
    }

    IEnumerator GoToPartyState(){
        PartyState.i.BattleSystem = battleSystem;
        yield return GameController.i.StateMachine.PushAndWait(PartyState.i);
        
        var selectedPokemon = battleSystem.SelectedPokemon;
        if(selectedPokemon != null){
            battleSystem.SelectedAction = BattleAction.SwitchPokemon;
            battleSystem.SelectedPokemon = selectedPokemon;
            battleSystem.StateMachine.ChangeState(RunTurnState.i);
        }
    }

    IEnumerator GoToInventoryState(){
        InventoryState.i.BattleSystem = battleSystem;
        yield return GameController.i.StateMachine.PushAndWait(InventoryState.i);
        var selectedItem = InventoryState.i.SelectedItem;
        if(selectedItem != null){
            battleSystem.SelectedAction = BattleAction.UseItem;
            battleSystem.SelectedItem = selectedItem;
            battleSystem.StateMachine.ChangeState(RunTurnState.i);
        }
    }
}
