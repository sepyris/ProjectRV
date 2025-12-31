
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// 채집물 데이터를 관리하는 싱글톤 매니저

public class GatherableDataManager : MonoBehaviour
{
    public static GatherableDataManager Instance { get; private set; }

    [Header("CSV 파일")]
    public GatherableDataSO GatherableDatabaseSO;

    public Dictionary<string,GatherableData> gatherableDatabase = new();

    // ==========================================
    // 초기화 메서드
    // ==========================================
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (GatherableDatabaseSO != null)
            {
                BuildDictionary(GatherableDatabaseSO);
            }
            else
            {
                Debug.LogError("[GatherableDataManager] CSV 파일이 할당되지 않았습니다.");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    
    /// 데이터베이스 초기화 및 재구축
    
    void BuildDictionary(GatherableDataSO database)
    {
        gatherableDatabase.Clear();
        foreach (var item in database.Items)
        {
            if (!gatherableDatabase.ContainsKey(item.gatherableid))
            {
                gatherableDatabase.Add(item.gatherableid, item);
            }
            else
            {
                Debug.LogWarning($"[ItemDataManager] 중복 id 발견 (SO): {item.gatherableid}");
            }
        }
        Debug.Log($"[ItemDataManager] ScriptableObject에서 {gatherableDatabase.Count}개의 아이템 로드 완료");
    }
    
    

    // ==========================================
    // 조회 메서드
    // ==========================================

    
    /// 채집물 id로 데이터 가져오기
    
    public GatherableData GetGatherableData(string gatherableId)
    {
        if (gatherableDatabase.TryGetValue(gatherableId, out GatherableData data))
        {
            return data;
        }

        Debug.LogWarning($"[GatherableDataManager] 채집물을 찾을 수 없음: {gatherableId}");
        return null;
    }

    
    /// 모든 채집물 데이터 가져오기
    
    public Dictionary<string, GatherableData> GetAllGatherables()
    {
        return gatherableDatabase;
    }

    
    /// 특정 도구가 필요한 채집물만 가져오기
    
    public Dictionary<string, GatherableData> GetGatherablesByTool(GatherToolType tool)
    {
        var filteredGatherables = gatherableDatabase
            .Where(kv => kv.Value.requiredTool == tool)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        return filteredGatherables;
    }
}