using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum QuestStatus { None , Started, InProgress, Completed}

[System.Serializable]
public class Quest{
    public QuestBase Base{ get; private set; }
    public QuestStatus Status { get; private set; }

    public Quest(QuestBase _base){
        Base = _base;
    }

    public Quest(QuestSaveData saveData){
        Base = QuestDB.GetObjectByName(saveData.name);
        Status = saveData.status;
    }

    public IEnumerator StartQuest(){
        Status = QuestStatus.Started;
        yield return DialogManager.i.ShowDialog(Base.StartDialog);

        var questList = QuestList.GetQuestList();
        questList.AddOrUpdateQuest(this);
        PublishStartedEvent(questList);
    }

    public IEnumerator CompleteQuest(Transform player){
        Status = QuestStatus.Completed;
        yield return DialogManager.i.ShowDialog(Base.CompleteDialog);

        var inventory = Inventory.GetInventory();
        if(Base.RequiredItem != null){
            inventory.RemoveItem(Base.RequiredItem, Base.RequiredItemCount);
        }
        
        if(Base.RewardItem != null){
            inventory.AddItem(Base.RewardItem, Base.RewardItemCount);

            string name = player.GetComponent<PlayerController>().Name;
            yield return DialogManager.i.ShowDialogText($"{name} received {Base.RewardItemCount} {Base.RewardItem.Name}{(Base.RewardItemCount > 1 ? "s" : "")} as a reward for completing the quest.");
        }

        if(Base.RewardExperience > 0){
            var progression = player.GetComponent<PlayerProgression>();
            progression?.AddExperience(Base.RewardExperience, PlayerExperienceSource.Quest);
        }

        player.GetComponent<PlayerReputation>()?.ApplyChanges(Base.ReputationRewards);
        player.GetComponent<PlayerRelationships>()?.ApplyChanges(Base.RelationshipRewards);
        player.GetComponent<PlayerMilestones>()?.CompleteMilestones(Base.MilestonesToComplete);
        player.GetComponent<PlayerTitles>()?.ApplyGrants(Base.TitleRewards, player);
        player.GetComponent<PlayerRecipeBook>()?.ApplyGrants(Base.RecipeRewards, player);
        player.GetComponent<PlayerLifePathLog>()?.ApplyRewards(Base.LifePathRewards, $"quest:{Base.Name}", Base.Name, Base);

        var questList = QuestList.GetQuestList();
        questList.AddOrUpdateQuest(this);
        PublishCompletedEvent(player);
    }

    public bool CanBeCompleted(){
        var inventory = Inventory.GetInventory();
        if(Base.RequiredItem != null){
            if(!inventory.HasItemEnough(Base.RequiredItem, Base.RequiredItemCount)){
                return false;
            }
        }
        return true;
    }

    public QuestSaveData GetSaveData(){
        var saveData = new QuestSaveData(){
        name = Base.Name,
        status = Status
    };
        return saveData;
    }

    void PublishStartedEvent(UnityEngine.Object context) {
        GameEventPublishing.PublishOptional(
            Base.StartedEvent,
            $"quest.started.{Base.Name}",
            $"{Base.Name} started.",
            GameEventCategory.Quest,
            GameEventImportance.Info,
            context,
            "Quest",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("questName", Base.Name));
    }

    void PublishCompletedEvent(UnityEngine.Object context) {
        GameEventPublishing.PublishOptional(
            Base.CompletedEvent,
            $"quest.completed.{Base.Name}",
            $"{Base.Name} completed.",
            GameEventCategory.Quest,
            GameEventImportance.Success,
            context,
            "Quest",
            GameEventScope.Player,
            showInFeed: true,
            writeToDebugLog: false,
            GameEventPublishing.Value("questName", Base.Name),
            GameEventPublishing.Value("experience", Base.RewardExperience),
            GameEventPublishing.Value("rewardItem", Base.RewardItem != null ? Base.RewardItem.Name : null),
            GameEventPublishing.Value("rewardItemCount", Base.RewardItemCount));
    }
}

[System.Serializable]
public class QuestSaveData{
    public string name;
    public QuestStatus status;
}
