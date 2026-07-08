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

        var pokemon = battleSystem.UnitInSelection.Pokemon;

        if (!Moves.Any(move => pokemon.CanUseMove(move, battleSystem.ActiveVitalProfile))){
            battleSystem.AddBattleAction(new BattleAction(){
                Type = BattleActionType.Move,
                SelectedMove = new Move(GlobalSettings.i.BackUpMove),
                Target = battleSystem.EnemyUnits[0]
            });
            return;
        }

        moveSelectionUI.SetMoves(Moves, pokemon, battleSystem.ActiveVitalProfile);

        moveSelectionUI.gameObject.SetActive(true);
        moveSelectionUI.OnSelected += OnMoveSelected;
        moveSelectionUI.OnBack += OnBack;

        battleSystem.DialogBox.EnableDialogText(false);
    }

    public override void Execute(){}

    public override void Exit(){
        moveSelectionUI.ClearItems();

        moveSelectionUI.gameObject.SetActive(false);
        moveSelectionUI.OnSelected -= OnMoveSelected;
        moveSelectionUI.OnBack -= OnBack;

        battleSystem.DialogBox.EnableDialogText(true);
    }

    private void OnMoveSelected(int selection){
        StartCoroutine(OnMoveSelectedAsync(selection));
    }

    IEnumerator OnMoveSelectedAsync(int selection){
        int moveTarget = 0;
        if(battleSystem.ActiveEnemyUnitsCount > 1){
            yield return battleSystem.StateMachine.PushAndWait(TargetSelectionState.i);
            if(!TargetSelectionState.i.SelectionMade){
                yield break;
            } else {
                moveTarget = TargetSelectionState.i.SelectedTarget;
            }
        }
        battleSystem.AddBattleAction(new BattleAction(){
            Type = BattleActionType.Move,
            SelectedMove = Moves[selection],
            Target = battleSystem.EnemyUnits[moveTarget]
        });
    }

    private void OnBack(){
        if(battleSystem?.StateMachine != null){
            battleSystem.StateMachine.ChangeState(ActionSelectionState.i);
        }
    }
}
