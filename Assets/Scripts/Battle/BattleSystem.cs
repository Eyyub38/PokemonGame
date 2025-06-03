using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum BattleState { Start, ActionSelection, MoveSelection, RunningTurn, PartyScreen, AboutToUse, MoveToForget, BattleOver, Busy}
public enum BattleAction { Move, SwitchPokemon, UseItem, Run}

public class BattleSystem : MonoBehaviour{
    [Header("Battle Units")]
    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;

    [Header("UI")]
    [SerializeField] BattleDialogBox dialogBox;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] MoveSelectionUI moveSelectionUI; 
    
    [Header("Character Images")]
    [SerializeField] Image playerImage;
    [SerializeField] Image trainerImage;

    [Header("Pokeball")]
    [SerializeField] GameObject pokeballSprite;

    BattleState state;
    int currentAction;
    int currentMove;
    int escapeAttempts;
    bool aboutToUseChoice = true;

    PokemonParty playerParty;
    PokemonParty trainerParty;
    Pokemon wildPokemon;
    PlayerController player;
    TrainerController trainer;
    MoveBase moveToLearn;

    bool isTrainerBattle = false;

    public Action<bool> OnBattleOver;

    public void StartBattle(PokemonParty playerParty, Pokemon wildPokemon){
        this.playerParty = playerParty;
        this.wildPokemon = wildPokemon;

        isTrainerBattle = false;
        player = playerParty.GetComponent<PlayerController>();
        StartCoroutine(SetupBattle());
    }
    
    public void StartTrainerBattle(PokemonParty playerParty, PokemonParty trainerParty){
        this.playerParty = playerParty;
        this.trainerParty = trainerParty;

        isTrainerBattle = true;
        player = playerParty.GetComponent<PlayerController>();
        trainer = trainerParty.GetComponent<TrainerController>();

        StartCoroutine(SetupBattle());
    }

    public IEnumerator SetupBattle(){
        playerUnit.Clear();
        enemyUnit.Clear();
        if(!isTrainerBattle){
            playerUnit.Setup(playerParty.GetHealthyPokemon());
            enemyUnit.Setup(wildPokemon);

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
            var enemyPokemon = trainerParty.GetHealthyPokemon();
            enemyUnit.Setup(enemyPokemon);
            yield return dialogBox.TypeDialog($"{trainer.Name} send out {enemyPokemon.Base.Name} for battle!");

            playerImage.gameObject.SetActive(false);
            playerUnit.gameObject.SetActive(true);
            var playerPokemon = playerParty.GetHealthyPokemon();
            playerUnit.Setup(playerPokemon);
            yield return dialogBox.TypeDialog($"Go {playerPokemon.Base.Name}! I choose you.");
            dialogBox.SetMoveBars(playerPokemon.Moves);
        }

        escapeAttempts = 0;
        partyScreen.Init();
        ActionSelection();
    }

    void ActionSelection(){
        state = BattleState.ActionSelection;
        dialogBox.SetDialog("Choose an Action");
        dialogBox.EnableActionSelector(true);
    }

    void MoveSelection(){
        state = BattleState.MoveSelection;
        dialogBox.EnableActionSelector(false);
        dialogBox.EnableDialogText(false);
        dialogBox.EnableMoveSelector(true);
        dialogBox.SetMoveBars(playerUnit.Pokemon.Moves);
    }

    IEnumerator AboutToUse(Pokemon newPokemon){
        state = BattleState.Busy;
        yield return dialogBox.TypeDialog($"{trainer.Name} is about to use {newPokemon.Base.Name}. Do you want to change your Pokemon?");

        state = BattleState.AboutToUse;
        dialogBox.EnableChoiceBox(true);
    }

    IEnumerator ChooseMoveToForget(Pokemon pokemon, MoveBase newMove){
        state = BattleState.Busy;
        yield return dialogBox.TypeDialog($"Choose a move you want {pokemon.Base.Name} to forget!");
        moveSelectionUI.gameObject.SetActive(true);

        moveSelectionUI.SetMoveSelectionBars(pokemon.Moves, newMove);
        moveSelectionUI.SetMoveDetails(pokemon.Moves[0].Base, newMove);
        moveToLearn = newMove;

        state = BattleState.MoveToForget;
    }

    void OpenPartyScreen(){
        partyScreen.CallFrom = state;
        state = BattleState.PartyScreen;
        partyScreen.SetPartyData(playerParty.Pokemons);
        partyScreen.gameObject.SetActive(true);
    }

    IEnumerator RunTurns(BattleAction playerAction){
        state = BattleState.RunningTurn;

        if(playerAction == BattleAction.Move){
            playerUnit.Pokemon.CurrentMove = playerUnit.Pokemon.Moves[currentMove];
            enemyUnit.Pokemon.CurrentMove = enemyUnit.Pokemon.GetRandomMove();

            int playerMovePriority = playerUnit.Pokemon.CurrentMove.Base.Priority;
            int enemyMovePriority = enemyUnit.Pokemon.CurrentMove.Base.Priority;

            bool playerGoesFirst = true;
            if(playerMovePriority > enemyMovePriority){ 
                playerGoesFirst = false; 
            } else if(playerMovePriority == enemyMovePriority){
                playerGoesFirst = playerUnit.Pokemon.Speed >= enemyUnit.Pokemon.Speed;
            }

            var firstUnit = (playerGoesFirst) ? playerUnit : enemyUnit;
            var secondUnit = (playerGoesFirst) ? enemyUnit : playerUnit;

            var secondPokemon = secondUnit.Pokemon;

            yield return RunMove(firstUnit, secondUnit, firstUnit.Pokemon.CurrentMove);
            yield return RunAfterTurn(firstUnit);
            if(state == BattleState.BattleOver) yield break;

            if(secondPokemon.HP > 0){
                yield return RunMove(secondUnit, firstUnit, secondUnit.Pokemon.CurrentMove);
                yield return RunAfterTurn(secondUnit);
                if(state == BattleState.BattleOver) yield break;
            }
        } else {
            if(playerAction == BattleAction.SwitchPokemon){
                var selectedPokemon = partyScreen.SelectedMember;
                state = BattleState.Busy;
                yield return SwitchPokemon(selectedPokemon);
            } else if(playerAction == BattleAction.UseItem){
                dialogBox.EnableActionSelector(false);
                yield return ThrowPokeball();
            } else if(playerAction == BattleAction.Run){
                dialogBox.EnableActionSelector(false);
                yield return TryToEscape();
            }

                var enemyMove = enemyUnit.Pokemon.GetRandomMove();
            yield return RunMove(enemyUnit, playerUnit, enemyMove);
            yield return RunAfterTurn(enemyUnit);
            if(state == BattleState.BattleOver) yield break;
        }
        if(state !=  BattleState.BattleOver){
            ActionSelection();
        }
    }

    IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move){
        bool canRunMove = sourceUnit.Pokemon.OnBeforeTurn();
        if(canRunMove == false){
            yield return ShowStatusChanges(sourceUnit.Pokemon);
            yield return sourceUnit.Hud.UpdateHP();
            yield break;
        }
        yield return ShowStatusChanges(sourceUnit.Pokemon);

        move.PP--;
        yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name} used {move.Base.Name}.");

        if(CheckIfMoveHits(move, sourceUnit.Pokemon, targetUnit.Pokemon)){
            sourceUnit.PlayAttackAnimation();
            yield return new WaitForSeconds(1f);

            targetUnit.PlayHitAnimation();

            if(move.Base.Category == MoveCategory.Status){
                yield return RunMoveEffects(move.Base.Effects, sourceUnit.Pokemon, targetUnit.Pokemon, move.Base.Target);
            } else {
                var damageDetails = targetUnit.Pokemon.TakeDamage(move, sourceUnit.Pokemon);
                yield return targetUnit.Hud.UpdateHP();
                yield return ShowDamageDetails(damageDetails);
            }

            if(move.Base.Secondaries != null && move.Base.Secondaries.Count > 0 && targetUnit.Pokemon.HP > 0){
                foreach(var secondary in move.Base.Secondaries){
                    var rnd = UnityEngine.Random.Range(1, 101);
                    if(rnd <= secondary.Chance){
                    yield return RunMoveEffects(secondary, sourceUnit.Pokemon, targetUnit.Pokemon, secondary.Target);
                    }
                }
            }

            if(targetUnit.Pokemon.HP <= 0){
                yield return HandlePokemonFainted(targetUnit);
            }
        } else {
            yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name}'s attack missed!");
        }
    }

    IEnumerator RunAfterTurn(BattleUnit sourceUnit){
        if(state == BattleState.BattleOver) yield break;
        yield return new WaitUntil(() => state == BattleState.RunningTurn);

        sourceUnit.Pokemon.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit.Pokemon);
        yield return sourceUnit.Hud.UpdateHP();
        
        if(sourceUnit.Pokemon.HP <= 0){
            yield return HandlePokemonFainted(sourceUnit);
            yield return new WaitUntil(() => state == BattleState.RunningTurn);
        }
    }

    IEnumerator RunMoveEffects(MoveEffects effects, Pokemon source, Pokemon target, MoveTarget moveTarget){
        if(effects.Boosts != null){
            if(moveTarget == MoveTarget.Self){
                source.ApplyBoosts(effects.Boosts);
            } else {
                target.ApplyBoosts(effects.Boosts);
            }

            yield return ShowStatusChanges(source);
            yield return ShowStatusChanges(target);
        }

        if(effects.Status != ConditionID.non){
            target.SetStatus(effects.Status);
        }
        if(effects.VolatileStatus != ConditionID.non){
            target.SetVolatileStatus(effects.VolatileStatus);
        }
    }

    void CheckForBattleOver(BattleUnit faintedUnit){
        if(faintedUnit.IsPlayerUnit){
            var nextPokemon = playerParty.GetHealthyPokemon();
            if(nextPokemon != null){
                OpenPartyScreen();
            } else {
                BattleOver(false);
            }
        } else {
            if(!isTrainerBattle){
                BattleOver(true);
            } else {
                var nextPokemon = trainerParty.GetHealthyPokemon();
                if(nextPokemon != null){
                    StartCoroutine(AboutToUse(nextPokemon));
                } else {
                    BattleOver(true);
                }
            }
        }
    }

    bool CheckIfMoveHits(Move move, Pokemon source, Pokemon target){
        if(move.Base.AlwaysHits){
            return true;
        }

        float moveAccuracy = move.Base.Accuracy;

        int accuracy = source.StatBoosts[Stat.Accuracy];
        int evasion = target.StatBoosts[Stat.Evasion];

        var boostValues = new float[]{ 1f, 4f/3f, 5f/3f, 2f, 7f/3f, 8f/3f, 3f};
        if(accuracy > 0){
            moveAccuracy *= boostValues[accuracy];
        } else {
            moveAccuracy /= boostValues[-accuracy];
        }
        if(evasion > 0){
            moveAccuracy /= boostValues[evasion];
        } else {
            moveAccuracy *= boostValues[-evasion];
        }
        return UnityEngine.Random.Range(1, 101) <= moveAccuracy;
    }

    void BattleOver(bool won){
        state = BattleState.BattleOver;
        playerParty.Pokemons.ForEach(p => p.OnBattleOver());
        OnBattleOver(won);
    }

    IEnumerator ShowDamageDetails(DamageDetails damageDetails){
        if(damageDetails.Critical > 1f){
            yield return dialogBox.TypeDialog("A critical hit!");
        }
        if(damageDetails.TypeEffectiveness > 1f){
            yield return dialogBox.TypeDialog($"It's super effective!");
        } else if(damageDetails.TypeEffectiveness < 1f){
            yield return dialogBox.TypeDialog($"It's not very effective...");
        } else if(damageDetails.TypeEffectiveness == 0f){
            yield return dialogBox.TypeDialog($"It doesn't affect {enemyUnit.Pokemon.Base.Name}...");
        }
    }

    public void HandleUpdate(){
        if(state == BattleState.ActionSelection){
            HandleActionSelection();
        } else if(state == BattleState.MoveSelection){
            HandleMoveSelection();
        } else if(state == BattleState.PartyScreen){
            HandlePartySelection();
        } else if(state == BattleState.AboutToUse){
            HandleAboutToUse();
        } else if(state == BattleState.MoveToForget){
            Action<int> onMoveSelected = (moveIndex) => {
                if(moveIndex == PokemonBase.MaxNumberOfMoves){
                    moveSelectionUI.gameObject.SetActive(false);
                    StartCoroutine(dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} didn't learn{moveToLearn.Name}"));
                } else {
                    var selectedMove = playerUnit.Pokemon.Moves[ moveIndex ].Base;
                    moveSelectionUI.gameObject.SetActive(false);
                    StartCoroutine(dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} forgot {selectedMove.Name} and learned {moveToLearn.Name}"));

                    playerUnit.Pokemon.Moves[ moveIndex ] = new Move(moveToLearn);
                }
                moveToLearn = null;
                state = BattleState.RunningTurn;
            };

            moveSelectionUI.HandleMoveSelection(playerUnit, onMoveSelected);
        }
    }
    
    void HandleActionSelection(){
        if(Input.GetKeyDown(KeyCode.RightArrow)){
            ++currentAction;
        } else if(Input.GetKeyDown(KeyCode.LeftArrow)){
            --currentAction;
        } else if(Input.GetKeyDown(KeyCode.DownArrow)){
            currentAction += 2;
        } else if(Input.GetKeyDown(KeyCode.UpArrow)){
            currentAction -= 2;
        }

        currentAction = Mathf.Clamp(currentAction, 0, 3);

        dialogBox.UpdateActionSelection(currentAction);

        if(Input.GetKeyDown(KeyCode.Return)){
            if(currentAction == 0){
                //Fight
                MoveSelection();
            } else if(currentAction == 1){
                //Pokemon
                OpenPartyScreen();
            } else if(currentAction == 2){
                //Bag
                StartCoroutine(RunTurns(BattleAction.UseItem));
            } else if(currentAction == 3){
                //Run
                StartCoroutine(RunTurns(BattleAction.Run));
            }
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

    void HandleMoveSelection(){
        if(Input.GetKeyDown(KeyCode.DownArrow)){
            currentMove++;
        } else if(Input.GetKeyDown(KeyCode.UpArrow)){
            currentMove--;
        } else if(Input.GetKeyDown(KeyCode.RightArrow)){
            currentMove +=2;
        } else if(Input.GetKeyDown(KeyCode.LeftArrow)){
            currentMove -=2;
        }

        currentMove = Mathf.Clamp(currentMove, 0, playerUnit.Pokemon.Moves.Count - 1);

        dialogBox.UpdateMoveSelection(currentMove);

        if(Input.GetKeyDown(KeyCode.Return)){
            var move = playerUnit.Pokemon.Moves[currentMove];
            if(move.PP <= 0){ return; }
            
            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);
            StartCoroutine(RunTurns(BattleAction.Move));
        } else if(Input.GetKeyDown(KeyCode.Escape)){
            
            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);
            ActionSelection();
        }
    }

    void HandlePartySelection(){
        Action onSelected = () => {
            var selectedMember = partyScreen.SelectedMember;
            if(selectedMember.HP <= 0){
                partyScreen.SetMessageText($"{selectedMember.Base.Name} is fainted. You cannot send out to battle.");
                return;
            }
            if(selectedMember == playerUnit.Pokemon){
                partyScreen.SetMessageText($"{selectedMember.Base.Name} is already in battle.");
                return;
            }

            partyScreen.gameObject.SetActive(false);
            if(partyScreen.CallFrom == BattleState.ActionSelection){
                StartCoroutine(RunTurns(BattleAction.SwitchPokemon));
            } else {
                state = BattleState.Busy;
                bool isTrainerAboutToUse = partyScreen.CallFrom == BattleState.AboutToUse;
                StartCoroutine(SwitchPokemon(selectedMember,isTrainerAboutToUse));
            }

            partyScreen.CallFrom = null;
        };
        Action onBack = () => {
            if(playerUnit.Pokemon.HP <=0){
                partyScreen.SetMessageText("Your Pokemon is fainted! You need to choose new Pokemon");
                return;
            }
            partyScreen.gameObject.SetActive(false);
            if(partyScreen.CallFrom == BattleState.AboutToUse){
                StartCoroutine(SendNextTrainerPokemon());
            } else {
                ActionSelection();
            }
            partyScreen.CallFrom = null;
        };

        partyScreen.HandleUpdate(onSelected, onBack);
    }

    IEnumerator HandlePokemonFainted(BattleUnit faintedUnit){
        yield return dialogBox.TypeDialog($"{faintedUnit.Pokemon.Base.Name} fainted");
        faintedUnit.PlayFaintedAnimation();
        yield return new WaitForSeconds(2f);

        if(!faintedUnit.IsPlayerUnit){
            int expYield = faintedUnit.Pokemon.Base.XpYield;
            int enemyLevel = faintedUnit.Pokemon.Level;
            float trainerBonus = (isTrainerBattle)? 1.5f : 1f;

            int expGain = Mathf.FloorToInt( expYield * enemyLevel * trainerBonus)  / 7;
            playerUnit.Pokemon.Exp += expGain;

            yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} gained {expGain} XP from this battle.");
            yield return playerUnit.Hud.SetExpSmooth();

            while(playerUnit.Pokemon.CheckForLevelUp()) {
                playerUnit.Hud.SetLevel();
                yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} level up to Lvl {playerUnit.Pokemon.Level}!");

                var newMove = playerUnit.Pokemon.GetLearnableMoveAtCurrLevel();
                if(newMove != null) {
                    if(playerUnit.Pokemon.Moves.Count < PokemonBase.MaxNumberOfMoves){
                        playerUnit.Pokemon.LearnMove(newMove);
                        yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} learned {newMove.Base.Name}");
                        dialogBox.SetMoveBars(playerUnit.Pokemon.Moves);
                    } else {
                        yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} trying to learn {newMove.Base.Name}...");
                        yield return dialogBox.TypeDialog($"But its is already knew {PokemonBase.MaxNumberOfMoves} moves.");
                        yield return ChooseMoveToForget(playerUnit.Pokemon, newMove.Base);
                        yield return new WaitUntil(() => state != BattleState.MoveToForget);
                        yield return new WaitForSeconds(2f);
                    }
                }

                yield return playerUnit.Hud.SetExpSmooth(true);
            }
            yield return new WaitForSeconds(1f);
        }

        CheckForBattleOver(faintedUnit);
    }
    
    IEnumerator SwitchPokemon(Pokemon newPokemon, bool isTrainerAboutToUse = false){
        if (playerUnit.Pokemon.HP > 0){
            yield return dialogBox.TypeDialog($"Come back {playerUnit.Pokemon.Base.Name}!. Thank you for your hard work!");
            playerUnit.PlayFaintedAnimation();
            yield return new WaitForSeconds(2f);
        }

        playerUnit.Setup(newPokemon);
        dialogBox.SetMoveBars(newPokemon.Moves);

        yield return dialogBox.TypeDialog($"Your turn {newPokemon.Base.Name}!");

        if(isTrainerAboutToUse){
            StartCoroutine(SendNextTrainerPokemon());
        } else {
            state = BattleState.RunningTurn;
        }
    }

    IEnumerator SendNextTrainerPokemon(){
        state = BattleState.Busy;
        var nextPokemon = trainerParty.GetHealthyPokemon();
        enemyUnit.Setup(nextPokemon);
        yield return dialogBox.TypeDialog($"{trainer.Name} send out {nextPokemon.Base.Name}!");
        state = BattleState.RunningTurn;
    }
    
    IEnumerator ShowStatusChanges(Pokemon pokemon){
        while (pokemon.StatusChanges.Count > 0){
            var message = pokemon.StatusChanges.Dequeue();
            yield return dialogBox.TypeDialog(message);
        }
    }

    IEnumerator ThrowPokeball() {
        state = BattleState.Busy;

        if(isTrainerBattle){
            yield return dialogBox.TypeDialog("You are trying to steal someone's pokemon... You cannot do it!");
            state = BattleState.RunningTurn;
            yield break;
        }

        yield return dialogBox.TypeDialog($"{player.Name} used Pokeball");

         var pokeballObj = Instantiate(pokeballSprite, playerUnit.transform.position - new Vector3(2, 0), Quaternion.identity);
        var pokeball = pokeballObj.GetComponent<SpriteRenderer>();
        var animator = pokeballObj.GetComponent<Animator>();

        if (animator != null)
            animator.Play("Throw");

        yield return pokeball.transform
            .DOJump(enemyUnit.transform.position + new Vector3(0, 2), 2f, 1, 1.4f)
            .WaitForCompletion();

        yield return enemyUnit.PlayCaptureAnimation();

        yield return pokeball.transform
            .DOMoveY(enemyUnit.transform.position.y - 1.8f, 0.5f)
            .WaitForCompletion();

        yield return new WaitForSeconds(2f);
        if(animator!=null)
            animator.Play("Idle", 0, 0.25f);

        int shakeCount = TryCatchPokemon( enemyUnit.Pokemon);
        for (int i = 0; i < Mathf.Min( shakeCount, 3); i++) {
            if (animator != null)
                animator.Play("Shake", 0, 0f);

            yield return new WaitForSeconds(2f);
        }

        if(shakeCount == 4){
            yield return dialogBox.TypeDialog($" Congrats!! {enemyUnit.Pokemon.Base.Name} was caught.");
            animator.Play("Catch");
            yield return pokeball.DOFade(0, 1.5f).WaitForCompletion();

            playerParty.AddPokemon(enemyUnit.Pokemon);
            yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} has been added to your party.");


            Destroy(pokeball);
            BattleOver(true);
        } else {
            yield return new WaitForSeconds(1f);
            pokeball.DOFade(0, 0.2f);
            yield return enemyUnit.PlayBreakAnimation();

            if(shakeCount < 2){
                yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} broke free.");
            } else {
                yield return dialogBox.TypeDialog($"Almost caught it!");
            }

            Destroy(pokeball);
            state = BattleState.RunningTurn;
        }
    }
    
    int TryCatchPokemon(Pokemon pokemon){
        float a = ( 3 * pokemon.MaxHp - 2 * pokemon.HP) * pokemon.Base.CatchRate * ConditionsDB.GetStatusBonus(pokemon.Status) / ( 3 * pokemon.MaxHp);
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

    IEnumerator TryToEscape(){
        state = BattleState.Busy;

        if(isTrainerBattle){
            yield return dialogBox.TypeDialog("You cannot run from trainer battle!");
            state = BattleState.RunningTurn;
            yield break;
        }

        ++escapeAttempts;
        int playerSpeed = playerUnit.Pokemon.Speed;
        int enemySpeed = enemyUnit.Pokemon.Speed;

        if(playerSpeed > enemySpeed){ 
            yield return dialogBox.TypeDialog($"Looks like {enemyUnit.Pokemon.Base.Name} left you and {playerUnit.Pokemon.Base.Name} alone");
            BattleOver(true);
        } else {
            float f = ( playerSpeed * 128) / enemySpeed + 30 * escapeAttempts;
            f = f % 256;

            if(UnityEngine.Random.Range(0, 255) < f){
                yield return dialogBox.TypeDialog($"Looks like {enemyUnit.Pokemon.Base.Name} left you and {playerUnit.Pokemon.Base.Name} alone.");
                BattleOver(true);
            } else {
                yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} left you and {playerUnit.Pokemon.Base.Name} no chance to escape.");
                state = BattleState.RunningTurn;
            }
        }
    }
}
