using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameLayers : MonoBehaviour{
    [SerializeField] LayerMask solidObjectsLayer;
    [SerializeField] LayerMask grassLayer;
    [SerializeField] LayerMask interactableLayer;
    [SerializeField] LayerMask playerLayer;

    public LayerMask SolidObjectsLayer => solidObjectsLayer;
    public LayerMask GrassLayer => grassLayer;
    public LayerMask InteractableLayer => interactableLayer;
    public LayerMask PlayerLayer => playerLayer;

    public static GameLayers i { get; set; }

    private void Awake(){
        i = this;
    }
}