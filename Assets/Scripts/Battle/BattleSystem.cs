using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;
using System.Linq;

public enum BattleTrigger {LongGrass, Water}

public class BattleSystem : MonoBehaviour, IBattleWeatherProvider{
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

    [Header("AI")]
    [SerializeField] BattleAIProfile wildBattleAI;
    [SerializeField] BattleAIProfile trainerBattleAI;

    [Header("Backgrounds & Pokemon Spots")]
    [SerializeField] Image backgroundImages;
    [SerializeField] Image playerUnitSpotImage;
    [SerializeField] Image enemyUnitSpotImage;

    [Header("Battle Environments")]
    [Tooltip("Add one entry per BattleTrigger type. The system picks the first matching entry. Add a catch-all entry with no trigger as a fallback.")]
    [SerializeField] List<BattleEnvironmentVisuals> battleEnvironments = new List<BattleEnvironmentVisuals>();

    PlayerController player;
    BattleTrigger battleTrigger;
    List<BattleUnit> playerUnits;
    List<BattleUnit> enemyUnits;
    List<BattleAction> battleActions;
    BattlePowerMechanicController powerMechanicController;

    int unitCount = 1;
    int unitInSelectionIndex = 0;

    public int UnitCount => unitCount;
    public int ActivePlayerUnitsCount => playerUnits.Count(u => u.Pokemon != null && u.Pokemon.HP > 0);
    public int ActiveEnemyUnitsCount => enemyUnits.Count(u => u.Pokemon != null && u.Pokemon.HP > 0);
    public int EscapeAttempts { get; set; }
    public bool IsBattleOver {get; private set;}
    public bool IsTrainerBattle {get; private set;} = false;

    public Action<bool> OnBattleOver;
    public StateMachine<BattleSystem> StateMachine {get; private set;}
    public TrainerController Trainer { get; private set; }
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
    public List<BattleUnit> AllUnits => playerUnits.Concat(enemyUnits).ToList();
    public BattleUnit UnitInSelection => playerUnits[unitInSelectionIndex];
    public BattleField Field { get; private set;}
    public BattleRuleContext RuleContext { get; private set; }
    public BattleModeDefinition BattleMode { get; private set; }
    public PokemonVitalProfileDefinition ActiveVitalProfile => RuleContext != null ? RuleContext.VitalProfile : null;
    public bool SpendCoreStaminaOnBattleEntry => RuleContext == null || RuleContext.SpendCoreStaminaOnBattleEntry;
    public bool CapBattleHpByCoreHealth => RuleContext == null || RuleContext.CapBattleHpByCoreHealth;
    public BattleModeKind RequestedBattleModeKind => BattleMode != null ? BattleMode.Kind : BattleModeKind.ClassicFourMove;
    public BattleModeKind EffectiveBattleModeKind => BattleMode != null && BattleMode.ImplementedInCurrentBattleSystem ? BattleMode.Kind : BattleModeKind.ClassicFourMove;
    public BattlePowerMechanicController PowerMechanicController {
        get {
            if(powerMechanicController != null){
                return powerMechanicController;
            }

            powerMechanicController = GetComponent<BattlePowerMechanicController>();
            if(powerMechanicController == null){
                powerMechanicController = gameObject.AddComponent<BattlePowerMechanicController>();
            }

            return powerMechanicController;
        }
    }
    public AudioClip WildVicBattleMusic => wildBattleVictoryMusic;
    public AudioClip TrainerVicBattleMusic => trainerBattleVictoryMusic;
    public static BattleSystem i { get; private set; }

    // IBattleWeatherProvider — exposes the current field weather to Pokemon stat calculation
    // without Pokemon needing to reference BattleSystem directly.
    public WeatherCondition CurrentWeather => Field?.Weather;

    void Awake(){
        i = this;
        Pokemon.WeatherProvider = this;
    }

    private void OnDestroy(){
        if(Pokemon.WeatherProvider == (IBattleWeatherProvider)this){
            Pokemon.WeatherProvider = null;
        }
    }

    public void StartBattle(PokemonParty playerParty, Pokemon wildPokemon, BattleTrigger trigger = BattleTrigger.LongGrass, 
                            WeatherConditionID weather = WeatherConditionID.None){
        this.PlayerParty = playerParty;
        this.WildPokemon = wildPokemon;
        this.unitCount = 1;
        player = playerParty.GetComponent<PlayerController>();
        IsTrainerBattle = false;
        RuleContext = BattleRuleManager.i != null ? BattleRuleManager.i.CurrentContext : null;
        BattleMode = ResolveBattleMode();

        battleTrigger = trigger;
        AudioManager.i.PlayMusic(wildBattleMusic);

        StartCoroutine(SetupBattle(weather));
    }
    
    public void StartTrainerBattle(PokemonParty playerParty, PokemonParty trainerParty, BattleTrigger trigger = BattleTrigger.LongGrass, 
                                   WeatherConditionID weather = WeatherConditionID.None, int unitCount = 1){
        this.PlayerParty = playerParty;
        this.TrainerParty = trainerParty;
        this.unitCount = unitCount;
        player = playerParty.GetComponent<PlayerController>();
        Trainer = trainerParty.GetComponent<TrainerController>();
        IsTrainerBattle = true;
        RuleContext = BattleRuleManager.i != null ? BattleRuleManager.i.CurrentContext : null;
        BattleMode = ResolveBattleMode();

        battleTrigger = trigger;
        AudioManager.i.PlayMusic(trainerBattleMusic);

        StartCoroutine(SetupBattle(weather));
    }

    public IEnumerator SetupBattle(WeatherConditionID weather){
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
        Field = new BattleField();

        for(int i = 0; i < unitCount; i++){
            playerUnits[i].Clear();
            enemyUnits[i].Clear();
        }

        ApplyBattleEnvironment(battleTrigger);
        if(!IsTrainerBattle){
            var startingPokemon = PlayerParty.GetVitalReadyPokemon() ?? PlayerParty.GetHealthyPokemon();
            if(startingPokemon == null) {
                yield return dialogBox.TypeDialog("No Pokemon can battle right now.");
                BattleOver(false);
                yield break;
            }

            SetupBattleUnit(playerUnits[0], startingPokemon);
            SetupBattleUnit(enemyUnits[0], WildPokemon);

            playerUnits[0].Pokemon.OnBattleEntry(new List<Pokemon>(){enemyUnits[0].Pokemon});
            enemyUnits[0].Pokemon.OnBattleEntry(new List<Pokemon>(){playerUnits[0].Pokemon});

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
            for(int i = 0; i < enemyPokemons.Count; i++){
                enemyUnits[i].gameObject.SetActive(true);
                SetupBattleUnit(enemyUnits[i], enemyPokemons[i]);
            }
            
            var pokemonNames = String.Join(" and ", enemyPokemons.Select(p => p.Base.Name));
            yield return dialogBox.TypeDialog($"{Trainer.Name} send out {pokemonNames} for battle!");

            for(int i = 0; i < enemyPokemons.Count; i++){
                enemyUnits[i].Pokemon.OnBattleEntry(PlayerParty.Pokemons.Take(unitCount).ToList());
            }
            
            playerImage.gameObject.SetActive(false);
            var playerPokemons = PlayerParty.GetVitalReadyPokemons(unitCount);
            if(playerPokemons.Count == 0) {
                yield return dialogBox.TypeDialog("No Pokemon can battle right now.");
                BattleOver(false);
                yield break;
            }

            for(int i = 0; i < playerPokemons.Count; i++){
                playerUnits[i].gameObject.SetActive(true);
                SetupBattleUnit(playerUnits[i], playerPokemons[i]);
            }
            
            pokemonNames = String.Join(" and ", playerPokemons.Select(p => p.Base.Name));
            yield return dialogBox.TypeDialog($"Go {pokemonNames}! I choose you.");

            for(int i = 0; i < playerPokemons.Count; i++){
                playerUnits[i].Pokemon.OnBattleEntry(enemyPokemons);
            }
        }

        if(weather != WeatherConditionID.None){
            Field.SetWeather(weather);
            yield return dialogBox.TypeDialog(Field.Weather.StartMessage);
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

        if(battleActions.Count == ActivePlayerUnitsCount){
            foreach(var enemyUnit in enemyUnits){
                if(enemyUnit == null || enemyUnit.Pokemon == null || enemyUnit.Pokemon.HP <= 0){
                    continue;
                }

                var aiProfile = ResolveBattleAIProfile();
                var action = aiProfile != null
                    ? aiProfile.ChooseAction(enemyUnit, playerUnits, enemyUnits, TrainerParty, this)
                    : CreateFallbackEnemyAction(enemyUnit);

                if(action != null){
                    battleActions.Add(action);
                }
            }

            battleActions = battleActions.OrderByDescending(a => a.Priority).ThenByDescending(
                a => a.User.Pokemon.ModifySpeed( a.User.Pokemon.Speed, a.Target?.Pokemon, a.SelectedMove)).ToList();

            RunTurnState.i.Actions = battleActions;
            StateMachine.ChangeState(RunTurnState.i);
        } else {
            ++unitInSelectionIndex;
            StateMachine.ChangeState(ActionSelectionState.i);
        }
    }

    BattleAIProfile ResolveBattleAIProfile(){
        if(IsTrainerBattle && Trainer != null && Trainer.BattleAIProfile != null){
            return Trainer.BattleAIProfile;
        }

        return IsTrainerBattle ? trainerBattleAI : wildBattleAI;
    }

    BattleModeDefinition ResolveBattleMode(){
        if(RuleContext != null && RuleContext.BattleMode != null){
            return RuleContext.BattleMode;
        }

        var mode = player != null ? player.GetComponent<PlayerBattleModeSettings>()?.SelectedBattleMode : null;
        if(mode == null){
            return null;
        }

        if(!mode.CanAccess(player, out _)){
            return null;
        }

        if(!mode.CanRunWithCurrentBattleSystem(out var failureMessage, out var fallbackMessage)){
            GameDebug.Warning(failureMessage, GameDebugCategory.BattleRule, this, "BattleSystem");
            return null;
        }

        if(!string.IsNullOrWhiteSpace(fallbackMessage)){
            GameDebug.Warning(fallbackMessage, GameDebugCategory.BattleRule, this, "BattleSystem");
        }

        return mode;
    }

    void SetupBattleUnit(BattleUnit unit, Pokemon pokemon){
        if(unit == null){
            return;
        }

        unit.Setup(pokemon, ActiveVitalProfile, SpendCoreStaminaOnBattleEntry, CapBattleHpByCoreHealth);
    }

    BattleAction CreateFallbackEnemyAction(BattleUnit enemyUnit){
        var validTargets = playerUnits
            .Where(unit => unit != null && unit.Pokemon != null && unit.Pokemon.HP > 0)
            .ToList();
        var target = validTargets.Count > 0 ? validTargets[UnityEngine.Random.Range(0, validTargets.Count)] : null;
        var move = enemyUnit.Pokemon.GetRandomMove(ActiveVitalProfile) ?? new Move(GlobalSettings.i.BackUpMove);

        return new BattleAction{
            Type = BattleActionType.Move,
            SelectedMove = move,
            User = enemyUnit,
            Target = move.Base.Target == MoveTarget.Self ? enemyUnit : target
        };
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
        if(RuleContext != null && BattleRuleManager.i != null && BattleRuleManager.i.CurrentContext == RuleContext){
            BattleRuleManager.i.CompleteCurrent(won);
        }
        RuleContext = null;
    }

    public void HandleUpdate(){
        StateMachine.Execute();
    }

    public IEnumerator SwitchPokemon(Pokemon newPokemon, BattleUnit unitToSwitch){
        if (unitToSwitch.Pokemon.HP > 0){
            yield return dialogBox.TypeDialog($"Come back {unitToSwitch.Pokemon.Base.Name}! Thank you for your hard work!");
            unitToSwitch.Pokemon.OnSwitchOut();
            unitToSwitch.PlayFaintedAnimation();
            yield return new WaitForSeconds(2f);
        }

        SetupBattleUnit(unitToSwitch, newPokemon);
        yield return new WaitForSeconds(1f);

        if(unitToSwitch.IsPlayerUnit){
            newPokemon.OnBattleEntry(enemyUnits.Select(u => u.Pokemon).ToList());
        } else {
            newPokemon.OnBattleEntry(playerUnits.Select(u => u.Pokemon).ToList());
        }

        yield return CheckEntryHazards(unitToSwitch);

        dialogBox.SetMoveBars(newPokemon.Moves);

        yield return dialogBox.TypeDialog($"Your turn {newPokemon.Base.Name}!");
    }

    IEnumerator CheckEntryHazards(BattleUnit unit){
        var pokemon = unit.Pokemon;
        int spikes = unit.IsPlayerUnit ? Field.PlayerSpikes : Field.EnemySpikes;
        bool stealthRock = unit.IsPlayerUnit ? Field.PlayerStealthRock : Field.EnemyStealthRock;

        if(spikes > 0 && !pokemon.HasType(PokemonType.Flying) && pokemon.Ability?.Name != "Levitate"){
            float[] spikesDamage = { 0, 0.125f, 0.166f, 0.25f };
            int damage = Mathf.FloorToInt(pokemon.MaxHp * spikesDamage[Mathf.Clamp(spikes, 0, 3)]);
            pokemon.DecreaseHP(damage);
            yield return dialogBox.TypeDialog($"{pokemon.Base.Name} was hurt by spikes!");
            yield return unit.Hud.UpdateHPAsync();
        }

        if(stealthRock){
            float effectiveness = TypeChart.GetEffectiveness(PokemonType.Rock, pokemon.Base.Type1) * TypeChart.GetEffectiveness(PokemonType.Rock, pokemon.Base.Type2);
            int damage = Mathf.FloorToInt(pokemon.MaxHp * 0.125f * effectiveness);
            if(damage > 0){
                pokemon.DecreaseHP(damage);
                yield return dialogBox.TypeDialog("Pointed stones dug into " + pokemon.Base.Name + "!");
                yield return unit.Hud.UpdateHPAsync();
            }
        }
    }

    public IEnumerator SendNextTrainerPokemon(int faintedUnitIndex = 0){
        var activePokemons = EnemyUnits.Select(u => u.Pokemon).Where(p => p.HP > 0).ToList();

        var nextPokemon = TrainerParty.GetVitalReadyPokemon(doNotInclude: activePokemons);
        SetupBattleUnit(enemyUnits[faintedUnitIndex], nextPokemon);
        yield return dialogBox.TypeDialog($"{Trainer.Name} send out {nextPokemon.Base.Name}!");

        yield return CheckEntryHazards(enemyUnits[faintedUnitIndex]);
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
        float a = ( 3 * pokemon.MaxHp - 2 * pokemon.HP) * pokemon.Base.CatchRate * pokeball.CatchRateModifier * StatusConditionsDB.GetStatusBonus(pokemon.Status) / ( 3f * pokemon.MaxHp);
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

    public bool CanUseBattleItem(bool isPlayer, ItemBase item, out string failureMessage){
        if(RuleContext == null || !RuleContext.IsActive){
            failureMessage = null;
            return true;
        }

        return RuleContext.CanUseItem(isPlayer, item, out failureMessage);
    }

    public bool CanSwitchByRule(bool isPlayer, out string failureMessage){
        if(RuleContext == null || !RuleContext.IsActive){
            failureMessage = null;
            return true;
        }

        return RuleContext.CanSwitch(isPlayer, out failureMessage);
    }

    public bool CanRunByRule(out string failureMessage){
        if(RuleContext == null || !RuleContext.IsActive){
            failureMessage = null;
            return true;
        }

        return RuleContext.CanRunAway(out failureMessage);
    }

    public bool CanUsePowerMechanicByRule(bool isPlayer, PowerMechanicDefinition mechanic, out string failureMessage){
        if(RuleContext == null || !RuleContext.IsActive){
            failureMessage = null;
            return true;
        }

        return RuleContext.CanUsePowerMechanic(isPlayer, mechanic, out failureMessage);
    }

    public void RecordBattleItemUse(bool isPlayer){
        RuleContext?.RecordItemUse(isPlayer);
    }

    public void RecordBattleSwitch(bool isPlayer){
        RuleContext?.RecordSwitch(isPlayer);
    }

    public void RecordPowerMechanicUse(bool isPlayer, PowerMechanicDefinition mechanic){
        RuleContext?.RecordPowerMechanicUse(isPlayer, mechanic);
    }

    public bool TryUsePowerMechanic(BattleAction action, out string failureMessage){
        if(action == null || action.SelectedPowerMechanic == null){
            failureMessage = null;
            return true;
        }

        return PowerMechanicController.TryUse(
            action.SelectedPowerMechanic,
            action.User,
            action.Target,
            action.SelectedMove,
            action.User != null && action.User.IsPlayerUnit,
            "BattleAction",
            out failureMessage);
    }

    public bool RecordRuleTurnCompleted(out string ruleMessage, out BattleRuleTurnLimitOutcome outcome){
        outcome = BattleRuleTurnLimitOutcome.ContinueBattle;
        if(RuleContext == null || !RuleContext.IsActive){
            ruleMessage = null;
            return false;
        }

        bool reachedLimit = RuleContext.RecordTurnCompleted(out ruleMessage);
        if(reachedLimit && RuleContext.RuleSet != null){
            outcome = RuleContext.RuleSet.TurnLimitOutcome;
        }

        return reachedLimit;
    }

    /// <summary>
    /// Looks up the BattleEnvironmentVisuals entry matching the given trigger and
    /// applies its sprites to the background and unit spot images.
    /// Data-driven — add new environments in the Inspector list without touching code.
    /// </summary>
    void ApplyBattleEnvironment(BattleTrigger trigger){
        BattleEnvironmentVisuals visuals = null;

        foreach (var env in battleEnvironments){
            if (env.trigger == trigger){
                visuals = env;
                break;
            }
        }

        // Fallback: use the first entry if no exact match found.
        if (visuals == null && battleEnvironments.Count > 0){
            visuals = battleEnvironments[0];
            Debug.LogWarning($"[BattleSystem] No environment visuals found for trigger '{trigger}'. Using fallback entry.");
        }

        if (visuals == null){
            Debug.LogWarning("[BattleSystem] No battle environment visuals configured.");
            return;
        }

        if (backgroundImages != null)     backgroundImages.sprite    = visuals.background;
        if (playerUnitSpotImage != null)  playerUnitSpotImage.sprite = visuals.playerSpot;
        if (enemyUnitSpotImage != null)   enemyUnitSpotImage.sprite  = visuals.enemySpot;
    }
}
