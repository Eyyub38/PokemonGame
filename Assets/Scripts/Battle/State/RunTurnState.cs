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
    HashSet<BattleUnit> unitsActedThisTurn;

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

    IEnumerator HandlePokemonFainted(BattleUnit faintedUnit, BattleUnit killerUnit = null, bool wasOneHitKnockOut = false){
        if (wasOneHitKnockOut)
            yield return dialogBox.TypeDialog($"It's a One-hit KO!");
        else
            yield return dialogBox.TypeDialog($"{faintedUnit.Pokemon.NickName} fainted");

        faintedUnit.PlayFaintedAnimation();
        yield return new WaitForSeconds(2f);

        if (killerUnit != null && killerUnit.Pokemon.HP > 0){
            killerUnit.Pokemon.Ability?.OnKilledFoe?.Invoke(killerUnit.Pokemon, faintedUnit.Pokemon);
        }

        if(!faintedUnit.IsPlayerUnit){
            bool battleWon = true;
            if(isTrainerBattle){
                battleWon = trainerParty.GetVitalReadyPokemon() == null;
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

                int expGain = Mathf.FloorToInt( expYield * enemyLevel * trainerBonus)  / (int)( 7f * battleSystem.ActivePlayerUnitsCount);
                playerUnit.Pokemon.GainExp(expGain);
                AudioManager.i.PlaySfx(AudioId.ExpGain);

                yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} gained {expGain} XP from this battle.");
                // yield return playerUnit.Hud.SetExpSmooth(); // Handled by event subscription in BattleHud.cs

                while(playerUnit.Pokemon.CheckForLevelUp()) {
                    playerUnit.Hud.SetLevel();
                    yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} level up to Lvl {playerUnit.Pokemon.Level}!");

                    var newMoves = playerUnit.Pokemon.GetLearnableMovesAtCurrLevel();
                    foreach (var learnableMove in newMoves) {
                        var newMove = learnableMove.Base;
                        if(playerUnit.Pokemon.Moves.Count < PokemonBase.MaxNumberOfMoves){
                            playerUnit.Pokemon.LearnMove(newMove);
                            yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} learned {newMove.Name}");
                            dialogBox.SetMoveBars(playerUnit.Pokemon.Moves);
                        } else {
                            yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} trying to learn {newMove.Name}...");
                            yield return dialogBox.TypeDialog($"But it already knows {PokemonBase.MaxNumberOfMoves} moves.");
                            yield return dialogBox.TypeDialog($"Choose a move to forget.");

                            MoveForgetState.i.BattleSystem = battleSystem;
                            MoveForgetState.i.CurrentMoves = playerUnit.Pokemon.Moves;
                            MoveForgetState.i.NewMove = newMove;
                            
                            yield return GameController.i.StateMachine.PushAndWait(MoveForgetState.i);

                            var moveIndex = MoveForgetState.i.Selection;
                            if(moveIndex == PokemonBase.MaxNumberOfMoves){
                                yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} didn't learn {newMove.Name}.");
                            } else {
                                var selectedMove = playerUnit.Pokemon.Moves[ moveIndex ].Base;
                                yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} forgot {selectedMove.Name} and learned {newMove.Name}.");
                                playerUnit.Pokemon.SetActiveMove(moveIndex, newMove, PokemonTechniqueLearnSource.LevelUp, "level-up", "Level Up");
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
        unitsActedThisTurn = new HashSet<BattleUnit>();

        foreach(BattleAction action in Actions){
        
            if(action.IsInvalid){
                continue;   
            }
        
            if(action.Type == BattleActionType.Move){
                var moveToRun = action.SelectedMove;
                bool consumeMovePP = true;
                if(action.SelectedPowerMechanic != null){
                    if(!battleSystem.TryUsePowerMechanic(action, out var powerFailureMessage)){
                        yield return dialogBox.TypeDialog(string.IsNullOrWhiteSpace(powerFailureMessage) ? "That power mechanic failed." : powerFailureMessage);
                        continue;
                    }

                    var resolvedMove = action.SelectedPowerMechanic.ResolveBattleMove(action.SelectedMove);
                    if(resolvedMove != action.SelectedMove && action.SelectedPowerMechanic.ConsumeSelectedMovePP && action.SelectedMove != null){
                        action.SelectedMove.DecreasePP();
                    }

                    consumeMovePP = resolvedMove == action.SelectedMove;
                    moveToRun = resolvedMove;
                }

                if(moveToRun == null){
                    yield return dialogBox.TypeDialog("No move was selected.");
                    continue;
                }

                action.User.Pokemon.CurrentMove = moveToRun;
                yield return RunMove(action.User, action.Target, moveToRun, consumeMovePP);
                if(action.User.Pokemon.HP > 0){
                    yield return RunAfterTurn(action.User);
                } else {
                    yield return HandlePokemonFainted(action.User);
                }

            } else if(action.Type == BattleActionType.SwitchPokemon){
                if(!battleSystem.CanSwitchByRule(action.User.IsPlayerUnit, out var failureMessage)){
                    yield return dialogBox.TypeDialog(string.IsNullOrWhiteSpace(failureMessage) ? "Switching is blocked by the current battle rules." : failureMessage);
                    continue;
                }

                yield return battleSystem.SwitchPokemon(action.SelectedPokemon, action.User);
                battleSystem.RecordBattleSwitch(action.User.IsPlayerUnit);

            } else if(action.Type == BattleActionType.UseItem){
                if(!battleSystem.CanUseBattleItem(action.User.IsPlayerUnit, action.SelectedItem, out var failureMessage)){
                    yield return dialogBox.TypeDialog(string.IsNullOrWhiteSpace(failureMessage) ? "Items are blocked by the current battle rules." : failureMessage);
                    continue;
                }

                battleSystem.RecordBattleItemUse(action.User.IsPlayerUnit);
                if(action.SelectedItem is PokeballItem){
                    yield return battleSystem.ThrowPokeball(action.SelectedItem as PokeballItem);
                } else {
                    var usedItem = Inventory.GetInventory().UseItem(action.SelectedItem, action.SelectedPokemon);
                    if(usedItem == null) {
                        yield return dialogBox.TypeDialog($"{action.SelectedItem.Name} had no effect.");
                    } else {
                        yield return dialogBox.TypeDialog($"You use {action.SelectedItem.Name} on {action.SelectedPokemon.Base.Name}!");
                        var affectedUnit = battleSystem.PlayerUnits.FirstOrDefault(unit => unit.Pokemon == action.SelectedPokemon)
                            ?? battleSystem.EnemyUnits.FirstOrDefault(unit => unit.Pokemon == action.SelectedPokemon);
                        if(affectedUnit != null) {
                            yield return affectedUnit.Hud.UpdateHPAsync();
                        }
                    }
                }

            } else if(action.Type == BattleActionType.Run){
                if(!battleSystem.CanRunByRule(out var failureMessage)){
                    yield return dialogBox.TypeDialog(string.IsNullOrWhiteSpace(failureMessage) ? "Running is blocked by the current battle rules." : failureMessage);
                    continue;
                }

                yield return TryToEscape();
            } else if(action.Type == BattleActionType.PowerMechanic){
                if(!battleSystem.TryUsePowerMechanic(action, out var failureMessage)){
                    yield return dialogBox.TypeDialog(string.IsNullOrWhiteSpace(failureMessage) ? "That power mechanic failed." : failureMessage);
                    continue;
                }

                yield return ShowStatusChanges(action.User);
            }

            unitsActedThisTurn.Add(action.User);
            
            if(battleSystem.IsBattleOver) break;
        }
        if(battleSystem.Field.Weather != null){
            yield return RunWeatherEffects(battleSystem.Field.Weather);
        }

        if(battleSystem.Field.Terrain != null){
            yield return RunTerrainEffects(battleSystem.Field.Terrain);
        }

        battleSystem.Field.TickScreens();

        if(!battleSystem.IsBattleOver && battleSystem.RecordRuleTurnCompleted(out var ruleMessage, out var outcome)){
            yield return dialogBox.TypeDialog(ruleMessage);
            if(outcome == BattleRuleTurnLimitOutcome.PlayerWins){
                battleSystem.BattleOver(true);
            } else if(outcome == BattleRuleTurnLimitOutcome.PlayerLoses){
                battleSystem.BattleOver(false);
            }
        }
        
        battleSystem.ClearTurnData();

        if(!battleSystem.IsBattleOver){
            battleSystem.StateMachine.ChangeState(ActionSelectionState.i);
        }
    }

    IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move, bool consumeMovePP = true){
        if (battleSystem.Field.Terrain?.Id == TerrainID.Psychic && move.Base.Priority > 0 && targetUnit.Pokemon.Ability?.Name != "Levitate" && !targetUnit.Pokemon.HasType(PokemonType.Flying)){
            yield return dialogBox.TypeDialog($"{targetUnit.Pokemon.NickName} was protected by the Psychic Terrain!");
            yield break;
        }

        bool canRunMove = sourceUnit.Pokemon.OnBeforeTurn();
        if(canRunMove == false){
            yield return ShowStatusChanges(sourceUnit);
            yield break;
        }

        yield return ShowStatusChanges(sourceUnit);

        if(!sourceUnit.Pokemon.CanUseMove(move, battleSystem.ActiveVitalProfile)){
            yield return dialogBox.TypeDialog(sourceUnit.Pokemon.GetMoveRestrictionMessage(move, battleSystem.ActiveVitalProfile));
            sourceUnit.Pokemon.ConsecutiveUseCount = 0;
            yield break;
        }

        if(!sourceUnit.Pokemon.TrySpendMoveVitalCost(move, out var vitalFailureMessage, battleSystem.ActiveVitalProfile)){
            yield return dialogBox.TypeDialog(vitalFailureMessage);
            sourceUnit.Pokemon.ConsecutiveUseCount = 0;
            yield break;
        }

        if(consumeMovePP){
            move.DecreasePP();
        }
        if(move.Base == GlobalSettings.i.BackUpMove){
            yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name} has no more moves left!");
        }
        yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name} used {move.Base.Name}.");
        sourceUnit.Pokemon.LockMoveIfNeeded(move);

        bool targetIsProtected = targetUnit.IsPlayerUnit
            ? battleSystem.Field.PlayerProtect
            : battleSystem.Field.EnemyProtect;

        if(targetIsProtected && move.Base.Target == MoveTarget.Foe){
            yield return dialogBox.TypeDialog($"{targetUnit.Pokemon.NickName} protected itself!");
            sourceUnit.Pokemon.ConsecutiveUseCount = 0;
            yield break;
        }

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
                    var moveType = sourceUnit.Pokemon.GetMoveType(move, targetUnit.Pokemon);
                    float weatherModifier = battleSystem.Field.Weather?.OnDamageModify?.Invoke(moveType) ?? 1f;

                    damageDetails = targetUnit.Pokemon.TakeDamage(move, sourceUnit.Pokemon, weatherModifier, battleSystem.ActiveVitalProfile);
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

                if(sourceUnit.Pokemon.HP <= 0){
                    break;
                }

                if(targetUnit.Pokemon.HP <= 0){
                    break;
                }
            }

            yield return ShowTypeEffectiveness(typeEffectiveness);

            if(move.Base.IsMultiHitMove){
                yield return dialogBox.TypeDialog($"Hit {hitCount} times!");
            }

            if(targetUnit.Pokemon.HP <= 0){
                yield return HandlePokemonFainted(targetUnit, sourceUnit, move.Base.OneHitKoMoveEffect.isOneHitKnockOut);
            }

            // Increment streak for escalating-power moves
            if(move.Base.MovePowerBasedOn == PowerBasedOn.FuryCutter){
                sourceUnit.Pokemon.ConsecutiveUseCount = Mathf.Min(sourceUnit.Pokemon.ConsecutiveUseCount + 1, 4);
            } else {
                sourceUnit.Pokemon.ConsecutiveUseCount = 0;
            }

        } else {
            sourceUnit.Pokemon.ConsecutiveUseCount = 0; // reset on miss
            yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.NickName}'s attack missed!");
        }
    }

    IEnumerator RunAfterMove(DamageDetails damageDetails, MoveBase move, BattleUnit sourceUnit, BattleUnit targetUnit){
        if(damageDetails == null){
            yield break;
        }

        if(sourceUnit.Pokemon.HP <= 0){
            yield return ShowStatusChanges(sourceUnit);
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
        var effectPokemon = moveTarget == MoveTarget.Self ? source : target;
        var effectUnit = moveTarget == MoveTarget.Self ? sourceUnit : targetUnit;

        if(effects.Boosts != null){
            if(moveTarget == MoveTarget.Self){
                source.ApplyBoosts(effects.Boosts, source);
            } else {
                target.ApplyBoosts(effects.Boosts, source);
            }

        }

        if(effects.Status != StatusConditionID.None) {
            effectPokemon.SetStatus(effects.Status);
        }
        if(effects.VolatileStatus != StatusConditionID.None) {
            effectPokemon.SetVolatileStatus(effects.VolatileStatus);
        }
        if(effects.WeatherStatus != WeatherConditionID.None){
            battleSystem.Field.SetWeather(effects.WeatherStatus, 5);
            yield return dialogBox.TypeDialog(battleSystem.Field.Weather.StartByMoveMessage ?? battleSystem.Field.Weather.StartMessage);
        }
        if(effects.TerrainStatus != TerrainID.None){
            battleSystem.Field.SetTerrain(effects.TerrainStatus, 5);
            yield return dialogBox.TypeDialog(battleSystem.Field.Terrain.StartMessage);
        }

        if(effects.HealingPercentage > 0){
            var healAmount = Mathf.Max(1, Mathf.FloorToInt(effectPokemon.MaxHp * effects.HealingPercentage / 100f));
            effectPokemon.IncreaseHP(healAmount);
            yield return effectUnit.Hud.UpdateHPAsync();
            yield return dialogBox.TypeDialog($"{effectPokemon.NickName} restored health!");
        }

        if(effects.Flinch){
            if(!unitsActedThisTurn.Contains(targetUnit)){
                target.SetVolatileStatus(StatusConditionID.Flinch);
            }
        }

        if(effects.Taunt){
            effectPokemon.ApplyTaunt(effects.TauntTurns);
        }

        if(effects.Disable){
            effectPokemon.ApplyDisable(effects.DisableTurns);
        }

        if(effects.Encore){
            effectPokemon.ApplyEncore(effects.EncoreTurns);
        }

        if(effects.ClearUserStatBoosts){
            source.ClearStatBoosts();
        }

        if(effects.ClearTargetStatBoosts){
            target.ClearStatBoosts();
        }

        if(effects.Spikes){
            if(sourceUnit.IsPlayerUnit){
                if(battleSystem.Field.EnemySpikes < 3){
                    battleSystem.Field.EnemySpikes++;
                    yield return dialogBox.TypeDialog("Spikes were scattered all around the opposing team's feet!");
                }
            } else {
                if(battleSystem.Field.PlayerSpikes < 3){
                    battleSystem.Field.PlayerSpikes++;
                    yield return dialogBox.TypeDialog("Spikes were scattered all around your team's feet!");
                }
            }
        }

        if(effects.StealthRock){
            if(sourceUnit.IsPlayerUnit){
                if(!battleSystem.Field.EnemyStealthRock){
                    battleSystem.Field.EnemyStealthRock = true;
                    yield return dialogBox.TypeDialog("Pointed stones float in the air around the opposing team!");
                }
            } else {
                if(!battleSystem.Field.PlayerStealthRock){
                    battleSystem.Field.PlayerStealthRock = true;
                    yield return dialogBox.TypeDialog("Pointed stones float in the air around your team!");
                }
            }
        }

        if(effects.FocusEnergy){
            source.CritStage = Mathf.Min(source.CritStage + 2, 3);
            yield return dialogBox.TypeDialog($"{source.NickName} is getting pumped!");
        }

        if(effects.Reflect){
            if(sourceUnit.IsPlayerUnit && battleSystem.Field.PlayerReflect == 0){
                battleSystem.Field.PlayerReflect = 5;
                yield return dialogBox.TypeDialog("Reflect raised your team's Defense!");
            } else if(!sourceUnit.IsPlayerUnit && battleSystem.Field.EnemyReflect == 0){
                battleSystem.Field.EnemyReflect = 5;
                yield return dialogBox.TypeDialog("Reflect raised the opposing team's Defense!");
            }
        }

        if(effects.LightScreen){
            if(sourceUnit.IsPlayerUnit && battleSystem.Field.PlayerLightScreen == 0){
                battleSystem.Field.PlayerLightScreen = 5;
                yield return dialogBox.TypeDialog("Light Screen raised your team's Sp. Def!");
            } else if(!sourceUnit.IsPlayerUnit && battleSystem.Field.EnemyLightScreen == 0){
                battleSystem.Field.EnemyLightScreen = 5;
                yield return dialogBox.TypeDialog("Light Screen raised the opposing team's Sp. Def!");
            }
        }

        if(effects.AuroraVeil){
            if(sourceUnit.IsPlayerUnit && battleSystem.Field.PlayerAuroraVeil == 0){
                battleSystem.Field.PlayerAuroraVeil = 5;
                yield return dialogBox.TypeDialog("Aurora Veil reduced the damage your team takes!");
            } else if(!sourceUnit.IsPlayerUnit && battleSystem.Field.EnemyAuroraVeil == 0){
                battleSystem.Field.EnemyAuroraVeil = 5;
                yield return dialogBox.TypeDialog("Aurora Veil reduced the damage the opposing team takes!");
            }
        }

        if(effects.Protect){
            if(battleSystem.Field.TrySetProtect(sourceUnit.IsPlayerUnit)){
                yield return dialogBox.TypeDialog($"{source.NickName} protected itself!");
            } else {
                yield return dialogBox.TypeDialog($"But it failed!");
            }
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

        if (weather.OnWeatherEffect != null){
            foreach (var unit in battleSystem.AllUnits){
                if (unit.Pokemon == null || unit.Pokemon.HP <= 0) continue;
                weather.OnWeatherEffect(unit.Pokemon);
                yield return ShowStatusChanges(unit);
                if (unit.Pokemon.HP <= 0) yield return HandlePokemonFainted(unit);
                if (battleSystem.IsBattleOver) yield break;
            }
        }
    }

    IEnumerator RunTerrainEffects(TerrainCondition terrain){
        if (battleSystem.Field.TerrainDuration != null){
            battleSystem.Field.TerrainDuration--;
            if (battleSystem.Field.TerrainDuration <= 0){
                yield return dialogBox.TypeDialog(terrain.EndMessage);
                battleSystem.Field.SetTerrain(TerrainID.None);
            }
        }

        if (terrain.OnAfterTurn != null){
            foreach (var unit in battleSystem.AllUnits){
                terrain.OnAfterTurn(unit.Pokemon);
                yield return ShowStatusChanges(unit);
                if (battleSystem.IsBattleOver) yield break;
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

            var nextPokemon = playerParty.GetVitalReadyPokemon(doNotInclude: activePokemons);
            if(nextPokemon == null && activePokemons.Count == 0){
                battleSystem.BattleOver(false);

            } else if(nextPokemon == null && activePokemons.Count > 0){
                battleSystem.PlayerUnits.Remove(faintedUnit);
                faintedUnit.Hud.gameObject.SetActive(false);

                var actionsToChange = Actions.Where(a => a.Target == faintedUnit).ToList();
                actionsToChange.ForEach(a => a.Target = battleSystem.PlayerUnits.First());

            } else if(nextPokemon != null){
                yield return GameController.i.StateMachine.PushAndWait(PartyState.i);
                yield return battleSystem.SwitchPokemon(PartyState.i.SelectedPokemon, faintedUnit);
            }

        } else {
            if(!isTrainerBattle){
                battleSystem.BattleOver(true);
                yield break;

            }

            var activePokemons = battleSystem.EnemyUnits.Select(u => u.Pokemon).Where(p => p.HP > 0).ToList();

            var nextPokemon = trainerParty.GetVitalReadyPokemon(doNotInclude: activePokemons);
            if(nextPokemon == null && activePokemons.Count == 0){
                battleSystem.BattleOver(true);

            } else if(nextPokemon == null && activePokemons.Count > 0){
                battleSystem.EnemyUnits.Remove(faintedUnit);
                faintedUnit.Hud.gameObject.SetActive(false);

                var actionsToChange = Actions.Where(a => a.Target == faintedUnit).ToList();
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
            AudioManager.i.PlaySfx(AudioId.CriticalHit);
            yield return dialogBox.TypeDialog("A critical hit!");
        }
    }

    IEnumerator ShowTypeEffectiveness(float typeEffectiveness){
        if(typeEffectiveness == 0f){
            yield return dialogBox.TypeDialog($"It doesn't affect...");
        } else if(typeEffectiveness > 1f){
            yield return dialogBox.TypeDialog($"It's super effective!");
        } else if(typeEffectiveness < 1f){
            yield return dialogBox.TypeDialog($"It's not very effective...");
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
            } else if(statusEvent.Type == StatusEventType.Heal){
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
            float f = ( playerSpeed * 128f) / enemySpeed + 30 * battleSystem.EscapeAttempts;
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
