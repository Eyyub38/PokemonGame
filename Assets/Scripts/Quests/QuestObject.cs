using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ObjecttAction{ DoNothing, Enable, Desable}

public class QuestObject : MonoBehaviour{
    [SerializeField] QuestBase questToCheck;
    [SerializeField] ObjecttAction onStart;
    [SerializeField] ObjecttAction onComplete;

    QuestList questList;

    private void Start(){
        questList = QuestList.GetQuestList();
        questList.OnUpdated += UpdateObjectStatus;

        UpdateObjectStatus();
    }

    private void OnDestroy(){
        questList.OnUpdated -= UpdateObjectStatus;
    }

    public void UpdateObjectStatus(){
        if(onStart != ObjecttAction.DoNothing && questList.IsStarted(questToCheck.Name)){
            foreach(Transform child in transform){
                if(onStart == ObjecttAction.Enable){
                    child.gameObject.SetActive(true);
                } else if(onStart == ObjecttAction.Desable){
                    child.gameObject.SetActive(false);
                }
            }
        }
        if(onComplete != ObjecttAction.DoNothing && questList.IsCompleted(questToCheck.Name)){
            foreach(Transform child in transform){
                if(onComplete == ObjecttAction.Enable){
                    child.gameObject.SetActive(true);
                } else if(onComplete == ObjecttAction.Desable){
                    child.gameObject.SetActive(false);
                }
            }
        }
    }   
}
