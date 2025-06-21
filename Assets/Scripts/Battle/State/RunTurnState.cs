using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GDEUtills.StateMachine;
using System.Linq;

public class RunTurnState : State<BattleSystem>{
    BattleSystem battleSystem;
    BattleUnit playerUnit;
    BattleUnit enemyUnit;
    PartyScreen partyScreen;
    BattleDialogBox dialogBox;
    PokemonParty playerParty;
    PokemonParty trainerParty;

    bool isTrainerBattle;

    public static RunTurnState i {get; private set;}

    void Awake(){
        i = this;
    }

    public override void Enter(BattleSystem owner){
        battleSystem = owner;

        playerUnit = battleSystem.PlayerUnit;
        enemyUnit = battleSystem.EnemyUnit;
        partyScreen = battleSystem.PartyScreen;
        dialogBox = battleSystem.DialogBox;
        playerParty = battleSystem.PlayerParty;
        trainerParty = battleSystem.TrainerParty;
        isTrainerBattle = battleSystem.IsTrainerBattle;

        StartCoroutine(RunTurns(battleSystem.SelectedAction));
    }

    IEnumerator HandlePokemonFainted(BattleUnit faintedUnit, bool wasOneHitKnockOut = false){
        if (wasOneHitKnockOut)
            yield return dialogBox.TypeDialog($"It's a One-hit KO!");
        else
            yield return dialogBox.TypeDialog($"{faintedUnit.Pokemon.Base.Name} fainted");

        faintedUnit.PlayFaintedAnimation();
        yield return new WaitForSeconds(2f);

        if(!faintedUnit.IsPlayerUnit){
            bool battleWon = true;
            if(isTrainerBattle){
                battleWon = trainerParty.GetHealthyPokemon() == null;
            }
            if(battleWon){
                if(isTrainerBattle){
                    AudioManager.i.PlayMusic(battleSystem.TrainerVicBattleMusic);
                } else if(!isTrainerBattle){
                    AudioManager.i.PlayMusic(battleSystem.WildVicBattleMusic);
                }
            }
            int expYield = faintedUnit.Pokemon.Base.XpYield;
            int enemyLevel = faintedUnit.Pokemon.Level;
            float trainerBonus = (isTrainerBattle)? 1.5f : 1f;

            playerUnit.Pokemon.GainEvs(faintedUnit.Pokemon.Base.EvYields);

            int expGain = Mathf.FloorToInt( expYield * enemyLevel * trainerBonus)  / 7;
            playerUnit.Pokemon.GainExp(expGain);

            yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} gained {expGain} XP from this battle.");
            yield return playerUnit.Hud.SetExpSmooth();

            while(playerUnit.Pokemon.CheckForLevelUp()) {
                playerUnit.Hud.SetLevel();
                yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} level up to Lvl {playerUnit.Pokemon.Level}!");

                var newMove = playerUnit.Pokemon.GetLearnableMoveAtCurrLevel();
                if(newMove != null) {
                    if(playerUnit.Pokemon.Moves.Count < PokemonBase.MaxNumberOfMoves){
                        playerUnit.Pokemon.LearnMove(newMove.Base);
                        yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} learned {newMove.Base.Name}");
                        dialogBox.SetMoveBars(playerUnit.Pokemon.Moves);
                    } else {
                        yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} trying to learn {newMove.Base.Name}...");
                        yield return dialogBox.TypeDialog($"But its is already knew {PokemonBase.MaxNumberOfMoves} moves.");
                        yield return dialogBox.TypeDialog($"Choose a move to forget.");

                        MoveForgetState.i.BattleSystem = battleSystem;
                        MoveForgetState.i.CurrentMoves = playerUnit.Pokemon.Moves;
                        MoveForgetState.i.NewMove = newMove.Base;
                        MoveForgetState.i.NewMove = newMove.Base;
                        
                        yield return GameController.i.StateMachine.PushAndWait(MoveForgetState.i);

                        var moveIndex = MoveForgetState.i.Selection;
                        if(moveIndex == PokemonBase.MaxNumberOfMoves){
                            yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} didn't learn {newMove.Base.Name}.");
                        } else {
                            var selectedMove = playerUnit.Pokemon.Moves[ moveIndex ].Base;
                            yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} forgot {selectedMove.Name} and learned {newMove.Base.Name}.");
                            playerUnit.Pokemon.Moves[ moveIndex ] = new Move(newMove.Base);
                        }
                    }
                }

                yield return playerUnit.Hud.SetExpSmooth(true);
            }
            yield return new WaitForSeconds(1f);
        }

        yield return CheckForBattleOver(faintedUnit);
    }

    IEnumerator RunTurns(BattleAction playerAction){
        if(playerAction == BattleAction.Move){

            playerUnit.Pokemon.CurrentMove = (battleSystem.SelectedMove != -1) ? playerUnit.Pokemon.Moves[battleSystem.SelectedMove] : new Move(GlobalSettings.i.BackUpMove);
            
            Move enemyMove = enemyUnit.Pokemon.GetRandomMove();
            enemyUnit.Pokemon.CurrentMove = (enemyMove != null) ? enemyMove : new Move(GlobalSettings.i.BackUpMove);

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
            if(battleSystem.IsBattleOver) yield break;

            if(secondPokemon.HP > 0){
                yield return RunMove(secondUnit, firstUnit, secondUnit.Pokemon.CurrentMove);
                yield return RunAfterTurn(secondUnit);
                if(battleSystem.IsBattleOver) yield break;
            }
        } else {
            if(playerAction == BattleAction.SwitchPokemon){
                yield return battleSystem.SwitchPokemon(battleSystem.SelectedPokemon);
            } else if(playerAction == BattleAction.UseItem){
                if(battleSystem.SelectedItem is PokeballItem){
                    yield return battleSystem.ThrowPokeball(battleSystem.SelectedItem as PokeballItem);
                    if(battleSystem.IsBattleOver){
                        yield break;
                    }
                } else {

                }
            } else if(playerAction == BattleAction.Run){
                yield return TryToEscape();
            }

            Move enemyMove = enemyUnit.Pokemon.GetRandomMove();
            enemyUnit.Pokemon.CurrentMove = (enemyMove != null) ? enemyMove : new Move(GlobalSettings.i.BackUpMove);
            yield return RunMove(enemyUnit, playerUnit, enemyUnit.Pokemon.CurrentMove);
            yield return RunAfterTurn(enemyUnit);
            if(battleSystem.IsBattleOver) yield break;
        }
        if(!battleSystem.IsBattleOver){
            battleSystem.StateMachine.ChangeState(ActionSelectionState.i);
        }
    }

    IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move){
        bool canRunMove = sourceUnit.Pokemon.OnBeforeTurn();
        if(canRunMove == false){
            yield return ShowStatusChanges(sourceUnit.Pokemon);
            yield return sourceUnit.Hud.WaitForHPUpdate();
            yield break;
        }
        yield return ShowStatusChanges(sourceUnit.Pokemon);

        move.PP--;
        if(move.Base == GlobalSettings.i.BackUpMove){
            yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name} has no more moves left!");
        }
        yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name} used {move.Base.Name}.");

        if(CheckIfMoveHits(move, sourceUnit.Pokemon, targetUnit.Pokemon)){
            var damageDetails = new DamageDetails();
            sourceUnit.PlayAttackAnimation();
            AudioManager.i.PlaySfx(move.Base.SoundEffect);
            yield return new WaitForSeconds(1f);

            targetUnit.PlayHitAnimation();
            AudioManager.i.PlaySfx(AudioId.Hit);

            if(move.Base.Category == MoveCategory.Status){
                yield return RunMoveEffects(move.Base.Effects, sourceUnit.Pokemon, targetUnit.Pokemon, move.Base.Target);
            } else {
                damageDetails = targetUnit.Pokemon.TakeDamage(move, sourceUnit.Pokemon);
                yield return targetUnit.Hud.WaitForHPUpdate();
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

            yield return RunAfterMove(damageDetails, move.Base, sourceUnit, targetUnit);

            if(targetUnit.Pokemon.HP <= 0){
                yield return HandlePokemonFainted(targetUnit, move.Base.OneHitKoMoveEffect.isOneHitKnockOut);
            }
        } else {
            yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name}'s attack missed!");
        }
    }

    IEnumerator RunAfterMove(DamageDetails damageDetails, MoveBase move, BattleUnit sourceUnit, BattleUnit targetUnit){
        if(damageDetails == null){
            yield break;
        }

        if(move.DrainingPercentage != 0){
            int healedHP = Mathf.Clamp(Mathf.CeilToInt(damageDetails.DamageDealt / 100f * move.DrainingPercentage), 1, sourceUnit.Pokemon.MaxHp);
            sourceUnit.Pokemon.IncreaseHP(healedHP);
            yield return sourceUnit.Hud.WaitForHPUpdate();
        }

        if(move.Recoil.recoilType != RecoilType.None){
            int damage = 0;
            switch(move.Recoil.recoilType){
                case RecoilType.RecoilByMaxHP:
                    int maxHp = sourceUnit.Pokemon.MaxHp;
                    damage = Mathf.FloorToInt(maxHp * move.Recoil.recoilDamage / 100f);
                    sourceUnit.Pokemon.TakeRecoilDamage(damage);
                    break;
                case RecoilType.RecoilByCurrentHP:
                    int currentHp = sourceUnit.Pokemon.HP;
                    damage = Mathf.FloorToInt(currentHp * move.Recoil.recoilDamage / 100f);
                    sourceUnit.Pokemon.TakeRecoilDamage(damage);
                    break;
                case RecoilType.RecoilByDamage:
                    damage = Mathf.FloorToInt(damageDetails.DamageDealt * move.Recoil.recoilDamage / 100f);
                    sourceUnit.Pokemon.TakeRecoilDamage(damage);
                    break;
                default:
                    Debug.LogError($"Unknown recoil type: {move.Recoil.recoilType}");
                    break;
            }
        }

        yield return ShowStatusChanges(sourceUnit.Pokemon);
        yield return ShowStatusChanges(targetUnit.Pokemon);
    }

    IEnumerator RunAfterTurn(BattleUnit sourceUnit){
        if(battleSystem.IsBattleOver) yield break;

        sourceUnit.Pokemon.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit.Pokemon);
        yield return sourceUnit.Hud.WaitForHPUpdate();
        
        if(sourceUnit.Pokemon.HP <= 0){
            yield return HandlePokemonFainted(sourceUnit);
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

    bool CheckIfMoveHits(Move move, Pokemon source, Pokemon target){
        if(move.Base.AlwaysHits){
            return true;
        }
        if (move.Base.OneHitKoMoveEffect.isOneHitKnockOut){
            if (source.Level < target.Level)
            return false;
            if (source.HasType(move.Base.OneHitKoMoveEffect.immunityType))
                return false;

            int baseAccuracy = 30;
            if (move.Base.OneHitKoMoveEffect.lowerOddsException)
                baseAccuracy = (source.HasType(move.Base.Type)) ? 30 : 20;

            int chance = (source.Level - target.Level + baseAccuracy);

            return Random.Range(1, 101) <= chance;
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

    IEnumerator CheckForBattleOver(BattleUnit faintedUnit){
        if(faintedUnit.IsPlayerUnit){
            var nextPokemon = playerParty.GetHealthyPokemon();
            if(nextPokemon != null){
                yield return GameController.i.StateMachine.PushAndWait(PartyState.i);
                yield return battleSystem.SwitchPokemon(PartyState.i.SelectedPokemon);
            } else {
                battleSystem.BattleOver(false);
            }
        } else {
            if(!isTrainerBattle){
                battleSystem.BattleOver(true);
            } else {
                var nextPokemon = trainerParty.GetHealthyPokemon();
                if(nextPokemon != null){
                    AboutToUseState.i.NewPokemon = nextPokemon;
                    yield return battleSystem.StateMachine.PushAndWait(AboutToUseState.i);
                } else {
                    battleSystem.BattleOver(true);
                }
            }
        }
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

    IEnumerator ShowStatusChanges(Pokemon pokemon){
        while (pokemon.StatusChanges.Count > 0){
            var message = pokemon.StatusChanges.Dequeue();
            yield return dialogBox.TypeDialog(message);
        }
    }

    IEnumerator TryToEscape(){
        if(isTrainerBattle){
            yield return dialogBox.TypeDialog("You cannot run from trainer battle!");
            yield break;
        }

        ++battleSystem.EscapeAttempts;
        int playerSpeed = playerUnit.Pokemon.Speed;
        int enemySpeed = enemyUnit.Pokemon.Speed;

        if(playerSpeed > enemySpeed){ 
            yield return dialogBox.TypeDialog($"Looks like {enemyUnit.Pokemon.Base.Name} left you and {playerUnit.Pokemon.Base.Name} alone");
            battleSystem.BattleOver(true);
        } else {
            float f = ( playerSpeed * 128) / enemySpeed + 30 * battleSystem.EscapeAttempts;
            f = f % 256;

            if(UnityEngine.Random.Range(0, 255) < f){
                yield return dialogBox.TypeDialog($"Looks like {enemyUnit.Pokemon.Base.Name} left you and {playerUnit.Pokemon.Base.Name} alone.");
                battleSystem.BattleOver(true);
            } else {
                yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} no chance to escape.");
            }
        }
    }
}
