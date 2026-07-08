using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum InvestigationClueCategory {
    General,
    Witness,
    Evidence,
    Trace,
    Item,
    Pokemon,
    Location,
    Document,
    Rumor,
    ResearchData,
    LawRecord,
    Custom
}

[CreateAssetMenu(menuName = "Investigations/Clue Definition")]
public class InvestigationClueDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this clue. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in debug and future UI. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing explanation of this clue.")]
    [TextArea]
    [SerializeField] string description;
    [Tooltip("Broad clue category used by filters, requirements and future UI.")]
    [SerializeField] InvestigationClueCategory category = InvestigationClueCategory.General;
    [Tooltip("Free-form tags used by requirements, dialog and future UI filters.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Priority used by future UI sorting. Higher priority appears first.")]
    [SerializeField] int priority;

    [Header("Progress")]
    [Tooltip("Evidence points this clue adds to the case when discovered.")]
    [Min(0)]
    [SerializeField] int evidencePoints = 1;
    [Tooltip("If enabled, this clue is considered important for case completion.")]
    [SerializeField] bool keyClue;
    [Tooltip("Optional access profile required before this clue can be discovered.")]
    [SerializeField] AccessProfileDefinition requiredAccessProfile;
    [Tooltip("Optional requirements that must pass before this clue can be discovered.")]
    [SerializeField] List<ActivityRequirement> discoveryRequirements = new List<ActivityRequirement>();

    [Header("Events")]
    [Tooltip("Optional event published when this clue is discovered.")]
    [SerializeField] GameEventDefinition discoveredEvent;
    [Tooltip("If enabled, clue events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, clue events are written to the debug log.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public InvestigationClueCategory Category => category;
    public IReadOnlyList<string> Tags => tags;
    public int Priority => priority;
    public int EvidencePoints => Mathf.Max(0, evidencePoints);
    public bool KeyClue => keyClue;
    public IReadOnlyList<ActivityRequirement> DiscoveryRequirements => discoveryRequirements;

    public bool CanDiscover(PlayerController player, out string failureMessage) {
        if(requiredAccessProfile != null && !requiredAccessProfile.CanAccess(player, out failureMessage)) {
            return false;
        }

        foreach(var requirement in discoveryRequirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
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

    public void PublishDiscovered(PlayerController player, InvestigationCaseDefinition investigationCase, string sourceId, UnityEngine.Object context) {
        GameEventPublishing.PublishOptional(
            discoveredEvent,
            $"investigation.clue.{Id}",
            $"{DisplayName} discovered.",
            GameEventCategory.Investigation,
            keyClue ? GameEventImportance.Success : GameEventImportance.Info,
            context != null ? context : player,
            "InvestigationClueDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("clueId", Id),
            GameEventPublishing.Value("clueName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("caseId", investigationCase != null ? investigationCase.Id : null),
            GameEventPublishing.Value("caseName", investigationCase != null ? investigationCase.DisplayName : null),
            GameEventPublishing.Value("sourceId", sourceId),
            GameEventPublishing.Value("evidencePoints", EvidencePoints),
            GameEventPublishing.Value("keyClue", keyClue));
    }
}
