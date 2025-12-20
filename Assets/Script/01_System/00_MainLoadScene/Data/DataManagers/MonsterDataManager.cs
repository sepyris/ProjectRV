using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 몬스터 데이터를 로드하고 관리하는 싱글톤 매니저
/// </summary>
public class MonsterDataManager : MonoBehaviour
{
    public static MonsterDataManager Instance { get; private set; }

    [Header("SO파일")]
    public MonsterDataSO monsterDatabaseSO;

    private readonly Dictionary<string, MonsterData> monsterDatabase = new();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (monsterDatabaseSO != null)
            {
                BuildDictionary(monsterDatabaseSO);
            }
            else
            {
                Debug.LogWarning("[MonsterDataManager] CSV 파일이 할당되지 않았습니다.");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void BuildDictionary(MonsterDataSO database)
    {
        monsterDatabase.Clear();
        foreach (var item in database.Items)
        {
            if (!monsterDatabase.ContainsKey(item.monsterid))
            {
                monsterDatabase.Add(item.monsterid, item);
            }
            else
            {
                Debug.LogWarning($"[ItemDataManager] 중복 id 발견 (SO): {item.monsterid}");
            }
        }
        Debug.Log($"[ItemDataManager] ScriptableObject에서 {monsterDatabase.Count}개의 아이템 로드 완료");
    }

    // ==========================================
    // 조회 메서드
    // ==========================================

    /// <summary>
    /// 몬스터 id로 데이터 가져오기
    /// </summary>
    public MonsterData GetMonsterData(string monsterId)
    {
        if (monsterDatabase.TryGetValue(monsterId, out MonsterData data))
        {
            return data;
        }

        Debug.LogWarning($"[MonsterDataManager] 몬스터를 찾을 수 없음: {monsterId}");
        return null;
    }

    /// <summary>
    /// 모든 몬스터 데이터 가져오기
    /// </summary>
    public Dictionary<string, MonsterData> GetAllMonsters()
    {
        return monsterDatabase;
    }

    /// <summary>
    /// 특정 타입의 몬스터만 가져오기
    /// </summary>
    public Dictionary<string, MonsterData> GetMonstersByType(MonsterType type)
    {
        var filteredMonsters = monsterDatabase
            .Where(kv => kv.Value.monsterType == type)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        return filteredMonsters;
    }

    /// <summary>
    /// 특정 레벨 범위의 몬스터 가져오기
    /// </summary>
    public Dictionary<string, MonsterData> GetMonstersByLevelRange(int minLevel, int maxLevel)
    {
        var filteredMonsters = monsterDatabase
            .Where(kv => kv.Value.level >= minLevel && kv.Value.level <= maxLevel)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        return filteredMonsters;
    }
}