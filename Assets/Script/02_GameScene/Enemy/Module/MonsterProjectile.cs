using UnityEngine;


/// 몬스터의 원거리 공격 발사체

public class MonsterProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private int damage;
    private bool isCritical;
    private float accuracy;
    private bool isInitialized = false;

    [Header("발사체 설정")]
    [SerializeField] private float lifetime = 1.5f; // 자동 파괴 시간
    [SerializeField] private bool destroyOnHit = true; // 충돌 시 파괴 여부

    
    /// 발사체 초기화
    
    public void Initialize(Vector2 dir, float spd, int dmg, bool critical, float acc)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        isCritical = critical;
        accuracy = acc;
        isInitialized = true;

        // Rigidbody2D가 있으면 속도 설정
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = direction * speed;
        }

        // 방향에 맞게 회전 (선택사항)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 일정 시간 후 자동 파괴
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Rigidbody2D가 없으면 직접 이동
        if (isInitialized && GetComponent<Rigidbody2D>() == null)
        {
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어와 충돌 체크
        if (other.CompareTag(Definitions.Def_Name.PLAYER_TAG))
        {
            var playerStats = other.GetComponent<PlayerStatsComponent>();
            if (playerStats != null)
            {
                int takenDamage = playerStats.Stats.TakeDamage(damage, isCritical, accuracy);

                if (takenDamage > 0)
                {
                    Debug.Log($"[MonsterProjectile] 플레이어 명중! 데미지: {takenDamage}");
                }
                else
                {
                    Debug.Log($"[MonsterProjectile] 플레이어 회피!");
                }
            }

            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }
        // 벽과 충돌 시 파괴 (선택사항)
        else if (other.gameObject.layer == LayerMask.NameToLayer("Wall") || other.gameObject.layer == LayerMask.NameToLayer("BlockingObject"))
        {
            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }
    }
}