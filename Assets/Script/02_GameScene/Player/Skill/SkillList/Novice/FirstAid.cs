using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstAid : ActiveSkillBase
{
    private const string EFFECT_PATH = "SkillsPrefabs/FirstAid";
    public FirstAid(SkillData data, int level = 1) : base(data, level)
    {
    }

    protected override void Execute(Transform caster, Vector3 targetPosition, Transform targetTransform)
    {
        PlayerStatsComponent playerStats = caster.GetComponent<PlayerStatsComponent>();

        float healRate = GetCurrentDamage();
        int healamount = Mathf.FloorToInt(playerStats.Stats.maxHP * (healRate / 100f));

        playerStats.Stats.Heal(healamount);
        SpawnEffect(EFFECT_PATH, caster.position, Quaternion.identity);
    }
}
