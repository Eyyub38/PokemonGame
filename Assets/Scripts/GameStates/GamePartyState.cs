using UnityEngine;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;

public class GamePartyState : State<GameController>{
    [SerializeField] PartyScreen partyScreen;
    GameController gameController;

    public static GamePartyState i{get; private set;}

    void Awake(){
        i = this;
    }

    public override void Enter(GameController owner){
        gameController = owner;
        partyScreen.gameObject.SetActive(true);

        partyScreen.OnSelected += OnPokemonSelected;
        partyScreen.OnBack += OnBack;
    }

    public override void Execute(){
        partyScreen.HandleUpdate();
    }

    void OnPokemonSelected(int selectedPokemon){
        if(gameController.StateMachine.GetPrevState() == InventoryState.i){
            StartCoroutine(GoToUseItemState());
        } else {
            Debug.Log("Summary Screen");
        }
    }

    IEnumerator GoToUseItemState(){
        yield return gameController.StateMachine.PushAndWait(UseItemState.i);
        gameController.StateMachine.Pop();
    }

    void OnBack(){
        gameController.StateMachine.Pop();
    }

    public override void Exit(){
        partyScreen.gameObject.SetActive(false);
        partyScreen.OnSelected -= OnPokemonSelected;
        partyScreen.OnBack -= OnBack;
    }
}
