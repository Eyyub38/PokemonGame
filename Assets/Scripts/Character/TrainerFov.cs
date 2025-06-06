using UnityEngine;

public class TrainerFov : MonoBehaviour, IPlayerTriggerable
{
    public bool TriggerRepeatedly => false;

    public void OnPlayerTriggered(PlayerController player){
        player.Character.Animator.IsMoving = false;
        GameController.i.OnEnterTrainersView(GetComponentInParent<TrainerController>());
    }
}