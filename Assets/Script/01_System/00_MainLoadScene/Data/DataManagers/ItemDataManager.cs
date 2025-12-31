using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


/// 아이템 데이터 관리 싱글톤

public class ItemDataManager : MonoBehaviour
{
    public static ItemDataManager Instance { get; private set; }

    [Header("CSV 파일")]
    public ItemDataSO ItemDatabaseSO;

    private Dictionary<string, ItemData> itemDatabase = new Dictionary<string, ItemData>();
    // ==========================================
    // 초기화 메서드
    // ==========================================
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (ItemDatabaseSO != null)
            {
                BuildDictionary(ItemDatabaseSO);
            }
            else
            {
                Debug.LogError("[DialogueDataManager] CSV 파일이 할당되지 않았습니다.");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    
    /// 데이터베이스 초기화 및 재구축
    
    void BuildDictionary(ItemDataSO database)
    {
        itemDatabase.Clear();
        foreach (var item in database.Items)
        {
            if (!itemDatabase.ContainsKey(item.itemId))
            {
                itemDatabase.Add(item.itemId, item);
            }
            else
            {
                Debug.LogWarning($"[ItemDataManager] 중복 id 발견 (SO): {item.itemId}");
            }
        }
        Debug.Log($"[ItemDataManager] ScriptableObject에서 {itemDatabase.Count}개의 아이템 로드 완료");
    }


    // ==========================================
    // 조회 메서드
    // ==========================================
    
    /// 아이템 id로 데이터 가져오기
    
    public ItemData GetItemData(string itemId)
    {
        if (itemDatabase.TryGetValue(itemId, out ItemData data))
        {
            return data;
        }

        Debug.LogWarning($"[ItemDataManager] 아이템을 찾을 수 없음: {itemId}");
        return null;
    }

    
    /// 모든 아이템 데이터 가져오기
    
    public Dictionary<string, ItemData> GetAllItems()
    {
        return itemDatabase;
    }

    
    /// 특정 타입의 아이템만 가져오기
    
    public Dictionary<string, ItemData> GetItemsByType(ItemType type)
    {
        var filteredItems = itemDatabase
            .Where(kv => kv.Value.itemType == type)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        return filteredItems;
    }
}