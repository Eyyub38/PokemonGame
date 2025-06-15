using UnityEngine;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;
using System;
using System.Linq;


public class MoveSelectionState : State<BattleSystem>{
    [SerializeField] MoveSelectionUI moveSelectionUI;    
    BattleSystem battleSystem;

    public List<Move> Moves {get; set;}

    public static MoveSelectionState i {get; private set;}

    void Awake(){
        i = this;
    }

    public override void Enter(BattleSystem owner){
        battleSystem = owner;

        if (Moves.Where(m => m.PP > 0).Count() == 0){
            battleSystem.SelectedMove = -1;
            battleSystem.StateMachine.ChangeState(RunTurnState.i);
            return;
        }

        moveSelectionUI.SetMoves(Moves);

        moveSelectionUI.gameObject.SetActive(true);
        moveSelectionUI.OnSelected += OnMoveSelected;
        moveSelectionUI.OnBack += OnBack;

        battleSystem.DialogBox.EnableDialogText(false);
    }

    public override void Execute(){
        moveSelectionUI.HandleUpdate();
    }

    public override void Exit(){
        moveSelectionUI.ClearItems();

        moveSelectionUI.gameObject.SetActive(false);
        moveSelectionUI.OnSelected -= OnMoveSelected;
        moveSelectionUI.OnBack -= OnBack;

        battleSystem.DialogBox.EnableDialogText(true);
    }

    private void OnMoveSelected(int selection){
        battleSystem.SelectedMove = selection;
        battleSystem.StateMachine.ChangeState(RunTurnState.i);   
    }

    private void OnBack(){
        if(battleSystem?.StateMachine != null){
        battleSystem.StateMachine.Pop();
            ActionSelectionState.i.ActionSelectionUI.gameObject.SetActive(true);
        }
    }
}
