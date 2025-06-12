using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapArea : MonoBehaviour{
    [SerializeField] List<PokemonEncounterRecord> wildPokemons;
    [SerializeField] List<PokemonEncounterRecord> wildPokemonsInWater;

    [HideInInspector]
    [SerializeField] int totalChance = 0;
    
    [HideInInspector]
    [SerializeField] int totalChanceWater = 0;

    private void OnValidate(){
        CalculateCahncePercentage();
    }

    private void Start(){
        CalculateCahncePercentage();
    }

    Gender SetPokemonGender(PokemonBase pokemon){
        if(pokemon.IsGenderless){
            return Gender.Genderless;
        } else {
            return (Random.Range(1, 101)) < (pokemon.MaleRatio * 100) ? Gender.Male : Gender.Female;
        }
    }

    void CalculateCahncePercentage(){
        totalChance = -1;
        totalChanceWater = -1;

        if(wildPokemons.Count > 0){
            foreach(var record in wildPokemons){
                record.chanceLower = totalChance;
                record.chanceUpper = totalChance + record.chancePercentage;

                totalChance += record.chancePercentage;
            }
        }
        
        if(wildPokemonsInWater.Count > 0){
            foreach(var record in wildPokemonsInWater){
                record.chanceLower = totalChanceWater;
                record.chanceUpper = totalChanceWater + record.chancePercentage;

                totalChanceWater += record.chancePercentage;
            }
        } 
    }

    public Pokemon GetRandomWildPokemon(BattleTrigger trigger){
        var pokemonList = (trigger == BattleTrigger.LongGrass) ? wildPokemons : wildPokemonsInWater;
        int randVal = Random.Range(1, 101);
        var pokemonRecord = pokemonList.First( p => randVal >= p.chanceLower && randVal <= p.chanceUpper);

        var levelRange =pokemonRecord.levelRange;
        int level = (int)((levelRange.y == 0) ? levelRange.x : Random.Range(levelRange.x, levelRange.y + 1));

        var wildPokemon = new Pokemon(pokemonRecord.pokemon, level);
        wildPokemon.Init();
        wildPokemon.Gender = SetPokemonGender(wildPokemon.Base);
        return wildPokemon;
    }
}

[System.Serializable]
public class PokemonEncounterRecord{
    public PokemonBase pokemon;
    public Vector2 levelRange;
    public int chancePercentage;

    public int chanceUpper {get; set;}
    public int chanceLower {get; set;}
}
