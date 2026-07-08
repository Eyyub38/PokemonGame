using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PokemonAnimator : MonoBehaviour{
    SpriteRenderer spriteRenderer;
    SpriteAnimator spriteAnimator;

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
        if (IsSurfing && SurferPokemon != null && SurferPokemon.SurfSprites != null && SurferPokemon.SurfSprites.Count >= 4){
            HandleSurfMovement();
        } else {
            spriteAnimator?.HandleUpdate();
        }
    }

    void HandleSurfMovement(){
        bool isCurrentlyMoving = Mathf.Abs(MoveX) > 0.2f || Mathf.Abs(MoveY) > 0.2f;
        
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
        if (SurferPokemon?.SurfSprites == null || SurferPokemon.SurfSprites.Count < 4) return;

        int spriteIndex = 0;
        
        if (MoveY > 0.2f){
            spriteIndex = 0;
        } else if (MoveY < -0.2f){
            spriteIndex = 1;
        } else if (MoveX > 0.2f){
            spriteIndex = 3;
        } else if (MoveX < -0.2f){
            spriteIndex = 2;
        }

        if (spriteIndex < SurferPokemon.SurfSprites.Count && spriteRenderer != null){
            spriteRenderer.sprite = SurferPokemon.SurfSprites[spriteIndex];
        }
    }

    void UpdatePosition(){
        Vector3 offset = Vector3.zero;
        
        if (MoveY > 0.2f){
            offset = new Vector3(0, 0.5f, 0);
        } else if (MoveY < -0.2f){
            offset = new Vector3(0, -0.5f, 0);
        } else if (MoveX > 0.2f){
            offset = new Vector3(0.5f, -0.25f, 0);
        } else if (MoveX < -0.2f){
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
