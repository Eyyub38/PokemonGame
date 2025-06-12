using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Cuttable : MonoBehaviour, Interactable{
    public IEnumerator Interact(Transform initiator){
        yield return DialogManager.i.ShowDialogText("This object looks like  it can be cut.");

        var pokemonWithCut = initiator.GetComponent<PokemonParty>().Pokemons.FirstOrDefault( p => p.Moves.Any( m => m.Base.Name == "Cut"));

        if(pokemonWithCut != null){
            int selectedChoice = 0;
            yield return DialogManager.i.ShowDialogText($"Should {pokemonWithCut.Base.Name} use Cut?",
                                                        choices: new List<string>(){ "Yes", "No" },
                                                        onChoiceSelected: selection => selectedChoice = selection);
            if(selectedChoice == 0){
                yield return DialogManager.i.ShowDialogText($"{pokemonWithCut.Base.Name} used Cut!");
                gameObject.SetActive(false);
            }
        }
    }
}
