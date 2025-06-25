using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum WeatherConditionID {None, Sandstorm, Sunny, Rainy, Hail, Stormy, Foggy, StrongWind, Snowy}

public class WeatherConditionsDB{
    public static void Init(){
        foreach(var kvp in Conditions){
            var conditionID = kvp.Key;
            var condition = kvp.Value;

            condition.Id = conditionID;
        }
    }

    public static Dictionary<WeatherConditionID, WeatherCondition> Conditions = new Dictionary<WeatherConditionID, WeatherCondition>(){
        { WeatherConditionID.Sandstorm,
            new WeatherCondition(){
                Name = "Sandstorm",
                StartMessage = "A sandstorm is raging",
                StartByMoveMessage = "A sandstorm is brewed",
                EffectMessage = "The sandstorm is rages",
                EndMessage = "The sandStorm subsided",
                OnWeatherEffect = (Pokemon pokemon) => {
                    if(pokemon.HasType(PokemonType.Ground) || pokemon.HasType(PokemonType.Rock) || pokemon.HasType(PokemonType.Steel)){
                        return;
                    }
                    pokemon.DecreaseHP(Mathf.CeilToInt(pokemon.MaxHp / 16f));
                    pokemon.AddStatusEvet(StatusEventType.Damage, $"{pokemon.Base.name} was buffeted by the sandstorm");
                }
            }
        },
        { WeatherConditionID.Hail,
            new WeatherCondition(){
                Name = "Hail",
                StartMessage = "It's hailing",
                StartByMoveMessage = "It started to hail",
                EffectMessage = "The hail continues to fall",
                EndMessage = "The hail stopped",
                OnWeatherEffect = (Pokemon pokemon) => {
                    if(pokemon.HasType(PokemonType.Ice)){
                        return;
                    }

                    pokemon.DecreaseHP(Mathf.CeilToInt(pokemon.MaxHp / 16f));
                    pokemon.AddStatusEvet(StatusEventType.Damage, $"{pokemon.Base.name} was buffeted by the hail");
                }
            }
        },
        { WeatherConditionID.Rainy,
            new WeatherCondition(){
                Name = "Rainy",
                StartMessage = "It's rainig",
                StartByMoveMessage = "It started to rain",
                EffectMessage = "The rain continues to fall",
                EndMessage = "The rain stopped",
                OnDamageModify = (Move move) =>{
                    if(move.Base.Type == PokemonType.Water || move.Base.Type == PokemonType.Grass){
                        return 1.5f;
                    } else if(move.Base.Type == PokemonType.Fire){
                        return 0.5f;
                    } else {
                        return 1f;
                    }
                }
            }
        },
        { WeatherConditionID.Sunny,
            new WeatherCondition(){
                Name = "Sunny",
                StartMessage = "The sunlight is harsh",
                StartByMoveMessage = "The sunlight turned harsh",
                EffectMessage = "The sunlight is harsh",
                EndMessage = "The sunlight faded",
                OnDamageModify = (Move move) =>{
                    if(move.Base.Type == PokemonType.Fire){
                        return 1.5f;
                    } else if(move.Base.Type == PokemonType.Water || move.Base.Type == PokemonType.Ice){
                        return 0.5f;
                    } else {
                        return 1f;
                    }
                }
            }
        }
    };
}

public class WeatherCondition{
    public string Name { get; set;}
    public string StartMessage { get; set;}
    public string StartByMoveMessage { get; set;}
    public string EffectMessage { get; set;}
    public string EndMessage { get; set;}

    public WeatherConditionID Id { get; set;}

    public Action<Pokemon> OnWeatherEffect { get; set;}
    public Func<Move, float> OnDamageModify { get; set;}
}
