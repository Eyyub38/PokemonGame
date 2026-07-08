using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Pokemon Assignments/Pokemon Assignment Board")]
public class PokemonAssignmentBoardDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this assignment board. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing explanation of this assignment board.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad board category used by future UI filters.")]
    [SerializeField] PokemonAssignmentCategory category = PokemonAssignmentCategory.General;
    [Tooltip("Free-form tags such as camp, farm, ranch, lab, ranger, delivery or town.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Optional icon used by future board UI.")]
    [SerializeField] Sprite icon = null;

    [Header("Offers")]
    [Tooltip("Assignments listed by this board. Each entry can override source, label, visibility and zone context.")]
    [SerializeField] List<PokemonAssignmentBoardEntry> entries = new List<PokemonAssignmentBoardEntry>();

    [Header("Defaults")]
    [Tooltip("If enabled, board entries without source overrides use board id as their source id.")]
    [SerializeField] bool useBoardIdAsDefaultSource = true;
    [Tooltip("If enabled, entries that are locked can still appear in UI snapshots with a failure reason.")]
    [SerializeField] bool showLockedEntriesByDefault = true;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public PokemonAssignmentCategory Category => category;
    public IReadOnlyList<string> Tags => tags != null ? tags : Array.Empty<string>();
    public Sprite Icon => icon;
    public IReadOnlyList<PokemonAssignmentBoardEntry> Entries => entries != null ? entries : Array.Empty<PokemonAssignmentBoardEntry>();
    public bool UseBoardIdAsDefaultSource => useBoardIdAsDefaultSource;
    public bool ShowLockedEntriesByDefault => showLockedEntriesByDefault;

    public IEnumerable<PokemonAssignmentBoardEntry> GetOrderedEntries() {
        return Entries
            .Where(entry => entry != null && entry.Assignment != null)
            .OrderByDescending(entry => entry.Priority)
            .ThenBy(entry => entry.DisplayName);
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }
}

[Serializable]
public class PokemonAssignmentBoardEntry {
    [Tooltip("Assignment offered by this board row.")]
    [SerializeField] PokemonAssignmentDefinition assignment = null;
    [Tooltip("Optional stable offer id used by UI actions. Empty uses assignment id.")]
    [SerializeField] string offerId = string.Empty;
    [Tooltip("Optional source id override saved into assignment logs. Empty uses board/default source.")]
    [SerializeField] string sourceIdOverride = string.Empty;
    [Tooltip("Optional display name override for this board row.")]
    [SerializeField] string displayNameOverride = string.Empty;
    [Tooltip("Optional description override for this board row.")]
    [TextArea]
    [SerializeField] string descriptionOverride = string.Empty;
    [Tooltip("Higher priority offers are shown first.")]
    [SerializeField] int priority;
    [Tooltip("If enabled, this offer is omitted from snapshots when locked and Include Locked Offers is false.")]
    [SerializeField] bool hideWhenLocked;
    [Tooltip("Optional zone override passed to assignment location checks.")]
    [SerializeField] ActivityZoneDefinition zoneOverride = null;
    [Tooltip("Additional requirements checked before this board entry can be shown/started.")]
    [SerializeField] List<ActivityRequirement> extraRequirements = new List<ActivityRequirement>();

    public virtual PokemonAssignmentDefinition Assignment => assignment;
    public virtual string OfferId => string.IsNullOrWhiteSpace(offerId) ? assignment != null ? assignment.Id : string.Empty : offerId;
    public virtual string SourceIdOverride => sourceIdOverride;
    public virtual string DisplayName => !string.IsNullOrWhiteSpace(displayNameOverride) ? displayNameOverride : assignment != null ? assignment.DisplayName : string.Empty;
    public virtual string Description => !string.IsNullOrWhiteSpace(descriptionOverride) ? descriptionOverride : assignment != null ? assignment.Description : string.Empty;
    public virtual int Priority => priority;
    public virtual bool HideWhenLocked => hideWhenLocked;
    public virtual ActivityZoneDefinition ZoneOverride => zoneOverride;
    public virtual IReadOnlyList<ActivityRequirement> ExtraRequirements => extraRequirements != null ? extraRequirements : Array.Empty<ActivityRequirement>();

    public virtual string ResolveSourceId(PokemonAssignmentBoardDefinition board, string fallbackSourceId) {
        if(!string.IsNullOrWhiteSpace(sourceIdOverride)) {
            return sourceIdOverride;
        }

        if(board != null && board.UseBoardIdAsDefaultSource) {
            return $"pokemon-assignment-board:{board.Id}";
        }

        return !string.IsNullOrWhiteSpace(fallbackSourceId) ? fallbackSourceId : assignment != null ? $"pokemon-assignment:{assignment.Id}" : "pokemon-assignment";
    }

    public virtual ActivityZoneDefinition ResolveZone(ActivityZoneDefinition fallbackZone) {
        return zoneOverride != null ? zoneOverride : fallbackZone;
    }

    public virtual bool RequirementsMet(PlayerController player, out string failureMessage) {
        foreach(var requirement in ExtraRequirements) {
            if(requirement != null && !requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }
}
