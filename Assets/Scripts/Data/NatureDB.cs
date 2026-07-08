using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum NatureID { 
    Hardy, Lonely, Brave, Adamant, Naughty, 
    Bold, Docile, Relaxed, Impish, Lax, 
    Timid, Hasty, Serious, Jolly, Naive, 
    Modest, Mild, Quiet, Rash, Bashful, 
    Calm, Gentle, Sassy, Careful, Quirky 
}

public class Nature {
    public string Name { get; set; }
    public Stat BoostedStat { get; set; }
    public Stat LoweredStat { get; set; }

    public float GetMultiplier(Stat stat) {
        if (stat == BoostedStat) return 1.1f;
        if (stat == LoweredStat) return 0.9f;
        return 1f;
    }
}

public class NatureDB {
    public static Dictionary<NatureID, Nature> Natures { get; private set; } = new Dictionary<NatureID, Nature>() {
        { NatureID.Hardy, new Nature { Name = "Hardy" } },
        { NatureID.Lonely, new Nature { Name = "Lonely", BoostedStat = Stat.Attack, LoweredStat = Stat.Defense } },
        { NatureID.Brave, new Nature { Name = "Brave", BoostedStat = Stat.Attack, LoweredStat = Stat.Speed } },
        { NatureID.Adamant, new Nature { Name = "Adamant", BoostedStat = Stat.Attack, LoweredStat = Stat.SpAttack } },
        { NatureID.Naughty, new Nature { Name = "Naughty", BoostedStat = Stat.Attack, LoweredStat = Stat.SpDefense } },
        
        { NatureID.Bold, new Nature { Name = "Bold", BoostedStat = Stat.Defense, LoweredStat = Stat.Attack } },
        { NatureID.Docile, new Nature { Name = "Docile" } },
        { NatureID.Relaxed, new Nature { Name = "Relaxed", BoostedStat = Stat.Defense, LoweredStat = Stat.Speed } },
        { NatureID.Impish, new Nature { Name = "Impish", BoostedStat = Stat.Defense, LoweredStat = Stat.SpAttack } },
        { NatureID.Lax, new Nature { Name = "Lax", BoostedStat = Stat.Defense, LoweredStat = Stat.SpDefense } },
        
        { NatureID.Timid, new Nature { Name = "Timid", BoostedStat = Stat.Speed, LoweredStat = Stat.Attack } },
        { NatureID.Hasty, new Nature { Name = "Hasty", BoostedStat = Stat.Speed, LoweredStat = Stat.Defense } },
        { NatureID.Serious, new Nature { Name = "Serious" } },
        { NatureID.Jolly, new Nature { Name = "Jolly", BoostedStat = Stat.Speed, LoweredStat = Stat.SpAttack } },
        { NatureID.Naive, new Nature { Name = "Naive", BoostedStat = Stat.Speed, LoweredStat = Stat.SpDefense } },
        
        { NatureID.Modest, new Nature { Name = "Modest", BoostedStat = Stat.SpAttack, LoweredStat = Stat.Attack } },
        { NatureID.Mild, new Nature { Name = "Mild", BoostedStat = Stat.SpAttack, LoweredStat = Stat.Defense } },
        { NatureID.Quiet, new Nature { Name = "Quiet", BoostedStat = Stat.SpAttack, LoweredStat = Stat.Speed } },
        { NatureID.Rash, new Nature { Name = "Rash", BoostedStat = Stat.SpAttack, LoweredStat = Stat.SpDefense } },
        { NatureID.Bashful, new Nature { Name = "Bashful" } },
        
        { NatureID.Calm, new Nature { Name = "Calm", BoostedStat = Stat.SpDefense, LoweredStat = Stat.Attack } },
        { NatureID.Gentle, new Nature { Name = "Gentle", BoostedStat = Stat.SpDefense, LoweredStat = Stat.Defense } },
        { NatureID.Sassy, new Nature { Name = "Sassy", BoostedStat = Stat.SpDefense, LoweredStat = Stat.Speed } },
        { NatureID.Careful, new Nature { Name = "Careful", BoostedStat = Stat.SpDefense, LoweredStat = Stat.SpAttack } },
        { NatureID.Quirky, new Nature { Name = "Quirky" } }
    };
}
