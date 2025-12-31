using Definitions;
using System;
using System.Collections.Generic;
using UnityEngine;

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
    public string requiredJob;
    public int requiredLevel;
    public int maxLevel;
    public float cooldown;
    public float damageRate;
    public float levelUpDamageRate;
}
