using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuardStance : PassiveSkillBase
{
    public GuardStance(SkillData data, int level = 1) : base(data, level)
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
