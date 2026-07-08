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

    Fader fader;
    PlayerController player;

    public Transform SpawnPoint => spawnPoint;

    public bool TriggerRepeatedly => false;

    public void OnPlayerTriggered(PlayerController player){
        this.player = player;
        player.Character.Animator.IsMoving = false;
        StartCoroutine(SwitchScene());
    }

    void Start(){
        fader = FindAnyObjectByType<Fader>();
    }

    IEnumerator SwitchScene(){
        if (sceneToLoad < 0) {
            Debug.LogError("Portal: sceneToLoad not set!");
            yield break;
        }

        DontDestroyOnLoad(gameObject);
        GameController.i.PauseGame(true);
        yield return fader.FadeIn(0.5f);

        yield return SceneManager.LoadSceneAsync(sceneToLoad);

        var destPortal = FindObjectsByType<Portal>().FirstOrDefault( x => x != this && x.destinationPortal == this.destinationPortal);
        
        if (destPortal != null) {
            player.Character.SetPositionAndSnapToTile(destPortal.SpawnPoint.position);
        } else {
            Debug.LogError($"Portal: Destination portal {destinationPortal} not found in scene {sceneToLoad}");
        }

        yield return fader.FadeOut(0.5f);

        GameController.i.PauseGame(false);
        Destroy(gameObject);
    }
}
