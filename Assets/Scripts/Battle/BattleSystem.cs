using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum BattleState { Start, ActionSelection, MoveSelection, RunningTurn, PartyScreen, AboutToUse, BattleOver, Busy}
public enum BattleAction { Move, SwitchPokemon, UseItem, Run}

public class BattleSystem : MonoBehaviour{
    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] BattleDialogBox dialogBox;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] Image playerImage;
    [SerializeField] Image trainerImage;
    [SerializeField] GameObject pokeballSprite;

    BattleState state;
    BattleState? prevState;
    int currentAction;
    int currentMove;
    int currentMember;
    bool aboutToUseChoice = true;

    PokemonParty playerParty;
    PokemonParty trainerParty;
    Pokemon wildPokemon;
    PlayerController player;
    TrainerController trainer;

    bool isTrainerBattle = false;

    public Action<bool> OnBattleOver;

    public void StartBattle(PokemonParty playerParty, Pokemon wildPokemon){
        this.playerParty = playerParty;
        this.wildPokemon = wildPokemon;

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
        dialogBox.gameObject.SetActive(true);
    }

    void OpenPartyScreen(){
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
                var selectedPokemon = playerParty.Pokemons[currentMember];
                state = BattleState.Busy;
                yield return SwitchPokemon(selectedPokemon);
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
                yield return dialogBox.TypeDialog($"{targetUnit.Pokemon.Base.Name} fainted");
                targetUnit.PlayFaintedAnimation();
                yield return new WaitForSeconds(2f);

                CheckForBattleOver(targetUnit);
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
            yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name} fainted");
            sourceUnit.PlayFaintedAnimation();
            yield return new WaitForSeconds(2f);

            CheckForBattleOver(sourceUnit);
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
            if (!isTrainerBattle){
                BattleOver(true);
            } else {
                var nextPokemon = trainerParty.GetHealthyPokemon();
                if (nextPokemon != null){
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
        }

        //Just for test
        if(Input.GetKeyDown(KeyCode.RightShift)){
            StartCoroutine(ThrowPokeball());
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
                prevState = state;
                OpenPartyScreen();
            } else if(currentAction == 2){
                //Bag
            } else if(currentAction == 3){
                //Run
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
                prevState = BattleState.AboutToUse;
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
        if(Input.GetKeyDown(KeyCode.DownArrow)){
            currentMember += 2;
        } else if(Input.GetKeyDown(KeyCode.UpArrow)){
            currentMember -= 2;
        } else if(Input.GetKeyDown(KeyCode.RightArrow)){
            currentMember++;
        } else if(Input.GetKeyDown(KeyCode.LeftArrow)){
            currentMember--;
        }

        currentMember = Mathf.Clamp(currentMember, 0, playerParty.Pokemons.Count - 1);
        partyScreen.UpdateMemberSelection(currentMember);

        if(Input.GetKeyDown(KeyCode.Return)){
            var selectedMember = playerParty.Pokemons[currentMember];
            if(selectedMember.HP <= 0){
                partyScreen.SetMessageText($"{selectedMember.Base.Name} is fainted. You cannot send out to battle.");
                return;
            }
            if(selectedMember == playerUnit.Pokemon){
                partyScreen.SetMessageText($"{selectedMember.Base.Name} is already in battle.");
                return;
            }

            partyScreen.gameObject.SetActive(false);
            if(prevState == BattleState.ActionSelection){
                prevState = null;
                StartCoroutine(RunTurns(BattleAction.SwitchPokemon));
            } else {
                state = BattleState.Busy;
                StartCoroutine(SwitchPokemon(selectedMember));
            }

        } else if(Input.GetKeyDown(KeyCode.Escape)){
            if(playerUnit.Pokemon.HP <=0){
                partyScreen.SetMessageText("Your Pokemon is fainted! You need to choose new Pokemon");
                return;
            }
            partyScreen.gameObject.SetActive(false);
            if(prevState == BattleState.AboutToUse){
                prevState = null;
                StartCoroutine(SendNextTrainerPokemon());
            } else {
                ActionSelection();
            }
        }
    }

    IEnumerator SwitchPokemon(Pokemon newPokemon){
        if (playerUnit.Pokemon.HP > 0){
            yield return dialogBox.TypeDialog($"Come back {playerUnit.Pokemon.Base.Name}!. Thank you for your hard work!");
            playerUnit.PlayFaintedAnimation();
            yield return new WaitForSeconds(2f);
        }

        playerUnit.Setup(newPokemon);
        dialogBox.SetMoveBars(newPokemon.Moves);

        yield return dialogBox.TypeDialog($"Your turn {newPokemon.Base.Name}!");

        if(prevState == null){
            state = BattleState.RunningTurn;
        } else if(prevState == BattleState.AboutToUse){
            prevState = null;
            StartCoroutine(SendNextTrainerPokemon());
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

        for (int i = 0; i < 3; i++) {
            if (animator != null)
                animator.Play("Shake", 0, 0f);

            yield return new WaitForSeconds(2f);
        }
    } 
}
