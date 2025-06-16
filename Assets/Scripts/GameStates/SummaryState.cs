using UnityEngine;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;

public class SummaryState : State<GameController>{
    [SerializeField] SummaryScreenUI summaryScreenUI;

    GameController gameController;
    List<Pokemon> playerParty;
    int selectedPage = 0;

    public int SelectedPokemonIndex { get; set; }

    public static SummaryState i { get; private set; }

    void Awake(){
        i = this;
    }

    void Start(){
        playerParty = PlayerController.i.GetComponent<PokemonParty>().Pokemons;
    }

    public override void Enter(GameController owner){
        gameController = owner;
        summaryScreenUI.gameObject.SetActive(true);
        summaryScreenUI.SetBasicDetails(playerParty[SelectedPokemonIndex]);
        summaryScreenUI.ShowPage(selectedPage);
        summaryScreenUI.SetTypeImage();
    }

    public override void Execute(){

        if(!summaryScreenUI.InMoveSelection){
            int prevPage = selectedPage;
            if(Input.GetKeyDown(KeyCode.LeftArrow)){
                selectedPage = Mathf.Abs(selectedPage - 1) % 2;
            } else if(Input.GetKeyDown(KeyCode.RightArrow)){
                selectedPage = (selectedPage + 1) % 2;
            }
            if(selectedPage != prevPage){
                summaryScreenUI.ShowPage(selectedPage);
            }

            int prevIndex = SelectedPokemonIndex;
            if(Input.GetKeyDown(KeyCode.DownArrow)){
                SelectedPokemonIndex += 1;
                if(SelectedPokemonIndex >= playerParty.Count){
                    SelectedPokemonIndex = 0;
                }
            } else if(Input.GetKeyDown(KeyCode.UpArrow)){
                SelectedPokemonIndex -= 1;
                if(SelectedPokemonIndex < 0){
                    SelectedPokemonIndex = playerParty.Count - 1;
                }
            }
            if(SelectedPokemonIndex != prevIndex){
                summaryScreenUI.SetBasicDetails(playerParty[SelectedPokemonIndex]);
                summaryScreenUI.ShowPage(selectedPage);
                summaryScreenUI.SetTypeImage();
            }
        }

        if(Input.GetKeyDown(KeyCode.Return)){
            if(selectedPage == 1 && !summaryScreenUI.InMoveSelection){
                summaryScreenUI.InMoveSelection = true;
            }
        } else if(Input.GetKeyDown(KeyCode.Escape)){
            if(summaryScreenUI.InMoveSelection){
                summaryScreenUI.InMoveSelection = false;
            } else {
                gameController.StateMachine.Pop();
                return;
            }

        }

        summaryScreenUI.HandleUpdate();
    }

    public override void Exit(){
        summaryScreenUI.gameObject.SetActive(false);
    }
}
