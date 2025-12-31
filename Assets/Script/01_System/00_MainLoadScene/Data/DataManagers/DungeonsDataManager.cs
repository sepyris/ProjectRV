using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// 던전 데이터를 관리하는 싱글톤 매니저

public class DungeonsDataManager : MonoBehaviour
{
    public static DungeonsDataManager Instance { get; private set; }

    [Header("던전 데이터베이스")]
    public DungeonsDataSO DungeonDatabaseSO;

    private Dictionary<string, DungeonData> dungeonDatabase = new Dictionary<string, DungeonData>();

    // ==========================================
    // 초기화 메서드
    // ==========================================
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (DungeonDatabaseSO != null)
            {
                BuildDictionary(DungeonDatabaseSO);
            }
            else
            {
                Debug.LogError("[DungeonsDataManager] DungeonDatabaseSO가 할당되지 않았습니다.");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    
    /// 데이터베이스 초기화 및 재구축
    
    void BuildDictionary(DungeonsDataSO database)
    {
        dungeonDatabase.Clear();
        foreach (var dungeon in database.Items)
        {
            if (!dungeonDatabase.ContainsKey(dungeon.dungeonId))
            {
                dungeonDatabase.Add(dungeon.dungeonId, dungeon);
            }
            else
            {
                Debug.LogWarning($"[DungeonsDataManager] 중복된 던전 ID 발견: {dungeon.dungeonId}");
            }
        }
        Debug.Log($"[DungeonsDataManager] {dungeonDatabase.Count}개의 던전 데이터 로드 완료");
    }

    // ==========================================
    // 조회 메서드
    // ==========================================

    
    /// 던전 ID로 데이터 가져오기
    
    public DungeonData GetDungeonData(string dungeonId)
    {
        if (dungeonDatabase.TryGetValue(dungeonId, out DungeonData data))
        {
            return data;
        }

        Debug.LogWarning($"[DungeonsDataManager] 던전을 찾을 수 없음: {dungeonId}");
        return null;
    }

    
    /// 모든 던전 데이터 가져오기
    
    public Dictionary<string, DungeonData> GetAllDungeons()
    {
        return dungeonDatabase;
    }

    
    /// 특정 레벨 범위의 던전 가져오기
    
    public List<DungeonData> GetDungeonsByLevelRange(int minLevel, int maxLevel)
    {
        return dungeonDatabase.Values
            .Where(d => d.recommendLevel >= minLevel && d.recommendLevel <= maxLevel)
            .ToList();
    }

    
    /// 시간 제한별 던전 가져오기
    
    public List<DungeonData> GetDungeonsByTimeRestriction(TimeRestriction timeRestriction)
    {
        return dungeonDatabase.Values
            .Where(d => d.timeRestriction == timeRestriction)
            .ToList();
    }
}