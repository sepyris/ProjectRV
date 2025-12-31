using System;
using UnityEngine;


/// 플레이어가 실제로 소유한 아이템 (인벤토리 슬롯)

[Serializable]
public class InventoryItem
{
    public string itemid;       // 아이템 id (ItemData 참조)
    public int quantity;        // 소유 개수
    public bool isEquipped;     // 장착 여부 (장비 아이템만 해당)

    [NonSerialized]
    private ItemData cachedData; // 캐시된 아이템 데이터

    // 편의 속성: 아이템 이름
    public string itemName
    {
        get
        {
            ItemData data = GetItemData();
            return data != null ? data.itemName : itemid;
        }
    }

    public InventoryItem(string id, int qty = 1)
    {
        itemid = id;
        quantity = qty;
        isEquipped = false;
    }
    
    /// 아이템 데이터 가져오기 (캐싱)
    
    public ItemData GetItemData()
    {
        if (cachedData == null)
        {
            if (ItemDataManager.Instance != null)
            {
                cachedData = ItemDataManager.Instance.GetItemData(itemid);
            }
        }
        return cachedData;
    }

    
    /// 아이템 추가 가능 여부 (스택)
    
    public bool CanStack(int amount = 1)
    {
        ItemData data = GetItemData();
        if (data == null) return false;

        return quantity + amount <= data.maxStack;
    }

    
    /// 아이템 개수 추가
    
    public int AddQuantity(int amount)
    {
        ItemData data = GetItemData();
        if (data == null) return 0;

        int maxAdd = data.maxStack - quantity;
        int actualAdd = Mathf.Min(amount, maxAdd);

        quantity += actualAdd;
        return actualAdd; // 실제로 추가된 개수 반환
    }

    
    /// 아이템 개수 감소
    
    public bool RemoveQuantity(int amount)
    {
        if (quantity >= amount)
        {
            quantity -= amount;
            return true;
        }
        return false;
    }

    
    /// 저장용 데이터로 변환
    
    public InventoryItemSaveData ToSaveData()
    {
        return new InventoryItemSaveData
        {
            itemid = this.itemid,
            quantity = this.quantity,
            isEquipped = this.isEquipped
        };
    }

    
    /// 저장 데이터에서 복원
    
    public static InventoryItem FromSaveData(InventoryItemSaveData data)
    {
        return new InventoryItem(data.itemid, data.quantity)
        {
            isEquipped = data.isEquipped
        };
    }
}


/// 인벤토리 아이템 저장 데이터

[Serializable]
public class InventoryItemSaveData
{
    public string itemid;
    public int quantity;
    public bool isEquipped;
}

