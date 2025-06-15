using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;

public enum GameState{ FreeRoam, Battle, Dialog, PartyScreen, Evolution, Menu, Bag, Cutscene , Shop, Paused }

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
    GameState stateBeforeEvolution;

    TrainerController trainer;

    public static GameController i { get; private set; }
    public GameObject LocationUI => locationUI;
    public Text LocationText => locationText;

    public StateMachine<GameController> StateMachine { get; private set; }
    public GameState State { get { return state; } }
    public GameState PrevState { get; private set; }
    public SceneDetails CurrentScene {get; private set;}
    public SceneDetails PrevScene {get; private set;}
    public PlayerController PlayerController => playerController;
    public Camera WorldCamera => worldCamera;

    private void SetState(GameState newState){
        PrevState = state;
        state = newState;
    }

    private void Awake(){
        i = this;

        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;

        PokemonDB.Init();
        MoveDB.Init();
        ConditionsDB.Init();
        ItemDB.Init();
        QuestDB.Init();
    }

    public void Update(){
        StateMachine.Execute();

        if(state == GameState.Cutscene){
            playerController.Character.HandleUpdate();
        } else if(state == GameState.Dialog){
            DialogManager.i.HandleUpdate();
        } else if(state == GameState.Shop){
            ShopController.i.HandleUpdate();
        }
    }

    void Start(){
        StateMachine = new StateMachine<GameController>(this);
        StateMachine.ChangeState(FreeRoamState.i);

        battleSystem.OnBattleOver += EndBattle;
        partyScreen.Init();
        DialogManager.i.OnShowDialog += () => SetState(GameState.Dialog);
        DialogManager.i.OnDialogFinished += () =>{
            if(state == GameState.Dialog){
                SetState(PrevState);
            }
        };

        EvolutionManager.i.OnStartEvolution += () => {
            stateBeforeEvolution = state;    
            SetState(GameState.Evolution);
        };
        EvolutionManager.i.OnCompleteEvolution += () => {
            partyScreen.SetPartyData();
            SetState(stateBeforeEvolution);

            AudioManager.i.PlayMusic(CurrentScene.SceneMusic, fade: true);
        };

        ShopController.i.OnStart += () => SetState(GameState.Shop);
        ShopController.i.OnFinish += () => SetState(GameState.FreeRoam);
    }

    public void OnEnterTrainersView(TrainerController trainer){
        SetState(GameState.Cutscene);
        StartCoroutine(trainer.TriggerTrainerBattle(playerController));
    }

    void EndBattle(bool won){
        if(trainer != null && won == true){
            trainer.BattleLost();
            trainer = null;
        }
        partyScreen.SetPartyData();

        SetState(GameState.FreeRoam);
        
        battleSystem.gameObject.SetActive(false);
        worldCamera.gameObject.SetActive(true);

        var playerParty = playerController.GetComponent<PokemonParty>();
        bool hasEvolutions = playerParty.CheckForEvolutions();
        if(hasEvolutions){
            StartCoroutine(playerParty.RunEvolution());
        } else {
            AudioManager.i.PlayMusic(CurrentScene.SceneMusic, fade: true);
        }
    }

    public void StartBattle(BattleTrigger trigger){
        BattleState.i.trigger = trigger;
        StateMachine.Push(BattleState.i);
    }

    public void StartTrainerBattle(TrainerController trainer){
        BattleState.i.trainer = trainer;
        StateMachine.Push(BattleState.i);
    }

    public void StartCutsceneState(){
        SetState(GameState.Cutscene);
    }

    public void StartFreeRoamState(){
        SetState(GameState.FreeRoam);
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

    public IEnumerator MoveCamera(Vector2 moveOffset, bool waitForFadeOut = false){
        yield return Fader.i.FadeIn(0.5f);
        worldCamera.transform.position +=new Vector3(moveOffset.x, moveOffset.y);

        if(waitForFadeOut){
            yield return Fader.i.FadeOut(0.5f);
        } else {
            StartCoroutine(Fader.i.FadeOut(0.5f));
        }
    }

    private void OnGUI(){
        var style = new GUIStyle();
        style.fontSize = 24;

        GUILayout.Label("State Stack", style);
        if(StateMachine?.StateStack != null){
        foreach(var state in StateMachine.StateStack){
                if(state != null){
            GUILayout.Label(state.GetType().ToString(), style);
                } else {
                    GUILayout.Label("null", style);
                }
            }
        }
    }  
}
