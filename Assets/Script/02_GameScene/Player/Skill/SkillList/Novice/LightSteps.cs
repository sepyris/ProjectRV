using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightSteps : ActiveSkillBase
{
    private const string EFFECT_PATH = "SkillsPrefabs/LightSteps";
    public LightSteps(SkillData data, int level = 1) : base(data, level)
    {
    }

    protected override void Execute(Transform caster, Vector3 targetPosition, Transform targetTransform)
    {
        // 버프 수치 계산
        float buffPercent = GetCurrentDamage() / 100f;
        float buffDuration = skillData.cooldown / 2f;

        // BuffManager로 버프 적용
        BuffManager.Instance.ApplyBuff(
            skillData.skillId,
            skillData.skillName,
            buffDuration,
            (stats) => {
                stats.skill_moveSpeedBonus += buffPercent;
                stats.RecalculateStats();
                Debug.Log($"[BraveHeart] 공격력 +{buffPercent * 100}%!");
            },
            (stats) => {
                stats.skill_moveSpeedBonus -= buffPercent;
                stats.RecalculateStats();
                Debug.Log($"[BraveHeart] 버프 종료");
            }
        );
        SpawnEffect(EFFECT_PATH, caster.position, Quaternion.identity);
    }
}
