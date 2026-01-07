using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostField : ActiveSkillBase
{
    private const string EFFECT_PATH = "Effects/FrostFieldEffect";
    private const float SLASH_RANGE = 5.0f;
    private const float SLASH_ANGLE = 120f;

    public FrostField(SkillData data, int level = 1) : base(data, level)
    {
    }

    protected override void Execute(Transform caster, Vector3 targetPosition, Transform targetTransform)
    {
        PlayerStatsComponent playerStats = caster.GetComponent<PlayerStatsComponent>();
        PlayerMovement movement = caster.GetComponent<PlayerController>()?.movement;

        if (playerStats == null)
        {
            Debug.LogError("[FrostField] PlayerStatsComponent 없음");
            return;
        }

        // 공격 방향 결정
        Vector2 attackDirection;
        if (movement != null)
        {
            attackDirection = movement.LastMoveDirection;
        }
        else
        {
            attackDirection = (targetPosition - caster.position).normalized;
        }

        // 데미지 계산
        float skillDamageRate = GetCurrentDamage();
        int damage = Mathf.FloorToInt(playerStats.Stats.attackPower * skillDamageRate / 100f);

        // 서리 공격
        PerformSlashAttack(caster, attackDirection, damage, playerStats.Stats);
    }

    /// <summary>
    /// 서리 범위 공격 (즉시 실행)
    /// </summary>
    private void PerformSlashAttack(Transform caster, Vector2 attackDirection, int damage, CharacterStats stats)
    {

        // 범위 내 적 탐색
        Collider2D[] hits = Physics2D.OverlapCircleAll(caster.position, SLASH_RANGE, LayerMask.GetMask("Monster"));

        int hitCount = 0;
        foreach (Collider2D hit in hits)
        {
            // 각도 체크 (부채꼴 범위)
            Vector2 toEnemy = (hit.transform.position - caster.position).normalized;
            float angle = Vector2.Angle(attackDirection, toEnemy);

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

        // 서리 이펙트
        Vector3 effectPosition = caster.position + (Vector3)attackDirection * 2.5f;  // 더 앞에 (5m 범위니까)
        float effectAngle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
        SpawnEffect(EFFECT_PATH, effectPosition, Quaternion.Euler(0, 0, effectAngle));

        Debug.Log($"[FrostField] {hitCount}명 공격!");
    }
}