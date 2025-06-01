using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public enum GameState{ FreeRoam, Battle, Dialog, CutScene, Paused }

public class GameController : MonoBehaviour{
    [SerializeField] PlayerController playerController;
    [SerializeField] BattleSystem battleSystem;
    [SerializeField] Camera worldCamera;

    [SerializeField] GameObject locationUI;
    [SerializeField] Text locationText;

    GameState state;
    GameState stateBeforePause;
    TrainerController trainer;

    public static GameController Instance { get; private set; }
    public GameObject LocationUI => locationUI;
    public Text LocationText => locationText;
    public SceneDetails CurrentScene {get; private set;}
    public SceneDetails PrevScene {get; private set;}

    private void Awake(){
        ConditionsDB.Init();
        Instance = this;
    }

    public void Update(){
        if(state == GameState.FreeRoam){
            playerController.HandleUpdate();
        } else if(state == GameState.Battle){
            battleSystem.HandleUpdate();
        } else if(state == GameState.Dialog){
            DialogManager.i.HandleUpdate();
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
}
