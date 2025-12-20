using Definitions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MapInfoSO : ScriptableObject
{
    // Dictionary 대신 직렬화 가능한 List를 사용합니다. (Unity는 Dictionary를 Inspector에서 표시하지 못함)
    // 데이터 접근을 빠르게 하기 위해 런타임에 List를 Dictionary로 변환할 것입니다.
    public List<Maps> Items = new List<Maps>();
}

[System.Serializable]
public class Maps
{
    public string mapId;
    public string mapName;
    public string mapType;          // Town, Forest, Dungeon, etc.
    public string mapRecommendedLevel;
    public string parentMapId;      // 상위 맵 (계층 구조용)

    // 런타임 데이터 (나중에 추가)
    public Vector3 spawnPoint;      // 맵 진입 위치
    public List<string> connectedMaps = new List<string>(); // 연결된 맵들
}

public enum MapType
{
    Town,
    Field,
    Dungeon
}