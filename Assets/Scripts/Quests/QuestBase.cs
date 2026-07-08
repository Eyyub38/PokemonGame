using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Quests/Creat a new quest")]
public class QuestBase : ScriptableObject{
    [Header("Quest Info")]
    [Tooltip("Quest name shown in logs/dialog.")]
    [SerializeField] string _name;
    [Tooltip("Quest description or designer notes.")]
    [SerializeField] string description;

    [Header("Quest Dialogs")]
    [Tooltip("Dialog shown when the quest starts.")]
    [SerializeField] Dialog startDialog;
    [Tooltip("Dialog shown while the quest is active but incomplete. Empty falls back to start dialog.")]
    [SerializeField] Dialog inProgressDialog;
    [Tooltip("Dialog shown when completing the quest.")]
    [SerializeField] Dialog completeDialog;

    [Header("Quest Requirements")]
    [Tooltip("Item required to complete this quest.")]
    [SerializeField] ItemBase requiredItem;
    [Tooltip("Amount of required item needed.")]
    [Min(1)]
    [SerializeField] int requiredItemCount = 1;

    [Header("Quest Rewards")]
    [Tooltip("Item awarded when the quest completes.")]
    [SerializeField] ItemBase rewardItem;
    [Tooltip("Amount of reward item awarded.")]
    [Min(0)]
    [SerializeField] int rewardItemCount = 1;
    [Tooltip("Trainer XP awarded when the quest completes.")]
    [Min(0)]
    [SerializeField] int rewardExperience = 50;
    [Tooltip("Faction reputation changes awarded on completion.")]
    [SerializeField] List<ReputationChange> reputationRewards = new List<ReputationChange>();
    [Tooltip("Relationship changes awarded on completion.")]
    [SerializeField] List<RelationshipChange> relationshipRewards = new List<RelationshipChange>();
    [Tooltip("Milestones completed when this quest completes.")]
    [SerializeField] List<MilestoneDefinition> milestonesToComplete = new List<MilestoneDefinition>();
    [Tooltip("Titles, badges or permits granted when this quest completes.")]
    [SerializeField] List<TitleGrant> titleRewards = new List<TitleGrant>();
    [Tooltip("Crafting recipes learned when this quest completes.")]
    [SerializeField] List<RecipeGrant> recipeRewards = new List<RecipeGrant>();
    [Tooltip("Life Path XP, branch progress, tag counters or perk unlocks awarded when this quest completes.")]
    [SerializeField] List<LifePathReward> lifePathRewards = new List<LifePathReward>();
    [Header("Events")]
    [Tooltip("Optional event published when this quest starts. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition startedEvent;
    [Tooltip("Optional event published when this quest completes. Empty uses a generated runtime event.")]
    [SerializeField] GameEventDefinition completedEvent;

    public string Name => _name;
    public string Description => description;
    public Dialog StartDialog => startDialog;
    public Dialog InProgressDialog => inProgressDialog?.Lines?.Count > 0 ? inProgressDialog : startDialog;
    public Dialog CompleteDialog => completeDialog;
    public ItemBase RequiredItem => requiredItem;
    public int RequiredItemCount => requiredItemCount; 
    public ItemBase RewardItem => rewardItem;
    public int RewardItemCount => rewardItemCount;
    public int RewardExperience => rewardExperience;
    public IReadOnlyList<ReputationChange> ReputationRewards => reputationRewards;
    public IReadOnlyList<RelationshipChange> RelationshipRewards => relationshipRewards;
    public IReadOnlyList<MilestoneDefinition> MilestonesToComplete => milestonesToComplete;
    public IReadOnlyList<TitleGrant> TitleRewards => titleRewards;
    public IReadOnlyList<RecipeGrant> RecipeRewards => recipeRewards;
    public IReadOnlyList<LifePathReward> LifePathRewards => lifePathRewards;
    public GameEventDefinition StartedEvent => startedEvent;
    public GameEventDefinition CompletedEvent => completedEvent;
}
