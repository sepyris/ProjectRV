using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Toughness : PassiveSkillBase
{
    public Toughness(SkillData data, int level = 1) : base(data, level)
    {
    }

    protected override void OnApply(CharacterStats stats)
    {
        float hpbonus = GetCurrentDamage();

        stats.skill_HPBonus += Mathf.FloorToInt(hpbonus);
        stats.RecalculateStats();

    }

    protected override void OnRemove(CharacterStats stats)
    {
        float hpbonus = GetCurrentDamage();

        stats.skill_HPBonus -= Mathf.FloorToInt(hpbonus);
        stats.RecalculateStats();
    }
}
