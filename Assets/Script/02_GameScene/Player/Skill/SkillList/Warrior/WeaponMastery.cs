using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponMastery : PassiveSkillBase
{
    public WeaponMastery(SkillData data, int level = 1) : base(data, level)
    {
    }

    protected override void OnApply(CharacterStats stats)
    {
        float attackBonus = GetCurrentModifier();

        stats.skill_attackBonus += Mathf.FloorToInt(attackBonus);
        stats.RecalculateStats();
    }

    protected override void OnRemove(CharacterStats stats)
    {
        float attackBonus = GetCurrentModifier();

        stats.skill_attackBonus += Mathf.FloorToInt(attackBonus);
        stats.RecalculateStats();
    }
}
