using UnityEngine;

public class BraveHeart : ActiveSkillBase
{
    private const string BUFF_ID = "BraveHeart";
    private const string EFFECT_PATH = "Effects/BuffEffect";

    public BraveHeart(SkillData data, int level = 1) : base(data, level)
    {
    }

    protected override void Execute(Transform caster, Vector3 targetPosition, Transform targetTransform)
    {
        // 버프 수치 계산
        float buffPercent = GetCurrentDamage() / 100f;
        float buffDuration = skillData.cooldown / 2f;  // ← 이걸 사용!

        // BuffManager로 버프 적용
        BuffManager.Instance.ApplyBuff(
            BUFF_ID,
            "든든한 마음",
            buffDuration,  // ← buff_duration 말고 buffDuration!
            (stats) => {
                stats.skill_attackBonus += buffPercent;
                stats.RecalculateStats();
                Debug.Log($"[BraveHeart] 공격력 +{buffPercent * 100}%!");
            },
            (stats) => {
                stats.skill_attackBonus -= buffPercent;
                stats.RecalculateStats();
                Debug.Log($"[BraveHeart] 버프 종료");
            }
        );

        // 이펙트
        SpawnEffect(EFFECT_PATH, caster.position, Quaternion.identity);
    }
}