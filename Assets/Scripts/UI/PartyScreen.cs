using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PartyScreen : MonoBehaviour{
    [SerializeField] Text messageText;
    PartyMemberUI[] memberSlots;
    List<Pokemon> pokemons;
    
    public void Init(){
        memberSlots = GetComponentsInChildren<PartyMemberUI>(true);
    }

    public void SetPartyData(List<Pokemon> pokemons){
        this.pokemons = pokemons;
        for(int i = 0; i < memberSlots.Length; i++){
            if(i < pokemons.Count){
                memberSlots[i].SetData(pokemons[i]);
            } else {
                memberSlots[i].gameObject.SetActive(false);
            }
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
}