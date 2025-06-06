using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public class PartyScreen : MonoBehaviour{
    [SerializeField] Text messageText;
    PartyMemberUI[] memberSlots;

    List<Pokemon> pokemons;
    BattleState? prevState;
    PokemonParty party;
    int selection = 0;


    public BattleState? CallFrom { get; set;}
    public Pokemon SelectedMember => pokemons[selection];
    
    public void Init(){
        memberSlots = GetComponentsInChildren<PartyMemberUI>(true);
        party = PokemonParty.GetPlayerParty();
        SetPartyData();

        party.OnUpdated += SetPartyData;
    }

    public void SetPartyData(){
        pokemons = party.Pokemons;

        for(int i = 0; i < memberSlots.Length; i++){
            if(i < pokemons.Count){
                memberSlots[i].gameObject.SetActive(true);
                memberSlots[i].SetData(pokemons[i]);
            } else {
                memberSlots[i].gameObject.SetActive(false);
            }
            UpdateMemberSelection(selection);
        }
    }

    public void UpdateMemberSelection(int selectedMember){
        
        for(int i = 0; i < memberSlots.Length; i++){
            if(i == selectedMember){
                memberSlots[i].SetSelected(true);
            } else {
                memberSlots[i].SetSelected(false);
            }
        }
    }

    public void SetMessageText(string message){
        messageText.text = message;
    }

    public void HandleUpdate(Action onSelected, Action onBack){
        var prevSelection = selection;

        if(Input.GetKeyDown(KeyCode.DownArrow)){
            selection += 2;
        } else if(Input.GetKeyDown(KeyCode.UpArrow)){
            selection -= 2;
        } else if(Input.GetKeyDown(KeyCode.RightArrow)){
            selection++;
        } else if(Input.GetKeyDown(KeyCode.LeftArrow)){
            selection--;
        }

        selection = Mathf.Clamp(selection, 0, pokemons.Count - 1);
        if(selection != prevSelection){
            UpdateMemberSelection(selection);
        }

        if(Input.GetKeyDown(KeyCode.Return)){
            onSelected?.Invoke();
        } else if(Input.GetKeyDown(KeyCode.Escape)){
            onBack?.Invoke();
        }
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
}