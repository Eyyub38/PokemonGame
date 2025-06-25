using UnityEngine;
using System.Collections;

public class BuddyController : MonoBehaviour{
    [Header("Following Settings")]
    [SerializeField] private float maxDistance = 1f;
    [SerializeField] private float minDistance = 0.5f;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float behindOffset = 0.5f; // Distance behind player when stopped
    
    private CharacterAnimator animator;
    private PlayerController player;
    private bool isMoving;
    private bool isJumping;
    private float moveSpeed;
    private Vector3 targetPosition;
    private bool isFollowing;
    private bool wasPlayerRunning;
    private bool wasPlayerMoving;
    private Vector3 lastPlayerPosition;

    public void OnPlayerRunningChanged(bool isRunning){
        if(animator != null){
            animator.IsRunning = isRunning;
            wasPlayerRunning = isRunning;
        }
    }

    public void Follow(Vector3 movePosition){
        if(player == null || animator == null){
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        bool playerRunning = player.Character.IsRunning;
        bool playerMoving = player.Character.IsMoving;
        
        if(playerMoving){
            lastPlayerPosition = player.transform.position;
        }
        
        UpdateFacingDirection();
        
        if(distanceToPlayer > maxDistance){
            isFollowing = true;
            targetPosition = player.transform.position;
            moveSpeed = player.Character.movingSpeed;
            if(playerRunning){
                moveSpeed *= player.Character.runningSpeed;
                animator.IsRunning = true;
            }
        } else if(distanceToPlayer > minDistance && !isMoving && !isJumping){
            isFollowing = true;
            targetPosition = movePosition;
            moveSpeed = player.Character.movingSpeed;
            
            if(playerRunning != wasPlayerRunning){
                animator.IsRunning = playerRunning;
                wasPlayerRunning = playerRunning;
            }
            
            if(playerRunning){
                moveSpeed *= player.Character.runningSpeed;
            }
        } else if(distanceToPlayer <= minDistance){
            isFollowing = false;
            StopMoving();
            return;
        }

        if(isFollowing && !isMoving && !isJumping){
            Vector2 moveVector = targetPosition - transform.position;
            StartCoroutine(Move(moveVector));
        }
    }

    public void OnPlayerJump(Vector3 jumpTarget){
        if(!isJumping && !isMoving){
            StartCoroutine(Jump(jumpTarget));
        }
    }

    public IEnumerator Move(Vector2 moveVec){
        if(animator == null){
            yield break;
        }
        
        animator.MoveX = Mathf.Clamp(moveVec.x, -1f, 1f);
        animator.MoveY = Mathf.Clamp(moveVec.y, -1f, 1f);

        var targetPos = transform.position;
        targetPos.x += moveVec.x;
        targetPos.y += moveVec.y;

        if(!IsPathClear(targetPos)){
            StopMoving();
            yield break;
        }

        isMoving = true;
        animator.IsMoving = true;

        float startTime = Time.time;
        Vector3 startPos = transform.position;
        float maxMoveTime = Vector3.Distance(startPos, targetPos) / moveSpeed + 0.5f;
        
        while((targetPos - transform.position).sqrMagnitude > 0.01f){
            float elapsed = Time.time - startTime;
            
            if(elapsed > maxMoveTime){
                Debug.LogWarning("Buddy move timeout, forcing position");
                break;
            }
            
            Vector3 oldPos = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            
            if(Vector3.Distance(oldPos, transform.position) < 0.001f){
                Debug.LogWarning("Buddy stuck, breaking movement");
                break;
            }
            
            yield return null;
        }

        transform.position = targetPos;
        isMoving = false;
        
        animator.IsMoving = false;
        animator.IsRunning = false;
    }

    private void StopMoving(){
        animator.IsMoving = false;
        animator.IsRunning = false;
    }

    public void SetPosition(){
        if(player == null || animator == null){
            return;
        }
        
        this.transform.position = player.transform.position;
        StopMoving();
        isMoving = false;
        isJumping = false;
        isFollowing = false;
        
        UpdateFacingDirection();
    }

    private bool IsPathClear(Vector3 targetPos){
        var diff = targetPos - transform.position;
        var dir = diff.normalized;

        var collisionLayer = GameLayers.i.SolidObjectsLayer | GameLayers.i.InteractableLayer | GameLayers.i.PlayerLayer;
        
        if(Physics2D.BoxCast(transform.position + dir, new Vector2(0.2f, 0.2f), 0f, dir, diff.magnitude - 1, collisionLayer)){
            return false;
        }
        return true;
    }

    private IEnumerator Jump(Vector3 jumpTarget){
        if(isJumping){
            yield break;
        }
        
        isJumping = true;
        animator.IsMoving = true;
        animator.IsJumping = true;
        
        Vector3 jumpDirection = (jumpTarget - transform.position).normalized;
        animator.MoveX = jumpDirection.x;
        animator.MoveY = jumpDirection.y;
        
        Vector3 startPos = transform.position;
        float startTime = Time.time;
        
        float jumpDuration = 0.5f;
        float jumpHeight = 0.3f; 
        
        while(Time.time - startTime < jumpDuration){
            float progress = (Time.time - startTime) / jumpDuration;
            float height = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
            
            Vector3 currentPos = Vector3.Lerp(startPos, jumpTarget, progress);
            currentPos.y += height;
            
            transform.position = currentPos;
            yield return null;
        }
        
        transform.position = jumpTarget;
        isJumping = false;
        animator.IsJumping = false;
        
        UpdateFacingDirection();
        animator.IsMoving = false;
    }

    private void Update(){
        if(player != null && animator != null && !isMoving && !isJumping){
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            bool playerMoving = player.Character.IsMoving;
            
            UpdateFacingDirection();
            
            if(playerMoving){
                if(distanceToPlayer > maxDistance){
                    Vector3 direction = (player.transform.position - transform.position).normalized;
                    transform.position = Vector3.Lerp(transform.position, player.transform.position, smoothSpeed * Time.deltaTime);
                    animator.IsMoving = true;
                    
                    if(player.Character.IsRunning){
                        animator.IsRunning = true;
                    }
                } else if(distanceToPlayer <= minDistance){
                    StopMoving();
                }
            } else {
                if(distanceToPlayer > 0.1f){
                    PositionBehindPlayer();
                }
            }
        } else if(player != null && animator != null) {
            UpdateFacingDirection();
        }
    }

    private void PositionBehindPlayer(){
        if(player == null || animator == null) return;
        
        Vector3 playerFacingDir = GetPlayerFacingDirection();
        
        Vector3 behindPosition = player.transform.position - (playerFacingDir * behindOffset);
        
        transform.position = Vector3.Lerp(transform.position, behindPosition, smoothSpeed * Time.deltaTime);
        
        animator.IsMoving = false;
        animator.IsRunning = false;
    }

    private Vector3 GetPlayerFacingDirection(){
        if(player == null || player.Character == null || player.Character.Animator == null) 
            return Vector3.down;
        
        var playerAnimator = player.Character.Animator;
        
        if(playerAnimator.MoveX != 0 || playerAnimator.MoveY != 0){
            return new Vector3(playerAnimator.MoveX, playerAnimator.MoveY, 0).normalized;
        }
        
        if(playerAnimator.LastMoveX != 0 || playerAnimator.LastMoveY != 0){
            return new Vector3(playerAnimator.LastMoveX, playerAnimator.LastMoveY, 0).normalized;
        }
        
        switch(playerAnimator.DefaultDirection){
            case FacingDirection.Up: return Vector3.up;
            case FacingDirection.Down: return Vector3.down;
            case FacingDirection.Left: return Vector3.left;
            case FacingDirection.Right: return Vector3.right;
            default: return Vector3.down;
        }
    }

    private void UpdateFacingDirection(){
        if(player == null || animator == null) return;
        
        Vector3 playerFacingDir = GetPlayerFacingDirection();
        
        animator.MoveX = playerFacingDir.x;
        animator.MoveY = playerFacingDir.y;
    }

    private void Start(){
        player = PlayerController.i;
        animator = GetComponent<CharacterAnimator>();
        
        if(player == null){
            Debug.LogError("BuddyController: PlayerController singleton not found!");
            return;
        }
        
        if(animator == null){
            Debug.LogError("BuddyController: CharacterAnimator component not found!");
            return;
        }
        
        player.Buddy = this;
        lastPlayerPosition = player.transform.position;
        
        SetPosition();
    }
}