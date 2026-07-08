using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum WitnessReportCategory {
    General,
    Crime,
    Trespass,
    Theft,
    Battle,
    Help,
    Research,
    PokemonCare,
    Assignment,
    Investigation,
    Shop,
    Rumor,
    Custom
}

public enum WitnessReportRepeatPolicy {
    Always,
    OnceGlobally,
    OncePerReporter,
    OncePerReporterAndSource,
    LimitedGlobally,
    LimitedPerReporter,
    LimitedPerReporterAndSource
}

[CreateAssetMenu(menuName = "Witnesses/Witness Report Definition")]
public class WitnessReportDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this witness report. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in debug and future UI. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note explaining what kind of witnessed event this report represents.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad report category used by requirements, dialog and future UI filters.")]
    [SerializeField] WitnessReportCategory category = WitnessReportCategory.General;
    [Tooltip("Free-form tags used by requirements, dialog and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Priority hint for future systems that choose between multiple possible witness reports.")]
    [SerializeField] int priority;

    [Header("Requirements")]
    [Tooltip("Player requirements that must be met before this report can apply.")]
    [SerializeField] List<ActivityRequirement> playerRequirements = new List<ActivityRequirement>();

    [Header("Repeat Rules")]
    [Tooltip("How often this report can be applied.")]
    [SerializeField] WitnessReportRepeatPolicy repeatPolicy = WitnessReportRepeatPolicy.Always;
    [Tooltip("Maximum reports used by Limited repeat policies.")]
    [Min(1)]
    [SerializeField] int maxReports = 1;
    [Tooltip("Minimum in-game hours before the same matching report can apply again. 0 disables cooldown.")]
    [Min(0)]
    [SerializeField] int cooldownHours;

    [Header("Authority")]
    [Tooltip("Faction or authority that receives this report. Empty can use reporter faction or global.")]
    [SerializeField] ReputationFactionDefinition authorityFaction;
    [Tooltip("Fallback authority id used when Authority Faction is empty.")]
    [SerializeField] string authorityIdOverride;
    [Tooltip("Fallback authority name used when Authority Faction is empty.")]
    [SerializeField] string authorityNameOverride;
    [Tooltip("If enabled and no authority is assigned, the reporter NPC's reputation faction becomes the authority.")]
    [SerializeField] bool useReporterFactionAsFallback = true;

    [Header("Witness Memory")]
    [Tooltip("If enabled, the reporter remembers this witnessed event in PlayerNPCMemoryLog.")]
    [SerializeField] bool recordWitnessMemory = true;
    [Tooltip("Interaction type stored in PlayerNPCMemoryLog when witness memory is recorded.")]
    [SerializeField] NPCInteractionMemoryType memoryInteractionType = NPCInteractionMemoryType.Law;
    [Tooltip("Optional memory topic remembered by the reporter.")]
    [SerializeField] NPCMemoryTopicDefinition memoryTopic;
    [Tooltip("Trust added to the reporter memory.")]
    [SerializeField] int trustDelta;
    [Tooltip("Suspicion added to the reporter memory.")]
    [SerializeField] int suspicionDelta = 1;
    [Tooltip("Familiarity added to the reporter memory.")]
    [SerializeField] int familiarityDelta;

    [Header("NPC Reactions")]
    [Tooltip("NPC reactions applied to the reporter when this witness report is recorded.")]
    [SerializeField] List<NPCReactionDefinition> witnessReactions = new List<NPCReactionDefinition>();

    [Header("Propagation")]
    [Tooltip("Propagation rules applied after this report is recorded, such as notifying police, professors, shops or townsfolk.")]
    [SerializeField] List<ReportPropagationDefinition> propagations = new List<ReportPropagationDefinition>();

    [Header("Relationship")]
    [Tooltip("Relationship delta applied to the reporter NPC's Relationship Subject from NPCMemoryProfile.")]
    [SerializeField] int reporterRelationshipDelta;
    [Tooltip("Extra relationship changes applied regardless of the reporter NPC.")]
    [SerializeField] List<RelationshipChange> relationshipChanges = new List<RelationshipChange>();

    [Header("Reputation")]
    [Tooltip("Reputation delta applied to the reporter NPC's Reputation Faction from NPCMemoryProfile.")]
    [SerializeField] int reporterFactionReputationDelta;
    [Tooltip("Extra reputation changes applied regardless of the reporter NPC.")]
    [SerializeField] List<ReputationChange> reputationChanges = new List<ReputationChange>();

    [Header("Progression")]
    [Tooltip("Milestones completed when this report is recorded.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Titles, badges, marks or permits granted when this report is recorded.")]
    [SerializeField] List<TitleGrant> titleGrants = new List<TitleGrant>();

    [Header("Law")]
    [Tooltip("If enabled, this report records a law violation through PlayerLawLog.")]
    [SerializeField] bool recordLawViolation;
    [Tooltip("Law violation recorded when Record Law Violation is enabled.")]
    [SerializeField] LawViolationDefinition lawViolation;
    [Tooltip("If enabled, the law violation also applies its configured consequences.")]
    [SerializeField] bool applyLawConsequences = true;

    [Header("Risk")]
    [Tooltip("If enabled, this report also records a risk incident through PlayerRiskLog.")]
    [SerializeField] bool recordRiskIncident = false;
    [Tooltip("Risk incident recorded when Record Risk Incident is enabled.")]
    [SerializeField] RiskIncidentDefinition riskIncident = null;
    [Tooltip("If enabled, the risk incident also applies its configured consequences.")]
    [SerializeField] bool applyRiskConsequences = true;

    [Header("Events")]
    [Tooltip("Optional event published when this witness report is recorded.")]
    [SerializeField] GameEventDefinition reportedEvent;
    [Tooltip("If enabled, witness report events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, witness report events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public WitnessReportCategory Category => category;
    public IReadOnlyList<string> Tags => tags;
    public int Priority => priority;
    public IReadOnlyList<ActivityRequirement> PlayerRequirements => playerRequirements;
    public WitnessReportRepeatPolicy RepeatPolicy => repeatPolicy;
    public int MaxReports => Mathf.Max(1, maxReports);
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public IReadOnlyList<NPCReactionDefinition> WitnessReactions => witnessReactions;
    public IReadOnlyList<ReportPropagationDefinition> Propagations => propagations;
    public IReadOnlyList<RelationshipChange> RelationshipChanges => relationshipChanges;
    public IReadOnlyList<ReputationChange> ReputationChanges => reputationChanges;
    public IReadOnlyList<MilestoneDefinition> MilestonesToComplete => milestonesToComplete;
    public IReadOnlyList<TitleGrant> TitleGrants => titleGrants;
    public bool RecordLawViolation => recordLawViolation;
    public LawViolationDefinition LawViolation => lawViolation;
    public bool RecordRiskIncident => recordRiskIncident;
    public RiskIncidentDefinition RiskIncident => riskIncident;

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public string ResolveAuthorityId(NPCMemoryProfile reporter) {
        if(authorityFaction != null) {
            return authorityFaction.Id;
        }

        if(!string.IsNullOrWhiteSpace(authorityIdOverride)) {
            return authorityIdOverride;
        }

        if(useReporterFactionAsFallback && reporter != null && reporter.ReputationFaction != null) {
            return reporter.ReputationFaction.Id;
        }

        return "global";
    }

    public string ResolveAuthorityName(NPCMemoryProfile reporter) {
        if(authorityFaction != null) {
            return authorityFaction.DisplayName;
        }

        if(!string.IsNullOrWhiteSpace(authorityNameOverride)) {
            return authorityNameOverride;
        }

        if(useReporterFactionAsFallback && reporter != null && reporter.ReputationFaction != null) {
            return reporter.ReputationFaction.DisplayName;
        }

        return ResolveAuthorityId(reporter);
    }

    public ReputationFactionDefinition ResolveAuthorityFaction(NPCMemoryProfile reporter) {
        if(authorityFaction != null) {
            return authorityFaction;
        }

        if(useReporterFactionAsFallback && reporter != null) {
            return reporter.ReputationFaction;
        }

        return null;
    }

    public bool CanReport(PlayerController player, NPCMemoryProfile reporter, PlayerWitnessReportLog log, string sourceId, out string failureMessage) {
        failureMessage = null;
        if(player == null) {
            failureMessage = "Player is missing.";
            return false;
        }

        if(playerRequirements != null) {
            foreach(var requirement in playerRequirements) {
                if(requirement == null) {
                    continue;
                }

                if(!requirement.IsMet(player)) {
                    failureMessage = requirement.FailureMessage;
                    return false;
                }
            }
        }

        if(log == null) {
            return true;
        }

        string reporterId = reporter != null ? reporter.NpcId : null;
        if(CooldownHours > 0) {
            int hours = log.GetHoursSinceLastReport(this, GetCooldownReporterFilter(reporterId), GetCooldownSourceFilter(sourceId));
            if(hours >= 0 && hours < CooldownHours) {
                failureMessage = $"Witness report is on cooldown for {CooldownHours - hours} more hour(s).";
                return false;
            }
        }

        int count = GetRepeatCount(log, reporterId, sourceId);
        switch(repeatPolicy) {
            case WitnessReportRepeatPolicy.OnceGlobally:
            case WitnessReportRepeatPolicy.OncePerReporter:
            case WitnessReportRepeatPolicy.OncePerReporterAndSource:
                return count <= 0;
            case WitnessReportRepeatPolicy.LimitedGlobally:
            case WitnessReportRepeatPolicy.LimitedPerReporter:
            case WitnessReportRepeatPolicy.LimitedPerReporterAndSource:
                return count < MaxReports;
            default:
                return true;
        }
    }

    public bool Apply(PlayerController player, NPCMemoryProfile reporter, string sourceId, UnityEngine.Object context, out string failureMessage) {
        var log = player != null ? player.GetComponent<PlayerWitnessReportLog>() : null;
        if(log == null && player != null) {
            log = player.gameObject.AddComponent<PlayerWitnessReportLog>();
        }

        if(!CanReport(player, reporter, log, sourceId, out failureMessage)) {
            return false;
        }

        ApplyWitnessMemory(player, reporter, sourceId);
        ApplyRelationshipChanges(player, reporter);
        ApplyReputationChanges(player, reporter);
        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);
        player.GetComponent<PlayerTitles>()?.ApplyGrants(titleGrants, this);

        string authorityId = ResolveAuthorityId(reporter);
        string authorityName = ResolveAuthorityName(reporter);

        if(recordLawViolation && lawViolation != null) {
            player.GetComponent<PlayerLawLog>()?.RecordViolation(lawViolation, sourceId, reporter != null ? reporter.NpcId : null, applyLawConsequences, context != null ? context : this);
        }

        if(recordRiskIncident && riskIncident != null) {
            var riskLog = player.GetComponent<PlayerRiskLog>() ?? player.gameObject.AddComponent<PlayerRiskLog>();
            riskLog.RecordIncident(riskIncident, sourceId, reporter != null ? reporter.NpcId : null, null, authorityId, authorityName, applyRiskConsequences, context != null ? context : this);
        }

        ApplyWitnessReactions(player, reporter, sourceId, context);

        var record = log?.RecordReport(this, reporter, authorityId, authorityName, sourceId, context != null ? context : this);
        ApplyPropagations(player, reporter, authorityId, authorityName, sourceId, record, context);
        PublishReported(player, reporter, authorityId, authorityName, sourceId, context != null ? context : this, record);
        failureMessage = null;
        return true;
    }

    int GetRepeatCount(PlayerWitnessReportLog log, string reporterId, string sourceId) {
        switch(repeatPolicy) {
            case WitnessReportRepeatPolicy.OncePerReporter:
            case WitnessReportRepeatPolicy.LimitedPerReporter:
                return log.GetCount(this, reporterId, null);
            case WitnessReportRepeatPolicy.OncePerReporterAndSource:
            case WitnessReportRepeatPolicy.LimitedPerReporterAndSource:
                return log.GetCount(this, reporterId, sourceId);
            default:
                return log.GetCount(this, null, null);
        }
    }

    string GetCooldownReporterFilter(string reporterId) {
        return repeatPolicy == WitnessReportRepeatPolicy.OncePerReporter
            || repeatPolicy == WitnessReportRepeatPolicy.OncePerReporterAndSource
            || repeatPolicy == WitnessReportRepeatPolicy.LimitedPerReporter
            || repeatPolicy == WitnessReportRepeatPolicy.LimitedPerReporterAndSource
            ? reporterId
            : null;
    }

    string GetCooldownSourceFilter(string sourceId) {
        return repeatPolicy == WitnessReportRepeatPolicy.OncePerReporterAndSource
            || repeatPolicy == WitnessReportRepeatPolicy.LimitedPerReporterAndSource
            ? sourceId
            : null;
    }

    void ApplyWitnessMemory(PlayerController player, NPCMemoryProfile reporter, string sourceId) {
        if(!recordWitnessMemory || reporter == null) {
            return;
        }

        reporter.RecordInteraction(player, memoryInteractionType, memoryTopic, trustDelta, suspicionDelta, familiarityDelta, sourceId);
    }

    void ApplyWitnessReactions(PlayerController player, NPCMemoryProfile reporter, string sourceId, UnityEngine.Object context) {
        if(witnessReactions == null) {
            return;
        }

        foreach(var reaction in witnessReactions) {
            if(reaction != null) {
                reaction.Apply(player, reporter, sourceId, context != null ? context : this, out _);
            }
        }
    }

    void ApplyPropagations(PlayerController player, NPCMemoryProfile reporter, string authorityId, string authorityName, string sourceId, WitnessReportRecord record, UnityEngine.Object context) {
        if(propagations == null) {
            return;
        }

        foreach(var propagation in propagations) {
            propagation?.Apply(player, this, reporter, authorityId, authorityName, sourceId, record, context != null ? context : this);
        }
    }

    void ApplyRelationshipChanges(PlayerController player, NPCMemoryProfile reporter) {
        var relationships = player.GetComponent<PlayerRelationships>();
        if(relationships == null) {
            return;
        }

        if(reporter != null && reporter.RelationshipSubject != null && reporterRelationshipDelta != 0) {
            relationships.AddRelationship(reporter.RelationshipSubject, reporterRelationshipDelta);
        }

        relationships.ApplyChanges(relationshipChanges);
    }

    void ApplyReputationChanges(PlayerController player, NPCMemoryProfile reporter) {
        var reputation = player.GetComponent<PlayerReputation>();
        if(reputation == null) {
            return;
        }

        if(reporter != null && reporter.ReputationFaction != null && reporterFactionReputationDelta != 0) {
            reputation.AddReputation(reporter.ReputationFaction, reporterFactionReputationDelta);
        }

        reputation.ApplyChanges(reputationChanges);
    }

    void PublishReported(PlayerController player, NPCMemoryProfile reporter, string authorityId, string authorityName, string sourceId, UnityEngine.Object context, WitnessReportRecord record) {
        GameEventPublishing.PublishOptional(
            reportedEvent,
            $"witness.reported.{Id}",
            $"{DisplayName} reported.",
            recordLawViolation ? GameEventCategory.Law : GameEventCategory.NPC,
            recordLawViolation ? GameEventImportance.Warning : GameEventImportance.Info,
            context != null ? context : player,
            "WitnessReportDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("reportId", Id),
            GameEventPublishing.Value("reportName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("reporterId", reporter != null ? reporter.NpcId : string.Empty),
            GameEventPublishing.Value("reporterName", reporter != null ? reporter.DisplayName : string.Empty),
            GameEventPublishing.Value("authorityId", authorityId),
            GameEventPublishing.Value("authorityName", authorityName),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("recordId", record != null ? record.recordId : string.Empty));
    }
}
