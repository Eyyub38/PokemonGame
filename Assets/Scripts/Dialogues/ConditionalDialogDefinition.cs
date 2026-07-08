using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum DialogConditionMatchMode {
    All,
    Any
}

public enum DialogComparison {
    Equal,
    NotEqual,
    Greater,
    GreaterOrEqual,
    Less,
    LessOrEqual
}

public enum DialogConditionType {
    Always,
    PlayerLevel,
    PlayerSkillLevel,
    PlayerSkillTagLevel,
    ReputationValue,
    RelationshipValue,
    MilestoneCompleted,
    WorldEventActive,
    TimePeriod,
    CurrentActivityZone,
    CurrentActivityZoneType,
    CurrentActivityZoneTag,
    ActivityAllowedInCurrentZone,
    HasItem,
    PartyHasPokemon,
    PartyHighestPokemonLevel,
    HasTitle,
    HasTitleTag,
    HasTitleKind,
    KnowsRecipe,
    KnowsRecipeTag,
    KnowsRecipeCategory,
    ShopPurchaseCount,
    EncounterSeenCount,
    EncounterBattleStartedCount,
    EncounterCapturedCount,
    EncounterStealthCapturedCount,
    HasActiveJob,
    JobCompletedCount,
    CompanionBondLevel,
    SpeakerPersonality,
    SpeakerPersonalityTrait,
    NPCMemoryHasMet,
    NPCMemoryInteractionCount,
    NPCMemoryInteractionTypeCount,
    NPCMemoryHasTopic,
    NPCMemoryTopicCount,
    NPCMemoryTopicTagCount,
    NPCMemoryTrustAtLeast,
    NPCMemorySuspicionAtLeast,
    NPCMemoryFamiliarityAtLeast,
    NPCMemoryHoursSinceLastInteractionAtMost,
    NPCReactionCount,
    NPCReactionTagCount,
    NPCReactionCategoryCount,
    NPCReactionHoursSinceLastAtMost,
    WitnessReportCount,
    WitnessReportTagCount,
    WitnessReportCategoryCount,
    WitnessReportHoursSinceLastAtMost,
    ReportPropagationCount,
    ReportPropagationTagCount,
    ReportPropagationCategoryCount,
    ReportPropagationHoursSinceLastAtMost,
    HasUnlockedTransitRoute,
    HasUnlockedTransitStop,
    TransitRouteTravelCount,
    TransitRouteTagTravelCount,
    TransitAnyTravelCount,
    HasEquippedCustomizationPart,
    HasEquippedCustomizationTag,
    HasEquippedCustomizationSlot,
    HasUnlockedCustomizationPart,
    CurrentCustomizationPreset,
    PokeNavPokemonKnowledgeAtLeast,
    PokeNavEntryDiscovered,
    PokeNavRegionDiscovered,
    PokeNavSocialPostUnlocked,
    PokeNavSocialPostRead,
    MapMarkerDiscovered,
    MapMarkerFavorite,
    MapMarkerHidden,
    RumorUnlocked,
    RumorHeard,
    RumorHeardCount,
    RumorTagHeard,
    RumorRead,
    RumorDismissed,
    CalendarEventUnlocked,
    CalendarEventSeen,
    CalendarEventCompleted,
    CalendarEventActive,
    CalendarEventVisible,
    CalendarEventCategorySeenCount,
    CalendarEventTagSeenCount,
    CalendarEventDismissed,
    BattleRuleUnlocked,
    BattleRuleTagUnlocked,
    BattleChallengeStarted,
    BattleChallengeCompleted,
    BattleChallengeWon,
    BattleChallengeLost,
    BattleRuleActive,
    BattleChallengeActive,
    ContestUnlocked,
    ContestAttemptCount,
    ContestWinCount,
    ContestBestScore,
    ContestBestRankAtLeast,
    ContestTagWinCount,
    ContestAvailable,
    CareerUnlocked,
    CareerJoined,
    CareerPointsAtLeast,
    CareerRankAtLeast,
    CareerTagJoined,
    CareerCanJoin,
    OrganizationUnlocked,
    OrganizationActiveMember,
    OrganizationPermanentMember,
    OrganizationPointsAtLeast,
    OrganizationRankAtLeast,
    OrganizationTagActiveMember,
    OrganizationCanJoin,
    AssignmentUnlocked,
    AssignmentActive,
    AssignmentCompleted,
    AssignmentCompletedCount,
    AssignmentTagCompletedCount,
    AssignmentAvailable,
    AccessProfileCanPass,
    AccessProfilePassed,
    AccessProfilePassedCount,
    AccessProfileDeniedCount,
    LawWantedScoreAtLeast,
    LawWantedLevelAtLeast,
    LawFineOwedAtLeast,
    LawViolationCount,
    LawViolationTagCount,
    LawViolationCategoryCount,
    InvestigationCaseUnlocked,
    InvestigationCaseActive,
    InvestigationCaseCompleted,
    InvestigationCaseCompletedCount,
    InvestigationCaseCanStart,
    InvestigationCaseCanComplete,
    InvestigationClueDiscovered,
    InvestigationClueTagDiscoveredCount,
    InvestigationEvidencePointsAtLeast,
    InvestigationStageAtLeast,
    ResearchStarted,
    ResearchCompleted,
    ResearchPointsAtLeast
}

[CreateAssetMenu(menuName = "Dialogues/Conditional Dialog Definition")]
public class ConditionalDialogDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id used by debug/event systems. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in editor/debug output. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer notes explaining where this conditional dialog is intended to be used.")]
    [TextArea]
    [SerializeField] string description;

    [Header("Fallback")]
    [Tooltip("Dialog used when no conditional entry matches.")]
    [SerializeField] Dialog fallbackDialog;

    [Header("Entries")]
    [Tooltip("Conditional dialog entries. Higher priority entries are evaluated first.")]
    [SerializeField] List<ConditionalDialogEntry> entries = new List<ConditionalDialogEntry>();

    [Header("Events")]
    [Tooltip("Optional event published when an entry is selected. Empty disables the custom event asset but a runtime event can still be generated.")]
    [SerializeField] GameEventDefinition selectedEvent;
    [Tooltip("If enabled, dialog selection writes to the event bus/debug feed.")]
    [SerializeField] bool publishSelectionEvent;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public Dialog FallbackDialog => fallbackDialog;
    public IReadOnlyList<ConditionalDialogEntry> Entries => entries;

    public Dialog SelectDialog(DialogContext context) {
        var entry = SelectEntry(context);
        PublishSelectedEntry(entry, context);
        return entry != null && entry.Dialog != null ? entry.Dialog : fallbackDialog;
    }

    public ConditionalDialogEntry SelectEntry(DialogContext context) {
        return entries
            .Where(e => e != null && e.Dialog != null && e.Matches(context))
            .OrderByDescending(e => e.Priority)
            .ThenBy(e => entries.IndexOf(e))
            .FirstOrDefault();
    }

    void PublishSelectedEntry(ConditionalDialogEntry entry, DialogContext context) {
        if(!publishSelectionEvent) {
            return;
        }

        string entryId = entry != null ? entry.Id : "fallback";
        string entryName = entry != null ? entry.DisplayName : "Fallback";
        GameEventPublishing.PublishOptional(
            selectedEvent,
            $"dialog.selected.{Id}.{entryId}",
            $"{DisplayName} selected {entryName}.",
            GameEventCategory.Dialogue,
            GameEventImportance.Trace,
            context != null ? context.Source : null,
            "ConditionalDialogDefinition",
            GameEventScope.Scene,
            showInFeed: false,
            writeToDebugLog: true,
            GameEventPublishing.Value("dialogId", Id),
            GameEventPublishing.Value("dialogName", DisplayName),
            GameEventPublishing.Value("entryId", entryId),
            GameEventPublishing.Value("entryName", entryName));
    }
}

[System.Serializable]
public class ConditionalDialogEntry {
    [Header("Identity")]
    [Tooltip("Stable entry id used by debug/event logs. Empty uses the display name or list order.")]
    [SerializeField] string id;
    [Tooltip("Editor/debug label for this entry.")]
    [SerializeField] string displayName;

    [Header("Selection")]
    [Tooltip("Higher priority entries are selected before lower priority entries when multiple entries match.")]
    [SerializeField] int priority;
    [Tooltip("Dialog shown when this entry matches.")]
    [SerializeField] Dialog dialog;
    [Tooltip("How the condition list is evaluated.")]
    [SerializeField] DialogConditionMatchMode matchMode = DialogConditionMatchMode.All;
    [Tooltip("If enabled, the final condition result is inverted.")]
    [SerializeField] bool invertResult;
    [Tooltip("Conditions that decide whether this entry can be selected. Empty means always valid.")]
    [SerializeField] List<DialogCondition> conditions = new List<DialogCondition>();

    public string Id => !string.IsNullOrWhiteSpace(id) ? id : (!string.IsNullOrWhiteSpace(displayName) ? displayName : "entry");
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
    public int Priority => priority;
    public Dialog Dialog => dialog;
    public IReadOnlyList<DialogCondition> Conditions => conditions;

    public bool Matches(DialogContext context) {
        bool result;
        if(conditions == null || conditions.Count == 0) {
            result = true;
        } else if(matchMode == DialogConditionMatchMode.Any) {
            result = conditions.Any(c => c == null || c.Evaluate(context));
        } else {
            result = conditions.All(c => c == null || c.Evaluate(context));
        }

        return invertResult ? !result : result;
    }
}

[System.Serializable]
public class DialogCondition {
    [Header("Rule")]
    [Tooltip("Which runtime value this condition checks.")]
    [SerializeField] DialogConditionType type = DialogConditionType.Always;
    [Tooltip("If enabled, this condition returns the opposite of the evaluated result.")]
    [SerializeField] bool invert;
    [Tooltip("Comparison used by numeric and enum-based conditions.")]
    [SerializeField] DialogComparison comparison = DialogComparison.GreaterOrEqual;

    [Header("Generic Values")]
    [Tooltip("Numeric value used by level, reputation, relationship, item count and trait checks.")]
    [SerializeField] int requiredValue;
    [Tooltip("Optional string id/tag used by conditions that need a free-form key.")]
    [SerializeField] string requiredKey;
    [Tooltip("Shop id checked by ShopPurchaseCount conditions.")]
    [SerializeField] string shopId;
    [Tooltip("Offer id checked by ShopPurchaseCount conditions.")]
    [SerializeField] string shopOfferId;
    [Tooltip("Board id checked by job status conditions. Empty accepts any board.")]
    [SerializeField] string jobBoardId;
    [Tooltip("Origin stop id checked by Transit Route Travel Count conditions. Empty accepts any origin.")]
    [SerializeField] string transitOriginStopId;
    [Tooltip("Destination stop id checked by Transit Route Travel Count conditions. Empty accepts any destination.")]
    [SerializeField] string transitDestinationStopId;
    [Tooltip("Expected boolean value for true/false conditions like milestone or world event state.")]
    [SerializeField] bool expectedBool = true;

    [Header("Definitions")]
    [Tooltip("Skill checked by PlayerSkillLevel conditions.")]
    [SerializeField] PlayerSkillDefinition skill;
    [Tooltip("Faction checked by ReputationValue conditions.")]
    [SerializeField] ReputationFactionDefinition faction;
    [Tooltip("Relationship subject checked by RelationshipValue conditions.")]
    [SerializeField] RelationshipSubjectDefinition relationshipSubject;
    [Tooltip("Optional NPC memory id override. Empty uses the speaker's NPCMemoryProfile id or SpeakerId.")]
    [SerializeField] string npcMemoryId;
    [Tooltip("NPC memory topic checked by NPC memory topic conditions.")]
    [SerializeField] NPCMemoryTopicDefinition npcMemoryTopic;
    [Tooltip("NPC reaction checked by reaction count conditions. Empty accepts any reaction.")]
    [SerializeField] NPCReactionDefinition npcReaction;
    [Tooltip("Optional source id filter for NPC reaction conditions.")]
    [SerializeField] string npcReactionSourceId;
    [Tooltip("Witness report checked by witness report count conditions. Empty accepts any report.")]
    [SerializeField] WitnessReportDefinition witnessReport;
    [Tooltip("Optional source id filter for witness report conditions.")]
    [SerializeField] string witnessReportSourceId;
    [Tooltip("Optional authority id filter for witness report conditions. Empty accepts any authority.")]
    [SerializeField] string witnessReportAuthorityId;
    [Tooltip("Report propagation checked by propagation count conditions. Empty accepts any propagation.")]
    [SerializeField] ReportPropagationDefinition reportPropagation;
    [Tooltip("Optional target id filter for report propagation conditions. Empty accepts any target.")]
    [SerializeField] string reportPropagationTargetId;
    [Tooltip("Optional source id filter for report propagation conditions.")]
    [SerializeField] string reportPropagationSourceId;
    [Tooltip("Milestone checked by MilestoneCompleted conditions.")]
    [SerializeField] MilestoneDefinition milestone;
    [Tooltip("World event checked by WorldEventActive conditions.")]
    [SerializeField] WorldEventDefinition worldEvent;
    [Tooltip("Activity checked by ActivityAllowedInCurrentZone conditions.")]
    [SerializeField] ActivityDefinition activity;
    [Tooltip("Activity zone checked by CurrentActivityZone conditions.")]
    [SerializeField] ActivityZoneDefinition activityZone;
    [Tooltip("Activity zone type checked by CurrentActivityZoneType conditions.")]
    [SerializeField] ActivityZoneType activityZoneType = ActivityZoneType.General;
    [Tooltip("Item checked by HasItem conditions.")]
    [SerializeField] ItemBase item;
    [Tooltip("Pokemon species checked by PartyHasPokemon conditions.")]
    [SerializeField] PokemonBase pokemon;
    [Tooltip("Title checked by HasTitle conditions.")]
    [SerializeField] TitleDefinition title;
    [Tooltip("Recipe checked by KnowsRecipe conditions.")]
    [SerializeField] RecipeDefinition recipe;
    [Tooltip("Job checked by job status conditions.")]
    [SerializeField] JobDefinition job;
    [Tooltip("Transit route checked by transit route conditions.")]
    [SerializeField] TransitRouteDefinition transitRoute;
    [Tooltip("Transit stop checked by transit stop conditions.")]
    [SerializeField] TransitStopDefinition transitStop;
    [Tooltip("Customization part checked by customization conditions.")]
    [SerializeField] CustomizationPartDefinition customizationPart;
    [Tooltip("Customization preset checked by Current Customization Preset conditions.")]
    [SerializeField] CustomizationPresetDefinition customizationPreset;
    [Tooltip("Customization slot checked by Has Equipped Customization Slot conditions.")]
    [SerializeField] CustomizationSlot customizationSlot = CustomizationSlot.Outfit;
    [Tooltip("PokeNav knowledge entry checked by PokeNav Entry Discovered conditions.")]
    [SerializeField] PokeNavEntryDefinition pokeNavEntry;
    [Tooltip("PokeNav region checked by PokeNav Region Discovered conditions.")]
    [SerializeField] RegionInfoDefinition pokeNavRegion;
    [Tooltip("PokeNav social post checked by social post conditions.")]
    [SerializeField] SocialPostDefinition pokeNavSocialPost;
    [Tooltip("Minimum Pokemon knowledge checked by PokeNav Pokemon Knowledge At Least conditions.")]
    [SerializeField] PokemonKnowledgeLevel pokemonKnowledgeLevel = PokemonKnowledgeLevel.Seen;
    [Tooltip("Map marker checked by map marker conditions.")]
    [SerializeField] MapMarkerDefinition mapMarker;
    [Tooltip("Optional map marker id override for map marker conditions. Empty uses Map Marker definition id.")]
    [SerializeField] string mapMarkerId;
    [Tooltip("Rumor checked by rumor conditions.")]
    [SerializeField] RumorDefinition rumor;
    [Tooltip("Optional rumor source id filter used by rumor heard conditions.")]
    [SerializeField] string rumorSourceId;
    [Tooltip("Calendar event checked by calendar conditions.")]
    [SerializeField] CalendarEventDefinition calendarEvent;
    [Tooltip("Calendar category checked by calendar category count conditions.")]
    [SerializeField] CalendarEventCategory calendarEventCategory = CalendarEventCategory.General;
    [Tooltip("Battle rule set checked by battle rule conditions.")]
    [SerializeField] BattleRuleSetDefinition battleRuleSet;
    [Tooltip("Battle challenge checked by battle challenge conditions.")]
    [SerializeField] BattleChallengeDefinition battleChallenge;
    [Tooltip("Contest checked by contest conditions.")]
    [SerializeField] ContestDefinition contest;
    [Tooltip("Career path checked by career conditions.")]
    [SerializeField] CareerPathDefinition careerPath;
    [Tooltip("Organization checked by organization conditions.")]
    [SerializeField] OrganizationDefinition organization;
    [Tooltip("Assignment checked by assignment conditions.")]
    [SerializeField] AssignmentDefinition assignment;
    [Tooltip("Optional assignment source id filter. Empty accepts any source.")]
    [SerializeField] string assignmentSourceId;
    [Tooltip("Access profile checked by access conditions.")]
    [SerializeField] AccessProfileDefinition accessProfile;
    [Tooltip("Optional access gate/context id filter. Empty accepts any context for history checks.")]
    [SerializeField] string accessContextId;
    [Tooltip("Law violation checked by law violation count conditions.")]
    [SerializeField] LawViolationDefinition lawViolation;
    [Tooltip("Optional authority faction filter for law conditions.")]
    [SerializeField] ReputationFactionDefinition lawAuthorityFaction;
    [Tooltip("Optional authority id override for law conditions. Empty uses Law Authority Faction or all authorities.")]
    [SerializeField] string lawAuthorityId;
    [Tooltip("Optional law source id filter for law violation count conditions.")]
    [SerializeField] string lawSourceId;
    [Tooltip("Investigation case checked by investigation conditions.")]
    [SerializeField] InvestigationCaseDefinition investigationCase;
    [Tooltip("Investigation clue checked by clue discovery conditions.")]
    [SerializeField] InvestigationClueDefinition investigationClue;
    [Tooltip("Research subject checked by research conditions.")]
    [SerializeField] ResearchSubjectDefinition researchSubject;

    [Header("Time And Personality")]
    [Tooltip("Day period checked by TimePeriod conditions.")]
    [SerializeField] DayPeriod dayPeriod = DayPeriod.None;
    [Tooltip("Companion bond level checked by CompanionBondLevel conditions.")]
    [SerializeField] CompanionBondLevel companionBondLevel = CompanionBondLevel.Stranger;
    [Tooltip("Speaker personality checked by SpeakerPersonality conditions.")]
    [SerializeField] PersonalityID personalityId = PersonalityID.Balanced;
    [Tooltip("Speaker personality trait checked by SpeakerPersonalityTrait conditions.")]
    [SerializeField] PersonalityTrait personalityTrait = PersonalityTrait.Courage;
    [Tooltip("Title kind checked by HasTitleKind conditions.")]
    [SerializeField] TitleKind titleKind = TitleKind.Title;
    [Tooltip("Recipe category checked by KnowsRecipeCategory conditions.")]
    [SerializeField] RecipeCategory recipeCategory = RecipeCategory.General;
    [Tooltip("Purchase period checked by ShopPurchaseCount conditions.")]
    [SerializeField] ShopStockLimitPeriod shopPurchasePeriod = ShopStockLimitPeriod.Total;
    [Tooltip("Encounter source filter checked by Encounter count conditions.")]
    [SerializeField] EncounterSourceType encounterSourceType = EncounterSourceType.Any;
    [Tooltip("Interaction type checked by NPC Memory Interaction Type Count conditions.")]
    [SerializeField] NPCInteractionMemoryType npcMemoryInteractionType = NPCInteractionMemoryType.Conversation;
    [Tooltip("Reaction category checked by NPC Reaction Category Count conditions.")]
    [SerializeField] NPCReactionCategory npcReactionCategory = NPCReactionCategory.General;
    [Tooltip("Witness report category checked by Witness Report Category Count conditions.")]
    [SerializeField] WitnessReportCategory witnessReportCategory = WitnessReportCategory.General;
    [Tooltip("Report propagation category checked by Report Propagation Category Count conditions.")]
    [SerializeField] ReportPropagationCategory reportPropagationCategory = ReportPropagationCategory.General;
    [Tooltip("Law category checked by Law Violation Category Count conditions.")]
    [SerializeField] LawViolationCategory lawViolationCategory = LawViolationCategory.General;

    public DialogConditionType Type => type;

    public bool Evaluate(DialogContext context) {
        bool result = EvaluateInternal(context);
        return invert ? !result : result;
    }

    bool EvaluateInternal(DialogContext context) {
        switch(type) {
            case DialogConditionType.Always:
                return true;
            case DialogConditionType.PlayerLevel:
                return Compare(GetPlayerProgression(context)?.Level ?? 0, requiredValue);
            case DialogConditionType.PlayerSkillLevel:
                return Compare(GetPlayerProgression(context)?.GetSkillLevel(skill) ?? 0, requiredValue);
            case DialogConditionType.PlayerSkillTagLevel:
                return Compare(GetPlayerProgression(context)?.GetHighestSkillLevelWithTag(requiredKey) ?? 0, requiredValue);
            case DialogConditionType.ReputationValue:
                return Compare(context?.GetPlayerComponent<PlayerReputation>()?.GetReputation(faction) ?? 0, requiredValue);
            case DialogConditionType.RelationshipValue:
                return Compare(context?.GetPlayerComponent<PlayerRelationships>()?.GetRelationship(relationshipSubject) ?? 0, requiredValue);
            case DialogConditionType.MilestoneCompleted:
                return (context?.GetPlayerComponent<PlayerMilestones>()?.HasMilestone(milestone) ?? false) == expectedBool;
            case DialogConditionType.WorldEventActive:
                return (WorldEventManager.i != null && WorldEventManager.i.IsEventActive(worldEvent)) == expectedBool;
            case DialogConditionType.TimePeriod:
                return Compare(TimeSystem.i != null ? (int)TimeSystem.i.CurrentPeriod : (int)DayPeriod.None, (int)dayPeriod);
            case DialogConditionType.CurrentActivityZone:
                return (PlayerActivityContext.CurrentZone == activityZone) == expectedBool;
            case DialogConditionType.CurrentActivityZoneType:
                return PlayerActivityContext.HasActiveZoneType(activityZoneType) == expectedBool;
            case DialogConditionType.CurrentActivityZoneTag:
                return PlayerActivityContext.HasActiveTag(requiredKey) == expectedBool;
            case DialogConditionType.ActivityAllowedInCurrentZone:
                return PlayerActivityContext.IsAllowed(activity, context?.Player) == expectedBool;
            case DialogConditionType.HasItem:
                return (Inventory.GetInventory()?.HasItemEnough(item, Mathf.Max(1, requiredValue)) ?? false) == expectedBool;
            case DialogConditionType.PartyHasPokemon:
                return HasPokemon(context, pokemon) == expectedBool;
            case DialogConditionType.PartyHighestPokemonLevel:
                return Compare(GetHighestPartyLevel(context), requiredValue);
            case DialogConditionType.HasTitle:
                return (context?.GetPlayerComponent<PlayerTitles>()?.HasTitle(title) ?? false) == expectedBool;
            case DialogConditionType.HasTitleTag:
                return (context?.GetPlayerComponent<PlayerTitles>()?.HasTitleWithTag(requiredKey) ?? false) == expectedBool;
            case DialogConditionType.HasTitleKind:
                return (context?.GetPlayerComponent<PlayerTitles>()?.HasTitleKind(titleKind) ?? false) == expectedBool;
            case DialogConditionType.KnowsRecipe:
                return (context?.GetPlayerComponent<PlayerRecipeBook>()?.KnowsRecipe(recipe) ?? false) == expectedBool;
            case DialogConditionType.KnowsRecipeTag:
                return (context?.GetPlayerComponent<PlayerRecipeBook>()?.KnowsRecipeWithTag(requiredKey) ?? false) == expectedBool;
            case DialogConditionType.KnowsRecipeCategory:
                return (context?.GetPlayerComponent<PlayerRecipeBook>()?.KnowsRecipeCategory(recipeCategory) ?? false) == expectedBool;
            case DialogConditionType.ShopPurchaseCount:
                return Compare(context?.GetPlayerComponent<PlayerShopLedger>()?.GetPurchasedCount(shopId, shopOfferId, shopPurchasePeriod) ?? 0, requiredValue);
            case DialogConditionType.EncounterSeenCount:
                return Compare(GetEncounterCount(context, EncounterLogCountType.Seen), requiredValue);
            case DialogConditionType.EncounterBattleStartedCount:
                return Compare(GetEncounterCount(context, EncounterLogCountType.BattleStarted), requiredValue);
            case DialogConditionType.EncounterCapturedCount:
                return Compare(GetEncounterCount(context, EncounterLogCountType.Captured), requiredValue);
            case DialogConditionType.EncounterStealthCapturedCount:
                return Compare(GetEncounterCount(context, EncounterLogCountType.StealthCaptured), requiredValue);
            case DialogConditionType.HasActiveJob:
                return (context?.GetPlayerComponent<PlayerJobLog>()?.HasActiveJob(job, jobBoardId) ?? false) == expectedBool;
            case DialogConditionType.JobCompletedCount:
                return Compare(context?.GetPlayerComponent<PlayerJobLog>()?.GetCompletedCount(job, jobBoardId) ?? 0, requiredValue);
            case DialogConditionType.HasUnlockedTransitRoute:
                return (context?.GetPlayerComponent<PlayerTransitLog>()?.HasUnlockedRoute(transitRoute) ?? false) == expectedBool;
            case DialogConditionType.HasUnlockedTransitStop:
                return (context?.GetPlayerComponent<PlayerTransitLog>()?.HasUnlockedStop(transitStop) ?? false) == expectedBool;
            case DialogConditionType.TransitRouteTravelCount:
                return Compare(context?.GetPlayerComponent<PlayerTransitLog>()?.GetTravelCount(transitRoute, transitOriginStopId, transitDestinationStopId) ?? 0, requiredValue);
            case DialogConditionType.TransitRouteTagTravelCount:
                return Compare(context?.GetPlayerComponent<PlayerTransitLog>()?.GetTravelCountWithTag(requiredKey) ?? 0, requiredValue);
            case DialogConditionType.TransitAnyTravelCount:
                return Compare(context?.GetPlayerComponent<PlayerTransitLog>()?.GetTotalTravelCount() ?? 0, requiredValue);
            case DialogConditionType.HasEquippedCustomizationPart:
                return (context?.GetPlayerComponent<PlayerCustomization>()?.HasEquippedPart(customizationPart) ?? false) == expectedBool;
            case DialogConditionType.HasEquippedCustomizationTag:
                return (context?.GetPlayerComponent<PlayerCustomization>()?.HasEquippedPartWithTag(requiredKey) ?? false) == expectedBool;
            case DialogConditionType.HasEquippedCustomizationSlot:
                return (context?.GetPlayerComponent<PlayerCustomization>()?.HasEquippedSlot(customizationSlot) ?? false) == expectedBool;
            case DialogConditionType.HasUnlockedCustomizationPart:
                return (context?.GetPlayerComponent<PlayerCustomization>()?.HasUnlockedPart(customizationPart) ?? false) == expectedBool;
            case DialogConditionType.CurrentCustomizationPreset:
                return (context?.GetPlayerComponent<PlayerCustomization>()?.CurrentPreset == customizationPreset) == expectedBool;
            case DialogConditionType.PokeNavPokemonKnowledgeAtLeast:
                return Compare((int)(context?.GetPlayerComponent<PlayerPokeNavLog>()?.GetPokemonKnowledgeLevel(pokemon) ?? PokemonKnowledgeLevel.Unknown), (int)pokemonKnowledgeLevel);
            case DialogConditionType.PokeNavEntryDiscovered:
                return (context?.GetPlayerComponent<PlayerPokeNavLog>()?.HasDiscoveredEntry(pokeNavEntry) ?? false) == expectedBool;
            case DialogConditionType.PokeNavRegionDiscovered:
                return (context?.GetPlayerComponent<PlayerPokeNavLog>()?.HasDiscoveredRegion(pokeNavRegion) ?? false) == expectedBool;
            case DialogConditionType.PokeNavSocialPostUnlocked:
                return (context?.GetPlayerComponent<PlayerPokeNavLog>()?.HasUnlockedPost(pokeNavSocialPost) ?? false) == expectedBool;
            case DialogConditionType.PokeNavSocialPostRead:
                return (context?.GetPlayerComponent<PlayerPokeNavLog>()?.IsPostRead(pokeNavSocialPost) ?? false) == expectedBool;
            case DialogConditionType.MapMarkerDiscovered:
                return (context?.GetPlayerComponent<PlayerMapLog>()?.HasDiscoveredMarker(GetMapMarkerId()) ?? false) == expectedBool;
            case DialogConditionType.MapMarkerFavorite:
                return (context?.GetPlayerComponent<PlayerMapLog>()?.IsMarkerFavorite(GetMapMarkerId()) ?? false) == expectedBool;
            case DialogConditionType.MapMarkerHidden:
                return (context?.GetPlayerComponent<PlayerMapLog>()?.IsMarkerHidden(GetMapMarkerId()) ?? false) == expectedBool;
            case DialogConditionType.RumorUnlocked:
                return (context?.GetPlayerComponent<PlayerRumorLog>()?.HasUnlockedRumor(rumor) ?? false) == expectedBool;
            case DialogConditionType.RumorHeard:
                return (context?.GetPlayerComponent<PlayerRumorLog>()?.HasHeardRumor(rumor, rumorSourceId) ?? false) == expectedBool;
            case DialogConditionType.RumorHeardCount:
                return Compare(context?.GetPlayerComponent<PlayerRumorLog>()?.GetHeardCount(rumor, rumorSourceId) ?? 0, requiredValue);
            case DialogConditionType.RumorTagHeard:
                return Compare(context?.GetPlayerComponent<PlayerRumorLog>()?.GetHeardCountWithTag(requiredKey) ?? 0, requiredValue);
            case DialogConditionType.RumorRead:
                return (context?.GetPlayerComponent<PlayerRumorLog>()?.IsRumorRead(rumor) ?? false) == expectedBool;
            case DialogConditionType.RumorDismissed:
                return (context?.GetPlayerComponent<PlayerRumorLog>()?.IsRumorDismissed(rumor) ?? false) == expectedBool;
            case DialogConditionType.CalendarEventUnlocked:
                return (context?.GetPlayerComponent<PlayerCalendarLog>()?.HasUnlockedEvent(calendarEvent) ?? false) == expectedBool;
            case DialogConditionType.CalendarEventSeen:
                return (context?.GetPlayerComponent<PlayerCalendarLog>()?.HasSeenEvent(calendarEvent) ?? false) == expectedBool;
            case DialogConditionType.CalendarEventCompleted:
                return (context?.GetPlayerComponent<PlayerCalendarLog>()?.HasCompletedEvent(calendarEvent) ?? false) == expectedBool;
            case DialogConditionType.CalendarEventActive:
                return (calendarEvent != null && calendarEvent.IsActiveNow()) == expectedBool;
            case DialogConditionType.CalendarEventVisible:
                return (calendarEvent != null && calendarEvent.CanShow(context?.Player, context?.GetPlayerComponent<PlayerCalendarLog>(), out _)) == expectedBool;
            case DialogConditionType.CalendarEventCategorySeenCount:
                return Compare(context?.GetPlayerComponent<PlayerCalendarLog>()?.GetSeenCountByCategory(calendarEventCategory) ?? 0, requiredValue);
            case DialogConditionType.CalendarEventTagSeenCount:
                return Compare(context?.GetPlayerComponent<PlayerCalendarLog>()?.GetSeenCountWithTag(requiredKey) ?? 0, requiredValue);
            case DialogConditionType.CalendarEventDismissed:
                return (context?.GetPlayerComponent<PlayerCalendarLog>()?.IsDismissed(calendarEvent) ?? false) == expectedBool;
            case DialogConditionType.BattleRuleUnlocked:
                return (context?.GetPlayerComponent<PlayerBattleRuleLog>()?.HasUnlockedRuleSet(battleRuleSet) ?? false) == expectedBool;
            case DialogConditionType.BattleRuleTagUnlocked:
                return HasUnlockedBattleRuleWithTag(context) == expectedBool;
            case DialogConditionType.BattleChallengeStarted:
                return Compare(context?.GetPlayerComponent<PlayerBattleRuleLog>()?.GetStartedCount(battleChallenge, battleRuleSet) ?? 0, requiredValue);
            case DialogConditionType.BattleChallengeCompleted:
                return Compare(context?.GetPlayerComponent<PlayerBattleRuleLog>()?.GetCompletedCount(battleChallenge, battleRuleSet) ?? 0, requiredValue);
            case DialogConditionType.BattleChallengeWon:
                return Compare(context?.GetPlayerComponent<PlayerBattleRuleLog>()?.GetWinCount(battleChallenge, battleRuleSet) ?? 0, requiredValue);
            case DialogConditionType.BattleChallengeLost:
                return Compare(context?.GetPlayerComponent<PlayerBattleRuleLog>()?.GetLossCount(battleChallenge, battleRuleSet) ?? 0, requiredValue);
            case DialogConditionType.BattleRuleActive:
                return (BattleRuleManager.i != null && BattleRuleManager.i.CurrentContext != null && BattleRuleManager.i.CurrentContext.IsActive && BattleRuleManager.i.CurrentContext.RuleSet == battleRuleSet) == expectedBool;
            case DialogConditionType.BattleChallengeActive:
                return (BattleRuleManager.i != null && BattleRuleManager.i.CurrentContext != null && BattleRuleManager.i.CurrentContext.IsActive && BattleRuleManager.i.CurrentContext.Challenge == battleChallenge) == expectedBool;
            case DialogConditionType.ContestUnlocked:
                return (context?.GetPlayerComponent<PlayerContestLog>()?.HasUnlockedContest(contest) ?? false) == expectedBool;
            case DialogConditionType.ContestAttemptCount:
                return Compare(context?.GetPlayerComponent<PlayerContestLog>()?.GetAttemptCount(contest) ?? 0, requiredValue);
            case DialogConditionType.ContestWinCount:
                return Compare(context?.GetPlayerComponent<PlayerContestLog>()?.GetWinCount(contest) ?? 0, requiredValue);
            case DialogConditionType.ContestBestScore:
                return Compare(context?.GetPlayerComponent<PlayerContestLog>()?.GetBestScore(contest) ?? 0, requiredValue);
            case DialogConditionType.ContestBestRankAtLeast:
                return Compare(context?.GetPlayerComponent<PlayerContestLog>()?.GetBestRankIndex(contest) ?? -1, requiredValue);
            case DialogConditionType.ContestTagWinCount:
                return Compare(context?.GetPlayerComponent<PlayerContestLog>()?.GetWinCountWithTag(requiredKey) ?? 0, requiredValue);
            case DialogConditionType.ContestAvailable:
                return (contest != null && contest.CanEnter(context?.Player, null, out _)) == expectedBool;
            case DialogConditionType.CareerUnlocked:
                return (context?.GetPlayerComponent<PlayerCareerLog>()?.HasUnlockedCareer(careerPath) ?? false) == expectedBool;
            case DialogConditionType.CareerJoined:
                return (context?.GetPlayerComponent<PlayerCareerLog>()?.HasJoinedCareer(careerPath) ?? false) == expectedBool;
            case DialogConditionType.CareerPointsAtLeast:
                return Compare(context?.GetPlayerComponent<PlayerCareerLog>()?.GetPoints(careerPath) ?? 0, requiredValue);
            case DialogConditionType.CareerRankAtLeast:
                return (context?.GetPlayerComponent<PlayerCareerLog>()?.HasReachedRank(careerPath, requiredValue) ?? false) == expectedBool;
            case DialogConditionType.CareerTagJoined:
                return (context?.GetPlayerComponent<PlayerCareerLog>()?.HasJoinedCareerWithTag(requiredKey) ?? false) == expectedBool;
            case DialogConditionType.CareerCanJoin:
                return (careerPath != null && careerPath.CanJoin(context?.Player, viaMentor: false, out _)) == expectedBool;
            case DialogConditionType.OrganizationUnlocked:
                return (context?.GetPlayerComponent<PlayerOrganizationLog>()?.HasUnlockedOrganization(organization) ?? false) == expectedBool;
            case DialogConditionType.OrganizationActiveMember:
                return (context?.GetPlayerComponent<PlayerOrganizationLog>()?.HasActiveMembership(organization) ?? false) == expectedBool;
            case DialogConditionType.OrganizationPermanentMember:
                return (context?.GetPlayerComponent<PlayerOrganizationLog>()?.HasPermanentMembership(organization) ?? false) == expectedBool;
            case DialogConditionType.OrganizationPointsAtLeast:
                return Compare(context?.GetPlayerComponent<PlayerOrganizationLog>()?.GetPoints(organization) ?? 0, requiredValue);
            case DialogConditionType.OrganizationRankAtLeast:
                return (context?.GetPlayerComponent<PlayerOrganizationLog>()?.HasReachedRank(organization, requiredValue) ?? false) == expectedBool;
            case DialogConditionType.OrganizationTagActiveMember:
                return (context?.GetPlayerComponent<PlayerOrganizationLog>()?.HasActiveOrganizationWithTag(requiredKey) ?? false) == expectedBool;
            case DialogConditionType.OrganizationCanJoin:
                return (organization != null && organization.CanJoin(context?.Player, viaInvitation: false, out _)) == expectedBool;
            case DialogConditionType.AssignmentUnlocked:
                return (context?.GetPlayerComponent<PlayerAssignmentLog>()?.HasUnlockedAssignment(assignment) ?? false) == expectedBool;
            case DialogConditionType.AssignmentActive:
                return (context?.GetPlayerComponent<PlayerAssignmentLog>()?.HasActiveAssignment(assignment, assignmentSourceId) ?? false) == expectedBool;
            case DialogConditionType.AssignmentCompleted:
                return (context?.GetPlayerComponent<PlayerAssignmentLog>()?.GetCompletedCount(assignment, assignmentSourceId) > 0) == expectedBool;
            case DialogConditionType.AssignmentCompletedCount:
                return Compare(context?.GetPlayerComponent<PlayerAssignmentLog>()?.GetCompletedCount(assignment, assignmentSourceId) ?? 0, requiredValue);
            case DialogConditionType.AssignmentTagCompletedCount:
                return Compare(context?.GetPlayerComponent<PlayerAssignmentLog>()?.GetCompletedCountWithTag(requiredKey) ?? 0, requiredValue);
            case DialogConditionType.AssignmentAvailable:
                return (assignment != null && assignment.CanAccept(context?.Player, context?.GetPlayerComponent<PlayerAssignmentLog>(), assignmentSourceId, out _)) == expectedBool;
            case DialogConditionType.AccessProfileCanPass:
                return (accessProfile != null && accessProfile.CanAccess(context?.Player, out _)) == expectedBool;
            case DialogConditionType.AccessProfilePassed:
                return (context?.GetPlayerComponent<PlayerAccessLog>()?.HasPassed(accessProfile, accessContextId) ?? false) == expectedBool;
            case DialogConditionType.AccessProfilePassedCount:
                return Compare(context?.GetPlayerComponent<PlayerAccessLog>()?.GetPassedCount(accessProfile, accessContextId) ?? 0, requiredValue);
            case DialogConditionType.AccessProfileDeniedCount:
                return Compare(context?.GetPlayerComponent<PlayerAccessLog>()?.GetDeniedCount(accessProfile, accessContextId) ?? 0, requiredValue);
            case DialogConditionType.LawWantedScoreAtLeast:
                return Compare(context?.GetPlayerComponent<PlayerLawLog>()?.GetWantedScore(GetLawAuthorityId()) ?? 0, requiredValue);
            case DialogConditionType.LawWantedLevelAtLeast:
                return Compare(context?.GetPlayerComponent<PlayerLawLog>()?.GetWantedLevel(GetLawAuthorityId()) ?? 0, requiredValue);
            case DialogConditionType.LawFineOwedAtLeast:
                return Compare(Mathf.FloorToInt(context?.GetPlayerComponent<PlayerLawLog>()?.GetFineOwed(GetLawAuthorityId()) ?? 0f), requiredValue);
            case DialogConditionType.LawViolationCount:
                return Compare(context?.GetPlayerComponent<PlayerLawLog>()?.GetViolationCount(lawViolation, GetLawAuthorityId(), lawSourceId) ?? 0, requiredValue);
            case DialogConditionType.LawViolationTagCount:
                return Compare(context?.GetPlayerComponent<PlayerLawLog>()?.GetViolationCountWithTag(requiredKey, GetLawAuthorityId()) ?? 0, requiredValue);
            case DialogConditionType.LawViolationCategoryCount:
                return Compare(context?.GetPlayerComponent<PlayerLawLog>()?.GetViolationCountByCategory(lawViolationCategory, GetLawAuthorityId()) ?? 0, requiredValue);
            case DialogConditionType.InvestigationCaseUnlocked:
                return (context?.GetPlayerComponent<PlayerInvestigationLog>()?.HasUnlockedCase(investigationCase) ?? false) == expectedBool;
            case DialogConditionType.InvestigationCaseActive:
                return (context?.GetPlayerComponent<PlayerInvestigationLog>()?.HasActiveCase(investigationCase) ?? false) == expectedBool;
            case DialogConditionType.InvestigationCaseCompleted:
                return (context?.GetPlayerComponent<PlayerInvestigationLog>()?.HasCompletedCase(investigationCase) ?? false) == expectedBool;
            case DialogConditionType.InvestigationCaseCompletedCount:
                return Compare(context?.GetPlayerComponent<PlayerInvestigationLog>()?.GetCompletedCount(investigationCase) ?? 0, requiredValue);
            case DialogConditionType.InvestigationCaseCanStart:
                return (investigationCase != null && investigationCase.CanStart(context?.Player, context?.GetPlayerComponent<PlayerInvestigationLog>(), out _)) == expectedBool;
            case DialogConditionType.InvestigationCaseCanComplete:
                return (investigationCase != null && investigationCase.CanComplete(context?.Player, context?.GetPlayerComponent<PlayerInvestigationLog>()?.GetActiveCase(investigationCase), out _)) == expectedBool;
            case DialogConditionType.InvestigationClueDiscovered:
                return (context?.GetPlayerComponent<PlayerInvestigationLog>()?.HasDiscoveredClue(investigationCase, investigationClue) ?? false) == expectedBool;
            case DialogConditionType.InvestigationClueTagDiscoveredCount:
                return Compare(context?.GetPlayerComponent<PlayerInvestigationLog>()?.GetDiscoveredClueCountWithTag(requiredKey) ?? 0, requiredValue);
            case DialogConditionType.InvestigationEvidencePointsAtLeast:
                return Compare(context?.GetPlayerComponent<PlayerInvestigationLog>()?.GetEvidencePoints(investigationCase) ?? 0, requiredValue);
            case DialogConditionType.InvestigationStageAtLeast:
                return Compare(context?.GetPlayerComponent<PlayerInvestigationLog>()?.GetStageIndex(investigationCase) ?? -1, requiredValue);
            case DialogConditionType.ResearchStarted:
                return (GetResearchEntry(context) != null && GetResearchEntry(context).points > 0) == expectedBool;
            case DialogConditionType.ResearchCompleted:
                return (context?.GetPlayerComponent<PlayerResearchLog>()?.IsCompleted(researchSubject) ?? false) == expectedBool;
            case DialogConditionType.ResearchPointsAtLeast:
                return Compare(GetResearchEntry(context)?.points ?? 0, requiredValue);
            case DialogConditionType.CompanionBondLevel:
                return Compare((int)(context?.GetSpeakerComponent<CompanionController>()?.BondLevel ?? CompanionBondLevel.Stranger), (int)companionBondLevel);
            case DialogConditionType.SpeakerPersonality:
                return (GetSpeakerPersonality(context) == personalityId) == expectedBool;
            case DialogConditionType.SpeakerPersonalityTrait:
                return Compare(context?.GetSpeakerComponent<PersonalityProfile>()?.GetTrait(personalityTrait) ?? 0, requiredValue);
            case DialogConditionType.NPCMemoryHasMet:
                return (context?.GetPlayerComponent<PlayerNPCMemoryLog>()?.HasMet(GetNPCMemoryId(context)) ?? false) == expectedBool;
            case DialogConditionType.NPCMemoryInteractionCount:
                return Compare(context?.GetPlayerComponent<PlayerNPCMemoryLog>()?.GetInteractionCount(GetNPCMemoryId(context)) ?? 0, requiredValue);
            case DialogConditionType.NPCMemoryInteractionTypeCount:
                return Compare(context?.GetPlayerComponent<PlayerNPCMemoryLog>()?.GetInteractionCountByType(GetNPCMemoryId(context), npcMemoryInteractionType) ?? 0, requiredValue);
            case DialogConditionType.NPCMemoryHasTopic:
                return (context?.GetPlayerComponent<PlayerNPCMemoryLog>()?.HasTopic(GetNPCMemoryId(context), npcMemoryTopic) ?? false) == expectedBool;
            case DialogConditionType.NPCMemoryTopicCount:
                return Compare(context?.GetPlayerComponent<PlayerNPCMemoryLog>()?.GetTopicCount(GetNPCMemoryId(context), npcMemoryTopic) ?? 0, requiredValue);
            case DialogConditionType.NPCMemoryTopicTagCount:
                return Compare(context?.GetPlayerComponent<PlayerNPCMemoryLog>()?.GetTopicCountWithTag(GetNPCMemoryId(context), requiredKey) ?? 0, requiredValue);
            case DialogConditionType.NPCMemoryTrustAtLeast:
                return Compare(context?.GetPlayerComponent<PlayerNPCMemoryLog>()?.GetTrust(GetNPCMemoryId(context)) ?? 0, requiredValue);
            case DialogConditionType.NPCMemorySuspicionAtLeast:
                return Compare(context?.GetPlayerComponent<PlayerNPCMemoryLog>()?.GetSuspicion(GetNPCMemoryId(context)) ?? 0, requiredValue);
            case DialogConditionType.NPCMemoryFamiliarityAtLeast:
                return Compare(context?.GetPlayerComponent<PlayerNPCMemoryLog>()?.GetFamiliarity(GetNPCMemoryId(context)) ?? 0, requiredValue);
            case DialogConditionType.NPCMemoryHoursSinceLastInteractionAtMost:
                return CompareAtMostNonNegative(context?.GetPlayerComponent<PlayerNPCMemoryLog>()?.GetHoursSinceLastInteraction(GetNPCMemoryId(context)) ?? -1, requiredValue);
            case DialogConditionType.NPCReactionCount:
                return Compare(context?.GetPlayerComponent<PlayerNPCReactionLog>()?.GetCount(npcReaction, GetNPCMemoryId(context), npcReactionSourceId) ?? 0, requiredValue);
            case DialogConditionType.NPCReactionTagCount:
                return Compare(context?.GetPlayerComponent<PlayerNPCReactionLog>()?.GetCountWithTag(requiredKey, GetNPCMemoryId(context), npcReactionSourceId) ?? 0, requiredValue);
            case DialogConditionType.NPCReactionCategoryCount:
                return Compare(context?.GetPlayerComponent<PlayerNPCReactionLog>()?.GetCountByCategory(npcReactionCategory, GetNPCMemoryId(context), npcReactionSourceId) ?? 0, requiredValue);
            case DialogConditionType.NPCReactionHoursSinceLastAtMost:
                return CompareAtMostNonNegative(context?.GetPlayerComponent<PlayerNPCReactionLog>()?.GetHoursSinceLastReaction(npcReaction, GetNPCMemoryId(context), npcReactionSourceId) ?? -1, requiredValue);
            case DialogConditionType.WitnessReportCount:
                return Compare(context?.GetPlayerComponent<PlayerWitnessReportLog>()?.GetCount(witnessReport, GetNPCMemoryId(context), witnessReportSourceId, witnessReportAuthorityId) ?? 0, requiredValue);
            case DialogConditionType.WitnessReportTagCount:
                return Compare(context?.GetPlayerComponent<PlayerWitnessReportLog>()?.GetCountWithTag(requiredKey, GetNPCMemoryId(context), witnessReportSourceId, witnessReportAuthorityId) ?? 0, requiredValue);
            case DialogConditionType.WitnessReportCategoryCount:
                return Compare(context?.GetPlayerComponent<PlayerWitnessReportLog>()?.GetCountByCategory(witnessReportCategory, GetNPCMemoryId(context), witnessReportSourceId, witnessReportAuthorityId) ?? 0, requiredValue);
            case DialogConditionType.WitnessReportHoursSinceLastAtMost:
                return CompareAtMostNonNegative(context?.GetPlayerComponent<PlayerWitnessReportLog>()?.GetHoursSinceLastReport(witnessReport, GetNPCMemoryId(context), witnessReportSourceId, witnessReportAuthorityId) ?? -1, requiredValue);
            case DialogConditionType.ReportPropagationCount:
                return Compare(context?.GetPlayerComponent<PlayerReportPropagationLog>()?.GetCount(reportPropagation, witnessReport, reportPropagationTargetId, reportPropagationSourceId) ?? 0, requiredValue);
            case DialogConditionType.ReportPropagationTagCount:
                return Compare(context?.GetPlayerComponent<PlayerReportPropagationLog>()?.GetCountWithTag(requiredKey, reportPropagationTargetId, reportPropagationSourceId) ?? 0, requiredValue);
            case DialogConditionType.ReportPropagationCategoryCount:
                return Compare(context?.GetPlayerComponent<PlayerReportPropagationLog>()?.GetCountByCategory(reportPropagationCategory, reportPropagationTargetId, reportPropagationSourceId) ?? 0, requiredValue);
            case DialogConditionType.ReportPropagationHoursSinceLastAtMost:
                return CompareAtMostNonNegative(context?.GetPlayerComponent<PlayerReportPropagationLog>()?.GetHoursSinceLastPropagation(reportPropagation, witnessReport, reportPropagationTargetId, reportPropagationSourceId) ?? -1, requiredValue);
            default:
                return false;
        }
    }

    PlayerProgression GetPlayerProgression(DialogContext context) {
        return context?.GetPlayerComponent<PlayerProgression>();
    }

    bool HasPokemon(DialogContext context, PokemonBase pokemonBase) {
        if(pokemonBase == null) {
            return false;
        }

        var party = context?.GetPlayerComponent<PokemonParty>();
        return party != null && party.Pokemons != null && party.Pokemons.Any(p => p != null && p.Base == pokemonBase);
    }

    int GetHighestPartyLevel(DialogContext context) {
        var party = context?.GetPlayerComponent<PokemonParty>();
        if(party == null || party.Pokemons == null || party.Pokemons.Count == 0) {
            return 0;
        }

        return party.Pokemons.Where(p => p != null).Select(p => p.Level).DefaultIfEmpty(0).Max();
    }

    PersonalityID GetSpeakerPersonality(DialogContext context) {
        var profile = context?.GetSpeakerComponent<PersonalityProfile>();
        return profile != null ? profile.PersonalityID : PersonalityID.Balanced;
    }

    string GetNPCMemoryId(DialogContext context) {
        if(!string.IsNullOrWhiteSpace(npcMemoryId)) {
            return npcMemoryId;
        }

        var profile = context?.GetSpeakerComponent<NPCMemoryProfile>();
        if(profile != null) {
            return profile.NpcId;
        }

        if(!string.IsNullOrWhiteSpace(context?.SpeakerId)) {
            return context.SpeakerId;
        }

        return context?.Speaker != null ? context.Speaker.name : null;
    }

    int GetEncounterCount(DialogContext context, EncounterLogCountType countType) {
        return context?.GetPlayerComponent<PlayerEncounterLog>()?.GetCount(pokemon, encounterSourceType, countType) ?? 0;
    }

    string GetMapMarkerId() {
        return !string.IsNullOrWhiteSpace(mapMarkerId) ? mapMarkerId : mapMarker != null ? mapMarker.Id : string.Empty;
    }

    bool HasUnlockedBattleRuleWithTag(DialogContext context) {
        if(context == null || string.IsNullOrWhiteSpace(requiredKey)) {
            return false;
        }

        var log = context.GetPlayerComponent<PlayerBattleRuleLog>();
        if(log == null) {
            return false;
        }

        foreach(var candidate in Resources.LoadAll<BattleRuleSetDefinition>("")) {
            if(candidate != null && candidate.HasTag(requiredKey) && log.HasUnlockedRuleSet(candidate)) {
                return true;
            }
        }

        return false;
    }

    ResearchEntry GetResearchEntry(DialogContext context) {
        return context?.GetPlayerComponent<PlayerResearchLog>()?.GetEntry(researchSubject);
    }

    string GetLawAuthorityId() {
        if(!string.IsNullOrWhiteSpace(lawAuthorityId)) {
            return lawAuthorityId;
        }

        return lawAuthorityFaction != null ? lawAuthorityFaction.Id : null;
    }

    bool Compare(int currentValue, int targetValue) {
        switch(comparison) {
            case DialogComparison.Equal:
                return currentValue == targetValue;
            case DialogComparison.NotEqual:
                return currentValue != targetValue;
            case DialogComparison.Greater:
                return currentValue > targetValue;
            case DialogComparison.GreaterOrEqual:
                return currentValue >= targetValue;
            case DialogComparison.Less:
                return currentValue < targetValue;
            case DialogComparison.LessOrEqual:
                return currentValue <= targetValue;
            default:
                return false;
        }
    }

    bool CompareAtMostNonNegative(int currentValue, int targetValue) {
        return currentValue >= 0 && currentValue <= Mathf.Max(0, targetValue);
    }
}
