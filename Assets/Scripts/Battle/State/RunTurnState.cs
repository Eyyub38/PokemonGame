using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GDEUtills.StateMachine;
using System.Linq;

public class RunTurnState : State<BattleSystem>{
    BattleSystem battleSystem;
    PartyScreen partyScreen;
    BattleDialogBox dialogBox;
    PokemonParty playerParty;
    PokemonParty trainerParty;

    bool isTrainerBattle;

    public List<BattleAction> Actions { get; set;}

    public static RunTurnState i {get; private set;}

    void Awake(){
        i = this;
    }

    public override void Enter(BattleSystem owner){
        battleSystem = owner;

        partyScreen = battleSystem.PartyScreen;
        dialogBox = battleSystem.DialogBox;
        playerParty = battleSystem.PlayerParty;
        trainerParty = battleSystem.TrainerParty;
        isTrainerBattle = battleSystem.IsTrainerBattle;

        StartCoroutine(RunTurns());
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

            for(int i = 0; i < battleSystem.ActivePlayerUnitsCount; i++){
                var playerUnit = battleSystem.PlayerUnits[i];

                playerUnit.Pokemon.GainEvs(faintedUnit.Pokemon.Base.EvYields);

                int expGain = Mathf.FloorToInt( expYield * enemyLevel * trainerBonus)  / ( 7 * battleSystem.ActivePlayerUnitsCount);
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
            }
            yield return new WaitForSeconds(1f);
        }

        yield return NextStepsAfterFainting(faintedUnit);
    }

    IEnumerator RunTurns(){
        foreach(BattleAction action in Actions){
        
            if(action.IsInvalid){
                continue;   
            }
        
            if(action.Type == BattleActionType.Move){
                action.User.Pokemon.CurrentMove = action.SelectedMove;
                yield return RunMove(action.User, action.Target, action.SelectedMove);
                yield return RunAfterTurn(action.User);

            } else if(action.Type == BattleActionType.SwitchPokemon){
                yield return battleSystem.SwitchPokemon(action.SelectedPokemon, action.User);

            } else if(action.Type == BattleActionType.UseItem){
                if(action.SelectedItem is PokeballItem){
                    yield return battleSystem.ThrowPokeball(action.SelectedItem as PokeballItem);
                }

            } else if(action.Type == BattleActionType.Run){
                yield return TryToEscape();
            }
            
            if(battleSystem.IsBattleOver) break;
        }
        if(battleSystem.Field.Weather != null){
            yield return RunWeatherEffects(battleSystem.Field.Weather);
        }
        
        battleSystem.ClearTurnData();

        if(!battleSystem.IsBattleOver){
            battleSystem.StateMachine.ChangeState(ActionSelectionState.i);
        }
    }

    IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move){
        bool canRunMove = sourceUnit.Pokemon.OnBeforeTurn();
        if(canRunMove == false){
            yield return ShowStatusChanges(sourceUnit);
            yield break;
        }
        yield return ShowStatusChanges(sourceUnit);

        move.PP--;
        if(move.Base == GlobalSettings.i.BackUpMove){
            yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name} has no more moves left!");
        }
        yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name} used {move.Base.Name}.");

        if(CheckIfMoveHits(move, sourceUnit.Pokemon, targetUnit.Pokemon)){
            int hitCount = 0;
            float typeEffectiveness = 1;

            for(int i = 1; i <= move.Base.GetHitTimes(); ++i){

                var damageDetails = new DamageDetails();
                sourceUnit.PlayAttackAnimation();
                AudioManager.i.PlaySfx(move.Base.SoundEffect);
                yield return new WaitForSeconds(1f);

                targetUnit.PlayHitAnimation();
                AudioManager.i.PlaySfx(AudioId.Hit);

                if(move.Base.Category == MoveCategory.Status){
                    yield return RunMoveEffects(move.Base.Effects, sourceUnit, targetUnit, move.Base.Target);

                } else {
                    float weatherModifier = battleSystem.Field.Weather?.OnDamageModify?.Invoke(move) ?? 1f;

                    damageDetails = targetUnit.Pokemon.TakeDamage(move, sourceUnit.Pokemon, weatherModifier);
                    yield return targetUnit.Hud.UpdateHPAsync();
                    yield return ShowDamageDetails(damageDetails);
                    typeEffectiveness = damageDetails.TypeEffectiveness;
                }

                if(move.Base.Secondaries != null && move.Base.Secondaries.Count > 0 && targetUnit.Pokemon.HP > 0){
                    foreach(var secondary in move.Base.Secondaries){
                        var rnd = UnityEngine.Random.Range(1, 101);
                        if(rnd <= secondary.Chance){
                        yield return RunMoveEffects(secondary, sourceUnit, targetUnit, secondary.Target);
                        }
                    }
                }

                yield return RunAfterMove(damageDetails, move.Base, sourceUnit, targetUnit);

                hitCount++;

                if(targetUnit.Pokemon.HP <= 0){
                    break;
                }
            }

            yield return ShowTypeEffectiveness(typeEffectiveness);

            if(move.Base.IsMultiHitMove){
                yield return dialogBox.TypeDialog($"Hit {hitCount} times!");
            }

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
            yield return sourceUnit.Hud.UpdateHPAsync();
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

        yield return ShowStatusChanges(sourceUnit);
        yield return ShowStatusChanges(targetUnit);
    }

    IEnumerator RunAfterTurn(BattleUnit sourceUnit){
        if(battleSystem.IsBattleOver) yield break;

        sourceUnit.Pokemon.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit);
        
        if(sourceUnit.Pokemon.HP <= 0){
            yield return HandlePokemonFainted(sourceUnit);
        }
    }

    IEnumerator RunMoveEffects(MoveEffects effects, BattleUnit sourceUnit, BattleUnit targetUnit, MoveTarget moveTarget){
        var source = sourceUnit.Pokemon;
        var target = targetUnit.Pokemon;

        if(effects.Boosts != null){
            if(moveTarget == MoveTarget.Self){
                source.ApplyBoosts(effects.Boosts, source);
            } else {
                target.ApplyBoosts(effects.Boosts, source);
            }

        }

        if(effects.Status != StatusConditionID.None) {
            target.SetStatus(effects.Status);
        }
        if(effects.VolatileStatus != StatusConditionID.None) {
            target.SetVolatileStatus(effects.VolatileStatus);
        }
        if(effects.WeatherStatus != WeatherConditionID.None){
            battleSystem.Field.SetWeather(effects.WeatherStatus, 5);
            yield return dialogBox.TypeDialog(battleSystem.Field.Weather.StartByMoveMessage ?? battleSystem.Field.Weather.StartMessage);
        }

        yield return ShowStatusChanges(sourceUnit);
        yield return ShowStatusChanges(targetUnit);
    }

    IEnumerator RunWeatherEffects(WeatherCondition weather){
        if(battleSystem.Field.WeatherDuration != null){
            if(battleSystem.Field.WeatherDuration > 0){
                --battleSystem.Field.WeatherDuration;
            } else {
                battleSystem.Field.SetWeather(WeatherConditionID.None, null);
                yield return dialogBox.TypeDialog(weather.EndMessage);

                yield break;
            }
        }

        if(weather.EffectMessage != null){
            yield return dialogBox.TypeDialog(weather.EffectMessage);
        }

        var units = battleSystem.PlayerUnits.Concat(battleSystem.EnemyUnits);

        foreach(var unit in units){
            if(unit.Pokemon == null || unit.Pokemon.HP <= 0){
                continue;
            }

            weather.OnWeatherEffect?.Invoke(unit.Pokemon);
            yield return ShowStatusChanges(unit);

            if(unit.Pokemon.HP <= 0){
               yield return  HandlePokemonFainted(unit);
            }
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
        moveAccuracy = source.ModifyAccuracy(moveAccuracy, target, move);
        return UnityEngine.Random.Range(1, 101) <= moveAccuracy;
    }

    IEnumerator NextStepsAfterFainting(BattleUnit faintedUnit){

        var actionToRemove = Actions.FirstOrDefault(a => a.User == faintedUnit);

        if(actionToRemove != null){
            actionToRemove.IsInvalid = true;
        }

        if(faintedUnit.IsPlayerUnit){
            var activePokemons = battleSystem.PlayerUnits.Select(u => u.Pokemon).Where(p => p.HP > 0).ToList();

            var nextPokemon = playerParty.GetHealthyPokemon(doNotInclude: activePokemons);
            if(nextPokemon == null && activePokemons.Count == 0){
                battleSystem.BattleOver(false);

            } else if(nextPokemon == null && activePokemons.Count > 0){
                battleSystem.PlayerUnits.Remove(faintedUnit);
                faintedUnit.Hud.gameObject.SetActive(false);

                var actionsToChange = Actions.Where(a => a.Target = faintedUnit).ToList();
                actionsToChange.ForEach(a => a.Target = battleSystem.PlayerUnits.First());

            } else if(nextPokemon != null){
                yield return battleSystem.SwitchPokemon(PartyState.i.SelectedPokemon, faintedUnit);
                yield return GameController.i.StateMachine.PushAndWait(PartyState.i);
            }

        } else {
            if(!isTrainerBattle){
                battleSystem.BattleOver(true);
                yield break;

            }

            var activePokemons = battleSystem.EnemyUnits.Select(u => u.Pokemon).Where(p => p.HP > 0).ToList();

            var nextPokemon = trainerParty.GetHealthyPokemon(doNotInclude: activePokemons);
            if(nextPokemon == null && activePokemons.Count == 0){
                battleSystem.BattleOver(true);

            } else if(nextPokemon == null && activePokemons.Count > 0){
                battleSystem.EnemyUnits.Remove(faintedUnit);
                faintedUnit.Hud.gameObject.SetActive(false);

                var actionsToChange = Actions.Where(a => a.Target = faintedUnit).ToList();
                actionsToChange.ForEach(a => a.Target = battleSystem.EnemyUnits.First());

            } else if(nextPokemon != null){
                if(battleSystem.UnitCount == 1){
                    AboutToUseState.i.NewPokemon = nextPokemon;
                    yield return battleSystem.StateMachine.PushAndWait(AboutToUseState.i);

                } else {
                    yield return battleSystem.SendNextTrainerPokemon(battleSystem.EnemyUnits.IndexOf(faintedUnit));
                }
            }
        }
    }

    IEnumerator ShowDamageDetails(DamageDetails damageDetails){
        if(damageDetails.Critical > 1f){
            yield return dialogBox.TypeDialog("A critical hit!");
        }
    }

    IEnumerator ShowTypeEffectiveness(float typeEffectiveness){
        if(typeEffectiveness > 1f){
            yield return dialogBox.TypeDialog($"It's super effective!");
        } else if(typeEffectiveness < 1f){
            yield return dialogBox.TypeDialog($"It's not very effective...");
        } else if(typeEffectiveness == 0f){
            yield return dialogBox.TypeDialog($"It doesn't affect...");
        }
    }

    IEnumerator ShowStatusChanges(BattleUnit pokemonUnit){
        var pokemon = pokemonUnit.Pokemon;

        while (pokemon.StatusChanges.Count > 0){
            var statusEvent = pokemon.StatusChanges.Dequeue();
            yield return dialogBox.TypeDialog(statusEvent.Message);

            if(statusEvent.Type == StatusEventType.Damage){
                pokemonUnit.PlayHitAnimation();
                AudioManager.i.PlaySfx(AudioId.Hit);
                yield return pokemonUnit.Hud.UpdateHPAsync();
            }
        }
    }

    IEnumerator TryToEscape(){
        if(isTrainerBattle){
            yield return dialogBox.TypeDialog("You cannot run from trainer battle!");
            yield break;
        }

        var playerUnit = battleSystem.PlayerUnits[0];
        var enemyUnit = battleSystem.EnemyUnits[0];

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
