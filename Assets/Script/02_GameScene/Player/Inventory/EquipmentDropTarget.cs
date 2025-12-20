using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 장비 슬롯에 아이템을 드롭할 수 있게 해주는 컴포넌트
/// EquipmentSlotUI에 추가하여 사용
/// </summary>
public class EquipmentDropTarget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    private enum SlotMode
    {
        Equipment,  // 장비 모드
        Cosmetic    // 치장 모드
    }

    private SlotMode mode;
    private EquipmentSlot equipmentSlot;
    private CosmeticSlot cosmeticSlot;
    private bool isHighlighted = false;

    /// <summary>
    /// 장비 슬롯으로 초기화
    /// </summary>
    public void InitializeAsEquipment(EquipmentSlot slot)
    {
        mode = SlotMode.Equipment;
        equipmentSlot = slot;
    }

    /// <summary>
    /// 치장 슬롯으로 초기화
    /// </summary>
    public void InitializeAsCosmetic(CosmeticSlot slot)
    {
        mode = SlotMode.Cosmetic;
        cosmeticSlot = slot;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 드래그 중인 아이템이 있으면 하이라이트
        if (eventData.pointerDrag != null)
        {
            DraggableItemUI draggable = eventData.pointerDrag.GetComponent<DraggableItemUI>();
            if (draggable != null && CanEquipItem(draggable.GetItem()))
            {
                SetHighlight(true);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlight(false);
    }

    public void OnDrop(PointerEventData eventData)
    {
        SetHighlight(false);
    }

    /// <summary>
    /// 아이템이 드롭되었을 때 호출
    /// </summary>
    public bool OnItemDropped(InventoryItem item)
    {
        if (item == null) return false;

        ItemData data = item.GetItemData();
        if (data == null) return false;

        bool success = false;

        if (mode == SlotMode.Equipment)
        {
            //  일반 장비 장착 - 무기 슬롯은 근거리/원거리 둘 다 허용
            bool isCorrectSlot = false;

            if (equipmentSlot == EquipmentSlot.MeleeWeapon)
            {
                // 무기 슬롯은 근거리 또는 원거리 무기 허용
                isCorrectSlot = (data.equipSlot == EquipmentSlot.MeleeWeapon ||
                                data.equipSlot == EquipmentSlot.RangedWeapon) && !data.isCosmetic;
            }
            else
            {
                // 다른 슬롯은 정확히 일치해야 함
                isCorrectSlot = data.equipSlot == equipmentSlot && !data.isCosmetic;
            }

            if (isCorrectSlot)
            {
                success = EquipmentManager.Instance?.EquipItem(item.itemid) ?? false;
                if (success)
                {
                    Debug.Log($"[EquipmentDropTarget] {data.itemName} 장착됨 (슬롯: {equipmentSlot})");

                    // UI 갱신
                    if (ItemUIManager.Instance != null)
                    {
                        ItemUIManager.Instance.RefreshUI();
                    }
                    if (QuickSlotUIManager.Instance != null)
                    {
                        QuickSlotUIManager.Instance.RefreshAllSlots();
                    }
                    if (EquipmentUIManager.Instance != null)
                    {
                        EquipmentUIManager.Instance.RefreshUI();
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[EquipmentDropTarget] 잘못된 슬롯: {data.itemName}은 {data.equipSlot} 슬롯입니다.");
            }
        }
        else // SlotMode.Cosmetic
        {
            // 치장 아이템 장착
            if (data.ConvertToCosmeticSlot(data.equipSlot) == cosmeticSlot && data.isCosmetic)
            {
                success = EquipmentManager.Instance?.EquipItem(item.itemid) ?? false;
                if (success)
                {
                    Debug.Log($"[EquipmentDropTarget] 치장 {data.itemName} 장착됨 (슬롯: {cosmeticSlot})");

                    // UI 갱신
                    if (ItemUIManager.Instance != null)
                    {
                        ItemUIManager.Instance.RefreshUI();
                    }
                    if (QuickSlotUIManager.Instance != null)
                    {
                        QuickSlotUIManager.Instance.RefreshAllSlots();
                    }
                    if (EquipmentUIManager.Instance != null)
                    {
                        EquipmentUIManager.Instance.RefreshUI();
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[EquipmentDropTarget] 잘못된 치장 슬롯: {data.itemName}은 {data.ConvertToCosmeticSlot(data.equipSlot)} 슬롯입니다.");
            }
        }

        return success;
    }

    /// <summary>
    /// 아이템이 이 슬롯에 장착 가능한지 확인
    /// </summary>
    private bool CanEquipItem(InventoryItem item)
    {
        if (item == null) return false;

        ItemData data = item.GetItemData();
        if (data == null || data.itemType != ItemType.Equipment) return false;

        if (mode == SlotMode.Equipment)
        {
            //  무기 슬롯은 근거리/원거리 둘 다 허용
            if (equipmentSlot == EquipmentSlot.MeleeWeapon)
            {
                return (data.equipSlot == EquipmentSlot.MeleeWeapon ||
                        data.equipSlot == EquipmentSlot.RangedWeapon) && !data.isCosmetic;
            }

            return data.equipSlot == equipmentSlot && !data.isCosmetic;
        }
        else // SlotMode.Cosmetic
        {
            return data.ConvertToCosmeticSlot(data.equipSlot) == cosmeticSlot && data.isCosmetic;
        }
    }

    /// <summary>
    /// 하이라이트 효과 설정
    /// </summary>
    private void SetHighlight(bool highlight)
    {
        isHighlighted = highlight;

        // 시각적 피드백 (옵션)
        UnityEngine.UI.Image image = GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            if (highlight)
            {
                image.color = new Color(1f, 1f, 0.5f, 1f); // 노랑색 하이라이트
            }
            else
            {
                image.color = Color.white;
            }
        }
    }
}