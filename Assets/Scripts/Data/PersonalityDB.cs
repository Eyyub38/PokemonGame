using System.Collections.Generic;
using UnityEngine;

public enum PersonalityID {
    Balanced,
    Brave,
    Timid,
    Curious,
    Calm,
    Aggressive,
    Gentle,
    Stubborn,
    Playful,
    Diligent,
    Relaxed
}

public enum PersonalityTrait {
    Courage,
    Sociability,
    Curiosity,
    Discipline,
    Aggression,
    Empathy
}

public class Personality {
    public string Name { get; set; }
    public string Description { get; set; }
    public Dictionary<PersonalityTrait, int> Traits { get; set; } = new Dictionary<PersonalityTrait, int>();
    public float FriendshipGainMultiplier { get; set; } = 1f;

    public int GetTrait(PersonalityTrait trait) {
        return Traits.TryGetValue(trait, out int value) ? value : 0;
    }

    public int ModifyFriendshipGain(int amount) {
        return Mathf.Max(1, Mathf.RoundToInt(amount * FriendshipGainMultiplier));
    }
}

public static class PersonalityDB {
    public static Dictionary<PersonalityID, Personality> Personalities { get; private set; } = new Dictionary<PersonalityID, Personality>() {
        { PersonalityID.Balanced, new Personality {
            Name = "Balanced",
            Description = "Steady and adaptable."
        }},
        { PersonalityID.Brave, new Personality {
            Name = "Brave",
            Description = "Faces danger head-on.",
            FriendshipGainMultiplier = 1.05f,
            Traits = new Dictionary<PersonalityTrait, int> {
                { PersonalityTrait.Courage, 2 },
                { PersonalityTrait.Discipline, 1 },
                { PersonalityTrait.Aggression, 1 }
            }
        }},
        { PersonalityID.Timid, new Personality {
            Name = "Timid",
            Description = "Careful and easily pressured.",
            FriendshipGainMultiplier = 0.95f,
            Traits = new Dictionary<PersonalityTrait, int> {
                { PersonalityTrait.Courage, -2 },
                { PersonalityTrait.Empathy, 1 },
                { PersonalityTrait.Discipline, 1 }
            }
        }},
        { PersonalityID.Curious, new Personality {
            Name = "Curious",
            Description = "Drawn to new places and ideas.",
            Traits = new Dictionary<PersonalityTrait, int> {
                { PersonalityTrait.Curiosity, 2 },
                { PersonalityTrait.Sociability, 1 },
                { PersonalityTrait.Discipline, -1 }
            }
        }},
        { PersonalityID.Calm, new Personality {
            Name = "Calm",
            Description = "Keeps a level head.",
            FriendshipGainMultiplier = 1.05f,
            Traits = new Dictionary<PersonalityTrait, int> {
                { PersonalityTrait.Aggression, -2 },
                { PersonalityTrait.Discipline, 1 },
                { PersonalityTrait.Empathy, 1 }
            }
        }},
        { PersonalityID.Aggressive, new Personality {
            Name = "Aggressive",
            Description = "Prefers direct action.",
            Traits = new Dictionary<PersonalityTrait, int> {
                { PersonalityTrait.Aggression, 2 },
                { PersonalityTrait.Courage, 1 },
                { PersonalityTrait.Empathy, -1 }
            }
        }},
        { PersonalityID.Gentle, new Personality {
            Name = "Gentle",
            Description = "Warm and cooperative.",
            FriendshipGainMultiplier = 1.15f,
            Traits = new Dictionary<PersonalityTrait, int> {
                { PersonalityTrait.Empathy, 2 },
                { PersonalityTrait.Sociability, 1 },
                { PersonalityTrait.Aggression, -1 }
            }
        }},
        { PersonalityID.Stubborn, new Personality {
            Name = "Stubborn",
            Description = "Hard to sway once decided.",
            Traits = new Dictionary<PersonalityTrait, int> {
                { PersonalityTrait.Discipline, 2 },
                { PersonalityTrait.Courage, 1 },
                { PersonalityTrait.Sociability, -1 }
            }
        }},
        { PersonalityID.Playful, new Personality {
            Name = "Playful",
            Description = "Energetic and social.",
            FriendshipGainMultiplier = 1.1f,
            Traits = new Dictionary<PersonalityTrait, int> {
                { PersonalityTrait.Sociability, 2 },
                { PersonalityTrait.Curiosity, 1 },
                { PersonalityTrait.Discipline, -1 }
            }
        }},
        { PersonalityID.Diligent, new Personality {
            Name = "Diligent",
            Description = "Focused and hardworking.",
            Traits = new Dictionary<PersonalityTrait, int> {
                { PersonalityTrait.Discipline, 2 },
                { PersonalityTrait.Curiosity, 1 },
                { PersonalityTrait.Sociability, -1 }
            }
        }},
        { PersonalityID.Relaxed, new Personality {
            Name = "Relaxed",
            Description = "Easygoing and hard to rattle.",
            FriendshipGainMultiplier = 1.05f,
            Traits = new Dictionary<PersonalityTrait, int> {
                { PersonalityTrait.Aggression, -1 },
                { PersonalityTrait.Courage, 1 },
                { PersonalityTrait.Empathy, 1 }
            }
        }}
    };

    public static PersonalityID GetRandomPersonalityID() {
        var values = System.Enum.GetValues(typeof(PersonalityID));
        return (PersonalityID)values.GetValue(Random.Range(1, values.Length));
    }
}
