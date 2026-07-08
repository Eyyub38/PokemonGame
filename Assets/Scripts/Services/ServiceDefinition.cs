using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PlayerServiceCategory {
    General,
    Healing,
    Rest,
    Food,
    PokemonCare,
    Training,
    Travel,
    Grooming,
    Crafting,
    Research,
    Legal,
    Utility,
    Custom
}

public enum ServicePokemonTargetMode {
    None,
    FirstPartyPokemon,
    FirstHealthyPokemon,
    WholeParty,
    WholeHealthyParty
}

[CreateAssetMenu(menuName = "Services/Service Definition")]
public class ServiceDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable save/id key for this service. Empty uses the asset name.")]
    [SerializeField] string id = string.Empty;
    [Tooltip("Name shown in debug/future UI. Empty uses the asset name.")]
    [SerializeField] string displayName = string.Empty;
    [Tooltip("Designer note or player-facing description for this service.")]
    [TextArea]
    [SerializeField] string description = string.Empty;
    [Tooltip("Broad service category used by requirements, validators and future UI filters.")]
    [SerializeField] PlayerServiceCategory category = PlayerServiceCategory.General;
    [Tooltip("Free-form tags such as clinic, inn, spa, tutor, research, police or premium.")]
    [SerializeField] List<string> tags = new List<string>();
    [Tooltip("Optional icon used by future UI lists.")]
    [SerializeField] Sprite icon;

    [Header("Access And Repeat")]
    [Tooltip("How often this service can be used. Once Per Source, Daily and Cooldown use the provider/source id.")]
    [SerializeField] ConsequenceChainRepeatMode repeatMode = ConsequenceChainRepeatMode.Unlimited;
    [Tooltip("Cooldown in in-game hours when Repeat Mode is Cooldown Hours.")]
    [Min(0)]
    [SerializeField] int cooldownHours;
    [Tooltip("Maximum successful uses across all providers. 0 means unlimited.")]
    [Min(0)]
    [SerializeField] int maxUseCount;
    [Tooltip("If enabled, successful uses are saved in PlayerServiceLog.")]
    [SerializeField] bool recordHistory = true;
    [Tooltip("If enabled, blocked attempts are also saved in PlayerServiceLog.")]
    [SerializeField] bool recordBlockedAttempts;
    [Tooltip("How service-level requirements are evaluated.")]
    [SerializeField] ConsequenceRequirementMatchMode requirementMatchMode = ConsequenceRequirementMatchMode.All;
    [Tooltip("Requirements that must pass before this service can be used.")]
    [SerializeField] List<ActivityRequirement> requirements = new List<ActivityRequirement>();
    [Tooltip("Optional custom message shown when repeat rules block the service.")]
    [TextArea]
    [SerializeField] string repeatBlockedMessage = string.Empty;

    [Header("Cost")]
    [Tooltip("Money paid before the service applies. 0 means free.")]
    [Min(0f)]
    [SerializeField] float moneyCost;

    [Header("Linked Activity")]
    [Tooltip("Optional activity used for area rules, item/tool/need costs, XP and rewards.")]
    [SerializeField] ActivityDefinition activity;
    [Tooltip("If enabled, ActivityDefinition.CanPerform is checked before this service can run.")]
    [SerializeField] bool checkActivityCanPerform = true;
    [Tooltip("If enabled, the linked activity's item/tool/need costs are paid when the service starts.")]
    [SerializeField] bool payActivityCosts = true;
    [Tooltip("If enabled, the linked activity's XP, reputation, milestone, career, organization and outcome rewards are applied.")]
    [SerializeField] bool applyActivityRewards = true;
    [Tooltip("If enabled, the linked activity's relationship rewards are applied too.")]
    [SerializeField] bool applyActivityRelationshipRewards = true;

    [Header("Player Effects")]
    [Tooltip("Money granted after the service succeeds. Useful for paid contracts or reward services.")]
    [SerializeField] float moneyReward;
    [Tooltip("Nutrition restored through SurvivalNeedsController.Eat. 0 means no food effect.")]
    [Min(0)]
    [SerializeField] int nutrition;
    [Tooltip("Rest hours applied through SurvivalNeedsController.Rest. 0 means no rest effect.")]
    [Min(0)]
    [SerializeField] int restHours;
    [Tooltip("Sleep hours applied through SurvivalNeedsController.Sleep. 0 means no sleep effect.")]
    [Min(0)]
    [SerializeField] int sleepHours;
    [Tooltip("Specific survival need changes applied after the service succeeds.")]
    [SerializeField] List<ServiceNeedChange> needChanges = new List<ServiceNeedChange>();

    [Header("Pokemon Effects")]
    [Tooltip("Which Pokemon receive service effects.")]
    [SerializeField] ServicePokemonTargetMode pokemonTargetMode = ServicePokemonTargetMode.None;
    [Tooltip("If enabled, target Pokemon are fully healed and PP restored.")]
    [SerializeField] bool healPokemonToFull;
    [Tooltip("HP restored to each target Pokemon. 0 means no direct HP heal.")]
    [Min(0)]
    [SerializeField] int pokemonHpHeal;
    [Tooltip("Vital profile used when applying Pokemon core health/stamina and battle stamina effects. Empty uses default formulas.")]
    [SerializeField] PokemonVitalProfileDefinition pokemonVitalProfile;
    [Tooltip("If enabled, restores core health, core stamina and battle stamina to full for each target Pokemon.")]
    [SerializeField] bool restorePokemonVitalsToFull;
    [Tooltip("If enabled, restores only core health and core stamina to full for each target Pokemon.")]
    [SerializeField] bool restorePokemonCoreVitalsToFull;
    [Tooltip("If enabled, restores only battle stamina to full for each target Pokemon.")]
    [SerializeField] bool restorePokemonBattleVitalsToFull;
    [Tooltip("Fine-grained vital resource changes for each target Pokemon. Positive restores, negative drains/damages.")]
    [SerializeField] List<PokemonVitalChange> pokemonVitalChanges = new List<PokemonVitalChange>();
    [Tooltip("If enabled, regular status conditions are cleared from target Pokemon.")]
    [SerializeField] bool curePokemonStatus;
    [Tooltip("If enabled, volatile status conditions are cleared from target Pokemon.")]
    [SerializeField] bool curePokemonVolatileStatus;
    [Tooltip("Experience granted to each target Pokemon. 0 means no Pokemon XP.")]
    [Min(0)]
    [SerializeField] int pokemonExperience;
    [Tooltip("Optional Pokemon care action applied to each valid target Pokemon.")]
    [SerializeField] PokemonCareActionDefinition pokemonCareAction;
    [Tooltip("Flat bonus passed into the Pokemon care action.")]
    [SerializeField] int pokemonCareBonus;

    [Header("Progression Rewards")]
    [Tooltip("Title, badge, permit or license grants applied after the service succeeds.")]
    [SerializeField] List<TitleGrant> titleGrants = new List<TitleGrant>();
    [Tooltip("Milestones completed after the service succeeds.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Faction reputation changes applied after the service succeeds.")]
    [SerializeField] List<ReputationChange> reputationChanges = new List<ReputationChange>();
    [Tooltip("Personal relationship changes applied after the service succeeds.")]
    [SerializeField] List<RelationshipChange> relationshipChanges = new List<RelationshipChange>();
    [Tooltip("Lifestyle/playstyle point grants applied after the service succeeds.")]
    [SerializeField] List<LifestylePointGrant> lifestylePointGrants = new List<LifestylePointGrant>();
    [Tooltip("Career points awarded after the service succeeds.")]
    [SerializeField] List<CareerPointGrant> careerPointGrants = new List<CareerPointGrant>();
    [Tooltip("Life path XP, branch progress and tag counters awarded after the service succeeds.")]
    [SerializeField] List<LifePathReward> lifePathRewards = new List<LifePathReward>();
    [Tooltip("Organization memberships granted after the service succeeds.")]
    [SerializeField] List<OrganizationMembershipGrant> organizationMembershipGrants = new List<OrganizationMembershipGrant>();
    [Tooltip("Organization points awarded after the service succeeds.")]
    [SerializeField] List<OrganizationPointGrant> organizationPointGrants = new List<OrganizationPointGrant>();

    [Header("Consequences")]
    [Tooltip("Consequence chains applied after the service succeeds.")]
    [SerializeField] List<ConsequenceChainDefinition> completedChains = new List<ConsequenceChainDefinition>();
    [Tooltip("Consequence chains applied when the service is blocked.")]
    [SerializeField] List<ConsequenceChainDefinition> blockedChains = new List<ConsequenceChainDefinition>();

    [Header("Events")]
    [Tooltip("Optional event published when the service succeeds. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition completedEvent;
    [Tooltip("Optional event published when the service is blocked. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition blockedEvent;
    [Tooltip("If enabled, generated service events can appear in the notification feed.")]
    [SerializeField] bool showEventsInFeed = true;
    [Tooltip("If enabled, generated service events are written to GameDebugLogger.")]
    [SerializeField] bool writeEventsToDebugLog;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public PlayerServiceCategory Category => category;
    public IReadOnlyList<string> Tags => tags != null ? (IReadOnlyList<string>)tags : Array.Empty<string>();
    public Sprite Icon => icon;
    public ConsequenceChainRepeatMode RepeatMode => repeatMode;
    public int CooldownHours => Mathf.Max(0, cooldownHours);
    public int MaxUseCount => Mathf.Max(0, maxUseCount);
    public bool RecordHistory => recordHistory;
    public bool RecordBlockedAttempts => recordBlockedAttempts;
    public IReadOnlyList<ActivityRequirement> Requirements => requirements != null ? (IReadOnlyList<ActivityRequirement>)requirements : Array.Empty<ActivityRequirement>();
    public float MoneyCost => Mathf.Max(0f, moneyCost);
    public ActivityDefinition Activity => activity;
    public bool CheckActivityCanPerform => checkActivityCanPerform;
    public bool PayActivityCosts => payActivityCosts;
    public bool ApplyActivityRewards => applyActivityRewards;
    public bool ApplyActivityRelationshipRewards => applyActivityRelationshipRewards;
    public float MoneyReward => moneyReward;
    public int Nutrition => Mathf.Max(0, nutrition);
    public int RestHours => Mathf.Max(0, restHours);
    public int SleepHours => Mathf.Max(0, sleepHours);
    public IReadOnlyList<ServiceNeedChange> NeedChanges => needChanges != null ? (IReadOnlyList<ServiceNeedChange>)needChanges : Array.Empty<ServiceNeedChange>();
    public ServicePokemonTargetMode PokemonTargetMode => pokemonTargetMode;
    public bool HealPokemonToFull => healPokemonToFull;
    public int PokemonHpHeal => Mathf.Max(0, pokemonHpHeal);
    public PokemonVitalProfileDefinition PokemonVitalProfile => pokemonVitalProfile;
    public bool RestorePokemonVitalsToFull => restorePokemonVitalsToFull;
    public bool RestorePokemonCoreVitalsToFull => restorePokemonCoreVitalsToFull;
    public bool RestorePokemonBattleVitalsToFull => restorePokemonBattleVitalsToFull;
    public IReadOnlyList<PokemonVitalChange> PokemonVitalChanges => pokemonVitalChanges != null ? (IReadOnlyList<PokemonVitalChange>)pokemonVitalChanges : Array.Empty<PokemonVitalChange>();
    public bool CurePokemonStatus => curePokemonStatus;
    public bool CurePokemonVolatileStatus => curePokemonVolatileStatus;
    public int PokemonExperience => Mathf.Max(0, pokemonExperience);
    public PokemonCareActionDefinition PokemonCareAction => pokemonCareAction;
    public int PokemonCareBonus => pokemonCareBonus;
    public IReadOnlyList<TitleGrant> TitleGrants => titleGrants != null ? (IReadOnlyList<TitleGrant>)titleGrants : Array.Empty<TitleGrant>();
    public IReadOnlyList<MilestoneDefinition> MilestonesToComplete => milestonesToComplete != null ? (IReadOnlyList<MilestoneDefinition>)milestonesToComplete : Array.Empty<MilestoneDefinition>();
    public IReadOnlyList<ReputationChange> ReputationChanges => reputationChanges != null ? (IReadOnlyList<ReputationChange>)reputationChanges : Array.Empty<ReputationChange>();
    public IReadOnlyList<RelationshipChange> RelationshipChanges => relationshipChanges != null ? (IReadOnlyList<RelationshipChange>)relationshipChanges : Array.Empty<RelationshipChange>();
    public IReadOnlyList<LifestylePointGrant> LifestylePointGrants => lifestylePointGrants != null ? (IReadOnlyList<LifestylePointGrant>)lifestylePointGrants : Array.Empty<LifestylePointGrant>();
    public IReadOnlyList<CareerPointGrant> CareerPointGrants => careerPointGrants != null ? (IReadOnlyList<CareerPointGrant>)careerPointGrants : Array.Empty<CareerPointGrant>();
    public IReadOnlyList<LifePathReward> LifePathRewards => lifePathRewards != null ? (IReadOnlyList<LifePathReward>)lifePathRewards : Array.Empty<LifePathReward>();
    public IReadOnlyList<OrganizationMembershipGrant> OrganizationMembershipGrants => organizationMembershipGrants != null ? (IReadOnlyList<OrganizationMembershipGrant>)organizationMembershipGrants : Array.Empty<OrganizationMembershipGrant>();
    public IReadOnlyList<OrganizationPointGrant> OrganizationPointGrants => organizationPointGrants != null ? (IReadOnlyList<OrganizationPointGrant>)organizationPointGrants : Array.Empty<OrganizationPointGrant>();
    public IReadOnlyList<ConsequenceChainDefinition> CompletedChains => completedChains != null ? (IReadOnlyList<ConsequenceChainDefinition>)completedChains : Array.Empty<ConsequenceChainDefinition>();
    public IReadOnlyList<ConsequenceChainDefinition> BlockedChains => blockedChains != null ? (IReadOnlyList<ConsequenceChainDefinition>)blockedChains : Array.Empty<ConsequenceChainDefinition>();

    public bool HasTag(string tag) {
        return !string.IsNullOrWhiteSpace(tag)
            && Tags.Any(entry => string.Equals(entry, tag, StringComparison.OrdinalIgnoreCase));
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

            failureMessage = activeRequirements.FirstOrDefault()?.FailureMessage ?? "Service requirements are not met.";
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

    public bool CanUse(PlayerController player, PlayerServiceLog log, string providerId, out string failureMessage) {
        if(player == null) {
            failureMessage = "A player is required to use this service.";
            return false;
        }

        if(log != null && !log.CanUse(this, providerId, repeatMode, CooldownHours, MaxUseCount, out failureMessage)) {
            if(!string.IsNullOrWhiteSpace(repeatBlockedMessage)) {
                failureMessage = repeatBlockedMessage;
            }
            return false;
        }

        if(!RequirementsMet(player, out failureMessage)) {
            return false;
        }

        if(activity != null && checkActivityCanPerform && !activity.CanPerform(player, out failureMessage)) {
            return false;
        }

        if(MoneyCost > 0f && (Wallet.i == null || !Wallet.i.HasMoney(MoneyCost))) {
            failureMessage = $"You need {MoneyCost:0} money for {DisplayName}.";
            return false;
        }

        if(RequiresPokemonTarget() && !HasValidPokemonTarget(player, out failureMessage)) {
            return false;
        }

        failureMessage = null;
        return true;
    }

    public ServiceUseResult Use(PlayerController player, string providerId = null, string providerName = null, UnityEngine.Object context = null) {
        var result = new ServiceUseResult(Id, DisplayName, category, NormalizeProviderId(providerId), providerName);
        var unityContext = context != null ? context : this;
        var log = player != null ? player.GetComponent<PlayerServiceLog>() ?? player.gameObject.AddComponent<PlayerServiceLog>() : null;

        if(!CanUse(player, log, result.providerId, out var failureMessage)) {
            result.blocked = true;
            result.failureMessage = failureMessage;
            if(recordBlockedAttempts) {
                log?.RecordUse(this, result);
            }
            ApplyChains(player, blockedChains, result, unityContext);
            PublishServiceEvent(blockedEvent, "blocked", result, player, unityContext, GameEventImportance.Warning);
            return result;
        }

        if(activity != null && payActivityCosts && !activity.TryPayCosts(player, out failureMessage)) {
            result.blocked = true;
            result.failureMessage = failureMessage;
            if(recordBlockedAttempts) {
                log?.RecordUse(this, result);
            }
            ApplyChains(player, blockedChains, result, unityContext);
            PublishServiceEvent(blockedEvent, "blocked", result, player, unityContext, GameEventImportance.Warning);
            return result;
        }

        if(MoneyCost > 0f) {
            Wallet.i.TakeMoney(MoneyCost);
            result.moneyPaid = MoneyCost;
        }

        ApplyPlayerEffects(player, result, unityContext);
        ApplyPokemonEffects(player, result);
        ApplyProgressionRewards(player, result, unityContext);
        ApplyChains(player, completedChains, result, unityContext);

        if(recordHistory) {
            log?.RecordUse(this, result);
        }

        PublishServiceEvent(completedEvent, "completed", result, player, unityContext, GameEventImportance.Success);
        return result;
    }

    bool RequiresPokemonTarget() {
        return pokemonTargetMode != ServicePokemonTargetMode.None
            && (healPokemonToFull
                || PokemonHpHeal > 0
                || restorePokemonVitalsToFull
                || restorePokemonCoreVitalsToFull
                || restorePokemonBattleVitalsToFull
                || PokemonVitalChanges.Count > 0
                || curePokemonStatus
                || curePokemonVolatileStatus
                || PokemonExperience > 0
                || pokemonCareAction != null);
    }

    bool HasValidPokemonTarget(PlayerController player, out string failureMessage) {
        var targets = GetPokemonTargets(player);
        if(targets.Count == 0) {
            failureMessage = "No valid Pokemon target is available.";
            return false;
        }

        if(pokemonCareAction == null) {
            failureMessage = null;
            return true;
        }

        foreach(var target in targets) {
            if(pokemonCareAction.CanApply(target, out _)) {
                failureMessage = null;
                return true;
            }
        }

        pokemonCareAction.CanApply(targets.FirstOrDefault(), out failureMessage);
        failureMessage ??= "No Pokemon can receive this care service right now.";
        return false;
    }

    void ApplyPlayerEffects(PlayerController player, ServiceUseResult result, UnityEngine.Object context) {
        if(player == null) {
            return;
        }

        if(moneyReward != 0f && Wallet.i != null) {
            Wallet.i.AddMoney(moneyReward);
            result.moneyRewarded = moneyReward;
        }

        var needs = player.GetComponent<SurvivalNeedsController>();
        if(needs != null) {
            if(Nutrition > 0) {
                needs.Eat(Nutrition);
                result.messages.Add($"Nutrition restored by {Nutrition}.");
            }

            if(RestHours > 0) {
                needs.Rest(RestHours);
                result.messages.Add($"Rested for {RestHours} hour(s).");
            }

            if(SleepHours > 0) {
                needs.Sleep(SleepHours);
                result.messages.Add($"Slept for {SleepHours} hour(s).");
            }

            foreach(var change in NeedChanges) {
                if(change != null && change.need != null && change.amount != 0) {
                    needs.ChangeNeed(change.need, change.amount);
                }
            }
        }

        if(activity != null && applyActivityRewards) {
            activity.ApplyRewards(player);
        }

        if(activity != null && applyActivityRelationshipRewards) {
            activity.ApplyRelationshipRewards(player);
        }
    }

    void ApplyPokemonEffects(PlayerController player, ServiceUseResult result) {
        if(player == null || !RequiresPokemonTarget()) {
            return;
        }

        var party = player.GetComponent<PokemonParty>();
        if(party == null) {
            result.messages.Add("No Pokemon party found.");
            return;
        }

        bool changedParty = false;
        foreach(var pokemon in GetPokemonTargets(player)) {
            if(pokemon == null) {
                continue;
            }

            bool affected = false;
            if(healPokemonToFull) {
                pokemon.Heal();
                affected = true;
            }

            if(PokemonHpHeal > 0) {
                pokemon.IncreaseHP(PokemonHpHeal);
                affected = true;
            }

            if(ApplyPokemonVitalEffects(pokemon)) {
                affected = true;
            }

            if(curePokemonStatus) {
                pokemon.CureStatus();
                affected = true;
            }

            if(curePokemonVolatileStatus) {
                pokemon.CureVolatileStatus();
                affected = true;
            }

            if(PokemonExperience > 0) {
                pokemon.GainExp(PokemonExperience);
                affected = true;
            }

            if(pokemonCareAction != null) {
                if(pokemonCareAction.TryApply(pokemon, pokemonCareBonus, result.providerId, out var careFailure)) {
                    affected = true;
                } else if(!string.IsNullOrWhiteSpace(careFailure)) {
                    result.messages.Add($"{pokemon.NickName}: {careFailure}");
                }
            }

            if(affected) {
                result.affectedPokemonCount++;
                changedParty = true;
            }
        }

        if(changedParty) {
            party.PartyUpdated();
        }
    }

    void ApplyProgressionRewards(PlayerController player, ServiceUseResult result, UnityEngine.Object context) {
        if(player == null) {
            return;
        }

        player.GetComponent<PlayerTitles>()?.ApplyGrants(titleGrants, context);
        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);
        player.GetComponent<PlayerReputation>()?.ApplyChanges(reputationChanges);
        player.GetComponent<PlayerRelationships>()?.ApplyChanges(relationshipChanges);
        player.GetComponent<PlayerLifestyleLog>()?.ApplyGrants(lifestylePointGrants, $"service:{Id}", DisplayName, context);
        player.GetComponent<PlayerCareerLog>()?.ApplyPointGrants(careerPointGrants, $"service:{Id}");
        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(lifePathRewards, $"service:{Id}", DisplayName, context != null ? context : this);
        player.GetComponent<PlayerOrganizationLog>()?.ApplyMembershipGrants(organizationMembershipGrants, $"service:{Id}");
        player.GetComponent<PlayerOrganizationLog>()?.ApplyPointGrants(organizationPointGrants, $"service:{Id}");
    }

    void ApplyChains(PlayerController player, IEnumerable<ConsequenceChainDefinition> chains, ServiceUseResult result, UnityEngine.Object context) {
        if(player == null || chains == null) {
            return;
        }

        foreach(var chain in chains) {
            if(chain == null) {
                continue;
            }

            var chainResult = chain.Apply(player, new ConsequenceChainContext {
                SourceId = result.providerId,
                SourceName = string.IsNullOrWhiteSpace(result.providerName) ? result.serviceName : result.providerName,
                ContextObject = context
            }, context);

            if(chainResult != null) {
                result.appliedChainCount += Mathf.Max(0, chainResult.appliedSteps);
                if(chainResult.blocked || chainResult.failedSteps > 0) {
                    result.failedChainCount++;
                }
            }
        }
    }

    List<Pokemon> GetPokemonTargets(PlayerController player) {
        var party = player != null ? player.GetComponent<PokemonParty>() : null;
        var all = party?.Pokemons?.Where(pokemon => pokemon != null).ToList() ?? new List<Pokemon>();
        return pokemonTargetMode switch {
            ServicePokemonTargetMode.FirstPartyPokemon => all.Take(1).ToList(),
            ServicePokemonTargetMode.FirstHealthyPokemon => all.Where(pokemon => pokemon.HP > 0).Take(1).ToList(),
            ServicePokemonTargetMode.WholeParty => all,
            ServicePokemonTargetMode.WholeHealthyParty => all.Where(pokemon => pokemon.HP > 0).ToList(),
            _ => new List<Pokemon>()
        };
    }

    bool ApplyPokemonVitalEffects(Pokemon pokemon) {
        if(pokemon == null) {
            return false;
        }

        bool changed = false;
        if(restorePokemonVitalsToFull) {
            pokemon.RestoreVitalsToFull(pokemonVitalProfile);
            return true;
        }

        if(restorePokemonCoreVitalsToFull) {
            pokemon.RestoreCoreVitalsToFull(pokemonVitalProfile);
            changed = true;
        }

        if(restorePokemonBattleVitalsToFull) {
            pokemon.RestoreBattleVitalsToFull(pokemonVitalProfile);
            changed = true;
        }

        foreach(var change in PokemonVitalChanges) {
            if(change != null && change.Apply(pokemon, pokemonVitalProfile)) {
                changed = true;
            }
        }

        return changed;
    }

    void PublishServiceEvent(GameEventDefinition eventDefinition, string phase, ServiceUseResult result, PlayerController player, UnityEngine.Object context, GameEventImportance importance) {
        GameEventPublishing.PublishOptional(
            eventDefinition,
            $"service.{phase}.{Id}",
            phase == "blocked" ? $"{DisplayName} blocked." : $"{DisplayName} completed.",
            GameEventCategory.Activity,
            importance,
            context != null ? context : player,
            "ServiceDefinition",
            GameEventScope.Player,
            showInFeed: showEventsInFeed,
            writeToDebugLog: writeEventsToDebugLog,
            GameEventPublishing.Value("serviceId", Id),
            GameEventPublishing.Value("serviceName", DisplayName),
            GameEventPublishing.Value("category", category),
            GameEventPublishing.Value("providerId", result != null ? result.providerId : string.Empty),
            GameEventPublishing.Value("providerName", result != null ? result.providerName : string.Empty),
            GameEventPublishing.Value("blocked", result != null && result.blocked),
            GameEventPublishing.Value("moneyPaid", result != null ? result.moneyPaid : 0f),
            GameEventPublishing.Value("affectedPokemon", result != null ? result.affectedPokemonCount : 0));
    }

    static string NormalizeProviderId(string providerId) {
        return string.IsNullOrWhiteSpace(providerId) ? "service" : providerId;
    }
}

[Serializable]
public class ServiceNeedChange {
    [Tooltip("Survival need changed by this service.")]
    public SurvivalNeedDefinition need;
    [Tooltip("Amount added to the need. Negative values reduce it.")]
    public int amount;
}

public class ServiceUseResult {
    public readonly string serviceId;
    public readonly string serviceName;
    public readonly PlayerServiceCategory category;
    public readonly string providerId;
    public readonly string providerName;
    public bool blocked;
    public string failureMessage;
    public float moneyPaid;
    public float moneyRewarded;
    public int affectedPokemonCount;
    public int appliedChainCount;
    public int failedChainCount;
    public readonly List<string> messages = new List<string>();

    public ServiceUseResult(string serviceId, string serviceName, PlayerServiceCategory category, string providerId, string providerName) {
        this.serviceId = serviceId;
        this.serviceName = serviceName;
        this.category = category;
        this.providerId = providerId;
        this.providerName = providerName;
    }
}
