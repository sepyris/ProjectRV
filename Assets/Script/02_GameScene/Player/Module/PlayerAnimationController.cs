using UnityEngine;

public class PlayerAnimationController
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;  // 추가!

    private const string PARAM_SPEED = "Speed";
    private const string PARAM_MOVE_X = "MoveX";
    private const string PARAM_MOVE_Y = "MoveY";
    private const string PARAM_IS_MOVING = "IsMoving";
    private const string PARAM_ATTACK = "Attack";
    private const string PARAM_GATHER = "Gather";
    private const string PARAM_DEATH = "Death";
    private const string PARAM_HIT = "Hit";

    private Vector2 lastMoveDirection = Vector2.down;
    private bool isMoving = false;

    public PlayerAnimationController(Animator animator)
    {
        this.animator = animator;
        // SpriteRenderer 찾기
        if (animator != null)
        {
            this.spriteRenderer = animator.GetComponent<SpriteRenderer>();
        }
    }

    public void UpdateMovementAnimation(Vector2 moveInput, float speed)
    {
        if (animator == null) return;

        bool moving = moveInput.magnitude > 0.01f;

        if (moving)
        {
            lastMoveDirection = moveInput.normalized;
        }

        // Sprite Flip 처리 (오른쪽 방향일 때 좌우 반전)
        if (spriteRenderer != null)
        {
            // X가 양수면 오른쪽을 보고 있음
            if (lastMoveDirection.x > 0.01f)
            {
                spriteRenderer.flipX = true;  // 왼쪽 스프라이트를 뒤집어서 오른쪽으로
                // Animator에는 왼쪽 방향 값을 넣음
                animator.SetFloat(PARAM_MOVE_X, -Mathf.Abs(lastMoveDirection.x));
            }
            else if (lastMoveDirection.x < -0.01f)
            {
                spriteRenderer.flipX = false;  // 원본 (왼쪽)
                animator.SetFloat(PARAM_MOVE_X, lastMoveDirection.x);
            }
            else
            {
                // X가 0이면 (순수 위/아래) 현재 방향 유지
                animator.SetFloat(PARAM_MOVE_X, 0);
            }
        }

        animator.SetFloat(PARAM_SPEED, speed);
        animator.SetBool(PARAM_IS_MOVING, moving);
        animator.SetFloat(PARAM_MOVE_Y, lastMoveDirection.y);

        isMoving = moving;
    }

    public void PlayAttackAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger(PARAM_ATTACK);
        }
    }

    public void PlayGatherAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger(PARAM_GATHER);
        }
    }

    public void PlayDeathAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger(PARAM_DEATH);
            animator.SetBool(PARAM_IS_MOVING, false);
        }
    }

    public void PlayHitAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger(PARAM_HIT);
        }
    }

    public void PlayAnimation(string trigger)
    {
        if (animator != null)
        {
            animator.SetTrigger(trigger);
        }
    }

    public void SetIdle()
    {
        if (animator != null)
        {
            animator.SetBool(PARAM_IS_MOVING, false);
            animator.SetFloat(PARAM_SPEED, 0);
        }
    }

    public void SetAnimationSpeed(float speed)
    {
        if (animator != null)
        {
            animator.speed = speed;
        }
    }

    public bool IsMoving()
    {
        return isMoving;
    }

    public Vector2 GetLastMoveDirection()
    {
        return lastMoveDirection;
    }
}