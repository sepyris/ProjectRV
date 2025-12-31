using System.Collections.Generic;
using UnityEngine;


/// NPC 데이터 관리 싱글톤

public class NPCInfoManager : MonoBehaviour
{
    public static NPCInfoManager Instance { get; private set; }

    [Header("CSV 파일")]
    public NPCInfoSO npdDataBaseSO;

    private readonly Dictionary<string, Npcs> npcInfoDictionary = new();
    // ==========================================
    // 초기화 메서드
    // ==========================================
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (npdDataBaseSO != null)
            {

                BuildDictionary(npdDataBaseSO);
            }
            else
            {
                Debug.LogWarning("[NPCInfoManager] CSV 파일이 할당되지 않았습니다.");
            }

        }
        else
        {
            Destroy(gameObject);
        }
    }

    
    /// 데이터베이스 초기화 및 재구축
    
    void BuildDictionary(NPCInfoSO database)
    {
        npcInfoDictionary.Clear();
        foreach (var item in database.Items)
        {
            if (!npcInfoDictionary.ContainsKey(item.npcId))
            {
                npcInfoDictionary.Add(item.npcId, item);
            }
            else
            {
                Debug.LogWarning($"[ItemDataManager] 중복 id 발견 (SO): {item.npcId}");
            }
        }
        Debug.Log($"[ItemDataManager] ScriptableObject에서 {npcInfoDictionary.Count}개의 아이템 로드 완료");
    }

    // ==========================================
    // 조회 메서드
    // ==========================================

    
    /// NPC id로 이름 가져오기
    
    public string GetNPCName(string npcId)
    {
        if (npcInfoDictionary.TryGetValue(npcId, out Npcs data))
        {
            return data.npcName;
        }

        Debug.LogWarning($"[NPCInfoManager] NPC를 찾을 수 없음: {npcId}");
        return null;
    }

    
    /// NPC id로 전체 정보 가져오기
    
    public Npcs GetNPCInfo(string npcId)
    {
        if (npcInfoDictionary.TryGetValue(npcId, out Npcs data))
        {
            return data;
        }

        Debug.LogWarning($"[NPCInfoManager] NPC를 찾을 수 없음: {npcId}");
        return null;
    }
}