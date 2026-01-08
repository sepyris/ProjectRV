using UnityEngine;
using System.Collections;

public class MultiSlash : ActiveSkillBase
{
    private const string SLASH_EFFECT_PATH = "Effects/SlashEffect";
    private const float SLASH_RANGE = 2.5f;
    private const float SLASH_ANGLE = 150f;
    private const float SLASH_INTERVAL = 0.15f;

    public MultiSlash(SkillData data, int level = 1) : base(data, level)
    {
    }

    /// <summary>
    /// 레벨별 베기 횟수
    /// </summary>
    private int GetSlashCount()
    {
        if (currentLevel >= 10) return 5;     // Lv.10: 5타
        if (currentLevel >= 5) return 4;      // Lv.5~9: 4타
        return 3;                             // Lv.1~4: 3타
    }

    protected override void Execute(Transform caster, Vector3 targetPosition, Transform targetTransform)
    {
        PlayerStatsComponent playerStats = caster.GetComponent<PlayerStatsComponent>();
        PlayerMovement movement = caster.GetComponent<PlayerController>()?.movement;

        if (playerStats == null)
        {
            Debug.LogError("[MultiSlash] PlayerStatsComponent 없음");
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

        // 레벨별 베기 횟수
        int slashCount = GetSlashCount();

        // 데미지 계산 (총 데미지를 나눔)
        float skillDamageRate = GetCurrentDamage();
        int totalDamage = Mathf.FloorToInt(playerStats.Stats.attackPower * skillDamageRate / 100f);
        int damagePerSlash = Mathf.FloorToInt(totalDamage / (float)slashCount);

        // 연속 베기 시작
        PlayerController player = caster.GetComponent<PlayerController>();
        if (player is MonoBehaviour mono)
        {
            mono.StartCoroutine(PerformMultiSlash(caster, attackDirection, damagePerSlash, playerStats.Stats, slashCount));
        }
    }

    /// <summary>
    /// 연속 베기 (코루틴)
    /// </summary>
    private IEnumerator PerformMultiSlash(Transform caster, Vector2 attackDirection, int damagePerSlash, CharacterStats stats, int slashCount)
    {
        Debug.Log($"[MultiSlash] 연속 베기 시작! ({slashCount}회, 레벨: {currentLevel})");

        int totalHitCount = 0;

        for (int i = 0; i < slashCount; i++)
        {
            // 각 베기마다 약간 각도 변화
            float angleOffset = 0f;
            if (slashCount == 3)
            {
                angleOffset = (i - 1) * 15f;  // -15°, 0°, +15°
            }
            else if (slashCount == 4)
            {
                angleOffset = (i - 1.5f) * 12f;  // -18°, -6°, +6°, +18°
            }
            else if (slashCount == 5)
            {
                angleOffset = (i - 2) * 10f;  // -20°, -10°, 0°, +10°, +20°
            }

            Vector2 currentDirection = RotateVector(attackDirection, angleOffset);

            // 단일 베기 실행
            int hitCount = PerformSingleSlash(caster, currentDirection, damagePerSlash, stats, i + 1);
            totalHitCount += hitCount;

            // 마지막 베기가 아니면 대기
            if (i < slashCount - 1)
            {
                yield return new WaitForSeconds(SLASH_INTERVAL);
            }
        }

        Debug.Log($"[MultiSlash] 연속 베기 완료! 총 {totalHitCount}명 공격");
    }

    /// <summary>
    /// 단일 베기
    /// </summary>
    private int PerformSingleSlash(Transform caster, Vector2 attackDirection, int damage, CharacterStats stats, int slashNumber)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(caster.position, SLASH_RANGE, LayerMask.GetMask("Monster"));

        int hitCount = 0;
        foreach (Collider2D hit in hits)
        {
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

        // 베기 이펙트
        Vector3 effectPosition = caster.position + (Vector3)attackDirection * 1.2f;
        float effectAngle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
        bool isFacingLeft = attackDirection.x < 0;
        SpawnEffect(SLASH_EFFECT_PATH, effectPosition, Quaternion.Euler(0, 0, effectAngle), isFacingLeft);

        Debug.Log($"[MultiSlash] {slashNumber}번째 베기: {hitCount}명 공격");

        return hitCount;
    }

    /// <summary>
    /// 벡터 회전 헬퍼
    /// </summary>
    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }
}