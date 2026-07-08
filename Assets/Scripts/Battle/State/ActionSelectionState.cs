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

        battleSystem.DialogBox.SetDialog($"Choose an Action for {battleSystem.UnitInSelection.Pokemon.Base.Name}");
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
            MoveSelectionState.i.Moves = battleSystem.UnitInSelection.Pokemon.Moves;
            battleSystem.StateMachine.ChangeState(MoveSelectionState.i);

        } else if(selectedAction == 1){
            if(!battleSystem.CanSwitchByRule(true, out var failureMessage)){
                StartCoroutine(ShowRuleBlocked(failureMessage));
                return;
            }

            StartCoroutine(GoToPartyState());

        } else if(selectedAction == 2){
            if(!battleSystem.CanUseBattleItem(true, null, out var failureMessage)){
                StartCoroutine(ShowRuleBlocked(failureMessage));
                return;
            }

            StartCoroutine(GoToInventoryState());

        } else if(selectedAction == 3){
            if(!battleSystem.CanRunByRule(out var failureMessage)){
                StartCoroutine(ShowRuleBlocked(failureMessage));
                return;
            }

            battleSystem.AddBattleAction(new BattleAction(){
                Type = BattleActionType.Run
            });
        }
    }

    IEnumerator ShowRuleBlocked(string message){
        yield return battleSystem.DialogBox.TypeDialog(string.IsNullOrWhiteSpace(message) ? "That action is blocked by the current battle rules." : message);
        battleSystem.StateMachine.ChangeState(ActionSelectionState.i);
    }

    IEnumerator GoToPartyState(){
        PartyState.i.BattleSystem = battleSystem;
        yield return GameController.i.StateMachine.PushAndWait(PartyState.i);
        
        var selectedPokemon = PartyState.i.SelectedPokemon;
        if(selectedPokemon != null){
            battleSystem.AddBattleAction(new BattleAction(){
                Type = BattleActionType.SwitchPokemon,
                SelectedPokemon = selectedPokemon
            });
        }
    }

    IEnumerator GoToInventoryState(){
        InventoryState.i.BattleSystem = battleSystem;
        yield return GameController.i.StateMachine.PushAndWait(InventoryState.i);
        var selectedItem = InventoryState.i.SelectedItem;
        if(selectedItem != null){
            battleSystem.AddBattleAction(new BattleAction(){
                Type = BattleActionType.UseItem,
                SelectedItem = selectedItem,
                SelectedPokemon = PartyState.i.SelectedPokemon
            });
        }
    }
}
