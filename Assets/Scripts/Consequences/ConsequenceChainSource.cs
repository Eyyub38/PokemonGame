using UnityEngine;

public enum ConsequenceChainSourceTriggerMode {
    ManualOnly,
    ApplyOnTrigger,
    ApplyWhenAccessFails
}

public class ConsequenceChainSource : MonoBehaviour, IPlayerTriggerable {
    [Header("Source")]
    [Tooltip("Stable source id used by repeat rules and history. Empty uses GameObject name.")]
    [SerializeField] string sourceId = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses GameObject name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Consequence chain applied by this source.")]
    [SerializeField] ConsequenceChainDefinition chain = null;

    [Header("Context")]
    [Tooltip("Optional reporter id passed to risk/law steps, such as NPC, shop, camera, sign or zone id.")]
    [SerializeField] string reporterId = string.Empty;
    [Tooltip("Optional region passed to regional steps.")]
    [SerializeField] RegionInfoDefinition region = null;
    [Tooltip("Optional activity zone passed to zone-aware steps.")]
    [SerializeField] ActivityZoneDefinition zone = null;
    [Tooltip("Optional rumor source passed to rumor hear/lifecycle steps.")]
    [SerializeField] RumorSource rumorSource = null;
    [Tooltip("Optional authority faction passed to risk clear/filter steps.")]
    [SerializeField] ReputationFactionDefinition authorityFaction = null;
    [Tooltip("Optional authority id override. Empty uses Authority Faction or none.")]
    [SerializeField] string authorityId = string.Empty;
    [Tooltip("Optional authority display name override.")]
    [SerializeField] string authorityName = string.Empty;

    [Header("Trigger")]
    [Tooltip("How this source behaves when the player triggers it.")]
    [SerializeField] ConsequenceChainSourceTriggerMode triggerMode = ConsequenceChainSourceTriggerMode.ApplyOnTrigger;
    [Tooltip("Optional access profile checked when Trigger Mode is Apply When Access Fails.")]
    [SerializeField] AccessProfileDefinition accessProfile = null;
    [Tooltip("If enabled, repeated player triggers can call this source more than once.")]
    [SerializeField] bool triggerRepeatedly = true;

    [Header("Debug")]
    [Tooltip("If enabled, source attempts are written to GameEventBus/GameDebugLogger.")]
    [SerializeField] bool logAttempts;

    public string SourceId => string.IsNullOrWhiteSpace(sourceId) ? name : sourceId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public ConsequenceChainDefinition Chain => chain;
    public AccessProfileDefinition AccessProfile => accessProfile;
    public ConsequenceChainSourceTriggerMode TriggerMode => triggerMode;
    public bool TriggerRepeatedly => triggerRepeatedly;

    public void OnPlayerTriggered(PlayerController player) {
        if(triggerMode == ConsequenceChainSourceTriggerMode.ManualOnly) {
            PublishSourceEvent(player, "manual", $"{DisplayName} is manual-only.", GameEventImportance.Trace);
            return;
        }

        if(triggerMode == ConsequenceChainSourceTriggerMode.ApplyWhenAccessFails) {
            string failureMessage = null;
            bool accessPassed = accessProfile != null && accessProfile.CanAccess(player, out failureMessage);
            accessProfile?.PublishChecked(player, accessPassed, SourceId, failureMessage, this);
            if(accessPassed) {
                PublishSourceEvent(player, "access-passed", $"{DisplayName} access passed; chain was not applied.", GameEventImportance.Trace);
                return;
            }
        }

        Apply(player);
    }

    public ConsequenceChainRunResult Apply(PlayerController player) {
        if(player == null || chain == null) {
            PublishSourceEvent(player, "blocked", player == null ? "A player is required to apply a consequence chain." : "No consequence chain is assigned.", GameEventImportance.Warning);
            return null;
        }

        var result = chain.Apply(player, BuildContext(), this);
        PublishSourceEvent(player, result != null && !result.blocked ? "applied" : "blocked", BuildResultMessage(result), result != null && !result.blocked ? GameEventImportance.Info : GameEventImportance.Warning);
        return result;
    }

    ConsequenceChainContext BuildContext() {
        return new ConsequenceChainContext {
            SourceId = SourceId,
            SourceName = DisplayName,
            ReporterId = reporterId,
            Region = region,
            Zone = zone,
            RumorSource = rumorSource,
            AuthorityId = ResolveAuthorityId(),
            AuthorityName = ResolveAuthorityName(),
            ContextObject = this
        };
    }

    string ResolveAuthorityId() {
        if(!string.IsNullOrWhiteSpace(authorityId)) {
            return authorityId;
        }

        return authorityFaction != null ? authorityFaction.Id : string.Empty;
    }

    string ResolveAuthorityName() {
        if(!string.IsNullOrWhiteSpace(authorityName)) {
            return authorityName;
        }

        return authorityFaction != null ? authorityFaction.DisplayName : ResolveAuthorityId();
    }

    string BuildResultMessage(ConsequenceChainRunResult result) {
        if(result == null) {
            return $"{DisplayName} did not run a consequence chain.";
        }

        if(result.blocked) {
            return string.IsNullOrWhiteSpace(result.failureMessage) ? $"{chain.DisplayName} blocked." : result.failureMessage;
        }

        return $"{chain.DisplayName} applied {result.appliedSteps} step(s).";
    }

    void PublishSourceEvent(PlayerController player, string phase, string message, GameEventImportance importance) {
        if(!logAttempts && importance < GameEventImportance.Warning) {
            return;
        }

        GameEventPublishing.PublishOptional(
            null,
            $"consequence-chain-source.{phase}.{SourceId}",
            message,
            GameEventCategory.Consequence,
            importance,
            this,
            "ConsequenceChainSource",
            GameEventScope.Scene,
            showInFeed: false,
            writeToDebugLog: logAttempts,
            GameEventPublishing.Value("sourceId", SourceId),
            GameEventPublishing.Value("sourceName", DisplayName),
            GameEventPublishing.Value("chainId", chain != null ? chain.Id : string.Empty),
            GameEventPublishing.Value("chainName", chain != null ? chain.DisplayName : string.Empty),
            GameEventPublishing.Value("phase", phase),
            GameEventPublishing.Value("player", player != null ? player.name : string.Empty));
    }
}
