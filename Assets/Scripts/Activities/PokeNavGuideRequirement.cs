using System.Linq;
using UnityEngine;

public enum PokeNavGuideRequirementMode {
    ItemSeen,
    ItemRead,
    ItemPinned,
    ItemDismissed,
    SectionVisibleCountAtLeast,
    SectionUnreadCountAtLeast
}

[CreateAssetMenu(menuName = "Activities/Requirements/PokeNav Guide Requirement")]
public class PokeNavGuideRequirement : ActivityRequirement {
    [Tooltip("Which PokeNav guide value this requirement checks.")]
    [SerializeField] PokeNavGuideRequirementMode mode = PokeNavGuideRequirementMode.ItemSeen;
    [Tooltip("Content type checked by item-specific modes.")]
    [SerializeField] PokeNavGuideContentType contentType = PokeNavGuideContentType.PokeNavEntry;
    [Tooltip("Optional direct Pokedex entry used to resolve item id.")]
    [SerializeField] PokedexEntryDefinition pokedexEntry;
    [Tooltip("Optional direct region info used to resolve item id.")]
    [SerializeField] RegionInfoDefinition regionInfo;
    [Tooltip("Optional direct PokeNav entry used to resolve item id.")]
    [SerializeField] PokeNavEntryDefinition pokeNavEntry;
    [Tooltip("Optional direct feed item used to resolve item id.")]
    [SerializeField] PokeNavFeedItemDefinition feedItem;
    [Tooltip("Optional direct social post used to resolve item id.")]
    [SerializeField] SocialPostDefinition socialPost;
    [Tooltip("Optional direct map marker used to resolve item id.")]
    [SerializeField] MapMarkerDefinition mapMarker;
    [Tooltip("Optional direct calendar event used to resolve item id.")]
    [SerializeField] CalendarEventDefinition calendarEvent;
    [Tooltip("Optional direct world discovery used to resolve item id.")]
    [SerializeField] WorldDiscoveryDefinition worldDiscovery;
    [Tooltip("Optional item id override. Empty uses the selected direct asset id.")]
    [SerializeField] string itemId = string.Empty;
    [Tooltip("Guide section checked by section count modes.")]
    [SerializeField] PokeNavGuideSectionDefinition section;
    [Tooltip("Optional tag filter used by section count modes.")]
    [SerializeField] string requiredTag = string.Empty;
    [Tooltip("Required count used by section count modes.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;
    [Tooltip("If enabled, the selected guide condition must be true. If disabled, it must be false.")]
    [SerializeField] bool mustBeMet = true;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerPokeNavGuideLog>() : null;
        string id = ResolveItemId();
        bool result = mode switch {
            PokeNavGuideRequirementMode.ItemRead => log != null && log.IsRead(contentType, id),
            PokeNavGuideRequirementMode.ItemPinned => log != null && log.IsPinned(contentType, id),
            PokeNavGuideRequirementMode.ItemDismissed => log != null && log.IsDismissed(contentType, id),
            PokeNavGuideRequirementMode.SectionVisibleCountAtLeast => section != null && section.BuildItems(player).Count(item => MatchesTag(item)) >= Mathf.Max(0, requiredCount),
            PokeNavGuideRequirementMode.SectionUnreadCountAtLeast => section != null && section.BuildItems(player).Count(item => MatchesTag(item) && !item.read) >= Mathf.Max(0, requiredCount),
            _ => log != null && log.IsSeen(contentType, id)
        };

        return mustBeMet ? result : !result;
    }

    string ResolveItemId() {
        if(!string.IsNullOrWhiteSpace(itemId)) {
            return itemId;
        }

        return contentType switch {
            PokeNavGuideContentType.PokedexEntry => pokedexEntry != null ? pokedexEntry.Id : string.Empty,
            PokeNavGuideContentType.RegionInfo => regionInfo != null ? regionInfo.Id : string.Empty,
            PokeNavGuideContentType.PokeNavEntry => pokeNavEntry != null ? pokeNavEntry.Id : string.Empty,
            PokeNavGuideContentType.FeedItem => feedItem != null ? feedItem.Id : string.Empty,
            PokeNavGuideContentType.SocialPost => socialPost != null ? socialPost.Id : string.Empty,
            PokeNavGuideContentType.MapMarker => mapMarker != null ? mapMarker.Id : string.Empty,
            PokeNavGuideContentType.CalendarEvent => calendarEvent != null ? calendarEvent.Id : string.Empty,
            PokeNavGuideContentType.WorldDiscovery => worldDiscovery != null ? worldDiscovery.Id : string.Empty,
            _ => string.Empty
        };
    }

    bool MatchesTag(PokeNavGuideItemRecord item) {
        return item != null && (string.IsNullOrWhiteSpace(requiredTag) || item.HasTag(requiredTag));
    }
}
