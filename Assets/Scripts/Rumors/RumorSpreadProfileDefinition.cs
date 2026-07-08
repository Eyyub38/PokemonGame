using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RumorImportanceLevel {
    Minor,
    Local,
    Regional,
    Major,
    Legendary
}

public enum RumorLifecycleStage {
    Fresh,
    Known,
    Stale,
    Forgotten,
    Archived
}

[CreateAssetMenu(menuName = "Rumors/Rumor Spread Profile")]
public class RumorSpreadProfileDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this spread profile. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note explaining the rumor spread behavior.")]
    [TextArea]
    [SerializeField] string description;

    [Header("Importance")]
    [Tooltip("Default importance level used by rumors using this profile.")]
    [SerializeField] RumorImportanceLevel importance = RumorImportanceLevel.Local;
    [Tooltip("Default region where rumors using this profile begin. A rumor can override this.")]
    [SerializeField] RegionInfoDefinition defaultOriginRegion;
    [Tooltip("If enabled, the rumor can be seeded from any source that lists it.")]
    [SerializeField] bool canSeedFromAnyListedSource = true;

    [Header("Lifecycle Hours")]
    [Tooltip("Hours after seeding while this rumor is considered fresh.")]
    [Min(0)]
    [SerializeField] int freshHours = 24;
    [Tooltip("Hours after seeding while this rumor is broadly known.")]
    [Min(0)]
    [SerializeField] int knownHours = 168;
    [Tooltip("Hours after seeding while this rumor is stale but still mentionable.")]
    [Min(0)]
    [SerializeField] int staleHours = 336;
    [Tooltip("Hours after seeding when regular sources forget this rumor. 0 means never forgotten by time.")]
    [Min(0)]
    [SerializeField] int forgottenAfterHours = 720;
    [Tooltip("Hours after seeding when archive-only memory starts. 0 means no archive transition.")]
    [Min(0)]
    [SerializeField] int archivedAfterHours;
    [Tooltip("If enabled, archived rumors may still be heard from archive-capable sources.")]
    [SerializeField] bool archiveSourcesCanShare = true;

    [Header("Source Matching")]
    [Tooltip("Source types that can hear this rumor immediately from its origin. Empty means any source type.")]
    [SerializeField] List<RumorSourceType> originSourceTypes = new List<RumorSourceType>();
    [Tooltip("Source tags that can hear this rumor immediately from its origin. Empty means no tag restriction.")]
    [SerializeField] List<string> originSourceTags = new List<string>();
    [Tooltip("Source types that can still mention archived rumors.")]
    [SerializeField] List<RumorSourceType> archiveSourceTypes = new List<RumorSourceType>();
    [Tooltip("Source tags that can still mention archived rumors.")]
    [SerializeField] List<string> archiveSourceTags = new List<string>();

    [Header("Spread Steps")]
    [Tooltip("Rules that describe where this rumor spreads after delays.")]
    [SerializeField] List<RumorSpreadStep> spreadSteps = new List<RumorSpreadStep>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public RumorImportanceLevel Importance => importance;
    public RegionInfoDefinition DefaultOriginRegion => defaultOriginRegion;
    public bool CanSeedFromAnyListedSource => canSeedFromAnyListedSource;
    public int FreshHours => Mathf.Max(0, freshHours);
    public int KnownHours => Mathf.Max(FreshHours, knownHours);
    public int StaleHours => Mathf.Max(KnownHours, staleHours);
    public int ForgottenAfterHours => Mathf.Max(0, forgottenAfterHours);
    public int ArchivedAfterHours => Mathf.Max(0, archivedAfterHours);
    public bool ArchiveSourcesCanShare => archiveSourcesCanShare;
    public IReadOnlyList<RumorSourceType> OriginSourceTypes => originSourceTypes != null ? (IReadOnlyList<RumorSourceType>)originSourceTypes : System.Array.Empty<RumorSourceType>();
    public IReadOnlyList<string> OriginSourceTags => originSourceTags != null ? (IReadOnlyList<string>)originSourceTags : System.Array.Empty<string>();
    public IReadOnlyList<RumorSourceType> ArchiveSourceTypes => archiveSourceTypes != null ? (IReadOnlyList<RumorSourceType>)archiveSourceTypes : System.Array.Empty<RumorSourceType>();
    public IReadOnlyList<string> ArchiveSourceTags => archiveSourceTags != null ? (IReadOnlyList<string>)archiveSourceTags : System.Array.Empty<string>();
    public IReadOnlyList<RumorSpreadStep> SpreadSteps => spreadSteps != null ? (IReadOnlyList<RumorSpreadStep>)spreadSteps : System.Array.Empty<RumorSpreadStep>();

    public RumorLifecycleStage GetStage(int elapsedHours) {
        elapsedHours = Mathf.Max(0, elapsedHours);
        if(ArchivedAfterHours > 0 && elapsedHours >= ArchivedAfterHours) {
            return RumorLifecycleStage.Archived;
        }

        if(ForgottenAfterHours > 0 && elapsedHours >= ForgottenAfterHours) {
            return RumorLifecycleStage.Forgotten;
        }

        if(elapsedHours <= FreshHours) {
            return RumorLifecycleStage.Fresh;
        }

        if(elapsedHours <= KnownHours) {
            return RumorLifecycleStage.Known;
        }

        return elapsedHours <= StaleHours ? RumorLifecycleStage.Stale : RumorLifecycleStage.Forgotten;
    }

    public bool CanSeedFrom(RumorSource source) {
        if(source == null) {
            return false;
        }

        if(canSeedFromAnyListedSource) {
            return true;
        }

        return MatchesSource(source, OriginSourceTypes, OriginSourceTags, DefaultOriginRegion);
    }

    public bool CanReachSource(PlayerRumorLifecycleState state, RumorSource source, int elapsedHours) {
        if(state == null || source == null) {
            return false;
        }

        var stage = GetStage(elapsedHours);
        if(stage == RumorLifecycleStage.Forgotten) {
            return false;
        }

        if(stage == RumorLifecycleStage.Archived) {
            return archiveSourcesCanShare && MatchesSource(source, ArchiveSourceTypes, ArchiveSourceTags, null);
        }

        if(source.SourceId == state.originSourceId) {
            return true;
        }

        var originRegion = state.ResolveOriginRegion();
        if(originRegion != null && source.Region == originRegion && MatchesSource(source, OriginSourceTypes, OriginSourceTags, originRegion)) {
            return true;
        }

        foreach(var step in SpreadSteps) {
            if(step != null && step.CanReach(state, source, elapsedHours)) {
                return true;
            }
        }

        return false;
    }

    static bool MatchesSource(RumorSource source, IReadOnlyList<RumorSourceType> sourceTypes, IReadOnlyList<string> sourceTags, RegionInfoDefinition region) {
        if(source == null) {
            return false;
        }

        if(region != null && source.Region != region) {
            return false;
        }

        bool typeMatches = sourceTypes == null || sourceTypes.Count == 0 || sourceTypes.Contains(source.SourceType);
        bool tagMatches = sourceTags == null || sourceTags.Count == 0 || sourceTags.Any(source.HasSourceTag);
        return typeMatches && tagMatches;
    }
}

[System.Serializable]
public class RumorSpreadStep {
    [Tooltip("In-game hours after seeding before this spread step becomes active.")]
    [Min(0)]
    public int delayHours;
    [Tooltip("Regions that can receive the rumor at this step. Empty means any region.")]
    public List<RegionInfoDefinition> regions = new List<RegionInfoDefinition>();
    [Tooltip("Source types that can receive the rumor at this step. Empty means any source type.")]
    public List<RumorSourceType> sourceTypes = new List<RumorSourceType>();
    [Tooltip("Source tags that can receive the rumor at this step. Empty means no tag restriction.")]
    public List<string> sourceTags = new List<string>();
    [Tooltip("If enabled, this step can reach the origin region even if Regions does not list it.")]
    public bool includeOriginRegion = true;
    [Tooltip("If enabled, this step reaches every region regardless of Regions list.")]
    public bool reachesAnyRegion;

    public bool CanReach(PlayerRumorLifecycleState state, RumorSource source, int elapsedHours) {
        if(state == null || source == null || elapsedHours < Mathf.Max(0, delayHours)) {
            return false;
        }

        bool regionMatches = reachesAnyRegion;
        if(!regionMatches && source.Region != null) {
            regionMatches = regions != null && regions.Contains(source.Region);
        }

        var originRegion = state.ResolveOriginRegion();
        if(!regionMatches && includeOriginRegion && originRegion != null && source.Region == originRegion) {
            regionMatches = true;
        }

        if(!regionMatches && (regions == null || regions.Count == 0) && source.Region == null) {
            regionMatches = true;
        }

        bool typeMatches = sourceTypes == null || sourceTypes.Count == 0 || sourceTypes.Contains(source.SourceType);
        bool tagMatches = sourceTags == null || sourceTags.Count == 0 || sourceTags.Any(source.HasSourceTag);
        return regionMatches && typeMatches && tagMatches;
    }
}
