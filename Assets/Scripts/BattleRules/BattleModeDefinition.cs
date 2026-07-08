using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BattleModeKind {
    ClassicFourMove,
    CommandPalette,
    Hybrid,
    Custom
}

[CreateAssetMenu(menuName = "Battle Rules/Battle Mode Definition")]
public class BattleModeDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this battle mode. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in new game/options/battle rule UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer or player-facing explanation of this battle mode.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad battle mode kind used by validation, UI and future battle system routing.")]
    [SerializeField] BattleModeKind kind = BattleModeKind.ClassicFourMove;
    [Tooltip("Free-form tags such as classic, experimental, stamina, command-palette or story.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Implementation")]
    [Tooltip("If enabled, the current BattleSystem can run this mode directly. Leave disabled for future/custom modes until their UI/backend exists.")]
    [SerializeField] bool implementedInCurrentBattleSystem = true;
    [Tooltip("If enabled, unsupported modes can fall back to the classic battle loop instead of blocking battle start.")]
    [SerializeField] bool allowFallbackToClassic = true;
    [Tooltip("Optional backend key for future routing. Example: classic, command-palette, hybrid.")]
    [SerializeField] string battleSystemKey = "classic";

    [Header("Move/Resource Metadata")]
    [Tooltip("If enabled, this mode uses the old four-move selection limit.")]
    [SerializeField] bool usesFourMoveLimit = true;
    [Tooltip("If enabled, this mode expects a known-move command palette instead of a fixed move list.")]
    [SerializeField] bool usesKnownMovePalette;
    [Tooltip("If enabled, this mode expects action point resource checks.")]
    [SerializeField] bool usesActionPoints;
    [Tooltip("If enabled, this mode expects stamina resource checks.")]
    [SerializeField] bool usesStamina;
    [Tooltip("If enabled, this mode expects elemental modifiers or typed action overlays.")]
    [SerializeField] bool usesElementModifiers;
    [Tooltip("Suggested maximum visible action buttons for future UI. 0 means no suggestion.")]
    [Min(0)]
    [SerializeField] int suggestedVisibleActionCount = 4;

    [Header("Access")]
    [Tooltip("Optional title, badge, permit or license required before this battle mode can be selected.")]
    [SerializeField] TitleDefinition requiredTitle;
    [Tooltip("Optional milestone required before this battle mode can be selected.")]
    [SerializeField] MilestoneDefinition requiredMilestone;
    [Tooltip("Optional faction whose reputation gates this battle mode.")]
    [SerializeField] ReputationFactionDefinition requiredFaction;
    [Tooltip("Minimum required reputation with the selected faction.")]
    [SerializeField] int requiredReputation;
    [Tooltip("Message shown when access fails and no more specific message exists.")]
    [TextArea]
    [SerializeField] string lockedMessage = "This battle mode is not available yet.";

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public BattleModeKind Kind => kind;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public bool ImplementedInCurrentBattleSystem => implementedInCurrentBattleSystem;
    public bool AllowFallbackToClassic => allowFallbackToClassic;
    public string BattleSystemKey => string.IsNullOrWhiteSpace(battleSystemKey) ? "classic" : battleSystemKey;
    public bool UsesFourMoveLimit => usesFourMoveLimit;
    public bool UsesKnownMovePalette => usesKnownMovePalette;
    public bool UsesActionPoints => usesActionPoints;
    public bool UsesStamina => usesStamina;
    public bool UsesElementModifiers => usesElementModifiers;
    public int SuggestedVisibleActionCount => Mathf.Max(0, suggestedVisibleActionCount);

    public bool CanAccess(PlayerController player, out string failureMessage) {
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

        failureMessage = null;
        return true;
    }

    public bool CanRunWithCurrentBattleSystem(out string failureMessage, out string fallbackMessage) {
        fallbackMessage = null;
        if(implementedInCurrentBattleSystem) {
            failureMessage = null;
            return true;
        }

        if(allowFallbackToClassic) {
            failureMessage = null;
            fallbackMessage = $"{DisplayName} is not implemented yet; classic battle will be used as fallback.";
            return true;
        }

        failureMessage = $"{DisplayName} is not implemented in the current BattleSystem.";
        return false;
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }
}
