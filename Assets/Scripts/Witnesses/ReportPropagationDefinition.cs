using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ReportPropagationCategory {
    General,
    Law,
    Police,
    Professor,
    Town,
    Shop,
    Research,
    Organization,
    Career,
    Rumor,
    Custom
}

public enum ReportPropagationRepeatPolicy {
    Always,
    OnceGlobally,
    OncePerReport,
    OncePerReportAndTarget,
    OncePerReportTargetAndSource,
    LimitedGlobally,
    LimitedPerReport,
    LimitedPerReportAndTarget,
    LimitedPerReportTargetAndSource
}

public enum ReportPropagationTargetType {
    ReportAuthority,
    ReporterFaction,
    ExplicitFaction,
    RelationshipSubject,
    Organization,
    Career,
    CustomGroup
}

[CreateAssetMenu(menuName = "Witnesses/Report Propagation Definition")]
public class ReportPropagationDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this propagation. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in debug and future UI. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note explaining how this report propagation spreads information.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad propagation category used by requirements, dialog and future UI filters.")]
    [SerializeField] ReportPropagationCategory category = ReportPropagationCategory.General;
    [Tooltip("Free-form tags used by requirements, dialog and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Priority hint for future systems that choose between multiple propagation rules.")]
    [SerializeField] int priority;

    [Header("Requirements")]
    [Tooltip("Player requirements that must be met before this propagation can apply.")]
    [SerializeField] List<ActivityRequirement> playerRequirements = new List<ActivityRequirement>();

    [Header("Repeat Rules")]
    [Tooltip("How often this propagation can be recorded.")]
    [SerializeField] ReportPropagationRepeatPolicy repeatPolicy = ReportPropagationRepeatPolicy.Always;
    [Tooltip("Maximum records used by Limited repeat policies.")]
    [Min(1)]
    [SerializeField] int maxRecords = 1;
    [Tooltip("Minimum in-game hours before the same matching propagation can apply again. 0 disables cooldown.")]
    [Min(0)]
    [SerializeField] int cooldownHours;

    [Header("Targets")]
    [Tooltip("Targets that receive the propagated report. Empty can use the source report authority as a default target.")]
    [SerializeField] List<ReportPropagationTarget> targets = new List<ReportPropagationTarget>();
    [Tooltip("If enabled and Targets is empty, the report authority receives one propagation record.")]
    [SerializeField] bool useReportAuthorityWhenNoTargets = true;

    [Header("Shared Effects")]
    [Tooltip("Relationship changes applied once if at least one target receives this propagation.")]
    [SerializeField] List<RelationshipChange> relationshipChanges = new List<RelationshipChange>();
    [Tooltip("Reputation changes applied once if at least one target receives this propagation.")]
    [SerializeField] List<ReputationChange> reputationChanges = new List<ReputationChange>();
    [Tooltip("Milestones completed once if at least one target receives this propagation.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Titles, badges, marks or permits granted once if at least one target receives this propagation.")]
    [SerializeField] List<TitleGrant> titleGrants = new List<TitleGrant>();

    [Header("Events")]
    [Tooltip("Optional event published when this propagation reaches a target.")]
    [SerializeField] GameEventDefinition propagatedEvent;
    [Tooltip("If enabled, propagation events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, propagation events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public ReportPropagationCategory Category => category;
    public IReadOnlyList<string> Tags => tags;
    public int Priority => priority;
    public IReadOnlyList<ActivityRequirement> PlayerRequirements => playerRequirements;
    public ReportPropagationRepeatPolicy RepeatPolicy => repeatPolicy;
    public int MaxRecords => Mathf.Max(1, maxRecords);
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public IReadOnlyList<ReportPropagationTarget> Targets => targets;
    public IReadOnlyList<RelationshipChange> RelationshipChanges => relationshipChanges;
    public IReadOnlyList<ReputationChange> ReputationChanges => reputationChanges;
    public IReadOnlyList<MilestoneDefinition> MilestonesToComplete => milestonesToComplete;
    public IReadOnlyList<TitleGrant> TitleGrants => titleGrants;

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && tags != null
            && tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }

    public int Apply(
        PlayerController player,
        WitnessReportDefinition sourceReport,
        NPCMemoryProfile reporter,
        string authorityId,
        string authorityName,
        string sourceId,
        WitnessReportRecord sourceRecord,
        UnityEngine.Object context
    ) {
        if(player == null || sourceReport == null) {
            return 0;
        }

        var log = player.GetComponent<PlayerReportPropagationLog>();
        if(log == null) {
            log = player.gameObject.AddComponent<PlayerReportPropagationLog>();
        }

        if(!MeetsRequirements(player, out _)) {
            return 0;
        }

        bool appliedSharedEffects = false;
        int applied = 0;
        foreach(var target in GetRuntimeTargets()) {
            if(target == null || !target.TryResolve(sourceReport, reporter, authorityId, authorityName, out var resolved)) {
                continue;
            }

            if(!CanApplyToTarget(log, sourceReport, resolved.targetId, sourceId)) {
                continue;
            }

            target.ApplyEffects(player, sourceReport, reporter, sourceId);
            if(!appliedSharedEffects) {
                ApplySharedEffects(player);
                appliedSharedEffects = true;
            }

            var record = log.RecordPropagation(this, sourceReport, reporter, resolved, authorityId, authorityName, sourceId, sourceRecord, context != null ? context : this);
            PublishPropagated(player, sourceReport, reporter, resolved, authorityId, authorityName, sourceId, sourceRecord, record, context != null ? context : this);
            applied++;
        }

        return applied;
    }

    bool MeetsRequirements(PlayerController player, out string failureMessage) {
        failureMessage = null;
        if(playerRequirements == null) {
            return true;
        }

        foreach(var requirement in playerRequirements) {
            if(requirement == null) {
                continue;
            }

            if(!requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        return true;
    }

    IEnumerable<ReportPropagationTarget> GetRuntimeTargets() {
        if(targets != null && targets.Count > 0) {
            return targets.Where(target => target != null);
        }

        return useReportAuthorityWhenNoTargets
            ? new[] { ReportPropagationTarget.CreateReportAuthorityTarget() }
            : Array.Empty<ReportPropagationTarget>();
    }

    bool CanApplyToTarget(PlayerReportPropagationLog log, WitnessReportDefinition sourceReport, string targetId, string sourceId) {
        if(log == null) {
            return true;
        }

        if(CooldownHours > 0) {
            int hours = log.GetHoursSinceLastPropagation(this, GetCooldownReportFilter(sourceReport), GetCooldownTargetFilter(targetId), GetCooldownSourceFilter(sourceId));
            if(hours >= 0 && hours < CooldownHours) {
                return false;
            }
        }

        int count = GetRepeatCount(log, sourceReport, targetId, sourceId);
        switch(repeatPolicy) {
            case ReportPropagationRepeatPolicy.OnceGlobally:
            case ReportPropagationRepeatPolicy.OncePerReport:
            case ReportPropagationRepeatPolicy.OncePerReportAndTarget:
            case ReportPropagationRepeatPolicy.OncePerReportTargetAndSource:
                return count <= 0;
            case ReportPropagationRepeatPolicy.LimitedGlobally:
            case ReportPropagationRepeatPolicy.LimitedPerReport:
            case ReportPropagationRepeatPolicy.LimitedPerReportAndTarget:
            case ReportPropagationRepeatPolicy.LimitedPerReportTargetAndSource:
                return count < MaxRecords;
            default:
                return true;
        }
    }

    int GetRepeatCount(PlayerReportPropagationLog log, WitnessReportDefinition sourceReport, string targetId, string sourceId) {
        switch(repeatPolicy) {
            case ReportPropagationRepeatPolicy.OncePerReport:
            case ReportPropagationRepeatPolicy.LimitedPerReport:
                return log.GetCount(this, sourceReport, null, null);
            case ReportPropagationRepeatPolicy.OncePerReportAndTarget:
            case ReportPropagationRepeatPolicy.LimitedPerReportAndTarget:
                return log.GetCount(this, sourceReport, targetId, null);
            case ReportPropagationRepeatPolicy.OncePerReportTargetAndSource:
            case ReportPropagationRepeatPolicy.LimitedPerReportTargetAndSource:
                return log.GetCount(this, sourceReport, targetId, sourceId);
            default:
                return log.GetCount(this, null, null, null);
        }
    }

    WitnessReportDefinition GetCooldownReportFilter(WitnessReportDefinition sourceReport) {
        return repeatPolicy == ReportPropagationRepeatPolicy.OncePerReport
            || repeatPolicy == ReportPropagationRepeatPolicy.OncePerReportAndTarget
            || repeatPolicy == ReportPropagationRepeatPolicy.OncePerReportTargetAndSource
            || repeatPolicy == ReportPropagationRepeatPolicy.LimitedPerReport
            || repeatPolicy == ReportPropagationRepeatPolicy.LimitedPerReportAndTarget
            || repeatPolicy == ReportPropagationRepeatPolicy.LimitedPerReportTargetAndSource
            ? sourceReport
            : null;
    }

    string GetCooldownTargetFilter(string targetId) {
        return repeatPolicy == ReportPropagationRepeatPolicy.OncePerReportAndTarget
            || repeatPolicy == ReportPropagationRepeatPolicy.OncePerReportTargetAndSource
            || repeatPolicy == ReportPropagationRepeatPolicy.LimitedPerReportAndTarget
            || repeatPolicy == ReportPropagationRepeatPolicy.LimitedPerReportTargetAndSource
            ? targetId
            : null;
    }

    string GetCooldownSourceFilter(string sourceId) {
        return repeatPolicy == ReportPropagationRepeatPolicy.OncePerReportTargetAndSource
            || repeatPolicy == ReportPropagationRepeatPolicy.LimitedPerReportTargetAndSource
            ? sourceId
            : null;
    }

    void ApplySharedEffects(PlayerController player) {
        player.GetComponent<PlayerRelationships>()?.ApplyChanges(relationshipChanges);
        player.GetComponent<PlayerReputation>()?.ApplyChanges(reputationChanges);
        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);
        player.GetComponent<PlayerTitles>()?.ApplyGrants(titleGrants, this);
    }

    void PublishPropagated(
        PlayerController player,
        WitnessReportDefinition sourceReport,
        NPCMemoryProfile reporter,
        ReportPropagationResolvedTarget target,
        string authorityId,
        string authorityName,
        string sourceId,
        WitnessReportRecord sourceRecord,
        ReportPropagationRecord record,
        UnityEngine.Object context
    ) {
        GameEventPublishing.PublishOptional(
            propagatedEvent,
            $"report-propagation.{Id}.{target.targetId}",
            $"{DisplayName} reached {target.targetName}.",
            GameEventCategory.NPC,
            GameEventImportance.Info,
            context != null ? context : player,
            "ReportPropagationDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("propagationId", Id),
            GameEventPublishing.Value("propagationName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("reportId", sourceReport.Id),
            GameEventPublishing.Value("reportName", sourceReport.DisplayName),
            GameEventPublishing.Value("reporterId", reporter != null ? reporter.NpcId : string.Empty),
            GameEventPublishing.Value("reporterName", reporter != null ? reporter.DisplayName : string.Empty),
            GameEventPublishing.Value("authorityId", authorityId),
            GameEventPublishing.Value("authorityName", authorityName),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("sourceRecordId", sourceRecord != null ? sourceRecord.recordId : string.Empty),
            GameEventPublishing.Value("targetType", target.targetType),
            GameEventPublishing.Value("targetId", target.targetId),
            GameEventPublishing.Value("targetName", target.targetName),
            GameEventPublishing.Value("recordId", record != null ? record.recordId : string.Empty));
    }
}

[Serializable]
public class ReportPropagationTarget {
    [Tooltip("Kind of target that receives this propagated report.")]
    public ReportPropagationTargetType targetType = ReportPropagationTargetType.ReportAuthority;
    [Tooltip("Explicit faction used by Explicit Faction targets, or as an optional faction for Report Authority targets.")]
    public ReputationFactionDefinition faction;
    [Tooltip("Relationship subject used by Relationship Subject targets.")]
    public RelationshipSubjectDefinition relationshipSubject;
    [Tooltip("Organization used by Organization targets.")]
    public OrganizationDefinition organization;
    [Tooltip("Career used by Career targets.")]
    public CareerPathDefinition career;
    [Tooltip("Custom target id used by Custom Group targets.")]
    public string customTargetId;
    [Tooltip("Custom target name used by Custom Group targets. Empty uses the custom id.")]
    public string customTargetName;

    [Header("Target Effects")]
    [Tooltip("Reputation added to the resolved faction target. Negative values reduce reputation.")]
    public int reputationDelta;
    [Tooltip("Relationship added to the resolved relationship target. Negative values reduce relationship.")]
    public int relationshipDelta;
    [Tooltip("Organization points added to the resolved organization target.")]
    [Min(0)]
    public int organizationPoints;
    [Tooltip("If enabled, organization point gain can auto-join when the organization allows it.")]
    public bool autoJoinOrganizationOnPointGain = true;
    [Tooltip("Career points added to the resolved career target.")]
    [Min(0)]
    public int careerPoints;

    [Header("Extra Effects")]
    [Tooltip("Additional reputation changes applied when this target receives the report.")]
    public List<ReputationChange> reputationChanges = new List<ReputationChange>();
    [Tooltip("Additional relationship changes applied when this target receives the report.")]
    public List<RelationshipChange> relationshipChanges = new List<RelationshipChange>();
    [Tooltip("Milestones completed when this target receives the report.")]
    public List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Titles, badges, marks or permits granted when this target receives the report.")]
    public List<TitleGrant> titleGrants = new List<TitleGrant>();

    public static ReportPropagationTarget CreateReportAuthorityTarget() {
        return new ReportPropagationTarget {
            targetType = ReportPropagationTargetType.ReportAuthority
        };
    }

    public bool TryResolve(WitnessReportDefinition sourceReport, NPCMemoryProfile reporter, string authorityId, string authorityName, out ReportPropagationResolvedTarget resolved) {
        resolved = new ReportPropagationResolvedTarget {
            targetType = targetType
        };

        switch(targetType) {
            case ReportPropagationTargetType.ReportAuthority:
                resolved.targetId = string.IsNullOrWhiteSpace(authorityId) ? "global" : authorityId;
                resolved.targetName = string.IsNullOrWhiteSpace(authorityName) ? resolved.targetId : authorityName;
                resolved.faction = faction != null ? faction : sourceReport?.ResolveAuthorityFaction(reporter);
                break;
            case ReportPropagationTargetType.ReporterFaction:
                resolved.faction = reporter != null ? reporter.ReputationFaction : null;
                resolved.targetId = resolved.faction != null ? resolved.faction.Id : string.Empty;
                resolved.targetName = resolved.faction != null ? resolved.faction.DisplayName : string.Empty;
                break;
            case ReportPropagationTargetType.ExplicitFaction:
                resolved.faction = faction;
                resolved.targetId = faction != null ? faction.Id : string.Empty;
                resolved.targetName = faction != null ? faction.DisplayName : string.Empty;
                break;
            case ReportPropagationTargetType.RelationshipSubject:
                resolved.relationshipSubject = relationshipSubject;
                resolved.targetId = relationshipSubject != null ? relationshipSubject.Id : string.Empty;
                resolved.targetName = relationshipSubject != null ? relationshipSubject.DisplayName : string.Empty;
                break;
            case ReportPropagationTargetType.Organization:
                resolved.organization = organization;
                resolved.targetId = organization != null ? organization.Id : string.Empty;
                resolved.targetName = organization != null ? organization.DisplayName : string.Empty;
                break;
            case ReportPropagationTargetType.Career:
                resolved.career = career;
                resolved.targetId = career != null ? career.Id : string.Empty;
                resolved.targetName = career != null ? career.DisplayName : string.Empty;
                break;
            default:
                resolved.targetId = string.IsNullOrWhiteSpace(customTargetId) ? "custom" : customTargetId;
                resolved.targetName = string.IsNullOrWhiteSpace(customTargetName) ? resolved.targetId : customTargetName;
                break;
        }

        return !string.IsNullOrWhiteSpace(resolved.targetId);
    }

    public void ApplyEffects(PlayerController player, WitnessReportDefinition sourceReport, NPCMemoryProfile reporter, string sourceId) {
        if(player == null) {
            return;
        }

        if(TryResolve(sourceReport, reporter, sourceReport?.ResolveAuthorityId(reporter), sourceReport?.ResolveAuthorityName(reporter), out var resolved)) {
            if(resolved.faction != null && reputationDelta != 0) {
                player.GetComponent<PlayerReputation>()?.AddReputation(resolved.faction, reputationDelta);
            }

            if(resolved.relationshipSubject != null && relationshipDelta != 0) {
                player.GetComponent<PlayerRelationships>()?.AddRelationship(resolved.relationshipSubject, relationshipDelta);
            }

            if(resolved.organization != null && organizationPoints > 0) {
                player.GetComponent<PlayerOrganizationLog>()?.AddPoints(resolved.organization, organizationPoints, sourceId, autoJoinOrganizationOnPointGain);
            }

            if(resolved.career != null && careerPoints > 0) {
                player.GetComponent<PlayerCareerLog>()?.AddPoints(resolved.career, careerPoints, sourceId);
            }
        }

        player.GetComponent<PlayerReputation>()?.ApplyChanges(reputationChanges);
        player.GetComponent<PlayerRelationships>()?.ApplyChanges(relationshipChanges);
        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);
        player.GetComponent<PlayerTitles>()?.ApplyGrants(titleGrants, sourceReport);
    }
}

public class ReportPropagationResolvedTarget {
    public ReportPropagationTargetType targetType;
    public string targetId;
    public string targetName;
    public ReputationFactionDefinition faction;
    public RelationshipSubjectDefinition relationshipSubject;
    public OrganizationDefinition organization;
    public CareerPathDefinition career;
}
