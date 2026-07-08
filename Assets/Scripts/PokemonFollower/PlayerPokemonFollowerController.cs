using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPokemonFollowerController : MonoBehaviour {
    [Header("References")]
    [Tooltip("Player controlled by this follower controller. Empty uses this GameObject or PlayerController.i.")]
    [SerializeField] PlayerController playerOverride;
    [Tooltip("Catalog used to resolve Pokemon species into follower visuals.")]
    [SerializeField] PokemonFollowerCatalogDefinition catalog;
    [Tooltip("Follower log used to save selection/history. Empty uses or installs PlayerPokemonFollowerLog on the player.")]
    [SerializeField] PlayerPokemonFollowerLog followerLogOverride;
    [Tooltip("If enabled, PlayerPokemonFollowerLog is added automatically when missing.")]
    [SerializeField] bool autoInstallFollowerLog = true;

    [Header("Selection")]
    [Tooltip("How the active follower Pokemon is selected.")]
    [SerializeField] PokemonFollowerSelectionMode selectionMode = PokemonFollowerSelectionMode.PartySlot;
    [Tooltip("Party slot used when Selection Mode is Party Slot.")]
    [Min(0)]
    [SerializeField] int partySlotIndex;
    [Tooltip("If enabled, a missing/invalid Party Slot selection falls back to the first healthy Pokemon.")]
    [SerializeField] bool fallbackToFirstHealthy = true;
    [Tooltip("If enabled, the follower is refreshed when the party list changes.")]
    [SerializeField] bool refreshOnPartyChanged = true;
    [Tooltip("If enabled, the follower is spawned during Start.")]
    [SerializeField] bool spawnOnStart = true;
    [Tooltip("If enabled, follower visuals are hidden while PlayerRideController is mounted.")]
    [SerializeField] bool hideWhileMounted = true;

    [Header("Feedback")]
    [Tooltip("If enabled, mount/stop/blocked events are published through GameEventBus.")]
    [SerializeField] bool publishEvents = true;
    [Tooltip("If enabled, short debug messages are written for follower attempts.")]
    [SerializeField] bool writeDebugLogs;

    readonly Queue<Vector3> trail = new Queue<Vector3>();
    PlayerController player;
    PokemonParty party;
    PlayerPokemonFollowerLog followerLog;
    PlayerRideController rideController;
    GameObject activeVisual;
    PokemonFollowerVisualController visualController;
    Pokemon activePokemon;
    PokemonFollowerVisualDefinition activeDefinition;
    Coroutine moveRoutine;

    public Pokemon ActivePokemon => activePokemon;
    public PokemonFollowerVisualDefinition ActiveDefinition => activeDefinition;
    public bool HasFollower => activeVisual != null && activePokemon != null;

    void Awake() {
        ResolveReferences();
    }

    void OnEnable() {
        Subscribe();
    }

    void Start() {
        ResolveReferences();
        Subscribe();
        RestoreSelectionFromLog();
        if(spawnOnStart) {
            RefreshFollower("start");
        }
    }

    void OnDisable() {
        Unsubscribe();
        DestroyVisual(recordStop: false, sourceId: "disable");
    }

    void Update() {
        if(activeVisual != null && hideWhileMounted && rideController != null) {
            activeVisual.SetActive(!rideController.IsMounted);
        }
    }

    public bool SetFollowerPartySlot(int slotIndex, out string failureMessage) {
        selectionMode = PokemonFollowerSelectionMode.PartySlot;
        partySlotIndex = Mathf.Max(0, slotIndex);
        SaveSelection();
        return RefreshFollower("party-slot", out failureMessage);
    }

    public bool FollowFirstHealthy(out string failureMessage) {
        selectionMode = PokemonFollowerSelectionMode.FirstHealthyPokemon;
        SaveSelection();
        return RefreshFollower("first-healthy", out failureMessage);
    }

    public bool DisableFollower(out string failureMessage) {
        selectionMode = PokemonFollowerSelectionMode.Disabled;
        SaveSelection();
        DestroyVisual(recordStop: true, sourceId: "disabled");
        failureMessage = null;
        return true;
    }

    public bool ToggleFollower(out string failureMessage) {
        if(selectionMode != PokemonFollowerSelectionMode.Disabled && HasFollower) {
            return DisableFollower(out failureMessage);
        }

        if(selectionMode == PokemonFollowerSelectionMode.Disabled) {
            selectionMode = PokemonFollowerSelectionMode.PartySlot;
        }

        SaveSelection();
        return RefreshFollower("toggle", out failureMessage);
    }

    public bool RefreshFollower(string sourceId = "refresh") {
        return RefreshFollower(sourceId, out _);
    }

    public bool RefreshFollower(string sourceId, out string failureMessage) {
        ResolveReferences();

        if(selectionMode == PokemonFollowerSelectionMode.Disabled) {
            DestroyVisual(recordStop: true, sourceId: sourceId);
            failureMessage = null;
            return true;
        }

        var pokemon = ResolveSelectedPokemon();
        if(pokemon == null) {
            DestroyVisual(recordStop: true, sourceId: sourceId);
            failureMessage = "No Pokemon is available to follow.";
            RecordBlocked(null, null, sourceId, failureMessage);
            return false;
        }

        var definition = catalog != null ? catalog.FindDefinition(pokemon) : null;
        if(definition == null) {
            DestroyVisual(recordStop: true, sourceId: sourceId);
            failureMessage = $"{pokemon.NickName} has no follower visual definition.";
            RecordBlocked(pokemon, null, sourceId, failureMessage);
            return false;
        }

        if(!definition.CanFollow(player, pokemon, out failureMessage)) {
            DestroyVisual(recordStop: true, sourceId: sourceId);
            RecordBlocked(pokemon, definition, sourceId, failureMessage);
            return false;
        }

        if(activeVisual != null && activePokemon == pokemon && activeDefinition == definition) {
            failureMessage = null;
            return true;
        }

        DestroyVisual(recordStop: true, sourceId: sourceId);
        CreateVisual(pokemon, definition, sourceId);
        failureMessage = null;
        return true;
    }

    public bool CycleNextHealthy(out string failureMessage) {
        ResolveReferences();
        if(party == null || party.Pokemons == null || party.Pokemons.Count == 0) {
            failureMessage = "No party Pokemon found.";
            return false;
        }

        int start = Mathf.Clamp(partySlotIndex + 1, 0, party.Pokemons.Count);
        for(int offset = 0; offset < party.Pokemons.Count; offset++) {
            int index = (start + offset) % party.Pokemons.Count;
            var pokemon = party.Pokemons[index];
            if(pokemon != null && pokemon.HP > 0) {
                return SetFollowerPartySlot(index, out failureMessage);
            }
        }

        failureMessage = "No healthy Pokemon found.";
        return false;
    }

    void CreateVisual(Pokemon pokemon, PokemonFollowerVisualDefinition definition, string sourceId) {
        if(player == null || pokemon == null || definition == null) {
            return;
        }

        activePokemon = pokemon;
        activeDefinition = definition;
        trail.Clear();

        activeVisual = definition.VisualPrefab != null
            ? Instantiate(definition.VisualPrefab)
            : new GameObject($"{pokemon.NickName} Follower");

        activeVisual.transform.position = GetSnapPosition();
        activeVisual.transform.localScale = definition.VisualScale;
        visualController = activeVisual.GetComponent<PokemonFollowerVisualController>();
        if(visualController == null) {
            visualController = activeVisual.AddComponent<PokemonFollowerVisualController>();
        }
        visualController.Initialize(pokemon, definition, player);

        followerLog?.SaveSelection(selectionMode, partySlotIndex, pokemon);
        followerLog?.RecordStarted(pokemon, definition, sourceId);
        if(publishEvents) {
            definition.PublishFollowerEvent(pokemon, player, "started", this);
        }
        WriteDebug($"{pokemon.NickName} started following.");
    }

    void DestroyVisual(bool recordStop, string sourceId) {
        if(moveRoutine != null) {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        var stoppedPokemon = activePokemon;
        var stoppedDefinition = activeDefinition;
        if(activeVisual != null) {
            Destroy(activeVisual);
        }

        activeVisual = null;
        visualController = null;
        activePokemon = null;
        activeDefinition = null;
        trail.Clear();

        if(recordStop && stoppedPokemon != null) {
            followerLog?.RecordStopped(stoppedPokemon, stoppedDefinition, sourceId);
            if(publishEvents && stoppedDefinition != null) {
                stoppedDefinition.PublishFollowerEvent(stoppedPokemon, player, "stopped", this);
            }
        }
    }

    void OnPlayerMoved(Vector3 playerPosition) {
        if(activeVisual == null || activeDefinition == null || moveRoutine != null) {
            if(activeVisual != null) {
                trail.Enqueue(playerPosition);
            }
            return;
        }

        if(Vector3.Distance(activeVisual.transform.position, player.transform.position) > activeDefinition.TeleportDistance) {
            activeVisual.transform.position = GetSnapPosition();
            trail.Clear();
            return;
        }

        trail.Enqueue(playerPosition);
        if(trail.Count < activeDefinition.FollowDistanceTiles) {
            return;
        }

        var targetPosition = trail.Dequeue() + activeDefinition.VisualOffset;
        moveRoutine = StartCoroutine(MoveVisual(targetPosition));
    }

    IEnumerator MoveVisual(Vector3 targetPosition) {
        if(activeVisual == null) {
            moveRoutine = null;
            yield break;
        }

        var startPosition = activeVisual.transform.position;
        var moveVector = targetPosition - startPosition;
        visualController?.SetFacing(moveVector);
        visualController?.SetMoving(true);

        float speed = ResolveMoveSpeed();
        int iterations = 0;
        while(activeVisual != null && (targetPosition - activeVisual.transform.position).sqrMagnitude > 0.01f && iterations < 1000) {
            activeVisual.transform.position = Vector3.MoveTowards(activeVisual.transform.position, targetPosition, speed * Time.deltaTime);
            iterations++;
            yield return null;
        }

        if(activeVisual != null) {
            activeVisual.transform.position = targetPosition;
            visualController?.SetMoving(false);
        }

        moveRoutine = null;
    }

    Pokemon ResolveSelectedPokemon() {
        if(selectionMode == PokemonFollowerSelectionMode.Disabled) {
            return null;
        }

        ResolveReferences();
        if(party == null || party.Pokemons == null || party.Pokemons.Count == 0) {
            return null;
        }

        switch(selectionMode) {
            case PokemonFollowerSelectionMode.FirstPartyPokemon:
                return party.Pokemons[0];
            case PokemonFollowerSelectionMode.FirstHealthyPokemon:
                return party.GetHealthyPokemon();
            case PokemonFollowerSelectionMode.PartySlot:
                if(partySlotIndex >= 0 && partySlotIndex < party.Pokemons.Count && party.Pokemons[partySlotIndex] != null) {
                    return party.Pokemons[partySlotIndex];
                }
                return fallbackToFirstHealthy ? party.GetHealthyPokemon() : null;
            default:
                return null;
        }
    }

    Vector3 GetSnapPosition() {
        if(player == null) {
            return transform.position;
        }

        var facing = player.GetLastFacingDirection();
        if(facing == Vector3.zero) {
            facing = Vector3.down;
        }

        return player.transform.position - facing + (activeDefinition != null ? activeDefinition.VisualOffset : Vector3.zero);
    }

    float ResolveMoveSpeed() {
        float baseSpeed = player != null && player.Character != null ? player.Character.movingSpeed : 5f;
        float multiplier = activeDefinition != null ? activeDefinition.MoveSpeedMultiplier : 1f;
        return Mathf.Max(0.01f, baseSpeed * multiplier);
    }

    void RestoreSelectionFromLog() {
        followerLog = ResolveLog();
        if(followerLog == null) {
            return;
        }

        selectionMode = followerLog.SelectionMode;
        partySlotIndex = followerLog.PartySlotIndex;
    }

    void SaveSelection() {
        followerLog = ResolveLog();
        followerLog?.SaveSelection(selectionMode, partySlotIndex, ResolveSelectedPokemon());
    }

    void RecordBlocked(Pokemon pokemon, PokemonFollowerVisualDefinition definition, string sourceId, string reason) {
        followerLog?.RecordBlocked(pokemon, definition, sourceId, reason);
        WriteDebug(reason, warning: true);
    }

    void ResolveReferences() {
        player = playerOverride != null ? playerOverride : GetComponent<PlayerController>();
        player = player != null ? player : PlayerController.i;
        party = player != null ? player.GetComponent<PokemonParty>() : null;
        followerLog = ResolveLog();
        rideController = player != null ? player.GetComponent<PlayerRideController>() : null;
    }

    PlayerPokemonFollowerLog ResolveLog() {
        if(followerLogOverride != null) {
            return followerLogOverride;
        }

        var target = playerOverride != null ? playerOverride : GetComponent<PlayerController>();
        target = target != null ? target : PlayerController.i;
        if(target == null) {
            return null;
        }

        var log = target.GetComponent<PlayerPokemonFollowerLog>();
        if(log == null && autoInstallFollowerLog) {
            log = target.gameObject.AddComponent<PlayerPokemonFollowerLog>();
        }
        return log;
    }

    void Subscribe() {
        ResolveReferences();
        if(player != null) {
            player.OnMovedTile -= OnPlayerMoved;
            player.OnMovedTile += OnPlayerMoved;
        }

        if(party != null) {
            party.OnUpdated -= HandlePartyUpdated;
            party.OnUpdated += HandlePartyUpdated;
        }
    }

    void Unsubscribe() {
        if(player != null) {
            player.OnMovedTile -= OnPlayerMoved;
        }

        if(party != null) {
            party.OnUpdated -= HandlePartyUpdated;
        }
    }

    void HandlePartyUpdated() {
        if(refreshOnPartyChanged) {
            RefreshFollower("party-updated");
        }
    }

    void WriteDebug(string message, bool warning = false) {
        if(!writeDebugLogs || string.IsNullOrWhiteSpace(message)) {
            return;
        }

        if(warning) {
            GameDebug.Warning(message, GameDebugCategory.General, this, "PlayerPokemonFollowerController");
        } else {
            GameDebug.Success(message, GameDebugCategory.General, this, "PlayerPokemonFollowerController");
        }
    }
}
