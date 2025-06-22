using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GDEUtills.GenerciSelectionUI;

public class PokemonStorageUI : SelectionUI<ImageSlot>{
    [SerializeField] List<ImageSlot> boxSlots;
    
    PokemonParty party;
    PokemonStorageBoxes storageBoxes;
    int totalColumns = 7;

    List<BoxPartySlotUI> partySlots = new List<BoxPartySlotUI>();
    List<BoxStorageSlotUI> storageSlots = new List<BoxStorageSlotUI>();
   
    public int SelectedBox {get; private set;} = 0;

    void Awake(){
        foreach(var boxSlot in boxSlots){
            var storageSlot = boxSlot.GetComponent<BoxStorageSlotUI>();
            if(storageSlot != null){
                storageSlots.Add(storageSlot);
            } else {
                partySlots.Add(boxSlot.GetComponent<BoxPartySlotUI>());
            }
        }

        party = PokemonParty.GetPlayerParty();
        storageBoxes = PokemonStorageBoxes.GetPlayersStorageBoxes();
    }

    void Start(){
        SetItems(boxSlots);
        SetSelectionSettings(SelectionType.Grid, totalColumns);
    }

    public void SetDataInPartySlots(){
        for(int i = 0; i < partySlots.Count; i++){
            if(i < party.Pokemons.Count){
                partySlots[i].SetData(party.Pokemons[i]);
            } else {
                partySlots[i].ClearData();
            }
        }    
    }
    
    public void SetDataInStorageSlots(){
        for(int i = 0; i < storageSlots.Count; i++){
            var pokemon = storageBoxes.GetPokemon(SelectedBox,i);
            if(pokemon != null){
                storageSlots[i].SetData(pokemon);
            } else {
                storageSlots[i].ClearData();
            }
        }
    }

    public Pokemon TakePokemonFromSlot(int slotIndex){
        Pokemon pokemon;
        if(IsPartySlot(slotIndex)){
            int partyIndex = slotIndex / totalColumns;

            if(partyIndex >= party.Pokemons.Count){
                return null;
            }

            pokemon = party.Pokemons[partyIndex];
            party.Pokemons[partyIndex] = null;
        } else {
            int boxSlotIndex = slotIndex - (slotIndex / totalColumns + 1);
            pokemon = storageBoxes.GetPokemon(SelectedBox ,boxSlotIndex);
            storageBoxes.RemovePokemon(SelectedBox, boxSlotIndex);
        }

        return pokemon;
    }

    public void PutPokemonIntoSlot(Pokemon pokemon, int slotIndex){
        if(IsPartySlot(slotIndex)){
            int partyIndex = slotIndex / totalColumns;

            if(partyIndex >= party.Pokemons.Count){
                party.Pokemons.Add(pokemon);
            } else {
                party.Pokemons[partyIndex] = pokemon;
            }
        } else {
            int boxSlotIndex = slotIndex - (slotIndex / totalColumns + 1);
            storageBoxes.AddPokemon(pokemon, SelectedBox, boxSlotIndex);
        }
    }

    public bool IsPartySlot(int slotIndex){
        return slotIndex % totalColumns == 0;
    }
}
