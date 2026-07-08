using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RiskIncidentCategory {
    General,
    Trespass,
    Theft,
    PublicDisturbance,
    SuspiciousBehavior,
    Contraband,
    IllegalHarvest,
    IllegalMining,
    IllegalResearch,
    Battle,
    Market,
    Wildlife,
    Social,
    Authority,
    Custom
}

public enum RiskIncidentSeverity {
    Minor,
    Moderate,
    Major,
    Severe,
    Critical
}

[CreateAssetMenu(menuName = "Risk/Risk Incident Definition")]
public class RiskIncidentDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this risk incident. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining what player action this risk incident represents.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad category used by requirements, validators and future UI filters.")]
    [SerializeField] RiskIncidentCategory category = RiskIncidentCategory.General;
    [Tooltip("Severity used by sorting, future UI and escalation logic.")]
    [SerializeField] RiskIncidentSeverity severity = RiskIncidentSeverity.Minor;
    [Tooltip("Free-form tags such as shop, police, stealth, market, littering, theft or curfew.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Authority")]
    [Tooltip("Optional faction that reacts to this incident, such as police, shopkeepers or professors.")]
    [SerializeField] ReputationFactionDefinition authorityFaction = null;
    [Tooltip("Fallback authority id used when Authority Faction is empty. Empty uses global.")]
    [SerializeField] string authorityIdOverride = string.Empty;
    [Tooltip("Fallback authority name used when Authority Faction is empty.")]
    [SerializeField] string authorityNameOverride = string.Empty;

    [Header("Location")]
    [Tooltip("Default region affected by this incident. Runtime sources can override this.")]
    [SerializeField] RegionInfoDefinition defaultRegion = null;
    [Tooltip("If enabled, the incident also counts when querying all regions.")]
    [SerializeField] bool contributesToGlobalRisk = true;

    [Header("Risk Points")]
    [Tooltip("Heat added to authority attention. Use for police/security/shopkeeper pressure.")]
    [Min(0)]
    [SerializeField] int heatPoints = 1;
    [Tooltip("Suspicion added to social/NPC attention. Use for rumors, distrust and soft blocking.")]
    [Min(0)]
    [SerializeField] int suspicionPoints = 0;
    [Tooltip("Evidence added for harder-to-ignore proof. Use for cameras, witnesses or written records.")]
    [Min(0)]
    [SerializeField] int evidencePoints = 0;

    [Header("Decay")]
    [Tooltip("In-game hours this incident contributes to active risk. 0 means it contributes until cleared.")]
    [Min(0)]
    [SerializeField] int activeDurationHours = 24;
    [Tooltip("If enabled, this incident never expires by time and must be cleared or overwritten by story systems.")]
    [SerializeField] bool permanentUntilCleared = false;

    [Header("Consequences")]
    [Tooltip("If enabled, recording this risk incident also records the linked law violation.")]
    [SerializeField] bool recordLawViolation = false;
    [Tooltip("Law violation recorded when Record Law Violation is enabled.")]
    [SerializeField] LawViolationDefinition lawViolation = null;
    [Tooltip("If enabled, the linked law violation applies its configured consequences.")]
    [SerializeField] bool applyLawConsequences = true;
    [Tooltip("If enabled, reputation changes below are applied when this incident is recorded.")]
    [SerializeField] bool applyReputationChanges = false;
    [Tooltip("Faction reputation changes applied when this incident is recorded.")]
    [SerializeField] List<ReputationChange> reputationChanges = new List<ReputationChange>();
    [Tooltip("Milestones completed when this incident is recorded.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Titles, badges or marks granted when this incident is recorded.")]
    [SerializeField] List<TitleGrant> titleGrants = new List<TitleGrant>();

    [Header("Events")]
    [Tooltip("Optional event published when this incident is recorded. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition recordedEvent = null;
    [Tooltip("Optional event published when this incident expires or is cleared. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition clearedEvent = null;
    [Tooltip("If enabled, risk events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = false;
    [Tooltip("If enabled, risk events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog = false;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public RiskIncidentCategory Category => category;
    public RiskIncidentSeverity Severity => severity;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : System.Array.Empty<string>();
    public ReputationFactionDefinition AuthorityFaction => authorityFaction;
    public string AuthorityId => authorityFaction != null ? authorityFaction.Id : string.IsNullOrWhiteSpace(authorityIdOverride) ? "global" : authorityIdOverride;
    public string AuthorityName => authorityFaction != null ? authorityFaction.DisplayName : string.IsNullOrWhiteSpace(authorityNameOverride) ? AuthorityId : authorityNameOverride;
    public RegionInfoDefinition DefaultRegion => defaultRegion;
    public bool ContributesToGlobalRisk => contributesToGlobalRisk;
    public int HeatPoints => Mathf.Max(0, heatPoints);
    public int SuspicionPoints => Mathf.Max(0, suspicionPoints);
    public int EvidencePoints => Mathf.Max(0, evidencePoints);
    public int ActiveDurationHours => permanentUntilCleared ? 0 : Mathf.Max(0, activeDurationHours);
    public bool PermanentUntilCleared => permanentUntilCleared;
    public bool RecordLawViolation => recordLawViolation;
    public LawViolationDefinition LawViolation => lawViolation;
    public bool ApplyReputationChanges => applyReputationChanges;
    public IReadOnlyList<ReputationChange> ReputationChanges => reputationChanges != null ? (IReadOnlyList<ReputationChange>)reputationChanges : System.Array.Empty<ReputationChange>();
    public IReadOnlyList<MilestoneDefinition> MilestonesToComplete => milestonesToComplete != null ? (IReadOnlyList<MilestoneDefinition>)milestonesToComplete : System.Array.Empty<MilestoneDefinition>();
    public IReadOnlyList<TitleGrant> TitleGrants => titleGrants != null ? (IReadOnlyList<TitleGrant>)titleGrants : System.Array.Empty<TitleGrant>();
    public GameEventDefinition RecordedEvent => recordedEvent;
    public GameEventDefinition ClearedEvent => clearedEvent;
    public bool ShowEventsInFeed => showEventsInFeed;
    public bool WriteEventsToDebugLog => writeEventsToDebugLog;

    public PlayerRiskIncidentRecord Apply(
        PlayerController player,
        string sourceId = null,
        string reporterId = null,
        RegionInfoDefinition regionOverride = null,
        string authorityIdOverride = null,
        string authorityNameOverride = null,
        bool applyConsequences = true,
        UnityEngine.Object context = null
    ) {
        if(player == null) {
            return null;
        }

        var log = player.GetComponent<PlayerRiskLog>() ?? player.gameObject.AddComponent<PlayerRiskLog>();
        return log.RecordIncident(this, sourceId, reporterId, regionOverride, authorityIdOverride, authorityNameOverride, applyConsequences, context != null ? context : this);
    }

    public void ApplyConsequences(PlayerController player, string sourceId, string reporterId, UnityEngine.Object context) {
        if(player == null) {
            return;
        }

        if(recordLawViolation && lawViolation != null) {
            var lawLog = player.GetComponent<PlayerLawLog>() ?? player.gameObject.AddComponent<PlayerLawLog>();
            lawLog.RecordViolation(lawViolation, sourceId, reporterId, applyLawConsequences, context != null ? context : this);
        }

        if(applyReputationChanges) {
            player.GetComponent<PlayerReputation>()?.ApplyChanges(reputationChanges);
        }

        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);
        player.GetComponent<PlayerTitles>()?.ApplyGrants(titleGrants, this);
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, System.StringComparison.OrdinalIgnoreCase));
    }
}
