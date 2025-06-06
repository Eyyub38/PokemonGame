using UnityEngine;

public class LongGrass: MonoBehaviour, IPlayerTriggerable {
    public bool TriggerRepeatedly => true;

    public void OnPlayerTriggered(PlayerController player){
            if(UnityEngine.Random.Range(1,101) <= 10){
                player.Character.Animator.IsMoving = false;
                GameController.i.StartBattle();
            }
        }
}
