using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// 상점 데이터 관리 싱글톤

public class ShopDataManager : MonoBehaviour
{
    public static ShopDataManager Instance { get; private set; }

    [Header("ScriptableObject")]
    [SerializeField] private ShopDataSO shopDataSO;

    private Dictionary<string, ShopData> shopDatabase= new Dictionary<string, ShopData>();

    // ==========================================
    // 초기화 메서드
    // ==========================================

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (shopDataSO != null)
            {
                BuildDictionary(shopDataSO);
            }
            else
            {
                Debug.LogWarning("[ShopDataManager] SO data is not assigned.");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    
    /// 데이터베이스 초기화 및 재구축
    
    void BuildDictionary(ShopDataSO database)
    {
        shopDatabase.Clear();
        foreach (var item in database.Items)
        {
            if (!shopDatabase.ContainsKey(item.shopid))
            {
                shopDatabase.Add(item.shopid, item);
            }
            else
            {
                Debug.LogWarning($"[SkillDataManager] Duplicate id found (SO): {item.shopid}");
            }
        }
        Debug.Log($"[SkillDataManager] Loaded {shopDatabase.Count} skills from ScriptableObject.");
    }

    // ==========================================
    // 조회 메서드
    // ==========================================

    
    /// 해당 Shopid에 해당하는 ShopData를 반환합니다.
    
    public ShopData GetShopData(string shopid)
    {
        if (shopDatabase.TryGetValue(shopid, out ShopData data))
        {
            return data;
        }

        Debug.LogWarning($"[ShopDataManager] 상점 id '{shopid}'를 찾을 수 없습니다.");
        return null;
    }
}