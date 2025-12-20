using System.Collections.Generic;
using UnityEngine;
using System.Linq;



public class MapInfoManager : MonoBehaviour
{
    public static MapInfoManager Instance { get; private set; }

    [Header("CSV 파일")]
    public MapInfoSO mapInfoDatabaseSO;

    [Header("현재 맵 정보")]
    public string currentMapId = "map_town_center";

    private readonly Dictionary<string, Maps> mapInfoDictionary = new();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (mapInfoDatabaseSO != null)
            {
                BuildDictionary(mapInfoDatabaseSO);
            }
            else
            {
                Debug.LogError("[MapInfoManager] CSV 파일이 할당되지 않았습니다.");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void BuildDictionary(MapInfoSO database)
    {
        mapInfoDictionary.Clear();
        foreach (var item in database.Items)
        {
            if (!mapInfoDictionary.ContainsKey(item.mapId))
            {
                mapInfoDictionary.Add(item.mapId, item);
            }
            else
            {
                Debug.LogWarning($"[ItemDataManager] 중복 id 발견 (SO): {item.mapId}");
            }
        }
        Debug.Log($"[ItemDataManager] ScriptableObject에서 {mapInfoDictionary.Count}개의 아이템 로드 완료");
    }




    /// <summary>
    /// 맵 id로 맵 이름 가져오기
    /// </summary>
    public string GetMapName(string mapId)
    {
        if (mapId != null)
        {
            if (mapInfoDictionary.TryGetValue(mapId, out Maps info))
            {
                return info.mapName;
            }
        }

        Debug.LogWarning($"[MapInfoManager] 맵 정보를 찾을 수 없음: {mapId}");
        return mapId;
    }

    /// <summary>
    /// 맵 id로 Unity 씬 이름 생성하기
    /// 형식: Map_{type}_{mapid}
    /// 예: mapId="town_center", mapType="Town" → "Map_Town_town_center"
    /// </summary>
    public string GetSceneName(string mapId)
    {
        if(mapId != null)
        {
            if (mapInfoDictionary.TryGetValue(mapId, out Maps info))
            {
                // Map_{type}_{mapid} 형식으로 씬 이름 생성
                string sceneName = $"Map_{info.mapType}_{mapId}";
                Debug.Log($"[MapInfoManager] 씬 이름 생성: {mapId} → {sceneName}");
                return sceneName;
            }
        }
        Debug.LogWarning($"[MapInfoManager] 맵 정보를 찾을 수 없어 씬 이름 생성 실패: {mapId}");
        return null;
    }

    /// <summary>
    /// 맵 id로 전체 정보 가져오기
    /// </summary>
    public Maps GetMapInfo(string mapId)
    {
        if (mapInfoDictionary.TryGetValue(mapId, out Maps info))
        {
            return info;
        }
        Debug.LogWarning($"[MapInfoManager] 맵 정보를 찾을 수 없음: {mapId}");
        return null;
    }

    public void SetCurrentMap(string mapId)
    {
        currentMapId = mapId;
    }

    /// <summary>
    /// 현재 맵 이름 가져오기
    /// </summary>
    public string GetCurrentMapName()
    {
        return GetMapName(currentMapId);
    }

    /// <summary>
    /// 맵 변경 (씬 전환 시 호출)
    /// </summary>
    public void ChangeMap(string newMapId)
    {
        if (mapInfoDictionary.ContainsKey(newMapId))
        {
            string oldMapName = GetMapName(currentMapId);
            currentMapId = newMapId;
            string newMapName = GetMapName(currentMapId);

            Debug.Log($"[MapInfoManager] 맵 이동: {oldMapName} → {newMapName}");
        }
        else
        {
            Debug.LogWarning($"[MapInfoManager] 존재하지 않는 맵: {newMapId}");
        }
    }

    /// <summary>
    /// 모든 맵 목록 가져오기 (특정 타입)
    /// </summary>
    public Dictionary<string, Maps> GetMapsByType(string mapType)
    {
        var filteredMaps = mapInfoDictionary
            .Where(kv => kv.Value.mapType == mapType)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        return filteredMaps;
    }

    /// <summary>
    /// 두 맵 간의 경로 찾기 (간단한 계층 구조 기반)
    /// </summary>
    public List<string> FindPathBetweenMaps(string fromMapId, string toMapId)
    {
        List<string> path = new();

        // 현재는 간단한 구현 - 나중에 A* 알고리즘 등으로 개선 가능
        Maps fromMap = GetMapInfo(fromMapId);
        Maps toMap = GetMapInfo(toMapId);

        if (fromMap == null || toMap == null)
        {
            Debug.LogWarning($"[MapInfoManager] 경로 탐색 실패: {fromMapId} → {toMapId}");
            return path;
        }

        // 같은 맵이면 경로 없음
        if (fromMapId == toMapId)
        {
            return path;
        }

        // 간단한 구현: 부모 맵을 거쳐가는 경로
        path.Add(fromMapId);

        // fromMap에서 공통 부모로 올라가기
        string currentId = fromMapId;
        while (!string.IsNullOrEmpty(currentId))
        {
            Maps current = GetMapInfo(currentId);
            if (current == null) break;

            if (!string.IsNullOrEmpty(current.parentMapId))
            {
                path.Add(current.parentMapId);
                currentId = current.parentMapId;
            }
            else
            {
                break;
            }
        }

        // toMap까지의 경로 추가 (역순)
        List<string> toPath = new();
        currentId = toMapId;
        while (!string.IsNullOrEmpty(currentId))
        {
            Maps current = GetMapInfo(currentId);
            if (current == null) break;

            toPath.Insert(0, currentId);

            if (!string.IsNullOrEmpty(current.parentMapId))
            {
                currentId = current.parentMapId;
            }
            else
            {
                break;
            }
        }

        // 공통 부모 찾아서 중복 제거
        // (실제로는 더 정교한 알고리즘 필요)
        foreach (var mapId in toPath)
        {
            if (!path.Contains(mapId))
            {
                path.Add(mapId);
            }
        }

        return path;
    }
}