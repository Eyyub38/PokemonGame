using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Unity.VisualScripting;
using System.Collections.Generic;

public enum GameState{ FreeRoam, Battle, Dialog, Menu, CutScene, Paused }

public class GameController : MonoBehaviour{
    [SerializeField] PlayerController playerController;
    [SerializeField] BattleSystem battleSystem;
    [SerializeField] Camera worldCamera;

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
        }
    }

    void Start(){
        battleSystem.OnBattleOver += EndBattle;
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

        } else if(selectedItem == 1){

        } else if(selectedItem == 2){
            SavingSystem.i.Save("saveSlot1");
        } else if(selectedItem == 3){
            SavingSystem.i.Load("saveSlot1");
        }
        state = GameState.FreeRoam;
    }
}
