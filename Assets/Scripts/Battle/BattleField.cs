using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BattleField{
    public WeatherCondition Weather { get; private set;}
    public int? WeatherDuration { get; set;}

    public void SetWeather(WeatherConditionID weatherID, int? weatherDuration = null){
        if(weatherID == WeatherConditionID.None){
            Weather = null;
            
        } else {
            Weather = WeatherConditionsDB.Conditions[weatherID];
        }
        WeatherDuration = weatherDuration;
    }
}
