using Definitions;
using System.Collections.Generic;
using UnityEngine;


public class NPCInfoSO : ScriptableObject
{
    // Dictionary 대신 직렬화 가능한 List를 사용합니다. (Unity는 Dictionary를 Inspector에서 표시하지 못함)
    // 데이터 접근을 빠르게 하기 위해 런타임에 List를 Dictionary로 변환할 것입니다.
    public List<Npcs> Items = new List<Npcs>();
}

[System.Serializable]
public class Npcs
{
    public string npcId;
    public string npcName;
    public string npcTitle;
    public string npcDescription;

    //  위치 정보 추가 
    public string mapId;         // NPC가 있는 맵
    public Vector2 position;     // 맵 내 위치 (posX, posY)

    /// <summary>
    /// NPC 위치 정보를 포함한 설명
    /// </summary>
    public string GetLocationDescription()
    {
        if (MapInfoManager.Instance != null && !string.IsNullOrEmpty(mapId))
        {
            string mapName = MapInfoManager.Instance.GetMapName(mapId);
            return $"{mapName}에 위치";
        }
        return "위치 정보 없음";
    }
}
