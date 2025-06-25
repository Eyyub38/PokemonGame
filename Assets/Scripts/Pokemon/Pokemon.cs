using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public enum Gender{ None, Male, Female, Genderless}
public enum StatusEventType { Text, Damage, StatBoost}

[System.Serializable]
public class Pokemon{
    [SerializeField] PokemonBase _base;
    [SerializeField] int level;
    [SerializeField] Gender gender;
    [SerializeField] PokeballItem pokeball;


    public PokemonBase Base { get{ return _base; }}
    public int Level { get{ return level; } }
    public Gender Gender { get{ return gender; } set{ gender = value; } }
    public PokeballItem Pokeball { get => pokeball; set => pokeball = value; }
    public Dictionary<Stat, int> StatEffortValues { get; private set; }


    public int HP{ get; set; }
    public int Exp{ get; set; }
    public List<Move> Moves{ get; set; }
    public Move CurrentMove{ get; set; }
    public Dictionary<Stat, int> Stats{get; private set;}
    public Dictionary<Stat, int> StatBoosts{ get; private set;}
    public StatusCondition Status{ get; private set;}
    public StatusCondition VolatileStatus{ get; private set;}
    public int StatusTime{ get; set; }
    public int VolatileStatusTime{ get; set; }

    public Queue<StatusEvent> StatusChanges { get; private set; }

    public event System.Action OnStatusChanged;
    public event System.Action OnHpChanged;
    public event System.Action OnExpChanged;
    
    public int MaxHp{ get; private set;}
    public int Attack{ get{ return GetStat(Stat.Attack);}}
    public int Defense{ get{ return GetStat(Stat.Defense);}}
    public int SpAttack{ get{ return GetStat(Stat.SpAttack);}}
    public int SpDefense{ get{ return GetStat(Stat.SpDefense);}}
    public int Speed{ get{ return GetStat(Stat.Speed);}}
    
    public Pokemon(PokemonBase pBase, int pLvl, PokeballItem pokeball = null){
        _base = pBase;
        level = pLvl;
        this.pokeball = pokeball;
        Init();
    }

    public Pokemon(PokemonSaveData saveData){
        _base = PokemonDB.GetObjectByName(saveData.name);
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

        CalculateStats();
        StatusChanges = new Queue<StatusEvent>();
        ResetStatBoosts();
        VolatileStatus = null;
    }

    public void Init(){
        Moves = new List<Move>();
        foreach(var move in Base.LearnableMoves){
            if(Level >= move.Level){
                Moves.Add(new Move(move.Base));
            }
            if(Moves.Count >= PokemonBase.MaxNumberOfMoves){
                break;
            }
        }
        if(gender == Gender.None){
            if (Base.IsGenderless){
                gender = Gender.Genderless;
            } else {
                gender = UnityEngine.Random.value < Base.MaleRatio ? Gender.Male : Gender.Female;
            }
        }
        
        StatEffortValues = new Dictionary<Stat, int>() { { Stat.HitPoints, 0 }, { Stat.Attack, 0 }, { Stat.Defense, 0 }, { Stat.SpAttack, 0 }, { Stat.SpDefense, 0 }, { Stat.Speed, 0 } };

        Exp = Base.GetExpForLevel(Level);
        StatusChanges = new Queue<StatusEvent>();
        CalculateStats();
        HP = MaxHp;
        ResetStatBoosts();
        Status = null;
        VolatileStatus = null;
    }

    void CalculateStats(){
    	Stats = new Dictionary<Stat, int>();
   	 
    	Stats.Add(Stat.Attack, Mathf.FloorToInt((((2f * Base.Attack + (StatEffortValues[Stat.Attack] / 4f)) * Level) / 100f) + 5f));
    	Stats.Add(Stat.Defense, Mathf.FloorToInt((((2f * Base.Defense + (StatEffortValues[Stat.Defense] / 4f)) * Level) / 100f) + 5f));
    	Stats.Add(Stat.SpAttack, Mathf.FloorToInt((((2f * Base.SpAttack + (StatEffortValues[Stat.SpAttack] / 4f)) * Level) / 100f) + 5f));
    	Stats.Add(Stat.SpDefense, Mathf.FloorToInt((((2f * Base.SpDefense + (StatEffortValues[Stat.SpDefense] / 4f)) * Level) / 100f) + 5f));
    	Stats.Add(Stat.Speed, Mathf.FloorToInt((((2f * Base.Speed + (StatEffortValues[Stat.Speed] / 4f)) * Level) / 100f) + 5f));

    	int oldMaxHP = MaxHp;
    	MaxHp = Mathf.FloorToInt((((2f * Base.MaxHp + (StatEffortValues[Stat.HitPoints] / 4f)) * Level) / 100f) + Level + 10f);

    	if (oldMaxHP != 0){
        	HP += MaxHp - oldMaxHP;
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

    int GetStat(Stat stat){
        int statVal = Stats[stat];

        int boost = StatBoosts[stat];
        var boostVal = new float[]{ 1f, 1.5f, 2f, 2.5f, 3f, 3.5f, 4f};
        if(boost >= 0){
            statVal = Mathf.FloorToInt(statVal * boostVal[boost]);
        } else {
            statVal = Mathf.FloorToInt(statVal / boostVal[-boost]);
        }
        return statVal;
    }

    public Move GetRandomMove(){
        var movesWithPP = Moves.Where(x => x.PP > 0).ToList();
        if(movesWithPP.Count == 0){
            return null;
        }
        
        int r = UnityEngine.Random.Range( 0, movesWithPP.Count);
        return movesWithPP[r];
    }

    public LearnableMove GetLearnableMoveAtCurrLevel() {
        return Base.LearnableMoves.Where(x => x.Level == level).FirstOrDefault();
    }

    public PokemonSaveData GetSaveData(){
        var saveData = new PokemonSaveData(){
            name = Base.name,
            Hp = HP,
            level = level,
            xp = Exp,
            pokeball = Pokeball.name,
            statusId = Status?.Id,
            moves = Moves.Select(x => x.GetSaveData()).ToList()
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

    public void LearnMove(MoveBase moveToLearn){
        if(Moves.Count > PokemonBase.MaxNumberOfMoves) {
            return;
        }
        Moves.Add(new Move(moveToLearn));
    }
    
    public void SetStatus(StatusConditionID conditionID){
        if(Status != null) return;

        Status = StatusConditionsDB.Conditions[conditionID];
        Status?.OnStart?.Invoke(this);
        AddStatusEvet($"{Base.Name} {Status.StartMessage}");
        OnStatusChanged?.Invoke();
    }
    
    public void SetVolatileStatus(StatusConditionID conditionID){
        if(VolatileStatus != null) return;

        VolatileStatus = StatusConditionsDB.Conditions[conditionID];
        VolatileStatus?.OnStart?.Invoke(this);
        AddStatusEvet($"{Base.Name} {VolatileStatus.StartMessage}");
    }

    public void CureStatus(){
        Status = null;
        OnStatusChanged?.Invoke();
    }
    
    public void CureVolatileStatus(){
        VolatileStatus = null;
    }

    public Evolution CheckForEvolution(){
        return Base.Evolutions.FirstOrDefault(e => e.RequiredLevel <= level);
    }
    
    public Evolution CheckForEvolution(ItemBase item){
        return Base.Evolutions.FirstOrDefault(e => e.RequiredItem == item);
    }

    public void Evolve(Evolution evolution){
        _base = evolution.EvolvesInto;
        CalculateStats(); 
    }

    public void ApplyBoosts(List<StatBoosts> statBoosts){
        foreach(var statBoost in statBoosts){
            var stat = statBoost.stat;
            var boost = statBoost.boost;
            bool changeIsPositive = (boost > 0)? true : false;

            if(changeIsPositive && StatBoosts[stat] == 6 || !changeIsPositive && StatBoosts[stat] == -6){
                string riseOrFall = changeIsPositive ? "rose" : "fell";
                AddStatusEvet(StatusEventType.StatBoost, $"{Base.Name}'s {stat} cannot go any higher, it has already {riseOrFall} to the maximum!");
            } else {
                StatBoosts[stat] = Mathf.Clamp(StatBoosts[stat] += boost,-6, 6);
                string riseOrFall = changeIsPositive ? "rose" : "fell";
                string bigChance = Mathf.Abs(boost) >= 3 ? "severly" : Mathf.Abs(boost) == 2 ? "harshly" : "";
                AddStatusEvet(StatusEventType.StatBoost, $"{Base.Name}'s {stat} {bigChance} {riseOrFall}!");
            }
        }
    }

    public DamageDetails TakeDamage(Move move, Pokemon attacker, float weatherModifier = 1f){
        float critical = 1f;

        int power = move.Base.Power;
        if(move.Base.MovePowerBasedOn == PowerBasedOn.target){
            power = GetPowerFromBaseWeight();
        } else if(move.Base.MovePowerBasedOn == PowerBasedOn.difference){
            power = GetPowerFromWeightDifference(attacker);
        }
        if (move.Base.OneHitKoMoveEffect.isOneHitKnockOut){
            int oneHitDamage = HP;
            DecreaseHP(oneHitDamage, true);
            return new DamageDetails() { TypeEffectiveness = 1f, Critical = 1f, Fainted = false };
        }

        if(!(move.Base.CritBehaviour == CritBehaviour.NeverCrit)){
            if(move.Base.CritBehaviour == CritBehaviour.AlwaysCrit){
                critical = 1.5f;
            } else {
                int critChance = 0 + (move.Base.CritBehaviour == CritBehaviour.HighCritRatio ? 1 : 0);
                float[] chances = new float[]{(4.146f), (12.5f),(50f), 100f};
                if(UnityEngine.Random.value * 100f <= chances[Mathf.Clamp(critChance, 0, 3)]){
                    critical = 1.5f;
                }
            }
        }

        
        float typeEffectiveness = TypeChart.GetEffectiveness(move.Base.Type, this.Base.Type1) * TypeChart.GetEffectiveness(move.Base.Type, this.Base.Type2);
        
        var damageDetails = new DamageDetails(){
            Critical = critical,
            TypeEffectiveness = typeEffectiveness,
            Fainted = false,

            DamageDealt = 0
        };

        float attack = (move.Base.Category == MoveCategory.Special)? attacker.SpAttack : attacker.Attack;
        float defense = (move.Base.Category == MoveCategory.Special)? SpDefense : Defense;

        float modifiers = UnityEngine.Random.Range( 0.85f, 1f) * typeEffectiveness * critical * weatherModifier;
        float a = ( 2 * attacker.Level + 10) / 250f;
        float d = a * move.Base.Power * ((float)attack / defense) + 2;
        int damage = Mathf.FloorToInt(d * modifiers);

        DecreaseHP(damage, true);
        damageDetails.DamageDealt = damage;

        return damageDetails;
    }

    public void TakeRecoilDamage(int damage){
        if(damage < 1){
            damage = 1;
        }
        DecreaseHP(damage, true);
        AddStatusEvet($"{Base.Name} took {damage} recoil damage!");
    }

    public void OnBattleOver(){
        VolatileStatus = null;
        ResetStatBoosts();
    }

    public void AddStatusEvet(StatusEventType type, string message){
        StatusChanges.Enqueue(new StatusEvent(type, message));
    }
    
    public void AddStatusEvet(string message){
        StatusChanges.Enqueue(new StatusEvent(StatusEventType.Text, message));
    }

    public void Heal(){
        HP = MaxHp;
        
        OnHpChanged?.Invoke();        
        CureStatus();
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
        Status?.OnAfterTurn?.Invoke(this);
        VolatileStatus?.OnAfterTurn?.Invoke(this);
    }

    public void DecreaseHP(int damage, bool callUpdateEvent = false){
        HP = Mathf.Clamp(HP - damage, 0, MaxHp);
        if(callUpdateEvent){
            OnHpChanged?.Invoke();
        }
    }

    public void IncreaseHP(int amount){
        HP = Mathf.Clamp(HP + amount, 0, MaxHp);
        OnHpChanged?.Invoke();
    }

    public void GainExp(int exp){
        Exp += exp;
        OnExpChanged?.Invoke();
    }

    public void GainEvs(Dictionary<Stat, int> evGained){
    	foreach (var sev in StatEffortValues.ToArray()){
            if (sev.Value < GlobalSettings.i.MaxEvPerStat && GetTotalEvs() < GlobalSettings.i.MaxEvs){
                evGained[sev.Key] = Mathf.Clamp(evGained[sev.Key], 0, (GlobalSettings.i.MaxEvs - GetTotalEvs()));
                StatEffortValues[sev.Key] = Mathf.Clamp((StatEffortValues[sev.Key] += evGained[sev.Key]), 0, GlobalSettings.i.MaxEvPerStat);
            }
        }
    }

    public int GetTotalEvs(){
        return StatEffortValues.Values.Sum();
    }

    public int GetPowerFromBaseWeight(){
        float weight = _base.BaseWeight;
        if(weight < 10f){
            return 20;
        } else if(weight < 25f){
            return 40;
        } else if(weight < 50f){
            return 60;
        } else if(weight < 50f){
            return 80;
        } else if(weight < 50f){
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
}

[System.Serializable]
public class PokemonSaveData{
    public string name;
    public int Hp;
    public int level;
    public int xp;
    public string pokeball;
    public StatusConditionID? statusId;
    public List<MoveSaveData> moves;
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