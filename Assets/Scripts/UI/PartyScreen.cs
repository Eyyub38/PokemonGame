using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using GDEUtills.GenerciSelectionUI;

public class PartyScreen : SelectionUI<IconSlot>{
    [SerializeField] Text messageText;

    [Header("Input")]
    [SerializeField] SelectionInputFromActions selectionInput;
    
    PartyMemberUI[] memberSlots;
    List<Pokemon> pokemons;
    PokemonParty party;

    public Pokemon SelectedMember => (pokemons != null && pokemons.Count > 0) ? pokemons[Mathf.Clamp(selectedItem, 0, pokemons.Count - 1)] : null;

    public void Init(){
        memberSlots = GetComponentsInChildren<PartyMemberUI>(true);
        SetSelectionSettings(SelectionType.Grid, 2);
        party = PokemonParty.GetPlayerParty();
        SetPartyData();
        ClearMemberSlotMessage();

        party.OnUpdated += SetPartyData;
    }

    public void SetPartyData(){
        pokemons = party.Pokemons;
        ClearItems();

        for(int i = 0; i < memberSlots.Length; i++){
            if(i < pokemons.Count){
                memberSlots[i].gameObject.SetActive(true);
                memberSlots[i].SetData(pokemons[i]);
            } else {
                memberSlots[i].gameObject.SetActive(false);
            }
        }
        var iconSlots = memberSlots.Select(m => m.GetComponent<IconSlot>());
        SetItems(iconSlots.Take(pokemons.Count).ToList());
        selectedItem = Mathf.Clamp(selectedItem, 0, pokemons.Count - 1);
        UpdateSelectionInUI();

        messageText.text = "Choose a Pokemon";
    }

    public void SetMessageText(string message){
        messageText.text = message;
    }
    
    public void ShowIfTmUsable(TmItem tmItem){
        for(int i= 0; i < pokemons.Count; i++){
            string message = tmItem.CanBeTaught(pokemons[i])? "ABLE" : "NOT ABLE";
            memberSlots[i].SetMessageText($"{message}");
        }
    }
    
    public void ClearMemberSlotMessage(){
        for(int i= 0; i < pokemons.Count; i++){
            memberSlots[i].SetMessageText("");
        }
    }

    void OnEnable() {
        InputSource = selectionInput;
    }
}
