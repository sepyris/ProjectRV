using Definitions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// 맵 데이터를 저장하는 ScriptableObject
public class MapInfoSO : ScriptableObject
{
    public List<Maps> Items = new List<Maps>();
}

[System.Serializable]
public class Maps
{
    public string mapId;
    public string mapName;
    public string mapType;          // Town, Field, Dungeon
    public string mapRecommendedLevel;
    public Vector3 spawnPoint;      // 맵 진입 위치

    // 네비게이션을 위한 데이터(현재는 사용 안함)
    public string parentMapId;      // 상위 맵 (계층 구조용)
    public List<string> connectedMaps = new List<string>(); // 연결된 맵들
}

public enum MapType
{
    Town,
    Field,
    Dungeon
}