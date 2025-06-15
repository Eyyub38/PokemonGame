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
            actionSelectionUI.gameObject.SetActive(false);
            StartCoroutine(GoToPartyState());
        } else if(selectedAction == 2){
            //BAG BURAYA DOKUNMA
        } else if(selectedAction == 3){
            battleSystem.SelectedAction = BattleAction.Run;
            battleSystem.StateMachine.ChangeState(RunTurnState.i);
        }
    }

    IEnumerator GoToPartyState(){
        battleSystem.OpenPartyScreen();
        yield return new WaitUntil(() => battleSystem.state != BattleStates.PartyScreen);
        
        var selectedPokemon = battleSystem.SelectedPokemon;
        if(selectedPokemon != null){
            battleSystem.SelectedAction = BattleAction.SwitchPokemon;
            battleSystem.StateMachine.ChangeState(RunTurnState.i);
        } else {
            actionSelectionUI.gameObject.SetActive(true);
        }
    }
}
