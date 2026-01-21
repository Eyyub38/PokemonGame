using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using GDEUtills.GenerciSelectionUI;
using System.Linq;

public class PokemonStorageUI : SelectionUI<ImageSlot>{
    [SerializeField] List<ImageSlot> boxSlots;
    [SerializeField] Image movingPokemonImage;
    [SerializeField] Text boxNameText;
    
    PokemonParty party;
    PokemonStorageBoxes storageBoxes;
    int totalColumns = 7;

    List<BoxPartySlotUI> partySlots = new List<BoxPartySlotUI>();
    List<BoxStorageSlotUI> storageSlots = new List<BoxStorageSlotUI>();
    List<Image> boxSlotImages;
   
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
        movingPokemonImage.gameObject.SetActive(false);
    }

    void Start(){
        SetItems(boxSlots);
        SetSelectionSettings(SelectionType.Grid, totalColumns);

        boxSlotImages = boxSlots.Select(s => s.transform.GetChild(0).GetChild(0).GetComponent<Image>()).ToList();
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

    public override void HandleUpdate(){
        int prevSelectedBox = SelectedBox;
        if(Keyboard.current.qKey.isPressed){
            SelectedBox = (SelectedBox > 0) ? (SelectedBox - 1) : (SelectedBox = storageBoxes.NumberOfBoxes - 1);
        } else if(Keyboard.current.eKey.isPressed){
            SelectedBox = (SelectedBox + 1) %  storageBoxes.NumberOfBoxes;
        }

        if(SelectedBox != prevSelectedBox){
            SetDataInStorageSlots();
            UpdateSelectionInUI();
            return;
        }

        base.HandleUpdate();
    }

    public Pokemon TakePokemonFromSlot(int slotIndex){
        Pokemon pokemon;
        if(IsPartySlot(slotIndex)){
            int partyIndex = slotIndex / totalColumns;

            if(partyIndex >= party.Pokemons.Count){
                return null;
            }

            pokemon = party.Pokemons[partyIndex];
            if(pokemon == null) return null;

            party.Pokemons[partyIndex] = null;
        } else {
            int boxSlotIndex = slotIndex - (slotIndex / totalColumns + 1);
            pokemon = storageBoxes.GetPokemon(SelectedBox ,boxSlotIndex);
            if(pokemon == null) return null;

            storageBoxes.RemovePokemon(SelectedBox, boxSlotIndex);
        }

        movingPokemonImage.sprite = boxSlotImages[slotIndex].sprite;
        movingPokemonImage.transform.position = boxSlotImages[slotIndex].transform.position + (Vector3.up * 50f);
        boxSlotImages[slotIndex].color =  new Color( 1, 1, 1, 0);
        movingPokemonImage.gameObject.SetActive(true);

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

        movingPokemonImage.gameObject.SetActive(false);
    }

    public bool IsPartySlot(int slotIndex){
        return slotIndex % totalColumns == 0;
    }

    public override void UpdateSelectionInUI(){
        base.UpdateSelectionInUI();
        
        boxNameText.text = "Box " + (SelectedBox + 1);

        if(movingPokemonImage.gameObject.activeSelf){
            movingPokemonImage.transform.position = boxSlotImages[selectedItem].transform.position + (Vector3.up * 50f);
        }
    }
}
