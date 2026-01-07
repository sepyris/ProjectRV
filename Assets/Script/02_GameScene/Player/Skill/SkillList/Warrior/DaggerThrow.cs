using UnityEngine;
using System.Collections;

public class DaggerThrow : ActiveSkillBase
{
    private const string PROJECTILE_PATH = "Prefabs/Skills/DaggerProjectile";
    private const float PROJECTILE_SPEED = 15f;
    private const float PROJECTILE_DISTANCE = 12f;
    private const float SEARCH_RADIUS = 8f;
    private const float SEARCH_ANGLE = 120f;
    private const float THROW_INTERVAL = 0.1f;  // 단검 발사 간격

    public DaggerThrow(SkillData data, int level = 1) : base(data, level)
    {
    }

    /// <summary>
    /// 레벨별 단검 개수
    /// </summary>
    private int GetDaggerCount()
    {
        if (currentLevel >= 10) return 4;     // Lv.10: 4발
        if (currentLevel >= 5) return 3;      // Lv.5~9: 3발
        return 2;                             // Lv.1~4: 2발
    }

    /// <summary>
    /// 사용 가능 조건 체크
    /// </summary>
    public override bool CanUse()
    {
        if (!base.CanUse())
            return false;

        if (PlayerController.Instance == null)
            return false;

        Transform caster = PlayerController.Instance.transform;
        PlayerMovement movement = PlayerController.Instance.movement;

        if (movement == null)
            return false;

        Vector2 lookDirection = movement.LastMoveDirection;
        MonsterController target = FindTargetInSector(caster.position, lookDirection, SEARCH_RADIUS, SEARCH_ANGLE);

        if (target == null)
        {
            if (FloatingNotificationManager.Instance != null)
            {
                FloatingNotificationManager.Instance.ShowNotification("범위 내 적이 없습니다!");
            }
            return false;
        }

        return true;
    }

    protected override void Execute(Transform caster, Vector3 targetPosition, Transform targetTransform)
    {
        PlayerStatsComponent playerStats = caster.GetComponent<PlayerStatsComponent>();
        PlayerMovement movement = caster.GetComponent<PlayerController>()?.movement;

        if (playerStats == null)
        {
            Debug.LogError("[DaggerThrow] PlayerStatsComponent 없음");
            return;
        }

        // 바라보는 방향
        Vector2 lookDirection;
        if (movement != null)
        {
            lookDirection = movement.LastMoveDirection;
        }
        else
        {
            lookDirection = Vector2.right;
        }

        // 부채꼴 범위 내 가장 가까운 적 찾기
        MonsterController target = FindTargetInSector(caster.position, lookDirection, SEARCH_RADIUS, SEARCH_ANGLE);

        if (target == null)
        {
            Debug.LogWarning("[DaggerThrow] 적을 찾을 수 없음");
            return;
        }

        // 레벨별 단검 개수
        int daggerCount = GetDaggerCount();

        // 데미지 계산 (총 데미지를 나눔)
        float skillDamageRate = GetCurrentDamage();
        int totalDamage = Mathf.FloorToInt(playerStats.Stats.attackPower * skillDamageRate / 100f);
        int damagePerDagger = Mathf.FloorToInt(totalDamage / (float)daggerCount);

        // 연속 투척 시작
        PlayerController player = caster.GetComponent<PlayerController>();
        if (player is MonoBehaviour mono)
        {
            mono.StartCoroutine(PerformDaggerThrow(caster, target, damagePerDagger, playerStats.Stats, daggerCount));
        }
    }

    /// <summary>
    /// 연속 단검 투척 (코루틴)
    /// </summary>
    private IEnumerator PerformDaggerThrow(Transform caster, MonsterController target, int damagePerDagger, CharacterStats stats, int daggerCount)
    {
        Debug.Log($"[DaggerThrow] 연속 투척 시작! ({daggerCount}발, 타겟: {target.GetMonsterName()}, 레벨: {currentLevel})");

        for (int i = 0; i < daggerCount; i++)
        {
            // 타겟이 죽었으면 중단
            if (target == null || target.IsDead())
            {
                Debug.Log("[DaggerThrow] 타겟 사망 - 투척 중단");
                break;
            }

            // 타겟 방향으로 유도탄 발사
            Vector2 direction = (target.transform.position - caster.position).normalized;
            SpawnHomingProjectile(caster.position, direction, damagePerDagger, stats, target, i);

            Debug.Log($"[DaggerThrow] {i + 1}번째 단검 투척");

            // 마지막 단검이 아니면 대기
            if (i < daggerCount - 1)
            {
                yield return new WaitForSeconds(THROW_INTERVAL);
            }
        }

        Debug.Log($"[DaggerThrow] 연속 투척 완료!");
    }

    /// <summary>
    /// 부채꼴 범위 내 적 탐색
    /// </summary>
    private MonsterController FindTargetInSector(Vector2 center, Vector2 direction, float radius, float angle)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, LayerMask.GetMask("Monster"));

        MonsterController nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            MonsterController monster = hit.GetComponent<MonsterController>();
            if (monster != null && !monster.IsDead())
            {
                Vector2 toMonster = (hit.transform.position - (Vector3)center).normalized;
                float monsterAngle = Vector2.Angle(direction, toMonster);

                if (monsterAngle <= angle / 2f)
                {
                    float distance = Vector2.Distance(center, hit.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = monster;
                    }
                }
            }
        }

        return nearest;
    }

    /// <summary>
    /// 유도 발사체 생성
    /// </summary>
    private void SpawnHomingProjectile(Vector3 position, Vector2 direction, int damage, CharacterStats stats, MonsterController target, int index)
    {
        GameObject prefab = Resources.Load<GameObject>(PROJECTILE_PATH);
        if (prefab == null)
        {
            Debug.LogError($"[DaggerThrow] 프리팹을 찾을 수 없음: {PROJECTILE_PATH}");
            return;
        }

        // 약간 오프셋 (연속 발사 시 겹치지 않게)
        Vector3 spawnOffset = new Vector3(
            Random.Range(-0.15f, 0.15f),
            Random.Range(-0.15f, 0.15f),
            0f
        );

        GameObject projectileObj = Object.Instantiate(prefab, position + spawnOffset, Quaternion.identity);
        PlayerProjectile projectile = projectileObj.GetComponent<PlayerProjectile>();

        if (projectile != null)
        {
            projectile.Initialize(
                damage,
                stats.criticalChance,
                stats.criticalDamage,
                stats.accuracy,
                PROJECTILE_DISTANCE
            );

            projectile.SetVelocity(direction * PROJECTILE_SPEED);

            // 유도 컴포넌트 추가
            HomingComponent homing = projectileObj.AddComponent<HomingComponent>();
            homing.Initialize(target, PROJECTILE_SPEED);
        }
        else
        {
            Debug.LogError("[DaggerThrow] PlayerProjectile 컴포넌트 없음");
            Object.Destroy(projectileObj);
        }
    }
}