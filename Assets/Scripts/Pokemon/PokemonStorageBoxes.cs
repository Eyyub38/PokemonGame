using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PokemonStorageBoxes : MonoBehaviour{
    Pokemon[,] boxes = new Pokemon[30,36];

    public static PokemonStorageBoxes GetPlayersStorageBoxes(){
        return FindFirstObjectByType<PlayerController>().GetComponent<PokemonStorageBoxes>();
    }

    public void AddPokemon(Pokemon pokemon, int boxIndex, int slotIndex){
        boxes[boxIndex, slotIndex] = pokemon;
    }
    
    public void RemovePokemon(int boxIndex, int slotIndex){
        boxes[boxIndex, slotIndex] = null;
    }
    
    public Pokemon GetPokemon(int boxIndex, int slotIndex){
        return boxes[boxIndex, slotIndex];
    }
}
