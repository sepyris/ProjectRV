using Definitions;
using System.Collections.Generic;
using UnityEngine;

/// NPC 데이터를 저장하는 ScriptableObject
public class NPCInfoSO : ScriptableObject
{
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

    
    /// NPC 위치 정보를 포함한 설명
    
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
