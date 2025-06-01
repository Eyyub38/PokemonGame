using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public enum DestinationIdentifier { A, B, C, D, E, F, G, H, I, J}

public class Portal : MonoBehaviour, IPlayerTriggerable{
    [SerializeField] int sceneToLoad = -1;
    [SerializeField] Transform spawnPoint;
    [SerializeField] DestinationIdentifier destinationPortal;

    public Transform SpawnPoint => spawnPoint;

    PlayerController player;

    public void OnPlayerTriggered(PlayerController player){
        this.player = player;
        StartCoroutine(SwitchScene());
    }

    IEnumerator SwitchScene(){
        DontDestroyOnLoad(gameObject);
        GameController.Instance.PauseGame(true);

        yield return SceneManager.LoadSceneAsync(sceneToLoad);

        var destPortal = FindObjectsOfType<Portal>().First( x => x != this && x.destinationPortal == this.destinationPortal);
        player.Character.SetPositionAndSnapToTile(destPortal.SpawnPoint.position);

        GameController.Instance.PauseGame(false);
        Destroy(gameObject);
    }
}
