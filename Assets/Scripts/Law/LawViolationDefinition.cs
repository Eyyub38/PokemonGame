using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum LawViolationCategory {
    General,
    Trespass,
    Theft,
    Assault,
    IllegalCapture,
    IllegalHarvest,
    IllegalMining,
    IllegalResearch,
    Curfew,
    Contraband,
    BattleRuleBreak,
    Custom
}

public enum LawViolationSeverity {
    Minor,
    Moderate,
    Major,
    Severe,
    Critical
}

[CreateAssetMenu(menuName = "Law/Law Violation Definition")]
public class LawViolationDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this law violation. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in debug and future UI. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing explanation of this violation.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad violation category used by filters, dialog and future UI.")]
    [SerializeField] LawViolationCategory category = LawViolationCategory.General;
    [Tooltip("Severity used by wanted scoring, sorting and future UI styling.")]
    [SerializeField] LawViolationSeverity severity = LawViolationSeverity.Minor;
    [Tooltip("Free-form tags used by requirements, dialog and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Authority")]
    [Tooltip("Optional faction that acts as authority for this violation, such as local police or a professor guild.")]
    [SerializeField] ReputationFactionDefinition authorityFaction;
    [Tooltip("Fallback authority id used when Authority Faction is empty. Empty uses global.")]
    [SerializeField] string authorityIdOverride;
    [Tooltip("Fallback authority name used when Authority Faction is empty.")]
    [SerializeField] string authorityNameOverride;

    [Header("Penalty")]
    [Tooltip("Wanted score added when this violation is recorded.")]
    [Min(0)]
    [SerializeField] int wantedPoints = 1;
    [Tooltip("Money owed as a fine when this violation is recorded.")]
    [Min(0f)]
    [SerializeField] float fineAmount;
    [Tooltip("If enabled, applying this violation also changes faction reputation.")]
    [SerializeField] bool applyReputationChanges = true;
    [Tooltip("Faction reputation changes applied when this violation is recorded.")]
    [SerializeField] List<ReputationChange> reputationChanges = new List<ReputationChange>();
    [Tooltip("Milestones completed when this violation is recorded.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Titles, badges or marks granted when this violation is recorded.")]
    [SerializeField] List<TitleGrant> titleGrants = new List<TitleGrant>();

    [Header("Messages")]
    [Tooltip("Message used when this violation is recorded. Empty generates a default message.")]
    [SerializeField] string reportedMessage;
    [Tooltip("Message used when this violation fine is paid. Empty generates a default message.")]
    [SerializeField] string finePaidMessage;
    [Tooltip("Message used when this violation authority is pardoned or cleared. Empty generates a default message.")]
    [SerializeField] string pardonedMessage;

    [Header("Events")]
    [Tooltip("Optional event published when this violation is recorded.")]
    [SerializeField] GameEventDefinition reportedEvent;
    [Tooltip("Optional event published when a related fine payment is made.")]
    [SerializeField] GameEventDefinition finePaidEvent;
    [Tooltip("Optional event published when a related wanted/fine state is pardoned.")]
    [SerializeField] GameEventDefinition pardonedEvent;
    [Tooltip("If enabled, law events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, law events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public LawViolationCategory Category => category;
    public LawViolationSeverity Severity => severity;
    public IReadOnlyList<string> Tags => tags;
    public ReputationFactionDefinition AuthorityFaction => authorityFaction;
    public string AuthorityId => authorityFaction != null ? authorityFaction.Id : string.IsNullOrWhiteSpace(authorityIdOverride) ? "global" : authorityIdOverride;
    public string AuthorityName => authorityFaction != null ? authorityFaction.DisplayName : string.IsNullOrWhiteSpace(authorityNameOverride) ? AuthorityId : authorityNameOverride;
    public int WantedPoints => Mathf.Max(0, wantedPoints);
    public float FineAmount => Mathf.Max(0f, fineAmount);
    public IReadOnlyList<ReputationChange> ReputationChanges => reputationChanges;
    public IReadOnlyList<MilestoneDefinition> MilestonesToComplete => milestonesToComplete;
    public IReadOnlyList<TitleGrant> TitleGrants => titleGrants;

    public void ApplyConsequences(PlayerController player) {
        if(player == null) {
            return;
        }

        if(applyReputationChanges) {
            player.GetComponent<PlayerReputation>()?.ApplyChanges(reputationChanges);
        }

        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);
        player.GetComponent<PlayerTitles>()?.ApplyGrants(titleGrants, this);
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public void PublishReported(PlayerController player, string sourceId, string reporterId, UnityEngine.Object context) {
        PublishLawEvent(
            reportedEvent,
            $"law.violation.{Id}",
            string.IsNullOrWhiteSpace(reportedMessage) ? $"{DisplayName} reported." : reportedMessage,
            GameEventImportance.Warning,
            player,
            context,
            "reported",
            sourceId,
            reporterId,
            null);
    }

    public void PublishFinePaid(PlayerController player, string authorityId, float amount, UnityEngine.Object context) {
        PublishLawEvent(
            finePaidEvent,
            $"law.fine-paid.{authorityId}",
            string.IsNullOrWhiteSpace(finePaidMessage) ? $"Paid {amount:0} fine to {AuthorityName}." : finePaidMessage,
            GameEventImportance.Success,
            player,
            context,
            "finePaid",
            authorityId,
            null,
            amount);
    }

    public void PublishPardoned(PlayerController player, string authorityId, UnityEngine.Object context) {
        PublishLawEvent(
            pardonedEvent,
            $"law.pardoned.{authorityId}",
            string.IsNullOrWhiteSpace(pardonedMessage) ? $"{AuthorityName} record cleared." : pardonedMessage,
            GameEventImportance.Success,
            player,
            context,
            "pardoned",
            authorityId,
            null,
            null);
    }

    void PublishLawEvent(
        GameEventDefinition eventDefinition,
        string fallbackId,
        string message,
        GameEventImportance importance,
        PlayerController player,
        UnityEngine.Object context,
        string phase,
        string sourceId,
        string reporterId,
        float? amount
    ) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            fallbackId,
            message,
            GameEventCategory.Law,
            importance,
            context != null ? context : player,
            "LawViolationDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("violationId", Id),
            GameEventPublishing.Value("violationName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("severity", severity),
            GameEventPublishing.Value("authorityId", AuthorityId),
            GameEventPublishing.Value("authorityName", AuthorityName),
            GameEventPublishing.Value("wantedPoints", WantedPoints),
            GameEventPublishing.Value("fineAmount", FineAmount),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("reporterId", reporterId),
            GameEventPublishing.Value("amount", amount));
    }
}
