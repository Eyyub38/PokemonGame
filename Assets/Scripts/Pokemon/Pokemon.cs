using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public enum Gender{ None, Male, Female, Genderless}

[System.Serializable]
public class Pokemon{
    [SerializeField] PokemonBase _base;
    [SerializeField] int level;
    [SerializeField] Gender gender;
    [SerializeField] PokeballItem pokeball;


    public PokemonBase Base { get{ return _base; }}
    public int Level { get{ return level; } }
    public Gender Gender { get{ return gender; } }
    public PokeballItem Pokeball { get => pokeball; set => pokeball = value; }

    public int HP{ get; set; }
    public int Exp{ get; set; }
    public List<Move> Moves{ get; set; }
    public Move CurrentMove{ get; set; }
    public Dictionary<Stat, int> Stats{get; private set;}
    public Dictionary<Stat, int> StatBoosts{ get; private set;}
    public Condition Status{ get; private set;}
    public Condition VolatileStatus{ get; private set;}
    public int StatusTime{ get; set; }
    public int VolatileStatusTime{ get; set; }

    public Queue<string> StatusChanges { get; private set; }

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
            Status = ConditionsDB.Conditions[saveData.statusId.Value];
        } else {
            Status = null;
        }

        Moves = saveData.moves.Select(s => new Move(s)).ToList();

        CalculateStats();
        StatusChanges = new Queue<string>();
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

        Exp = Base.GetExpForLevel(Level);
        StatusChanges = new Queue<string>();
        CalculateStats();
        HP = MaxHp;
        ResetStatBoosts();
        Status = null;
        VolatileStatus = null;
    }

    void CalculateStats(){
        Stats = new Dictionary<Stat, int>();
        Stats.Add(Stat.Attack, Mathf.FloorToInt((Base.Attack * Level) / 100f) + 5);
        Stats.Add(Stat.Defense, Mathf.FloorToInt((Base.Defense * Level) / 100f) + 5);
        Stats.Add(Stat.SpAttack, Mathf.FloorToInt((Base.SpAttack * Level) / 100f) + 5);
        Stats.Add(Stat.SpDefense, Mathf.FloorToInt((Base.SpDefense * Level) / 100f) + 5);
        Stats.Add(Stat.Speed, Mathf.FloorToInt((Base.Speed * Level) / 100f) + 5);

        int oldMaxHP = MaxHp;
        MaxHp = Mathf.FloorToInt((Base.MaxHp * Level) / 100f) + 10 + Level;

        if(oldMaxHP != 0){
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

    public bool HasMove(MoveBase moveToCheck){
        return Moves.Count( m => m.Base == moveToCheck) > 0;
    }

    public void LearnMove(MoveBase moveToLearn){
        if(Moves.Count > PokemonBase.MaxNumberOfMoves) {
            return;
        }
        Moves.Add(new Move(moveToLearn));
    }
    
    public void SetStatus(ConditionID conditionID){
        if(Status != null) return;

        Status = ConditionsDB.Conditions[conditionID];
        Status?.OnStart?.Invoke(this);
        StatusChanges.Enqueue($"{Base.Name} {Status.StartMessage}");
        OnStatusChanged?.Invoke();
    }
    
    public void SetVolatileStatus(ConditionID conditionID){
        if(VolatileStatus != null) return;

        VolatileStatus = ConditionsDB.Conditions[conditionID];
        VolatileStatus?.OnStart?.Invoke(this);
        StatusChanges.Enqueue($"{Base.Name} {VolatileStatus.StartMessage}");
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
                StatusChanges.Enqueue($"{Base.Name}'s {stat} cannot go any higher, it has already {riseOrFall} to the maximum!");
            } else {
                StatBoosts[stat] = Mathf.Clamp(StatBoosts[stat] += boost,-6, 6);
                string riseOrFall = changeIsPositive ? "rose" : "fell";
                string bigChance = Mathf.Abs(boost) >= 3 ? "severly" : Mathf.Abs(boost) == 2 ? "harshly" : "";
                StatusChanges.Enqueue($"{Base.Name}'s {stat} {bigChance} {riseOrFall}!");
            }
        }
    }

    public DamageDetails TakeDamage(Move move, Pokemon attacker){
        float critical = 1f;

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

        float modifiers = UnityEngine.Random.Range( 0.85f, 1f) * typeEffectiveness * critical;
        float a = ( 2 * attacker.Level + 10) / 250f;
        float d = a * move.Base.Power * ((float)attack / defense) + 2;
        int damage = Mathf.FloorToInt(d * modifiers);

        DecreaseHP(damage);
        damageDetails.DamageDealt = damage;

        return damageDetails;
    }

    public void TakeRecoilDamage(int damage){
        if(damage < 1){
            damage = 1;
        }
        DecreaseHP(damage);
        StatusChanges.Enqueue($"{Base.Name} took {damage} recoil damage!");
    }

    public void OnBattleOver(){
        VolatileStatus = null;
        ResetStatBoosts();
    }

    public void Heal(){
        HP = MaxHp;
        OnHpChanged?.Invoke();
        
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

    public void DecreaseHP(int damage){
        HP = Mathf.Clamp(HP - damage, 0, MaxHp);
        OnHpChanged?.Invoke();
    }

    public void IncreaseHP(int amount){
        HP = Mathf.Clamp(HP + amount, 0, MaxHp);
        OnHpChanged?.Invoke();
    }

    public void GainExp(int exp){
        Exp += exp;
        OnExpChanged?.Invoke();
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
    public ConditionID? statusId;
    public List<MoveSaveData> moves;
}
