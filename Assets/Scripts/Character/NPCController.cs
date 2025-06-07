using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum NPCState{ Idle, Walking, Dialog}

public class NPCController : MonoBehaviour, Interactable{
    [Header("NPC Dialog")]
    [SerializeField] Dialog dialog;
 
    [Header("NPC Move Pattern")]
    [SerializeField] List<Vector2> movementPattern;
    [SerializeField] float timeBetweenPattern;

    [Header("Quests")]
    [SerializeField] QuestBase questToStart;
    [SerializeField] QuestBase questToComplete;

    Character character;
    ItemGiver itemGiver;
    PokemonGiver pokemonGiver;
    NPCState state;
    Quest activeQuest;
    float idleTimer = 0f;
    int currentPattern = 0;

    void Awake(){
        character = GetComponent<Character>();
        itemGiver = GetComponent<ItemGiver>();
        pokemonGiver = GetComponent<PokemonGiver>();
    }

    public IEnumerator Interact(Transform initiator){
        if(state == NPCState.Idle){
            state = NPCState.Dialog;

            character.LookTowards(initiator.position);

            if(questToComplete != null){
                var quest = new Quest(questToComplete);
                quest.CompleteQuest(initiator);
                questToComplete = null;
            }

            if(itemGiver != null && itemGiver.CanBeGiven()){
                yield return itemGiver.GiveItem(initiator.GetComponent<PlayerController>());
            } else if(pokemonGiver != null && pokemonGiver.CanBeGiven()){
                yield return pokemonGiver.GivePokemon(initiator.GetComponent<PlayerController>());
            } else if(questToStart != null){
                activeQuest = new Quest(questToStart);
                yield return activeQuest.StartQuest();
                questToStart = null;

                if(activeQuest.CanBeCompleted()){
                    yield return activeQuest.CompleteQuest(initiator);
                    activeQuest = null;
                }
            } else if(activeQuest != null){
                if(activeQuest.CanBeCompleted()){
                    yield return activeQuest.CompleteQuest(initiator);
                    activeQuest = null;
                } else {
                    yield return DialogManager.i.ShowDialog(activeQuest.Base.InProgressDialog);
                }
            } else {
                yield return DialogManager.i.ShowDialog( dialog);
            }
            idleTimer = 0f;
            state = NPCState.Idle;
        }
    }

    void Update(){
        if(state == NPCState.Idle){
            idleTimer += Time.deltaTime;
            if(idleTimer > timeBetweenPattern){
                idleTimer = 0f;
                if(movementPattern.Count > 0)
                    StartCoroutine(Walk());
            }
        }
        character.HandleUpdate();
    }

    IEnumerator Walk(){
        state = NPCState.Walking;

        var oldPos = transform.position;

        yield return character.Move(movementPattern[currentPattern]);

        if(oldPos != transform.position){
            currentPattern = (currentPattern + 1) % movementPattern.Count;
        }

        state = NPCState.Idle;
    }
}