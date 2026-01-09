using UnityEngine;

public class LightningCircle : ActiveSkillBase
{
    private const string EFFECT_PATH = "SkillsPrefabs/LightningCircle";
    private const float ATTACK_RADIUS = 5.0f;  // 공격 반경

    public LightningCircle(SkillData data, int level = 1) : base(data, level)
    {
    }

    protected override void Execute(Transform caster, Vector3 targetPosition, Transform targetTransform)
    {
        PlayerStatsComponent playerStats = caster.GetComponent<PlayerStatsComponent>();

        if (playerStats == null)
        {
            Debug.LogError("[LightningCircle] PlayerStatsComponent 없음");
            return;
        }

        // 데미지 계산
        float skillDamageRate = GetCurrentDamage();
        int damage = Mathf.FloorToInt(playerStats.Stats.attackPower * skillDamageRate / 100f);

        // 원형 범위 공격
        PerformCircleAttack(caster, damage, playerStats.Stats);
    }

    /// <summary>
    /// 원형 범위 공격
    /// </summary>
    private void PerformCircleAttack(Transform caster, int damage, CharacterStats stats)
    {

        // 범위 내 모든 적 탐색
        Collider2D[] hits = Physics2D.OverlapCircleAll(caster.position, ATTACK_RADIUS, LayerMask.GetMask("Monster"));

        int hitCount = 0;
        foreach (Collider2D hit in hits)
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
                SpawnEffect(EFFECT_PATH, monster.transform.position, Quaternion.identity);
                monster.TakeDamage(finalDamage, isCritical, stats.accuracy);
                hitCount++;
            }
        }
        Debug.Log($"[LightningCircle] {hitCount}명 공격!");
    }
}