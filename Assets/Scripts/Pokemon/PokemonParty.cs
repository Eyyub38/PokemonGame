using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PokemonParty : MonoBehaviour{
    [SerializeField] List<Pokemon> pokemons;

    public event Action OnUpdated;

    public List<Pokemon> Pokemons{get { return pokemons; } set{ pokemons = value; OnUpdated?.Invoke();}}

    private void Awake(){
        foreach(var pokemon in pokemons){
            pokemon.Init();
        }
    }

    public Pokemon GetHealthyPokemon(){
        return pokemons.Where( x => x.HP > 0).FirstOrDefault();
    }

    public void AddPokemon(Pokemon newPokemon){
        if(pokemons.Count < 6){
            pokemons.Add(newPokemon);
            OnUpdated?.Invoke();
        } else {

        }
    }

    public static PokemonParty GetPlayerParty(){
        return FindObjectOfType<PlayerController>().GetComponent<PokemonParty>();
    }

    public IEnumerator CheckForEvolutions(){
        foreach(var pokemon in pokemons){
            var evolution = pokemon.CheckForEvolution();
            if(evolution != null){
                yield return DialogManager.i.ShowDialogText($"{pokemon.Base.Name} is evolving into {evolution.EvolvesInto.Name}!");
                pokemon.Evolve(evolution);
            }
        }
        OnUpdated?.Invoke();
    }
}
