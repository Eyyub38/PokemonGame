using System;
using UnityEngine;
using System.Collections.Generic;

public class Ability{
    public string Name {get; set;}
    public string Description {get; set;}

    public Func<float, Pokemon, Pokemon, Move, float> OnModifyAttack {get; set;}
    public Func<float, Pokemon, Pokemon, Move, float> OnModifySpAttack {get; set;}
    public Func<float, Pokemon, Pokemon, Move, float> OnModifyDefense {get; set;}
    public Func<float, Pokemon, Pokemon, Move, float> OnModifySpDefense {get; set;}
    public Func<float, Pokemon, Pokemon, Move, float> OnModifySpeed {get; set;}
    public Func<float, Pokemon, Pokemon, Move, float> OnModifyAccuracy {get; set;}
    public Func<StatusConditionID, Pokemon, EffectSource, bool> OnTrySetStatus { get; set; }
    public Func<StatusConditionID, Pokemon, EffectSource, bool> OnTrySetVolatileStatus { get; set; }
    public Func<float, Pokemon, Pokemon, Move, float> OnModifyMoveBasePower { get; set; }

    public Action<Dictionary<Stat, int>, Pokemon, Pokemon> OnBoost {get; set;}
    public Action<float, Pokemon, Pokemon, Move> OnDamagingHit { get; set; }
}
