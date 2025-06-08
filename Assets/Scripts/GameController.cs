using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum GameState{ FreeRoam, Battle, Dialog, PartyScreen, Menu, Bag, CutScene, Paused }

public class GameController : MonoBehaviour{
    [Header("Referances")]
    [SerializeField] PlayerController playerController;
    [SerializeField] BattleSystem battleSystem;
    [SerializeField] Camera worldCamera;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] InventoryUI inventoryUI;

    [Header("LocationUI")]
    [SerializeField] GameObject locationUI;
    [SerializeField] Text locationText;

    GameState state;
    GameState stateBeforePause;
    TrainerController trainer;
    MenuController menuController;

    public static GameController i { get; private set; }
    public GameObject LocationUI => locationUI;
    public Text LocationText => locationText;

    public GameState State { get { return state; } }
    public GameState PrevState { get; private set; }
    public SceneDetails CurrentScene {get; private set;}
    public SceneDetails PrevScene {get; private set;}

    private void SetState(GameState newState){
        PrevState = state;
        state = newState;
    }

    private void Awake(){
        i = this;

        menuController = GetComponent<MenuController>();

        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;

        PokemonDB.Init();
        MoveDB.Init();
        ConditionsDB.Init();
        ItemDB.Init();
        QuestDB.Init();
    }

    public void Update(){
        if(state == GameState.FreeRoam){
            playerController.HandleUpdate();
            if(Input.GetKeyDown(KeyCode.Tab)){
                menuController.OpenMenu();
                SetState(GameState.Menu);
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
                if(PrevState == GameState.Battle){
                    SetState(GameState.Battle);
                } else {
                    SetState(GameState.FreeRoam);
                }
            };
            partyScreen.HandleUpdate(OnSelected, onBack);
        } else if(state == GameState.Bag){
            Action onBack = () => {
                inventoryUI.gameObject.SetActive(false);
                if(PrevState == GameState.Battle){
                    SetState(GameState.Battle);
                } else {
                    SetState(GameState.FreeRoam);
                }
            };
            inventoryUI.HandleUpdate(onBack);
        }
    }

    void Start(){
        battleSystem.OnBattleOver += EndBattle;
        partyScreen.Init();
        DialogManager.i.OnShowDialog += () => SetState(GameState.Dialog);
        DialogManager.i.OnDialogFinished += () =>{
            if(state == GameState.Dialog){
                SetState(PrevState);
            }
        };

        menuController.onBack += () => SetState(GameState.FreeRoam);
        menuController.onMenuSelected += OnMenuSelected;
    }

    public void OnEnterTrainersView(TrainerController trainer){
        SetState(GameState.CutScene);
        StartCoroutine(trainer.TriggerTrainerBattle(playerController));
    }

    void EndBattle(bool won){
        if(trainer != null && won == true){
            trainer.BattleLost();
            trainer = null;
        }
        SetState(GameState.FreeRoam);
        battleSystem.gameObject.SetActive(false);
        worldCamera.gameObject.SetActive(true);

        var playerParty = playerController.GetComponent<PokemonParty>();
        StartCoroutine(playerParty.CheckForEvolutions());
    }

    public void StartBattle(){
        SetState(GameState.Battle);
        battleSystem.gameObject.SetActive(true);
        worldCamera.gameObject.SetActive(false);

        var playerParty = playerController.GetComponent<PokemonParty>();
        var wildPokemon = CurrentScene.GetComponent<MapArea>().GetRandomWildPokemon();

        var enemyPokemon = new Pokemon(wildPokemon.Base, wildPokemon.Level);
        battleSystem.StartBattle(playerParty, enemyPokemon);
    }

    public void StartTrainerBattle(TrainerController trainer){
        this.trainer = trainer;

        SetState(GameState.Battle);
        battleSystem.gameObject.SetActive(true);
        worldCamera.gameObject.SetActive(false);

        var playerParty = playerController.GetComponent<PokemonParty>();
        var trainerParty = trainer.GetComponent<PokemonParty>();

        battleSystem.StartTrainerBattle(playerParty, trainerParty);
    }

    public void PauseGame(bool pause){
        if(pause){
            stateBeforePause = state;
            SetState(GameState.Paused);
        } else {
            SetState(stateBeforePause);
        }
    }

    public void SetCurrentScene(SceneDetails currScene){
        PrevScene = CurrentScene;
        CurrentScene = currScene;
    }

    void OnMenuSelected(int selectedItem){
        if(selectedItem == 0){
            //Pokemon
            SetState(GameState.PartyScreen);
            partyScreen.gameObject.SetActive(true);
        } else if(selectedItem == 1){
            //Bag
            SetState(GameState.Bag);
            inventoryUI.gameObject.SetActive(true);
        } else if(selectedItem == 2){
            //Save
            SavingSystem.i.Save("saveSlot1");
            SetState(GameState.FreeRoam);
        } else if(selectedItem == 3){
            //Load
            SavingSystem.i.Load("saveSlot1");
            SetState(GameState.FreeRoam);
        }
    }
}
