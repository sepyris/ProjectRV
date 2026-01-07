using Definitions;
using System;
using System.Collections.Generic;
using UnityEngine;

/// 스킬 데이터를 저장하는 ScriptableObject
public class SkillDataSO : ScriptableObject
{
    public List<SkillData> Items = new List<SkillData>();
}
[System.Serializable]
public class SkillData
{
    public string skillId;
    public string skillName;
    public string description;
    public SkillType skillType;
    public string requiredJob;
    public int requiredLevel;
    public int maxLevel;
    public float cooldown;
    public float damageRate;
    public float levelUpDamageRate;
    public string skillIconPath;

    // 유틸리티 메서드
    public float GetDamageAtLevel(int level)
    {
        if (level <= 1) return damageRate;
        return damageRate + (levelUpDamageRate * (level - 1));
    }
}
public enum SkillType
{
    Active,      // 액티브 스킬
    Passive,     // 패시브 스킬
    None
}


