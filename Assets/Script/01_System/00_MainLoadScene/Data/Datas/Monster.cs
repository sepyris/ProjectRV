using Definitions;
using GameData.Common;
using System.Collections.Generic;
using UnityEngine;

public class MonsterDataSO : ScriptableObject
{
    public List<MonsterData> Items = new List<MonsterData>();
}

public enum MonsterType
{
    Normal,
    Elite,
    Boss
}

[System.Serializable]
public class MonsterData
{
    [Header("기본 정보")]
    public string monsterid;
    public string monsterName;
    public string description;
    public int level;
    public MonsterType monsterType;

    [Header("AI 설정")]
    public bool isAggressive;
    public bool isRanged;
    public float attackSpeed;
    public float moveSpeed;
    public float detectionRange;

    [Header("스탯 보너스 (선택)")]
    public int strBonus;
    public int dexBonus;
    public int intBonus;
    public int lukBonus;
    public int tecBonus;

    [Header("드롭")]
    public int dropExp;
    public int dropGold;
    public List<ItemReward> dropItems = new List<ItemReward>();
}