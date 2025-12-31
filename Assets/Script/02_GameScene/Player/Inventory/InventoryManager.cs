using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// 플레이어 인벤토리 관리 싱글톤
/// Collect 타입 퀘스트와 자동 연동

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("인벤토리 설정")]
    [SerializeField] public int maxSlots = 300;

    private List<InventoryItem> items = new List<InventoryItem>();

    // 이벤트
    public event Action<InventoryItem> OnItemAdded;
    public event Action<InventoryItem> OnItemRemoved;
    public event Action<InventoryItem> OnItemUsed;
    public event Action OnInventoryChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ===== 아이템 추가 =====

    
    /// 아이템 추가
    
    public bool AddItem(string itemid, int quantity = 1)
    {
        ItemData data = ItemDataManager.Instance?.GetItemData(itemid);
        if (data == null)
        {
            Debug.LogError($"[Inventory] 존재하지 않는 아이템: {itemid}");
            return false;
        }

        int remainingQty = quantity;

        // 스택 가능한 아이템인 경우 기존 슬롯에 추가
        if (data.maxStack > 1)
        {
            foreach (var item in items)
            {
                if (item.itemid == itemid && item.CanStack(remainingQty))
                {
                    int added = item.AddQuantity(remainingQty);
                    remainingQty -= added;

                    if (remainingQty <= 0)
                    {
                        OnItemAdded?.Invoke(item);
                        OnInventoryChanged?.Invoke();

                        //  퀘스트 업데이트 (Collect & Gather) 
                        UpdateQuestProgress(itemid, quantity);

                        Debug.Log($"[Inventory] {data.itemName} x{quantity} 추가됨 (기존 스택)");
                        return true;
                    }
                }
            }
        }

        // 새 슬롯에 추가
        while (remainingQty > 0)
        {
            if (items.Count >= maxSlots)
            {
                Debug.LogWarning("[Inventory] 인벤토리가 가득 찼습니다.");
                return false;
            }

            int stackSize = Mathf.Min(remainingQty, data.maxStack);
            InventoryItem newItem = new InventoryItem(itemid, stackSize);
            items.Add(newItem);
            remainingQty -= stackSize;

            OnItemAdded?.Invoke(newItem);
        }

        OnInventoryChanged?.Invoke();

        //  퀘스트 업데이트 (Collect & Gather) 
        UpdateQuestProgress(itemid, quantity);

        Debug.Log($"[Inventory] {data.itemName} x{quantity} 추가됨 (새 슬롯)");
        return true;
    }

    // ===== 아이템 제거 =====

    
    /// 아이템 제거
    
    public bool RemoveItem(string itemid, int quantity = 1)
    {
        int totalQty = GetItemQuantity(itemid);
        if (totalQty < quantity)
        {
            Debug.LogWarning($"[Inventory] 아이템 부족: {itemid} (보유: {totalQty}, 필요: {quantity})");
            return false;
        }

        int remainingQty = quantity;

        for (int i = items.Count - 1; i >= 0 && remainingQty > 0; i--)
        {
            if (items[i].itemid == itemid)
            {
                int removeQty = Mathf.Min(remainingQty, items[i].quantity);
                items[i].RemoveQuantity(removeQty);
                remainingQty -= removeQty;

                if (items[i].quantity <= 0)
                {
                    InventoryItem removedItem = items[i];
                    items.RemoveAt(i);
                    OnItemRemoved?.Invoke(removedItem);
                }
            }
        }

        OnInventoryChanged?.Invoke();

        //  Collect 타입 퀘스트 갱신 (아이템이 줄어들었으므로) 
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.RefreshAllCollectObjectives();
        }

        Debug.Log($"[Inventory] {itemid} x{quantity} 제거됨");
        return true;
    }

    
    /// 퀘스트 진행 상황 업데이트
    
    private void UpdateQuestProgress(string itemid, int quantity)
    {
        if (QuestManager.Instance != null)
        {
            // Collect: 현재 인벤토리 개수로 업데이트
            // Gather: 획득한 개수만큼 증가
            QuestManager.Instance.UpdateItemProgress(itemid, quantity);
        }
    }

    // ===== 아이템 사용 =====

    
    /// 아이템 사용
    
    public bool UseItem(string itemid, CharacterStats stats = null)
    {
        InventoryItem item = items.FirstOrDefault(i => i.itemid == itemid);
        if (item == null)
        {
            Debug.LogWarning($"[Inventory] 아이템을 찾을 수 없음: {itemid}");
            return false;
        }

        ItemData data = item.GetItemData();
        if (data == null) return false;

        // 아이템 효과 적용
        bool used = ApplyItemEffect(data, stats);

        if (used)
        {
            OnItemUsed?.Invoke(item);

            // 소비 아이템은 개수 감소
            if (data.itemType == ItemType.Consumable)
            {
                RemoveItem(itemid, 1);
            }

            Debug.Log($"[Inventory] {data.itemName} 사용됨");
        }

        return used;
    }

    
    /// 아이템 효과 적용
    
    private bool ApplyItemEffect(ItemData data, CharacterStats stats)
    {
        switch (data.itemType)
        {
            case ItemType.Consumable:
                if (stats != null)
                {
                    // 체력 회복
                    if (data.GetHealAmount() > 0)
                    {
                        //stats.SetDamageTextSpawner(PlayerStatsComponent.Instance.Stats.SetDamageTextSpawner(PlayerController.Instance.GetComponent<DamageTextSpawner>()))
                        stats.Heal(data.GetHealAmount());
                        Debug.Log($"[Inventory] HP +{data.GetHealAmount()}");
                    }

                    return true;
                }
                break;

            case ItemType.Equipment:
                // 장비 장착/해제 로직
                Debug.Log($"[Inventory] 장비 장착: {data.itemName}");
                return true;

            default:
                Debug.LogWarning($"[Inventory] 사용할 수 없는 아이템: {data.itemName}");
                return false;
        }

        return false;
    }

    // ===== 조회 =====

    
    /// 아이템 소유 개수 확인
    
    public int GetItemQuantity(string itemid)
    {
        return items.Where(i => i.itemid == itemid).Sum(i => i.quantity);
    }

    
    /// 아이템 소유 여부 확인
    
    public bool HasItem(string itemid, int quantity = 1)
    {
        return GetItemQuantity(itemid) >= quantity;
    }

    
    /// 모든 아이템 가져오기
    
    public List<InventoryItem> GetAllItems()
    {
        return new List<InventoryItem>(items);
    }

    
    /// 특정 타입의 아이템만 가져오기
    
    public List<InventoryItem> GetItemsByType(ItemType type)
    {
        return items.Where(i => i.GetItemData()?.itemType == type).ToList();
    }

    // ===== 저장/로드 =====

    
    /// 인벤토리 데이터 저장
    
    public InventorySaveData ToSaveData()
    {
        return new InventorySaveData
        {
            items = items.Select(i => i.ToSaveData()).ToList()
        };
    }

    
    /// 인벤토리 데이터 로드
    
    public void LoadFromData(InventorySaveData data)
    {
        items.Clear();

        if (data != null && data.items != null)
        {
            foreach (var itemData in data.items)
            {
                items.Add(InventoryItem.FromSaveData(itemData));
            }
        }

        OnInventoryChanged?.Invoke();

        //  로드 후 Collect 타입 퀘스트 갱신 
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.RefreshAllCollectObjectives();
        }

        Debug.Log($"[Inventory] 데이터 로드 완료 ({items.Count}개 아이템)");
    }

    
    /// 인벤토리 초기화 (새 게임)
    
    /// 
    public void ClearInventory()
    {
        items.Clear();
        OnInventoryChanged?.Invoke();
        Debug.Log("[Inventory] 인벤토리 초기화됨");
    }

    // ===== 디버그 =====
    public void DebugPrintInventory()
    {
        Debug.Log($"===== 인벤토리 ({items.Count}/{maxSlots}) =====");
        foreach (var item in items)
        {
            ItemData data = item.GetItemData();
            string name = data != null ? data.itemName : item.itemid;
            Debug.Log($"- {name} x{item.quantity}");
        }
    }
    
    /// 특정 아이템 ID로 아이템 가져오기 (첫 번째 매칭 아이템)
    
    public InventoryItem GetItem(string itemId)
    {
        return items.FirstOrDefault(i => i.itemid == itemId);
    }

    
    /// 특정 아이템 ID의 모든 아이템 가져오기
    
    public List<InventoryItem> GetItems(string itemId)
    {
        return items.Where(i => i.itemid == itemId).ToList();
    }

    
    /// 사용 가능한 인벤토리 슬롯 수 반환
    
    public int GetAvailableSlots()
    {
        return maxSlots - items.Count;
    }
}


/// 인벤토리 저장 데이터

[Serializable]
public class InventorySaveData
{
    public List<InventoryItemSaveData> items = new List<InventoryItemSaveData>();
}