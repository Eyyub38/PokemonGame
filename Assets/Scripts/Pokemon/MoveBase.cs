using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum MoveTarget{ Foe, Self}
public enum MoveCategory{ Physical, Special, Status}
public enum CritBehaviour{ None, HighCritRatio, AlwaysCrit, NeverCrit}
public enum RecoilType{ None, RecoilByMaxHP, RecoilByCurrentHP, RecoilByDamage}
public enum MoveTag{ Contact, MinimizeBonusDamage, SoundBased, BallOrBomb, AuraOrPulse, Bite, Dance, Explosive, PowderOrSpore, Punching, WindBased, SlicingMove, SemiInvulnerableBonusDamageFlying}

[CreateAssetMenu(fileName = "Move", menuName = "Move/Create new Move")]
public class MoveBase : ScriptableObject{
    [Header("Pokemon Details")]
    [SerializeField] string _name;
    [TextArea]
    [SerializeField] string description;

    [Header("Type")]
    [SerializeField] PokemonType type;
    
    [Header("Stats")]
    [SerializeField] int power;
    [SerializeField] int accuracy;
    [SerializeField] bool alwaysHits;
    [SerializeField] int pp;
    [SerializeField] int priority;
    [SerializeField] MoveCategory category;

    [Header("Effects")]
    [SerializeField] MoveEffects effects;
    [SerializeField] MoveTarget target;
    [SerializeField] List<SecondaryEffects> secondaries;
    [SerializeField] RecoilMoveEffect recoil = new RecoilMoveEffect();

    [Header("Crit Behaviour")]
    [SerializeField] CritBehaviour critBehaviour;

    //Properties
    public string Name{ get{return _name;}} 
    public string Description{ get{return description;}}
    public int Power{ get{return power;}}
    public int PP{ get{return pp;}}
    public int Priority{ get{return priority;}}
    public int Accuracy{ get{return accuracy;}}
    public bool AlwaysHits{ get{return alwaysHits;}}
    public PokemonType Type{ get{return type;}}
    public MoveCategory Category{ get{return category;}}
    public MoveEffects Effects{ get{return effects;}}
    public MoveTarget Target{ get{return target;}}
    public List<SecondaryEffects> Secondaries{ get{return secondaries;}}
    public RecoilMoveEffect Recoil{ get{return recoil;} }
    public CritBehaviour CritBehaviour{ get{return critBehaviour;} }
}

[System.Serializable]
public class MoveEffects{
    [SerializeField] List<StatBoosts> boosts;
    [SerializeField] ConditionID status;
    [SerializeField] ConditionID volatileStatus;

    public List<StatBoosts> Boosts{get{return boosts;}}
    public ConditionID Status{get{return status;}}
    public ConditionID VolatileStatus{get{return volatileStatus;}}
}

[System.Serializable]
public class SecondaryEffects : MoveEffects{
    [SerializeField] int chance;
    [SerializeField] MoveTarget target;

    public int Chance{ get{return chance;}}
    public MoveTarget Target{ get{return target;}}
}

[System.Serializable]
public class StatBoosts{
    public Stat stat;
    public int boost;
}

[System.Serializable]
public class RecoilMoveEffect{
    public RecoilType recoilType;
    public int recoilDamage = 0;
}


