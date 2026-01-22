using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class TargetSelectionState : State<BattleSystem> {
    [Header("Input")] 
    [SerializeField] InputActionAsset actions;
    [SerializeField] string actionMapName = "UI";
    [SerializeField] string navigateName = "Navigate";
    [SerializeField] string selectName = "Select";
    [SerializeField] string backName = "Back";
    
    BattleSystem battleSystem;
    InputAction navigate;
    InputAction select;
    InputAction back;

    float navTimer = 0f;
    const float navSpeed = 8f;
    
    int selectedTarget = 0;

    public bool SelectionMade {get; set;}
    public int SelectedTarget => selectedTarget;
    public static TargetSelectionState i {get; private set;}

    void Awake(){
        i = this;

        if(actions == null) {
            Debug.LogError("MoveSelectionUI: actions not set");
            enabled = false;
            return;
        }

        var map = actions.FindActionMap(actionMapName);
        navigate = map.FindAction("Navigate");
        select = map.FindAction("Select");
        back = map.FindAction("Back");
    }

    public override void Enter(BattleSystem owner){
        battleSystem = owner;

        SelectionMade = false;
        selectedTarget = 0;
        UpdateSelectionInUI();
        
        navigate.Enable();
        select.Enable();
        back.Enable();
    }

    public override void Execute(){
        navTimer = Mathf.Clamp(navTimer - Time.deltaTime, 0f, navTimer);
        
        int prevSelected = selectedTarget;
        
        Vector2 navVector =  navigate.ReadValue<Vector2>();
        navVector.x = Mathf.RoundToInt(navVector.x);

        if(navTimer == 0 && Mathf.Abs(navVector.x) < 0.2f) {
            selectedTarget += (int)Mathf.Sign(navVector.x);
            navTimer = 1 / navSpeed;
        }

        selectedTarget = Mathf.Clamp(selectedTarget, 0, battleSystem.ActiveEnemyUnitsCount - 1);

        if(selectedTarget != prevSelected) {
            UpdateSelectionInUI();
        }

        if(select.WasPressedThisFrame()) {
            SelectionMade = true;
            battleSystem.StateMachine.Pop();
        } else if(back.WasPressedThisFrame()) {
            SelectionMade = false;
            battleSystem.StateMachine.Pop();
        }
    }

    public override void Exit(){
        navigate.Disable();
        select.Disable();
        back.Disable();
        
        battleSystem.EnemyUnits[selectedTarget].SetSelected(false);
    }

    void UpdateSelectionInUI(){
        for(int i = 0; i < battleSystem.ActiveEnemyUnitsCount; i++){
            battleSystem.EnemyUnits[i].SetSelected(i == selectedTarget);
        }
    }
}
