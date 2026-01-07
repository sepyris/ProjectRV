using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

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

        if(skillData.skillId == "Skill_001")
        {
            return new FullSwing(skillData,level);
        }
        if (skillData.skillId == "Skill_002")
        {
            return new MagicMissile(skillData, level);
        }
        if (skillData.skillId == "Skill_003")
        {
            return new FirstAid(skillData, level);
        }
        if (skillData.skillId == "Skill_004")
        {
            return new LightSteps(skillData, level);
        }
        if (skillData.skillId == "Skill_005")
        {
            return new BashDash(skillData, level);
        }
        if (skillData.skillId == "Skill_006")
        {
            return new MultiSlash(skillData, level);
        }
        if (skillData.skillId == "Skill_007")
        {
            return new BraveHeart(skillData, level);
        }
        if (skillData.skillId == "Skill_008")
        {
            return new DaggerThrow(skillData, level);
        }
        if (skillData.skillId == "Skill_009")
        {
            return new Toughness(skillData, level);
        }
        if (skillData.skillId == "Skill_010")
        {
            return new WeaponMastery(skillData, level);
        }
        if (skillData.skillId == "Skill_011")
        {
            return new GuardStance(skillData, level);
        }
        if (skillData.skillId == "Skill_012")
        {
            return new FireBall(skillData, level);
        }
        if (skillData.skillId == "Skill_013")
        {
            return new FrostField(skillData, level);
        }
        if (skillData.skillId == "Skill_014")
        {
            return new LightningCircle(skillData, level);
        }
        if (skillData.skillId == "Skill_015")
        {
            return new Heal(skillData, level);
        }
        if (skillData.skillId == "Skill_016")
        {
            return new Afterimage(skillData, level);
        }
        if (skillData.skillId == "Skill_017")
        {
            return new ManaControl(skillData, level);
        }
        if (skillData.skillId == "Skill_018")
        {
            return new MentalTraining(skillData, level);
        }
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