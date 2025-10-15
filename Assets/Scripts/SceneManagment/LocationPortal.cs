using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class LocationPortal : MonoBehaviour, IPlayerTriggerable{
    [SerializeField] Transform spawnPoint;
    [SerializeField] DestinationIdentifier destinationPortal;

    Fader fader;

    public Transform SpawnPoint => spawnPoint;

    public bool TriggerRepeatedly => false;

    PlayerController player;

    public void OnPlayerTriggered(PlayerController player){
        this.player = player;
        player.Character.Animator.IsMoving = false;
        StartCoroutine(Teleport());
    }

    void Start(){
        fader = FindAnyObjectByType<Fader>();
    }

    IEnumerator Teleport(){
        GameController.i.PauseGame(true);
        yield return fader.FadeIn(0.5f);

        var destPortal = FindObjectsByType<LocationPortal>(FindObjectsSortMode.None).First( x => x != this && x.destinationPortal == this.destinationPortal);
        player.Character.SetPositionAndSnapToTile(destPortal.SpawnPoint.position);
        yield return fader.FadeOut(0.5f);

        GameController.i.PauseGame(false);
    }
}
