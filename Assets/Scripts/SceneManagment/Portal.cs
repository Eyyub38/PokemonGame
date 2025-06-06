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

    public Transform SpawnPoint => spawnPoint;
    public bool TriggerRepeatedly => false;

    PlayerController player;

    public void OnPlayerTriggered(PlayerController player){
        this.player = player;
        player.Character.Animator.IsMoving = false;
        StartCoroutine(SwitchScene());
    }

    void Start(){
        fader = FindObjectOfType<Fader>();
    }

    IEnumerator SwitchScene(){
        DontDestroyOnLoad(gameObject);
        GameController.i.PauseGame(true);
        yield return fader.FadeIn(0.5f);

        yield return SceneManager.LoadSceneAsync(sceneToLoad);

        var destPortal = FindObjectsOfType<Portal>().First( x => x != this && x.destinationPortal == this.destinationPortal);
        player.Character.SetPositionAndSnapToTile(destPortal.SpawnPoint.position);
        yield return fader.FadeOut(0.5f);

        GameController.i.PauseGame(false);
        Destroy(gameObject);
    }
}
