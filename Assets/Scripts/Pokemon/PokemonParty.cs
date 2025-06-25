using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PokemonParty : MonoBehaviour{
    [SerializeField] List<Pokemon> pokemons;

    PokemonStorageBoxes storageBoxes;

    public event Action OnUpdated;

    public List<Pokemon> Pokemons{get { return pokemons; } set{ pokemons = value; OnUpdated?.Invoke();}}

    private void Awake(){
        storageBoxes = GetComponent<PokemonStorageBoxes>();
        foreach(var pokemon in pokemons){
            pokemon.Init();
        }
    }

    public Pokemon GetHealthyPokemon(List<Pokemon> doNotInclude = null){
        var healthyPokemons = pokemons.Where( p => p.HP > 0).ToList();
        if(doNotInclude != null){
            healthyPokemons = healthyPokemons.Where(p => !doNotInclude.Contains(p)).ToList();
        }
        return healthyPokemons.FirstOrDefault();
    }
    
    public List<Pokemon> GetHealthyPokemons(int unitCount){
        return pokemons.Where( x => x.HP > 0).Take(unitCount).ToList();
    }

    public void AddPokemon(Pokemon newPokemon){
        if(pokemons.Count < 6){
            pokemons.Add(newPokemon);
            OnUpdated?.Invoke();
        } else {
            storageBoxes.AddPokemonToEmptySlot(newPokemon);
        }
    }

    public static PokemonParty GetPlayerParty(){
        return FindFirstObjectByType<PlayerController>().GetComponent<PokemonParty>();
    }

    public bool CheckForEvolutions(){
        return pokemons.Any( p => p.CheckForEvolution() != null);
    }

    public IEnumerator RunEvolution(){
        foreach(var pokemon in pokemons){
            var evolution = pokemon.CheckForEvolution();
            if(evolution != null && evolution.RequiredItem == null){
                yield return EvolutionState.i.Evolve(pokemon, evolution);
            }
        }
    }

    public void PartyUptaded(){
        OnUpdated?.Invoke();
    }
}
