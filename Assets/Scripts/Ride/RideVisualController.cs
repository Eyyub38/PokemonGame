using System.Collections.Generic;
using UnityEngine;

public class RideVisualController : MonoBehaviour {
    [Tooltip("Ride definition that controls sprites, offsets and sorting. Usually assigned by PlayerRideController at runtime.")]
    [SerializeField] RidePokemonDefinition rideDefinition;
    [Tooltip("Player this visual follows. Empty tries PlayerController.i.")]
    [SerializeField] PlayerController playerOverride;
    [Tooltip("SpriteRenderer used for ride sprites. Empty uses or creates one on this GameObject.")]
    [SerializeField] SpriteRenderer spriteRendererOverride;
    [Tooltip("If enabled, the visual follows directional offsets every frame.")]
    [SerializeField] bool applyDirectionalOffsets = true;

    SpriteRenderer spriteRenderer;
    SpriteRenderer playerRenderer;
    float frameTimer;
    int frameIndex;
    FacingDirection lastDirection;
    bool lastMoving;

    public void Bind(RidePokemonDefinition definition, PlayerController player) {
        rideDefinition = definition;
        playerOverride = player;
        InitializeRenderer();
        ApplyFrame(force: true);
    }

    void Awake() {
        InitializeRenderer();
    }

    void LateUpdate() {
        if(rideDefinition == null) {
            return;
        }

        var player = ResolvePlayer();
        var animator = player != null && player.Character != null ? player.Character.Animator : null;
        if(animator == null) {
            return;
        }

        if(applyDirectionalOffsets && rideDefinition.VisualOffsets != null) {
            transform.localPosition = rideDefinition.VisualOffsets.GetOffset(animator.CurrentFacingDirection);
        }

        ApplySorting(player);
        float frameSeconds = rideDefinition.DirectionalSprites != null ? rideDefinition.DirectionalSprites.FrameSeconds : 0.15f;
        frameTimer += Time.deltaTime;
        if(animator.CurrentFacingDirection != lastDirection || animator.IsMoving != lastMoving || frameTimer >= frameSeconds) {
            ApplyFrame(force: false);
        }
    }

    void InitializeRenderer() {
        spriteRenderer = spriteRendererOverride != null ? spriteRendererOverride : GetComponent<SpriteRenderer>();
        if(spriteRenderer == null) {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    void ApplyFrame(bool force) {
        var player = ResolvePlayer();
        var animator = player != null && player.Character != null ? player.Character.Animator : null;
        if(animator == null || rideDefinition == null || rideDefinition.DirectionalSprites == null || !rideDefinition.DirectionalSprites.HasAnySprite) {
            return;
        }

        var direction = animator.CurrentFacingDirection;
        bool moving = animator.IsMoving;
        IReadOnlyList<Sprite> frames = rideDefinition.DirectionalSprites.GetFrames(direction, moving);
        if(frames.Count == 0) {
            return;
        }

        if(force || direction != lastDirection || moving != lastMoving) {
            frameIndex = 0;
            frameTimer = 0f;
        } else {
            frameIndex = (frameIndex + 1) % frames.Count;
            frameTimer = 0f;
        }

        InitializeRenderer();
        spriteRenderer.sprite = frames[Mathf.Clamp(frameIndex, 0, frames.Count - 1)];
        lastDirection = direction;
        lastMoving = moving;
    }

    void ApplySorting(PlayerController player) {
        InitializeRenderer();
        if(spriteRenderer == null || rideDefinition == null) {
            return;
        }

        if(playerRenderer == null && player != null) {
            playerRenderer = player.GetComponent<SpriteRenderer>();
        }

        if(!string.IsNullOrWhiteSpace(rideDefinition.SortingLayerName)) {
            spriteRenderer.sortingLayerName = rideDefinition.SortingLayerName;
        }

        if(playerRenderer != null) {
            spriteRenderer.sortingOrder = playerRenderer.sortingOrder + rideDefinition.SortingOrderOffset;
        }
    }

    PlayerController ResolvePlayer() {
        if(playerOverride != null) {
            return playerOverride;
        }

        playerOverride = PlayerController.i != null ? PlayerController.i : FindAnyObjectByType<PlayerController>();
        return playerOverride;
    }
}
