using System;
using System.Collections.Generic;
using UnityEngine;


/// 플레이어 장비 관리 싱글톤
/// 장비 장착/해제 및 스탯 적용 담당
/// 
///  개선사항: 
/// 1. 장착된 아이템은 인벤토리에서 제거되어 슬롯을 차지하지 않음
/// 2. 장비 스탯을 누적 계산하여 정확하게 적용
/// 3. GetTotalStatBonus() 메서드를 재활용하여 코드 중복 제거
/// 4. 장비 정보 저장/로드 기능 추가

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    // 장비 슬롯 (실제 능력치 적용)
    private Dictionary<EquipmentSlot, InventoryItem> equippedItems = new Dictionary<EquipmentSlot, InventoryItem>();

    // 치장 슬롯 (외형만 적용)
    private Dictionary<CosmeticSlot, InventoryItem> cosmeticItems = new Dictionary<CosmeticSlot, InventoryItem>();

    // 이벤트
    public event Action<EquipmentSlot, InventoryItem> OnEquipmentChanged;
    public event Action<CosmeticSlot, InventoryItem> OnCosmeticChanged;
    public event Action OnEquipmentStatsChanged;

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

    // ==================== 장비 장착/해제 ====================

    
    /// 장비 아이템 장착
    ///  개선: 인벤토리에서 아이템을 제거하여 슬롯 확보
    
    public bool EquipItem(string itemId)
    {
        ItemData data = ItemDataManager.Instance?.GetItemData(itemId);
        if (data == null || data.itemType != ItemType.Equipment)
        {
            Debug.LogWarning($"[Equipment] 장착할 수 없는 아이템: {itemId}");
            return false;
        }

        // 인벤토리에서 아이템 찾기
        InventoryItem inventoryItem = InventoryManager.Instance?.GetItem(itemId);
        if (inventoryItem == null)
        {
            Debug.LogWarning($"[Equipment] 인벤토리에 아이템이 없음: {itemId}");
            return false;
        }

        // 이미 장착된 아이템은 장착 불가
        if (inventoryItem.isEquipped)
        {
            Debug.LogWarning($"[Equipment] 이미 장착된 아이템: {itemId}");
            return false;
        }

        // 치장 아이템인 경우
        if (data.isCosmetic)
        {
            return EquipCosmetic(inventoryItem, data);
        }

        // 일반 장비 아이템
        EquipmentSlot slot = data.equipSlot;
        if (slot == EquipmentSlot.None)
        {
            Debug.LogWarning($"[Equipment] 유효하지 않은 장비 슬롯: {itemId}");
            return false;
        }

        //  인벤토리에서 먼저 아이템 제거 (슬롯 확보) 
        // 이렇게 해야 인벤토리가 꽉 찼을 때도 장비 교체 가능
        bool removed = InventoryManager.Instance?.RemoveItem(itemId, 1) ?? false;
        if (!removed)
        {
            Debug.LogWarning($"[Equipment] 인벤토리에서 아이템 제거 실패: {itemId}");
            return false;
        }

        //  무기 슬롯은 상호 배타적 처리 (근거리/원거리 동시 장착 불가)
        if (slot == EquipmentSlot.MeleeWeapon || slot == EquipmentSlot.RangedWeapon)
        {
            // 근거리 무기 장착 시도 → 원거리 무기가 있으면 해제
            if (slot == EquipmentSlot.MeleeWeapon && equippedItems.ContainsKey(EquipmentSlot.RangedWeapon))
            {
                if (!UnequipItem(EquipmentSlot.RangedWeapon))
                {
                    // 해제 실패 시 새 아이템을 다시 인벤토리에 반환
                    InventoryManager.Instance?.AddItem(itemId, 1);
                    Debug.LogWarning($"[Equipment] 기존 무기 해제 실패로 장착 취소: {itemId}");
                    return false;
                }
            }
            // 원거리 무기 장착 시도 → 근거리 무기가 있으면 해제
            else if (slot == EquipmentSlot.RangedWeapon && equippedItems.ContainsKey(EquipmentSlot.MeleeWeapon))
            {
                if (!UnequipItem(EquipmentSlot.MeleeWeapon))
                {
                    // 해제 실패 시 새 아이템을 다시 인벤토리에 반환
                    InventoryManager.Instance?.AddItem(itemId, 1);
                    Debug.LogWarning($"[Equipment] 기존 무기 해제 실패로 장착 취소: {itemId}");
                    return false;
                }
            }
        }

        // 기존 장비가 있으면 해제 (같은 슬롯)
        if (equippedItems.ContainsKey(slot))
        {
            if (!UnequipItem(slot))
            {
                // 해제 실패 시 새 아이템을 다시 인벤토리에 반환
                InventoryManager.Instance?.AddItem(itemId, 1);
                Debug.LogWarning($"[Equipment] 기존 장비 해제 실패로 장착 취소: {itemId}");
                return false;
            }
        }

        // 새 장비 장착
        equippedItems[slot] = inventoryItem;
        inventoryItem.isEquipped = true;

        //  모든 장비 스탯 재계산 및 적용
        RecalculateAndApplyAllEquipmentStats();

        // 무기 타입 설정
        if (data.equipSlot == EquipmentSlot.MeleeWeapon || data.equipSlot == EquipmentSlot.RangedWeapon)
        {
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetAttackType(data.equipSlot);
            }
        }

        OnEquipmentChanged?.Invoke(slot, inventoryItem);
        OnEquipmentStatsChanged?.Invoke();

        Debug.Log($"[Equipment] {data.itemName} 장착됨 (슬롯: {slot}) - 인벤토리 슬롯 확보됨");
        return true;
    }

    
    /// 장비 아이템 해제
    ///  개선: 인벤토리에 아이템을 다시 추가
    
    public bool UnequipItem(EquipmentSlot slot)
    {
        if (!equippedItems.ContainsKey(slot))
        {
            Debug.LogWarning($"[Equipment] 해당 슬롯에 장착된 아이템 없음: {slot}");
            return false;
        }

        InventoryItem item = equippedItems[slot];
        ItemData data = item.GetItemData();

        if (data != null)
        {
            // 인벤토리에 아이템 다시 추가
            bool added = InventoryManager.Instance?.AddItem(data.itemId, 1) ?? false;
            if (!added)
            {
                Debug.LogWarning($"[Equipment] 인벤토리가 가득 차서 해제할 수 없음: {data.itemName}");
                PopupManager.Instance.ShowWarningPopup("인벤토리가 가득 차서 장비를 해제할 수 없습니다.");
                return false;
            }

            Debug.Log($"[Equipment] {data.itemName} 해제됨 (슬롯: {slot}) - 인벤토리에 반환됨");
        }

        item.isEquipped = false;
        equippedItems.Remove(slot);

        //  모든 장비 스탯 재계산 및 적용
        RecalculateAndApplyAllEquipmentStats();

        OnEquipmentChanged?.Invoke(slot, null);
        OnEquipmentStatsChanged?.Invoke();

        return true;
    }

    
    /// 치장 아이템 장착
    ///  개선: 인벤토리에서 아이템을 제거하여 슬롯 확보
    
    private bool EquipCosmetic(InventoryItem inventoryItem, ItemData data)
    {
        CosmeticSlot slot = data.ConvertToCosmeticSlot(data.equipSlot);
        if (slot == CosmeticSlot.None)
        {
            Debug.LogWarning($"[Equipment] 유효하지 않은 치장 슬롯: {data.itemId}");
            return false;
        }

        // 이미 장착된 아이템은 장착 불가
        if (inventoryItem.isEquipped)
        {
            Debug.LogWarning($"[Equipment] 이미 장착된 치장 아이템: {data.itemId}");
            return false;
        }

        // 기존 치장이 있으면 해제
        if (cosmeticItems.ContainsKey(slot))
        {
            UnequipCosmetic(slot);
        }

        //  인벤토리에서 아이템 제거 (슬롯 확보) 
        bool removed = InventoryManager.Instance?.RemoveItem(data.itemId, 1) ?? false;
        if (!removed)
        {
            Debug.LogWarning($"[Equipment] 인벤토리에서 치장 아이템 제거 실패: {data.itemId}");
            return false;
        }

        // 새 치장 장착
        cosmeticItems[slot] = inventoryItem;
        inventoryItem.isEquipped = true;

        OnCosmeticChanged?.Invoke(slot, inventoryItem);

        Debug.Log($"[Equipment] 치장 {data.itemName} 장착됨 (슬롯: {slot}) - 인벤토리 슬롯 확보됨");
        return true;
    }

    
    /// 치장 아이템 해제
    ///  개선: 인벤토리에 아이템을 다시 추가
    
    public bool UnequipCosmetic(CosmeticSlot slot)
    {
        if (!cosmeticItems.ContainsKey(slot))
        {
            Debug.LogWarning($"[Equipment] 해당 슬롯에 장착된 치장 없음: {slot}");
            return false;
        }

        InventoryItem item = cosmeticItems[slot];
        ItemData data = item.GetItemData();

        if (data != null)
        {
            // 인벤토리에 아이템 다시 추가
            bool added = InventoryManager.Instance?.AddItem(data.itemId, 1) ?? false;
            if (!added)
            {
                Debug.LogWarning($"[Equipment] 인벤토리가 가득 차서 해제할 수 없음: {data.itemName}");
                return false;
            }

            Debug.Log($"[Equipment] 치장 {data.itemName} 해제됨 (슬롯: {slot}) - 인벤토리에 반환됨");
        }

        item.isEquipped = false;
        cosmeticItems.Remove(slot);

        OnCosmeticChanged?.Invoke(slot, null);

        return true;
    }

    // ====================  스탯 적용 (GetTotalStatBonus 재활용)  ====================

    
    ///  모든 장착된 장비의 스탯을 합산하여 플레이어에게 적용
    /// GetTotalStatBonus() 메서드를 재활용하여 중복 제거
    
    public void RecalculateAndApplyAllEquipmentStats()
    {
        if (PlayerStatsComponent.Instance == null)
        {
            Debug.LogWarning("[Equipment] PlayerStatsComponent를 찾을 수 없습니다.");
            return;
        }

        CharacterStats stats = PlayerStatsComponent.Instance.Stats;

        //  GetTotalStatBonus()를 재활용하여 각 스탯 합계 계산
        int totalStrength = GetTotalStatBonus(StatType.Strength);
        int totalDexterity = GetTotalStatBonus(StatType.Dexterity);
        int totalIntelligence = GetTotalStatBonus(StatType.Intelligence);
        int totalLuck = GetTotalStatBonus(StatType.Luck);
        int totalTechnique = GetTotalStatBonus(StatType.Technique);
        int totalAttackBonus = GetTotalStatBonus(StatType.AttackPower);
        int totalDefenseBonus = GetTotalStatBonus(StatType.Defense);
        int totalHPBonus = GetTotalStatBonus(StatType.MaxHP);

        // 합산된 스탯을 한 번에 적용
        stats.ModifyStat(StatType.Strength, totalStrength);
        stats.ModifyStat(StatType.Dexterity, totalDexterity);
        stats.ModifyStat(StatType.Intelligence, totalIntelligence);
        stats.ModifyStat(StatType.Luck, totalLuck);
        stats.ModifyStat(StatType.Technique, totalTechnique);
        stats.ModifyStat(StatType.AttackPower, totalAttackBonus);
        stats.ModifyStat(StatType.Defense, totalDefenseBonus);
        stats.ModifyStat(StatType.MaxHP, totalHPBonus);

        // PlayerController의 공격력 업데이트
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.UpdateStats();
        }

        Debug.Log($"[Equipment] 전체 장비 스탯 적용 완료 - STR:{totalStrength} DEX:{totalDexterity} INT:{totalIntelligence} LUK:{totalLuck} TEC:{totalTechnique} ATK:{totalAttackBonus} DEF:{totalDefenseBonus} HP:{totalHPBonus}");
    }

    // ==================== 조회 메서드 ====================

    
    /// 특정 슬롯에 장착된 장비 가져오기
    
    public InventoryItem GetEquippedItem(EquipmentSlot slot)
    {
        return equippedItems.ContainsKey(slot) ? equippedItems[slot] : null;
    }

    
    /// 특정 슬롯에 장착된 치장 가져오기
    
    public InventoryItem GetCosmeticItem(CosmeticSlot slot)
    {
        return cosmeticItems.ContainsKey(slot) ? cosmeticItems[slot] : null;
    }

    
    /// 모든 장착된 장비 가져오기
    
    public Dictionary<EquipmentSlot, InventoryItem> GetAllEquippedItems()
    {
        return new Dictionary<EquipmentSlot, InventoryItem>(equippedItems);
    }

    
    /// 모든 장착된 치장 가져오기
    
    public Dictionary<CosmeticSlot, InventoryItem> GetAllCosmeticItems()
    {
        return new Dictionary<CosmeticSlot, InventoryItem>(cosmeticItems);
    }

    
    /// 해당 슬롯에 장비가 장착되어 있는지 확인
    
    public bool IsSlotEquipped(EquipmentSlot slot)
    {
        return equippedItems.ContainsKey(slot);
    }

    
    /// 해당 슬롯에 치장이 장착되어 있는지 확인
    
    public bool IsCosmeticSlotEquipped(CosmeticSlot slot)
    {
        return cosmeticItems.ContainsKey(slot);
    }

    // ====================  총 장비 보너스 계산 (재활용)  ====================

    
    /// 현재 장착된 모든 장비의 특정 스탯 보너스 합계
    ///  RecalculateAndApplyAllEquipmentStats()에서 재활용됨
    
    public int GetTotalStatBonus(StatType statType)
    {
        int total = 0;

        foreach (var kvp in equippedItems)
        {
            ItemData data = kvp.Value.GetItemData();
            if (data == null) continue;

            switch (statType)
            {
                case StatType.AttackPower:
                    total += data.attackBonus;
                    break;
                case StatType.Defense:
                    total += data.defenseBonus;
                    break;
                case StatType.MaxHP:
                    total += data.hpBonus;
                    break;
                case StatType.Strength:
                    total += data.strBonus;
                    break;
                case StatType.Dexterity:
                    total += data.dexBonus;
                    break;
                case StatType.Intelligence:
                    total += data.intBonus;
                    break;
                case StatType.Luck:
                    total += data.lukBonus;
                    break;
                case StatType.Technique:
                    total += data.tecBonus;
                    break;
            }
        }

        return total;
    }

    // ==================== 인벤토리 용량 확인 ====================

    
    /// 장비를 해제할 수 있는지 확인 (인벤토리 공간 확인)
    
    public bool CanUnequip(EquipmentSlot slot)
    {
        if (!equippedItems.ContainsKey(slot))
            return false;

        // 인벤토리에 공간이 있는지 확인
        if (InventoryManager.Instance == null)
            return false;

        // 인벤토리가 가득 차지 않았는지 확인
        return InventoryManager.Instance.GetAllItems().Count < 50; // maxSlots 하드코딩 대신 getter 필요
    }

    
    /// 치장을 해제할 수 있는지 확인 (인벤토리 공간 확인)
    
    public bool CanUnequipCosmetic(CosmeticSlot slot)
    {
        if (!cosmeticItems.ContainsKey(slot))
            return false;

        if (InventoryManager.Instance == null)
            return false;

        return InventoryManager.Instance.GetAllItems().Count < 50;
    }
    // ====================  저장/로드 기능  ====================

    
    /// 장비 데이터를 저장 가능한 형식으로 변환
    ///  수정: InventoryItemSaveData 전체를 저장 (itemId만 저장하면 로드 시 인벤토리에서 못 찾음)
    
    public EquipmentSaveData ToSaveData()
    {
        EquipmentSaveData saveData = new EquipmentSaveData();

        // 장비 슬롯 저장
        foreach (var kvp in equippedItems)
        {
            saveData.equippedItems.Add(new EquipmentSlotSaveData
            {
                slot = kvp.Key,
                itemData = kvp.Value.ToSaveData() //  전체 아이템 데이터 저장
            });
        }

        // 치장 슬롯 저장
        foreach (var kvp in cosmeticItems)
        {
            saveData.cosmeticItems.Add(new CosmeticSlotSaveData
            {
                slot = kvp.Key,
                itemData = kvp.Value.ToSaveData() //  전체 아이템 데이터 저장
            });
        }

        Debug.Log($"[Equipment] 장비 저장: {saveData.equippedItems.Count}개 장비, {saveData.cosmeticItems.Count}개 치장");
        return saveData;
    }

    
    /// 저장된 장비 데이터를 로드
    ///  수정: InventoryItemSaveData에서 InventoryItem을 복원
    
    public void LoadFromSaveData(EquipmentSaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogWarning("[Equipment] 저장 데이터가 없습니다.");
            return;
        }

        // 기존 장비 모두 해제
        ClearAllEquipment();

        //  무기 타입 추적 변수
        EquipmentSlot? equippedWeaponSlot = null;

        // 장비 슬롯 복원
        foreach (var slotData in saveData.equippedItems)
        {
            //  InventoryItemSaveData에서 InventoryItem 복원
            InventoryItem item = InventoryItem.FromSaveData(slotData.itemData);
            if (item != null)
            {
                equippedItems[slotData.slot] = item;
                item.isEquipped = true;

                //  무기 슬롯 확인 (나중에 한 번만 설정하기 위해 기록)
                if (slotData.slot == EquipmentSlot.MeleeWeapon || slotData.slot == EquipmentSlot.RangedWeapon)
                {
                    equippedWeaponSlot = slotData.slot;
                }

                Debug.Log($"[Equipment] 장비 복원: {slotData.itemData.itemid} -> {slotData.slot}");
            }
            else
            {
                Debug.LogWarning($"[Equipment] 아이템 복원 실패: {slotData.itemData?.itemid}");
            }
        }

        // 치장 슬롯 복원
        foreach (var slotData in saveData.cosmeticItems)
        {
            InventoryItem item = InventoryItem.FromSaveData(slotData.itemData);
            if (item != null)
            {
                cosmeticItems[slotData.slot] = item;
                item.isEquipped = true;
                Debug.Log($"[Equipment] 치장 복원: {slotData.itemData.itemid} -> {slotData.slot}");
            }
            else
            {
                Debug.LogWarning($"[Equipment] 치장 아이템 복원 실패: {slotData.itemData?.itemid}");
            }
        }
        // 장비가 하나라도 있으면 스탯 재계산 및 무기 타입 설정
        if (saveData.equippedItems.Count != 0)
        {
            // 스탯 재계산
            RecalculateAndApplyAllEquipmentStats();

            // 무기 타입 설정 (한 번만, 실제 장착된 무기 기준으로)
            if (equippedWeaponSlot.HasValue && PlayerController.Instance != null)
            {
                PlayerController.Instance.SetAttackType(equippedWeaponSlot.Value);
                Debug.Log($"[Equipment] 무기 타입 설정: {equippedWeaponSlot.Value}");
            }
        }
        
        // 이벤트 발생
        foreach (var kvp in equippedItems)
        {
            OnEquipmentChanged?.Invoke(kvp.Key, kvp.Value);
        }
        foreach (var kvp in cosmeticItems)
        {
            OnCosmeticChanged?.Invoke(kvp.Key, kvp.Value);
        }

        Debug.Log($"[Equipment] 장비 로드 완료: {equippedItems.Count}개 장비, {cosmeticItems.Count}개 치장");
    }

    
    /// 모든 장비 해제 (초기화용)
    
    public void ClearAllEquipment()
    {
        equippedItems.Clear();
        cosmeticItems.Clear();
        Debug.Log("[Equipment] 모든 장비 초기화");
    }
}

// ==================== 저장 데이터 구조 ====================

[System.Serializable]
public class EquipmentSaveData
{
    public List<EquipmentSlotSaveData> equippedItems = new List<EquipmentSlotSaveData>();
    public List<CosmeticSlotSaveData> cosmeticItems = new List<CosmeticSlotSaveData>();
}

[System.Serializable]
public class EquipmentSlotSaveData
{
    public EquipmentSlot slot;
    public InventoryItemSaveData itemData; //  itemId → itemData로 변경
}

[System.Serializable]
public class CosmeticSlotSaveData
{
    public CosmeticSlot slot;
    public InventoryItemSaveData itemData; //  itemId → itemData로 변경
}