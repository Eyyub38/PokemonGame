using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PokemonAnimator : MonoBehaviour{
    SpriteRenderer spriteRenderer;
    SpriteAnimator spriteAnimator;
    PokemonBase surferPokemon;

    public float MoveX { get; set; }
    public float MoveY { get; set; }
    public bool IsSurfing { get; set; }
    public PokemonBase SurferPokemon { get; set; }

    private Vector3 originalPosition = Vector3.zero;
    private bool wasMoving = false;
    private bool shouldReturnToOriginal = false;

    void Start(){
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteAnimator = new SpriteAnimator(null, spriteRenderer);
        
        if (spriteRenderer != null){
            spriteRenderer.sortingLayerName = "Objects";
        }
    }

    void Update(){
        if (IsSurfing && SurferPokemon != null && SurferPokemon.SurfSprites != null && SurferPokemon.SurfSprites.Count > 0){
            HandleSurfMovement();
        } else {
            spriteAnimator?.HandleUpdate();
        }
    }

    void HandleSurfMovement(){
        bool isCurrentlyMoving = MoveX != 0 || MoveY != 0;
        
        if (isCurrentlyMoving){
            if (!wasMoving && shouldReturnToOriginal){
                transform.localPosition = originalPosition;
                shouldReturnToOriginal = false;
            }
            PlaySurfAnimation();
            UpdatePosition();
            wasMoving = true;
        } else {
            if (wasMoving){
                shouldReturnToOriginal = true;
            }
            wasMoving = false;
        }
    }

    void PlaySurfAnimation(){
        if (SurferPokemon?.SurfSprites == null || SurferPokemon.SurfSprites.Count == 0) return;

        int spriteIndex = 0;
        
        if (MoveY == 1){
            spriteIndex = 0;
        } else if (MoveY == -1){
            spriteIndex = 1;
        } else if (MoveX == 1){
            spriteIndex = 3;
        } else if (MoveX == -1){
            spriteIndex = 2;
        }

        if (spriteIndex < SurferPokemon.SurfSprites.Count && spriteRenderer != null){
            spriteRenderer.sprite = SurferPokemon.SurfSprites[spriteIndex];
        }
    }

    void UpdatePosition(){
        Vector3 offset = Vector3.zero;
        
        if (MoveY == 1){
            offset = new Vector3(0, 0.5f, 0);
        } else if (MoveY == -1){
            offset = new Vector3(0, -0.5f, 0);
        } else if (MoveX == 1){
            offset = new Vector3(0.5f, -0.25f, 0);
        } else if (MoveX == -1){
            offset = new Vector3(-0.5f, -0.25f, 0);
        }
        
        transform.localPosition = offset;
    }

    public void SetSurferPokemon(PokemonBase pokemon){
        SurferPokemon = pokemon;
    }

    public void StartSurfing(){
        IsSurfing = true;
        originalPosition = Vector3.zero;
        wasMoving = false;
        shouldReturnToOriginal = false;
        
        if (spriteRenderer == null){
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        if (spriteRenderer != null){
            spriteRenderer.sortingLayerName = "Objects";
        }
        
        if (SurferPokemon != null && SurferPokemon.SurfSprites != null && SurferPokemon.SurfSprites.Count > 0 && spriteRenderer != null){
            spriteRenderer.sprite = SurferPokemon.SurfSprites[0];
        }
        UpdatePosition();
    }

    public void StopSurfing(){
        IsSurfing = false;
        if (SurferPokemon != null && spriteRenderer != null){
            spriteRenderer.sprite = SurferPokemon.BackSprite;
        }
    }
}
