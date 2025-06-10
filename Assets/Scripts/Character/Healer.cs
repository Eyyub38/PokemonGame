using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Healer : MonoBehaviour{
    public IEnumerator Heal(Transform player, Dialog dialog){
        int selectedChoice = 0;
        yield return DialogManager.i.ShowDialog(dialog, new List<string> { "Yes", "No" }, (choiceIndex) => selectedChoice = choiceIndex );

        if(selectedChoice == 0){
            yield return Fader.i.FadeIn(0.5f);
            
            var playerParty = player.GetComponent<PokemonParty>();
            playerParty.Pokemons.ForEach(p => p.Heal());
            playerParty.PartyUptaded();

            yield return DialogManager.i.ShowDialogText("Your party has been healed!");

            yield return Fader.i.FadeOut(0.5f);
        } else if(selectedChoice == 1) {
            yield return DialogManager.i.ShowDialogText("Come back when you need healing.");
        }
    }
}
