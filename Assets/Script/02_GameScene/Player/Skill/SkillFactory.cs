using UnityEngine;

/// <summary>
/// 스킬 팩토리
/// SkillData를 기반으로 적절한 스킬 인스턴스 생성
/// </summary>
public static class SkillFactory
{
    /// <summary>
    /// 스킬 데이터로부터 스킬 인스턴스 생성
    /// </summary>
    public static SkillBase CreateSkill(SkillData skillData, int level = 1)
    {
        if (skillData == null)
        {
            Debug.LogError("[SkillFactory] skillData가 null입니다.");
            return null;
        }
        /*
        switch (skillData.skillType)
        {
            case SkillType.Damage:
                return new DamageSkill(skillData, level);

            case SkillType.Heal:
                return new HealSkill(skillData, level);

            case SkillType.Projectile:
                return new ProjectileSkill(skillData, level);

            case SkillType.Area:
                return new AreaSkill(skillData, level);

            case SkillType.Buff:
                return new BuffSkill(skillData, level);

            case SkillType.Dash:
                return new DashSkill(skillData, level);

            case SkillType.Summon:
                return new SummonSkill(skillData, level);

            // TODO: 다른 스킬 타입 추가

            default:
                Debug.LogWarning($"[SkillFactory] 지원하지 않는 스킬 타입: {skillData.skillType}");
                return new DamageSkill(skillData, level); // 기본값
        }
        */
        return null;
    }

    /// <summary>
    /// 스킬 ID로 스킬 생성 (SkillDataManager 필요)
    /// </summary>
    public static SkillBase CreateSkillById(string skillId, int level = 1)
    {
        if (SkillDataManager.Instance == null)
        {
            Debug.LogError("[SkillFactory] SkillDataManager가 없습니다.");
            return null;
        }

        SkillData skillData = SkillDataManager.Instance.GetSkillData(skillId);
        if (skillData == null)
        {
            Debug.LogError($"[SkillFactory] 스킬 데이터를 찾을 수 없음: {skillId}");
            return null;
        }

        return CreateSkill(skillData, level);
    }
}