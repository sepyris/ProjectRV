using Definitions;

using System.Collections.Generic;
using UnityEngine;


/// 채집물 데이터를 저장하는 ScriptableObject

public class GatherableDataSO : ScriptableObject
{
    public List<GatherableData> Items = new List<GatherableData>();
}

/// 채집 도구 타입

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


/// 채집물 데이터

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