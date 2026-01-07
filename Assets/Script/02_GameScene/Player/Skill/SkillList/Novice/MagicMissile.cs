using UnityEngine;
using System.Collections.Generic;

public class MagicMissile : ActiveSkillBase
{
    private const string PROJECTILE_PATH = "Prefabs/Skills/MagicMissileProjectile";
    private const float PROJECTILE_SPEED = 18f;
    private const float PROJECTILE_DISTANCE = 15f;
    private const float SEARCH_RADIUS = 10f;

    public MagicMissile(SkillData data, int level = 1) : base(data, level)
    {
    }

    /// <summary>
    /// 레벨별 발사체 개수
    /// </summary>
    private int GetMissileCount()
    {
        if (currentLevel >= 10) return 3;     // Lv.10: 3발
        if (currentLevel >= 5) return 2;      // Lv.5~9: 2발
        return 1;                             // Lv.1~4: 1발
    }

    /// <summary>
    /// 사용 가능 조건 체크 (원형 범위 내 적이 있어야 함)
    /// </summary>
    public override bool CanUse()
    {
        if (!base.CanUse())
            return false;

        if (PlayerController.Instance == null)
            return false;

        Transform caster = PlayerController.Instance.transform;

        // 원형 범위 내 적 탐색
        List<MonsterController> targets = FindTargetsInCircle(caster.position, SEARCH_RADIUS, GetMissileCount());

        if (targets.Count == 0)
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

        if (playerStats == null)
        {
            Debug.LogError("[MagicMissile] PlayerStatsComponent 없음");
            return;
        }

        // 레벨별 발사체 개수
        int missileCount = GetMissileCount();

        // 원형 범위 내 적들 찾기 (최대 missileCount명)
        List<MonsterController> targets = FindTargetsInCircle(caster.position, SEARCH_RADIUS, missileCount);

        if (targets.Count == 0)
        {
            Debug.LogWarning("[MagicMissile] 적을 찾을 수 없음");
            return;
        }

        // 데미지 계산
        float skillDamageRate = GetCurrentDamage();
        int damage = Mathf.FloorToInt(playerStats.Stats.attackPower * skillDamageRate / 100f);

        // 각 타겟에게 유도탄 발사
        for (int i = 0; i < targets.Count; i++)
        {
            Vector2 direction = (targets[i].transform.position - caster.position).normalized;
            SpawnHomingProjectile(caster.position, direction, damage, playerStats.Stats, targets[i], i);
        }

        Debug.Log($"[MagicMissile] {targets.Count}발 발사! (레벨: {currentLevel})");
    }

    /// <summary>
    /// 원형 범위 내 여러 적 탐색
    /// </summary>
    private List<MonsterController> FindTargetsInCircle(Vector2 center, float radius, int maxCount)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, LayerMask.GetMask("Monster"));

        List<MonsterController> validTargets = new List<MonsterController>();

        // 살아있는 적만 필터링
        foreach (Collider2D hit in hits)
        {
            MonsterController monster = hit.GetComponent<MonsterController>();
            if (monster != null && !monster.IsDead())
            {
                validTargets.Add(monster);
            }
        }

        // 거리순 정렬
        validTargets.Sort((a, b) => {
            float distA = Vector2.Distance(center, a.transform.position);
            float distB = Vector2.Distance(center, b.transform.position);
            return distA.CompareTo(distB);
        });

        // 최대 개수만큼만 반환
        if (validTargets.Count > maxCount)
        {
            validTargets = validTargets.GetRange(0, maxCount);
        }

        return validTargets;
    }

    /// <summary>
    /// 유도 발사체 생성
    /// </summary>
    private void SpawnHomingProjectile(Vector3 position, Vector2 direction, int damage, CharacterStats stats, MonsterController target, int index)
    {
        GameObject prefab = Resources.Load<GameObject>(PROJECTILE_PATH);
        if (prefab == null)
        {
            Debug.LogError($"[MagicMissile] 프리팹을 찾을 수 없음: {PROJECTILE_PATH}");
            return;
        }

        // 약간 오프셋 (여러 발이 겹치지 않게)
        Vector3 spawnOffset = new Vector3(
            Random.Range(-0.2f, 0.2f),
            Random.Range(-0.2f, 0.2f),
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
            Debug.LogError("[MagicMissile] PlayerProjectile 컴포넌트 없음");
            Object.Destroy(projectileObj);
        }
    }
}