using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RadialEncounterActionKind {
    Capture,
    Calm,
    Feed,
    Observe,
    Distract,
    Treat,
    Custom,
    Cancel
}

public class RadialEncounterMenuProvider : MonoBehaviour, IRadialMenuProvider {
    [Header("Encounter Context")]
    [Tooltip("UI manager used as the primary source of encounter choice rows. Empty searches at runtime.")]
    [SerializeField] EncounterResolutionUIManager uiManager;
    [Tooltip("Choice source used when no UI manager is assigned. Empty can be supplied through context payload.")]
    [SerializeField] EncounterResolutionChoiceSource choiceSource;
    [Tooltip("If enabled, the UI manager/source snapshot is refreshed before radial options are built.")]
    [SerializeField] bool refreshBeforeBuild = true;
    [Tooltip("If enabled, blocked choices are included as disabled radial options.")]
    [SerializeField] bool includeBlockedChoices = true;
    [Tooltip("Maximum choice options shown by this provider. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxOptions = 8;

    [Header("Icons")]
    [Tooltip("Icon shown for capture choices.")]
    [SerializeField] Sprite captureIcon;
    [Tooltip("Icon shown for calm choices.")]
    [SerializeField] Sprite calmIcon;
    [Tooltip("Icon shown for feed choices.")]
    [SerializeField] Sprite feedIcon;
    [Tooltip("Icon shown for observe choices.")]
    [SerializeField] Sprite observeIcon;
    [Tooltip("Icon shown for distract choices.")]
    [SerializeField] Sprite distractIcon;
    [Tooltip("Icon shown for treat choices.")]
    [SerializeField] Sprite treatIcon;
    [Tooltip("Icon shown for custom choices.")]
    [SerializeField] Sprite customIcon;
    [Tooltip("Icon shown for cancel/back.")]
    [SerializeField] Sprite cancelIcon;

    [Header("Selection")]
    [Tooltip("If enabled, confirming a radial choice runs it through the EncounterResolutionUIManager or ChoiceSource.")]
    [SerializeField] bool runChoiceOnSelect;
    [Tooltip("If enabled, a Back/Cancel option is added after encounter choices.")]
    [SerializeField] bool includeCancelAction = true;

    [Header("Debug")]
    [Tooltip("If enabled, selected radial encounter choices are written to GameDebug.")]
    [SerializeField] bool logSelectedActions = true;

    EncounterResolutionUIScreenSnapshot lastUiSnapshot;
    EncounterResolutionChoiceSnapshot lastSourceSnapshot;

    public EncounterResolutionUIManager UIManager => uiManager;
    public EncounterResolutionChoiceSource ChoiceSource => choiceSource;
    public bool RunChoiceOnSelect => runChoiceOnSelect;
    public bool IncludeBlockedChoices => includeBlockedChoices;
    public int MaxOptions => maxOptions;
    public event Action<RadialEncounterActionKind, EncounterResolutionChoiceRow, EncounterResolutionResult, RadialMenuOption> OnEncounterActionSelected;

    public IReadOnlyList<RadialMenuOption> BuildRadialOptions(RadialMenuContext context) {
        var rows = BuildRows(context).ToList();
        if(maxOptions > 0) {
            rows = rows.Take(maxOptions).ToList();
        }

        var options = rows
            .Where(row => row != null)
            .Select(BuildOption)
            .ToList();

        if(includeCancelAction) {
            options.Add(new RadialMenuOption {
                id = RadialEncounterActionKind.Cancel.ToString(),
                label = "Back",
                description = "Close encounter choices.",
                icon = cancelIcon,
                priority = 1000
            });
        }

        return options;
    }

    public void OnRadialOptionSelected(RadialMenuOption option, RadialMenuContext context) {
        var kind = ResolveActionKind(option);
        var row = kind == RadialEncounterActionKind.Cancel ? null : FindRow(option?.id);
        EncounterResolutionResult result = null;

        if(runChoiceOnSelect && row != null && row.canRun) {
            result = RunChoice(row.choiceId, context);
        }

        OnEncounterActionSelected?.Invoke(kind, row, result, option);

        if(logSelectedActions) {
            string label = row != null ? row.displayName : "Back";
            string resultText = result != null ? $" Result: {result.message}" : string.Empty;
            GameDebug.Step($"Encounter radial action selected: {kind} / {label}.{resultText}", GameDebugCategory.Encounter, this, "RadialEncounterMenuProvider");
        }
    }

    public void OnRadialMenuClosed(RadialMenuContext context) {
    }

    IEnumerable<EncounterResolutionChoiceRow> BuildRows(RadialMenuContext context) {
        var manager = ResolveUIManager(context);
        if(manager != null) {
            lastUiSnapshot = refreshBeforeBuild ? manager.Refresh() : manager.CurrentSnapshot;
            return FilterRows(lastUiSnapshot?.rows);
        }

        var source = ResolveChoiceSource(context);
        if(source != null) {
            lastSourceSnapshot = source.GetSnapshot(PlayerController.i, includeBlockedChoices);
            return FilterRows(lastSourceSnapshot?.rows);
        }

        lastUiSnapshot = null;
        lastSourceSnapshot = null;
        return Array.Empty<EncounterResolutionChoiceRow>();
    }

    IEnumerable<EncounterResolutionChoiceRow> FilterRows(IEnumerable<EncounterResolutionChoiceRow> rows) {
        rows ??= Array.Empty<EncounterResolutionChoiceRow>();
        return rows
            .Where(row => row != null && (includeBlockedChoices || row.canRun))
            .OrderBy(row => row.priority)
            .ThenBy(row => row.displayName);
    }

    RadialMenuOption BuildOption(EncounterResolutionChoiceRow row) {
        string costText = row.itemCosts != null && row.itemCosts.Count > 0
            ? $" Cost: {string.Join(", ", row.itemCosts)}."
            : string.Empty;
        string chanceText = row.chancePercent > 0f
            ? $" Chance: {Mathf.RoundToInt(row.chancePercent)}%."
            : string.Empty;

        return new RadialMenuOption {
            id = row.choiceId,
            label = row.displayName,
            description = $"{row.description}{chanceText}{costText}",
            icon = ResolveIcon(row.kind),
            disabled = !row.canRun,
            disabledReason = row.canRun ? string.Empty : row.blockedReason,
            priority = row.priority
        };
    }

    EncounterResolutionResult RunChoice(string choiceId, RadialMenuContext context) {
        var manager = ResolveUIManager(context);
        if(manager != null) {
            return manager.RunChoice(choiceId);
        }

        var source = ResolveChoiceSource(context);
        return source != null
            ? source.RunChoice(choiceId, PlayerController.i)
            : new EncounterResolutionResult {
                blocked = true,
                message = "No encounter choice source was available."
            };
    }

    EncounterResolutionChoiceRow FindRow(string choiceId) {
        if(string.IsNullOrWhiteSpace(choiceId)) {
            return null;
        }

        var rows = lastUiSnapshot?.rows ?? lastSourceSnapshot?.rows;
        return rows?.FirstOrDefault(row => row != null && string.Equals(row.choiceId, choiceId, StringComparison.OrdinalIgnoreCase));
    }

    RadialEncounterActionKind ResolveActionKind(RadialMenuOption option) {
        if(option == null || string.IsNullOrWhiteSpace(option.id)) {
            return RadialEncounterActionKind.Custom;
        }

        if(Enum.TryParse(option.id, true, out RadialEncounterActionKind directKind)) {
            return directKind;
        }

        var row = FindRow(option.id);
        return row != null ? MapKind(row.kind) : RadialEncounterActionKind.Custom;
    }

    RadialEncounterActionKind MapKind(EncounterResolutionKind kind) {
        return kind switch {
            EncounterResolutionKind.Capture => RadialEncounterActionKind.Capture,
            EncounterResolutionKind.Calm => RadialEncounterActionKind.Calm,
            EncounterResolutionKind.Feed => RadialEncounterActionKind.Feed,
            EncounterResolutionKind.Observe => RadialEncounterActionKind.Observe,
            EncounterResolutionKind.Distract => RadialEncounterActionKind.Distract,
            EncounterResolutionKind.Treat => RadialEncounterActionKind.Treat,
            _ => RadialEncounterActionKind.Custom
        };
    }

    Sprite ResolveIcon(EncounterResolutionKind kind) {
        return kind switch {
            EncounterResolutionKind.Capture => captureIcon,
            EncounterResolutionKind.Calm => calmIcon,
            EncounterResolutionKind.Feed => feedIcon,
            EncounterResolutionKind.Observe => observeIcon,
            EncounterResolutionKind.Distract => distractIcon,
            EncounterResolutionKind.Treat => treatIcon,
            _ => customIcon
        };
    }

    EncounterResolutionUIManager ResolveUIManager(RadialMenuContext context) {
        if(uiManager != null) {
            return uiManager;
        }

        if(context != null) {
            if(context.payload is EncounterResolutionUIManager payloadManager) {
                uiManager = payloadManager;
                return uiManager;
            }

            if(context.owner is EncounterResolutionUIManager ownerManager) {
                uiManager = ownerManager;
                return uiManager;
            }
        }

        uiManager = FindAnyObjectByType<EncounterResolutionUIManager>();
        return uiManager;
    }

    EncounterResolutionChoiceSource ResolveChoiceSource(RadialMenuContext context) {
        if(choiceSource != null) {
            return choiceSource;
        }

        if(context != null) {
            if(context.payload is EncounterResolutionChoiceSource payloadSource) {
                choiceSource = payloadSource;
                return choiceSource;
            }

            if(context.owner is EncounterResolutionChoiceSource ownerSource) {
                choiceSource = ownerSource;
                return choiceSource;
            }
        }

        choiceSource = FindAnyObjectByType<EncounterResolutionChoiceSource>();
        return choiceSource;
    }
}
