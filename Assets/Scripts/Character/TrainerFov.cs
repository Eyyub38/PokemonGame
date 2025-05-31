using UnityEngine;

public class TrainerFov : MonoBehaviour, IPlayerTriggerable
{
    public void OnPlayerTriggered(PlayerController player){
        GameController.Instance.OnEnterTrainersView(GetComponentInParent<TrainerController>());
    }
}