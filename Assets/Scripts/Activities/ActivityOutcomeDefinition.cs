using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Activities/Activity Outcome Definition")]
public class ActivityOutcomeDefinition : ScriptableObject {
    [Header("Identity")]
    [Tooltip("Stable id for this outcome. Empty uses the asset name.")]
    [SerializeField] string id;
    [Tooltip("Name shown in UI/debug. Empty uses the asset name.")]
    [SerializeField] string displayName;
    [Tooltip("Designer note or player-facing explanation of this outcome.")]
    [TextArea][SerializeField] string description;
    [Tooltip("Chance for this outcome to trigger after its parent activity completes.")]
    [Range(0f, 1f)][SerializeField] float chance = 1f;

    [Header("Rewards")]
    [Tooltip("Items granted if this outcome triggers.")]
    [SerializeField] List<ActivityOutcomeItemReward> itemRewards = new List<ActivityOutcomeItemReward>();
    [Tooltip("Trainer XP granted if this outcome triggers.")]
    [Min(0)]
    [SerializeField] int trainerExperience;
    [Tooltip("Progression source used for trainer XP multipliers.")]
    [SerializeField] PlayerExperienceSource experienceSource = PlayerExperienceSource.Exploration;
    [Tooltip("Faction reputation changes applied if this outcome triggers.")]
    [SerializeField] List<ReputationChange> reputationChanges = new List<ReputationChange>();
    [Tooltip("Personal relationship changes applied if this outcome triggers.")]
    [SerializeField] List<RelationshipChange> relationshipChanges = new List<RelationshipChange>();
    [Tooltip("Milestones completed if this outcome triggers.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Crafting recipes learned if this outcome triggers.")]
    [SerializeField] List<RecipeGrant> recipeRewards = new List<RecipeGrant>();
    [Tooltip("Career points awarded if this outcome triggers.")]
    [SerializeField] List<CareerPointGrant> careerPointRewards = new List<CareerPointGrant>();
    [Tooltip("Life path XP, branch progress and tag counters awarded if this outcome triggers.")]
    [SerializeField] List<LifePathReward> lifePathRewards = new List<LifePathReward>();
    [Tooltip("Organization memberships granted if this outcome triggers.")]
    [SerializeField] List<OrganizationMembershipGrant> organizationMembershipRewards = new List<OrganizationMembershipGrant>();
    [Tooltip("Organization points awarded if this outcome triggers.")]
    [SerializeField] List<OrganizationPointGrant> organizationPointRewards = new List<OrganizationPointGrant>();
    [Tooltip("Survival need changes applied if this outcome triggers. Positive restores, negative drains.")]
    [SerializeField] List<ActivityNeedReward> needChanges = new List<ActivityNeedReward>();
    [Tooltip("Pokemon mood changes applied if this outcome triggers.")]
    [SerializeField] List<PokemonMoodChange> pokemonMoodChanges = new List<PokemonMoodChange>();
    [Tooltip("If enabled, mood changes apply to the whole party; otherwise only the first healthy Pokemon.")]
    [SerializeField] bool applyPokemonMoodToWholeParty;

    [Header("Consequences")]
    [Tooltip("Optional consequence chains applied after this outcome's direct rewards. Use these for reusable story/system side effects.")]
    [SerializeField] List<ConsequenceChainDefinition> consequenceChains = new List<ConsequenceChainDefinition>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public float Chance => Mathf.Clamp01(chance);
    public IReadOnlyList<CareerPointGrant> CareerPointRewards => careerPointRewards;
    public IReadOnlyList<LifePathReward> LifePathRewards => lifePathRewards;
    public IReadOnlyList<OrganizationMembershipGrant> OrganizationMembershipRewards => organizationMembershipRewards;
    public IReadOnlyList<OrganizationPointGrant> OrganizationPointRewards => organizationPointRewards;
    public IReadOnlyList<ConsequenceChainDefinition> ConsequenceChains => consequenceChains;

    public bool TryApply(PlayerController player) {
        if(player == null || Random.value > Chance) {
            return false;
        }

        ApplyItemRewards();

        if(trainerExperience > 0) {
            player.GetComponent<PlayerProgression>()?.AddExperience(trainerExperience, experienceSource);
        }

        player.GetComponent<PlayerReputation>()?.ApplyChanges(reputationChanges);
        player.GetComponent<PlayerRelationships>()?.ApplyChanges(relationshipChanges);
        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(milestonesToComplete);
        player.GetComponent<PlayerRecipeBook>()?.ApplyGrants(recipeRewards, player);
        player.GetComponent<PlayerCareerLog>()?.ApplyPointGrants(careerPointRewards, $"activity-outcome:{Id}");
        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(lifePathRewards, $"activity-outcome:{Id}", DisplayName, this);
        var organizationLog = player.GetComponent<PlayerOrganizationLog>();
        organizationLog?.ApplyMembershipGrants(organizationMembershipRewards, $"activity-outcome:{Id}");
        organizationLog?.ApplyPointGrants(organizationPointRewards, $"activity-outcome:{Id}");
        ApplyNeedChanges(player);
        ApplyPokemonMoodChanges(player);
        ApplyConsequenceChains(player);
        return true;
    }

    void ApplyItemRewards() {
        var inventory = Inventory.GetInventory();
        if(inventory == null) {
            return;
        }

        foreach(var reward in itemRewards) {
            if(reward == null || reward.item == null) {
                continue;
            }

            int count = reward.RollCount();
            if(count > 0) {
                inventory.AddItem(reward.item, count);
            }
        }
    }

    void ApplyNeedChanges(PlayerController player) {
        var needs = player.GetComponent<SurvivalNeedsController>();
        if(needs == null) {
            return;
        }

        foreach(var change in needChanges) {
            if(change != null && change.need != null && change.amount != 0) {
                needs.ChangeNeed(change.need, change.amount);
            }
        }
    }

    void ApplyPokemonMoodChanges(PlayerController player) {
        if(pokemonMoodChanges == null || pokemonMoodChanges.Count == 0) {
            return;
        }

        var party = player.GetComponent<PokemonParty>();
        if(party == null || party.Pokemons == null) {
            return;
        }

        if(applyPokemonMoodToWholeParty) {
            foreach(var pokemon in party.Pokemons) {
                ApplyPokemonMoodChanges(pokemon);
            }
        } else {
            ApplyPokemonMoodChanges(party.GetHealthyPokemon());
        }
    }

    void ApplyPokemonMoodChanges(Pokemon pokemon) {
        if(pokemon == null) {
            return;
        }

        foreach(var moodChange in pokemonMoodChanges) {
            if(moodChange != null && moodChange.mood != null && moodChange.amount != 0) {
                pokemon.ChangeMood(moodChange.mood, moodChange.amount);
            }
        }
    }

    void ApplyConsequenceChains(PlayerController player) {
        if(player == null || consequenceChains == null || consequenceChains.Count == 0) {
            return;
        }

        var context = new ConsequenceChainContext {
            SourceId = $"activity-outcome:{Id}",
            SourceName = DisplayName,
            ContextObject = this
        };

        foreach(var chain in consequenceChains) {
            chain?.Apply(player, context, this);
        }
    }
}

[System.Serializable]
public class ActivityOutcomeItemReward {
    [Tooltip("Item granted by this outcome.")]
    public ItemBase item;
    [Tooltip("Minimum item count granted.")]
    [Min(0)]
    public int minCount = 1;
    [Tooltip("Maximum item count granted.")]
    [Min(0)]
    public int maxCount = 1;

    public int RollCount() {
        int min = Mathf.Max(0, minCount);
        int max = Mathf.Max(min, maxCount);
        return Random.Range(min, max + 1);
    }
}

[System.Serializable]
public class ActivityNeedReward {
    [Tooltip("Survival need affected by this outcome.")]
    public SurvivalNeedDefinition need;
    [Tooltip("Amount to change. Positive restores, negative drains.")]
    public int amount;
}
