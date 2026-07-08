using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum EncounterSourceType {
    Any,
    Grass,
    Water,
    Tree,
    Roaming,
    Static,
    Cave,
    Fishing,
    HoneyTree,
    Event,
    Special
}

[CreateAssetMenu(menuName = "Encounters/Encounter Table Definition")]
public class EncounterTableDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this encounter table. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this encounter table.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Default source type for this table, such as Grass, Tree or Roaming.")]
    [SerializeField] EncounterSourceType sourceType = EncounterSourceType.Grass;
    [Tooltip("Free-form tags used by zones, requirements and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Roll Rules")]
    [Tooltip("Base chance for an encounter attempt to succeed. 10 means 10 percent.")]
    [Range(0f, 100f)]
    [SerializeField] float baseEncounterChancePercent = 10f;
    [Tooltip("Battle trigger used for battle background/audio when this table starts a battle.")]
    [SerializeField] BattleTrigger battleTrigger = BattleTrigger.LongGrass;
    [Tooltip("If enabled, encounters from this table can only roll while PlayerActivityContext says the current area allows wild activity.")]
    [SerializeField] bool requireWildActivityZone;
    [Tooltip("Optional message/debug note used when no valid encounter entry can be selected.")]
    [SerializeField] string noValidEncounterMessage = "No valid encounter is available here.";

    [Header("Entries")]
    [Tooltip("Weighted Pokemon entries for this encounter table.")]
    [SerializeField] List<EncounterTableEntry> entries = new List<EncounterTableEntry>();

    [Header("Events")]
    [Tooltip("Optional event published when this table starts a battle encounter.")]
    [SerializeField] GameEventDefinition encounterStartedEvent;
    [Tooltip("Optional event published when this table fails to roll an encounter.")]
    [SerializeField] GameEventDefinition noEncounterEvent;
    [Tooltip("If enabled, encounter events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed;
    [Tooltip("If enabled, encounter events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public EncounterSourceType SourceType => sourceType;
    public IReadOnlyList<string> Tags => tags;
    public float BaseEncounterChancePercent => Mathf.Clamp(baseEncounterChancePercent, 0f, 100f);
    public BattleTrigger BattleTrigger => battleTrigger;
    public bool RequireWildActivityZone => requireWildActivityZone;
    public string NoValidEncounterMessage => noValidEncounterMessage;
    public IReadOnlyList<EncounterTableEntry> Entries => entries;
    public GameEventDefinition EncounterStartedEvent => encounterStartedEvent;
    public GameEventDefinition NoEncounterEvent => noEncounterEvent;
    public bool ShowEventsInFeed => showEventsInFeed;
    public bool WriteEventsToDebugLog => writeEventsToDebugLog;

    public bool ShouldAttempt(float chanceMultiplier = 1f) {
        float chance = Mathf.Clamp(BaseEncounterChancePercent * Mathf.Max(0f, chanceMultiplier), 0f, 100f);
        return chance >= 100f || Random.value * 100f <= chance;
    }

    public bool RollPokemon(PlayerController player, out Pokemon pokemon, out EncounterTableEntry selectedEntry) {
        pokemon = null;
        selectedEntry = null;

        if(requireWildActivityZone && !PlayerActivityContext.HasActiveZoneType(ActivityZoneType.Wild)) {
            return false;
        }

        var validEntries = entries
            .Where(e => e != null && e.Pokemon != null && e.Weight > 0 && e.IsAvailable(player))
            .ToList();

        int totalWeight = validEntries.Sum(e => e.Weight);
        if(validEntries.Count == 0 || totalWeight <= 0) {
            return false;
        }

        int roll = Random.Range(0, totalWeight);
        int current = 0;
        foreach(var entry in validEntries) {
            current += entry.Weight;
            if(roll < current) {
                selectedEntry = entry;
                pokemon = entry.CreatePokemon();
                return pokemon != null;
            }
        }

        return false;
    }

    public bool HasTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag) || tags == null) {
            return false;
        }

        foreach(var entry in tags) {
            if(string.Equals(entry, tag, System.StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return false;
    }
}

[System.Serializable]
public class EncounterTableEntry {
    [Header("Pokemon")]
    [Tooltip("Pokemon species that can appear.")]
    [SerializeField] PokemonBase pokemon;
    [Tooltip("Minimum level rolled for this Pokemon.")]
    [Min(1)]
    [SerializeField] int minLevel = 2;
    [Tooltip("Maximum level rolled for this Pokemon.")]
    [Min(1)]
    [SerializeField] int maxLevel = 4;
    [Tooltip("Relative weight inside the table. Higher values appear more often.")]
    [Min(0)]
    [SerializeField] int weight = 10;

    [Header("Conditions")]
    [Tooltip("Allowed day periods. Empty means any time.")]
    [SerializeField] List<DayPeriod> allowedDayPeriods = new List<DayPeriod>();
    [Tooltip("Allowed map weather values. Empty means any weather.")]
    [SerializeField] List<WeatherConditionID> allowedWeather = new List<WeatherConditionID>();
    [Tooltip("Optional title, badge or permit required before this entry can appear.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Minimum trainer level required before this entry can appear.")]
    [Min(0)]
    [SerializeField] int minimumPlayerLevel;
    [Tooltip("Free-form tags used by future encounter filters.")]
    [SerializeField] List<string> tags = new List<string>();

    public PokemonBase Pokemon => pokemon;
    public int MinLevel => Mathf.Max(1, minLevel);
    public int MaxLevel => Mathf.Max(MinLevel, maxLevel);
    public int Weight => Mathf.Max(0, weight);
    public IReadOnlyList<DayPeriod> AllowedDayPeriods => allowedDayPeriods;
    public IReadOnlyList<WeatherConditionID> AllowedWeather => allowedWeather;
    public TitleDefinition RequiredTitle => requiredTitle;
    public int MinimumPlayerLevel => Mathf.Max(0, minimumPlayerLevel);
    public IReadOnlyList<string> Tags => tags;

    public bool IsAvailable(PlayerController player) {
        if(pokemon == null || Weight <= 0) {
            return false;
        }

        if(allowedDayPeriods != null && allowedDayPeriods.Count > 0) {
            var period = TimeSystem.i != null ? TimeSystem.i.CurrentPeriod : DayPeriod.None;
            if(!allowedDayPeriods.Contains(period)) {
                return false;
            }
        }

        if(allowedWeather != null && allowedWeather.Count > 0) {
            var weather = EncounterSystem.GetCurrentWeather();
            if(!allowedWeather.Contains(weather)) {
                return false;
            }
        }

        if(requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            return false;
        }

        if(minimumPlayerLevel > 0) {
            int playerLevel = player?.GetComponent<PlayerProgression>()?.Level ?? 0;
            if(playerLevel < minimumPlayerLevel) {
                return false;
            }
        }

        return true;
    }

    public Pokemon CreatePokemon() {
        if(pokemon == null) {
            return null;
        }

        int level = Random.Range(MinLevel, MaxLevel + 1);
        return new Pokemon(pokemon, level);
    }

    public bool HasTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag) || tags == null) {
            return false;
        }

        foreach(var entry in tags) {
            if(string.Equals(entry, tag, System.StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return false;
    }
}
