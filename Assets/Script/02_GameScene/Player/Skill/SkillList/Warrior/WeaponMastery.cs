using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponMastery : PassiveSkillBase
{
    public WeaponMastery(SkillData data, int level = 1) : base(data, level)
    {
    }
    public override string DetailDescription
    {
        get
        {
            return $"{Description}\n\n현재 공격력의 {GetCurrentDamage()}% 가 오릅니다";
        }
    }
    protected override void OnApply(CharacterStats stats)
    {
        float attackBonus = GetCurrentDamage();

        stats.skill_attackBonus += Mathf.FloorToInt(attackBonus);
        stats.RecalculateStats();
    }

    protected override void OnRemove(CharacterStats stats)
    {
        float attackBonus = GetCurrentDamage();

        stats.skill_attackBonus += Mathf.FloorToInt(attackBonus);
        stats.RecalculateStats();
    }
}
