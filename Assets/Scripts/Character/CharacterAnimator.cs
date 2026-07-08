using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum FacingDirection {Up, Down, Right, Left}
public enum CharacterAnimationState { Idle, Walk, Run, Jump, Surf }

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
    public FacingDirection CurrentFacingDirection { get; private set; }
    public CharacterAnimationState CurrentAnimationState { get; private set; } = CharacterAnimationState.Idle;
    public int CurrentFrameIndex => CurrentAnimationState == CharacterAnimationState.Idle ? 0 : currentAnim != null ? currentAnim.CurrentFrameIndex : 0;

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

    void Awake(){
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start(){
        InitializeAnimations();
        SetFacingDirection(defaultDirection);
        currentAnim = walkDownAnim;
    }

    void InitializeAnimations(){
        if(spriteRenderer == null) {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

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
    }

    private void Update(){
        var prevAnim = currentAnim;

        if(IsMoving){
            lastMoveX = MoveX;
            lastMoveY = MoveY;
        }

        CurrentFacingDirection = ResolveFacingDirection(IsMoving ? MoveX : lastMoveX, IsMoving ? MoveY : lastMoveY);

        if(!IsSurfing){
            if(!IsMoving){
                CurrentAnimationState = CharacterAnimationState.Idle;
                // Set idle animation based on last movement direction
                if(lastMoveX == 1){
                    currentAnim = walkRightAnim;
                } else if(lastMoveX == -1){
                    currentAnim = walkLeftAnim;
                } else if(lastMoveY == 1){
                    currentAnim = walkUpAnim;
                } else if(lastMoveY == -1){
                    currentAnim = walkDownAnim;
                }
            } else if(IsJumping){
                CurrentAnimationState = CharacterAnimationState.Jump;
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
                CurrentAnimationState = IsRunning ? CharacterAnimationState.Run : CharacterAnimationState.Walk;
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
                if (currentAnim.Frames != null && currentAnim.Frames.Count > 0)
                    spriteRenderer.sprite = currentAnim.Frames[0];
            }
        } else {
            CurrentAnimationState = CharacterAnimationState.Surf;
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

        lastMoveX = MoveX;
        lastMoveY = MoveY;
        CurrentFacingDirection = dir;
    }

    public void SetFacingDirection(float x, float y){
        MoveX = Mathf.Clamp(x, -1f, 1f);
        MoveY = Mathf.Clamp(y, -1f, 1f);
        lastMoveX = MoveX;
        lastMoveY = MoveY;
        CurrentFacingDirection = ResolveFacingDirection(lastMoveX, lastMoveY);
    }

    public void ApplyVisualSet(NPCVisualSetDefinition visualSet){
        if(visualSet == null) {
            return;
        }

        walkDownSprites = ToSpriteList(visualSet.WalkDownSprites);
        walkUpSprites = ToSpriteList(visualSet.WalkUpSprites);
        walkLeftSprites = ToSpriteList(visualSet.WalkLeftSprites);
        walkRightSprites = ToSpriteList(visualSet.WalkRightSprites);
        runDownSprites = visualSet.GetRunDownOrWalk();
        runUpSprites = visualSet.GetRunUpOrWalk();
        runLeftSprites = visualSet.GetRunLeftOrWalk();
        runRightSprites = visualSet.GetRunRightOrWalk();
        jumpDownSprites = visualSet.GetJumpDownOrWalk();
        jumpUpSprites = visualSet.GetJumpUpOrWalk();
        jumpLeftSprites = visualSet.GetJumpLeftOrWalk();
        jumpRightSprites = visualSet.GetJumpRightOrWalk();
        surfSprites = ToSpriteList(visualSet.SurfSprites);

        InitializeAnimations();
        SetFacingDirection(defaultDirection);
        currentAnim = GetIdleAnimation();
        if(currentAnim?.Frames != null && currentAnim.Frames.Count > 0 && spriteRenderer != null) {
            spriteRenderer.sprite = currentAnim.Frames[0];
        }
    }

    FacingDirection ResolveFacingDirection(float x, float y){
        if(x > 0f) return FacingDirection.Right;
        if(x < 0f) return FacingDirection.Left;
        if(y > 0f) return FacingDirection.Up;
        if(y < 0f) return FacingDirection.Down;
        return defaultDirection;
    }

    SpriteAnimator GetIdleAnimation(){
        if(lastMoveX == 1) return walkRightAnim;
        if(lastMoveX == -1) return walkLeftAnim;
        if(lastMoveY == 1) return walkUpAnim;
        return walkDownAnim;
    }

    List<Sprite> ToSpriteList(IReadOnlyList<Sprite> sprites){
        return sprites != null ? new List<Sprite>(sprites) : new List<Sprite>();
    }
}
