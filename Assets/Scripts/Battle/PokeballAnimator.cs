using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PokeballAnimator : MonoBehaviour{
    SpriteRenderer spriteRenderer;
    SpriteAnimator spriteAnimator;

    void Awake(){
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteAnimator = new SpriteAnimator(null, spriteRenderer);
    }

    void Update(){
        spriteAnimator?.HandleUpdate();
    }

    public void PlayThrow(PokeballItem ball){
        spriteAnimator.Start(ball.ThrowFrames, 0.08f, false);
    }

    public void PlayIdle(PokeballItem ball, float normalizedStart = 0f){
        var idleFrames = ball.IdleFrames;
        if (idleFrames == null || idleFrames.Count == 0){
            return;
        }

        int total = idleFrames.Count;
        int startIndex = Mathf.FloorToInt(normalizedStart * total);
        startIndex = Mathf.Clamp(startIndex, 0, total - 1);

        List<Sprite> slice = idleFrames.GetRange(startIndex, total - startIndex);
        if (slice.Count == 0){
            slice = idleFrames;        
        }

        spriteAnimator.Start(slice, 0.15f, true);
    }

    public void PlayShake(PokeballItem ball){
        spriteAnimator.Start(ball.ShakeFrames, 0.1f, false);
    }

    public void PlayCatch(PokeballItem ball){
        spriteAnimator.Start(ball.CatchFrames, 0.1f, false);
    }

}
