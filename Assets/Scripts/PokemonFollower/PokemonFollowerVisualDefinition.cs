using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokemonFollowerFallbackSpriteMode {
    Icon,
    Front,
    Back
}

[CreateAssetMenu(menuName = "Pokemon Follower/Follower Visual Definition")]
public class PokemonFollowerVisualDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this follower visual. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note for this follower visual setup.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as small, large, ground, water, flying or starter.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Pokemon Match")]
    [Tooltip("If enabled, this visual can be used by any Pokemon that passes requirement checks.")]
    [SerializeField] bool allowAnyPokemon;
    [Tooltip("Exact species this visual is meant for. Leave empty only when Allow Any Pokemon or type filters are used.")]
    [SerializeField] PokemonBase pokemon;
    [Tooltip("Pokemon types that can use this visual. Empty means no type filter.")]
    [SerializeField] List<PokemonType> allowedTypes = new List<PokemonType>();

    [Header("Requirements")]
    [Tooltip("If enabled, fainted Pokemon cannot follow the player.")]
    [SerializeField] bool requireHealthyPokemon = true;
    [Tooltip("Minimum level required before this Pokemon can follow.")]
    [Min(1)]
    [SerializeField] int minimumLevel = 1;
    [Tooltip("Minimum friendship required before this Pokemon can follow.")]
    [Range(0, 255)]
    [SerializeField] int minimumFriendship;
    [Tooltip("Titles, badges, permits or licenses required before this follower can be used.")]
    [SerializeField] List<TitleDefinition> requiredTitles = new List<TitleDefinition>();
    [Tooltip("Milestones required before this follower can be used.")]
    [SerializeField] List<MilestoneDefinition> requiredMilestones = new List<MilestoneDefinition>();
    [Tooltip("Additional reusable requirements checked before this follower can be used.")]
    [SerializeField] List<ActivityRequirement> extraRequirements = new List<ActivityRequirement>();

    [Header("Visual")]
    [Tooltip("Optional prefab spawned as the follower. It can contain SpriteRenderer, Animator, colliders or custom scripts.")]
    [SerializeField] GameObject visualPrefab;
    [Tooltip("Directional idle/move sprites used when the prefab has a SpriteRenderer or no prefab is assigned.")]
    [SerializeField] PokemonFollowerDirectionalSpriteSet directionalSprites = new PokemonFollowerDirectionalSpriteSet();
    [Tooltip("If no directional sprite exists, this PokemonBase sprite is used as a fallback.")]
    [SerializeField] PokemonFollowerFallbackSpriteMode fallbackSpriteMode = PokemonFollowerFallbackSpriteMode.Icon;
    [Tooltip("World-space offset applied to the follower visual after it moves to a tile.")]
    [SerializeField] Vector3 visualOffset = Vector3.zero;
    [Tooltip("Local scale applied to spawned visual objects.")]
    [SerializeField] Vector3 visualScale = Vector3.one;
    [Tooltip("If set, the follower SpriteRenderer is moved to this sorting layer.")]
    [SerializeField] string sortingLayerName = "Objects";
    [Tooltip("Sorting order added to the player's SpriteRenderer order for the follower visual.")]
    [SerializeField] int sortingOrderOffset = -1;

    [Header("Movement")]
    [Tooltip("How many player tile movements the follower stays behind.")]
    [Min(1)]
    [SerializeField] int followDistanceTiles = 2;
    [Tooltip("If farther than this distance from the player, the follower snaps near them.")]
    [Min(1f)]
    [SerializeField] float teleportDistance = 8f;
    [Tooltip("Multiplier applied to the player's current movement speed while the follower catches up.")]
    [Min(0.01f)]
    [SerializeField] float moveSpeedMultiplier = 1f;

    [Header("Events")]
    [Tooltip("Optional event published when this Pokemon starts following.")]
    [SerializeField] GameEventDefinition startedFollowingEvent;
    [Tooltip("Optional event published when this Pokemon stops following.")]
    [SerializeField] GameEventDefinition stoppedFollowingEvent;
    [Tooltip("If enabled, follower events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, follower events are also written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public bool AllowAnyPokemon => allowAnyPokemon;
    public PokemonBase Pokemon => pokemon;
    public IReadOnlyList<PokemonType> AllowedTypes => allowedTypes != null ? (IReadOnlyList<PokemonType>)allowedTypes : Array.Empty<PokemonType>();
    public bool RequireHealthyPokemon => requireHealthyPokemon;
    public int MinimumLevel => Mathf.Max(1, minimumLevel);
    public int MinimumFriendship => Mathf.Clamp(minimumFriendship, 0, 255);
    public IReadOnlyList<TitleDefinition> RequiredTitles => requiredTitles != null ? (IReadOnlyList<TitleDefinition>)requiredTitles : Array.Empty<TitleDefinition>();
    public IReadOnlyList<MilestoneDefinition> RequiredMilestones => requiredMilestones != null ? (IReadOnlyList<MilestoneDefinition>)requiredMilestones : Array.Empty<MilestoneDefinition>();
    public IReadOnlyList<ActivityRequirement> ExtraRequirements => extraRequirements != null ? (IReadOnlyList<ActivityRequirement>)extraRequirements : Array.Empty<ActivityRequirement>();
    public GameObject VisualPrefab => visualPrefab;
    public PokemonFollowerDirectionalSpriteSet DirectionalSprites => directionalSprites;
    public Vector3 VisualOffset => visualOffset;
    public Vector3 VisualScale => visualScale == Vector3.zero ? Vector3.one : visualScale;
    public string SortingLayerName => sortingLayerName;
    public int SortingOrderOffset => sortingOrderOffset;
    public int FollowDistanceTiles => Mathf.Max(1, followDistanceTiles);
    public float TeleportDistance => Mathf.Max(1f, teleportDistance);
    public float MoveSpeedMultiplier => Mathf.Max(0.01f, moveSpeedMultiplier);
    public GameEventDefinition StartedFollowingEvent => startedFollowingEvent;
    public GameEventDefinition StoppedFollowingEvent => stoppedFollowingEvent;
    public bool ShowEventsInFeed => showEventsInFeed;
    public bool WriteEventsToDebugLog => writeEventsToDebugLog;

    public bool Matches(Pokemon candidate) {
        if(candidate == null || candidate.Base == null) {
            return false;
        }

        if(allowAnyPokemon) {
            return true;
        }

        if(pokemon != null && candidate.OriginalBase == pokemon) {
            return true;
        }

        if(allowedTypes != null && allowedTypes.Count > 0) {
            return allowedTypes.Any(type => candidate.HasType(type));
        }

        return false;
    }

    public bool CanFollow(PlayerController player, Pokemon candidate, out string failureMessage) {
        if(candidate == null || candidate.Base == null) {
            failureMessage = "Pokemon data is missing.";
            return false;
        }

        if(!Matches(candidate)) {
            failureMessage = $"{candidate.NickName} has no matching follower visual.";
            return false;
        }

        if(requireHealthyPokemon && candidate.HP <= 0) {
            failureMessage = $"{candidate.NickName} cannot follow right now.";
            return false;
        }

        if(candidate.Level < MinimumLevel) {
            failureMessage = $"{candidate.NickName} needs to be at least level {MinimumLevel} to follow.";
            return false;
        }

        if(candidate.Friendship < MinimumFriendship) {
            failureMessage = $"{candidate.NickName} does not trust you enough to follow.";
            return false;
        }

        var titles = player != null ? player.GetComponent<PlayerTitles>() : null;
        foreach(var title in RequiredTitles) {
            if(title != null && (titles == null || !titles.HasTitle(title))) {
                failureMessage = $"{title.DisplayName} is required before {candidate.NickName} can follow.";
                return false;
            }
        }

        var milestones = player != null ? player.GetComponent<PlayerMilestones>() : null;
        foreach(var milestone in RequiredMilestones) {
            if(milestone != null && (milestones == null || !milestones.HasMilestone(milestone))) {
                failureMessage = $"{milestone.DisplayName} is required before {candidate.NickName} can follow.";
                return false;
            }
        }

        foreach(var requirement in ExtraRequirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public Sprite ResolveFallbackSprite(Pokemon candidate) {
        var baseData = candidate != null ? candidate.Base : pokemon;
        if(baseData == null) {
            return null;
        }

        return fallbackSpriteMode switch {
            PokemonFollowerFallbackSpriteMode.Front => baseData.FrontSprite,
            PokemonFollowerFallbackSpriteMode.Back => baseData.BackSprite,
            _ => baseData.IconSprite != null ? baseData.IconSprite : baseData.FrontSprite
        };
    }

    public bool HasTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag) || tags == null) {
            return false;
        }

        return tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public void PublishFollowerEvent(Pokemon candidate, PlayerController player, string phase, UnityEngine.Object context) {
        var eventDefinition = phase == "started" ? startedFollowingEvent : stoppedFollowingEvent;
        string pokemonName = candidate != null ? candidate.NickName : DisplayName;
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"pokemon.follower.{phase}.{Id}",
            $"{pokemonName} {phase} following.",
            GameEventCategory.Companion,
            phase == "started" ? GameEventImportance.Success : GameEventImportance.Info,
            context != null ? context : player,
            "PokemonFollowerVisualDefinition",
            GameEventScope.Player,
            showEventsInFeed,
            writeEventsToDebugLog,
            GameEventPublishing.Value("followerId", Id),
            GameEventPublishing.Value("pokemon", pokemonName),
            GameEventPublishing.Value("phase", phase));
    }
}

[Serializable]
public class PokemonFollowerDirectionalSpriteSet {
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
    [Tooltip("Seconds between follower animation frames.")]
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
