using Definitions;
using GameData.Common;
using System.Collections.Generic;
using UnityEngine;

public class GatherableDataSO : ScriptableObject
{
    // Dictionary 대신 직렬화 가능한 List를 사용합니다. (Unity는 Dictionary를 Inspector에서 표시하지 못함)
    // 데이터 접근을 빠르게 하기 위해 런타임에 List를 Dictionary로 변환할 것입니다.
    public List<GatherableData> Items = new List<GatherableData>();
}
/// <summary>
/// 채집 도구 타입
/// </summary>
public enum GatherToolType
{
    None,       // 도구 불필요
    Pickaxe,    // 곡괭이
    Sickle,     // 낫
    FishingRod, // 낚시대
    Axe         // 도끼
}

public enum GatherType
{
    None,
    Gathering,
    Mining,
    Fishing
}

/// <summary>
/// 채집물 데이터
/// </summary>
[System.Serializable]
public class GatherableData
{
    public string gatherableid;        // id
    public string gatherableName;      // 이름
    public string description;         // 설명
    public GatherType gatherType;     // 채집 타입
    public GatherToolType requiredTool; // 필요한 채집 도구
    public float gatherTime;           // 채집 소요 시간 (초)
    public List<ItemReward> dropItems = new List<ItemReward>(); // 드랍 아이템 테이블
}