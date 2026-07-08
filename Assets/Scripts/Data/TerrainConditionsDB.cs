using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum TerrainID { None, Electric, Grassy, Misty, Psychic }

public class TerrainCondition {
    public TerrainID Id { get; set; }
    public string Name { get; set; }
    public string StartMessage { get; set; }
    public string EndMessage { get; set; }

    public System.Action<Pokemon> OnAfterTurn { get; set; }
}

public class TerrainConditionsDB {
    public static Dictionary<TerrainID, TerrainCondition> Conditions { get; private set; } = new Dictionary<TerrainID, TerrainCondition>() {
        {
            TerrainID.Electric,
            new TerrainCondition() {
                Id = TerrainID.Electric,
                Name = "Electric Terrain",
                StartMessage = "An electric current ran across the battlefield!",
                EndMessage = "The electricity disappeared from the battlefield."
            }
        },
        {
            TerrainID.Grassy,
            new TerrainCondition() {
                Id = TerrainID.Grassy,
                Name = "Grassy Terrain",
                StartMessage = "Grass grew all over the battlefield!",
                EndMessage = "The grass disappeared from the battlefield.",
                OnAfterTurn = (pokemon) => {
                    if (pokemon.HP < pokemon.MaxHp) {
                        int amount = pokemon.MaxHp / 16;
                        pokemon.IncreaseHP(amount);
                        pokemon.AddStatusEvent($"{pokemon.NickName} restored HP from Grassy Terrain!");
                    }
                }
            }
        },
        {
            TerrainID.Misty,
            new TerrainCondition() {
                Id = TerrainID.Misty,
                Name = "Misty Terrain",
                StartMessage = "Mist swirled around the battlefield!",
                EndMessage = "The mist disappeared from the battlefield."
            }
        },
        {
            TerrainID.Psychic,
            new TerrainCondition() {
                Id = TerrainID.Psychic,
                Name = "Psychic Terrain",
                StartMessage = "The battlefield was enveloped in weirdness!",
                EndMessage = "The weirdness disappeared from the battlefield."
            }
        }
    };
}
