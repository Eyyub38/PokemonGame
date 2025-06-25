using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum MoveTarget{ Foe, Self}
public enum MoveCategory{ Physical, Special, Status}
public enum PowerBasedOn {Value, TargetWeight, WeightDifference}
public enum CritBehaviour{ None, HighCritRatio, AlwaysCrit, NeverCrit}
public enum RecoilType{ None, RecoilByMaxHP, RecoilByCurrentHP, RecoilByDamage}
public enum MoveTag{ Contact, MinimizeBonusDamage, SoundBased, BallOrBomb, AuraOrPulse, Bite, Dance, Explosive, PowderOrSpore, Punching, WindBased, SlicingMove, SemiInvulnerableBonusDamageFlying}

[CreateAssetMenu(fileName = "Move", menuName = "Move/Create new Move")]
public class MoveBase : ScriptableObject{
    [Header("Move Details")]
    [SerializeField] string _name;
    [TextArea]
    [SerializeField] string description;
    [SerializeField] AudioClip soundEffect;

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

    [Header("Recoils Moves")]
    [SerializeField] RecoilMoveEffect recoil = new RecoilMoveEffect();

    [Header("Drain Moves")]
    [SerializeField] int drianingPercentage = 0;

    [Header("Crit Behaviour")]
    [SerializeField] CritBehaviour critBehaviour;
    [SerializeField] OneHitKoMoveEffect oneHitKoMoveEffect = new OneHitKoMoveEffect();
    
    [Header("Multi-Hit Move")]
    [SerializeField] bool isMultiHitMove = false;
    [SerializeField] Vector2Int hitRange = new Vector2Int( 2, 0);

    [Header("Weight Base Moves")]
    [SerializeField] PowerBasedOn movePowerBasedOn = PowerBasedOn.Value;

    public string Name{ get{return _name;}} 
    public string Description{ get{return description;}}
    public int Power{ get{return power;}}
    public int PP{ get{return pp;}}
    public int Priority{ get{return priority;}}
    public int Accuracy{ get{return accuracy;}}
    public int DrainingPercentage => drianingPercentage;
    public bool AlwaysHits{ get{return alwaysHits;}}
    public bool IsMultiHitMove { get{return isMultiHitMove;}}
    public PokemonType Type{ get{return type;}}
    public MoveCategory Category{ get{return category;}}
    public MoveEffects Effects{ get{return effects;}}
    public MoveTarget Target{ get{return target;}}
    public List<SecondaryEffects> Secondaries{ get{return secondaries;}}
    public RecoilMoveEffect Recoil{ get{return recoil;} }
    public CritBehaviour CritBehaviour{ get{return critBehaviour;} }
    public AudioClip SoundEffect{ get{return soundEffect;} }
    public OneHitKoMoveEffect OneHitKoMoveEffect => oneHitKoMoveEffect;
    public PowerBasedOn MovePowerBasedOn => movePowerBasedOn;

    public int GetHitTimes(){
        if(IsMultiHitMove){
            if(hitRange.y == 0){
                return hitRange.x;
            } else {
                return Random.Range(hitRange.x, hitRange.y + 1);
            }
        } else {
            return 1;
        }
    }
}

[System.Serializable]
public class MoveEffects{
    [SerializeField] List<StatBoosts> boosts;
    [SerializeField] StatusConditionID status;
    [SerializeField] StatusConditionID volatileStatus;
    [SerializeField] WeatherConditionID weatherStatus;

    public List<StatBoosts> Boosts{get{return boosts;}}
    public StatusConditionID Status{get{return status;}}
    public StatusConditionID VolatileStatus{get{return volatileStatus;}}
    public WeatherConditionID WeatherStatus {get{return weatherStatus;}}
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

[System.Serializable]
public class OneHitKoMoveEffect{
	public bool isOneHitKnockOut;
	public bool lowerOddsException;
	public PokemonType immunityType;
}


