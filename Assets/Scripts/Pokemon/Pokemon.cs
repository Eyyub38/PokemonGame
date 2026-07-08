using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public enum Gender{ None, Male, Female, Genderless}
public enum StatusEventType { Text, Damage, Heal, StatBoost}
public enum PokemonCareCategory { General, Feeding, Grooming, Playing, Training, Resting, Medical, Bonding, Cleaning, Custom }

[System.Serializable]
public class Pokemon{
    string instanceId;
    PokemonBase battleBaseOverride;
    [Tooltip("Pokemon species/base data.")]
    [SerializeField] PokemonBase _base;
    [Tooltip("Starting/current level for this Pokemon.")]
    [Min(1)]
    [SerializeField] int level;
    [Tooltip("Starting/current gender. None lets initialization roll it from the species data.")]
    [SerializeField] Gender gender;
    [Tooltip("Pokeball this Pokemon is stored in.")]
    [SerializeField] PokeballItem pokeball;


    public PokemonBase Base { get{ return battleBaseOverride != null ? battleBaseOverride : _base; }}
    public PokemonBase OriginalBase { get{ return _base; }}
    public bool HasTemporaryBattleBase => battleBaseOverride != null;
    public string InstanceId {
        get {
            EnsureInstanceId();
            return instanceId;
        }
    }
    public int Level { get{ return level; } }
    public Gender Gender { get{ return gender; } set{ gender = value; } }
    public PokeballItem Pokeball { get => pokeball; set => pokeball = value; }
    public Dictionary<Stat, int> StatEffortValues { get; private set; }
    public bool IsShiny { get; set; }
    public int Friendship { get; set; }
    public Nature Nature { get; private set; }
    public PersonalityID PersonalityID { get; private set; }
    public Personality Personality => PersonalityDB.Personalities[PersonalityID];
    public string Nickname { get; set; }
    public string NickName { get { return Nickname ?? Base.Name; } }
    public ItemBase HeldItem { get; set; }
    public List<PokemonMoodValue> MoodValues { get; private set; } = new List<PokemonMoodValue>();
    public List<PokemonCareNeedValue> CareNeedValues { get; private set; } = new List<PokemonCareNeedValue>();
    public List<PokemonCareRecord> CareRecords { get; private set; } = new List<PokemonCareRecord>();
    public PokemonVitalState VitalState { get; private set; } = new PokemonVitalState();
    public List<PokemonTimedRecoveryEffect> ActiveTimedRecoveryEffects { get; private set; } = new List<PokemonTimedRecoveryEffect>();
    public List<BattlePowerMechanicRuntimeEffect> ActivePowerMechanicEffects { get; private set; } = new List<BattlePowerMechanicRuntimeEffect>();
    public PokemonGrowthState GrowthState { get; private set; } = new PokemonGrowthState();
    public PokemonEvolutionRuntimeState EvolutionState { get; private set; } = new PokemonEvolutionRuntimeState();
    public PokemonTechniqueMemoryState TechniqueMemory { get; private set; } = new PokemonTechniqueMemoryState();
    public PokemonAbilityTreeState AbilityTreeState { get; private set; } = new PokemonAbilityTreeState();
    public event System.Action OnHpChanged;
    public event System.Action OnVitalsChanged;
    public event System.Action OnMoodChanged;
    public event System.Action OnCareChanged;


    public int HP{ get; set; }
    public int Exp{ get; set; }
    public List<Move> Moves{ get; set; }
    public Move CurrentMove{ get; set; }
    public Move LockedMove{ get; private set; }
    public Move DisabledMove{ get; private set; }
    public Move EncoreMove{ get; private set; }
    public int DisableTurns{ get; private set; }
    public int EncoreTurns{ get; private set; }
    public int TauntTurns{ get; private set; }
    public Dictionary<Stat, int> Stats{get; private set;}
    public Dictionary<Stat, int> StatBoosts{ get; private set;}
    public int CritStage { get; set; }
    public int ConsecutiveUseCount { get; set; }
    public StatusCondition Status{ get; private set;}
    public StatusCondition VolatileStatus{ get; private set;}
    public int StatusTime{ get; set; }
    public int VolatileStatusTime{ get; set; }

    public Queue<StatusEvent> StatusChanges { get; private set; }

    public event System.Action OnStatusChanged;
    public event System.Action OnExpChanged;
    
    public int MaxHp{ get; private set;}
    public int Attack{ get{ return GetStat(Stat.Attack);}}
    public int Defense{ get{ return GetStat(Stat.Defense);}}
    public int SpAttack{ get{ return GetStat(Stat.SpAttack);}}
    public int SpDefense{ get{ return GetStat(Stat.SpDefense);}}
    public int Speed{ get{ return GetStat(Stat.Speed);}}

    public Ability Ability {get; set;}
    
    public Pokemon(PokemonBase pBase, int pLvl, PokeballItem pokeball = null){
        _base = pBase;
        level = pLvl;
        this.pokeball = pokeball;
        Init();
    }

    public Pokemon(PokemonSaveData saveData){
        _base = PokemonDB.GetObjectByName(saveData.name);
        instanceId = string.IsNullOrWhiteSpace(saveData.instanceId) ? System.Guid.NewGuid().ToString("N") : saveData.instanceId;
        HP = saveData.Hp;
        level = saveData.level;
        Exp = saveData.xp;
        pokeball = ItemDB.GetObjectByName(saveData.pokeball) as PokeballItem;
        
        if(saveData.statusId != null){
            Status = StatusConditionsDB.Conditions[saveData.statusId.Value];
        } else {
            Status = null;
        }

        Moves = saveData.moves.Select(s => new Move(s)).ToList();
        TechniqueMemory = saveData.techniqueMemory ?? new PokemonTechniqueMemoryState();
        SyncTechniqueMemoryWithActiveMoves();

        gender = saveData.gender;
        StatEffortValues = saveData.StatEffortValues;
        if (saveData.abilityId != AbilityID.None) {
            Ability = AbilityDB.Abilities[saveData.abilityId];
        }
        IsShiny = saveData.isShiny;
        Friendship = saveData.friendship;
        Nature = NatureDB.Natures[saveData.natureId];
        PersonalityID = saveData.personalityId;
        Nickname = saveData.nickname;
        MoodValues = saveData.moodValues ?? new List<PokemonMoodValue>();
        CareNeedValues = saveData.careNeedValues ?? new List<PokemonCareNeedValue>();
        CareRecords = saveData.careRecords ?? new List<PokemonCareRecord>();
        VitalState = new PokemonVitalState();
        VitalState.Restore(saveData.vitalState);
        GrowthState = saveData.growthState ?? new PokemonGrowthState();
        EvolutionState = saveData.evolutionState ?? new PokemonEvolutionRuntimeState();
        AbilityTreeState = saveData.abilityTreeState ?? new PokemonAbilityTreeState();
        if(saveData.heldItem != null){
            HeldItem = ItemDB.GetObjectByName(saveData.heldItem);
        }

        CalculateStats();
        EnsureVitalStateInitialized();
        StatusChanges = new Queue<StatusEvent>();
        ResetStatBoosts();
        VolatileStatus = null;
    }

    public void Init(){
        EnsureInstanceId();
        Moves = new List<Move>();
        foreach(var move in Base.LearnableMoves){
            if(Level >= move.Level){
                Moves.Add(new Move(move.Base));
            }
            if(Moves.Count >= PokemonBase.MaxNumberOfMoves){
                break;
            }
        }
        TechniqueMemory = new PokemonTechniqueMemoryState();
        SyncTechniqueMemoryWithActiveMoves();
        if(gender == Gender.None){
            if (Base.IsGenderless){
                gender = Gender.Genderless;
            } else {
                gender = UnityEngine.Random.value < Base.MaleRatio ? Gender.Male : Gender.Female;
            }
        }
        
        StatEffortValues = new Dictionary<Stat, int>() { { Stat.HitPoints, 0 }, { Stat.Attack, 0 }, { Stat.Defense, 0 }, { Stat.SpAttack, 0 }, { Stat.SpDefense, 0 }, { Stat.Speed, 0 } };
        IsShiny = (UnityEngine.Random.Range(0, 4096) == 0);
        Friendship = 70; // Base friendship
        Nature = NatureDB.Natures[(NatureID)UnityEngine.Random.Range(0, 25)];
        PersonalityID = PersonalityDB.GetRandomPersonalityID();
        MoodValues = new List<PokemonMoodValue>();
        CareNeedValues = new List<PokemonCareNeedValue>();
        CareRecords = new List<PokemonCareRecord>();

        Exp = Base.GetExpForLevel(Level);
        StatusChanges = new Queue<StatusEvent>();
        CalculateStats();
        HP = MaxHp;
        VitalState = new PokemonVitalState();
        EnsureVitalStateInitialized();
        GrowthState = new PokemonGrowthState();
        EvolutionState = new PokemonEvolutionRuntimeState();
        AbilityTreeState = new PokemonAbilityTreeState();

        if(Base.AbilityID != AbilityID.None){
            Ability = AbilityDB.Abilities[Base.AbilityID];
        }

        ResetStatBoosts();
        Status = null;
        VolatileStatus = null;
    }

    public void OnBattleEntry(List<Pokemon> enemies){
        Ability?.OnBattleEntry?.Invoke(this, enemies);
    }

    public void OnSwitchOut(){
        Ability?.OnSwitchOut?.Invoke(this);
        ClearMoveRestrictions();
        ClearPowerMechanicEffectsOnSwitch();
    }

    void CalculateStats(){
        Stats = new Dictionary<Stat, int>();

        Stats.Add(Stat.Attack, ApplyGrowthToStat(Stat.Attack, Mathf.FloorToInt(((((2f * Base.Attack + (StatEffortValues[Stat.Attack] / 4f)) * Level) / 100f) + 5f) * Nature.GetMultiplier(Stat.Attack))));
        Stats.Add(Stat.Defense, ApplyGrowthToStat(Stat.Defense, Mathf.FloorToInt(((((2f * Base.Defense + (StatEffortValues[Stat.Defense] / 4f)) * Level) / 100f) + 5f) * Nature.GetMultiplier(Stat.Defense))));
        Stats.Add(Stat.SpAttack, ApplyGrowthToStat(Stat.SpAttack, Mathf.FloorToInt(((((2f * Base.SpAttack + (StatEffortValues[Stat.SpAttack] / 4f)) * Level) / 100f) + 5f) * Nature.GetMultiplier(Stat.SpAttack))));
        Stats.Add(Stat.SpDefense, ApplyGrowthToStat(Stat.SpDefense, Mathf.FloorToInt(((((2f * Base.SpDefense + (StatEffortValues[Stat.SpDefense] / 4f)) * Level) / 100f) + 5f) * Nature.GetMultiplier(Stat.SpDefense))));
        Stats.Add(Stat.Speed, ApplyGrowthToStat(Stat.Speed, Mathf.FloorToInt(((((2f * Base.Speed + (StatEffortValues[Stat.Speed] / 4f)) * Level) / 100f) + 5f) * Nature.GetMultiplier(Stat.Speed))));

        int oldMaxHP = MaxHp;
        MaxHp = ApplyGrowthToStat(Stat.HitPoints, Mathf.FloorToInt((((2f * Base.MaxHp + (StatEffortValues[Stat.HitPoints] / 4f)) * Level) / 100f) + Level + 10f));

        if (oldMaxHP != 0){
            HP += MaxHp - oldMaxHP;
        }
        VitalState?.Clamp(this);
        if(VitalState != null && VitalState.initialized) {
            ClampHPToCoreHealthCap();
        }
    }

    public bool CheckForLevelUp(){
        if(Exp > Base.GetExpForLevel(level + 1)){
            ++level;
            CalculateStats();
            return true;
        }
        return false;
    }

    void ResetStatBoosts(){
        StatBoosts = new Dictionary<Stat, int>(){
            {Stat.Attack, 0},
            {Stat.Defense, 0},
            {Stat.SpAttack, 0},
            {Stat.SpDefense, 0},
            {Stat.Speed, 0},
            {Stat.Accuracy, 0},
            {Stat.Evasion, 0}
        };
    }

    public void ClearStatBoosts(){
        ResetStatBoosts();
        AddStatusEvent($"{NickName}'s stat changes were reset!");
    }

    /// <summary>
    /// Set by BattleSystem when a battle starts, cleared when it ends.
    /// Allows Pokemon to apply weather-based stat modifiers without
    /// directly coupling to the BattleSystem singleton.
    /// </summary>
    public static IBattleWeatherProvider WeatherProvider { get; set; }

    int GetStat(Stat stat){
        int statVal = Stats[stat];

        int boost = StatBoosts[stat];
        var boostVal = new float[]{ 1f, 1.5f, 2f, 2.5f, 3f, 3.5f, 4f};
        if(boost >= 0){
            statVal = Mathf.FloorToInt(statVal * boostVal[boost]);
        } else {
            statVal = Mathf.FloorToInt(statVal / boostVal[-boost]);
        }

        var weather = WeatherProvider?.CurrentWeather;
        if (weather != null){
            statVal = Mathf.FloorToInt(Ability?.OnModifyStatInWeather?.Invoke(statVal, this, weather) ?? statVal);
        }

        statVal = ApplyPowerMechanicStatModifiers(stat, statVal);
        return ApplyTimedRecoveryStatModifiers(stat, statVal);
    }

    public Move GetRandomMove(PokemonVitalProfileDefinition fallbackVitalProfile = null){
        var movesWithPP = Moves.Where(move => CanUseMove(move, fallbackVitalProfile)).ToList();
        if(movesWithPP.Count == 0){
            return null;
        }
        
        int r = UnityEngine.Random.Range( 0, movesWithPP.Count);
        return movesWithPP[r];
    }

    public bool CanUseMove(Move move, PokemonVitalProfileDefinition fallbackVitalProfile = null){
        if(move == null || move.PP <= 0){
            return false;
        }

        if(LockedMove != null && move != LockedMove){
            return false;
        }

        if(EncoreMove != null && move != EncoreMove){
            return false;
        }

        if(DisabledMove != null && move == DisabledMove){
            return false;
        }

        if(TauntTurns > 0 && move.Base.Category == MoveCategory.Status){
            return false;
        }

        return CanPayMoveVitalCost(move, out _, fallbackVitalProfile);
    }

    public string GetMoveRestrictionMessage(Move move, PokemonVitalProfileDefinition fallbackVitalProfile = null){
        if(move == null){
            return $"{NickName} has no move to use!";
        }

        if(move.PP <= 0){
            return $"{move.Base.Name} has no PP left!";
        }

        if(LockedMove != null && move != LockedMove){
            return $"{NickName} can only use {LockedMove.Base.Name}!";
        }

        if(EncoreMove != null && move != EncoreMove){
            return $"{NickName} got an encore and can only use {EncoreMove.Base.Name}!";
        }

        if(EncoreMove != null && move == EncoreMove && move.PP <= 0){
            return $"{EncoreMove.Base.Name} has no PP left!";
        }

        if(DisabledMove != null && move == DisabledMove){
            return $"{move.Base.Name} is disabled!";
        }

        if(TauntTurns > 0 && move.Base.Category == MoveCategory.Status){
            return $"{NickName} can't use {move.Base.Name} after the taunt!";
        }

        if(!CanPayMoveVitalCost(move, out var vitalFailureMessage, fallbackVitalProfile)) {
            return vitalFailureMessage;
        }

        return null;
    }

    public void LockMoveIfNeeded(Move move){
        if(move == null || move.Base == GlobalSettings.i.BackUpMove || LockedMove != null){
            return;
        }

        if(HeldItem is BattleHeldItem battleHeldItem && battleHeldItem.ShouldLockMoveOnUse){
            LockedMove = move;
        }
    }

    public void ClearLockedMove(){
        LockedMove = null;
    }

    public void ApplyTaunt(int turns){
        TauntTurns = Mathf.Max(TauntTurns, turns);
        AddStatusEvent($"{NickName} fell for the taunt!");
    }

    public bool ApplyDisable(int turns){
        if(CurrentMove == null || CurrentMove.Base == GlobalSettings.i.BackUpMove){
            AddStatusEvent($"But it failed!");
            return false;
        }

        DisabledMove = CurrentMove;
        DisableTurns = Mathf.Max(DisableTurns, turns);
        AddStatusEvent($"{DisabledMove.Base.Name} was disabled!");
        return true;
    }

    public bool ApplyEncore(int turns){
        if(CurrentMove == null || CurrentMove.Base == GlobalSettings.i.BackUpMove){
            AddStatusEvent($"But it failed!");
            return false;
        }

        EncoreMove = CurrentMove;
        EncoreTurns = Mathf.Max(EncoreTurns, turns);
        AddStatusEvent($"{NickName} received an encore!");
        return true;
    }

    public void ClearMoveRestrictions(){
        ClearLockedMove();
        DisabledMove = null;
        EncoreMove = null;
        DisableTurns = 0;
        EncoreTurns = 0;
        TauntTurns = 0;
    }

    public void ApplyPowerMechanicEffect(BattlePowerMechanicRuntimeEffect effect){
        if(effect == null){
            return;
        }

        if(ActivePowerMechanicEffects == null){
            ActivePowerMechanicEffects = new List<BattlePowerMechanicRuntimeEffect>();
        }

        ActivePowerMechanicEffects.RemoveAll(active => active != null && active.mechanicId == effect.mechanicId);
        ActivePowerMechanicEffects.Add(effect);

        if(effect.temporaryPokemonBase != null){
            ApplyTemporaryBattleBase(effect.temporaryPokemonBase, effect.preserveHpRatioOnFormChange);
        }

        AddStatusEvent($"{NickName} is empowered by {effect.mechanicName}!");
    }

    public void ApplyTemporaryBattleBase(PokemonBase temporaryBase, bool preserveHpRatio = true){
        if(temporaryBase == null){
            return;
        }

        battleBaseOverride = temporaryBase;
        RecalculateStatsAfterPowerMechanicChange(preserveHpRatio);
    }

    public void ClearTemporaryBattleBase(bool preserveHpRatio = true){
        if(battleBaseOverride == null){
            return;
        }

        battleBaseOverride = null;
        RecalculateStatsAfterPowerMechanicChange(preserveHpRatio);
    }

    public void ClearPowerMechanicEffects(){
        if(ActivePowerMechanicEffects == null || ActivePowerMechanicEffects.Count == 0){
            ClearTemporaryBattleBase();
            return;
        }

        ActivePowerMechanicEffects.Clear();
        ClearTemporaryBattleBase();
    }

    public void ClearPowerMechanicEffectsOnSwitch(){
        if(ActivePowerMechanicEffects == null || ActivePowerMechanicEffects.Count == 0){
            return;
        }

        bool removedAny = ActivePowerMechanicEffects.RemoveAll(effect => effect != null && effect.endsOnSwitch) > 0;
        if(removedAny){
            RefreshTemporaryBattleBaseFromActiveEffects();
        }
    }

    void TickPowerMechanicEffects(){
        if(ActivePowerMechanicEffects == null || ActivePowerMechanicEffects.Count == 0){
            return;
        }

        bool expiredAny = false;
        for(int i = ActivePowerMechanicEffects.Count - 1; i >= 0; i--){
            var effect = ActivePowerMechanicEffects[i];
            if(effect == null){
                ActivePowerMechanicEffects.RemoveAt(i);
                expiredAny = true;
                continue;
            }

            if(effect.TickTurn()){
                AddStatusEvent($"{effect.mechanicName} wore off!");
                ActivePowerMechanicEffects.RemoveAt(i);
                expiredAny = true;
            }
        }

        if(expiredAny){
            RefreshTemporaryBattleBaseFromActiveEffects();
        }
    }

    void RefreshTemporaryBattleBaseFromActiveEffects(){
        var formEffect = ActivePowerMechanicEffects != null
            ? ActivePowerMechanicEffects.LastOrDefault(effect => effect != null && effect.temporaryPokemonBase != null)
            : null;

        if(formEffect != null){
            ApplyTemporaryBattleBase(formEffect.temporaryPokemonBase, formEffect.preserveHpRatioOnFormChange);
        } else {
            ClearTemporaryBattleBase();
        }
    }

    void RecalculateStatsAfterPowerMechanicChange(bool preserveHpRatio){
        float hpRatio = MaxHp > 0 ? (float)HP / MaxHp : 1f;
        CalculateStats();
        if(preserveHpRatio){
            HP = Mathf.Clamp(Mathf.Max(1, Mathf.RoundToInt(MaxHp * hpRatio)), 0, MaxHp);
        } else {
            HP = Mathf.Clamp(HP, 0, MaxHp);
        }
        OnHpChanged?.Invoke();
    }

    int ApplyPowerMechanicStatModifiers(Stat stat, int value){
        if(ActivePowerMechanicEffects == null || ActivePowerMechanicEffects.Count == 0){
            return value;
        }

        int modified = value;
        foreach(var effect in ActivePowerMechanicEffects){
            if(effect?.statModifiers == null){
                continue;
            }

            foreach(var modifier in effect.statModifiers){
                if(modifier != null){
                    modified = modifier.Apply(stat, modified);
                }
            }
        }

        return modified;
    }

    float ApplyPowerMechanicMovePower(float value){
        if(ActivePowerMechanicEffects == null || ActivePowerMechanicEffects.Count == 0){
            return value;
        }

        float modified = value;
        foreach(var effect in ActivePowerMechanicEffects){
            if(effect != null){
                modified *= Mathf.Max(0f, effect.movePowerMultiplier);
            }
        }

        return modified;
    }

    float ApplyPowerMechanicAccuracy(float value){
        if(ActivePowerMechanicEffects == null || ActivePowerMechanicEffects.Count == 0){
            return value;
        }

        float modified = value;
        foreach(var effect in ActivePowerMechanicEffects){
            if(effect != null){
                modified *= Mathf.Max(0f, effect.accuracyMultiplier);
            }
        }

        return modified;
    }

    int GetPowerMechanicCritStageBonus(){
        if(ActivePowerMechanicEffects == null || ActivePowerMechanicEffects.Count == 0){
            return 0;
        }

        return ActivePowerMechanicEffects.Where(effect => effect != null).Sum(effect => effect.critStageBonus);
    }

    public List<LearnableMove> GetLearnableMovesAtCurrLevel() {
        return Base.LearnableMoves.Where(x => x.Level == level).ToList();
    }

    public void InitializeGrowth(PokemonGrowthProfileDefinition profile, bool preserveExisting = false) {
        if(profile == null) {
            return;
        }

        if(preserveExisting && GrowthState != null && GrowthState.initialized) {
            return;
        }

        GrowthState = profile.CreateState(this);
        CalculateStats();
        OnVitalsChanged?.Invoke();
    }

    public int GainGrowthTraining(Stat stat, int points, PokemonGrowthProfileDefinition profile = null, PokemonGrowthSource source = PokemonGrowthSource.Training, string sourceId = null, string sourceName = null) {
        if(points <= 0) {
            return 0;
        }

        EnsureGrowthState();
        int applied = GrowthState.AddTraining(stat, points, profile, source, sourceId, sourceName);
        if(applied > 0) {
            CalculateStats();
            OnVitalsChanged?.Invoke();
        }

        return applied;
    }

    public bool ApplyPassiveTrait(PokemonPassiveTraitDefinition trait, string sourceId = null) {
        if(trait == null) {
            return false;
        }

        EnsureGrowthState();
        bool alreadyHadTrait = GrowthState.HasTrait(trait.Id);
        trait.ApplyTo(GrowthState, sourceId);
        CalculateStats();
        return !alreadyHadTrait;
    }

    public bool HasPassiveTrait(string traitId) {
        return GrowthState != null && GrowthState.HasTrait(traitId);
    }

    public int GetAbilityPoints(PokemonAbilityTreeDefinition tree) {
        return tree != null && AbilityTreeState != null ? AbilityTreeState.GetPoints(tree.Id) : 0;
    }

    public int GainAbilityPoints(PokemonAbilityTreeDefinition tree, int points, PokemonAbilityPointSource source = PokemonAbilityPointSource.Training, string sourceId = null, string sourceName = null) {
        if(tree == null || points == 0) {
            return 0;
        }

        EnsureAbilityTreeState();
        return AbilityTreeState.AddPoints(tree.Id, tree.DisplayName, points, source, sourceId, sourceName);
    }

    public bool HasUnlockedAbilityNode(PokemonAbilityTreeDefinition tree, string nodeId) {
        return tree != null && AbilityTreeState != null && AbilityTreeState.HasUnlocked(tree.Id, nodeId);
    }

    public bool TryUnlockAbilityNode(PokemonAbilityTreeDefinition tree, string nodeId, PlayerController player, PokemonAbilityPointSource source, string sourceId, string sourceName, out string failureMessage) {
        EnsureAbilityTreeState();
        if(tree == null) {
            failureMessage = "Ability tree is missing.";
            return false;
        }

        if(!tree.CanUseTree(this, player, out failureMessage)) {
            return false;
        }

        var node = tree.GetNode(nodeId);
        if(node == null) {
            failureMessage = $"Ability node '{nodeId}' could not be found.";
            return false;
        }

        if(!node.CanUnlock(this, player, tree, out failureMessage)) {
            return false;
        }

        if(!AbilityTreeState.Unlock(tree, node, source, sourceId, sourceName)) {
            failureMessage = "Ability node could not be unlocked.";
            return false;
        }

        node.ApplyEffects(this, tree);
        CalculateStats();
        OnVitalsChanged?.Invoke();
        failureMessage = null;
        return true;
    }

    public void AddAbilityTreeStatModifier(PokemonAbilityTreeEffect effect, string sourceId) {
        if(effect == null || (effect.FlatStatBonus == 0 && Mathf.Approximately(effect.StatMultiplierBonus, 0f))) {
            return;
        }

        EnsureGrowthState();
        GrowthState.statModifiers ??= new List<PokemonGrowthStatModifier>();
        GrowthState.statModifiers.Add(new PokemonGrowthStatModifier {
            stat = effect.Stat,
            flatBonus = effect.FlatStatBonus,
            multiplierBonus = effect.StatMultiplierBonus,
            sourceId = sourceId,
            sourceName = effect.DisplayName
        });
    }

    public PokemonSaveData GetSaveData(){
        var saveData = new PokemonSaveData(){
            instanceId = InstanceId,
            name = _base.Name,
            Hp = HP,
            level = level,
            xp = Exp,
            pokeball = Pokeball?.Name,
            statusId = Status?.Id,
            moves = Moves.Select(x => x.GetSaveData()).ToList(),
            gender = gender,
            StatEffortValues = StatEffortValues,
            abilityId = (Ability != null) ? Ability.Id : AbilityID.None,
            isShiny = IsShiny,
            friendship = Friendship,
            natureId = (NatureID)System.Enum.Parse(typeof(NatureID), Nature.Name),
            personalityId = PersonalityID,
            nickname = Nickname,
            heldItem = HeldItem?.Name,
            moodValues = MoodValues,
            careNeedValues = CareNeedValues,
            careRecords = CareRecords,
            vitalState = VitalState?.ToSaveData(),
            growthState = GrowthState,
            evolutionState = EvolutionState,
            techniqueMemory = TechniqueMemory,
            abilityTreeState = AbilityTreeState
        };
        return saveData;
    }

    public float GetNormalizedExp(){
        int currLevelExp = Base.GetExpForLevel(Level);
        int nextLevelExp = Base.GetExpForLevel(Level + 1);

        float normilizedExp =  (float)( Exp - currLevelExp ) / (float)( nextLevelExp - currLevelExp);
        return Mathf.Clamp01(normilizedExp);
    }

    public bool HasMove(MoveBase moveToCheck){
        return Moves.Count( m => m.Base == moveToCheck) > 0;
    }

    public bool HasType(PokemonType typeToCheck){
        return Base.Type1 == typeToCheck || Base.Type2 == typeToCheck;
    }

    public void IncreaseFriendship(int amount){
        Friendship = Mathf.Min(255, Friendship + Personality.ModifyFriendshipGain(amount));
    }

    public int GetMoodValue(PokemonMoodDefinition mood){
        if(mood == null){
            return 0;
        }

        var moodValue = MoodValues.FirstOrDefault(x => x.moodId == mood.Id);
        return moodValue != null ? Mathf.Clamp(moodValue.value, mood.MinValue, mood.MaxValue) : mood.DefaultValue;
    }

    public void ChangeMood(PokemonMoodDefinition mood, int amount){
        if(mood == null || amount == 0){
            return;
        }

        var moodValue = MoodValues.FirstOrDefault(x => x.moodId == mood.Id);
        if(moodValue == null){
            moodValue = new PokemonMoodValue() {
                moodId = mood.Id,
                value = mood.DefaultValue
            };
            MoodValues.Add(moodValue);
        }

        moodValue.value = Mathf.Clamp(moodValue.value + amount, mood.MinValue, mood.MaxValue);
        OnMoodChanged?.Invoke();
    }

    public int GetCareNeedValue(PokemonCareNeedDefinition need){
        if(need == null){
            return 0;
        }

        var value = CareNeedValues.FirstOrDefault(x => x.needId == need.Id);
        return value != null ? Mathf.Clamp(value.value, need.MinValue, need.MaxValue) : need.DefaultValue;
    }

    public bool HasCareNeedValue(PokemonCareNeedDefinition need){
        if(need == null){
            return false;
        }

        return CareNeedValues.Any(x => x.needId == need.Id);
    }

    public void SetCareNeed(PokemonCareNeedDefinition need, int value){
        if(need == null){
            return;
        }

        var needValue = CareNeedValues.FirstOrDefault(x => x.needId == need.Id);
        if(needValue == null){
            needValue = new PokemonCareNeedValue() {
                needId = need.Id,
                value = need.DefaultValue
            };
            CareNeedValues.Add(needValue);
        }

        needValue.value = Mathf.Clamp(value, need.MinValue, need.MaxValue);
        OnCareChanged?.Invoke();
    }

    public void ChangeCareNeed(PokemonCareNeedDefinition need, int amount){
        if(need == null || amount == 0){
            return;
        }

        var value = CareNeedValues.FirstOrDefault(x => x.needId == need.Id);
        if(value == null){
            value = new PokemonCareNeedValue() {
                needId = need.Id,
                value = need.DefaultValue
            };
            CareNeedValues.Add(value);
        }

        value.value = Mathf.Clamp(value.value + amount, need.MinValue, need.MaxValue);
        OnCareChanged?.Invoke();
    }

    public void RecordCareAction(PokemonCareActionDefinition action, PokemonCareCategory category, string sourceId = null){
        if(action == null){
            return;
        }

        CareRecords.Add(new PokemonCareRecord {
            actionId = action.Id,
            actionName = action.DisplayName,
            category = category,
            sourceId = sourceId,
            day = GetCurrentDay(),
            absoluteHour = GetCurrentAbsoluteHour()
        });
        OnCareChanged?.Invoke();
    }

    public int GetCareActionCount(PokemonCareActionDefinition action = null){
        return CareRecords.Count(record => record != null && (action == null || record.actionId == action.Id));
    }

    public int GetCareCategoryCount(PokemonCareCategory category){
        return CareRecords.Count(record => record != null && record.category == category);
    }

    public int GetHoursSinceLastCare(PokemonCareActionDefinition action = null, PokemonCareCategory? category = null){
        var latest = CareRecords
            .Where(record => record != null)
            .Where(record => action == null || record.actionId == action.Id)
            .Where(record => !category.HasValue || record.category == category.Value)
            .OrderByDescending(record => record.absoluteHour)
            .FirstOrDefault();

        if(latest == null || latest.absoluteHour < 0){
            return -1;
        }

        return Mathf.Max(0, GetCurrentAbsoluteHour() - latest.absoluteHour);
    }

    int GetCurrentDay(){
        return TimeSystem.i != null ? Mathf.Max(1, TimeSystem.i.Day) : 1;
    }

    int GetCurrentAbsoluteHour(){
        return TimeSystem.i != null ? Mathf.Max(0, TimeSystem.i.Day * 24 + TimeSystem.i.Hour) : 0;
    }

    void EnsureInstanceId(){
        if(string.IsNullOrWhiteSpace(instanceId)){
            instanceId = System.Guid.NewGuid().ToString("N");
        }
    }

    public PokemonType GetMoveType(Move move, Pokemon defender = null){
        var moveType = move.Base.Type;
        if(Ability?.OnModifyMoveType != null){
            moveType = Ability.OnModifyMoveType(moveType, this, defender, move);
        }
        return moveType;
    }

    public void LearnMove(MoveBase moveToLearn){
        LearnMove(moveToLearn, PokemonTechniqueLearnSource.LevelUp, "learn-move", "Learn Move");
    }

    public bool LearnMove(MoveBase moveToLearn, PokemonTechniqueLearnSource source, string sourceId = null, string sourceName = null){
        if(moveToLearn == null) {
            return false;
        }

        RememberTechnique(moveToLearn, source, sourceId, sourceName);
        if(Moves.Count >= PokemonBase.MaxNumberOfMoves || HasMove(moveToLearn)) {
            SyncTechniqueMemoryWithActiveMoves();
            return false;
        }

        Moves.Add(new Move(moveToLearn));
        SyncTechniqueMemoryWithActiveMoves();
        return true;
    }

    public bool SetActiveMove(int index, MoveBase moveToSet, PokemonTechniqueLearnSource source = PokemonTechniqueLearnSource.Manual, string sourceId = null, string sourceName = null){
        if(moveToSet == null || index < 0 || index >= PokemonBase.MaxNumberOfMoves) {
            return false;
        }

        RememberTechnique(moveToSet, source, sourceId, sourceName);
        if(index < Moves.Count) {
            Moves[index] = new Move(moveToSet);
        } else if(Moves.Count < PokemonBase.MaxNumberOfMoves) {
            Moves.Add(new Move(moveToSet));
        } else {
            return false;
        }

        SyncTechniqueMemoryWithActiveMoves();
        return true;
    }

    public bool RememberTechnique(MoveBase move, PokemonTechniqueLearnSource source = PokemonTechniqueLearnSource.Unknown, string sourceId = null, string sourceName = null){
        if(move == null) {
            return false;
        }

        EnsureTechniqueMemory();
        return TechniqueMemory.Remember(move, source, sourceId, sourceName);
    }

    public bool HasKnownTechnique(MoveBase move){
        return move != null && TechniqueMemory != null && TechniqueMemory.HasMove(move);
    }

    public List<MoveBase> GetKnownTechniqueMoves(){
        EnsureTechniqueMemory();
        return TechniqueMemory.ResolveKnownMoves().ToList();
    }

    public void SyncTechniqueMemoryWithActiveMoves(){
        EnsureTechniqueMemory();
        TechniqueMemory.SyncActiveMoves(Moves);
    }

    public void ForgetActiveMove(int index){
        if(index < 0 || index >= Moves.Count) {
            return;
        }

        var move = Moves[index].Base;
        Moves.RemoveAt(index);
        TechniqueMemory?.MarkActive(move, false, PokemonTechniqueLearnSource.Manual, "forget-active", "Forget Active Move");
    }

    void EnsureTechniqueMemory(){
        if(TechniqueMemory == null) {
            TechniqueMemory = new PokemonTechniqueMemoryState();
        }
    }

    public void SetStatus(StatusConditionID conditionID, EffectSource source = EffectSource.Move) {
        if(Status != null) return;

        bool canSet = Ability?.OnTrySetStatus?.Invoke(conditionID, this, source) ?? true;
        if(!canSet) return;

        if (BattleSystem.i != null && BattleSystem.i.Field.Terrain != null){
            var terrain = BattleSystem.i.Field.Terrain;
            if (terrain.Id == TerrainID.Electric && conditionID == StatusConditionID.Sleep){
                AddStatusEvent($"{NickName} stayed awake because of the Electric Terrain!");
                return;
            }
            if (terrain.Id == TerrainID.Misty){
                AddStatusEvent($"{NickName} was protected by the Misty Terrain!");
                return;
            }
        }

        Status = StatusConditionsDB.Conditions[conditionID];
        Status?.OnStart?.Invoke(this);
        AddStatusEvent($"{Base.Name} {Status.StartMessage}");
        OnStatusChanged?.Invoke();
    }

    public void SetVolatileStatus(StatusConditionID conditionID, EffectSource source = EffectSource.Move) {
        if(VolatileStatus != null) return;

        bool canSet = Ability?.OnTrySetVolatileStatus?.Invoke(conditionID, this, source) ?? true;
        if(!canSet) return;

        VolatileStatus = StatusConditionsDB.Conditions[conditionID];
        VolatileStatus?.OnStart?.Invoke(this);
        AddStatusEvent($"{Base.Name} {VolatileStatus.StartMessage}");
    }

    public void CureStatus(){
        Status = null;
        OnStatusChanged?.Invoke();
    }
    
    public void CureVolatileStatus(){
        VolatileStatus = null;
    }

    public Evolution CheckForEvolution(){
        return Base.Evolutions.FirstOrDefault(e => e.RequiredLevel <= level && ((e.RequiredTime != GeneralDayPeriod.None) ? e.RequiredTime == TimeSystem.i.EvolutionTime : true));
    }

    public PokemonEvolutionDefinition CheckForEvolutionDefinition(PlayerController player = null, PokemonEvolutionTriggerKind trigger = PokemonEvolutionTriggerKind.LevelUp, ItemBase item = null, PokemonEvolutionContext context = null, bool includeDeferred = false){
        return PokemonEvolutionService.FindFirstRoute(this, player, trigger, item, context, includeDeferred);
    }
    
    public Evolution CheckForEvolution(ItemBase item){
        return Base.Evolutions.FirstOrDefault(e => e.RequiredItem == item && ((e.RequiredTime != GeneralDayPeriod.None) ? e.RequiredTime == TimeSystem.i.EvolutionTime : true));
    }

    public PokemonEvolutionDefinition CheckForEvolutionDefinition(ItemBase item, PlayerController player = null, PokemonEvolutionContext context = null, bool includeDeferred = false){
        return PokemonEvolutionService.FindFirstRoute(this, player, PokemonEvolutionTriggerKind.ItemUse, item, context, includeDeferred);
    }

    public void Evolve(Evolution evolution){
        _base = evolution.EvolvesInto;
        CalculateStats(); 
    }

    public void Evolve(PokemonEvolutionDefinition evolution, PokemonEvolutionTriggerKind trigger = PokemonEvolutionTriggerKind.Manual, string sourceId = null){
        if(evolution == null || evolution.EvolvesInto == null){
            return;
        }

        var oldBase = _base;
        _base = evolution.EvolvesInto;
        battleBaseOverride = null;
        EvolutionState ??= new PokemonEvolutionRuntimeState();
        EvolutionState.ClearDeferred(evolution.Id);
        EvolutionState.Record(evolution, oldBase, _base, trigger, sourceId);
        CalculateStats();
    }

    public void EvolveTo(PokemonBase evolvesInto, string sourceId = null){
        if(evolvesInto == null){
            return;
        }

        var oldBase = _base;
        _base = evolvesInto;
        battleBaseOverride = null;
        EvolutionState ??= new PokemonEvolutionRuntimeState();
        EvolutionState.Record(null, oldBase, _base, PokemonEvolutionTriggerKind.Manual, sourceId);
        CalculateStats();
    }

    public void DeferEvolution(PokemonEvolutionDefinition evolution){
        if(evolution == null || !evolution.AllowDeferral){
            return;
        }

        EvolutionState ??= new PokemonEvolutionRuntimeState();
        EvolutionState.Defer(evolution.Id);
    }

    public List<MoveBase> GetLearnableMoves() {
        return Base.LearnableMoves.Where(lm => lm.Level <= level && !HasKnownTechnique(lm.Base)).Select(lm => lm.Base).ToList();
    }

    public void ApplyBoosts(List<StatBoosts> statBoosts, Pokemon source){
        var statsDicc = statBoosts.ToDictionary(x => x.stat, x => x.boost);
        Ability?.OnBoost?.Invoke(statsDicc, source, this);

        foreach(var kvp in statsDicc){
            var stat = kvp.Key;
            var boost = kvp.Value;
            bool changeIsPositive = (boost > 0)? true : false;

            if(changeIsPositive && StatBoosts[stat] == 6 || !changeIsPositive && StatBoosts[stat] == -6){
                string riseOrFall = changeIsPositive ? "rise" : "fall";
                string highOrLow = changeIsPositive ? "higher" : "lower";
                string maxOrMin = changeIsPositive ? "maximum" : "minimum";
                string risenOrFell = changeIsPositive ? "risen" : "fell";

                AddStatusEvent(StatusEventType.StatBoost, $"{Base.Name}'s {stat} cannot go any {highOrLow}, it has already {risenOrFell} to the {maxOrMin}!");
            } else {
                StatBoosts[stat] = Mathf.Clamp(StatBoosts[stat] += boost,-6, 6);
                string riseOrFall = changeIsPositive ? "rose" : "fell";
                string bigChance = Mathf.Abs(boost) >= 3 ? "severely" : Mathf.Abs(boost) == 2 ? "harshly" : "";
                AddStatusEvent(StatusEventType.StatBoost, $"{Base.Name}'s {stat} {bigChance} {riseOrFall}!");
            }
        }
    }

    public DamageDetails TakeDamage(Move move, Pokemon attacker, float weatherModifier = 1f, PokemonVitalProfileDefinition fallbackVitalProfile = null){
        float critical = 1f;

        float power = move.Base.Power;
        if(move.Base.MovePowerBasedOn == PowerBasedOn.TargetWeight){
            power = GetPowerFromBaseWeight();
        } else if(move.Base.MovePowerBasedOn == PowerBasedOn.WeightDifference){
            power = GetPowerFromWeightDifference(attacker);
        } else if(move.Base.MovePowerBasedOn == PowerBasedOn.SpeedRatio){
            power = GetPowerFromSpeedRatio(attacker);
        } else if(move.Base.MovePowerBasedOn == PowerBasedOn.FuryCutter){
            power = GetPowerFromFuryCutter(attacker);
        }

        var moveType = attacker.GetMoveType(move, this);

        if (BattleSystem.i != null && BattleSystem.i.Field.Terrain != null){
            var terrain = BattleSystem.i.Field.Terrain;
            if (terrain.Id == TerrainID.Electric && moveType == PokemonType.Electric) power *= 1.3f;
            if (terrain.Id == TerrainID.Grassy && moveType == PokemonType.Grass) power *= 1.3f;
            if (terrain.Id == TerrainID.Psychic && moveType == PokemonType.Psychic) power *= 1.3f;
            if (terrain.Id == TerrainID.Misty && moveType == PokemonType.Dragon) power *= 0.5f;
        }
        if (move.Base.OneHitKoMoveEffect.isOneHitKnockOut){
            int hpBeforeOneHit = HP;
            int oneHitDamage = HP;
            if(HeldItem is BattleHeldItem oneHitDefensiveHeldItem) {
                oneHitDamage = oneHitDefensiveHeldItem.ModifyIncomingDamage(oneHitDamage, this);
            }
            DecreaseHP(oneHitDamage, true);
            int oneHitOverkill = Mathf.Max(0, oneHitDamage - hpBeforeOneHit);
            int oneHitCoreDamage = ApplyMoveCoreHealthDamage(move.Base, oneHitDamage, oneHitOverkill, fallbackVitalProfile);
            return new DamageDetails() { TypeEffectiveness = 1f, Critical = 1f, Fainted = false, DamageDealt = oneHitDamage, OverkillDamage = oneHitOverkill, CoreHealthDamage = oneHitCoreDamage };
        }

        if(!(move.Base.CritBehaviour == CritBehaviour.NeverCrit)){
            int critStage = CritStage + GetPowerMechanicCritStageBonus();
            if(move.Base.CritBehaviour == CritBehaviour.HighCritRatio) critStage += 1;
            if(move.Base.CritBehaviour == CritBehaviour.AlwaysCrit) critStage = 3;

            // Add other boosts (Focus Energy, items etc) later

            float[] chances = new float[]{(4.146f), (12.5f),(50f), 100f};
            if(UnityEngine.Random.value * 100f <= chances[Mathf.Clamp(critStage, 0, 3)]){
                critical = (Ability?.Name == "Sniper") ? 2.25f : 1.5f;
            }
        }


        float typeEffectiveness = TypeChart.GetEffectiveness(moveType, this.Base.Type1) * TypeChart.GetEffectiveness(moveType, this.Base.Type2);

        if (moveType == PokemonType.Ground && Ability?.Name == "Levitate"){
            typeEffectiveness = 0f;
        }

        var damageDetails = new DamageDetails(){
            Critical = critical,
            TypeEffectiveness = typeEffectiveness,
            Fainted = false,

            DamageDealt = 0
        };

        // float attack = (move.Base.Category == MoveCategory.Special)? attacker.SpAttack : attacker.Attack;
        // float defense = (move.Base.Category == MoveCategory.Special)? SpDefense : Defense;

        float attack, defense;

        if(move.Base.Category == MoveCategory.Special){
            attack = attacker.SpAttack;
            defense = SpDefense;

            attack = attacker.ModifySpAttack(attack, this, move);
            defense = ModifySpDefense(defense, attacker, move);

        } else {
            attack = attacker.Attack;
            defense = Defense;

            attack = attacker.ModifyAttack(attack, this, move);
            defense = ModifyDefense(defense, attacker, move);
        }

        float basePower = attacker.ModifyMoveBasePower(power, this, move);

        float screenModifier = 1f;
        if(BattleSystem.i != null){
            var field = BattleSystem.i.Field;
            bool defenderIsPlayer = (BattleSystem.i.PlayerUnits.FirstOrDefault(u => u.Pokemon == this) != null);
            if(move.Base.Category == MoveCategory.Physical){
                int reflect = defenderIsPlayer ? field.PlayerReflect : field.EnemyReflect;
                int veil = defenderIsPlayer ? field.PlayerAuroraVeil : field.EnemyAuroraVeil;
                if(reflect > 0 || veil > 0) screenModifier = 0.5f;
            } else if(move.Base.Category == MoveCategory.Special){
                int lightScreen = defenderIsPlayer ? field.PlayerLightScreen : field.EnemyLightScreen;
                int veil = defenderIsPlayer ? field.PlayerAuroraVeil : field.EnemyAuroraVeil;
                if(lightScreen > 0 || veil > 0) screenModifier = 0.5f;
            }
        }

        float stab = attacker.HasType(moveType) ? 1.5f : 1f;
        float modifiers = UnityEngine.Random.Range( 0.85f, 1f) * typeEffectiveness * critical * weatherModifier * screenModifier * stab;
        float a = ( 2 * attacker.Level + 10) / 250f;
        float d = a * basePower * ((float)attack / defense) + 2;
        int damage = Mathf.FloorToInt(d * modifiers);

        if(HeldItem is BattleHeldItem defensiveHeldItem) {
            damage = defensiveHeldItem.ModifyIncomingDamage(damage, this);
        }

        int hpBeforeDamage = HP;
        DecreaseHP(damage, true);
        int overkillDamage = Mathf.Max(0, damage - hpBeforeDamage);
        int coreHealthDamage = ApplyMoveCoreHealthDamage(move.Base, damage, overkillDamage, fallbackVitalProfile);
        if(damage > 0) {
            Ability?.OnDamagingHit?.Invoke(damage, attacker, this, move);

            if(move.Base.HasFlag(MoveFlag.Contact) && attacker.HP > 0) {
                Ability?.OnAfterContact?.Invoke(attacker, this, move);

                if(HeldItem is BattleHeldItem contactHeldItem) {
                    contactHeldItem.OnAfterContact(attacker, this, move);
                }
            }

            if(attacker.HP > 0 && attacker.HeldItem is BattleHeldItem attackingHeldItem) {
                attackingHeldItem.OnAfterDamagingHit(damage, attacker);
            }
        }

        damageDetails.DamageDealt = damage;
        damageDetails.OverkillDamage = overkillDamage;
        damageDetails.CoreHealthDamage = coreHealthDamage;

        return damageDetails;
    }

    int ApplyMoveCoreHealthDamage(MoveBase move, int damage, int overkillDamage, PokemonVitalProfileDefinition fallbackVitalProfile = null) {
        if(move == null || damage <= 0 || !move.CanDamageCoreHealth) {
            return 0;
        }

        var resolvedProfile = move.ResolveVitalProfile(fallbackVitalProfile);
        int thresholdDamage = ApplyCoreHealthDamageFromBattleDamage(damage, overkillDamage, resolvedProfile, move.ForceCoreHealthDamage);
        int extraFlat = Mathf.Max(0, move.FlatCoreHealthDamage);
        int extraPercent = move.ExtraCoreHealthDamagePercent > 0f ? Mathf.RoundToInt(damage * move.ExtraCoreHealthDamagePercent) : 0;
        int extraDamage = extraFlat + extraPercent;
        if(extraDamage > 0) {
            extraDamage = -ChangeVitalResource(PokemonVitalResourceKind.CoreHealth, -extraDamage, resolvedProfile);
        }

        int totalCoreDamage = thresholdDamage + extraDamage;
        if(totalCoreDamage > 0) {
            AddStatusEvent(StatusEventType.Damage, $"{NickName}'s core health was damaged!");
        }

        return totalCoreDamage;
    }

    public float ModifyAttack(float attack, Pokemon defender, Move move){
        if(Ability?.OnModifyAttack != null){
            return Ability.OnModifyAttack(attack, this, defender, move);
        }
        
        return attack;
    }
    
    public float ModifySpAttack(float spAttack, Pokemon defender, Move move){
        if(Ability?.OnModifySpAttack != null){
            return Ability.OnModifySpAttack(spAttack, this, defender, move);
        }

        return spAttack;
    }

    public float ModifyDefense(float defense, Pokemon attacker, Move move){
        if(Ability?.OnModifyDefense != null){
            return Ability.OnModifyDefense(defense, attacker, this, move);
        }

        return defense;
    }

    public float ModifySpDefense(float spDefense, Pokemon attacker, Move move){
        if(Ability?.OnModifySpDefense != null){
            return Ability.OnModifySpDefense(spDefense, attacker, this, move);
        }

        return spDefense;
    }

    public float ModifySpeed(float speed, Pokemon defender, Move move){
        if(Ability?.OnModifySpeed != null){
            speed = Ability.OnModifySpeed(speed, this, defender, move);
        }

        if(HeldItem is BattleHeldItem battleHeldItem) {
            speed = battleHeldItem.ModifySpeed(speed, this);
        }

        return speed;
    }

    public float ModifyAccuracy(float accuracy, Pokemon defender, Move move){
        if(Ability?.OnModifyAccuracy != null){
            accuracy = Ability.OnModifyAccuracy(accuracy, this, defender, move);
        }

        return ApplyPowerMechanicAccuracy(accuracy);
    }

    public float ModifyMoveBasePower(float basePower, Pokemon defender, Move move) {
        if(Ability?.OnModifyMoveBasePower != null) {
            basePower = Ability.OnModifyMoveBasePower(basePower, this, defender, move);
        }

        if(HeldItem is BattleHeldItem battleHeldItem) {
            basePower = battleHeldItem.ModifyMoveBasePower(basePower, this, defender, move);
        }

        return ApplyPowerMechanicMovePower(basePower);
    }

    public void TakeRecoilDamage(int damage){
        if(damage < 1){
            damage = 1;
        }
        DecreaseHP(damage, true);
        AddStatusEvent($"{Base.Name} took {damage} recoil damage!");
    }

    public void OnBattleOver(){
        ClearPowerMechanicEffects();
        VolatileStatus = null;
        ClearMoveRestrictions();
        CritStage = 0;
        ResetStatBoosts();
        TickTimedRecoveryBattleDurations();
    }

    public void AddStatusEvent(StatusEventType type, string message){
        StatusChanges.Enqueue(new StatusEvent(type, message));
    }
    
    public void AddStatusEvent(string message){
        StatusChanges.Enqueue(new StatusEvent(StatusEventType.Text, message));
    }

    public void Heal(){
        RestoreVitalsToFull();
        HP = MaxHp;
        Moves.ForEach(m => m.PP = m.Base.PP);
        
        OnHpChanged?.Invoke();        
        CureStatus();
    }

    public void EnsureVitalStateInitialized(PokemonVitalProfileDefinition profile = null) {
        if(VitalState == null) {
            VitalState = new PokemonVitalState();
        }

        if(!VitalState.initialized) {
            VitalState.Initialize(this, profile);
        } else {
            VitalState.Clamp(this, profile);
        }
    }

    public int GetVitalMax(PokemonVitalResourceKind resource, PokemonVitalProfileDefinition profile = null) {
        return resource switch {
            PokemonVitalResourceKind.CoreHealth => PokemonVitalDefaults.GetMaxCoreHealth(this, profile),
            PokemonVitalResourceKind.CorePhysicalStamina => PokemonVitalDefaults.GetMaxCorePhysicalStamina(this, profile),
            PokemonVitalResourceKind.CoreElementalStamina => PokemonVitalDefaults.GetMaxCoreElementalStamina(this, profile),
            PokemonVitalResourceKind.BattlePhysicalStamina => PokemonVitalDefaults.GetMaxBattlePhysicalStamina(this, profile),
            PokemonVitalResourceKind.BattleElementalStamina => PokemonVitalDefaults.GetMaxBattleElementalStamina(this, profile),
            _ => 1
        };
    }

    public int GetVitalValue(PokemonVitalResourceKind resource, PokemonVitalProfileDefinition profile = null) {
        EnsureVitalStateInitialized(profile);
        return resource switch {
            PokemonVitalResourceKind.CoreHealth => VitalState.coreHealth,
            PokemonVitalResourceKind.CorePhysicalStamina => VitalState.corePhysicalStamina,
            PokemonVitalResourceKind.CoreElementalStamina => VitalState.coreElementalStamina,
            PokemonVitalResourceKind.BattlePhysicalStamina => VitalState.battlePhysicalStamina,
            PokemonVitalResourceKind.BattleElementalStamina => VitalState.battleElementalStamina,
            _ => 0
        };
    }

    public float GetVitalNormalized(PokemonVitalResourceKind resource, PokemonVitalProfileDefinition profile = null) {
        int max = GetVitalMax(resource, profile);
        return max <= 0 ? 0f : Mathf.Clamp01(GetVitalValue(resource, profile) / (float)max);
    }

    public int ChangeVitalResource(PokemonVitalResourceKind resource, int amount, PokemonVitalProfileDefinition profile = null) {
        EnsureVitalStateInitialized(profile);
        if(amount > 0) {
            amount = ApplyVitalRecoveryModifiers(amount);
        }

        int before = GetVitalValue(resource, profile);
        int max = GetVitalMax(resource, profile);
        int after = Mathf.Clamp(before + amount, 0, max);
        int delta = after - before;
        if(delta == 0) {
            return 0;
        }

        switch(resource) {
            case PokemonVitalResourceKind.CoreHealth:
                VitalState.coreHealth = after;
                ClampHPToCoreHealthCap(profile);
                break;
            case PokemonVitalResourceKind.CorePhysicalStamina:
                VitalState.corePhysicalStamina = after;
                break;
            case PokemonVitalResourceKind.CoreElementalStamina:
                VitalState.coreElementalStamina = after;
                break;
            case PokemonVitalResourceKind.BattlePhysicalStamina:
                VitalState.battlePhysicalStamina = after;
                break;
            case PokemonVitalResourceKind.BattleElementalStamina:
                VitalState.battleElementalStamina = after;
                break;
        }

        OnVitalsChanged?.Invoke();
        return delta;
    }

    public void RestoreVitalsToFull(PokemonVitalProfileDefinition profile = null) {
        EnsureVitalStateInitialized(profile);
        VitalState.Initialize(this, profile);
        OnVitalsChanged?.Invoke();
    }

    public void RestoreCoreVitalsToFull(PokemonVitalProfileDefinition profile = null) {
        EnsureVitalStateInitialized(profile);
        VitalState.coreHealth = PokemonVitalDefaults.GetMaxCoreHealth(this, profile);
        VitalState.corePhysicalStamina = PokemonVitalDefaults.GetMaxCorePhysicalStamina(this, profile);
        VitalState.coreElementalStamina = PokemonVitalDefaults.GetMaxCoreElementalStamina(this, profile);
        VitalState.Clamp(this, profile);
        OnVitalsChanged?.Invoke();
    }

    public void RestoreBattleVitalsToFull(PokemonVitalProfileDefinition profile = null) {
        EnsureVitalStateInitialized(profile);
        VitalState.battlePhysicalStamina = PokemonVitalDefaults.GetMaxBattlePhysicalStamina(this, profile);
        VitalState.battleElementalStamina = PokemonVitalDefaults.GetMaxBattleElementalStamina(this, profile);
        VitalState.Clamp(this, profile);
        OnVitalsChanged?.Invoke();
    }

    public void PrepareBattleVitals(PokemonVitalProfileDefinition profile = null, bool spendCoreStamina = true) {
        EnsureVitalStateInitialized(profile);
        RefillBattleStaminaFromCore(
            PokemonVitalResourceKind.BattlePhysicalStamina,
            PokemonVitalResourceKind.CorePhysicalStamina,
            PokemonVitalDefaults.GetMaxBattlePhysicalStamina(this, profile),
            profile != null ? profile.CorePhysicalCostPerBattlePhysical : 0.25f,
            spendCoreStamina,
            profile);
        RefillBattleStaminaFromCore(
            PokemonVitalResourceKind.BattleElementalStamina,
            PokemonVitalResourceKind.CoreElementalStamina,
            PokemonVitalDefaults.GetMaxBattleElementalStamina(this, profile),
            profile != null ? profile.CoreElementalCostPerBattleElemental : 0.25f,
            spendCoreStamina,
            profile);
    }

    void RefillBattleStaminaFromCore(PokemonVitalResourceKind battleResource, PokemonVitalResourceKind coreResource, int battleMax, float coreCostPerPoint, bool spendCoreStamina, PokemonVitalProfileDefinition profile) {
        int current = GetVitalValue(battleResource, profile);
        int missing = Mathf.Max(0, battleMax - current);
        if(missing <= 0) {
            return;
        }

        int refill = missing;
        if(spendCoreStamina && coreCostPerPoint > 0f) {
            int coreAvailable = GetVitalValue(coreResource, profile);
            refill = Mathf.Min(refill, Mathf.FloorToInt(coreAvailable / coreCostPerPoint));
            int coreCost = Mathf.CeilToInt(refill * coreCostPerPoint);
            ChangeVitalResource(coreResource, -coreCost, profile);
        }

        ChangeVitalResource(battleResource, refill, profile);
    }

    public bool TryUseBattleStamina(PokemonVitalResourceKind resource, int amount, PokemonVitalProfileDefinition profile, out string failureMessage) {
        failureMessage = null;
        if(resource != PokemonVitalResourceKind.BattlePhysicalStamina && resource != PokemonVitalResourceKind.BattleElementalStamina) {
            failureMessage = "Only battle stamina resources can be spent through this method.";
            return false;
        }

        amount = Mathf.Max(0, amount);
        if(amount == 0) {
            return true;
        }

        EnsureVitalStateInitialized(profile);
        if(GetVitalValue(resource, profile) < amount) {
            failureMessage = $"{NickName} does not have enough {resource}.";
            return false;
        }

        ChangeVitalResource(resource, -amount, profile);
        return true;
    }

    public bool CanPayMoveVitalCost(Move move, out string failureMessage, PokemonVitalProfileDefinition fallbackVitalProfile = null) {
        failureMessage = null;
        if(move == null || move.Base == null) {
            failureMessage = $"{NickName} has no move to use!";
            return false;
        }

        var profile = move.Base.ResolveVitalProfile(fallbackVitalProfile);
        EnsureVitalStateInitialized(profile);

        int physicalCost = move.Base.GetBattlePhysicalStaminaCost(this, fallbackVitalProfile);
        if(physicalCost > 0 && GetVitalValue(PokemonVitalResourceKind.BattlePhysicalStamina, profile) < physicalCost) {
            failureMessage = $"{NickName} is too physically exhausted to use {move.Base.Name}.";
            return false;
        }

        int elementalCost = move.Base.GetBattleElementalStaminaCost(this, fallbackVitalProfile);
        if(elementalCost > 0 && GetVitalValue(PokemonVitalResourceKind.BattleElementalStamina, profile) < elementalCost) {
            failureMessage = $"{NickName} lacks enough elemental stamina to use {move.Base.Name}.";
            return false;
        }

        return true;
    }

    public bool TrySpendMoveVitalCost(Move move, out string failureMessage, PokemonVitalProfileDefinition fallbackVitalProfile = null) {
        if(!CanPayMoveVitalCost(move, out failureMessage, fallbackVitalProfile)) {
            return false;
        }

        if(move?.Base == null) {
            failureMessage = $"{NickName} has no move to use!";
            return false;
        }

        var profile = move.Base.ResolveVitalProfile(fallbackVitalProfile);
        int physicalCost = move.Base.GetBattlePhysicalStaminaCost(this, fallbackVitalProfile);
        if(physicalCost > 0) {
            ChangeVitalResource(PokemonVitalResourceKind.BattlePhysicalStamina, -physicalCost, profile);
        }

        int elementalCost = move.Base.GetBattleElementalStaminaCost(this, fallbackVitalProfile);
        if(elementalCost > 0) {
            ChangeVitalResource(PokemonVitalResourceKind.BattleElementalStamina, -elementalCost, profile);
        }

        failureMessage = null;
        return true;
    }

    public int ApplyCoreHealthDamageFromBattleDamage(int battleDamage, int overkillDamage = 0, PokemonVitalProfileDefinition profile = null, bool forceCoreDamage = false) {
        EnsureVitalStateInitialized(profile);
        int coreDamage = profile != null
            ? profile.CalculateCoreDamageFromBattleDamage(this, battleDamage, overkillDamage, forceCoreDamage)
            : PokemonVitalDefaults.CalculateFallbackCoreDamage(this, battleDamage, overkillDamage, forceCoreDamage);
        if(coreDamage <= 0) {
            return 0;
        }

        return -ChangeVitalResource(PokemonVitalResourceKind.CoreHealth, -coreDamage, profile);
    }

    public bool IsVitallyUsable(PokemonVitalProfileDefinition profile, out PokemonVitalBlockReason reason) {
        EnsureVitalStateInitialized(profile);
        reason = PokemonVitalBlockReason.None;

        bool blockCoreHealth = profile == null || profile.CoreHealthDepletionBlocksUse;
        bool blockCoreStamina = profile == null || profile.CoreStaminaDepletionBlocksUse;
        if(blockCoreHealth && VitalState.coreHealth <= 0) {
            reason = PokemonVitalBlockReason.CoreHealthDepleted;
            return false;
        }

        if(blockCoreStamina && (VitalState.corePhysicalStamina <= 0 || VitalState.coreElementalStamina <= 0)) {
            reason = PokemonVitalBlockReason.CoreStaminaDepleted;
            return false;
        }

        return true;
    }

    public bool NeedsLongTermTreatment(PokemonVitalProfileDefinition profile = null) {
        EnsureVitalStateInitialized(profile);
        return VitalState.coreHealth <= 0;
    }

    public bool NeedsRestOrFeeding(PokemonVitalProfileDefinition profile = null) {
        EnsureVitalStateInitialized(profile);
        return VitalState.corePhysicalStamina <= 0 || VitalState.coreElementalStamina <= 0;
    }

    public int GetBattleHpCapFromCoreHealth(PokemonVitalProfileDefinition profile = null) {
        EnsureVitalStateInitialized(profile);
        int maxCoreHealth = PokemonVitalDefaults.GetMaxCoreHealth(this, profile);
        if(maxCoreHealth <= 0 || VitalState.coreHealth <= 0) {
            return 0;
        }

        float coreRatio = Mathf.Clamp01(VitalState.coreHealth / (float)maxCoreHealth);
        return Mathf.Clamp(Mathf.CeilToInt(MaxHp * coreRatio), 1, MaxHp);
    }

    public bool ClampHPToCoreHealthCap(PokemonVitalProfileDefinition profile = null) {
        int cap = GetBattleHpCapFromCoreHealth(profile);
        int clamped = Mathf.Clamp(HP, 0, cap);
        if(clamped == HP) {
            return false;
        }

        HP = clamped;
        OnHpChanged?.Invoke();
        return true;
    }

    public void PrepareBattleEntryVitals(PokemonVitalProfileDefinition profile = null, bool spendCoreStamina = true, bool capHpByCoreHealth = true) {
        EnsureVitalStateInitialized(profile);
        PrepareBattleVitals(profile, spendCoreStamina);
        if(capHpByCoreHealth) {
            ClampHPToCoreHealthCap(profile);
        }
    }

    public void AddTimedRecoveryEffect(PokemonTimedRecoveryEffect effect) {
        if(effect == null) {
            return;
        }

        ActiveTimedRecoveryEffects ??= new List<PokemonTimedRecoveryEffect>();
        ActiveTimedRecoveryEffects.Add(effect);
        if(effect.HasStatModifiers) {
            AddStatusEvent($"{NickName} gained a temporary boost from {effect.sourceName}!");
        }
    }

    int ApplyHealingReceivedModifiers(int amount) {
        if(amount <= 0 || ActiveTimedRecoveryEffects == null || ActiveTimedRecoveryEffects.Count == 0) {
            return amount;
        }

        float multiplier = 1f;
        foreach(var effect in ActiveTimedRecoveryEffects) {
            if(effect != null) {
                multiplier *= Mathf.Max(0f, effect.healingReceivedMultiplier);
            }
        }

        return Mathf.Max(0, Mathf.RoundToInt(amount * multiplier));
    }

    int ApplyVitalRecoveryModifiers(int amount) {
        if(amount <= 0 || ActiveTimedRecoveryEffects == null || ActiveTimedRecoveryEffects.Count == 0) {
            return amount;
        }

        float multiplier = 1f;
        foreach(var effect in ActiveTimedRecoveryEffects) {
            if(effect != null) {
                multiplier *= Mathf.Max(0f, effect.vitalRecoveryMultiplier);
            }
        }

        return Mathf.Max(0, Mathf.RoundToInt(amount * multiplier));
    }

    int ApplyTimedRecoveryStatModifiers(Stat stat, int value) {
        if(ActiveTimedRecoveryEffects == null || ActiveTimedRecoveryEffects.Count == 0) {
            return value;
        }

        int modified = value;
        foreach(var effect in ActiveTimedRecoveryEffects) {
            if(effect != null) {
                modified = effect.ApplyStat(stat, modified);
            }
        }

        return modified;
    }

    void TickTimedRecoveryTurnEffects(PokemonVitalProfileDefinition profile = null) {
        if(ActiveTimedRecoveryEffects == null || ActiveTimedRecoveryEffects.Count == 0) {
            return;
        }

        bool changed = false;
        foreach(var effect in ActiveTimedRecoveryEffects) {
            if(effect != null) {
                changed |= effect.TickTurn(this, profile);
            }
        }

        int before = ActiveTimedRecoveryEffects.Count;
        ActiveTimedRecoveryEffects.RemoveAll(effect => effect == null || effect.IsExpired);
        if(changed || before != ActiveTimedRecoveryEffects.Count) {
            OnVitalsChanged?.Invoke();
        }
    }

    void TickTimedRecoveryBattleDurations() {
        if(ActiveTimedRecoveryEffects == null || ActiveTimedRecoveryEffects.Count == 0) {
            return;
        }

        foreach(var effect in ActiveTimedRecoveryEffects) {
            effect?.TickBattle();
        }

        ActiveTimedRecoveryEffects.RemoveAll(effect => effect == null || effect.IsExpired);
    }

    public bool OnBeforeTurn(){
        bool canPerformMove = true;

        if(Status?.OnBeforeMove != null){
            if(!Status.OnBeforeMove(this)){
                canPerformMove = false;
            }
        }
        if(VolatileStatus?.OnBeforeMove != null){
            if(!VolatileStatus.OnBeforeMove(this)){
                canPerformMove = false;
            }
        }
        return canPerformMove;
    }
    
    public void OnAfterTurn(){
        Ability?.OnAfterTurn?.Invoke(this);
        Status?.OnAfterTurn?.Invoke(this);
        VolatileStatus?.OnAfterTurn?.Invoke(this);

        if(HeldItem is BattleHeldItem battleHeldItem) {
            battleHeldItem.OnAfterTurn(this);
        }

        TickMoveRestrictions();
        TickPowerMechanicEffects();
        TickTimedRecoveryTurnEffects();
    }

    void TickMoveRestrictions(){
        if(TauntTurns > 0){
            TauntTurns--;
            if(TauntTurns == 0){
                AddStatusEvent($"{NickName}'s taunt wore off!");
            }
        }

        if(DisableTurns > 0){
            DisableTurns--;
            if(DisableTurns == 0){
                string disabledMoveName = DisabledMove?.Base.Name;
                DisabledMove = null;
                AddStatusEvent($"{NickName}'s {disabledMoveName} is no longer disabled!");
            }
        }

        if(EncoreTurns > 0){
            EncoreTurns--;
            if(EncoreTurns == 0){
                EncoreMove = null;
                AddStatusEvent($"{NickName}'s encore ended!");
            }
        }
    }

    public void DecreaseHP(int damage, bool callUpdateEvent = false){
        HP = Mathf.Clamp(HP - damage, 0, MaxHp);
        if(callUpdateEvent){
            OnHpChanged?.Invoke();
        }
        CheckHeldItem();
    }

    public void CheckHeldItem(){
        if(HeldItem == null) return;
        if(HP <= MaxHp / 2 && HeldItem is RecoveryItem){
            if(HeldItem.Use(this)){
                AddStatusEvent($"{NickName} used its {HeldItem.Name}!");
                HeldItem = null; // Consume item
            }
        }
    }

    public bool IncreaseHPWithResult(int amount){
        if(amount <= 0) {
            return false;
        }

        amount = ApplyHealingReceivedModifiers(amount);
        int before = HP;
        int cap = GetBattleHpCapFromCoreHealth();
        HP = Mathf.Clamp(HP + amount, 0, Mathf.Min(MaxHp, cap));
        if(HP == before) {
            return false;
        }

        OnHpChanged?.Invoke();
        return true;
    }

    public void IncreaseHP(int amount){
        IncreaseHPWithResult(amount);
    }

    public void GainExp(int exp){
        int maxExp = Base.GetExpForLevel(100);
        Exp = Mathf.Min(Exp + exp, maxExp);
        OnExpChanged?.Invoke();
    }

    public void GainEvs(Dictionary<Stat, int> evYield){
        foreach (var yield in evYield){
            if (StatEffortValues.ContainsKey(yield.Key)){
                if (StatEffortValues[yield.Key] < GlobalSettings.i.MaxEvPerStat && GetTotalEvs() < GlobalSettings.i.MaxEvs){
                    int amountToAdd = Mathf.Min(yield.Value, GlobalSettings.i.MaxEvPerStat - StatEffortValues[yield.Key]);
                    amountToAdd = Mathf.Min(amountToAdd, GlobalSettings.i.MaxEvs - GetTotalEvs());

                    StatEffortValues[yield.Key] += amountToAdd;
                }
            }
        }
    }

    public int GetTotalEvs(){
        return StatEffortValues.Values.Sum();
    }

    int ApplyGrowthToStat(Stat stat, int baseValue){
        if(GrowthState == null || !GrowthState.initialized) {
            return Mathf.Max(1, baseValue);
        }

        float multiplier = 1f + GrowthState.GetStatMultiplierBonus(stat);
        int flatBonus = GrowthState.GetFlatStatBonus(stat, null);
        return Mathf.Max(1, Mathf.FloorToInt(baseValue * Mathf.Max(0.01f, multiplier)) + flatBonus);
    }

    void EnsureGrowthState(){
        if(GrowthState == null) {
            GrowthState = new PokemonGrowthState();
        }
    }

    void EnsureAbilityTreeState(){
        if(AbilityTreeState == null) {
            AbilityTreeState = new PokemonAbilityTreeState();
        }
    }

    public int GetPowerFromSpeedRatio(Pokemon attacker){
        float ratio = (float)attacker.Speed / Speed;
        if(ratio < 1f) return 40;
        if(ratio < 2f) return 60;
        if(ratio < 3f) return 80;
        if(ratio < 4f) return 120;
        return 150;
    }

    public int GetPowerFromFuryCutter(Pokemon attacker){
        int[] powers = { 10, 20, 40, 80, 160 };
        return powers[Mathf.Clamp(attacker.ConsecutiveUseCount, 0, 4)];
    }

    public int GetPowerFromBaseWeight(){
        float weight = _base.BaseWeight;
        if(weight < 10f){
            return 20;
        } else if(weight < 25f){
            return 40;
        } else if(weight < 50f){
            return 60;
        } else if(weight < 100f){
            return 80;
        } else if(weight < 200f){
            return 100;
        } else {
            return 120;
        }
    }

    public int GetPowerFromWeightDifference(Pokemon source){
        float defending = _base.BaseWeight;
        float attacking = source.Base.BaseWeight;

        if(defending > (attacking * 0.5f)){
            return 40;
        } else if(defending > (attacking * 0.3335f)){
            return 60;
        } else if(defending > (attacking * 0.2501f)){
            return 80;
        } else if(defending > (attacking * 0.2001f)){
            return 100;
        } else {
            return 120;
        }
    }
}

public class DamageDetails{
    public bool Fainted { get; set;}
    public float Critical { get; set;}
    public float TypeEffectiveness { get; set;}

    public int DamageDealt {get; set;}
    public int OverkillDamage { get; set; }
    public int CoreHealthDamage { get; set; }
}

[System.Serializable]
public class PokemonSaveData{
    public string instanceId;
    public string name;
    public int Hp;
    public int level;
    public int xp;
    public string pokeball;
#pragma warning disable UAC1001
    public StatusConditionID? statusId;
#pragma warning restore UAC1001
    public List<MoveSaveData> moves;
    public Gender gender;
#pragma warning disable UAC1009
    public Dictionary<Stat, int> StatEffortValues;
#pragma warning restore UAC1009
    public AbilityID abilityId;
    public bool isShiny;
    public int friendship;
    public NatureID natureId;
    public PersonalityID personalityId;
    public string nickname;
    public string heldItem;
    public List<PokemonMoodValue> moodValues;
    public List<PokemonCareNeedValue> careNeedValues;
    public List<PokemonCareRecord> careRecords;
    public PokemonVitalSaveData vitalState;
    public PokemonGrowthState growthState;
    public PokemonEvolutionRuntimeState evolutionState;
    public PokemonTechniqueMemoryState techniqueMemory;
    public PokemonAbilityTreeState abilityTreeState;
}

[System.Serializable]
public class PokemonCareNeedValue {
    [Tooltip("Saved care need id.")]
    public string needId;
    [Tooltip("Saved care need value.")]
    public int value;
}

[System.Serializable]
public class PokemonCareRecord {
    [Tooltip("Care action id that was applied.")]
    public string actionId;
    [Tooltip("Care action display name saved for fallback/debug output.")]
    public string actionName;
    [Tooltip("Care category saved for history and requirements.")]
    public PokemonCareCategory category;
    [Tooltip("Source id that applied this care action.")]
    public string sourceId;
    [Tooltip("In-game day this care action was applied.")]
    public int day;
    [Tooltip("Absolute in-game hour this care action was applied.")]
    public int absoluteHour;
}

[System.Serializable]
public class StatusEvent{
    public StatusEventType Type {get; private set;}
    public string Message {get; private set;}

    public StatusEvent(StatusEventType type, string message){
        Type = type;
        Message = message;
    }
}
