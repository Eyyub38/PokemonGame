using UnityEngine;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class AboutToUseState : State<BattleSystem>{
    BattleSystem battleSystem;
    bool aboutToUseChoice = true;

    public Pokemon NewPokemon { get; set; }

    public static AboutToUseState i { get; private set; }

    private void Awake(){
        i = this;
    }

    public override void Enter(BattleSystem owner){
        battleSystem = owner;
        StartCoroutine(StartState());
    }

    public override void Execute(){
        if(!battleSystem.DialogBox.IsChoiceBoxEnabled){
            return;
        }
        
        if(Keyboard.current.upArrowKey.isPressed || Keyboard.current.downArrowKey.isPressed){
            aboutToUseChoice = !aboutToUseChoice;
        }

        battleSystem.DialogBox.UpdateChoiceSelection(aboutToUseChoice);
        if(Keyboard.current.enterKey.isPressed){
            battleSystem.DialogBox.EnableChoiceBox(false);
            if(aboutToUseChoice == true){
                StartCoroutine(SwitchCountinueBattle());
            } else {
                StartCoroutine(CountinueBattle());
            }
        } else if(Keyboard.current.escapeKey.isPressed){
            battleSystem.DialogBox.EnableChoiceBox(false);
            StartCoroutine(CountinueBattle());
        }
    }

    IEnumerator StartState(){
        yield return battleSystem.DialogBox.TypeDialog($"{battleSystem.Trainer.Name} is about to use {NewPokemon.Base.Name}. Do you want to change your Pokemon?");
        battleSystem.DialogBox.EnableChoiceBox(true);
    }

    IEnumerator CountinueBattle(){
        yield return battleSystem.SendNextTrainerPokemon();
        battleSystem.StateMachine.Pop();
    }
    
    IEnumerator SwitchCountinueBattle(){
        yield return GameController.i.StateMachine.PushAndWait(PartyState.i);
        var selectedPokemon = PartyState.i.SelectedPokemon;
        if(selectedPokemon != null){
            yield return battleSystem.SwitchPokemon(selectedPokemon, battleSystem.PlayerUnits[0]);
        }

        yield return CountinueBattle();
    }
}
