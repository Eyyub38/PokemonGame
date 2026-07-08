using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;

public class GameController : MonoBehaviour{
    [Header("References")]
    [SerializeField] PlayerController playerController;
    [SerializeField] BattleSystem battleSystem;
    [SerializeField] Camera worldCamera;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] InventoryUI inventoryUI;

    [Header("LocationUI")]
    [SerializeField] GameObject locationUI;
    [SerializeField] Text locationText;

    [Header("Input")]
    [SerializeField] InputMapController inputMaps;
    
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
    public InputMapController InputMaps => inputMaps;

    private void Awake(){
        i = this;

        inputMaps = GetComponent<InputMapController>();

        PokemonDB.Init();
        MoveDB.Init();
        StatusConditionsDB.Init();
        WeatherConditionsDB.Init();
        ItemDB.Init();
        QuestDB.Init();
    }

    public void Update(){
        StateMachine.Execute();
    }

    void Start(){
        StateMachine = new StateMachine<GameController>(this);
        StateMachine.ChangeState(FreeRoamState.i);
        PlayerActivityContext.ClearAll();

        if(playerController != null) {
            var installer = playerController.GetComponent<PlayerSystemsInstaller>() ?? playerController.gameObject.AddComponent<PlayerSystemsInstaller>();
            installer.Install();
        }

        battleSystem.OnBattleOver += EndBattle;
        partyScreen.Init();
        DialogManager.i.OnShowDialog += () => { 
            EnterDialogState();
        };
        DialogManager.i.OnDialogFinished += () =>{
            ExitDialogState();
        };

        if(SpeechBubbleDialogManager.i != null) {
            SpeechBubbleDialogManager.i.OnDialogStarted += EnterDialogState;
            SpeechBubbleDialogManager.i.OnDialogFinished += ExitDialogState;
        }
    }

    void EnterDialogState() {
        inputMaps.EnableUI();
        StateMachine.Push(DialogState.i);
    }

    void ExitDialogState() {
        StateMachine.Pop();
        if(StateMachine.CurrentState is FreeRoamState) {
            inputMaps.EnablePlayer();
        } else {
            inputMaps.EnableUI();
        }
        if (StateMachine.CurrentState == null) {
            Debug.LogWarning("No current state after pop.");
        }
    }

    public void OnEnterTrainersView(TrainerController trainer){
        StartCoroutine(trainer.TriggerTrainerBattle(playerController));
    }

    void EndBattle(bool won){
        if(won == true && playerController != null){
            int battleXp = trainer != null ? 80 + trainer.BattleUnitCount * 40 : 35;
            playerController.GetComponent<PlayerProgression>()?.AddExperience(battleXp, PlayerExperienceSource.Battle);
        }

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
        BattleState.i.WildPokemonOverride = null;
        StateMachine.Push(BattleState.i);
    }

    public void StartWildBattle(Pokemon wildPokemon, BattleTrigger trigger = BattleTrigger.LongGrass){
        if(wildPokemon == null){
            Debug.LogWarning("GameController.StartWildBattle called with null Pokemon.");
            return;
        }

        BattleState.i.trigger = trigger;
        BattleState.i.trainer = null;
        BattleState.i.WildPokemonOverride = wildPokemon;
        StateMachine.Push(BattleState.i);
    }

    public void StartTrainerBattle(TrainerController trainer){
        this.trainer = trainer;
        BattleState.i.trainer = trainer;
        BattleState.i.WildPokemonOverride = null;
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
        worldCamera.transform.position += new Vector3(moveOffset.x, moveOffset.y);

        if(waitForFadeOut){
            yield return Fader.i.FadeOut(0.5f);
        } else {
            StartCoroutine(Fader.i.FadeOut(0.5f));
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // Cached style — created once to avoid per-frame heap allocations from new GUIStyle().
    static readonly GUIStyle debugLabelStyle = new GUIStyle { fontSize = 72 };

    private void OnGUI(){
        GUILayout.Label("State Stack", debugLabelStyle);
        if(StateMachine?.StateStack != null){
            foreach(var state in StateMachine.StateStack){
                GUILayout.Label(state != null ? state.GetType().ToString() : "null", debugLabelStyle);
            }
        }
    }
#endif
}
