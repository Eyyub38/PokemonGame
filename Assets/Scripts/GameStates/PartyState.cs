using UnityEngine;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;

public class PartyState : State<GameController>{
    [SerializeField] PartyScreen partyScreen;
    GameController gameController;

    public Pokemon SelectedPokemon {get; set;}
    public BattleSystem BattleSystem {get; set;}
    public ItemBase SelectedItem {get; set;}

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
        
        if(SelectedItem is TmItem tmItem){
            partyScreen.ShowIfTmUsable(tmItem);
            partyScreen.SetMessageText($"Choose a Pokemon to teach {tmItem.Move.Name}");
        } else {
            partyScreen.ClearMemberSlotMessage();
            partyScreen.SetMessageText("Choose a Pokemon");
        }
    }

    public override void Execute(){
        partyScreen.HandleUpdate();
    }

    public override void Exit(){
        partyScreen.gameObject.SetActive(false);
        BattleSystem = null;
        partyScreen.OnSelected -= OnPokemonSelected;
        partyScreen.OnBack -= OnBack;
        partyScreen.ClearMemberSlotMessage();
        SelectedItem = null;
    }

    void OnPokemonSelected(int selectedPokemon){
        SelectedPokemon = partyScreen.SelectedMember;

        StartCoroutine(PokemonSelectedAction(selectedPokemon));
    }

    IEnumerator PokemonSelectedAction(int selectedPokemon){
        var prevState = gameController.StateMachine.GetPrevState();

        if(prevState == InventoryState.i){
            yield return gameController.StateMachine.PushAndWait(UseItemState.i);
            gameController.StateMachine.Pop();
        } else if(prevState == BattleState.i || BattleSystem != null){
            BattleSystem battleSystem = BattleSystem;
            battleSystem = BattleState.i.BattleSystem;
            if(battleSystem != null && prevState == BattleState.i){
                var battleState = prevState as BattleState;
                DynamicMenuState.EnsureInstance().MenuItems = new List<string>() {"Shift" ,"Summary", "Cancel"};
                yield return gameController.StateMachine.PushAndWait(DynamicMenuState.i);
                if(DynamicMenuState.i.SelectedItem == 0){
                    if(SelectedPokemon.HP <= 0){
                        partyScreen.SetMessageText($"{SelectedPokemon.Base.Name} is fainted. You cannot send out to battle.");
                        yield break;
                    }
                    if(SelectedPokemon == battleSystem.PlayerUnit.Pokemon){
                        partyScreen.SetMessageText($"{SelectedPokemon.Base.Name} is already in battle.");
                        yield break;
                    }
                
                    battleSystem.SelectedPokemon = SelectedPokemon;
                    gameController.StateMachine.Pop();
                } else if(DynamicMenuState.i.SelectedItem == 1){
                    SummaryState.i.SelectedPokemonIndex = selectedPokemon;
                    yield return gameController.StateMachine.PushAndWait(SummaryState.i);
                } else {
                    yield break;
                }
            }
        } else {
            DynamicMenuState.EnsureInstance().MenuItems = new List<string>() {"Summary","Switch","Cancel"};
            yield return gameController.StateMachine.PushAndWait(DynamicMenuState.i);
            if(DynamicMenuState.i.SelectedItem == 0){
                SummaryState.i.SelectedPokemonIndex = selectedPokemon;
                yield return gameController.StateMachine.PushAndWait(SummaryState.i);
            } else if(DynamicMenuState.i.SelectedItem == 1){
                Debug.Log("Switch Position");
            } else {
                yield break;
            }
        }
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
}
