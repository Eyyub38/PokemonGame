using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PokemonStorageBoxes : MonoBehaviour, ISavable{
    const int numberOfBoxes = 30;
    const int numberOfSlotsPerBox = 36;

    Pokemon[,] boxes = new Pokemon[ numberOfBoxes, numberOfSlotsPerBox];

    public int NumberOfBoxes => numberOfBoxes;
    public int NumberOfSlotsPerBox => numberOfSlotsPerBox;

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
            for(int slotIndex = 0; slotIndex < numberOfSlotsPerBox; slotIndex++){
                if(boxes[boxIndex, slotIndex] == null){
                    boxes[boxIndex,slotIndex] = pokemon;
                    return;
                }
            }
        }
    }

    public object CaptureState(){
        var saveData = new BoxSaveData(){
            boxSlots = new List<BoxSlotSaveData>()
        };

        for(int box = 0; box < numberOfBoxes; box++){
            for(int slot = 0; slot < numberOfSlotsPerBox; slot++){
                if(boxes[box, slot] != null){
                    var boxSlot = new BoxSlotSaveData(){
                        pokemonData = boxes[box, slot].GetSaveData(),
                        boxIndex = box,
                        slotIndex = slot
                    };

                    saveData.boxSlots.Add(boxSlot);
                }
            }
        }
        
        return saveData;
    }

    public void RestoreState(object state){
        var saveData = state as BoxSaveData;

        for(int box = 0; box < numberOfBoxes; box++){
            for(int slot = 0; slot < numberOfSlotsPerBox; slot++){
                boxes[box,slot] = null;
            }
        }

        foreach(var slot in saveData.boxSlots){
            boxes[slot.boxIndex, slot.slotIndex] =new Pokemon(slot.pokemonData);
        }
    }
}

[System.Serializable]
public class BoxSaveData{
    public List<BoxSlotSaveData> boxSlots;
}

[System.Serializable]
public class BoxSlotSaveData{
    public PokemonSaveData pokemonData;
    public int boxIndex;
    public int slotIndex;
}