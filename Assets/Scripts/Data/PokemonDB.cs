using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PokemonDB{
    static Dictionary<string, PokemonBase> pokemons;

    public static void Init(){
        pokemons = new Dictionary<string, PokemonBase>();

        var pokemonArray = Resources.LoadAll<PokemonBase>("");
        foreach(var pokemon in pokemonArray){
            if(pokemons.ContainsKey(pokemon.Name)){
                continue;
            }
            pokemons[pokemon.Name] = pokemon;
        }
    }

    public static PokemonBase GetPokemonByName(string name){
        if(!pokemons.ContainsKey(name)){
            return null;
        }

        return pokemons[name];
    }
}