using UnityEngine;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;

public class BattlePartyState : State<BattleSystem>{
    BattleSystem battleSystem;

    public static BattlePartyState i {get; private set;}

    void Awake(){
        i = this;
    }

    public override void Enter(BattleSystem owner){
        battleSystem = owner;
        
        battleSystem.PartyScreen.gameObject.SetActive(true);
        battleSystem.PartyScreen.Init();
        battleSystem.PartyScreen.OnSelected += OnPokemonSelected;
        battleSystem.PartyScreen.OnBack += OnBack;
    }

    public override void Execute(){
        battleSystem.PartyScreen.HandleUpdate();
    }

    void OnPokemonSelected(int selectedIndex){
        var selectedMember = battleSystem.PartyScreen.SelectedMember;
        if(selectedMember.HP <= 0){
            battleSystem.PartyScreen.SetMessageText($"{selectedMember.Base.Name} is fainted. You cannot send out to battle.");
            return;
        }
        if(selectedMember == battleSystem.PlayerUnit.Pokemon){
            battleSystem.PartyScreen.SetMessageText($"{selectedMember.Base.Name} is already in battle.");
            return;
        }

        battleSystem.SelectedPokemon = selectedMember;
        battleSystem.PartyScreen.gameObject.SetActive(false);
        battleSystem.PartyScreen.OnSelected -= OnPokemonSelected;
        battleSystem.PartyScreen.OnBack -= OnBack;
        
        battleSystem.StateMachine.Pop();
    }

    void OnBack(){
        if(battleSystem.PlayerUnit.Pokemon.HP <=0){
            battleSystem.PartyScreen.SetMessageText("Your Pokemon is fainted! You need to choose new Pokemon");
            return;
        }
        battleSystem.PartyScreen.gameObject.SetActive(false);
        battleSystem.PartyScreen.OnSelected -= OnPokemonSelected;
        battleSystem.PartyScreen.OnBack -= OnBack;
        
        battleSystem.StateMachine.Pop();
    }

    public override void Exit(){
        battleSystem.PartyScreen.gameObject.SetActive(false);
        battleSystem.PartyScreen.OnSelected -= OnPokemonSelected;
        battleSystem.PartyScreen.OnBack -= OnBack;
    }
} 