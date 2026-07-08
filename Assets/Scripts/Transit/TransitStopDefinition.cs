using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum TransitStopType {
    Generic,
    BusStop,
    TrainStation,
    Harbor,
    Airport,
    TaxiStand,
    BikeDock,
    RidePokemonPoint,
    Special
}

[CreateAssetMenu(menuName = "Transit/Stop Definition")]
public class TransitStopDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this stop. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in future transit UI/debug messages. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing description for this stop.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad stop type used by UI filters and world logic.")]
    [SerializeField] TransitStopType stopType = TransitStopType.Generic;
    [Tooltip("Free-form tags used by requirements, jobs, dialog conditions and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Unlock Rules")]
    [Tooltip("If enabled, this stop is considered discovered/unlocked before PlayerTransitLog records it.")]
    [SerializeField] bool unlockedByDefault = true;
    [Tooltip("Message shown when stop access is blocked.")]
    [SerializeField] string lockedMessage = "This transit stop is not available yet.";

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required to use this stop.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional faction whose reputation gates this stop.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Optional milestone required before this stop can be used.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional world event whose active state gates this stop.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent;
    [Tooltip("Expected active state for the required world event.")]
    [SerializeField] bool requiredWorldEventActive = true;

    [Header("Routes")]
    [Tooltip("Routes available from this stop. UI can list these and call TransitStation.TryTravel.")]
    [SerializeField] List<TransitRouteDefinition> routes = new List<TransitRouteDefinition>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public TransitStopType StopType => stopType;
    public IReadOnlyList<string> Tags => tags;
    public bool UnlockedByDefault => unlockedByDefault;
    public string LockedMessage => lockedMessage;
    public IReadOnlyList<TransitRouteDefinition> Routes => routes;

    public bool IsUnlocked(PlayerController player, PlayerTransitLog log, out string failureMessage) {
        if(!unlockedByDefault && !(log?.HasUnlockedStop(Id) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is locked." : lockedMessage;
            return false;
        }

        if(requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredFaction != null) {
            int reputation = player?.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0;
            if(reputation < requiredReputation) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more reputation with {requiredFaction.DisplayName}." : lockedMessage;
                return false;
            }
        }

        if(requiredMilestone != null && !(player?.GetComponent<PlayerMilestones>()?.HasMilestone(requiredMilestone) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredMilestone.DisplayName} first." : lockedMessage;
            return false;
        }

        if(requiredWorldEvent != null) {
            bool active = WorldEventManager.i != null && WorldEventManager.i.IsEventActive(requiredWorldEvent);
            if(active != requiredWorldEventActive) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{DisplayName} is not available right now." : lockedMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public List<TransitRouteDefinition> GetAvailableRoutes(PlayerController player, PlayerTransitLog log, string currentStopId) {
        if(!IsUnlocked(player, log, out _)) {
            return new List<TransitRouteDefinition>();
        }

        return (routes ?? new List<TransitRouteDefinition>())
            .Where(route => route != null && route.CanUse(player, log, currentStopId, out _))
            .OrderBy(route => route.RouteType)
            .ThenBy(route => route.DisplayName)
            .ToList();
    }

    public bool HasTag(string tag) {
        if(string.IsNullOrWhiteSpace(tag) || tags == null) {
            return false;
        }

        foreach(var entry in tags) {
            if(string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return false;
    }
}
