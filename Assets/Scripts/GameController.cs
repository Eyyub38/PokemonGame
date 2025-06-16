using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;

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

    TrainerController trainer;

    public static GameController i { get; private set; }
    public GameObject LocationUI => locationUI;
    public Text LocationText => locationText;

    public StateMachine<GameController> StateMachine { get; private set; }
    public SceneDetails CurrentScene {get; private set;}
    public SceneDetails PrevScene {get; private set;}
    public PlayerController PlayerController => playerController;
    public Camera WorldCamera => worldCamera;
    public PartyScreen PartyScreen => partyScreen;

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
    }

    void Start(){
        StateMachine = new StateMachine<GameController>(this);
        StateMachine.ChangeState(FreeRoamState.i);

        battleSystem.OnBattleOver += EndBattle;
        partyScreen.Init();
        DialogManager.i.OnShowDialog += () => StateMachine.Push(DialogState.i);
        DialogManager.i.OnDialogFinished += () =>{
            StateMachine.Pop();
        };
    }

    public void OnEnterTrainersView(TrainerController trainer){
        StartCoroutine(trainer.TriggerTrainerBattle(playerController));
    }

    void EndBattle(bool won){
        if(trainer != null && won == true){
            trainer.BattleLost();
            trainer = null;
        }
        partyScreen.SetPartyData();
        
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

    public void PauseGame(bool pause){
        if(pause){
            StateMachine.Push(PauseState.i);
        } else {
            StateMachine.Pop();
        }
    }

    public void SetCurrentScene(SceneDetails currScene){
        PrevScene = CurrentScene;
        CurrentScene = currScene;
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
        style.fontSize = 72;

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
