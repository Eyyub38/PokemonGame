using UnityEngine;

public class GameLayers : MonoBehaviour{
    [SerializeField] LayerMask solidObjectsLayer;
    [SerializeField] LayerMask grassLayer;
    [SerializeField] LayerMask interactableLayer;
    [SerializeField] LayerMask playerLayer;
    [SerializeField] LayerMask fovLayer;
    [SerializeField] LayerMask portalLayer;
    [SerializeField] LayerMask triggersLayer;
    [SerializeField] LayerMask ledgesLayer;
    [SerializeField] LayerMask waterLayer;

    public LayerMask SolidObjectsLayer => solidObjectsLayer;
    public LayerMask GrassLayer => grassLayer;
    public LayerMask InteractableLayer => interactableLayer;
    public LayerMask PlayerLayer => playerLayer;
    public LayerMask FovLayer => fovLayer;
    public LayerMask PortalLayer => portalLayer;
    public LayerMask LedgesLayer => ledgesLayer;
    public LayerMask WaterLayer => waterLayer;

    public LayerMask TriggerableLayers {
        get => grassLayer | fovLayer | portalLayer | triggersLayer | waterLayer;
    }

    public static GameLayers i { get; set; }

    private void Awake(){
        i = this;
    }
}