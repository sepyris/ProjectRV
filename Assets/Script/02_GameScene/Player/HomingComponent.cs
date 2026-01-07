using UnityEngine;

/// <summary>
/// 유도 기능 컴포넌트 (발사체에 추가)
/// </summary>
public class HomingComponent : MonoBehaviour
{
    private MonsterController target;
    private float projectileSpeed;
    private Rigidbody2D rb;

    private const float ROTATION_SPEED = 300f;  // 회전 속도 (초당 각도)

    public void Initialize(MonsterController targetMonster, float speed)
    {
        target = targetMonster;
        projectileSpeed = speed;
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        // 타겟이 없거나 죽었으면 직진
        if (target == null || target.IsDead())
        {
            return;
        }

        // 타겟 방향 계산
        Vector2 direction = (target.transform.position - transform.position).normalized;

        // 현재 속도 방향
        Vector2 currentDirection = rb.velocity.normalized;

        // 부드럽게 회전 (Lerp)
        Vector2 newDirection = Vector2.Lerp(currentDirection, direction, ROTATION_SPEED * Time.fixedDeltaTime / 360f);

        // 속도 적용
        rb.velocity = newDirection.normalized * projectileSpeed;
    }
}