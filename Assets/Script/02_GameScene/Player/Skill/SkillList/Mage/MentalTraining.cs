using UnityEngine;
using Definitions;

/// <summary>
/// 정신 수양 - 방어력을 증가시키는 패시브 스킬
/// </summary>
public class MentalTraining : PassiveSkillBase
{
    public MentalTraining(SkillData data, int level = 1) : base(data, level)
    {
    }

    protected override void OnApply(CharacterStats stats)
    {
        float defenseBonus = GetCurrentModifier();

        stats.skill_defenseBonus += Mathf.FloorToInt(defenseBonus);
        stats.RecalculateStats();
    }

    protected override void OnRemove(CharacterStats stats)
    {
        float defenseBonus = GetCurrentModifier();

        stats.skill_defenseBonus -= Mathf.FloorToInt(defenseBonus);
        stats.RecalculateStats();
    }
}