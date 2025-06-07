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
        questList.AddQuest(this);
    }

    public IEnumerator CompleteQuest(Transform player){
        Status = QuestStatus.Completed;
        yield return DialogManager.i.ShowDialog(Base.CompleteDialog);

        var inventory = Inventory.GetInventory();
        if(Base.RequiredItem != null){
            for(int i = 0; i < Base.RequiredItemCount; i++){
                inventory.RemoveItem(Base.RequiredItem);
            }
        }
        
        if(Base.RewardItem != null){
            inventory.AddItem(Base.RewardItem, Base.RewardItemCount);

            string name = player.GetComponent<PlayerController>().Name;
            yield return DialogManager.i.TypeDialog($"{name} received {Base.RewardItemCount} {Base.RewardItem.Name}{(Base.RewardItemCount > 1 ? "s" : "")} as a reward for completing the quest.");
        }

        var questList = QuestList.GetQuestList();
        questList.AddQuest(this);
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
}

[System.Serializable]
public class QuestSaveData{
    public string name;
    public QuestStatus status;
}