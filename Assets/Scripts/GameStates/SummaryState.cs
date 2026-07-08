using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;
using GDEUtills.GenerciSelectionUI;
using NUnit.Framework.Internal.Filters;

public class SummaryState : State<GameController>{
    [SerializeField] SummaryScreenUI summaryScreenUI;

    GameController gameController;
    List<Pokemon> playerParty;
    
    int selectedPage = 0;
    float navTimer = 0f;
    const float navSpeed = 7f;

    public int SelectedPokemonIndex { get; set; }
    public static SummaryState i { get; private set; }

    bool CanNav() => navTimer <= 0f;
    void TickNavTimer() => navTimer = Mathf.Max(0f, navTimer - Time.deltaTime);
    void ResetNavTimer() => navTimer = 1f / navSpeed;

    void Awake(){
        i = this;
    }

    void Start(){
        playerParty = PlayerController.i.GetComponent<PokemonParty>().Pokemons;
    }

    public override void Enter(GameController owner){
        gameController = owner;
        gameController.InputMaps.EnableUI();
        
        summaryScreenUI.InputSource = InputRouter.i.UI;
        
        summaryScreenUI.gameObject.SetActive(true);
        summaryScreenUI.SetBasicDetails(playerParty[SelectedPokemonIndex]);
        summaryScreenUI.ShowPage(selectedPage);
        summaryScreenUI.SetTypeImage();
    }

    public override void Execute() {
        TickNavTimer();

        var input = summaryScreenUI.InputSource;

        if(input != null && CanNav()) {
            Vector2 navVector = input.Navigate;
            int prevPage = selectedPage;

            if(!summaryScreenUI.InMoveSelection && (input.LeftPressedThisFrame || input.RightPressedThisFrame)) {
                selectedPage = 1 - selectedPage;
                if(selectedPage != prevPage) {
                    summaryScreenUI.ShowPage(selectedPage);
                    ResetNavTimer();
                }
            }

            if(input.BackPressedThisFrame) {
                if(summaryScreenUI.InMoveSelection) {
                    summaryScreenUI.InMoveSelection = false;
                } else {
                    gameController.StateMachine.Pop();
                    return;
                }
            }

            if(input.SelectPressedThisFrame) {
                if(selectedPage == 1 && !summaryScreenUI.InMoveSelection) {
                    summaryScreenUI.InMoveSelection = true;
                }
            }

            if(input.DownPressedThisFrame) {
                if(!summaryScreenUI.InMoveSelection) {
                    SelectedPokemonIndex = (SelectedPokemonIndex + 1) % playerParty.Count;
                    Refresh();
                    ResetNavTimer();
                }
            } else if(input.UpPressedThisFrame) {
                if(!summaryScreenUI.InMoveSelection) {
                    SelectedPokemonIndex = (SelectedPokemonIndex - 1 + playerParty.Count) % playerParty.Count;
                    Refresh();
                    ResetNavTimer();
                }
            }

            if (summaryScreenUI.InMoveSelection)
                summaryScreenUI.HandleUpdate();
        }
    }

    void Refresh() {
        summaryScreenUI.SetBasicDetails(playerParty[SelectedPokemonIndex]);
        summaryScreenUI.ShowPage(selectedPage);
        summaryScreenUI.SetTypeImage();
    }

    public override void Exit(){
        summaryScreenUI.gameObject.SetActive(false);
    }
}
