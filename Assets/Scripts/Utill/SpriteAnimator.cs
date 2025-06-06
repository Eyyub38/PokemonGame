using UnityEngine;
using System.Collections.Generic;

public class SpriteAnimator{
    SpriteRenderer spriteRenderer;
    List<Sprite> frames;

    int currentFrame;
    float frameRate;
    float timer;
    bool isLooping = true;

    public bool IsDone { get; private set; } = false;
    public List<Sprite> Frames { get { return frames; } }

    public SpriteAnimator(List<Sprite> frames, SpriteRenderer spriteRenderer, float frameRate = 0.1f, bool isLooping = true){
        this.spriteRenderer = spriteRenderer;
        Start(frames, frameRate, isLooping);
    }

    public void Start(){
        currentFrame = 0;
        timer = 0f;
        IsDone = false;
        if (frames != null && frames.Count > 0)
            spriteRenderer.sprite = frames[0];
    }

    public void Start(List<Sprite> newFrames, float frameRate = 0.1f, bool isLooping = true){
        this.frames = newFrames;
        this.frameRate = frameRate;
        this.isLooping = isLooping;
        Start();
    }

    public void HandleUpdate(){
        if (frames == null || frames.Count == 0 || IsDone){
            return;
        }

        timer += Time.deltaTime;
        if (timer > frameRate){
            timer -= frameRate;
            currentFrame++;

            if (currentFrame >= frames.Count){
                if (isLooping){
                    currentFrame = 0;
                } else {
                    currentFrame = frames.Count - 1;
                    IsDone = true;
                }
            }

            spriteRenderer.sprite = frames[currentFrame];
        }
    }
}
