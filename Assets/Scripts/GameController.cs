using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public enum GameState{ FreeRoam, Battle, Dialog, CutScene }

public class GameController : MonoBehaviour{
    GameState state;
    [SerializeField] PlayerController playerController;
    [SerializeField] BattleSystem battleSystem;
    [SerializeField] Camera worldCamera;

    public static GameController Instance { get; private set; }

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
        playerController.OnEncountered += StartBattle;
        battleSystem.OnBattleOver += EndBattle;

        playerController.OnEnterTrainersView += (Collider2D trainerCollider) =>{
            var trainer = trainerCollider.GetComponentInParent<TrainerController>();
            if(trainer != null){
                state = GameState.CutScene;
                StartCoroutine(trainer.TriggerTrainerBattle(playerController));
            }
        };

        DialogManager.i.OnShowDialog += () => state = GameState.Dialog;
        DialogManager.i.OnCloseDialog += () =>{
            if(state == GameState.Dialog){
                state = GameState.FreeRoam;
            }
        };
    }

    void EndBattle(bool won){
        state = GameState.FreeRoam;
        battleSystem.gameObject.SetActive(false);
        worldCamera.gameObject.SetActive(true);
    }

    void StartBattle(){
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true);
        worldCamera.gameObject.SetActive(false);

        var playerParty = playerController.GetComponent<PokemonParty>();
        var wildPokemon = FindObjectOfType<MapArea>().GetComponent<MapArea>().GetRandomWildPokemon();

        battleSystem.StartBattle(playerParty, wildPokemon);
    }

    public void StartTrainerBattle(TrainerController trainer){
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true);
        worldCamera.gameObject.SetActive(false);

        var playerParty = playerController.GetComponent<PokemonParty>();
        var trainerParty = trainer.GetComponent<PokemonParty>();

        battleSystem.StartTrainerBattle(playerParty, trainerParty);
    }
}
