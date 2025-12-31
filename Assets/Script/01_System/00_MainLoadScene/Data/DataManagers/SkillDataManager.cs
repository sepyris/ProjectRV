using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// 스킬 데이터 관리 싱글톤

public class SkillDataManager : MonoBehaviour
{
    public static SkillDataManager Instance { get; private set; }
    [Header("SO Data")]
    public SkillDataSO skillDatabaseSO;

    private readonly Dictionary<string, SkillData> skillDatabase = new();
    // ==========================================
    // 초기화 메서드
    // ==========================================
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (skillDatabaseSO != null)
            {
                BuildDictionary(skillDatabaseSO);
            }
            else
            {
                Debug.LogWarning("[SkillDataManager] SO data is not assigned.");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// 데이터베이스 초기화 및 재구축
    
    void BuildDictionary(SkillDataSO database)
    {
        skillDatabase.Clear();
        foreach (var item in database.Items)
        {
            if (!skillDatabase.ContainsKey(item.skillId))
            {
                skillDatabase.Add(item.skillId, item);
            }
            else
            {
                Debug.LogWarning($"[SkillDataManager] Duplicate id found (SO): {item.skillId}");
            }
        }
        Debug.Log($"[SkillDataManager] Loaded {skillDatabase.Count} skills from ScriptableObject.");
    }

    // ==========================================
    // 조회 메서드
    // ==========================================

    
    /// 스킬 id로 데이터 가져오기
     
    public SkillData GetSkillData(string skillid)
    {
        if (skillDatabase.TryGetValue(skillid, out SkillData data))
        {
            return data;
        }
        Debug.LogWarning($"[SkillDataManager] Skill ID not found: {skillid}");
        return null;
    }

    
    /// 직업단위로 스킬 가져오기
    >
    public Dictionary<string, SkillData> GetJobSkills(string job)
    {
        var filteredSkills = skillDatabase
            .Where(kv => kv.Value.requiredJob == job)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        return filteredSkills;
    }
}