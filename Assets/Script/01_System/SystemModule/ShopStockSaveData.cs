using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// 상점 재고 저장 데이터 (캐릭터별)
///  임시 데이터 기능: 상점 거래는 임시 데이터에 기록되고, 세이브 시에만 실제 데이터에 반영
///  JsonUtility 호환: Dictionary 대신 List 사용

[System.Serializable]
public class ShopStockSaveData
{
    // ========== 실제 저장 데이터 (파일에 저장됨) ==========
    //  JsonUtility 호환을 위해 List로 변경
    public List<ShopItemPurchaseRecord> purchasedItems = new List<ShopItemPurchaseRecord>();
    public List<RebuyItemRecord> rebuyItems = new List<RebuyItemRecord>();

    // ========== 임시 데이터 (세이브 전까지만 유효, 파일에 저장 안됨) ==========
    [System.NonSerialized]
    private Dictionary<string, int> tempPurchasedItems = new Dictionary<string, int>();

    [System.NonSerialized]
    private Dictionary<string, int> tempRebuyItems = new Dictionary<string, int>();

    [System.NonSerialized]
    private bool hasUnsavedChanges = false;

    // ========== Dictionary 캐시 (빠른 접근용) ==========
    [System.NonSerialized]
    private Dictionary<string, int> purchasedDict = null;

    [System.NonSerialized]
    private Dictionary<string, int> rebuyDict = null;

    // ==================== 초기화 ====================

    
    /// Dictionary 캐시 초기화
    
    private void EnsureDictionaries()
    {
        if (purchasedDict == null)
        {
            purchasedDict = new Dictionary<string, int>();
            foreach (var record in purchasedItems)
            {
                purchasedDict[record.key] = record.quantity;
            }
        }

        if (rebuyDict == null)
        {
            rebuyDict = new Dictionary<string, int>();
            foreach (var record in rebuyItems)
            {
                rebuyDict[record.itemId] = record.quantity;
            }
        }
    }

    // ==================== 구매 관련 메서드 ====================

    
    /// 제한 재고 아이템 구매 기록 (임시 데이터에 저장)
    
    public void RecordPurchase(string shopId, string itemId, int quantity)
    {
        string key = $"{shopId}_{itemId}";

        if (tempPurchasedItems.ContainsKey(key))
        {
            tempPurchasedItems[key] += quantity;
        }
        else
        {
            // 실제 데이터의 현재 값을 임시 데이터에 복사
            EnsureDictionaries();
            int currentValue = purchasedDict.ContainsKey(key) ? purchasedDict[key] : 0;
            tempPurchasedItems[key] = currentValue + quantity;
        }

        hasUnsavedChanges = true;
        Debug.Log($"[ShopStock] 임시 구매 기록: {key} = {tempPurchasedItems[key]}개 (저장 안됨)");
    }

    
    /// 특정 아이템의 구매한 수량 가져오기 (임시 데이터 우선)
    
    public int GetPurchasedQuantity(string shopId, string itemId)
    {
        string key = $"{shopId}_{itemId}";

        // 임시 데이터가 있으면 임시 데이터 반환
        if (tempPurchasedItems.ContainsKey(key))
        {
            return tempPurchasedItems[key];
        }

        // 없으면 실제 데이터 반환
        EnsureDictionaries();
        return purchasedDict.ContainsKey(key) ? purchasedDict[key] : 0;
    }

    // ==================== 재매입 관련 메서드 ====================

    
    /// 재매입 아이템 추가 (임시 데이터에 저장)
    
    public void AddRebuyItem(string itemId, int quantity)
    {
        if (tempRebuyItems.ContainsKey(itemId))
        {
            tempRebuyItems[itemId] += quantity;
        }
        else
        {
            // 실제 데이터의 현재 값을 임시 데이터에 복사
            EnsureDictionaries();
            int currentValue = rebuyDict.ContainsKey(itemId) ? rebuyDict[itemId] : 0;
            tempRebuyItems[itemId] = currentValue + quantity;
        }

        hasUnsavedChanges = true;
        Debug.Log($"[ShopStock] 임시 재매입 추가: {itemId} = {tempRebuyItems[itemId]}개 (저장 안됨)");
    }

    
    /// 재매입 아이템 수량 감소 (임시 데이터에서)
    
    public void ReduceRebuyItem(string itemId, int quantity)
    {
        // 임시 데이터가 없으면 실제 데이터에서 복사
        if (!tempRebuyItems.ContainsKey(itemId))
        {
            EnsureDictionaries();
            int currentValue = rebuyDict.ContainsKey(itemId) ? rebuyDict[itemId] : 0;
            tempRebuyItems[itemId] = currentValue;
        }

        tempRebuyItems[itemId] -= quantity;

        if (tempRebuyItems[itemId] <= 0)
        {
            tempRebuyItems[itemId] = 0; // 음수 방지
        }

        hasUnsavedChanges = true;
        Debug.Log($"[ShopStock] 임시 재매입 감소: {itemId} = {tempRebuyItems[itemId]}개 (저장 안됨)");
    }

    
    /// 재매입 아이템 수량 가져오기 (임시 데이터 우선)
    
    public int GetRebuyQuantity(string itemId)
    {
        // 임시 데이터가 있으면 임시 데이터 반환
        if (tempRebuyItems.ContainsKey(itemId))
        {
            return tempRebuyItems[itemId];
        }

        // 없으면 실제 데이터 반환
        EnsureDictionaries();
        return rebuyDict.ContainsKey(itemId) ? rebuyDict[itemId] : 0;
    }

    // ==================== 임시 데이터 관리 ====================

    
    /// 임시 데이터를 실제 데이터에 커밋 (세이브 시 호출)
    
    public void CommitTempData()
    {
        if (!hasUnsavedChanges)
        {
            Debug.Log("[ShopStock] 저장할 변경사항이 없습니다.");
            return;
        }

        EnsureDictionaries();

        // 구매 데이터 커밋
        foreach (var kvp in tempPurchasedItems)
        {
            purchasedDict[kvp.Key] = kvp.Value;
        }

        // 재매입 데이터 커밋
        foreach (var kvp in tempRebuyItems)
        {
            if (kvp.Value > 0)
            {
                rebuyDict[kvp.Key] = kvp.Value;
            }
            else if (rebuyDict.ContainsKey(kvp.Key))
            {
                rebuyDict.Remove(kvp.Key);
            }
        }

        //  Dictionary를 List로 변환 (JsonUtility가 직렬화할 수 있도록)
        SyncDictionariesToLists();

        // 임시 데이터 초기화
        tempPurchasedItems.Clear();
        tempRebuyItems.Clear();
        hasUnsavedChanges = false;

        Debug.Log("[ShopStock] 임시 데이터를 실제 데이터에 커밋 완료!");
        Debug.Log($"[ShopStock] 저장될 데이터: 구매 {purchasedItems.Count}개, 재매입 {rebuyItems.Count}개");
    }

    
    /// Dictionary를 List로 동기화 (직렬화 전에 호출)
    
    private void SyncDictionariesToLists()
    {
        // 구매 데이터
        purchasedItems.Clear();
        foreach (var kvp in purchasedDict)
        {
            purchasedItems.Add(new ShopItemPurchaseRecord { key = kvp.Key, quantity = kvp.Value });
        }

        // 재매입 데이터
        rebuyItems.Clear();
        foreach (var kvp in rebuyDict)
        {
            rebuyItems.Add(new RebuyItemRecord { itemId = kvp.Key, quantity = kvp.Value });
        }
    }

    
    /// 임시 데이터 롤백 (저장 안하고 종료 시)
    
    public void RollbackTempData()
    {
        if (!hasUnsavedChanges)
        {
            return;
        }

        tempPurchasedItems.Clear();
        tempRebuyItems.Clear();
        hasUnsavedChanges = false;

        Debug.Log("[ShopStock] 임시 데이터 롤백 완료! (변경사항 취소됨)");
    }

    
    /// 저장되지 않은 변경사항이 있는지 확인
    
    public bool HasUnsavedChanges()
    {
        return hasUnsavedChanges;
    }

    
    /// 데이터 로드 후 초기화 (임시 데이터 클리어)
    
    public void OnDataLoaded()
    {
        tempPurchasedItems.Clear();
        tempRebuyItems.Clear();
        hasUnsavedChanges = false;

        // Dictionary 캐시 초기화
        purchasedDict = null;
        rebuyDict = null;
        EnsureDictionaries();

        Debug.Log($"[ShopStock] 데이터 로드 완료: 구매 {purchasedItems.Count}개, 재매입 {rebuyItems.Count}개");
    }
}

// ==================== 직렬화 가능한 데이터 구조 ====================


/// 상점 아이템 구매 기록 (JsonUtility 직렬화 가능)

[System.Serializable]
public class ShopItemPurchaseRecord
{
    public string key;       // "{shopId}_{itemId}"
    public int quantity;     // 구매한 수량
}


/// 재매입 아이템 기록 (JsonUtility 직렬화 가능)

[System.Serializable]
public class RebuyItemRecord
{
    public string itemId;    // 아이템 ID
    public int quantity;     // 재매입 가능 수량
}