using System.Linq;
using UnityEngine;

public enum PokeNavFeedRequirementMode {
    HasUnlockedItem,
    HasActiveItem,
    ItemRead,
    ItemUnread,
    AvailableItemWithTag,
    UnreadItemWithTag,
    UnreadCountAtLeast
}

[CreateAssetMenu(menuName = "Activities/Requirements/PokeNav Feed Requirement")]
public class PokeNavFeedRequirement : ActivityRequirement {
    [Header("Feed")]
    [Tooltip("Feed item checked by item-specific modes.")]
    [SerializeField] PokeNavFeedItemDefinition feedItem;
    [Tooltip("How this feed requirement is evaluated.")]
    [SerializeField] PokeNavFeedRequirementMode mode = PokeNavFeedRequirementMode.HasActiveItem;

    [Header("Filters")]
    [Tooltip("Tag checked by tag-based modes.")]
    [SerializeField] string requiredTag;
    [Tooltip("Minimum unread count required by Unread Count At Least mode.")]
    [Min(0)]
    [SerializeField] int requiredCount = 1;

    public override bool IsMet(PlayerController player) {
        var log = player != null ? player.GetComponent<PlayerPokeNavFeedLog>() : null;
        return mode switch {
            PokeNavFeedRequirementMode.HasUnlockedItem => log != null && log.HasUnlockedItem(feedItem),
            PokeNavFeedRequirementMode.ItemRead => log != null && log.IsRead(feedItem),
            PokeNavFeedRequirementMode.ItemUnread => log != null && log.HasActiveItem(feedItem, out _) && !log.IsRead(feedItem),
            PokeNavFeedRequirementMode.AvailableItemWithTag => log != null && log.GetAvailableFeedItems().Any(item => item.HasTag(requiredTag)),
            PokeNavFeedRequirementMode.UnreadItemWithTag => log != null && log.GetAvailableFeedItems(includeRead: false).Any(item => item.HasTag(requiredTag)),
            PokeNavFeedRequirementMode.UnreadCountAtLeast => log != null && log.GetUnreadCount(requiredTag) >= Mathf.Max(0, requiredCount),
            _ => log != null && log.HasActiveItem(feedItem, out _)
        };
    }
}
