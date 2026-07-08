using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RegionTravelPartyPolicyMode {
    RouteDefault,
    KeepCurrentParty,
    OnePokemonOnly,
    StorePartyExceptSelected,
    LocalPokemonOnly,
    Custom
}

public enum RegionTravelChallengePolicyMode {
    RouteDefault,
    DoNotStartChallenge,
    StartRouteChallenge,
    RequireRouteChallenge,
    StartOverrideChallenge,
    RequireOverrideChallenge
}

[CreateAssetMenu(menuName = "World Regions/Region Travel Policy")]
public class RegionTravelPolicyDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this travel policy. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in future region travel UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note explaining what kind of travel decisions this policy represents.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Free-form tags such as new-region, postgame, ferry, league or optional-challenge.")]
    [SerializeField] List<string> tags = new List<string>();

    [Header("Options")]
    [Tooltip("Selectable travel options exposed by this policy. The first option is used when no default is marked.")]
    [SerializeField] List<RegionTravelPolicyOption> options = new List<RegionTravelPolicyOption>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public IReadOnlyList<RegionTravelPolicyOption> Options => options != null ? (IReadOnlyList<RegionTravelPolicyOption>)options : Array.Empty<RegionTravelPolicyOption>();

    public RegionTravelPolicyOption GetDefaultOption() {
        return Options.FirstOrDefault(option => option != null && option.IsDefault)
            ?? Options.FirstOrDefault(option => option != null);
    }

    public RegionTravelPolicyOption GetOption(string optionId) {
        if(string.IsNullOrWhiteSpace(optionId)) {
            return GetDefaultOption();
        }

        return Options.FirstOrDefault(option => option != null && option.Matches(optionId))
            ?? GetDefaultOption();
    }

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
    }
}

[Serializable]
public class RegionTravelPolicyOption {
    [Header("Identity")]
    [Tooltip("Stable option id saved into travel history. Empty uses the display name.")]
    [SerializeField] string optionId = string.Empty;
    [Tooltip("Name shown in future region travel UI. Empty uses the option id.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note/player-facing explanation for this travel option.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("If enabled, this option is selected when no explicit option id is supplied.")]
    [SerializeField] bool isDefault;

    [Header("Party")]
    [Tooltip("How this option treats the active party. Route Default uses the route challenge profile if one starts.")]
    [SerializeField] RegionTravelPartyPolicyMode partyMode = RegionTravelPartyPolicyMode.RouteDefault;
    [Tooltip("If enabled, this option requires a selected Pokemon before travel can start.")]
    [SerializeField] bool requireSelectedPokemon;
    [Tooltip("If enabled, the selected Pokemon must have HP above 0.")]
    [SerializeField] bool requireHealthySelectedPokemon = true;
    [Tooltip("Maximum Pokemon allowed in the active party before this option can be used. 0 disables this check.")]
    [Min(0)]
    [SerializeField] int maxActivePartyPokemon;

    [Header("Challenge")]
    [Tooltip("How this option starts or suppresses a route challenge.")]
    [SerializeField] RegionTravelChallengePolicyMode challengeMode = RegionTravelChallengePolicyMode.RouteDefault;
    [Tooltip("Challenge profile used by Start Override Challenge and Require Override Challenge modes.")]
    [SerializeField] RegionChallengeProfileDefinition challengeOverride;
    [Tooltip("If enabled, the current active region challenge is completed before travel starts.")]
    [SerializeField] bool completeActiveChallengeBeforeTravel;
    [Tooltip("If enabled, the current active region challenge is cleared before travel starts without rewards.")]
    [SerializeField] bool clearActiveChallengeBeforeTravel;
    [Tooltip("If enabled, travel is blocked while any region challenge is active.")]
    [SerializeField] bool blockIfAnyChallengeActive;

    [Header("Access")]
    [Tooltip("How custom requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Extra requirements checked only for this travel option.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Message shown when this option blocks travel and no more specific message is available.")]
    [TextArea]
    [SerializeField] string blockedMessage = "This travel option is not available right now.";

    public string Id => string.IsNullOrWhiteSpace(optionId) ? DisplayName : optionId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? (string.IsNullOrWhiteSpace(optionId) ? "Travel Option" : optionId) : displayName;
    public string Description => description;
    public bool IsDefault => isDefault;
    public RegionTravelPartyPolicyMode PartyMode => partyMode;
    public bool RequireSelectedPokemon => requireSelectedPokemon || partyMode == RegionTravelPartyPolicyMode.OnePokemonOnly || partyMode == RegionTravelPartyPolicyMode.StorePartyExceptSelected;
    public bool RequireHealthySelectedPokemon => requireHealthySelectedPokemon;
    public int MaxActivePartyPokemon => Mathf.Max(0, maxActivePartyPokemon);
    public RegionTravelChallengePolicyMode ChallengeMode => challengeMode;
    public RegionChallengeProfileDefinition ChallengeOverride => challengeOverride;
    public bool CompleteActiveChallengeBeforeTravel => completeActiveChallengeBeforeTravel;
    public bool ClearActiveChallengeBeforeTravel => clearActiveChallengeBeforeTravel;
    public bool BlockIfAnyChallengeActive => blockIfAnyChallengeActive;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();

    public bool Matches(string value) {
        return !string.IsNullOrWhiteSpace(value)
            && (string.Equals(Id, value, StringComparison.OrdinalIgnoreCase)
                || string.Equals(DisplayName, value, StringComparison.OrdinalIgnoreCase));
    }

    public RegionChallengeProfileDefinition ResolveChallengeProfile(RegionTravelRouteDefinition route) {
        return challengeMode switch {
            RegionTravelChallengePolicyMode.DoNotStartChallenge => null,
            RegionTravelChallengePolicyMode.StartOverrideChallenge => challengeOverride,
            RegionTravelChallengePolicyMode.RequireOverrideChallenge => challengeOverride,
            _ => route != null ? route.ChallengeProfile : null
        };
    }

    public bool RequiresChallengeProfile() {
        return challengeMode == RegionTravelChallengePolicyMode.RequireRouteChallenge
            || challengeMode == RegionTravelChallengePolicyMode.RequireOverrideChallenge;
    }

    public RegionPartyTransferMode ResolvePartyTransferMode(RegionChallengeProfileDefinition challenge) {
        return partyMode switch {
            RegionTravelPartyPolicyMode.KeepCurrentParty => RegionPartyTransferMode.KeepCurrentParty,
            RegionTravelPartyPolicyMode.OnePokemonOnly => RegionPartyTransferMode.OnePokemonOnly,
            RegionTravelPartyPolicyMode.StorePartyExceptSelected => RegionPartyTransferMode.StorePartyExceptSelected,
            RegionTravelPartyPolicyMode.LocalPokemonOnly => RegionPartyTransferMode.LocalPokemonOnly,
            RegionTravelPartyPolicyMode.Custom => RegionPartyTransferMode.Custom,
            _ => challenge != null ? challenge.PartyTransferMode : RegionPartyTransferMode.KeepCurrentParty
        };
    }

    public List<string> BuildAllowedPokemonIds(PokemonParty party, Pokemon selectedPokemon, RegionChallengeProfileDefinition challenge) {
        if(partyMode == RegionTravelPartyPolicyMode.OnePokemonOnly || partyMode == RegionTravelPartyPolicyMode.StorePartyExceptSelected) {
            return selectedPokemon != null ? new List<string> { selectedPokemon.InstanceId } : new List<string>();
        }

        return challenge != null ? challenge.BuildAllowedPokemonIds(party, selectedPokemon) : new List<string>();
    }

    public bool CanUse(PlayerController player, RegionTravelRouteDefinition route, PlayerWorldRegionLog log, Pokemon selectedPokemon, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to use this regional travel option.";
            return false;
        }

        var party = player.GetComponent<PokemonParty>();
        if(MaxActivePartyPokemon > 0 && party?.Pokemons != null && party.Pokemons.Count(pokemon => pokemon != null) > MaxActivePartyPokemon) {
            failureMessage = $"{DisplayName} allows at most {MaxActivePartyPokemon} active Pokemon.";
            return false;
        }

        if(RequireSelectedPokemon && !ValidateSelectedPokemon(party, selectedPokemon, out failureMessage)) {
            return false;
        }

        if(blockIfAnyChallengeActive && log != null && log.HasActiveChallenge) {
            failureMessage = string.IsNullOrWhiteSpace(blockedMessage) ? "A region challenge is already active." : blockedMessage;
            return false;
        }

        if(completeActiveChallengeBeforeTravel && clearActiveChallengeBeforeTravel) {
            failureMessage = "Travel option cannot both complete and clear the active challenge.";
            return false;
        }

        var challenge = ResolveChallengeProfile(route);
        if(RequiresChallengeProfile() && challenge == null) {
            failureMessage = string.IsNullOrWhiteSpace(blockedMessage) ? $"{DisplayName} requires a region challenge profile." : blockedMessage;
            return false;
        }

        if(challenge != null && !challenge.CanStart(player, out failureMessage)) {
            return false;
        }

        return RequirementsMet(player, out failureMessage);
    }

    public void ApplyBeforeTravel(PlayerController player, PlayerWorldRegionLog log, RegionTravelResult result, UnityEngine.Object context) {
        if(player == null || log == null) {
            return;
        }

        if(completeActiveChallengeBeforeTravel && log.HasActiveChallenge) {
            log.CompleteActiveChallenge(player, applyRewards: true, context);
            result?.messages.Add("Previous region challenge was completed before travel.");
        } else if(clearActiveChallengeBeforeTravel && log.HasActiveChallenge) {
            log.ClearActiveChallenge();
            result?.messages.Add("Previous region challenge was cleared before travel.");
        }
    }

    bool ValidateSelectedPokemon(PokemonParty party, Pokemon selectedPokemon, out string failureMessage) {
        if(selectedPokemon == null) {
            failureMessage = string.IsNullOrWhiteSpace(blockedMessage) ? $"{DisplayName} requires a selected Pokemon." : blockedMessage;
            return false;
        }

        if(party == null || party.Pokemons == null || !party.Pokemons.Contains(selectedPokemon)) {
            failureMessage = "Selected Pokemon must be in the active party.";
            return false;
        }

        if(requireHealthySelectedPokemon && selectedPokemon.HP <= 0) {
            failureMessage = $"{selectedPokemon.NickName} cannot travel with this option right now.";
            return false;
        }

        failureMessage = null;
        return true;
    }

    bool RequirementsMet(PlayerController player, out string failureMessage) {
        var activeRequirements = requirements?.Where(requirement => requirement != null).ToList() ?? new List<ActivityRequirement>();
        if(activeRequirements.Count == 0) {
            failureMessage = null;
            return true;
        }

        if(requirementMatchMode == ConsequenceRequirementMatchMode.Any) {
            foreach(var requirement in activeRequirements) {
                if(requirement.IsMet(player)) {
                    failureMessage = null;
                    return true;
                }
            }

            failureMessage = activeRequirements.FirstOrDefault()?.FailureMessage ?? blockedMessage;
            return false;
        }

        foreach(var requirement in activeRequirements) {
            if(!requirement.IsMet(player)) {
                failureMessage = requirement.FailureMessage;
                return false;
            }
        }

        failureMessage = null;
        return true;
    }
}
