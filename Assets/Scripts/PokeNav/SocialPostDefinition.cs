using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SocialPostType {
    General,
    Rumor,
    Event,
    PokemonSighting,
    TrainerSighting,
    ShopNews,
    PoliceNotice,
    ResearchNotice,
    TransitNotice,
    ClubNotice,
    ContestNotice,
    WeatherNotice
}

[CreateAssetMenu(menuName = "PokeNav/Social Post Definition")]
public class SocialPostDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this social post. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Short title shown in social feed UI. Empty uses the asset name.")]
    [SerializeField] string title;
    [Tooltip("Author, source or organization shown by future feed UI.")]
    [SerializeField] string author;
    [Tooltip("Main social feed text.")]
    [TextArea]
    [SerializeField] string body;
    [Tooltip("Broad post type used by filters and future UI styling.")]
    [SerializeField] SocialPostType postType = SocialPostType.General;
    [Tooltip("Priority used by feed sorting and notification importance.")]
    [SerializeField] NotificationPriority priority = NotificationPriority.Normal;
    [Tooltip("If enabled, this post is pinned in future social feed UI.")]
    [SerializeField] bool pinned;
    [Tooltip("Free-form tags used by filters, requirements and map links.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Related Data")]
    [Tooltip("Optional related Pokemon.")]
    [SerializeField] PokemonBase relatedPokemon;
    [Tooltip("Minimum knowledge level required for the related Pokemon before this post appears.")]
    [SerializeField] PokemonKnowledgeLevel requiredPokemonKnowledge = PokemonKnowledgeLevel.Unknown;
    [Tooltip("Optional related region.")]
    [SerializeField] RegionInfoDefinition relatedRegion;
    [Tooltip("Optional related trainer/NPC name.")]
    [SerializeField] string relatedCharacterName;
    [Tooltip("Optional related shop.")]
    [SerializeField] ShopCatalogDefinition relatedShop;
    [Tooltip("Optional related transit route.")]
    [SerializeField] TransitRouteDefinition relatedTransitRoute;
    [Tooltip("Optional related world event.")]
    [SerializeField] WorldEventDefinition relatedWorldEvent;

    [Header("Access")]
    [Tooltip("If enabled, this post can appear without being explicitly unlocked.")]
    [SerializeField] bool visibleByDefault = true;
    [Tooltip("Optional title, badge, permit or license required before this post can appear.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this post can appear.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this post.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("If assigned, this world event must match the expected active state.")]
    [SerializeField] WorldEventDefinition requiredWorldEvent;
    [Tooltip("Expected active state for the required world event.")]
    [SerializeField] bool requiredWorldEventActive = true;
    [Tooltip("Allowed day periods. Empty means any time.")]
    [SerializeField] List<DayPeriod> allowedPeriods = new List<DayPeriod>();
    [Tooltip("Message/debug reason used when this post is blocked.")]
    [SerializeField] string lockedMessage = "This post is not available yet.";

    [Header("Events")]
    [Tooltip("Optional event published when this post is unlocked or pushed. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition publishedEvent;
    [Tooltip("If enabled, publishing this post also creates a NotificationFeed entry.")]
    [SerializeField] bool publishToNotificationFeed = true;
    [Tooltip("If enabled, post events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string Title => string.IsNullOrWhiteSpace(title) ? name : title;
    public string Author => author;
    public string Body => body;
    public SocialPostType PostType => postType;
    public NotificationPriority Priority => priority;
    public bool Pinned => pinned;
    public IReadOnlyList<string> Tags => tags;
    public PokemonBase RelatedPokemon => relatedPokemon;
    public RegionInfoDefinition RelatedRegion => relatedRegion;
    public bool VisibleByDefault => visibleByDefault;

    public bool CanShow(PlayerController player, PlayerPokeNavLog log, out string failureMessage) {
        if(!visibleByDefault && !(log?.HasUnlockedPost(this) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{Title} is not unlocked." : lockedMessage;
            return false;
        }

        if(requiredTitle != null && !(player?.GetComponent<PlayerTitles>()?.HasTitle(requiredTitle) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredTitle.DisplayName}." : lockedMessage;
            return false;
        }

        if(requiredMilestone != null && !(player?.GetComponent<PlayerMilestones>()?.HasMilestone(requiredMilestone) ?? false)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need {requiredMilestone.DisplayName} first." : lockedMessage;
            return false;
        }

        if(requiredFaction != null) {
            int reputation = player?.GetComponent<PlayerReputation>()?.GetReputation(requiredFaction) ?? 0;
            if(reputation < requiredReputation) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more reputation with {requiredFaction.DisplayName}." : lockedMessage;
                return false;
            }
        }

        if(requiredWorldEvent != null) {
            bool active = WorldEventManager.i != null && WorldEventManager.i.IsEventActive(requiredWorldEvent);
            if(active != requiredWorldEventActive) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{Title} is not active right now." : lockedMessage;
                return false;
            }
        }

        if(allowedPeriods != null && allowedPeriods.Count > 0) {
            DayPeriod current = TimeSystem.i != null ? TimeSystem.i.CurrentPeriod : DayPeriod.None;
            if(!allowedPeriods.Contains(current)) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"{Title} is not available at this time." : lockedMessage;
                return false;
            }
        }

        if(relatedRegion != null && !(log?.HasDiscoveredRegion(relatedRegion) ?? relatedRegion.VisibleByDefault)) {
            failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You have not discovered {relatedRegion.DisplayName} yet." : lockedMessage;
            return false;
        }

        if(relatedPokemon != null && requiredPokemonKnowledge > PokemonKnowledgeLevel.Unknown) {
            if(log == null || log.GetPokemonKnowledgeLevel(relatedPokemon) < requiredPokemonKnowledge) {
                failureMessage = string.IsNullOrWhiteSpace(lockedMessage) ? $"You need more information about {relatedPokemon.Name}." : lockedMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }

    public void Publish(PlayerController player, string phase = "published") {
        GameEventPublishing.PublishOptional(
            publishedEvent,
            $"pokenav.post.{phase}.{Id}",
            Body,
            GameEventCategory.PokeNav,
            ToGameEventImportance(priority),
            player != null ? player : this,
            "SocialPostDefinition",
            GameEventScope.Player,
            showInFeed: false,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("postId", Id),
            GameEventPublishing.Value("title", Title),
            GameEventPublishing.Value("author", author),
            GameEventPublishing.Value("postType", postType),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("relatedPokemon", relatedPokemon != null ? relatedPokemon.name : string.Empty),
            GameEventPublishing.Value("relatedRegion", relatedRegion != null ? relatedRegion.Id : string.Empty),
            GameEventPublishing.Value("relatedCharacter", relatedCharacterName),
            GameEventPublishing.Value("relatedWorldEvent", relatedWorldEvent != null ? relatedWorldEvent.Id : string.Empty));

        if(publishToNotificationFeed) {
            NotificationFeed.Publish(
                Title,
                Body,
                ToNotificationKind(postType),
                priority,
                NotificationChannel.Story,
                "PokeNav",
                sourceEventId: Id,
                pinned: pinned,
                values: new[] {
                    GameEventPublishing.Value("postId", Id),
                    GameEventPublishing.Value("postType", postType),
                    GameEventPublishing.Value("author", author)
                });
        }
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    GameEventImportance ToGameEventImportance(NotificationPriority notificationPriority) {
        return notificationPriority switch {
            NotificationPriority.Low => GameEventImportance.Trace,
            NotificationPriority.High => GameEventImportance.Warning,
            NotificationPriority.Critical => GameEventImportance.Error,
            _ => GameEventImportance.Info
        };
    }

    NotificationKind ToNotificationKind(SocialPostType type) {
        return type switch {
            SocialPostType.PokemonSighting => NotificationKind.Activity,
            SocialPostType.TrainerSighting => NotificationKind.NPC,
            SocialPostType.ShopNews => NotificationKind.Item,
            SocialPostType.PoliceNotice => NotificationKind.Quest,
            SocialPostType.ResearchNotice => NotificationKind.Activity,
            SocialPostType.TransitNotice => NotificationKind.World,
            SocialPostType.Event => NotificationKind.World,
            SocialPostType.ContestNotice => NotificationKind.Activity,
            SocialPostType.ClubNotice => NotificationKind.Social,
            SocialPostType.WeatherNotice => NotificationKind.World,
            _ => NotificationKind.Social
        };
    }
}
