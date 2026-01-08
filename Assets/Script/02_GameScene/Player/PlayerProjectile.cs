using UnityEngine;


/// 플레이어의 원거리 공격 발사체
/// 몬스터와 충돌 시 데미지를 주고 자동으로 파괴됨
/// 거리 기반으로 자동 파괴 가능

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerProjectile : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int baseDamage = 10;
    [SerializeField] private float criticalChance = 5f;
    [SerializeField] private float criticalDamage = 150f;
    [SerializeField] private float accuracy = 100f;

    [Header("Collision Settings")]
    [SerializeField] private bool destroyOnHit = true;
    [SerializeField] private bool penetrateEnemies = false;  // 관통 기능
    [SerializeField] private int maxPenetrationCount = 1;    // 관통 가능 횟수
    [SerializeField] private bool ignoreWalls = false;       // 벽 무시 (스킬용, 평타는 false)

    [Header("Lifetime Settings")]
    [SerializeField] private float maxDistance = 2f;        // 최대 이동 거리 (외부에서 설정)

    [Header("Visual Effects")]
    [SerializeField] private GameObject hitEffectPrefab;  // 충돌 시 이펙트
    [SerializeField] private TrailRenderer trailRenderer; // 발사체 궤적

    private int penetrationCount = 0;
    private Rigidbody2D rb;
    private Vector3 startPosition;
    private float traveledDistance = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Rigidbody2D 기본 설정
        if (rb != null)
        {
            rb.gravityScale = 0f;  // 중력 무시
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;  // 빠른 발사체를 위한 연속 충돌 감지
        }

        // Collider가 Trigger인지 확인
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;  // Trigger로 설정하여 OnTriggerEnter2D 사용
        }

        // 시작 위치 저장
        startPosition = transform.position;
    }

    void Update()
    {
        // 발사체가 날아가는 방향으로 회전 (선택적)
        RotateTowardsVelocity();

        // 거리 기반 파괴 체크
        CheckDistanceDestroy();
    }

    
    /// 거리 기반 파괴 체크
    
    private void CheckDistanceDestroy()
    {
        // 현재까지 이동한 거리 계산
        traveledDistance = Vector3.Distance(startPosition, transform.position);

        // 최대 거리 초과 시 파괴
        if (traveledDistance >= maxDistance)
        {
            Debug.Log($"[PlayerProjectile] 최대 거리 도달 ({traveledDistance:F2}m), 발사체 파괴");
            DestroyProjectile();
        }
    }

    
    /// 발사체 초기화 (PlayerAttack에서 호출)
    
    public void Initialize(int damage, float crit, float critDmg, float acc, float distance)
    {
        baseDamage = damage;
        criticalChance = crit;
        criticalDamage = critDmg;
        accuracy = acc;
        maxDistance = distance;
    }

    
    /// 관통 설정 (선택적)
    
    public void SetPenetration(bool enable, int maxCount = 1)
    {
        penetrateEnemies = enable;
        maxPenetrationCount = maxCount;
        destroyOnHit = !enable;  // 관통이면 즉시 파괴 안함
    }
    public void SetIgnoreWalls(bool ignore)
    {
        ignoreWalls = ignore;
    }

    /// Trigger 충돌 감지

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 몬스터 레이어인지 확인
        if (collision.gameObject.layer == LayerMask.NameToLayer("Monster"))
        {
            MonsterController monster = collision.GetComponent<MonsterController>();
            if (monster != null)
            {
                // 데미지 계산
                bool is_critical = false;
                int finalDamage = CalculateDamage(ref is_critical);

                // 몬스터에게 데미지 적용
                int actualDamage = monster.TakeDamage(finalDamage, is_critical, accuracy);

                if (actualDamage > 0)
                {
                    Debug.Log($"[PlayerProjectile] {monster.GetMonsterName()}에게 {actualDamage} 데미지! (이동 거리: {traveledDistance:F2}m)");

                    // 충돌 이펙트 생성
                    SpawnHitEffect(collision.transform.position);
                }

                // 관통 처리
                if (penetrateEnemies)
                {
                    penetrationCount++;
                    if (penetrationCount >= maxPenetrationCount)
                    {
                        DestroyProjectile();
                    }
                }
                else if (destroyOnHit)
                {
                    DestroyProjectile();
                }
            }
        }
        // 벽이나 장애물과 충돌
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Wall") ||
                 collision.gameObject.layer == LayerMask.NameToLayer("BlockingObject"))
        {
            // 벽 무시 옵션 체크
            if (ignoreWalls)
            {
                // 스킬 발사체는 벽을 통과
                Debug.Log($"[PlayerProjectile] 벽 통과 (스킬)");
                return;
            }

            // 평타 발사체는 벽에 막힘
            Debug.Log($"[PlayerProjectile] 벽에 충돌! (이동 거리: {traveledDistance:F2}m)");
            SpawnHitEffect(collision.transform.position);
            DestroyProjectile();
        }
    }



    /// Collision 충돌 감지 (Trigger가 아닌 경우)

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Trigger 방식을 권장하지만, Collision 방식도 지원
        if (collision.gameObject.layer == LayerMask.NameToLayer("Monster"))
        {
            MonsterController monster = collision.gameObject.GetComponent<MonsterController>();
            if (monster != null)
            {
                bool is_critical = false;
                int finalDamage = CalculateDamage(ref is_critical);
                int actualDamage = monster.TakeDamage(finalDamage, is_critical, accuracy);

                if (actualDamage > 0)
                {
                    Debug.Log($"[PlayerProjectile] {monster.GetMonsterName()}에게 {actualDamage} 데미지! (이동 거리: {traveledDistance:F2}m)");
                    SpawnHitEffect(collision.contacts[0].point);
                }

                if (destroyOnHit)
                {
                    DestroyProjectile();
                }
            }
        }
        else
        {
            // 벽 무시 옵션 체크
            if (ignoreWalls)
            {
                Debug.Log($"[PlayerProjectile] 벽 통과 (스킬)");
                return;
            }

            // 다른 물체와 충돌 시 파괴
            SpawnHitEffect(collision.contacts[0].point);
            DestroyProjectile();
        }
    }


    /// 데미지 계산 (크리티컬 포함)

    private int CalculateDamage(ref bool is_critical)
    {
        float damageVariance = Random.Range(0.8f, 1.2f); // 80% ~ 120%
        int damage = Mathf.RoundToInt(baseDamage * damageVariance);

        // 크리티컬 확률 체크
        if (Random.Range(0f, 100f) <= criticalChance)
        {
            damage = Mathf.RoundToInt(damage * (criticalDamage / 100f));
            Debug.Log($"[PlayerProjectile] 크리티컬! {damage} 데미지");
            is_critical = true;
        }

        return damage;
    }

    
    /// 충돌 이펙트 생성
    
    private void SpawnHitEffect(Vector3 position)
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 1f);  // 1초 후 이펙트 제거
        }
    }

    
    /// 발사체 파괴
    
    private void DestroyProjectile()
    {
        // Trail Renderer 처리
        if (trailRenderer != null)
        {
            trailRenderer.transform.SetParent(null);  // 부모에서 분리
            Destroy(trailRenderer.gameObject, trailRenderer.time);  // Trail 시간만큼 유지 후 삭제
        }

        Destroy(gameObject);
    }

    
    /// 발사체 속도 설정 (외부에서 호출)
    
    public void SetVelocity(Vector2 velocity)
    {
        if (rb != null)
        {
            rb.velocity = velocity;
        }
    }

    
    /// 발사체 방향으로 회전 (선택적)
    
    public void RotateTowardsVelocity()
    {
        if (rb != null && rb.velocity != Vector2.zero)
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    
    /// 현재 이동 거리 가져오기 (디버깅용)
    
    public float GetTraveledDistance()
    {
        return traveledDistance;
    }

    
    /// 남은 거리 가져오기 (디버깅용)
    
    public float GetRemainingDistance()
    {
        return maxDistance - traveledDistance;
    }
}