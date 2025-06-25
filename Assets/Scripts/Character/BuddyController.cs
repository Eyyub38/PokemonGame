using UnityEngine;
using System.Collections;

public class BuddyController : MonoBehaviour{
    [Header("Following Settings")]
    [SerializeField] private float maxDistance = 1f;
    [SerializeField] private float minDistance = 0.5f;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float catchUpSpeed = 8f;
    
    private CharacterAnimator animator;
    private PlayerController player;
    private bool isMoving;
    private float moveSpeed;
    private Vector3 targetPosition;
    private bool isFollowing;

    public void Follow(Vector3 movePosition){
        if(player == null || animator == null){
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        
        if(distanceToPlayer > maxDistance){
            isFollowing = true;
            targetPosition = player.transform.position;
            moveSpeed = player.Character.movingSpeed * catchUpSpeed;
        } else if(distanceToPlayer > minDistance && !isMoving){
            isFollowing = true;
            targetPosition = movePosition;
            moveSpeed = player.Character.movingSpeed;
        } else if(distanceToPlayer <= minDistance){
            isFollowing = false;
            animator.IsMoving = false;
            return;
        }

        if(isFollowing && !isMoving){
            Vector2 moveVector = targetPosition - transform.position;
            StartCoroutine(Move(moveVector));
        }
    }

    public void SetPosition(){
        if(player == null || animator == null){
            return;
        }
        
        this.transform.position = player.transform.position;
        this.animator.IsMoving = false;
        isMoving = false;
        isFollowing = false;
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

        isMoving = true;
        animator.IsMoving = true;

        while((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon){
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPos;

        isMoving = false;
        animator.IsMoving = false;
    }

    private void Update(){
        if(player != null && animator != null && !isMoving){
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            
            if(distanceToPlayer > maxDistance){
                Vector3 direction = (player.transform.position - transform.position).normalized;
                transform.position = Vector3.Lerp(transform.position, player.transform.position, smoothSpeed * Time.deltaTime);
                animator.IsMoving = true;
            } else if(distanceToPlayer <= minDistance){
                animator.IsMoving = false;
            }
        }
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
        
        SetPosition();
    }
}