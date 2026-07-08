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
                    pokemon.AddStatusEvent(StatusEventType.Damage, $"{pokemon.Base.Name} was buffeted by the sandstorm");
                }
            }
        },
        { WeatherConditionID.Snowy,
            new WeatherCondition(){
                Name = "Snowy",
                StartMessage = "It's snowing",
                StartByMoveMessage = "It started to snow",
                EffectMessage = "The snow continues to fall",
                EndMessage = "The snow stopped",
                OnWeatherEffect = (Pokemon pokemon) => {
                    if(pokemon.HasType(PokemonType.Ice)){
                        return;
                    }

                    pokemon.DecreaseHP(Mathf.CeilToInt(pokemon.MaxHp / 16f));
                    pokemon.AddStatusEvent(StatusEventType.Damage, $"{pokemon.Base.Name} was buffeted by the snow");
                }
            }
        },
        { WeatherConditionID.Foggy,
            new WeatherCondition(){
                Name = "Foggy",
                StartMessage = "The fog is deep",
                StartByMoveMessage = "Fog is brewed",
                EffectMessage = "The fog is deep",
                EndMessage = "The fog subsided",
            }
        },
        { WeatherConditionID.Stormy,
            new WeatherCondition(){
                Name = "Stormy",
                StartMessage = "A storm is raging",
                StartByMoveMessage = "A storm is brewed",
                EffectMessage = "The storm is raging",
                EndMessage = "The storm subsided",
            }
        },
        { WeatherConditionID.StrongWind,
            new WeatherCondition(){
                Name = "Strong Wind",
                StartMessage = "The wind is strong",
                StartByMoveMessage = "A strong wind is brewed",
                EffectMessage = "The wind is strong",
                EndMessage = "The strong wind subsided",
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
                    pokemon.AddStatusEvent(StatusEventType.Damage, $"{pokemon.Base.Name} was buffeted by the hail");
                }
            }
        },
        { WeatherConditionID.Rainy,
            new WeatherCondition(){
                Name = "Rainy",
                StartMessage = "It's raining",
                StartByMoveMessage = "It started to rain",
                EffectMessage = "The rain continues to fall",
                EndMessage = "The rain stopped",
                OnDamageModify = (PokemonType moveType) =>{
                    if(moveType == PokemonType.Water || moveType == PokemonType.Grass){
                        return 1.5f;
                    } else if(moveType == PokemonType.Fire){
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
                OnDamageModify = (PokemonType moveType) =>{
                    if(moveType == PokemonType.Fire){
                        return 1.5f;
                    } else if(moveType == PokemonType.Water || moveType == PokemonType.Ice){
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
    public Func<PokemonType, float> OnDamageModify { get; set;}
}
