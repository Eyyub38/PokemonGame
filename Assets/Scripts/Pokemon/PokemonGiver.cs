using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PokemonGiver : MonoBehaviour, ISavable{
    [SerializeField] Pokemon pokemonToGive;
    [SerializeField] Dialog dialog;

    bool used = false;

    public IEnumerator GivePokemon(PlayerController player){
        yield return DialogManager.i.ShowDialog(dialog);
        pokemonToGive.Init();
        player.GetComponent<PokemonParty>().AddPokemon(pokemonToGive);
        used = true;
        AudioManager.i.PlaySfx(AudioId.PokemonObtained, pauseMusic: true);
        yield return DialogManager.i.ShowDialogText($"{player.name} received {pokemonToGive.Base.name}");
    } 

    public bool CanBeGiven(){
        return pokemonToGive != null && !used;
    }

    public object CaptureState(){
        return used;
    }

    public void RestoreState(object state){
        used = (bool) state;
    }
}
