using UnityEngine;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;

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
        
        if(Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)){
            aboutToUseChoice = !aboutToUseChoice;
        }

        battleSystem.DialogBox.UpdateChoiceSelection(aboutToUseChoice);
        if(Input.GetKeyDown(KeyCode.Return)){
            battleSystem.DialogBox.EnableChoiceBox(false);
            if(aboutToUseChoice == true){
                StartCoroutine(SwitchCountinueBattle());
            } else {
                StartCoroutine(CountinueBattle());
            }
        } else if(Input.GetKeyDown(KeyCode.Escape)){
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
            yield return battleSystem.SwitchPokemon(selectedPokemon);
        }

        yield return CountinueBattle();
    }
}
