using System.Collections.Generic;
using UnityEngine;

public class PokemonFollowerVisualController : MonoBehaviour {
    [Tooltip("SpriteRenderer used by this follower visual. Empty uses or creates one on this GameObject.")]
    [SerializeField] SpriteRenderer spriteRendererOverride;

    Pokemon pokemon;
    PokemonFollowerVisualDefinition definition;
    PlayerController player;
    SpriteRenderer spriteRenderer;
    FacingDirection facingDirection = FacingDirection.Down;
    bool moving;
    float frameTimer;
    int frameIndex;

    public void Initialize(Pokemon pokemon, PokemonFollowerVisualDefinition definition, PlayerController player) {
        this.pokemon = pokemon;
        this.definition = definition;
        this.player = player;
        ResolveRenderer();
        ApplySorting();
        transform.localScale = definition != null ? definition.VisualScale : Vector3.one;
        RefreshSprite(forceFrameReset: true);
    }

    public void SetFacing(Vector3 moveVector) {
        if(Mathf.Abs(moveVector.x) > Mathf.Abs(moveVector.y)) {
            facingDirection = moveVector.x >= 0f ? FacingDirection.Right : FacingDirection.Left;
        } else if(Mathf.Abs(moveVector.y) > 0.01f) {
            facingDirection = moveVector.y >= 0f ? FacingDirection.Up : FacingDirection.Down;
        }

        RefreshSprite(forceFrameReset: false);
    }

    public void SetMoving(bool isMoving) {
        if(moving == isMoving) {
            return;
        }

        moving = isMoving;
        RefreshSprite(forceFrameReset: true);
    }

    void Update() {
        if(definition == null || spriteRenderer == null) {
            return;
        }

        var frames = GetCurrentFrames();
        if(frames.Count <= 1) {
            return;
        }

        frameTimer += Time.deltaTime;
        if(frameTimer < definition.DirectionalSprites.FrameSeconds) {
            return;
        }

        frameTimer = 0f;
        frameIndex = (frameIndex + 1) % frames.Count;
        spriteRenderer.sprite = frames[frameIndex];
    }

    void ResolveRenderer() {
        spriteRenderer = spriteRendererOverride != null ? spriteRendererOverride : GetComponent<SpriteRenderer>();
        if(spriteRenderer == null) {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    void ApplySorting() {
        if(spriteRenderer == null || definition == null) {
            return;
        }

        if(!string.IsNullOrWhiteSpace(definition.SortingLayerName)) {
            spriteRenderer.sortingLayerName = definition.SortingLayerName;
        }

        var playerRenderer = player != null ? player.GetComponent<SpriteRenderer>() : null;
        if(playerRenderer != null) {
            spriteRenderer.sortingOrder = playerRenderer.sortingOrder + definition.SortingOrderOffset;
        }
    }

    void RefreshSprite(bool forceFrameReset) {
        if(spriteRenderer == null || definition == null) {
            return;
        }

        if(forceFrameReset) {
            frameIndex = 0;
            frameTimer = 0f;
        }

        var frames = GetCurrentFrames();
        if(frames.Count > 0) {
            spriteRenderer.sprite = frames[Mathf.Clamp(frameIndex, 0, frames.Count - 1)];
            return;
        }

        spriteRenderer.sprite = definition.ResolveFallbackSprite(pokemon);
    }

    IReadOnlyList<Sprite> GetCurrentFrames() {
        if(definition == null || definition.DirectionalSprites == null) {
            return System.Array.Empty<Sprite>();
        }

        return definition.DirectionalSprites.GetFrames(facingDirection, moving);
    }
}
