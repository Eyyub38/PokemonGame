using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;
using System.Linq;

public enum BattleTrigger {LongGrass, Water}

public class BattleSystem : MonoBehaviour{
    [Header("Single Battle")]
    [SerializeField] GameObject singleBattleElements;
    [SerializeField] BattleUnit playerSingleUnit;
    [SerializeField] BattleUnit enemySingleUnit;

    [Header("Multi Battle")]
    [SerializeField] GameObject multiBattleElements;
    [SerializeField] List<BattleUnit> playerMultiUnits;
    [SerializeField] List<BattleUnit> enemyMultiUnits;

    [Header("UI")]
    [SerializeField] BattleDialogBox dialogBox;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] InventoryUI inventoryUI;
    [SerializeField] MoveForgetSelectionUI moveForgetSelectionUI; 
    [SerializeField] DynamicMenuUI dynamicMenuUI;
    
    [Header("Character Images")]
    [SerializeField] Image playerImage;
    [SerializeField] Image trainerImage;

    [Header("Pokeball")]
    [SerializeField] GameObject pokeballSprite;

    [Header("Audio")]
    [SerializeField] AudioClip wildBattleMusic;
    [SerializeField] AudioClip trainerBattleMusic;
    [SerializeField] AudioClip wildBattleVictoryMusic;
    [SerializeField] AudioClip trainerBattleVictoryMusic;

    [Header("Backgrounds & Pokemon Spots")]
    [SerializeField] Image backgroundImages;
    [SerializeField] Image playerUnitSpotImage;
    [SerializeField] Image enemyUnitSpotImage;

    [Header("Battleground Sprites")]
    [SerializeField] Sprite longGrassBackground;
    [SerializeField] Sprite longGrassSpot;
    [SerializeField] Sprite waterBackground;
    [SerializeField] Sprite waterSpot;

    PlayerController player;
    BattleTrigger battleTrigger;
    List<BattleUnit> playerUnits;
    List<BattleUnit> enemyUnits;
    List<BattleAction> battleActions; 
    int unitCount = 1;
    int unitInSelectionIndex = 0;

    public Action<bool> OnBattleOver;

    public int EscapeAttempts {get; set;}
    public int UnitCount => unitCount;
    public bool IsBattleOver {get; private set;}
    public bool IsTrainerBattle {get; private set;} = false;
    public TrainerController Trainer{get; private set;}
    public StateMachine<BattleSystem> StateMachine {get; private set;}
    public PokemonParty PlayerParty {get; private set;}
    public PokemonParty TrainerParty {get; private set;}
    public Pokemon WildPokemon {get; private set;}
    public PartyScreen PartyScreen => partyScreen;
    public InventoryUI InventoryUI => inventoryUI;
    public BattleDialogBox DialogBox => dialogBox;
    public DynamicMenuUI DynamicMenuUI => dynamicMenuUI;
    public MoveForgetSelectionUI MoveForgetSelectionUI => moveForgetSelectionUI;
    public List<BattleUnit> PlayerUnits => playerUnits;
    public List<BattleUnit> EnemyUnits => enemyUnits;
    public BattleUnit UnitInSelection => playerUnits[unitInSelectionIndex];
    public AudioClip WildVicBattleMusic => wildBattleVictoryMusic;
    public AudioClip TrainerVicBattleMusic => trainerBattleVictoryMusic;

    public void StartBattle(PokemonParty playerParty, Pokemon wildPokemon, BattleTrigger trigger = BattleTrigger.LongGrass){
        this.PlayerParty = playerParty;
        this.WildPokemon = wildPokemon;
        this.unitCount = 1;
        player = playerParty.GetComponent<PlayerController>();
        IsTrainerBattle = false;

        battleTrigger = trigger;
        AudioManager.i.PlayMusic(wildBattleMusic);

        StartCoroutine(SetupBattle());
    }
    
    public void StartTrainerBattle(PokemonParty playerParty, PokemonParty trainerParty, BattleTrigger trigger = BattleTrigger.LongGrass, int unitCount = 1){
        this.PlayerParty = playerParty;
        this.TrainerParty = trainerParty;
        this.unitCount = unitCount;
        player = playerParty.GetComponent<PlayerController>();
        Trainer = trainerParty.GetComponent<TrainerController>();
        IsTrainerBattle = true;

        battleTrigger = trigger;
        AudioManager.i.PlayMusic(trainerBattleMusic);

        StartCoroutine(SetupBattle());
    }

    public IEnumerator SetupBattle(){
        singleBattleElements.SetActive(unitCount == 1);
        multiBattleElements.SetActive(unitCount > 1);

        if(unitCount == 1){
            playerUnits = new List<BattleUnit>(){playerSingleUnit};
            enemyUnits = new List<BattleUnit>(){enemySingleUnit};
        } else if(unitCount > 1){
            playerUnits = playerMultiUnits.GetRange(0, playerMultiUnits.Count);
            enemyUnits = enemyMultiUnits.GetRange(0, enemyMultiUnits.Count);
        }

        StateMachine = new StateMachine<BattleSystem>(this);
        battleActions = new List<BattleAction>();

        for(int i = 0; i < unitCount; i++){
            playerUnits[i].Clear();
            enemyUnits[i].Clear();
        }

        if(battleTrigger == BattleTrigger.Water){
            backgroundImages.sprite = waterBackground;
            playerUnitSpotImage.sprite = waterSpot;
            enemyUnitSpotImage.sprite = waterSpot;
        } else {
            backgroundImages.sprite = longGrassBackground;
            playerUnitSpotImage.sprite = longGrassSpot;
            enemyUnitSpotImage.sprite = longGrassSpot;
        }
        if(!IsTrainerBattle){
            playerUnits[0].Setup(PlayerParty.GetHealthyPokemon());
            enemyUnits[0].Setup(WildPokemon);

            dialogBox.SetMoveBars(playerUnits[0].Pokemon.Moves);

            yield return dialogBox.TypeDialog($"A wild {enemyUnits[0].Pokemon.Base.Name} appeared.");
        } else {
            for(int i = 0; i < unitCount; i++){
                playerUnits[i].gameObject.SetActive(false);
                enemyUnits[i].gameObject.SetActive(false);
            }


            playerImage.gameObject.SetActive(true);
            trainerImage.gameObject.SetActive(true);

            playerImage.sprite = player.BattleImage;
            trainerImage.sprite = Trainer.BattleImage;

            yield return dialogBox.TypeDialog($"The battle between you and {Trainer.Name} is started.");

            trainerImage.gameObject.SetActive(false);
            var enemyPokemons = TrainerParty.GetHealthyPokemons(unitCount);
            for(int i = 0; i < unitCount; i++){
                enemyUnits[i].gameObject.SetActive(true);
                enemyUnits[i].Setup(enemyPokemons[i]);
            }
            
            var pokemonNames = String.Join(" and ", enemyPokemons.Select(p => p.Base.Name));
            yield return dialogBox.TypeDialog($"{Trainer.Name} send out {pokemonNames} for battle!");
            
            playerImage.gameObject.SetActive(false);
            var playerPokemons = PlayerParty.GetHealthyPokemons(unitCount);

            for(int i = 0; i < unitCount; i++){
                playerUnits[i].gameObject.SetActive(true);
                playerUnits[i].Setup(playerPokemons[i]);
            }
            
            pokemonNames = String.Join("and", playerPokemons.Select(p => p.Base.Name));
            yield return dialogBox.TypeDialog($"Go {pokemonNames}! I choose you.");
        }

        IsBattleOver = false;
        EscapeAttempts = 0;
        partyScreen.Init();
        unitInSelectionIndex = 0;
        StateMachine.ChangeState(ActionSelectionState.i);
    }

    public void AddBattleAction(BattleAction battleAction){
        battleAction.User = UnitInSelection;
        battleActions.Add(battleAction);

        if(battleActions.Count == unitCount){
            foreach(var enemyUnit in enemyUnits){
                battleActions.Add(new BattleAction{
                    Type = BattleActionType.Move,
                    SelectedMove = enemyUnit.Pokemon.GetRandomMove(),
                    User = enemyUnit,
                    Target = playerUnits[UnityEngine.Random.Range( 0, playerUnits.Count)]
                });
            }

            battleActions = battleActions.OrderByDescending(a => a.Priority).ThenByDescending(a => a.User.Pokemon.Base.Speed).ToList();

            RunTurnState.i.Actions = battleActions;
            StateMachine.ChangeState(RunTurnState.i);
        } else {
            ++unitInSelectionIndex;
            StateMachine.ChangeState(ActionSelectionState.i);
        }
    }

    public void ClearTurnData(){
        battleActions = new List<BattleAction>();
        unitInSelectionIndex = 0;
    }

    public void BattleOver(bool won){
        IsBattleOver = true;
        PlayerParty.Pokemons.ForEach(p => p.OnBattleOver());

        playerUnits.ForEach(u => u.Hud.ClearData());
        enemyUnits.ForEach(u => u.Hud.ClearData());

        OnBattleOver(won);
    }

    public void HandleUpdate(){
        StateMachine.Execute();
    }

    public IEnumerator SwitchPokemon(Pokemon newPokemon, BattleUnit unitToSwitch){
        if (unitToSwitch.Pokemon.HP > 0){
            yield return dialogBox.TypeDialog($"Come back {unitToSwitch.Pokemon.Base.Name}!. Thank you for your hard work!");
            unitToSwitch.PlayFaintedAnimation();
            yield return new WaitForSeconds(2f);
        }

        unitToSwitch.Setup(newPokemon);
        dialogBox.SetMoveBars(newPokemon.Moves);

        yield return dialogBox.TypeDialog($"Your turn {newPokemon.Base.Name}!");
    }

    public IEnumerator SendNextTrainerPokemon(){
        var activePokemons = EnemyUnits.Select(u => u.Pokemon).Where(p => p.HP > 0).ToList();

        var nextPokemon = TrainerParty.GetHealthyPokemon(doNotInclude: activePokemons);
        enemyUnits[0].Setup(nextPokemon);
        yield return dialogBox.TypeDialog($"{Trainer.Name} send out {nextPokemon.Base.Name}!");
    }
    
    public IEnumerator ThrowPokeball(PokeballItem pokeball){

        if (IsTrainerBattle){
            yield return dialogBox.TypeDialog("You are trying to steal someone's pokemon... You cannot do it!");
            yield break;
        }

        var playerUnit = playerUnits[0];
        var enemyUnit = enemyUnits[0];

        yield return dialogBox.TypeDialog($"{player.Name} used {pokeball.Name}");

        var pokeballObj = Instantiate(pokeballSprite, playerUnit.transform.position + new Vector3(0, 2), Quaternion.identity);
        var pokeballRenderer = pokeballObj.GetComponent<SpriteRenderer>();
        var pokeballAnim = pokeballObj.GetComponent<PokeballAnimator>();

        pokeballAnim.PlayThrow(pokeball);

        Vector3 jumpTarget = enemyUnit.transform.position + new Vector3(0, 2f);
        Tween jumpTween = pokeballRenderer.transform
            .DOJump(jumpTarget, 2f, 1, 1.0f);

        yield return jumpTween.WaitForCompletion();
        yield return enemyUnit.PlayCaptureAnimation();

        pokeballAnim.PlayIdle(pokeball, 0.25f);
        yield return pokeballRenderer.transform
            .DOMoveY(enemyUnit.transform.position.y - 1.8f, 0.5f)
            .WaitForCompletion();

        yield return new WaitForSeconds(0.3f);

        int shakeCount = TryCatchPokemon(enemyUnit.Pokemon, pokeball);

        for (int i = 0; i < Mathf.Min(shakeCount, 3); i++){
            pokeballAnim.PlayShake(pokeball);
            yield return new WaitForSeconds(2f);
        }

        if (shakeCount == 4){
            pokeballAnim.PlayCatch(pokeball);
            yield return dialogBox.TypeDialog($" Congrats!! {enemyUnit.Pokemon.Base.Name} was caught.");
            yield return pokeballRenderer.DOFade(0, 1.5f).WaitForCompletion();

            enemyUnit.Pokemon.Pokeball = pokeball;
            PlayerParty.AddPokemon(enemyUnit.Pokemon);
            yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} has been added to your party.");

            Destroy(pokeballObj);
            BattleOver(true);
        } else {
            yield return new WaitForSeconds(1f);
            pokeballRenderer.DOFade(0, 0.2f);
            yield return enemyUnit.PlayBreakAnimation();

            if (shakeCount < 2){
                yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} broke free.");
            } else {
                yield return dialogBox.TypeDialog($"Almost caught it!");
            }

            Destroy(pokeballObj);
        }
    }
    
    int TryCatchPokemon(Pokemon pokemon, PokeballItem pokeball){
        float a = ( 3 * pokemon.MaxHp - 2 * pokemon.HP) * pokemon.Base.CatchRate * pokeball.CatchRateModifier * ConditionsDB.GetStatusBonus(pokemon.Status) / ( 3 * pokemon.MaxHp);
        if(a >= 255){
            return 4;
        }
        float b = 1048560 / Mathf.Sqrt(Mathf.Sqrt( 16711680 / a));
        int shakeCount = 0;
        while( shakeCount < 4){
            if( UnityEngine.Random.Range( 0, 65535) >= b){
                break;
            }
            ++shakeCount;
        }
        return shakeCount;
    }

    public bool IsPokemonSelectedToShift(Pokemon pokemon){
        return battleActions.Any(a => a.Type == BattleActionType.SwitchPokemon && a.SelectedPokemon == pokemon);
    }
}
