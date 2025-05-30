using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum MoveTarget{ Foe, Self}
public enum MoveCategory{ Physical, Special, Status}

[CreateAssetMenu(fileName = "Move", menuName = "Move/Create new Move")]
public class MoveBase : ScriptableObject{
    [SerializeField] string _name;
    [TextArea]
    [SerializeField] string description;

    //Stats&Type
    [SerializeField] PokemonType type;
    [SerializeField] int power;
    [SerializeField] int accuracy;
    [SerializeField] bool alwaysHits;
    [SerializeField] int pp;
    [SerializeField] int priority;
    [SerializeField] MoveCategory category;
    [SerializeField] MoveEffects effects;
    [SerializeField] List<SecondaryEffects> secondaries;
    [SerializeField] MoveTarget target;

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


