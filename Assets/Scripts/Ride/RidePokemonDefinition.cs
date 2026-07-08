using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokemonRideMode {
    Ground,
    FastRun,
    Surf,
    Fly,
    Dive,
    Climb,
    Custom
}

public enum PokemonRidePokemonMatchMode {
    AnyConfiguredFilter,
    AllConfiguredFilters
}

public enum RideVisualMode {
    None,
    PrefabOrSprites,
    Custom
}

[CreateAssetMenu(menuName = "Ride/Pokemon Ride Definition")]
public class RidePokemonDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this ride. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in UI, debug logs and future ride menus. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation for what this ride does.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as surf, fly, city, license, utility or legendary.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Optional icon used by future ride selection UI.")]
    [SerializeField] Sprite icon;

    [Header("Ride Type")]
    [Tooltip("Broad ride category. Runtime systems can use this to group movement and access rules.")]
    [SerializeField] PokemonRideMode rideMode = PokemonRideMode.Ground;
    [Tooltip("If enabled, the CharacterAnimator surfing flag is enabled while mounted. This also lets the current Character movement cross water.")]
    [SerializeField] bool setCharacterSurfingFlag;
    [Tooltip("If enabled, this ride is considered capable of crossing water even when custom movement checks read ride metadata.")]
    [SerializeField] bool canCrossWater;
    [Tooltip("If enabled, the ride dismounts when the CharacterAnimator surfing flag is cleared by existing movement code.")]
    [SerializeField] bool dismountWhenSurfingFlagClears = true;

    [Header("Pokemon Requirements")]
    [Tooltip("If enabled, the ride can be used without checking the party Pokemon filters below.")]
    [SerializeField] bool allowAnyPokemon;
    [Tooltip("If enabled, a usable Pokemon must exist in the player's current party.")]
    [SerializeField] bool requirePokemonInParty = true;
    [Tooltip("If enabled, the first matching party Pokemon is selected when no Pokemon is passed by UI/code.")]
    [SerializeField] bool autoSelectFirstUsablePokemon = true;
    [Tooltip("How species/type/move filters are combined when Allow Any Pokemon is disabled.")]
    [SerializeField] PokemonRidePokemonMatchMode pokemonMatchMode = PokemonRidePokemonMatchMode.AnyConfiguredFilter;
    [Tooltip("Exact Pokemon species that can use this ride.")]
    [SerializeField] List<PokemonBase> allowedPokemon = new List<PokemonBase>();
    [Tooltip("Pokemon types that can use this ride.")]
    [SerializeField] List<PokemonType> allowedTypes = new List<PokemonType>();
    [Tooltip("Optional move required on the selected Pokemon, such as Surf or Fly.")]
    [SerializeField] MoveBase requiredMove;
    [Tooltip("Minimum Pokemon level required to use this ride.")]
    [Min(1)]
    [SerializeField] int minimumLevel = 1;
    [Tooltip("Minimum friendship required to use this ride.")]
    [Min(0)]
    [SerializeField] int minimumFriendship;
    [Tooltip("If enabled, fainted Pokemon cannot be selected for this ride.")]
    [SerializeField] bool requireHealthyPokemon = true;

    [Header("Player Access")]
    [Tooltip("Titles, badges, permits or licenses required to use this ride.")]
    [SerializeField] List<TitleDefinition> requiredTitles = new List<TitleDefinition>();
    [Tooltip("Milestones that must be completed before this ride can be used.")]
    [SerializeField] List<MilestoneDefinition> requiredMilestones = new List<MilestoneDefinition>();
    [Tooltip("Optional current world region required to use this ride.")]
    [SerializeField] WorldRegionDefinition requiredCurrentRegion;
    [Tooltip("Additional reusable activity requirements that must pass before this ride can be used.")]
    [SerializeField] List<ActivityRequirement> extraRequirements = new List<ActivityRequirement>();
    [Tooltip("Fallback message shown when access checks fail without a more specific message.")]
    [SerializeField] string lockedMessage = "You cannot ride that right now.";

    [Header("Terrain")]
    [Tooltip("If enabled, required/blocked terrain masks are checked before mounting.")]
    [SerializeField] bool checkTerrainOnMount;
    [Tooltip("If enabled, required/blocked terrain masks are checked after each tile movement while mounted.")]
    [SerializeField] bool enforceTerrainWhileMounted;
    [Tooltip("Terrain layers that must be under the player to mount or remain mounted. Empty means no required terrain.")]
    [SerializeField] LayerMask requiredTerrainLayers;
    [Tooltip("Terrain layers that block mounting or force dismount while mounted. Empty means nothing is blocked by this ride definition.")]
    [SerializeField] LayerMask blockedTerrainLayers;
    [Tooltip("Radius used when checking terrain layers around the player.")]
    [Min(0.01f)]
    [SerializeField] float terrainCheckRadius = 0.3f;
    [Tooltip("Message shown when the current terrain does not match this ride's rules.")]
    [SerializeField] string terrainFailureMessage = "This ride cannot be used here.";

    [Header("Movement")]
    [Tooltip("Multiplier applied to Character.movingSpeed while mounted.")]
    [Min(0.01f)]
    [SerializeField] float moveSpeedMultiplier = 1f;
    [Tooltip("Multiplier applied to Character.runningSpeed while mounted.")]
    [Min(0.01f)]
    [SerializeField] float runSpeedMultiplier = 1f;
    [Tooltip("If disabled, dismount requests are blocked while the Character is currently moving between tiles.")]
    [SerializeField] bool allowDismountWhileMoving;

    [Header("Visual")]
    [Tooltip("How the ride visual should be created. None means this definition only changes movement/access state.")]
    [SerializeField] RideVisualMode visualMode = RideVisualMode.PrefabOrSprites;
    [Tooltip("Optional prefab spawned as a child of the player while mounted.")]
    [SerializeField] GameObject rideVisualPrefab;
    [Tooltip("If enabled, the player's SpriteRenderer is hidden while the ride is active.")]
    [SerializeField] bool hidePlayerSprite;
    [Tooltip("Optional directional ride sprites used when no prefab is assigned or the prefab has a SpriteRenderer.")]
    [SerializeField] RideDirectionalSpriteSet directionalSprites = new RideDirectionalSpriteSet();
    [Tooltip("Local visual offset by facing direction.")]
    [SerializeField] RideDirectionalOffset visualOffsets = new RideDirectionalOffset();
    [Tooltip("If set, the ride visual SpriteRenderer is moved to this sorting layer.")]
    [SerializeField] string sortingLayerName = string.Empty;
    [Tooltip("Sorting order added to the player's SpriteRenderer order for the ride visual.")]
    [SerializeField] int sortingOrderOffset = -1;

    [Header("Events")]
    [Tooltip("Optional event definition published when this ride is mounted.")]
    [SerializeField] GameEventDefinition mountedEvent;
    [Tooltip("Optional event definition published when this ride is dismounted.")]
    [SerializeField] GameEventDefinition dismountedEvent;
    [Tooltip("Optional event definition published when a mount attempt is blocked.")]
    [SerializeField] GameEventDefinition blockedEvent;
    [Tooltip("If enabled, ride mount/dismount events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, ride events are also written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public Sprite Icon => icon;
    public PokemonRideMode RideMode => rideMode;
    public bool SetCharacterSurfingFlag => setCharacterSurfingFlag || rideMode == PokemonRideMode.Surf || canCrossWater;
    public bool CanCrossWater => canCrossWater || rideMode == PokemonRideMode.Surf;
    public bool DismountWhenSurfingFlagClears => dismountWhenSurfingFlagClears;
    public bool AllowAnyPokemon => allowAnyPokemon;
    public bool RequirePokemonInParty => requirePokemonInParty;
    public bool AutoSelectFirstUsablePokemon => autoSelectFirstUsablePokemon;
    public PokemonRidePokemonMatchMode PokemonMatchMode => pokemonMatchMode;
    public IReadOnlyList<PokemonBase> AllowedPokemon => allowedPokemon != null ? (IReadOnlyList<PokemonBase>)allowedPokemon : Array.Empty<PokemonBase>();
    public IReadOnlyList<PokemonType> AllowedTypes => allowedTypes != null ? (IReadOnlyList<PokemonType>)allowedTypes : Array.Empty<PokemonType>();
    public MoveBase RequiredMove => requiredMove;
    public int MinimumLevel => Mathf.Max(1, minimumLevel);
    public int MinimumFriendship => Mathf.Max(0, minimumFriendship);
    public bool RequireHealthyPokemon => requireHealthyPokemon;
    public IReadOnlyList<TitleDefinition> RequiredTitles => requiredTitles != null ? (IReadOnlyList<TitleDefinition>)requiredTitles : Array.Empty<TitleDefinition>();
    public IReadOnlyList<MilestoneDefinition> RequiredMilestones => requiredMilestones != null ? (IReadOnlyList<MilestoneDefinition>)requiredMilestones : Array.Empty<MilestoneDefinition>();
    public WorldRegionDefinition RequiredCurrentRegion => requiredCurrentRegion;
    public IReadOnlyList<ActivityRequirement> ExtraRequirements => extraRequirements != null ? (IReadOnlyList<ActivityRequirement>)extraRequirements : Array.Empty<ActivityRequirement>();
    public bool CheckTerrainOnMount => checkTerrainOnMount;
    public bool EnforceTerrainWhileMounted => enforceTerrainWhileMounted;
    public LayerMask RequiredTerrainLayers => requiredTerrainLayers;
    public LayerMask BlockedTerrainLayers => blockedTerrainLayers;
    public float TerrainCheckRadius => Mathf.Max(0.01f, terrainCheckRadius);
    public float MoveSpeedMultiplier => Mathf.Max(0.01f, moveSpeedMultiplier);
    public float RunSpeedMultiplier => Mathf.Max(0.01f, runSpeedMultiplier);
    public bool AllowDismountWhileMoving => allowDismountWhileMoving;
    public RideVisualMode VisualMode => visualMode;
    public GameObject RideVisualPrefab => rideVisualPrefab;
    public bool HidePlayerSprite => hidePlayerSprite;
    public RideDirectionalSpriteSet DirectionalSprites => directionalSprites;
    public RideDirectionalOffset VisualOffsets => visualOffsets;
    public string SortingLayerName => sortingLayerName;
    public int SortingOrderOffset => sortingOrderOffset;
    public GameEventDefinition MountedEvent => mountedEvent;
    public GameEventDefinition DismountedEvent => dismountedEvent;
    public GameEventDefinition BlockedEvent => blockedEvent;
    public bool ShowEventsInFeed => showEventsInFeed;
    public bool WriteEventsToDebugLog => writeEventsToDebugLog;

    public bool CanUse(PlayerController player, Pokemon selectedPokemon, out string failureMessage) {
        if(player == null) {
            failureMessage = "Player is missing.";
            return false;
        }

        if(!CanAccessPlayerSystems(player, out failureMessage)) {
            return false;
        }

        Pokemon pokemon = selectedPokemon;
        if(requirePokemonInParty && pokemon == null && autoSelectFirstUsablePokemon) {
            pokemon = FindUsablePokemon(player, out _);
        }

        if(requirePokemonInParty) {
            if(pokemon == null) {
                failureMessage = "No usable Pokemon is available for this ride.";
                return false;
            }

            if(!CanUsePokemon(pokemon, out failureMessage)) {
                return false;
            }
        } else if(pokemon != null && !CanUsePokemon(pokemon, out failureMessage)) {
            return false;
        }

        if(checkTerrainOnMount && !IsTerrainAllowed(player.transform.position, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public Pokemon FindUsablePokemon(PlayerController player, out string failureMessage) {
        var party = player != null ? player.GetComponent<PokemonParty>() : null;
        if(party == null || party.Pokemons == null || party.Pokemons.Count == 0) {
            failureMessage = "No party Pokemon found.";
            return null;
        }

        foreach(var pokemon in party.Pokemons) {
            if(pokemon != null && CanUsePokemon(pokemon, out _)) {
                failureMessage = null;
                return pokemon;
            }
        }

        failureMessage = "No party Pokemon matches this ride.";
        return null;
    }

    public bool CanUsePokemon(Pokemon pokemon, out string failureMessage) {
        if(pokemon == null || pokemon.Base == null) {
            failureMessage = "Pokemon data is missing.";
            return false;
        }

        if(requireHealthyPokemon && pokemon.HP <= 0) {
            failureMessage = $"{pokemon.NickName} is unable to ride.";
            return false;
        }

        if(pokemon.Level < MinimumLevel) {
            failureMessage = $"{pokemon.NickName} needs to be at least level {MinimumLevel}.";
            return false;
        }

        if(pokemon.Friendship < MinimumFriendship) {
            failureMessage = $"{pokemon.NickName} does not trust you enough for this ride.";
            return false;
        }

        if(allowAnyPokemon) {
            failureMessage = null;
            return true;
        }

        if(!HasPokemonFilter()) {
            failureMessage = "This ride has no Pokemon filters configured.";
            return false;
        }

        bool speciesMatch = allowedPokemon != null && allowedPokemon.Any(species => species != null && species == pokemon.Base);
        bool typeMatch = allowedTypes != null && allowedTypes.Any(type => type != PokemonType.None && pokemon.HasType(type));
        bool moveMatch = requiredMove != null && pokemon.HasMove(requiredMove);

        if(pokemonMatchMode == PokemonRidePokemonMatchMode.AllConfiguredFilters) {
            bool passesSpecies = allowedPokemon == null || allowedPokemon.Count == 0 || speciesMatch;
            bool passesTypes = allowedTypes == null || allowedTypes.Count == 0 || typeMatch;
            bool passesMove = requiredMove == null || moveMatch;
            if(passesSpecies && passesTypes && passesMove) {
                failureMessage = null;
                return true;
            }
        } else if(speciesMatch || typeMatch || moveMatch) {
            failureMessage = null;
            return true;
        }

        failureMessage = $"{pokemon.NickName} cannot use {DisplayName}.";
        return false;
    }

    public bool CanRemainMounted(PlayerController player, out string failureMessage) {
        if(player == null) {
            failureMessage = "Player is missing.";
            return false;
        }

        var animator = player.Character != null ? player.Character.Animator : null;
        if(DismountWhenSurfingFlagClears && SetCharacterSurfingFlag && animator != null && !animator.IsSurfing) {
            failureMessage = "Ride state was cleared by movement.";
            return false;
        }

        if(enforceTerrainWhileMounted && !IsTerrainAllowed(player.transform.position, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool IsTerrainAllowed(Vector3 worldPosition, out string failureMessage) {
        if(blockedTerrainLayers.value != 0 && Physics2D.OverlapCircle(worldPosition, TerrainCheckRadius, blockedTerrainLayers) != null) {
            failureMessage = string.IsNullOrWhiteSpace(terrainFailureMessage) ? "This ride cannot be used here." : terrainFailureMessage;
            return false;
        }

        if(requiredTerrainLayers.value != 0 && Physics2D.OverlapCircle(worldPosition, TerrainCheckRadius, requiredTerrainLayers) == null) {
            failureMessage = string.IsNullOrWhiteSpace(terrainFailureMessage) ? "This ride cannot be used here." : terrainFailureMessage;
            return false;
        }

        failureMessage = null;
        return true;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasPokemonFilter() {
        return (allowedPokemon != null && allowedPokemon.Any(entry => entry != null))
            || (allowedTypes != null && allowedTypes.Any(type => type != PokemonType.None))
            || requiredMove != null;
    }

    bool CanAccessPlayerSystems(PlayerController player, out string failureMessage) {
        var titles = player.GetComponent<PlayerTitles>();
        foreach(var title in RequiredTitles) {
            if(title != null && (titles == null || !titles.HasTitle(title))) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {title.DisplayName}." : lockedMessage;
                return false;
            }
        }

        var milestones = player.GetComponent<PlayerMilestones>();
        foreach(var milestone in RequiredMilestones) {
            if(milestone != null && (milestones == null || !milestones.HasMilestone(milestone))) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You have not unlocked {milestone.DisplayName}." : lockedMessage;
                return false;
            }
        }

        if(requiredCurrentRegion != null) {
            var regionLog = player.GetComponent<PlayerWorldRegionLog>();
            if(regionLog == null || !regionLog.IsCurrentRegion(requiredCurrentRegion)) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"This ride requires {requiredCurrentRegion.DisplayName}." : lockedMessage;
                return false;
            }
        }

        foreach(var requirement in ExtraRequirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = string.IsNullOrWhiteSpace(requirement.FailureMessage) ? lockedMessage : requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }
}

[Serializable]
public class RideDirectionalOffset {
    [Tooltip("Local offset used while facing up.")]
    [SerializeField] Vector3 up = Vector3.zero;
    [Tooltip("Local offset used while facing down.")]
    [SerializeField] Vector3 down = Vector3.zero;
    [Tooltip("Local offset used while facing left.")]
    [SerializeField] Vector3 left = Vector3.zero;
    [Tooltip("Local offset used while facing right.")]
    [SerializeField] Vector3 right = Vector3.zero;

    public Vector3 GetOffset(FacingDirection direction) {
        return direction switch {
            FacingDirection.Up => up,
            FacingDirection.Left => left,
            FacingDirection.Right => right,
            _ => down
        };
    }
}

[Serializable]
public class RideDirectionalSpriteSet {
    [Header("Idle")]
    [Tooltip("Idle sprite used while facing up.")]
    [SerializeField] Sprite idleUp;
    [Tooltip("Idle sprite used while facing down.")]
    [SerializeField] Sprite idleDown;
    [Tooltip("Idle sprite used while facing left.")]
    [SerializeField] Sprite idleLeft;
    [Tooltip("Idle sprite used while facing right.")]
    [SerializeField] Sprite idleRight;

    [Header("Moving")]
    [Tooltip("Animation frames used while moving up.")]
    [SerializeField] List<Sprite> moveUp = new List<Sprite>();
    [Tooltip("Animation frames used while moving down.")]
    [SerializeField] List<Sprite> moveDown = new List<Sprite>();
    [Tooltip("Animation frames used while moving left.")]
    [SerializeField] List<Sprite> moveLeft = new List<Sprite>();
    [Tooltip("Animation frames used while moving right.")]
    [SerializeField] List<Sprite> moveRight = new List<Sprite>();
    [Tooltip("Seconds between ride visual animation frames.")]
    [Min(0.01f)]
    [SerializeField] float frameSeconds = 0.15f;

    public float FrameSeconds => Mathf.Max(0.01f, frameSeconds);
    public bool HasAnySprite => idleUp != null || idleDown != null || idleLeft != null || idleRight != null
        || HasFrames(moveUp) || HasFrames(moveDown) || HasFrames(moveLeft) || HasFrames(moveRight);

    public IReadOnlyList<Sprite> GetFrames(FacingDirection direction, bool moving) {
        var frames = moving ? GetMoveFrames(direction) : null;
        if(frames != null && frames.Count > 0) {
            return frames;
        }

        var idle = GetIdleSprite(direction);
        if(idle != null) {
            return new[] { idle };
        }

        frames = GetMoveFrames(direction);
        if(frames != null && frames.Count > 0) {
            return frames;
        }

        return Array.Empty<Sprite>();
    }

    Sprite GetIdleSprite(FacingDirection direction) {
        return direction switch {
            FacingDirection.Up => idleUp,
            FacingDirection.Left => idleLeft,
            FacingDirection.Right => idleRight,
            _ => idleDown
        };
    }

    List<Sprite> GetMoveFrames(FacingDirection direction) {
        return direction switch {
            FacingDirection.Up => moveUp,
            FacingDirection.Left => moveLeft,
            FacingDirection.Right => moveRight,
            _ => moveDown
        };
    }

    bool HasFrames(List<Sprite> frames) {
        return frames != null && frames.Any(frame => frame != null);
    }
}
