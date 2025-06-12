using UnityEngine;
using System.Linq;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class Surfable : MonoBehaviour,  Interactable{
    public IEnumerator Interact(Transform initiator){
        yield return DialogManager.i.ShowDialogText("The water is deep blue.");

        var pokemonWithSurf = initiator.GetComponent<PokemonParty>().Pokemons.FirstOrDefault( p => p.Moves.Any( m => m.Base.Name == "Surf"));

        if(pokemonWithSurf != null){
            int selectedChoice = 0;
            yield return DialogManager.i.ShowDialogText($"Should {pokemonWithSurf.Base.Name} use Surf?",
                                                        choices: new List<string>(){ "Yes", "No" },
                                                        onChoiceSelected: selection => selectedChoice = selection);
            if(selectedChoice == 0){
                yield return DialogManager.i.ShowDialogText($"{pokemonWithSurf.Base.Name} used Surf!");

                var animator = initiator.GetComponent<CharacterAnimator>();
                var dir = new Vector3(animator.MoveX, animator.MoveY);
                var targetPos = initiator.position + dir;

                animator.IsSurfing = true;
                yield return initiator.DOJump(targetPos, 0.3f, 1, 0.5f).WaitForCompletion();
            }
        }
    }
}
