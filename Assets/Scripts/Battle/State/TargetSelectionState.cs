using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;

public class TargetSelectionState : State<BattleSystem>{
    BattleSystem battleSystem;

    int selectedTarget = 0;

    public bool SelectionMade {get; set;}
    public int SelectedTarget => selectedTarget;
    public static TargetSelectionState i {get; private set;}

    void Awake(){
        i = this;
    }

    public override void Enter(BattleSystem owner){
        battleSystem = owner;

        SelectionMade = false;
        selectedTarget = 0;
        UpdateSelectionInUI();
    }

    public override void Execute(){
        int prevSelected = selectedTarget;
        if(Input.GetKeyDown(KeyCode.RightArrow)){
            ++selectedTarget;

        } else if(Input.GetKeyDown(KeyCode.LeftArrow)){
            --selectedTarget;
        }

        selectedTarget = Mathf.Clamp(selectedTarget, 0, battleSystem.UnitCount - 1);

        if(selectedTarget != prevSelected){
            UpdateSelectionInUI();
        }

        if(Input.GetButtonDown("Action")){
            SelectionMade = true;
            battleSystem.StateMachine.Pop();
        } else if(Input.GetButtonDown("Back")){
            SelectionMade = false;
            battleSystem.StateMachine.Pop();
        }
    }

    public override void Exit(){
        battleSystem.EnemyUnits[selectedTarget].SetSelected(false);
    }

    void UpdateSelectionInUI(){
        for(int i = 0; i < battleSystem.EnemyUnits.Count; i++){
            battleSystem.EnemyUnits[i].SetSelected(i == selectedTarget);
        }
    }
}
