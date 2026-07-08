using System;
using UnityEngine;

public class PlayerRideController : MonoBehaviour {
    [Tooltip("Player controlled by this ride controller. Empty uses this GameObject or PlayerController.i.")]
    [SerializeField] PlayerController playerOverride;
    [Tooltip("Ride log used to save mount/dismount history. Empty uses or installs PlayerRideLog on the player.")]
    [SerializeField] PlayerRideLog rideLogOverride;
    [Tooltip("If enabled, PlayerRideLog is added automatically when missing.")]
    [SerializeField] bool autoInstallRideLog = true;
    [Tooltip("If enabled, original Character movement speeds are restored on dismount.")]
    [SerializeField] bool restoreSpeedOnDismount = true;
    [Tooltip("If enabled, runtime ride visual objects are destroyed on dismount.")]
    [SerializeField] bool destroyVisualOnDismount = true;
    [Tooltip("If enabled, mount/dismount/blocked events are published through GameEventBus.")]
    [SerializeField] bool publishEvents = true;
    [Tooltip("If enabled, short debug messages are written for mount/dismount attempts.")]
    [SerializeField] bool writeDebugLogs;

    RidePokemonDefinition activeRide;
    Pokemon activePokemon;
    GameObject activeVisual;
    SpriteRenderer playerRenderer;
    bool originalPlayerRendererEnabled = true;
    float originalMoveSpeed;
    float originalRunSpeed;
    bool hasOriginalSpeed;
    bool subscribedToMovement;

    public RidePokemonDefinition ActiveRide => activeRide;
    public Pokemon ActivePokemon => activePokemon;
    public bool IsMounted => activeRide != null;
    public event Action<RidePokemonDefinition, Pokemon> OnMounted;
    public event Action<RidePokemonDefinition, Pokemon, string> OnDismounted;

    void Awake() {
        ResolvePlayer();
        ResolveLog();
    }

    void OnEnable() {
        TrySubscribeToMovement();
    }

    void Start() {
        TrySubscribeToMovement();
    }

    void OnDisable() {
        UnsubscribeFromMovement();
    }

    public bool TryMount(RidePokemonDefinition ride, Pokemon selectedPokemon, string sourceId, out string failureMessage) {
        var player = ResolvePlayer();
        if(player == null) {
            failureMessage = "Player is missing.";
            return false;
        }

        if(ride == null) {
            failureMessage = "Ride definition is missing.";
            return false;
        }

        Pokemon pokemon = selectedPokemon;
        if(pokemon == null && ride.AutoSelectFirstUsablePokemon) {
            pokemon = ride.FindUsablePokemon(player, out _);
        }

        if(!ride.CanUse(player, pokemon, out failureMessage)) {
            ResolveLog()?.RecordBlocked(ride, pokemon, sourceId, failureMessage);
            PublishRideEvent(ride, pokemon, "blocked", failureMessage, ride.BlockedEvent, GameEventImportance.Warning, sourceId);
            WriteDebug($"Ride blocked: {ride.DisplayName} - {failureMessage}", warning: true);
            return false;
        }

        if(IsMounted) {
            Dismount("switch", out _);
        }

        activeRide = ride;
        activePokemon = pokemon;
        ApplyMovement(ride);
        ApplyAnimatorFlags(ride);
        CreateVisual(ride, player);
        ResolveLog()?.RecordMount(ride, pokemon, sourceId);
        TrySubscribeToMovement();
        PublishRideEvent(ride, pokemon, "mounted", $"{ride.DisplayName} mounted.", ride.MountedEvent, GameEventImportance.Success, sourceId);
        WriteDebug($"Ride mounted: {ride.DisplayName}");
        OnMounted?.Invoke(ride, pokemon);
        failureMessage = null;
        return true;
    }

    public bool TryMount(RidePokemonDefinition ride, out string failureMessage) {
        return TryMount(ride, null, "PlayerRideController", out failureMessage);
    }

    public bool ToggleRide(RidePokemonDefinition ride, Pokemon selectedPokemon, string sourceId, out string failureMessage) {
        if(activeRide != null && ride != null && activeRide.Id == ride.Id) {
            return Dismount(sourceId, out failureMessage);
        }

        return TryMount(ride, selectedPokemon, sourceId, out failureMessage);
    }

    public bool Dismount(string reason, out string failureMessage) {
        if(activeRide == null) {
            failureMessage = "No active ride.";
            return false;
        }

        var player = ResolvePlayer();
        var character = player != null ? player.Character : null;
        if(character != null && character.IsMoving && !activeRide.AllowDismountWhileMoving) {
            failureMessage = "Cannot dismount while moving.";
            return false;
        }

        var ride = activeRide;
        var pokemon = activePokemon;
        RestoreMovement(character);
        RestoreAnimatorFlags(character);
        ClearVisual();
        RestorePlayerSprite();
        ResolveLog()?.RecordDismount(reason, reason);
        activeRide = null;
        activePokemon = null;
        PublishRideEvent(ride, pokemon, "dismounted", $"{ride.DisplayName} dismounted.", ride.DismountedEvent, GameEventImportance.Info, reason);
        WriteDebug($"Ride dismounted: {ride.DisplayName} ({reason})");
        OnDismounted?.Invoke(ride, pokemon, reason);
        failureMessage = null;
        return true;
    }

    public bool CanMount(RidePokemonDefinition ride, Pokemon selectedPokemon, out string failureMessage) {
        var player = ResolvePlayer();
        if(ride == null) {
            failureMessage = "Ride definition is missing.";
            return false;
        }

        if(selectedPokemon == null && ride.AutoSelectFirstUsablePokemon) {
            selectedPokemon = ride.FindUsablePokemon(player, out _);
        }

        return ride.CanUse(player, selectedPokemon, out failureMessage);
    }

    void OnPlayerMoved(Vector3 position) {
        if(activeRide == null) {
            return;
        }

        var player = ResolvePlayer();
        if(activeRide.CanRemainMounted(player, out _)) {
            return;
        }

        Dismount("ride-rule", out _);
    }

    void ApplyMovement(RidePokemonDefinition ride) {
        var player = ResolvePlayer();
        var character = player != null ? player.Character : null;
        if(character == null) {
            return;
        }

        if(!hasOriginalSpeed) {
            originalMoveSpeed = character.movingSpeed;
            originalRunSpeed = character.runningSpeed;
            hasOriginalSpeed = true;
        }

        character.movingSpeed = originalMoveSpeed * ride.MoveSpeedMultiplier;
        character.runningSpeed = originalRunSpeed * ride.RunSpeedMultiplier;
    }

    void RestoreMovement(Character character) {
        if(!restoreSpeedOnDismount || character == null || !hasOriginalSpeed) {
            return;
        }

        character.movingSpeed = originalMoveSpeed;
        character.runningSpeed = originalRunSpeed;
        hasOriginalSpeed = false;
    }

    void ApplyAnimatorFlags(RidePokemonDefinition ride) {
        var player = ResolvePlayer();
        var animator = player != null && player.Character != null ? player.Character.Animator : null;
        if(animator != null && ride.SetCharacterSurfingFlag) {
            animator.IsSurfing = true;
        }
    }

    void RestoreAnimatorFlags(Character character) {
        var animator = character != null ? character.Animator : null;
        if(animator != null) {
            animator.IsSurfing = false;
        }
    }

    void CreateVisual(RidePokemonDefinition ride, PlayerController player) {
        if(ride.VisualMode == RideVisualMode.None || player == null) {
            return;
        }

        playerRenderer = playerRenderer != null ? playerRenderer : player.GetComponent<SpriteRenderer>();
        if(playerRenderer != null) {
            originalPlayerRendererEnabled = playerRenderer.enabled;
            if(ride.HidePlayerSprite) {
                playerRenderer.enabled = false;
            }
        }

        if(ride.RideVisualPrefab == null && (ride.DirectionalSprites == null || !ride.DirectionalSprites.HasAnySprite)) {
            return;
        }

        activeVisual = ride.RideVisualPrefab != null
            ? Instantiate(ride.RideVisualPrefab, player.transform)
            : new GameObject($"{ride.DisplayName} Visual");

        activeVisual.transform.SetParent(player.transform, false);
        var visualController = activeVisual.GetComponent<RideVisualController>();
        if(visualController == null) {
            visualController = activeVisual.AddComponent<RideVisualController>();
        }

        visualController.Bind(ride, player);
    }

    void ClearVisual() {
        if(activeVisual == null) {
            return;
        }

        if(destroyVisualOnDismount) {
            Destroy(activeVisual);
        } else {
            activeVisual.SetActive(false);
        }

        activeVisual = null;
    }

    void RestorePlayerSprite() {
        if(playerRenderer != null) {
            playerRenderer.enabled = originalPlayerRendererEnabled;
        }
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        playerOverride = GetComponent<PlayerController>();
        if(playerOverride == null) {
            playerOverride = PlayerController.i != null ? PlayerController.i : FindAnyObjectByType<PlayerController>();
        }

        return playerOverride;
    }

    PlayerRideLog ResolveLog() {
        if(rideLogOverride != null) {
            return rideLogOverride;
        }

        var player = ResolvePlayer();
        if(player == null) {
            return null;
        }

        rideLogOverride = player.GetComponent<PlayerRideLog>();
        if(rideLogOverride == null && autoInstallRideLog) {
            rideLogOverride = player.gameObject.AddComponent<PlayerRideLog>();
        }

        return rideLogOverride;
    }

    void TrySubscribeToMovement() {
        if(subscribedToMovement) {
            return;
        }

        var player = ResolvePlayer();
        if(player == null) {
            return;
        }

        player.OnMovedTile += OnPlayerMoved;
        subscribedToMovement = true;
    }

    void UnsubscribeFromMovement() {
        var player = ResolvePlayer();
        if(player != null && subscribedToMovement) {
            player.OnMovedTile -= OnPlayerMoved;
        }

        subscribedToMovement = false;
    }

    void PublishRideEvent(RidePokemonDefinition ride, Pokemon pokemon, string phase, string message, GameEventDefinition eventDefinition, GameEventImportance importance, string sourceId) {
        if(!publishEvents || ride == null) {
            return;
        }

        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"ride.{phase}.{ride.Id}",
            message,
            GameEventCategory.Transit,
            importance,
            this,
            "PlayerRideController",
            GameEventScope.Player,
            ride.ShowEventsInFeed,
            ride.WriteEventsToDebugLog,
            GameEventPublishing.Value("rideId", ride.Id),
            GameEventPublishing.Value("rideName", ride.DisplayName),
            GameEventPublishing.Value("rideMode", ride.RideMode),
            GameEventPublishing.Value("pokemonInstanceId", pokemon != null ? pokemon.InstanceId : string.Empty),
            GameEventPublishing.Value("pokemonName", pokemon != null ? pokemon.NickName : string.Empty),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", sourceId));
    }

    void WriteDebug(string message, bool warning = false) {
        if(!writeDebugLogs) {
            return;
        }

        if(warning) {
            GameDebug.Warning(message, GameDebugCategory.General, this, "PlayerRideController");
        } else {
            GameDebug.Step(message, GameDebugCategory.General, this, "PlayerRideController");
        }
    }
}
