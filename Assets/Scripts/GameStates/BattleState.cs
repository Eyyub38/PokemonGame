using UnityEngine;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;

public class BattleState : State<GameController>{
    [Tooltip("Battle system used while this state is active.")]
    [SerializeField] BattleSystem battleSystem;

    GameController gameController;
    
    public BattleTrigger trigger { get; set; }
    public TrainerController trainer { get; set; }
    public Pokemon WildPokemonOverride { get; set; }
    public BattleSystem BattleSystem => battleSystem;

    public static BattleState i { get; private set; }

    void Awake(){
        i = this;
    }

    public override void Enter(GameController owner){
        gameController = owner;

        battleSystem.gameObject.SetActive(true);
        gameController.WorldCamera.gameObject.SetActive(false);

        var playerParty = gameController.PlayerController.GetComponent<PokemonParty>();
        var mapArea = gameController.CurrentScene != null ? gameController.CurrentScene.GetComponent<MapArea>() : null;
        if(mapArea == null && trainer == null && WildPokemonOverride == null){
            Debug.LogError("BattleState: Current scene does not have a MapArea.");
            gameController.StateMachine.Pop();
            return;
        }
        

        if(trainer == null){
            var wildPokemon = WildPokemonOverride ?? mapArea.GetRandomWildPokemon(trigger);
            if(wildPokemon == null){
                gameController.StateMachine.Pop();
                return;
            }
            battleSystem.StartBattle(playerParty, wildPokemon, trigger, weather: mapArea != null ? mapArea.Weather : WeatherConditionID.None);
        } else {
            var trainerParty = trainer.GetComponent<PokemonParty>();
            battleSystem.StartTrainerBattle(playerParty, trainerParty, unitCount: trainer.BattleUnitCount, weather: mapArea != null ? mapArea.Weather : WeatherConditionID.None);
        }

        battleSystem.OnBattleOver += EndBattle;
    }

    public override void Execute(){
        battleSystem.HandleUpdate();
    }

    public override void Exit(){
        battleSystem.gameObject.SetActive(false);
        gameController.WorldCamera.gameObject.SetActive(true);
        battleSystem.OnBattleOver -= EndBattle;
        if(BattleRuleManager.i != null
            && BattleRuleManager.i.CurrentContext != null
            && (battleSystem.RuleContext == BattleRuleManager.i.CurrentContext || !battleSystem.IsBattleOver)){
            BattleRuleManager.i.ClearCurrent();
        }
        WildPokemonOverride = null;
    }

    void EndBattle(bool won){
        if(trainer != null && won == true){
            trainer.BattleLost();
            trainer = null;
        }

        gameController.StateMachine.Pop();
    }
}
