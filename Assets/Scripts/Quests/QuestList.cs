using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System;

public class QuestList : MonoBehaviour, ISavable{
    List<Quest> quests = new List<Quest>();

    public event Action OnUpdated;

    public void AddQuest(Quest quest){
        if(!quests.Any(q => q.Base.Name == quest.Base.Name)){
            quests.Add(quest);
        }
        OnUpdated?.Invoke();
    }

    public void AddOrUpdateQuest(Quest quest){
        if(quest == null || quest.Base == null){
            return;
        }

        var index = quests.FindIndex(q => q.Base.Name == quest.Base.Name);
        if(index >= 0){
            quests[index] = quest;
        } else {
            quests.Add(quest);
        }
        OnUpdated?.Invoke();
    }

    public static QuestList GetQuestList(){
        return FindAnyObjectByType<PlayerController>().GetComponent<QuestList>();
    }

    public bool IsStarted(string questName){
        var questStatus = quests.FirstOrDefault(q => q.Base.Name == questName)?.Status;
        return questStatus == QuestStatus.Started || questStatus == QuestStatus.Completed;
    }

    public Quest GetQuest(string questName){
        if(string.IsNullOrWhiteSpace(questName)){
            return null;
        }

        return quests.FirstOrDefault(q => q.Base.Name == questName);
    }

    public bool IsCompleted(string questName){
        var questStatus = quests.FirstOrDefault(q => q.Base.Name == questName)?.Status;
        return questStatus == QuestStatus.Completed;
    }

    public object CaptureState(){
        return quests.Select(q => q.GetSaveData()).ToList();
    }

    public void RestoreState(object state){
        var saveData = state as List<QuestSaveData>;
        if(saveData != null){
            quests = saveData.Select(q => new Quest(q)).ToList();
            OnUpdated?.Invoke();
        }
    }
}
