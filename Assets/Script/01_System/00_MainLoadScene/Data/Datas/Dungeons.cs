using System;
using System.Collections.Generic;
using UnityEngine;



/// 던전 데이터를 저장하는 ScriptableObject

public class DungeonsDataSO : ScriptableObject
{
    public List<DungeonData> Items = new List<DungeonData>();
}


/// 시간 제한 타입

public enum TimeRestriction
{
    None,      // 제한 없음
    Night,     // 야간만 입장 가능
    Day        // 주간만 입장 가능
}


/// 던전 데이터 클래스

[Serializable]
public class DungeonData
{
    [Tooltip("던전 고유 ID")]
    public string dungeonId;

    [Tooltip("던전 이름")]
    public string dungeonName;

    [Tooltip("던전 설명")]
    public string description;

    [Tooltip("던전 이미지 경로")]
    public string dungeonImagePath;

    [Tooltip("입장할 맵 ID")]
    public string entryMapId;

    [Tooltip("권장 레벨")]
    public int recommendLevel;

    [Tooltip("시간 제한")]
    public TimeRestriction timeRestriction;

    [Tooltip("클리어 보상 아이템 리스트")]
    public List<ItemReward> clearRewardItems = new List<ItemReward>();
}