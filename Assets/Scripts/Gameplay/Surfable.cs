using UnityEngine;
using System.Linq;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class Surfable : MonoBehaviour,  Interactable, IPlayerTriggerable{
    bool IsJumpingToWater = false;

    public bool TriggerRepeatedly => true;


    public IEnumerator Interact(Transform initiator){
        var animator = initiator.GetComponent<CharacterAnimator>();
        if(animator.IsSurfing || IsJumpingToWater){
            yield break;
        }
        yield return DialogManager.i.ShowDialogText("The water is deep blue.");

        var pokemonWithSurf = initiator.GetComponent<PokemonParty>().Pokemons.FirstOrDefault( p => p.Moves.Any( m => m.Base.Name == "Surf"));

        if(pokemonWithSurf != null){
            int selectedChoice = 0;
            yield return DialogManager.i.ShowDialogText($"Should {pokemonWithSurf.Base.Name} use Surf?",
                                                        choices: new List<string>(){ "Yes", "No" },
                                                        onChoiceSelected: selection => selectedChoice = selection);
            if(selectedChoice == 0){
                yield return DialogManager.i.ShowDialogText($"{pokemonWithSurf.Base.Name} used Surf!");

                var dir = GetDirectionToWater(initiator);
                var targetPos = initiator.position + dir;

                if(IsValidWaterPosition(targetPos)){
                    IsJumpingToWater = true;
                    yield return initiator.DOJump(targetPos, 0.3f, 1, 0.5f).WaitForCompletion();
                    IsJumpingToWater = false;

                    var character = initiator.GetComponent<Character>();
                    if(character != null){
                        character.SetPositionAndSnapToTile(targetPos);
                    } else {
                        initiator.position = targetPos;
                    }
                    
                    animator.IsSurfing = true;
                    GameObject pokemonAnimatorObj = CreatePokemonAnimator(initiator, pokemonWithSurf.Base);
                } else {
                    yield return DialogManager.i.ShowDialogText("There's no water to surf on!");
                }
            }
        }
    }

    private Vector3 GetDirectionToWater(Transform initiator){
        var playerController = initiator.GetComponent<PlayerController>();
        if(playerController != null){
            return playerController.GetLastFacingDirection();
        }
        
        var animator = initiator.GetComponent<CharacterAnimator>();
        if(animator.MoveX != 0 || animator.MoveY != 0){
            return new Vector3(animator.MoveX, animator.MoveY, 0);
        }
        
        if(animator.LastMoveX != 0 || animator.LastMoveY != 0){
            return new Vector3(animator.LastMoveX, animator.LastMoveY, 0);
        }
        
        return Vector3.down;
    }

    private bool IsValidWaterPosition(Vector3 position){
        var waterCollider = Physics2D.OverlapCircle(position, 0.1f, GameLayers.i.WaterLayer);
        return waterCollider != null;
    }

    public void OnPlayerTriggered(PlayerController player){
        if(UnityEngine.Random.Range(1,101) <= 10){
            GameController.i.StartBattle(BattleTrigger.Water);
        }
    }


    GameObject CreatePokemonAnimator(Transform player, PokemonBase pokemon){
        GameObject pokemonAnimatorObj = new GameObject("PokemonAnimator");
        pokemonAnimatorObj.transform.SetParent(player);
        pokemonAnimatorObj.transform.localPosition = Vector3.zero;
        
        SpriteRenderer spriteRenderer = pokemonAnimatorObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = player.GetComponent<SpriteRenderer>().sortingOrder;
        
        PokemonAnimator pokemonAnimator = pokemonAnimatorObj.AddComponent<PokemonAnimator>();
        pokemonAnimator.SetSurferPokemon(pokemon);
        pokemonAnimator.StartSurfing();
        
        Character character = player.GetComponent<Character>();
        if (character != null){
            character.SetPokemonAnimator(pokemonAnimator);
        }
        
        return pokemonAnimatorObj;
    }
}
