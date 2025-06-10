using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum FacingDirection {Up, Down, Right, Left}

public class CharacterAnimator : MonoBehaviour{
    [Header("Walking Sprites")]
    [SerializeField] List<Sprite> walkDownSprites;
    [SerializeField] List<Sprite> walkUpSprites;
    [SerializeField] List<Sprite> walkLeftSprites;
    [SerializeField] List<Sprite> walkRightSprites;

    [Header("Running Sprites")]
    [SerializeField] List<Sprite> runDownSprites;
    [SerializeField] List<Sprite> runUpSprites;
    [SerializeField] List<Sprite> runLeftSprites;
    [SerializeField] List<Sprite> runRightSprites;

    [Header("Default Facing Direction")]
    [SerializeField] FacingDirection defaultDirection = FacingDirection.Down;

    [Header("Animation Settings")]
    [SerializeField] float walkFrameRate = 0.1f;
    [SerializeField] float runFrameRate = 0.08f;

    public float MoveX { get; set; }
    public float MoveY { get; set; }
    public bool IsMoving { get; set; }
    public bool IsRunning { get; set; }
    public FacingDirection DefaultDirection => defaultDirection;

    SpriteAnimator walkDownAnim;
    SpriteAnimator walkUpAnim;
    SpriteAnimator walkLeftAnim;
    SpriteAnimator walkRightAnim;

    SpriteAnimator runDownAnim;
    SpriteAnimator runUpAnim;
    SpriteAnimator runLeftAnim;
    SpriteAnimator runRightAnim;

    SpriteAnimator currentAnim;

    bool wasPreviouslyMoving;
    bool wasPreviouslyRunning;

    SpriteRenderer spriteRenderer;

    private void Start(){
        spriteRenderer = GetComponent<SpriteRenderer>();

        walkDownAnim = new SpriteAnimator(walkDownSprites, spriteRenderer, walkFrameRate);
        walkUpAnim = new SpriteAnimator(walkUpSprites, spriteRenderer, walkFrameRate);
        walkLeftAnim = new SpriteAnimator(walkLeftSprites, spriteRenderer, walkFrameRate);
        walkRightAnim = new SpriteAnimator(walkRightSprites, spriteRenderer, walkFrameRate);

        runDownAnim = new SpriteAnimator(runDownSprites, spriteRenderer, runFrameRate);
        runUpAnim = new SpriteAnimator(runUpSprites, spriteRenderer, runFrameRate);
        runLeftAnim = new SpriteAnimator(runLeftSprites, spriteRenderer, runFrameRate);
        runRightAnim = new SpriteAnimator(runRightSprites, spriteRenderer, runFrameRate);

        SetFacingDirection(defaultDirection);
        currentAnim = walkDownAnim;
    }

    private void Update(){
        var prevAnim = currentAnim;

        if(!IsMoving){
            MoveX = 0f;
            MoveY = 0f;
        }

        if(MoveX == 1){
            currentAnim = IsRunning ? runRightAnim : walkRightAnim;
        } else if(MoveX == -1){
            currentAnim = IsRunning ? runLeftAnim : walkLeftAnim;
        } else if(MoveY == 1){
            currentAnim = IsRunning ? runUpAnim : walkUpAnim;
        } else if(MoveY == -1){
            currentAnim = IsRunning ? runDownAnim : walkDownAnim;
        }

        if(currentAnim != prevAnim || IsMoving != wasPreviouslyMoving || IsRunning != wasPreviouslyRunning){
            currentAnim.Start();
        }

        if(IsMoving){
            currentAnim.HandleUpdate();
        } else {
            spriteRenderer.sprite = currentAnim.Frames[0];
        }
        
        wasPreviouslyMoving = IsMoving;
        wasPreviouslyRunning = IsRunning;
    }

    public void SetFacingDirection(FacingDirection dir){
        if(dir == FacingDirection.Right){
            MoveX = 1;
        } else if(dir == FacingDirection.Left){
            MoveX = -1;
        } else if(dir == FacingDirection.Down){
            MoveY = -1;
        } else if(dir == FacingDirection.Up){
            MoveY = 1;
        }
    }
}