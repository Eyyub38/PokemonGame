using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokeNavEntryType {
    General,
    Pokemon,
    Region,
    NPC,
    Trainer,
    Shop,
    Transit,
    Activity,
    Research,
    Event,
    Club,
    Law,
    Tutorial
}

[CreateAssetMenu(menuName = "PokeNav/Knowledge Entry Definition")]
public class PokeNavEntryDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this knowledge entry. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in PokeNav UI. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Body text shown by future PokeNav UI.")]
    [TextArea]
    [SerializeField] string body;
    [Tooltip("Broad entry type used by filters and future UI tabs.")]
    [SerializeField] PokeNavEntryType entryType = PokeNavEntryType.General;
    [Tooltip("Optional icon shown by future PokeNav UI.")]
    [SerializeField] Sprite icon;
    [Tooltip("Free-form tags used by filters, requirements and social posts.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Related Data")]
    [Tooltip("Optional related Pokemon.")]
    [SerializeField] PokemonBase relatedPokemon;
    [Tooltip("Optional related region.")]
    [SerializeField] RegionInfoDefinition relatedRegion;
    [Tooltip("Optional related activity.")]
    [SerializeField] ActivityDefinition relatedActivity;
    [Tooltip("Optional related shop.")]
    [SerializeField] ShopCatalogDefinition relatedShop;
    [Tooltip("Optional related transit route.")]
    [SerializeField] TransitRouteDefinition relatedTransitRoute;

    [Header("Access")]
    [Tooltip("If enabled, this entry is treated as discovered before PlayerPokeNavLog records it.")]
    [SerializeField] bool visibleByDefault;
    [Tooltip("Optional title, badge, permit or license required before this entry can be discovered.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this entry can be discovered.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional minimum Pokemon knowledge level required for the related Pokemon.")]
    [SerializeField] PokemonKnowledgeLevel requiredPokemonKnowledge = PokemonKnowledgeLevel.Unknown;
    [Tooltip("Message shown when access rules block this entry.")]
    [SerializeField] string lockedMessage = "This PokeNav entry is not available yet.";

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Body => body;
    public PokeNavEntryType EntryType => entryType;
    public Sprite Icon => icon;
    public IReadOnlyList<string> Tags => tags;
    public PokemonBase RelatedPokemon => relatedPokemon;
    public RegionInfoDefinition RelatedRegion => relatedRegion;
    public ActivityDefinition RelatedActivity => relatedActivity;
    public ShopCatalogDefinition RelatedShop => relatedShop;
    public TransitRouteDefinition RelatedTransitRoute => relatedTransitRoute;
    public bool VisibleByDefault => visibleByDefault;

    public bool CanDiscover(PlayerController player, out string failureMessage) {
        if(requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredMilestone != null && !(player?.GetComponent<PlayerMilestones>()?.HasMilestone(requiredMilestone) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredMilestone.DisplayName} first." : lockedMessage;
            return false;
        }

        if(relatedPokemon != null && requiredPokemonKnowledge > PokemonKnowledgeLevel.Unknown) {
            var log = player != null ? player.GetComponent<PlayerPokeNavLog>() : null;
            if(log == null || log.GetPokemonKnowledgeLevel(relatedPokemon) < requiredPokemonKnowledge) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more information about {relatedPokemon.Name}." : lockedMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }
}
