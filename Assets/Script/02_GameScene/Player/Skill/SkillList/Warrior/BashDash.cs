using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BashDash : ActiveSkillBase
{
    private const string DASH_EFFECT_PATH = "Effects/DashEffect";
    private const string SLASH_EFFECT_PATH = "Effects/SlashEffect";

    private const float TARGET_SEARCH_RADIUS = 8f;  // 적 탐색 반경
    private const float DASH_DISTANCE = 5f;          // 대쉬 거리 (적 없을 때)
    private const float DASH_DURATION = 0.3f;
    private const float SLASH_RANGE = 2.5f;          // 휘두르기 범위
    private const float SLASH_ANGLE = 150f;          // 휘두르기 각도

    private HashSet<MonsterController> dashHitMonsters = new HashSet<MonsterController>();

    public BashDash(SkillData data, int level = 1) : base(data, level)
    {
    }

    protected override void Execute(Transform caster, Vector3 targetPosition, Transform targetTransform)
    {
        PlayerController player = caster.GetComponent<PlayerController>();
        PlayerStatsComponent playerStats = caster.GetComponent<PlayerStatsComponent>();
        PlayerMovement movement = player?.GetComponent<PlayerController>()?.movement;

        if (player == null || playerStats == null)
        {
            Debug.LogError("[BashDash] 필요한 컴포넌트 없음");
            return;
        }

        // 1단계: 대쉬 목표 결정
        Vector2 dashDirection;
        float dashDistance;
        MonsterController targetMonster = FindNearestMonster(caster.position, TARGET_SEARCH_RADIUS);

        if (targetMonster != null)
        {
            // 가까운 적이 있으면 → 적에게 돌진
            Vector2 toMonster = (targetMonster.transform.position - caster.position);
            dashDirection = toMonster.normalized;
            dashDistance = Mathf.Min(toMonster.magnitude - 0.5f, DASH_DISTANCE);  // 적 바로 앞까지

            Debug.Log($"[BashDash] 적 발견! {targetMonster.GetMonsterName()}에게 돌진");
        }
        else
        {
            // 적이 없으면 → 바라보는 방향으로 돌진
            if (movement != null)
            {
                dashDirection = movement.LastMoveDirection;
            }
            else
            {
                dashDirection = Vector2.right;  // fallback
            }
            dashDistance = DASH_DISTANCE;

            Debug.Log($"[BashDash] 적 없음. 바라보는 방향으로 돌진: {dashDirection}");
        }

        // 데미지 계산
        float skillDamageRate = GetCurrentDamage();
        int dashDamage = Mathf.FloorToInt(playerStats.Stats.attackPower * skillDamageRate / 100f);
        int slashDamage = Mathf.FloorToInt(playerStats.Stats.attackPower * (skillDamageRate * 1.2f) / 100f);  // 휘두르기는 20% 더 강함

        dashHitMonsters.Clear();

        // 2단계: 대쉬 실행
        player.Dash(
            dashDirection,
            dashDistance,
            DASH_DURATION,
            (currentPos) => {
                // 대쉬 중 충돌 체크
                CheckDashCollision(currentPos, dashDamage, playerStats.Stats);
            },
            () => {
                // 3단계: 대쉬 완료 후 휘두르기
                if (player is MonoBehaviour mono)
                {
                    mono.StartCoroutine(PerformSlashAttack(caster, dashDirection, slashDamage, playerStats.Stats));
                }
            }
        );

        // 시작 이펙트
        SpawnEffect(DASH_EFFECT_PATH, caster.position, Quaternion.identity);
    }

    /// <summary>
    /// 가장 가까운 적 찾기
    /// </summary>
    private MonsterController FindNearestMonster(Vector2 position, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius, LayerMask.GetMask("Monster"));

        MonsterController nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            MonsterController monster = hit.GetComponent<MonsterController>();
            if (monster != null && !monster.IsDead())
            {
                float distance = Vector2.Distance(position, monster.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = monster;
                }
            }
        }

        return nearest;
    }

    /// <summary>
    /// 대쉬 중 충돌 체크
    /// </summary>
    private void CheckDashCollision(Vector2 currentPos, int damage, CharacterStats stats)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(currentPos, 0.8f, LayerMask.GetMask("Monster"));

        foreach (Collider2D hit in hits)
        {
            MonsterController monster = hit.GetComponent<MonsterController>();

            if (monster != null && !dashHitMonsters.Contains(monster))
            {
                dashHitMonsters.Add(monster);

                bool isCritical = Random.Range(0f, 100f) <= stats.criticalChance;
                int finalDamage = damage;

                if (isCritical)
                {
                    finalDamage = Mathf.FloorToInt(damage * (stats.criticalDamage / 100f));
                }

                monster.TakeDamage(finalDamage, isCritical, stats.accuracy);
            }
        }
    }

    /// <summary>
    /// 대쉬 후 휘두르기 공격
    /// </summary>
    private IEnumerator PerformSlashAttack(Transform caster, Vector2 dashDirection, int damage, CharacterStats stats)
    {
        // 짧은 딜레이 (대쉬 후 휘두르기)
        yield return new WaitForSeconds(0.1f);

        Debug.Log("[BashDash] 휘두르기 공격!");

        // 휘두르기 범위 내 적 탐색
        Collider2D[] hits = Physics2D.OverlapCircleAll(caster.position, SLASH_RANGE, LayerMask.GetMask("Monster"));

        int hitCount = 0;
        foreach (Collider2D hit in hits)
        {
            // 각도 체크 (부채꼴 범위)
            Vector2 toEnemy = (hit.transform.position - caster.position).normalized;
            float angle = Vector2.Angle(dashDirection, toEnemy);

            if (angle <= SLASH_ANGLE / 2f)
            {
                MonsterController monster = hit.GetComponent<MonsterController>();
                if (monster != null)
                {
                    bool isCritical = Random.Range(0f, 100f) <= stats.criticalChance;
                    int finalDamage = damage;

                    if (isCritical)
                    {
                        finalDamage = Mathf.FloorToInt(damage * (stats.criticalDamage / 100f));
                    }

                    monster.TakeDamage(finalDamage, isCritical, stats.accuracy);
                    hitCount++;
                }
            }
        }

        // 휘두르기 이펙트
        Vector3 effectPosition = caster.position + (Vector3)dashDirection * 1.2f;
        float effectAngle = Mathf.Atan2(dashDirection.y, dashDirection.x) * Mathf.Rad2Deg;
        SpawnEffect(SLASH_EFFECT_PATH, effectPosition, Quaternion.Euler(0, 0, effectAngle));

        Debug.Log($"[BashDash] 휘두르기로 {hitCount}명 공격!");

        // 대쉬 중 맞은 적 리스트 초기화
        dashHitMonsters.Clear();
    }
}