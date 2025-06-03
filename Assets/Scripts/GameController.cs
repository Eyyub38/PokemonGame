using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum GameState{ FreeRoam, Battle, Dialog, PartyScreen, Menu, Bag, CutScene, Paused }

public class GameController : MonoBehaviour{
    [SerializeField] PlayerController playerController;
    [SerializeField] BattleSystem battleSystem;
    [SerializeField] Camera worldCamera;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] InventoryUI inventoryUI;

    [SerializeField] GameObject locationUI;
    [SerializeField] Text locationText;

    GameState state;
    GameState stateBeforePause;
    TrainerController trainer;
    MenuController menuController;

    public static GameController Instance { get; private set; }
    public GameObject LocationUI => locationUI;
    public Text LocationText => locationText;
    public SceneDetails CurrentScene {get; private set;}
    public SceneDetails PrevScene {get; private set;}

    private void Awake(){
        Instance = this;

        menuController = GetComponent<MenuController>();

        PokemonDB.Init();
        MoveDB.Init();
        ConditionsDB.Init();
    }

    public void Update(){
        if(state == GameState.FreeRoam){
            playerController.HandleUpdate();
            if(Input.GetKeyDown(KeyCode.Tab)){
                menuController.OpenMenu();
                state = GameState.Menu;
            }
        } else if(state == GameState.Battle){
            battleSystem.HandleUpdate();
        } else if(state == GameState.Dialog){
            DialogManager.i.HandleUpdate();
        } else if(state == GameState.Menu){
            menuController.HandleUpdate();
        } else if(state == GameState.PartyScreen){
            Action OnSelected = () => {
                //Summary Screen
                Debug.Log("Summary Screen");
            };
            Action onBack = () => {
                partyScreen.gameObject.SetActive(false);
                state = GameState.FreeRoam;
            };
            partyScreen.HandleUpdate(OnSelected, onBack);
        } else if(state == GameState.Bag){
            Action onBack = () => {
                inventoryUI.gameObject.SetActive(false);
                state = GameState.FreeRoam;
            };
            inventoryUI.HandleUpdate(onBack);
        }
    }

    void Start(){
        battleSystem.OnBattleOver += EndBattle;
        partyScreen.Init();
        DialogManager.i.OnShowDialog += () => state = GameState.Dialog;
        DialogManager.i.OnCloseDialog += () =>{
            if(state == GameState.Dialog){
                state = GameState.FreeRoam;
            }
        };

        menuController.onBack += () => state = GameState.FreeRoam;
        menuController.onMenuSelected += OnMenuSelected;
    }

    public void OnEnterTrainersView(TrainerController trainer){
        state = GameState.CutScene;
        StartCoroutine(trainer.TriggerTrainerBattle(playerController));
    }

    void EndBattle(bool won){
        if(trainer != null && won == true){
            trainer.BattleLost();
            trainer = null;
        }
        state = GameState.FreeRoam;
        battleSystem.gameObject.SetActive(false);
        worldCamera.gameObject.SetActive(true);
    }

    public void StartBattle(){
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true);
        worldCamera.gameObject.SetActive(false);

        var playerParty = playerController.GetComponent<PokemonParty>();
        var wildPokemon = CurrentScene.GetComponent<MapArea>().GetRandomWildPokemon();

        var enemyPokemon = new Pokemon(wildPokemon.Base, wildPokemon.Level);
        battleSystem.StartBattle(playerParty, enemyPokemon);
    }

    public void StartTrainerBattle(TrainerController trainer){
        this.trainer = trainer;

        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true);
        worldCamera.gameObject.SetActive(false);

        var playerParty = playerController.GetComponent<PokemonParty>();
        var trainerParty = trainer.GetComponent<PokemonParty>();

        battleSystem.StartTrainerBattle(playerParty, trainerParty);
    }

    public void PauseGame(bool pause){
        if(pause){
            stateBeforePause = state;
            state = GameState.Paused;
        } else {
            state = stateBeforePause;
        }
    }

    public void SetCurrentScene(SceneDetails currScene){
        PrevScene = CurrentScene;
        CurrentScene = currScene;
    }

    void OnMenuSelected(int selectedItem){
        if(selectedItem == 0){
            //Pokemon
            partyScreen.gameObject.SetActive(true);
            partyScreen.SetPartyData(playerController.GetComponent<PokemonParty>().Pokemons);
            state = GameState.PartyScreen;
        } else if(selectedItem == 1){
            //Bag
            inventoryUI.gameObject.SetActive(true);
            state = GameState.Bag;
        } else if(selectedItem == 2){
            //Save
            SavingSystem.i.Save("saveSlot1");
            state = GameState.FreeRoam;
        } else if(selectedItem == 3){
            //Load
            SavingSystem.i.Load("saveSlot1");
            state = GameState.FreeRoam;
        }
    }
}
