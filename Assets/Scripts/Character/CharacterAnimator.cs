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

    [Header("Jumping Sprites")]
    [SerializeField] List<Sprite> jumpDownSprites;
    [SerializeField] List<Sprite> jumpUpSprites;
    [SerializeField] List<Sprite> jumpLeftSprites;
    [SerializeField] List<Sprite> jumpRightSprites;

    [Header("Surfing Sprites")]
    [SerializeField] List<Sprite> surfSprites;

    [Header("Default Facing Direction")]
    [SerializeField] FacingDirection defaultDirection = FacingDirection.Down;


    public float MoveX { get; set; }
    public float MoveY { get; set; }
    public bool IsMoving { get; set; }
    public bool IsRunning { get; set; }
    public bool IsJumping { get; set; }
    public bool IsSurfing { get; set; }
    public FacingDirection DefaultDirection => defaultDirection;
    public float LastMoveX => lastMoveX;
    public float LastMoveY => lastMoveY;

    SpriteAnimator walkDownAnim;
    SpriteAnimator walkUpAnim;
    SpriteAnimator walkLeftAnim;
    SpriteAnimator walkRightAnim;

    SpriteAnimator runDownAnim;
    SpriteAnimator runUpAnim;
    SpriteAnimator runLeftAnim;
    SpriteAnimator runRightAnim;

    SpriteAnimator jumpDownAnim;
    SpriteAnimator jumpUpAnim;
    SpriteAnimator jumpLeftAnim;
    SpriteAnimator jumpRightAnim;

    SpriteAnimator currentAnim;

    bool wasPreviouslyMoving;
    bool wasPreviouslyRunning;
    bool wasPreviouslyJumping;

    float lastMoveX;
    float lastMoveY;

    SpriteRenderer spriteRenderer;

    private void Start(){
        spriteRenderer = GetComponent<SpriteRenderer>();

        walkDownAnim = new SpriteAnimator(walkDownSprites, spriteRenderer, 0.2f);
        walkUpAnim = new SpriteAnimator(walkUpSprites, spriteRenderer, 0.2f);
        walkLeftAnim = new SpriteAnimator(walkLeftSprites, spriteRenderer, 0.2f);
        walkRightAnim = new SpriteAnimator(walkRightSprites, spriteRenderer, 0.2f);

        runDownAnim = new SpriteAnimator(runDownSprites, spriteRenderer, 0.15f);
        runUpAnim = new SpriteAnimator(runUpSprites, spriteRenderer, 0.15f);
        runLeftAnim = new SpriteAnimator(runLeftSprites, spriteRenderer, 0.15f);
        runRightAnim = new SpriteAnimator(runRightSprites, spriteRenderer, 0.15f);

        jumpDownAnim = new SpriteAnimator(jumpDownSprites, spriteRenderer, 0.1f);
        jumpUpAnim = new SpriteAnimator(jumpUpSprites, spriteRenderer, 0.1f);
        jumpLeftAnim = new SpriteAnimator(jumpLeftSprites, spriteRenderer, 0.1f);
        jumpRightAnim = new SpriteAnimator(jumpRightSprites, spriteRenderer, 0.1f);

        SetFacingDirection(defaultDirection);
        currentAnim = walkDownAnim;
    }

    private void Update(){
        var prevAnim = currentAnim;

        if(IsMoving){
            lastMoveX = MoveX;
            lastMoveY = MoveY;
        }
        if(!IsSurfing){
            if(IsJumping){
                if(lastMoveX == 1){
                    currentAnim = jumpRightAnim;
                } else if(lastMoveX == -1){
                    currentAnim = jumpLeftAnim;
                } else if(lastMoveY == 1){
                    currentAnim = jumpUpAnim;
                } else if(lastMoveY == -1){
                    currentAnim = jumpDownAnim;
                }
            } else {
                if(MoveX == 1){
                    currentAnim = IsRunning ? runRightAnim : walkRightAnim;
                } else if(MoveX == -1){
                    currentAnim = IsRunning ? runLeftAnim : walkLeftAnim;
                } else if(MoveY == 1){
                    currentAnim = IsRunning ? runUpAnim : walkUpAnim;
                } else if(MoveY == -1){
                    currentAnim = IsRunning ? runDownAnim : walkDownAnim;
                }
            }
            if(currentAnim != prevAnim || IsMoving != wasPreviouslyMoving || IsRunning != wasPreviouslyRunning || IsJumping != wasPreviouslyJumping){
                currentAnim.Start();
            }

            if(IsJumping){
                currentAnim.HandleUpdate();
            } else if(IsMoving){
                currentAnim.HandleUpdate();
            } else {
                spriteRenderer.sprite = currentAnim.Frames[0];
            }
        } else {
            if(MoveY == 1){
                spriteRenderer.sprite = surfSprites[0];
            } else if(MoveY == -1){
                spriteRenderer.sprite = surfSprites[1];
            } else if(MoveX == 1){
                spriteRenderer.sprite = surfSprites[3];
            } else if(MoveX == -1){
                spriteRenderer.sprite = surfSprites[2];
            }
        }
        
        wasPreviouslyMoving = IsMoving;
        wasPreviouslyRunning = IsRunning;
        wasPreviouslyJumping = IsJumping;
    }

    public void SetFacingDirection(FacingDirection dir){
        MoveX = 0;
        MoveY = 0;

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

    public void SetFacingDirection(float x, float y){
        MoveX = Mathf.Clamp(x, -1f, 1f);
        MoveY = Mathf.Clamp(y, -1f, 1f);
    }
}