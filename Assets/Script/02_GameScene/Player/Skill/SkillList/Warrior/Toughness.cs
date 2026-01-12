using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Toughness : PassiveSkillBase
{
    public Toughness(SkillData data, int level = 1) : base(data, level)
    {
    }
    public override string DetailDescription
    {
        get
        {
            return $"{Description}\n\n현재 체력의 {GetCurrentDamage()}% 가 오릅니다";
        }
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
