using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum MoveTarget{ Foe, Self}
public enum MoveCategory{ Physical, Special, Status}
public enum PowerBasedOn {Value, TargetWeight, WeightDifference, SpeedRatio, FuryCutter}
public enum CritBehaviour{ None, HighCritRatio, AlwaysCrit, NeverCrit}
public enum RecoilType{ None, RecoilByMaxHP, RecoilByCurrentHP, RecoilByDamage}
public enum MoveFlag { Contact, Punch, Bite, Sound, MinimizeBonusDamage, BallOrBomb, AuraOrPulse, Dance, Explosive, PowderOrSpore, WindBased, SlicingMove, SemiInvulnerableBonusDamageFlying }
public enum EffectSource { Move, Ability, Item }

[CreateAssetMenu(fileName = "Move", menuName = "Move/Create new Move")]
public class MoveBase : ScriptableObject{
    [Header("Move Details")]
    [Tooltip("Move name shown in battle and menus.")]
    [SerializeField] string _name;
    [Tooltip("Move description shown in menus.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Sound effect played when the move is used.")]
    [SerializeField] AudioClip soundEffect;
    [Tooltip("Tags used by abilities/items/effects to identify move behavior.")]
    [SerializeField] List<MoveFlag> flags = new List<MoveFlag>();

    [Header("Type")]
    [Tooltip("Base elemental type of this move.")]
    [SerializeField] PokemonType type;
    
    [Header("Stats")]
    [Tooltip("Base power. 0 is typical for pure status moves.")]
    [Min(0)]
    [SerializeField] int power;
    [Tooltip("Accuracy percentage. Ignored when Always Hits is enabled.")]
    [Range(0, 100)]
    [SerializeField] int accuracy;
    [Tooltip("If enabled, this move bypasses normal accuracy checks.")]
    [SerializeField] bool alwaysHits;
    [Tooltip("Maximum PP for this move.")]
    [Min(1)]
    [SerializeField] int pp;
    [Tooltip("Turn priority. Higher priority moves act earlier.")]
    [SerializeField] int priority;
    [Tooltip("Physical, Special or Status category.")]
    [SerializeField] MoveCategory category;

    [Header("Effects")]
    [Tooltip("Primary effects applied by this move.")]
    [SerializeField] MoveEffects effects;
    [Tooltip("Default target type for this move.")]
    [SerializeField] MoveTarget target;
    [Tooltip("Secondary effects rolled after the main move effect.")]
    [SerializeField] List<SecondaryEffects> secondaries = new List<SecondaryEffects>();

    [Header("Recoils Moves")]
    [Tooltip("Recoil behavior applied after this move deals damage.")]
    [SerializeField] RecoilMoveEffect recoil = new RecoilMoveEffect();

    [Header("Drain Moves")]
    [Tooltip("Percentage of dealt damage restored to the user. 0 disables drain.")]
    [Range(0, 100)]
    [SerializeField] int drainingPercentage = 0;

    [Header("Battle Vital Costs")]
    [Tooltip("Optional vital profile used when this move calculates stamina costs or core damage. Empty uses default formulas.")]
    [SerializeField] PokemonVitalProfileDefinition vitalProfile;
    [Tooltip("Flat battle physical stamina cost paid when this move is used.")]
    [Min(0)]
    [SerializeField] int battlePhysicalStaminaCost;
    [Tooltip("Additional battle physical stamina cost as a fraction of the user's max battle physical stamina.")]
    [Range(0f, 1f)]
    [SerializeField] float battlePhysicalStaminaCostPercent;
    [Tooltip("Flat battle elemental stamina cost paid when this move is used.")]
    [Min(0)]
    [SerializeField] int battleElementalStaminaCost;
    [Tooltip("Additional battle elemental stamina cost as a fraction of the user's max battle elemental stamina.")]
    [Range(0f, 1f)]
    [SerializeField] float battleElementalStaminaCostPercent;

    [Header("Core Health Pressure")]
    [Tooltip("If enabled, damage from this move can reduce long-term core health using the vital profile's threshold rules.")]
    [SerializeField] bool canDamageCoreHealth;
    [Tooltip("If enabled, this move can damage core health even when normal core-damage threshold would not be met.")]
    [SerializeField] bool forceCoreHealthDamage;
    [Tooltip("Extra flat core health damage applied after normal threshold conversion when the move deals damage.")]
    [Min(0)]
    [SerializeField] int flatCoreHealthDamage;
    [Tooltip("Extra core health damage as a fraction of the dealt battle damage.")]
    [Range(0f, 1f)]
    [SerializeField] float extraCoreHealthDamagePercent;

    [Header("Crit Behaviour")]
    [Tooltip("Special critical-hit rule for this move.")]
    [SerializeField] CritBehaviour critBehaviour;
    [Tooltip("One-hit KO configuration for moves like Fissure.")]
    [SerializeField] OneHitKoMoveEffect oneHitKoMoveEffect = new OneHitKoMoveEffect();
    
    [Header("Multi-Hit Move")]
    [Tooltip("If enabled, this move hits multiple times.")]
    [SerializeField] bool isMultiHitMove = false;
    [Tooltip("X is minimum hits. Y is maximum hits. If Y is 0, X is used as fixed hit count.")]
    [SerializeField] Vector2Int hitRange = new Vector2Int( 2, 0);

    [Header("Weight Base Moves")]
    [Tooltip("Rule used to calculate dynamic move power.")]
    [SerializeField] PowerBasedOn movePowerBasedOn = PowerBasedOn.Value;

    public string Name{ get{return _name;}} 
    public string Description{ get{return description;}}
    public int Power{ get{return power;}}
    public int PP{ get{return pp;}}
    public int Priority{ get{return priority;}}
    public int Accuracy{ get{return accuracy;}}
    public int DrainingPercentage => drainingPercentage;
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
    public PokemonVitalProfileDefinition VitalProfile => vitalProfile;
    public int BattlePhysicalStaminaCost => battlePhysicalStaminaCost;
    public float BattlePhysicalStaminaCostPercent => battlePhysicalStaminaCostPercent;
    public int BattleElementalStaminaCost => battleElementalStaminaCost;
    public float BattleElementalStaminaCostPercent => battleElementalStaminaCostPercent;
    public bool CanDamageCoreHealth => canDamageCoreHealth;
    public bool ForceCoreHealthDamage => forceCoreHealthDamage;
    public int FlatCoreHealthDamage => flatCoreHealthDamage;
    public float ExtraCoreHealthDamagePercent => extraCoreHealthDamagePercent;

    public PokemonVitalProfileDefinition ResolveVitalProfile(PokemonVitalProfileDefinition fallbackProfile = null) {
        return vitalProfile != null ? vitalProfile : fallbackProfile;
    }

    public int GetBattlePhysicalStaminaCost(Pokemon user, PokemonVitalProfileDefinition fallbackProfile = null) {
        var resolvedProfile = ResolveVitalProfile(fallbackProfile);
        int percentCost = user != null && battlePhysicalStaminaCostPercent > 0f
            ? Mathf.RoundToInt(user.GetVitalMax(PokemonVitalResourceKind.BattlePhysicalStamina, resolvedProfile) * battlePhysicalStaminaCostPercent)
            : 0;
        return Mathf.Max(0, battlePhysicalStaminaCost + percentCost);
    }

    public int GetBattleElementalStaminaCost(Pokemon user, PokemonVitalProfileDefinition fallbackProfile = null) {
        var resolvedProfile = ResolveVitalProfile(fallbackProfile);
        int percentCost = user != null && battleElementalStaminaCostPercent > 0f
            ? Mathf.RoundToInt(user.GetVitalMax(PokemonVitalResourceKind.BattleElementalStamina, resolvedProfile) * battleElementalStaminaCostPercent)
            : 0;
        return Mathf.Max(0, battleElementalStaminaCost + percentCost);
    }

    public bool HasBattleVitalCost(Pokemon user, PokemonVitalProfileDefinition fallbackProfile = null) {
        return GetBattlePhysicalStaminaCost(user, fallbackProfile) > 0 || GetBattleElementalStaminaCost(user, fallbackProfile) > 0;
    }

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

    public bool HasFlag(MoveFlag flag){
        return flags.Contains(flag);
    }
}

[System.Serializable]
public class MoveEffects{
    [Tooltip("Stat changes applied by this effect.")]
    [SerializeField] List<StatBoosts> boosts = new List<StatBoosts>();
    [Tooltip("Regular status condition applied by this effect.")]
    [SerializeField] StatusConditionID status;
    [Tooltip("Volatile status condition applied by this effect.")]
    [SerializeField] StatusConditionID volatileStatus;
    [Tooltip("Weather started by this effect.")]
    [SerializeField] WeatherConditionID weatherStatus;
    [Tooltip("Terrain started by this effect.")]
    [SerializeField] TerrainID terrainStatus;

    [Tooltip("If enabled, sets Stealth Rock on the target side.")]
    [SerializeField] bool stealthRock;
    [Tooltip("If enabled, sets Spikes on the target side.")]
    [SerializeField] bool spikes;
    [Tooltip("If enabled, increases the user's critical-hit focus.")]
    [SerializeField] bool focusEnergy;
    [Tooltip("If enabled, sets Reflect on the user's side.")]
    [SerializeField] bool reflect;
    [Tooltip("If enabled, sets Light Screen on the user's side.")]
    [SerializeField] bool lightScreen;
    [Tooltip("If enabled, sets Aurora Veil on the user's side.")]
    [SerializeField] bool auroraVeil;
    [Tooltip("If enabled, protects the user for the turn.")]
    [SerializeField] bool protect;
    [Tooltip("If enabled, may make the target flinch.")]
    [SerializeField] bool flinch;
    [Tooltip("If enabled, prevents the target from using status moves.")]
    [SerializeField] bool taunt;
    [Tooltip("Number of turns Taunt lasts.")]
    [Min(1)]
    [SerializeField] int tauntTurns = 3;
    [Tooltip("If enabled, disables one of the target's moves.")]
    [SerializeField] bool disable;
    [Tooltip("Number of turns Disable lasts.")]
    [Min(1)]
    [SerializeField] int disableTurns = 4;
    [Tooltip("If enabled, forces the target to repeat its last move.")]
    [SerializeField] bool encore;
    [Tooltip("Number of turns Encore lasts.")]
    [Min(1)]
    [SerializeField] int encoreTurns = 3;
    [Tooltip("If enabled, clears stat boosts from the user.")]
    [SerializeField] bool clearUserStatBoosts;
    [Tooltip("If enabled, clears stat boosts from the target.")]
    [SerializeField] bool clearTargetStatBoosts;
    [Tooltip("Percentage of max HP restored to the target of the healing effect.")]
    [Range(0, 100)]
    [SerializeField] int healingPercentage;

    public List<StatBoosts> Boosts{get{return boosts;}}
    public StatusConditionID Status{get{return status;}}
    public StatusConditionID VolatileStatus{get{return volatileStatus;}}
    public WeatherConditionID WeatherStatus {get{return weatherStatus;}}
    public TerrainID TerrainStatus {get{return terrainStatus;}}
    public bool StealthRock => stealthRock;
    public bool Spikes => spikes;
    public bool FocusEnergy => focusEnergy;
    public bool Reflect => reflect;
    public bool LightScreen => lightScreen;
    public bool AuroraVeil => auroraVeil;
    public bool Protect => protect;
    public bool Flinch => flinch;
    public bool Taunt => taunt;
    public int TauntTurns => tauntTurns <= 0 ? 3 : tauntTurns;
    public bool Disable => disable;
    public int DisableTurns => disableTurns <= 0 ? 4 : disableTurns;
    public bool Encore => encore;
    public int EncoreTurns => encoreTurns <= 0 ? 3 : encoreTurns;
    public bool ClearUserStatBoosts => clearUserStatBoosts;
    public bool ClearTargetStatBoosts => clearTargetStatBoosts;
    public int HealingPercentage => healingPercentage;
}

[System.Serializable]
public class SecondaryEffects : MoveEffects{
    [Tooltip("Percent chance for this secondary effect to trigger.")]
    [Range(0, 100)]
    [SerializeField] int chance;
    [Tooltip("Target affected by this secondary effect.")]
    [SerializeField] MoveTarget target;

    public int Chance{ get{return chance;}}
    public MoveTarget Target{ get{return target;}}
}

[System.Serializable]
public class StatBoosts{
    [Tooltip("Stat affected by this boost.")]
    public Stat stat;
    [Tooltip("Boost stage amount. Positive raises, negative lowers.")]
    public int boost;
}

[System.Serializable]
public class RecoilMoveEffect{
    [Tooltip("How recoil damage is calculated.")]
    public RecoilType recoilType;
    [Tooltip("Recoil value used by the selected recoil type.")]
    [Min(0)]
    public int recoilDamage = 0;
}

[System.Serializable]
public class OneHitKoMoveEffect{
    [Tooltip("If enabled, this move uses one-hit knockout rules.")]
	public bool isOneHitKnockOut;
    [Tooltip("If enabled, lower-level odds exceptions are allowed by battle logic.")]
	public bool lowerOddsException;
    [Tooltip("Type that is immune to this one-hit KO effect.")]
	public PokemonType immunityType;
}


