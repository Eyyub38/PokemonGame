using System;
using UnityEngine;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;

public class MoveForgetState : State<GameController>{
    [SerializeField] MoveSelectionUI moveSelectionUI;

    GameController gameController;
    
    public List<Move> CurrentMoves {get; set;}
    public MoveBase NewMove {get; set;}
    public int Selection  {get; set;}

    public static MoveForgetState i { get; private set; }

    void Awake(){
        i = this;
    }

    public override void Enter(GameController owner){
        gameController = owner;
        Selection = 0;
        moveSelectionUI.gameObject.SetActive(true);
        moveSelectionUI.SetMoveSelectionBars(CurrentMoves, NewMove);
        moveSelectionUI.SetMoveDetails(CurrentMoves[0].Base, NewMove);

        moveSelectionUI.OnSelected += OnMoveSelected;
        moveSelectionUI.OnBack += OnBack;
    }

    public override void Execute(){
        moveSelectionUI.HandleUpdate();
    }

    public override void Exit(){
        moveSelectionUI.gameObject.SetActive(false);
        moveSelectionUI.OnSelected -= OnMoveSelected;
        moveSelectionUI.OnBack -= OnBack;
    }

    private void OnMoveSelected(int selectedMove){
        Selection = selectedMove;
        gameController.StateMachine.Pop();
    }
    
    private void OnBack(){
        Selection = -1;
        gameController.StateMachine.Pop();
    }
}
