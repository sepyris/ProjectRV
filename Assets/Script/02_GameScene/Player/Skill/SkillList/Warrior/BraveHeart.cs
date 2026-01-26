using UnityEngine;

public class BraveHeart : ActiveSkillBase
{
    public BraveHeart(SkillData data, int level = 1) : base(data, level)
    {
    }
    public override string DetailDescription
    {
        get
        {
            return $"{Description}\n\n{skillData.cooldown / 2f}초동안 공격력과 방어력이 {GetCurrentDamage()}% 만큼 오릅니다.";
        }
    }
    protected override void Execute(Transform caster, Vector3 targetPosition, Transform targetTransform)
    {
        // 버프 수치 계산
        float buffPercent = GetCurrentDamage();
        float buffDuration = skillData.cooldown / 2f;

        // BuffManager로 버프 적용
        BuffManager.Instance.ApplyBuff(
            skillData.skillId,
            skillData.skillName,
            buffDuration,
            (stats) => {
                stats.skill_attackBonus += buffPercent;
                stats.skill_defenseBonus += buffPercent;
                stats.RecalculateStats();
                Debug.Log($"[BraveHeart] 공격력 +{buffPercent * 100}%!");
            },
            (stats) => {
                stats.skill_attackBonus -= buffPercent;
                stats.skill_defenseBonus -= buffPercent;
                stats.RecalculateStats();
                Debug.Log($"[BraveHeart] 버프 종료");
            }
        );

        // 이펙트
        SpawnEffect("", caster.position, Quaternion.identity);
    }
}