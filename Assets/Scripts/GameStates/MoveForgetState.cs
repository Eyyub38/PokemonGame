using System;
using UnityEngine;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;

public class MoveForgetState : State<GameController>{
    [SerializeField] MoveForgetSelectionUI moveForgetSelectionUI;

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
        moveForgetSelectionUI.gameObject.SetActive(true);
        moveForgetSelectionUI.SetMoveSelectionBars(CurrentMoves, NewMove);
        moveForgetSelectionUI.SetMoveDetails(CurrentMoves[0].Base, NewMove);

        moveForgetSelectionUI.OnSelected += OnMoveSelected;
        moveForgetSelectionUI.OnBack += OnBack;
    }

    public override void Execute(){
        moveForgetSelectionUI.HandleUpdate();
    }

    public override void Exit(){
        moveForgetSelectionUI.gameObject.SetActive(false);
        moveForgetSelectionUI.OnSelected -= OnMoveSelected;
        moveForgetSelectionUI.OnBack -= OnBack;
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
