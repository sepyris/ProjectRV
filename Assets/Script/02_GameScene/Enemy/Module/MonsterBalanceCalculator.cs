using UnityEngine;

public static class MonsterBalanceCalculator
{
    // 평화로운 RPG - 스킬 약간 고려
    public const float EFFECTIVE_DPS_MULTIPLIER = 1.2f;

    public static CharacterStats CalculateMonsterStats(MonsterData monsterData)
    {
        CharacterStats stats = new CharacterStats();
        stats.Initialize(monsterData.monsterName, monsterData.level, true);

        float hpMultiplier = 1f;
        float attackMultiplier = 1f;
        float defenseMultiplier = 1f;
        float accuracyBonus = 0f;
        float evasionBonus = 0f;
        float critChanceBonus = 0f;

        switch (monsterData.monsterType)
        {
            case MonsterType.Normal:
                hpMultiplier = 0.7f * EFFECTIVE_DPS_MULTIPLIER;
                attackMultiplier = 0.6f;
                defenseMultiplier = 0.5f;
                accuracyBonus = -10f;
                evasionBonus = 0f;
                critChanceBonus = 0f;
                break;

            case MonsterType.Elite:
                hpMultiplier = 1.5f * EFFECTIVE_DPS_MULTIPLIER;
                attackMultiplier = 1.0f;
                defenseMultiplier = 1.0f;
                accuracyBonus = 0f;
                evasionBonus = 3f;
                critChanceBonus = 3f;
                break;

            case MonsterType.Boss:
                hpMultiplier = 3.5f * EFFECTIVE_DPS_MULTIPLIER;
                attackMultiplier = 1.3f;
                defenseMultiplier = 1.3f;
                accuracyBonus = 5f;
                evasionBonus = 5f;
                critChanceBonus = 8f;
                break;
        }

        int baseStr = 10 + (monsterData.level - 1) * 2 + monsterData.strBonus;
        int baseDex = 10 + (monsterData.level - 1) * 2 + monsterData.dexBonus;
        int baseInt = 10 + (monsterData.level - 1) * 2 + monsterData.intBonus;
        int baseLuk = 10 + (monsterData.level - 1) * 2 + monsterData.lukBonus;
        int baseTec = 10 + (monsterData.level - 1) * 2 + monsterData.tecBonus;

        stats.strength = baseStr;
        stats.dexterity = baseDex;
        stats.intelligence = baseInt;
        stats.luck = baseLuk;
        stats.technique = baseTec;

        int baseAttack = 5 + Mathf.FloorToInt(stats.strength * 0.35f) + Mathf.FloorToInt(stats.intelligence * 0.2f);
        int baseHP = 80 + ((monsterData.level - 1) * 10) + Mathf.FloorToInt(stats.strength * 1.2f);
        int baseDefense = 2 + Mathf.FloorToInt(stats.strength * 0.15f) + Mathf.FloorToInt(stats.intelligence * 0.1f);
        float baseAccuracy = 60 + Mathf.FloorToInt(stats.technique * 0.25f) + Mathf.FloorToInt(stats.luck * 0.15f);
        float baseEvasion = 2 + Mathf.FloorToInt(stats.dexterity * 0.2f) + Mathf.FloorToInt(stats.luck * 0.12f);
        float baseCrit = 2 + Mathf.FloorToInt(stats.luck * 0.2f) + Mathf.FloorToInt(stats.technique * 0.15f) + Mathf.FloorToInt(stats.intelligence * 0.08f);

        stats.attackPower = Mathf.RoundToInt(baseAttack * attackMultiplier);
        stats.maxHP = Mathf.RoundToInt(baseHP * hpMultiplier);
        stats.currentHP = stats.maxHP;
        stats.defense = Mathf.RoundToInt(baseDefense * defenseMultiplier);
        stats.accuracy = baseAccuracy + accuracyBonus;
        stats.evasionRate = baseEvasion + evasionBonus;
        stats.criticalChance = baseCrit + critChanceBonus;
        stats.criticalDamage = 150f;

        return stats;
    }

    public static int GetRecommendedPlayerCount(MonsterType type)
    {
        switch (type)
        {
            case MonsterType.Normal:
                return 1;
            case MonsterType.Elite:
                return 2;
            case MonsterType.Boss:
                return 4;
            default:
                return 1;
        }
    }
}