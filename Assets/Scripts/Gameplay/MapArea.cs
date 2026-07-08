using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapArea : MonoBehaviour{
    [Header("Wild Pokemons")]
    [SerializeField] List<PokemonEncounterRecord> wildPokemons;
    [SerializeField] List<PokemonEncounterRecord> wildPokemonsInWater;

    [HideInInspector]
    [SerializeField] int totalChance = 0;
    
    [HideInInspector]
    [SerializeField] int totalChanceWater = 0;
    [Header("Weather")]
    [SerializeField] WeatherConditionID weather;

    public WeatherConditionID Weather => weather;

    private void OnValidate(){
        CalculateChancePercentage();
    }

    private void Start(){
        CalculateChancePercentage();
    }

    Gender SetPokemonGender(PokemonBase pokemon){
        if(pokemon.IsGenderless){
            return Gender.Genderless;
        } else {
            return (Random.value < pokemon.MaleRatio) ? Gender.Male : Gender.Female;
        }
    }

    void CalculateChancePercentage(){
        totalChance = 0;
        totalChanceWater = 0;

        if(wildPokemons != null && wildPokemons.Count > 0){
            foreach(var record in wildPokemons){
                record.chanceLower = totalChance;
                record.chanceUpper = totalChance + record.chancePercentage;

                totalChance += record.chancePercentage;
            }
        }
        
        if(wildPokemonsInWater != null && wildPokemonsInWater.Count > 0){
            foreach(var record in wildPokemonsInWater){
                record.chanceLower = totalChanceWater;
                record.chanceUpper = totalChanceWater + record.chancePercentage;

                totalChanceWater += record.chancePercentage;
            }
        } 
    }

    public Pokemon GetRandomWildPokemon(BattleTrigger trigger){
        var pokemonList = (trigger == BattleTrigger.LongGrass) ? wildPokemons : wildPokemonsInWater;
        int maxChance = (trigger == BattleTrigger.LongGrass) ? totalChance : totalChanceWater;
        if(pokemonList == null || pokemonList.Count == 0 || maxChance <= 0){
            Debug.LogError($"MapArea: No wild Pokemon configured for {trigger} in {name}.");
            return null;
        }

        int randVal = Random.Range(0, maxChance);
        var pokemonRecord = pokemonList.FirstOrDefault( p => randVal >= p.chanceLower && randVal < p.chanceUpper);
        if(pokemonRecord == null || pokemonRecord.pokemon == null){
            Debug.LogError($"MapArea: Encounter table for {trigger} in {name} has invalid chance ranges or Pokemon entries.");
            return null;
        }

        var levelRange = pokemonRecord.levelRange;
        int level = (int)((levelRange.y == 0) ? levelRange.x : Random.Range(levelRange.x, levelRange.y + 1));

        var wildPokemon = new Pokemon(pokemonRecord.pokemon, level);
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
