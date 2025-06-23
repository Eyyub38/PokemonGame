using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PokemonStorageBoxes : MonoBehaviour{
    const int numberOfBoxes = 30;
    const int numberOfSlotsPerBox = 36;

    Pokemon[,] boxes = new Pokemon[ numberOfBoxes, numberOfSlotsPerBox];

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

    public void AddPokemonToEmptySlot(Pokemon pokemon){
        for(int boxIndex = 0; boxIndex < numberOfBoxes; boxIndex++){

        }
    }
}
