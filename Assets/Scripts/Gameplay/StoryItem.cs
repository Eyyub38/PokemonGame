using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StoryItem : MonoBehaviour, IPlayerTriggerable{
    [SerializeField] Dialog dialog;

    public bool TriggerRepeatedly => false;

    public void OnPlayerTriggered(PlayerController player){
        player.Character.Animator.IsMoving = false;
        StartCoroutine(DialogManager.i.ShowDialog(dialog));
    }
}
