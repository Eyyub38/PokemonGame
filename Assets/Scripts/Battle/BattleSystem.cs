using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;
using GDEUtills.StateMachine;
using System.Collections.Generic;

public enum BattleStates { Start, ActionSelection, MoveSelection, RunningTurn, PartyScreen, Bag,AboutToUse, MoveToForget, BattleOver, Busy}
public enum BattleAction { Move, SwitchPokemon, UseItem, Run}
public enum BattleTrigger {LongGrass, Water}

public class BattleSystem : MonoBehaviour{
    [Header("Battle Units")]
    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;

    [Header("UI")]
    [SerializeField] BattleDialogBox dialogBox;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] InventoryUI inventoryUI;
    [SerializeField] MoveForgetSelectionUI moveForgetSelectionUI; 
    
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

    int currentAction;
    int currentMove;
    bool aboutToUseChoice = true;

    public BattleStates state;
    PlayerController player;
    TrainerController trainer;
    MoveBase moveToLearn;
    BattleTrigger battleTrigger;

    public Action<bool> OnBattleOver;

    public int SelectedMove {get; set;}
    public int EscapeAttempts {get; set;}
    public bool IsBattleOver {get; private set;}
    public bool IsTrainerBattle {get; private set;} = false;
    public StateMachine<BattleSystem> StateMachine {get; private set;}
    public PokemonParty PlayerParty {get; private set;}
    public PokemonParty TrainerParty {get; private set;}
    public Pokemon WildPokemon {get; private set;}
    public Pokemon SelectedPokemon {get; set;}
    public PartyScreen PartyScreen => partyScreen;
    public BattleDialogBox DialogBox => dialogBox;
    public BattleAction SelectedAction {get; set;}
    public BattleUnit PlayerUnit => playerUnit;
    public BattleUnit EnemyUnit => enemyUnit;
    public AudioClip WildVicBattleMusic => wildBattleVictoryMusic;
    public AudioClip TrainerVicBattleMusic => trainerBattleVictoryMusic;

    public void StartBattle(PokemonParty playerParty, Pokemon wildPokemon, BattleTrigger trigger = BattleTrigger.LongGrass){
        this.PlayerParty = playerParty;
        this.WildPokemon = wildPokemon;
        player = playerParty.GetComponent<PlayerController>();
        IsTrainerBattle = false;

        battleTrigger = trigger;
        AudioManager.i.PlayMusic(wildBattleMusic);

        StartCoroutine(SetupBattle());
    }
    
    public void StartTrainerBattle(PokemonParty playerParty, PokemonParty trainerParty, BattleTrigger trigger = BattleTrigger.LongGrass){
        this.PlayerParty = playerParty;
        this.TrainerParty = trainerParty;
        player = playerParty.GetComponent<PlayerController>();
        trainer = trainerParty.GetComponent<TrainerController>();
        IsTrainerBattle = true;

        battleTrigger = trigger;
        AudioManager.i.PlayMusic(trainerBattleMusic);

        StartCoroutine(SetupBattle());
    }

    public IEnumerator SetupBattle(){
        StateMachine = new StateMachine<BattleSystem>(this);

        playerUnit.Clear();
        enemyUnit.Clear();
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
            playerUnit.Setup(PlayerParty.GetHealthyPokemon());
            enemyUnit.Setup(WildPokemon);

            dialogBox.SetMoveBars(playerUnit.Pokemon.Moves);

            yield return dialogBox.TypeDialog($"A wild {enemyUnit.Pokemon.Base.Name} appeared.");
        } else {
            playerUnit.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(false);

            playerImage.gameObject.SetActive(true);
            trainerImage.gameObject.SetActive(true);

            playerImage.sprite = player.BattleImage;
            trainerImage.sprite = trainer.BattleImage;

            yield return dialogBox.TypeDialog($"The battle between you and {trainer.Name} is started.");

            trainerImage.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(true);
            var enemyPokemon = TrainerParty.GetHealthyPokemon();
            enemyUnit.Setup(enemyPokemon);
            yield return dialogBox.TypeDialog($"{trainer.Name} send out {enemyPokemon.Base.Name} for battle!");

            playerImage.gameObject.SetActive(false);
            playerUnit.gameObject.SetActive(true);
            var playerPokemon = PlayerParty.GetHealthyPokemon();
            playerUnit.Setup(playerPokemon);
            yield return dialogBox.TypeDialog($"Go {playerPokemon.Base.Name}! I choose you.");
            dialogBox.SetMoveBars(playerPokemon.Moves);
        }

        IsBattleOver = false;
        EscapeAttempts = 0;
        partyScreen.Init();
        StateMachine.ChangeState(ActionSelectionState.i);
    
    }

    IEnumerator AboutToUse(Pokemon newPokemon){
        state = BattleStates.Busy;
        yield return dialogBox.TypeDialog($"{trainer.Name} is about to use {newPokemon.Base.Name}. Do you want to change your Pokemon?");

        state = BattleStates.AboutToUse;
        dialogBox.EnableChoiceBox(true);
    }

    IEnumerator ChooseMoveToForget(Pokemon pokemon, MoveBase newMove){
        state = BattleStates.Busy;
        yield return dialogBox.TypeDialog($"Choose a move you want {pokemon.Base.Name} to forget!");
        moveForgetSelectionUI.gameObject.SetActive(true);

        moveForgetSelectionUI.SetMoveSelectionBars(pokemon.Moves, newMove);
        moveForgetSelectionUI.SetMoveDetails(pokemon.Moves[0].Base, newMove);
        moveToLearn = newMove;

        state = BattleStates.MoveToForget;
    }

    public void OpenPartyScreen(){
        Debug.Log("OpenPartyScreen called");
        if(partyScreen == null){
            Debug.LogError("partyScreen is null in BattleSystem!");
            return;
        }
        
        state = BattleStates.PartyScreen;
        partyScreen.gameObject.SetActive(true);
        partyScreen.Init();
        partyScreen.OnSelected += OnPartyMemberSelected;
        partyScreen.OnBack += OnPartyScreenBack;
        Debug.Log("PartyScreen opened successfully");
    }

    void OnPartyMemberSelected(int selectedIndex){
        Debug.Log($"OnPartyMemberSelected called with index: {selectedIndex}");
        var selectedMember = partyScreen.SelectedMember;
        if(selectedMember.HP <= 0){
            partyScreen.SetMessageText($"{selectedMember.Base.Name} is fainted. You cannot send out to battle.");
            return;
        }
        if(selectedMember == playerUnit.Pokemon){
            partyScreen.SetMessageText($"{selectedMember.Base.Name} is already in battle.");
            return;
        }

        SelectedPokemon = selectedMember;
        partyScreen.gameObject.SetActive(false);
        partyScreen.OnSelected -= OnPartyMemberSelected;
        partyScreen.OnBack -= OnPartyScreenBack;
        state = BattleStates.ActionSelection;
        Debug.Log($"Pokemon selected: {SelectedPokemon.Base.Name}");
    }

    void OnPartyScreenBack(){
        Debug.Log("OnPartyScreenBack called");
        if(playerUnit.Pokemon.HP <=0){
            partyScreen.SetMessageText("Your Pokemon is fainted! You need to choose new Pokemon");
            return;
        }
        partyScreen.gameObject.SetActive(false);
        partyScreen.OnSelected -= OnPartyMemberSelected;
        partyScreen.OnBack -= OnPartyScreenBack;
        state = BattleStates.ActionSelection;
        Debug.Log("PartyScreen closed");
    }

    public void BattleOver(bool won){
        IsBattleOver = true;
        PlayerParty.Pokemons.ForEach(p => p.OnBattleOver());
        playerUnit.Hud.ClearData();
        enemyUnit.Hud.ClearData();
        OnBattleOver(won);
    }

    public void HandleUpdate(){
        StateMachine.Execute();

        if(state == BattleStates.PartyScreen){
            HandlePartySelection();
        } else if(state == BattleStates.Bag){
            Action onBack = () => {
                inventoryUI.gameObject.SetActive(false);
                state = BattleStates.ActionSelection;
            };
            Action<ItemBase> onItemUsed = (ItemBase usedItem) => {
                StartCoroutine(OnItemUsed(usedItem));
            };
            //inventoryUI.HandleUpdate(onBack, onItemUsed);

        } else if(state == BattleStates.AboutToUse){
            HandleAboutToUse();
        } else if(state == BattleStates.MoveToForget){
            Action<int> onMoveSelected = (moveIndex) => {
                if(moveIndex == PokemonBase.MaxNumberOfMoves){
                    moveForgetSelectionUI.gameObject.SetActive(false);
                    StartCoroutine(dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} didn't learn {moveToLearn.Name}"));
                } else {
                    var selectedMove = playerUnit.Pokemon.Moves[ moveIndex ].Base;
                    moveForgetSelectionUI.gameObject.SetActive(false);
                    StartCoroutine(dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} forgot {selectedMove.Name} and learned {moveToLearn.Name}"));

                    playerUnit.Pokemon.Moves[ moveIndex ] = new Move(moveToLearn);
                }
                moveToLearn = null;
                state = BattleStates.RunningTurn;
            };

            //moveSelectionUI.HandleMoveSelection(playerUnit.Pokemon, onMoveSelected);
        }
    }

    void HandleAboutToUse(){
        if(Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)){
            aboutToUseChoice = !aboutToUseChoice;
        }

        dialogBox.UpdateChoiceSelection(aboutToUseChoice);
        if(Input.GetKeyDown(KeyCode.Return)){
            dialogBox.EnableChoiceBox(false);
            if(aboutToUseChoice == true){
                OpenPartyScreen();
            } else {
                StartCoroutine(SendNextTrainerPokemon());
            }
        } else if(Input.GetKeyDown(KeyCode.Escape)){
            dialogBox.EnableChoiceBox(false);
            StartCoroutine(SendNextTrainerPokemon());
        }
    }

    void HandlePartySelection(){
        partyScreen.HandleUpdate();
    }

    IEnumerator OnItemUsed(ItemBase usedItem){
        state = BattleStates.Busy;
        inventoryUI.gameObject.SetActive(false);
        if(usedItem is PokeballItem){
            yield return ThrowPokeball((PokeballItem) usedItem);    
        }
        //StartCoroutine(RunTurns(BattleAction.UseItem));
    }

    public IEnumerator SwitchPokemon(Pokemon newPokemon){
        if (playerUnit.Pokemon.HP > 0){
            yield return dialogBox.TypeDialog($"Come back {playerUnit.Pokemon.Base.Name}!. Thank you for your hard work!");
            playerUnit.PlayFaintedAnimation();
            yield return new WaitForSeconds(2f);
        }

        playerUnit.Setup(newPokemon);
        dialogBox.SetMoveBars(newPokemon.Moves);

        yield return dialogBox.TypeDialog($"Your turn {newPokemon.Base.Name}!");
    }

    IEnumerator SendNextTrainerPokemon(){
        state = BattleStates.Busy;
        var nextPokemon = TrainerParty.GetHealthyPokemon();
        enemyUnit.Setup(nextPokemon);
        yield return dialogBox.TypeDialog($"{trainer.Name} send out {nextPokemon.Base.Name}!");
        state = BattleStates.RunningTurn;
    }
    


    IEnumerator ThrowPokeball(PokeballItem pokeball){
        state = BattleStates.Busy;

        if (IsTrainerBattle){
            yield return dialogBox.TypeDialog("You are trying to steal someone's pokemon... You cannot do it!");
            state = BattleStates.RunningTurn;
            yield break;
        }

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
            state = BattleStates.RunningTurn;
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
}
