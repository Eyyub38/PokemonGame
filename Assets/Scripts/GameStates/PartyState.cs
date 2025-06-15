using UnityEngine;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;

public class PartyState : State<GameController>{
    [SerializeField] PartyScreen partyScreen;
    GameController gameController;

    public Pokemon SelectedPokemon {get; set;}
    public BattleSystem BattleSystem {get; set;}

    public static PartyState i{get; private set;}

    void Awake(){
        i = this;
    }

    public override void Enter(GameController owner){
        gameController = owner;

        SelectedPokemon = null;
        
        if(BattleSystem != null){
            partyScreen = BattleSystem.PartyScreen;
        }
        
        partyScreen.gameObject.SetActive(true);
        partyScreen.OnSelected += OnPokemonSelected;
        partyScreen.OnBack += OnBack;
    }

    public override void Execute(){
        partyScreen.HandleUpdate();
    }

    void OnPokemonSelected(int selectedPokemon){
        SelectedPokemon = partyScreen.SelectedMember;
        var prevState = gameController.StateMachine.GetPrevState();

        if(prevState == InventoryState.i){
            StartCoroutine(GoToUseItemState());
        } else if(prevState == BattleState.i || BattleSystem != null){
            BattleSystem battleSystem = BattleSystem;
            if(battleSystem == null && prevState == BattleState.i){
                var battleState = prevState as BattleState;
                battleSystem = battleState.BattleSystem;
            }

            if(SelectedPokemon.HP <= 0){
                partyScreen.SetMessageText($"{SelectedPokemon.Base.Name} is fainted. You cannot send out to battle.");
                return;
            }
            if(SelectedPokemon == battleSystem.PlayerUnit.Pokemon){
                partyScreen.SetMessageText($"{SelectedPokemon.Base.Name} is already in battle.");
                return;
            }
        
            battleSystem.SelectedPokemon = SelectedPokemon;
            gameController.StateMachine.Pop();
        } else {
            Debug.Log("Summary Screen");
        }
    }

    IEnumerator GoToUseItemState(){
        yield return gameController.StateMachine.PushAndWait(UseItemState.i);
        gameController.StateMachine.Pop();
    }

    void OnBack(){
        SelectedPokemon = null;

        var prevState = gameController.StateMachine.GetPrevState();
        if(prevState == BattleState.i || BattleSystem != null){
            BattleSystem battleSystem = BattleSystem;
            if(battleSystem == null && prevState == BattleState.i){
                var battleState = prevState as BattleState;
                battleSystem = battleState.BattleSystem;
            }

            if(battleSystem.PlayerUnit.Pokemon.HP <=0){
                    partyScreen.SetMessageText("Your Pokemon is fainted! You need to choose new Pokemon");
                    return;
                }
                partyScreen.gameObject.SetActive(false);
            gameController.StateMachine.Pop();
        } else {
            gameController.StateMachine.Pop();
        }
    }

    public override void Exit(){
        partyScreen.gameObject.SetActive(false);
        partyScreen.OnSelected -= OnPokemonSelected;
        partyScreen.OnBack -= OnBack;
    }
}
