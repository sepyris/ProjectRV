using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


/// 인벤토리 아이템 호버 및 더블클릭 처리
/// 더블클릭 시 소모품 사용 또는 장비 장착

public class ItemDetailUiManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private InventoryItem item;
    private ItemUIManager uiManager;
    private float lastClickTime = 0f;
    private const float doubleClickThreshold = 0.3f;

    public void Initialize(InventoryItem inventoryItem, ItemUIManager manager)
    {
        item = inventoryItem;
        uiManager = manager;
    }

    // 마우스 커서가 UI 요소 위로 들어왔을 때 (호버 인)
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (uiManager != null && item != null)
        {
            // ItemUIManager의 상세정보 메서드를 호출하여 상세 정보 표시
            uiManager.ShowItemDetailOnHover(item, this.transform);
        }
    }

    // 마우스 커서가 UI 요소에서 벗어날 때 (호버 아웃)
    public void OnPointerExit(PointerEventData eventData)
    {
        if (uiManager != null)
        {
            uiManager.HideDetailPanelOnHoverExit();
        }
    }

    // 마우스 클릭 처리
    public void OnPointerClick(PointerEventData eventData)
    {
        if (item == null) return;

        // 더블클릭 감지
        if (Time.time - lastClickTime < doubleClickThreshold)
        {
            OnDoubleClick();
            lastClickTime = 0f;
        }
        else
        {
            lastClickTime = Time.time;
        }
    }

    // 더블클릭 처리
    private void OnDoubleClick()
    {
        if (item == null) return;

        ItemData data = item.GetItemData();
        if (data == null) return;

        // 아이템 타입에 따라 처리
        switch (data.itemType)
        {
            case ItemType.Consumable:
                UseConsumableItem();
                break;

            case ItemType.Equipment:
                EquipItem();
                break;

            default:
                Debug.Log($"[ItemDetailUI] {data.itemName}은(는) 사용할 수 없는 아이템입니다.");
                break;
        }
    }

    // 소모품 사용
    private void UseConsumableItem()
    {
        ItemData data = item.GetItemData();
        if (data == null) return;

        // 스탯 시스템 가져오기
        CharacterStats stats = null;
        if (PlayerController.Instance != null)
        {
            var statsComp = PlayerController.Instance.GetComponent<PlayerStatsComponent>();
            if (statsComp != null)
                stats = statsComp.Stats;
        }

        // 아이템 사용
        bool used = InventoryManager.Instance?.UseItem(item.itemid, stats) ?? false;

        if (used)
        {
            Debug.Log($"[ItemDetailUI] {data.itemName} 사용됨 (더블클릭)");

            // UI 갱신
            if (uiManager != null)
                uiManager.RefreshUI();
            if(QuickSlotUIManager.Instance != null)
            {
                QuickSlotUIManager.Instance.RefreshAllSlots();
            }
        }
    }

    // 장비 장착
    private void EquipItem()
    {
        ItemData data = item.GetItemData();
        if (data == null) return;

        // ? 새 시스템에서는 장착된 아이템이 인벤토리에서 제거되므로
        // isEquipped 체크는 사실상 불필요하지만, 안전을 위해 유지
        if (item.isEquipped)
        {
            // 이미 장착된 경우 (발생하지 않아야 함)
            Debug.LogWarning($"[ItemDetailUI] {data.itemName}은(는) 이미 장착되어 있습니다.");
            return;
        }

        // 장착 시도
        bool equipped = EquipmentManager.Instance?.EquipItem(item.itemid) ?? false;

        if (equipped)
        {
            Debug.Log($"[ItemDetailUI] {data.itemName} 장착됨 (더블클릭)");

            // UI 갱신
            if (uiManager != null)
                uiManager.RefreshUI();
            if (QuickSlotUIManager.Instance != null)
            {
                QuickSlotUIManager.Instance.RefreshAllSlots();
            }
            

            // 장비창 UI도 갱신
            if (EquipmentUIManager.Instance != null)
                EquipmentUIManager.Instance.RefreshAllSlots();
        }
    }
}