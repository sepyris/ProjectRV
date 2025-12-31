using UnityEngine;

public class MonsterCombat
{
    private Transform transform;
    private MonsterController controller;

    [Header("공격 설정")]
    public float attackRange = 1.5f;
    public float attackCooldown = 2f;
    public float preferredAttackDistance = 1.0f;

    [Header("원거리 공격 설정")]
    public bool canRangedAttack = false;  // 원거리 공격 가능 여부
    public float rangedAttackRange = 5f;  // 원거리 공격 사거리
    public GameObject projectilePrefab;   // 발사체 프리팹
    public float projectileSpeed = 10f;   // 발사체 속도

    private float lastAttackTime = -999f;

    public MonsterCombat(Transform transform, MonsterController controller)
    {
        this.transform = transform;
        this.controller = controller;
    }

    public bool CanAttack()
    {
        return Time.time - lastAttackTime >= attackCooldown;
    }

    public bool TryAttackPlayer(Transform playerTransform)
    {
        if (playerTransform == null || !CanAttack()) return false;

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if(canRangedAttack)
        {
            //원거리 공격일시 원거리 사거리 체크
            if (distance <= rangedAttackRange)
            {
                PerformRangedAttack(playerTransform);
                return true;
            }
        }
        else
        {
            //근접 공격일시 근접 사거리 체크
            if (distance <= attackRange)
            {
                PerformMeleeAttack(playerTransform);
                return true;
            }
        }
        return false;
    }

    
    /// 근접 공격
    
    private void PerformMeleeAttack(Transform target)
    {
        lastAttackTime = Time.time;

        if (controller == null)
        {
            Debug.LogWarning("[MonsterCombat] MonsterController가 없습니다!");
            return;
        }

        var playerStats = target.GetComponent<PlayerStatsComponent>();
        if (playerStats != null)
        {
            bool is_critical = false;
            int damage = controller.GetAttackPower(ref is_critical);
            float monsterAccuracy = controller.GetAccuracy();

            int takenDamage = playerStats.Stats.TakeDamage(damage, is_critical, monsterAccuracy);

            if (takenDamage > 0)
            {
                Debug.Log($"[Monster] 근접 공격 성공! 데미지: {takenDamage}");
            }
            else
            {
                Debug.Log($"[Monster] 플레이어 회피!");
            }
        }
    }

    
    /// 원거리 공격 (발사체)
    
    private void PerformRangedAttack(Transform target)
    {
        lastAttackTime = Time.time;

        if (controller == null)
        {
            Debug.LogWarning("[MonsterCombat] MonsterController가 없습니다!");
            return;
        }

        if (projectilePrefab == null)
        {
            Debug.LogWarning("[MonsterCombat] 발사체 프리팹이 없습니다!");
            return;
        }

        // 발사체 생성
        Vector2 direction = (target.position - transform.position).normalized;
        GameObject projectile = Object.Instantiate(projectilePrefab, transform.position, Quaternion.identity);

        // 발사체 설정
        var projectileScript = projectile.GetComponent<MonsterProjectile>();
        if (projectileScript != null)
        {
            bool is_critical = false;
            int damage = controller.GetAttackPower(ref is_critical);
            float monsterAccuracy = controller.GetAccuracy();

            projectileScript.Initialize(direction, projectileSpeed, damage, is_critical, monsterAccuracy);
        }
        else
        {
            Debug.LogWarning("[MonsterCombat] 발사체에 MonsterProjectile 스크립트가 없습니다!");
        }
    }

    public bool IsInAttackRange(Transform target)
    {
        if (target == null) return false;
        float distance = Vector2.Distance(transform.position, target.position);

        
        if (canRangedAttack)
        {
            // 원거리 공격 이면 원거리 사거리 체크
            return distance <= rangedAttackRange;
        }
        else
        {
            // 근접 공격 이면 근접 사거리 체크   
            return distance <= attackRange;
        }
    }

    public bool IsAtPreferredDistance(Transform target)
    {
        if (target == null) return false;
        float distance = Vector2.Distance(transform.position, target.position);
        return distance <= preferredAttackDistance;
    }

    public float GetDistanceTo(Transform target)
    {
        if (target == null) return float.MaxValue;
        return Vector2.Distance(transform.position, target.position);
    }
}