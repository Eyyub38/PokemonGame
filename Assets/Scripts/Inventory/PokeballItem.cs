using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Items/Create new pokeball item")]
public class PokeballItem : ItemBase{
    

    public override bool Use(Pokemon pokemon){
        return true;
    }
}