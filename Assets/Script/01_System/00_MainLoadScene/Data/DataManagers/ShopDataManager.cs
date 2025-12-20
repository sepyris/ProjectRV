using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopDataManager : MonoBehaviour
{
    public static ShopDataManager Instance { get; private set; }

    [Header("ScriptableObject")]
    [SerializeField] private ShopDataSO shopDataSO;

    private Dictionary<string, ShopData> shopDictionary = new Dictionary<string, ShopData>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadShopData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// ShopDataSO에서 데이터를 로드하여 Dictionary에 저장합니다.
    /// </summary>
    void LoadShopData()
    {
        if (shopDataSO == null)
        {
            Debug.LogError("[ShopDataManager] ShopDataSO가 할당되지 않았습니다!");
            return;
        }

        shopDictionary.Clear();

        foreach (ShopData shopData in shopDataSO.Items)
        {
            shopDictionary[shopData.shopid] = shopData;
        }

        Debug.Log($"[ShopDataManager] 상점 데이터 로드 완료: {shopDictionary.Count}개의 상점");
    }

    /// <summary>
    /// 특정 Shopid에 해당하는 ShopData를 반환합니다.
    /// </summary>
    public ShopData GetShopData(string shopid)
    {
        if (shopDictionary.TryGetValue(shopid, out ShopData data))
        {
            return data;
        }

        Debug.LogWarning($"[ShopDataManager] 상점 id '{shopid}'를 찾을 수 없습니다.");
        return null;
    }

    /// <summary>
    /// 모든 상점 id 목록을 반환합니다.
    /// </summary>
    public List<string> GetAllShopids()
    {
        return shopDictionary.Keys.ToList();
    }

    /// <summary>
    /// 데이터 검증 (SystemInitializer에서 호출)
    /// </summary>
    public bool ValidateData()
    {
        if (shopDataSO == null)
        {
            Debug.LogError("[ShopDataManager] ShopDataSO가 할당되지 않았습니다!");
            return false;
        }

        if (shopDataSO.Items == null || shopDataSO.Items.Count == 0)
        {
            Debug.LogError("[ShopDataManager] ShopDataSO에 상점 데이터가 없습니다!");
            return false;
        }

        // 각 상점의 아이템 id 검증
        foreach (ShopData shop in shopDataSO.Items)
        {
            foreach (ShopItemData item in shop.items)
            {
                // ItemDataManager를 통해 아이템 존재 확인
                // 실제 ItemDataManager 구조에 맞게 수정 필요
                // ItemData itemData = ItemDataManager.Instance?.GetItemData(item.itemid);
                // if (itemData == null)
                // {
                //     Debug.LogWarning($"[ShopDataManager] 상점 '{shop.shopid}'에 존재하지 않는 아이템 id '{item.itemid}'가 있습니다!");
                // }
            }
        }

        Debug.Log($"[ShopDataManager] 데이터 검증 완료: {shopDataSO.Items.Count}개의 상점");
        return true;
    }
}