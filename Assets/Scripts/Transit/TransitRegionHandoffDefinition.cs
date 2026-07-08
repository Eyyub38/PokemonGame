using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum TransitRegionHandoffTrigger {
    Manual,
    AtStop,
    Disembark,
    JourneyCompleted
}

public enum TransitRegionHandoffPokemonSelection {
    None,
    FirstHealthy,
    PartySlot
}

[CreateAssetMenu(menuName = "Transit/Transit Region Handoff Definition")]
public class TransitRegionHandoffDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this transit-region handoff. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining how this handoff connects vehicle travel to regional travel.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as ferry, airport, train, league, regional-border or vehicle-exit.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Entries")]
    [Tooltip("Handoff entries that map transit journey states/stops to regional routes.")]
    [SerializeField] List<TransitRegionHandoffEntry> entries = new List<TransitRegionHandoffEntry>();
    [Tooltip("If enabled, rows blocked by journey state or regional route requirements are included in snapshots.")]
    [SerializeField] bool includeBlockedRows = true;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public IReadOnlyList<TransitRegionHandoffEntry> Entries => entries != null ? (IReadOnlyList<TransitRegionHandoffEntry>)entries : Array.Empty<TransitRegionHandoffEntry>();
    public bool IncludeBlockedRows => includeBlockedRows;

    public TransitRegionHandoffSnapshot BuildSnapshot(PlayerController player, RegionTravelManager manager, bool? includeBlockedOverride = null) {
        var journeyLog = player != null ? player.GetComponent<PlayerTransitJourneyLog>() : null;
        var regionLog = manager != null ? manager.ResolveLog(player) : player != null ? player.GetComponent<PlayerWorldRegionLog>() : null;
        var rows = new List<TransitRegionHandoffRow>();
        bool includeBlocked = includeBlockedOverride ?? includeBlockedRows;

        foreach(var entry in Entries) {
            if(entry == null) {
                continue;
            }

            var row = TransitRegionHandoffRow.FromEntry(entry, player, journeyLog, regionLog);
            if(row != null && (includeBlocked || row.canRun)) {
                rows.Add(row);
            }
        }

        rows = rows
            .OrderBy(row => row.priority)
            .ThenBy(row => row.displayName)
            .ToList();

        return new TransitRegionHandoffSnapshot {
            handoffId = Id,
            handoffName = DisplayName,
            description = description,
            activeJourneyId = journeyLog != null && journeyLog.ActiveJourney != null ? journeyLog.ActiveJourney.journeyId : string.Empty,
            activeJourneyName = journeyLog != null && journeyLog.ActiveJourney != null ? journeyLog.ActiveJourney.journeyName : string.Empty,
            activeStopId = journeyLog != null && journeyLog.ActiveJourney != null ? journeyLog.ActiveJourney.currentStopId : string.Empty,
            activePhase = journeyLog != null && journeyLog.ActiveJourney != null ? journeyLog.ActiveJourney.phase : TransitJourneyPhase.None,
            currentRegionId = regionLog != null ? regionLog.CurrentRegionId : string.Empty,
            currentRegionName = regionLog != null ? regionLog.CurrentRegionName : string.Empty,
            rowCount = rows.Count,
            availableRowCount = rows.Count(row => row != null && row.canRun),
            blockedRowCount = rows.Count(row => row != null && !row.canRun),
            rows = rows
        };
    }

    public TransitRegionHandoffResult RunEntry(string entryId, PlayerController player, RegionTravelManager manager, UnityEngine.Object context) {
        var entry = FindEntry(entryId);
        if(entry == null) {
            return TransitRegionHandoffResult.Blocked(entryId, "Transit-region handoff entry is missing.");
        }

        return entry.Run(player, manager, context != null ? context : this);
    }

    public TransitRegionHandoffResult RunFirstAvailable(PlayerController player, RegionTravelManager manager, UnityEngine.Object context) {
        var snapshot = BuildSnapshot(player, manager, includeBlockedOverride: false);
        var first = snapshot.rows.FirstOrDefault(row => row != null && row.canRun);
        return first != null
            ? RunEntry(first.entryId, player, manager, context)
            : TransitRegionHandoffResult.Blocked(string.Empty, "No transit-region handoff is currently available.");
    }

    public TransitRegionHandoffEntry FindEntry(string entryId) {
        if(string.IsNullOrWhiteSpace(entryId)) {
            return null;
        }

        return Entries.FirstOrDefault(entry => entry != null && string.Equals(entry.EntryId, entryId, StringComparison.OrdinalIgnoreCase));
    }
}

[Serializable]
public class TransitRegionHandoffEntry {
    [Header("Identity")]
    [Tooltip("Stable id for this handoff entry. Empty uses regional route id.")]
    [SerializeField] string entryId = string.Empty;
    [Tooltip("Optional display name override. Empty uses regional route display name.")]
    [SerializeField] string displayNameOverride = string.Empty;
    [Tooltip("Lower priority rows appear first.")]
    [SerializeField] int priority;

    [Header("Transit Match")]
    [Tooltip("Transit journey that must be active or recently matched. Empty allows any journey.")]
    [SerializeField] TransitJourneyDefinition journey;
    [Tooltip("Stop id that must match the active transit stop. Empty allows any stop.")]
    [SerializeField] string stopId = string.Empty;
    [Tooltip("Transit state that makes this handoff valid.")]
    [SerializeField] TransitRegionHandoffTrigger trigger = TransitRegionHandoffTrigger.AtStop;
    [Tooltip("If enabled, this handoff requires an active PlayerTransitJourneyLog state.")]
    [SerializeField] bool requireActiveJourney = true;
    [Tooltip("If enabled and the active journey is at a valid stop, TryDisembark is called before regional travel.")]
    [SerializeField] bool disembarkBeforeRegionTravel = true;

    [Header("Region Travel")]
    [Tooltip("Regional route executed when this handoff runs.")]
    [SerializeField] RegionTravelRouteDefinition regionRoute;
    [Tooltip("Optional travel policy option id passed to the region route.")]
    [SerializeField] string policyOptionId = string.Empty;
    [Tooltip("How to choose the selected Pokemon for one-Pokemon or roster-lock regional challenge policies.")]
    [SerializeField] TransitRegionHandoffPokemonSelection pokemonSelection = TransitRegionHandoffPokemonSelection.FirstHealthy;
    [Tooltip("Party slot used when Pokemon Selection is Party Slot.")]
    [Range(0, 5)]
    [SerializeField] int partySlot;

    public string EntryId => !string.IsNullOrWhiteSpace(entryId) ? entryId : regionRoute != null ? regionRoute.Id : string.Empty;
    public string DisplayName => !string.IsNullOrWhiteSpace(displayNameOverride) ? displayNameOverride : regionRoute != null ? regionRoute.DisplayName : EntryId;
    public int Priority => priority;
    public TransitJourneyDefinition Journey => journey;
    public string StopId => stopId;
    public TransitRegionHandoffTrigger Trigger => trigger;
    public bool RequireActiveJourney => requireActiveJourney;
    public bool DisembarkBeforeRegionTravel => disembarkBeforeRegionTravel;
    public RegionTravelRouteDefinition RegionRoute => regionRoute;
    public string PolicyOptionId => policyOptionId;
    public TransitRegionHandoffPokemonSelection PokemonSelection => pokemonSelection;
    public int PartySlot => partySlot;

    public bool CanRun(PlayerController player, PlayerTransitJourneyLog journeyLog, PlayerWorldRegionLog regionLog, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required for transit-region handoff.";
            return false;
        }

        if(regionRoute == null) {
            failureMessage = "No regional route is assigned.";
            return false;
        }

        if(!MatchesTransitState(journeyLog, out failureMessage)) {
            return false;
        }

        var policyOption = regionRoute.ResolveTravelPolicyOption(policyOptionId);
        var selectedPokemon = ResolveSelectedPokemon(player);
        return regionRoute.CanUse(player, regionLog, EntryId, policyOption, selectedPokemon, out failureMessage);
    }

    public TransitRegionHandoffResult Run(PlayerController player, RegionTravelManager manager, UnityEngine.Object context) {
        var result = new TransitRegionHandoffResult {
            entryId = EntryId,
            entryName = DisplayName,
            routeId = regionRoute != null ? regionRoute.Id : string.Empty,
            routeName = regionRoute != null ? regionRoute.DisplayName : string.Empty
        };

        if(player == null) {
            result.blocked = true;
            result.message = "A player is required for transit-region handoff.";
            return result;
        }

        if(manager == null) {
            result.blocked = true;
            result.message = "RegionTravelManager is missing.";
            return result;
        }

        var journeyLog = player.GetComponent<PlayerTransitJourneyLog>();
        var regionLog = manager.ResolveLog(player);
        if(!CanRun(player, journeyLog, regionLog, out var failureMessage)) {
            result.blocked = true;
            result.message = failureMessage;
            return result;
        }

        if(disembarkBeforeRegionTravel && journeyLog != null && journeyLog.HasActiveJourney && journeyLog.ActiveJourney.phase == TransitJourneyPhase.AtStop && journeyLog.ActiveJourney.canDisembark) {
            if(journeyLog.TryDisembark(player, out var disembarkFailure)) {
                result.disembarkedTransit = true;
            } else if(!string.IsNullOrWhiteSpace(disembarkFailure)) {
                result.messages.Add(disembarkFailure);
            }
        }

        var selectedPokemon = ResolveSelectedPokemon(player);
        bool success = manager.TryTravelWithPolicy(regionRoute, selectedPokemon, policyOptionId, EntryId, DisplayName, out var regionResult);
        result.regionTravelResult = regionResult;
        result.success = success;
        result.blocked = !success;
        result.message = success
            ? $"Arrived at {regionResult.destinationRegionName}."
            : regionResult != null && !string.IsNullOrWhiteSpace(regionResult.failureMessage) ? regionResult.failureMessage : "Regional travel was blocked.";
        if(regionResult != null && regionResult.messages != null) {
            result.messages.AddRange(regionResult.messages);
        }

        Publish(player, result, context);
        return result;
    }

    bool MatchesTransitState(PlayerTransitJourneyLog journeyLog, out string failureMessage) {
        if(requireActiveJourney && (journeyLog == null || !journeyLog.HasActiveJourney)) {
            failureMessage = "No active transit journey.";
            return false;
        }

        var state = journeyLog != null ? journeyLog.ActiveJourney : null;
        if(state == null) {
            failureMessage = null;
            return true;
        }

        if(journey != null && !string.Equals(state.journeyId, journey.Id, StringComparison.OrdinalIgnoreCase)) {
            failureMessage = $"Active journey is not {journey.DisplayName}.";
            return false;
        }

        if(!string.IsNullOrWhiteSpace(stopId) && !string.Equals(state.currentStopId, stopId, StringComparison.OrdinalIgnoreCase)) {
            failureMessage = $"The vehicle has not reached {stopId}.";
            return false;
        }

        switch(trigger) {
            case TransitRegionHandoffTrigger.AtStop:
            case TransitRegionHandoffTrigger.Disembark:
                if(state.phase != TransitJourneyPhase.AtStop) {
                    failureMessage = "The vehicle is not at a stop.";
                    return false;
                }
                break;
            case TransitRegionHandoffTrigger.JourneyCompleted:
                if(state.phase != TransitJourneyPhase.Completed) {
                    failureMessage = "The journey is not completed.";
                    return false;
                }
                break;
        }

        failureMessage = null;
        return true;
    }

    Pokemon ResolveSelectedPokemon(PlayerController player) {
        if(player == null || pokemonSelection == TransitRegionHandoffPokemonSelection.None) {
            return null;
        }

        var party = player.GetComponent<PokemonParty>();
        if(party == null || party.Pokemons == null) {
            return null;
        }

        if(pokemonSelection == TransitRegionHandoffPokemonSelection.PartySlot) {
            return partySlot >= 0 && partySlot < party.Pokemons.Count ? party.Pokemons[partySlot] : null;
        }

        return party.GetHealthyPokemon() ?? party.Pokemons.FirstOrDefault(pokemon => pokemon != null);
    }

    void Publish(PlayerController player, TransitRegionHandoffResult result, UnityEngine.Object context) {
        GameEventPublishing.PublishOptional(
            null,
            $"transit-region-handoff.{(result != null && result.success ? "success" : "blocked")}.{EntryId}",
            result != null ? result.message : DisplayName,
            GameEventCategory.Transit,
            result != null && result.success ? GameEventImportance.Success : GameEventImportance.Warning,
            context != null ? context : player,
            "TransitRegionHandoff",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: true,
            GameEventPublishing.Value("entryId", EntryId),
            GameEventPublishing.Value("entryName", DisplayName),
            GameEventPublishing.Value("routeId", regionRoute != null ? regionRoute.Id : string.Empty),
            GameEventPublishing.Value("routeName", regionRoute != null ? regionRoute.DisplayName : string.Empty),
            GameEventPublishing.Value("success", result != null && result.success));
    }
}

public class TransitRegionHandoffSource : MonoBehaviour, Interactable, IPlayerTriggerable {
    [Header("Handoff")]
    [Tooltip("Transit-region handoff definition run by this source.")]
    [SerializeField] TransitRegionHandoffDefinition handoff;
    [Tooltip("Optional region travel manager. Empty searches the scene or creates one on this object.")]
    [SerializeField] RegionTravelManager managerOverride;

    [Header("Activation")]
    [Tooltip("If enabled, interacting with this object runs the first available handoff.")]
    [SerializeField] bool runFirstAvailableOnInteract = true;
    [Tooltip("If enabled, entering the trigger runs the first available handoff.")]
    [SerializeField] bool runFirstAvailableOnTrigger;
    [Tooltip("Controls IPlayerTriggerable.TriggerRepeatedly.")]
    [SerializeField] bool triggerRepeatedly = true;
    [Tooltip("If enabled, result text is shown through DialogManager when available.")]
    [SerializeField] bool showDialogFeedback = true;

    public TransitRegionHandoffDefinition Handoff => handoff;
    public RegionTravelManager ManagerOverride => managerOverride;
    public bool RunFirstAvailableOnInteract => runFirstAvailableOnInteract;
    public bool RunFirstAvailableOnTrigger => runFirstAvailableOnTrigger;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        if(runFirstAvailableOnTrigger) {
            RunFirstAvailable(player);
        }
    }

    public IEnumerator Interact(Transform initiator) {
        if(!runFirstAvailableOnInteract) {
            yield break;
        }

        var player = initiator != null ? initiator.GetComponent<PlayerController>() : PlayerController.i;
        var result = RunFirstAvailable(player);
        if(showDialogFeedback && DialogManager.i != null && result != null && !string.IsNullOrWhiteSpace(result.message)) {
            yield return DialogManager.i.ShowDialogText(result.message);
        }
    }

    public TransitRegionHandoffSnapshot GetSnapshot(PlayerController player = null, bool? includeBlockedOverride = null) {
        player = player != null ? player : PlayerController.i;
        return handoff != null
            ? handoff.BuildSnapshot(player, ResolveManager(), includeBlockedOverride)
            : new TransitRegionHandoffSnapshot();
    }

    public TransitRegionHandoffResult RunEntry(string entryId, PlayerController player = null) {
        player = player != null ? player : PlayerController.i;
        return handoff != null
            ? handoff.RunEntry(entryId, player, ResolveManager(), this)
            : TransitRegionHandoffResult.Blocked(entryId, "Transit-region handoff source has no definition.");
    }

    public TransitRegionHandoffResult RunFirstAvailable(PlayerController player = null) {
        player = player != null ? player : PlayerController.i;
        return handoff != null
            ? handoff.RunFirstAvailable(player, ResolveManager(), this)
            : TransitRegionHandoffResult.Blocked(string.Empty, "Transit-region handoff source has no definition.");
    }

    RegionTravelManager ResolveManager() {
        if(managerOverride != null) {
            return managerOverride;
        }

        managerOverride = FindAnyObjectByType<RegionTravelManager>();
        if(managerOverride == null) {
            managerOverride = gameObject.AddComponent<RegionTravelManager>();
        }

        return managerOverride;
    }
}

public class TransitRegionHandoffSnapshot {
    [Tooltip("Handoff definition id.")]
    public string handoffId;
    [Tooltip("Handoff definition display name.")]
    public string handoffName;
    [Tooltip("Handoff description.")]
    public string description;
    [Tooltip("Active transit journey id.")]
    public string activeJourneyId;
    [Tooltip("Active transit journey display name.")]
    public string activeJourneyName;
    [Tooltip("Active transit stop id.")]
    public string activeStopId;
    [Tooltip("Active transit phase.")]
    public TransitJourneyPhase activePhase;
    [Tooltip("Current world region id.")]
    public string currentRegionId;
    [Tooltip("Current world region display name.")]
    public string currentRegionName;
    [Tooltip("Visible row count.")]
    public int rowCount;
    [Tooltip("Rows that can run now.")]
    public int availableRowCount;
    [Tooltip("Rows that are visible but blocked.")]
    public int blockedRowCount;
    [Tooltip("Handoff rows available to UI.")]
    public List<TransitRegionHandoffRow> rows = new List<TransitRegionHandoffRow>();
}

public class TransitRegionHandoffRow {
    [Tooltip("Handoff entry id.")]
    public string entryId;
    [Tooltip("Handoff row display name.")]
    public string displayName;
    [Tooltip("Transit journey id filter.")]
    public string journeyId;
    [Tooltip("Transit journey display name filter.")]
    public string journeyName;
    [Tooltip("Stop id filter.")]
    public string stopId;
    [Tooltip("Regional route id.")]
    public string routeId;
    [Tooltip("Regional route display name.")]
    public string routeName;
    [Tooltip("Destination region id.")]
    public string destinationRegionId;
    [Tooltip("Destination region display name.")]
    public string destinationRegionName;
    [Tooltip("Transit handoff trigger.")]
    public TransitRegionHandoffTrigger trigger;
    [Tooltip("If enabled, this row can run now.")]
    public bool canRun;
    [Tooltip("Reason shown when row is blocked.")]
    public string blockedReason;
    [Tooltip("Lower priority rows appear first.")]
    public int priority;

    public static TransitRegionHandoffRow FromEntry(TransitRegionHandoffEntry entry, PlayerController player, PlayerTransitJourneyLog journeyLog, PlayerWorldRegionLog regionLog) {
        if(entry == null) {
            return null;
        }

        bool canRun = entry.CanRun(player, journeyLog, regionLog, out var failure);
        return new TransitRegionHandoffRow {
            entryId = entry.EntryId,
            displayName = entry.DisplayName,
            journeyId = entry.Journey != null ? entry.Journey.Id : string.Empty,
            journeyName = entry.Journey != null ? entry.Journey.DisplayName : string.Empty,
            stopId = entry.StopId,
            routeId = entry.RegionRoute != null ? entry.RegionRoute.Id : string.Empty,
            routeName = entry.RegionRoute != null ? entry.RegionRoute.DisplayName : string.Empty,
            destinationRegionId = entry.RegionRoute != null && entry.RegionRoute.DestinationRegion != null ? entry.RegionRoute.DestinationRegion.Id : string.Empty,
            destinationRegionName = entry.RegionRoute != null && entry.RegionRoute.DestinationRegion != null ? entry.RegionRoute.DestinationRegion.DisplayName : string.Empty,
            trigger = entry.Trigger,
            canRun = canRun,
            blockedReason = canRun ? string.Empty : failure,
            priority = entry.Priority
        };
    }
}

public class TransitRegionHandoffResult {
    public string entryId;
    public string entryName;
    public string routeId;
    public string routeName;
    public bool success;
    public bool blocked;
    public bool disembarkedTransit;
    public string message;
    public RegionTravelResult regionTravelResult;
    public readonly List<string> messages = new List<string>();

    public static TransitRegionHandoffResult Blocked(string entryId, string message) {
        return new TransitRegionHandoffResult {
            entryId = entryId,
            blocked = true,
            message = message
        };
    }
}
