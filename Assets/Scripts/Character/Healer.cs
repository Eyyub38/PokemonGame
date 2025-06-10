using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Healer : MonoBehaviour{
    public IEnumerator Heal(Transform player, Dialog dialog){
        int selectedChoice = 0;
        yield return DialogManager.i.ShowDialogText("Welcome to the Pokemon Center.");
        yield return DialogManager.i.ShowDialogText("Would you like to arrange a treatment for your party?",
                                                    choices: new List<string> { "Yes, please", "No, thanks" }, 
                                                    onChoiceSelected: (choiceIndex) => selectedChoice = choiceIndex );

        if(selectedChoice == 0){
            yield return Fader.i.FadeIn(0.5f);
            
            var playerParty = player.GetComponent<PokemonParty>();
            playerParty.Pokemons.ForEach(p => p.Heal());
            playerParty.PartyUptaded();

            yield return Fader.i.FadeOut(0.5f);

            yield return DialogManager.i.ShowDialogText("Your pokemons are healthy again!");

        } else if(selectedChoice == 1) {
            yield return DialogManager.i.ShowDialogText("Come back when you need healing.");
        }
    }
}
