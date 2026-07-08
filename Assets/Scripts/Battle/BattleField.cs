using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BattleField{
    public WeatherCondition Weather { get; private set;}
    public int? WeatherDuration { get; set;}

    public int PlayerSpikes { get; set; }
    public int EnemySpikes { get; set; }
    public bool PlayerStealthRock { get; set; }
    public bool EnemyStealthRock { get; set; }

    public TerrainCondition Terrain { get; private set; }
    public int? TerrainDuration { get; set; }

    // Screens (player side)
    public int PlayerReflect { get; set; }
    public int PlayerLightScreen { get; set; }
    public int PlayerAuroraVeil { get; set; }

    // Screens (enemy side)
    public int EnemyReflect { get; set; }
    public int EnemyLightScreen { get; set; }
    public int EnemyAuroraVeil { get; set; }

    // Protect tracking (per-turn)
    public bool PlayerProtect { get; set; }
    public bool EnemyProtect { get; set; }
    public int PlayerProtectStreak { get; set; }
    public int EnemyProtectStreak { get; set; }
    public bool PlayerAttemptedProtect { get; private set; }
    public bool EnemyAttemptedProtect { get; private set; }

    public bool TrySetProtect(bool playerSide){
        if(playerSide){
            PlayerAttemptedProtect = true;
            var result = TrySetProtect(PlayerProtectStreak);
            PlayerProtect = PlayerProtect || result.succeeded;
            PlayerProtectStreak = result.streak;
            return result.succeeded;
        }

        EnemyAttemptedProtect = true;
        var enemyResult = TrySetProtect(EnemyProtectStreak);
        EnemyProtect = EnemyProtect || enemyResult.succeeded;
        EnemyProtectStreak = enemyResult.streak;
        return enemyResult.succeeded;
    }

    (bool succeeded, int streak) TrySetProtect(int protectStreak){
        float successChance = protectStreak == 0 ? 1f : Mathf.Pow(1f / 3f, protectStreak);
        bool succeeded = Random.value <= successChance;

        if(succeeded){
            protectStreak++;
        } else {
            protectStreak = 0;
        }

        return (succeeded, protectStreak);
    }

    public void TickScreens(){
        if(PlayerReflect > 0) PlayerReflect--;
        if(PlayerLightScreen > 0) PlayerLightScreen--;
        if(PlayerAuroraVeil > 0) PlayerAuroraVeil--;
        if(EnemyReflect > 0) EnemyReflect--;
        if(EnemyLightScreen > 0) EnemyLightScreen--;
        if(EnemyAuroraVeil > 0) EnemyAuroraVeil--;
        PlayerProtect = false;
        EnemyProtect = false;

        if(!PlayerAttemptedProtect) PlayerProtectStreak = 0;
        if(!EnemyAttemptedProtect) EnemyProtectStreak = 0;

        PlayerAttemptedProtect = false;
        EnemyAttemptedProtect = false;
    }

    public void SetWeather(WeatherConditionID weatherID, int? weatherDuration = null){
        if(weatherID == WeatherConditionID.None){
            Weather = null;
            
        } else {
            Weather = WeatherConditionsDB.Conditions[weatherID];
        }
        WeatherDuration = weatherDuration;
    }

    public void SetTerrain(TerrainID terrainID, int? terrainDuration = null){
        if(terrainID == TerrainID.None){
            Terrain = null;
        } else {
            Terrain = TerrainConditionsDB.Conditions[terrainID];
        }
        TerrainDuration = terrainDuration;
    }
}
