using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 장착된 장비를 드래그하여 해제할 수 있게 해주는 컴포넌트
/// EquipmentSlotUI에 추가하여 사용
/// 
///  핵심 수정:
/// 1. dragVisual이 raycast를 차단하지 않도록 설정
/// 2. currentItem null 체크 강화
/// 3. 무기 슬롯 드래그 시 실제 장착된 슬롯(MeleeWeapon/RangedWeapon) 찾기
/// </summary>
public class DraggableEquipmentUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    //  현재 드래그 중인 인스턴스 추적 (정적 변수)
    private static DraggableEquipmentUI currentDragging = null;

    private enum SlotMode
    {
        Equipment,
        Cosmetic
    }

    private SlotMode mode;
    private EquipmentSlot equipmentSlot;
    private CosmeticSlot cosmeticSlot;
    private InventoryItem currentItem;
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private GameObject dragVisual;


    /// <summary>
    /// 드래그 중인 장비가 있는지 확인
    /// </summary>
    public static bool IsDragging()
    {
        return currentDragging != null;
    }

    /// <summary>
    /// 진행 중인 드래그를 강제로 취소
    /// </summary>
    public static void CancelCurrentDrag()
    {
        if (currentDragging != null)
        {
            Debug.Log("[DraggableEquipmentUI] 드래그 강제 취소 (UI 갱신으로 인한)");
            currentDragging.CleanupDrag();
            currentDragging = null;
        }
    }

    public void InitializeAsEquipment(EquipmentSlot slot)
    {
        mode = SlotMode.Equipment;
        equipmentSlot = slot;
        SetupDragComponents();
    }

    public void InitializeAsCosmetic(CosmeticSlot slot)
    {
        mode = SlotMode.Cosmetic;
        cosmeticSlot = slot;
        SetupDragComponents();
    }

    private void SetupDragComponents()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void SetCurrentItem(InventoryItem item)
    {
        currentItem = item;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        //  null 체크 강화
        if (currentItem == null)
        {
            Debug.LogWarning("[DraggableEquipmentUI] currentItem이 null입니다.");
            return;
        }

        //  이미 다른 드래그가 진행 중이면 취소
        if (currentDragging != null && currentDragging != this)
        {
            currentDragging.CleanupDrag();
        }

        //  현재 드래그 중으로 표시
        currentDragging = this;

        // 드래그 비주얼 생성
        dragVisual = CreateDragVisual();
        if (dragVisual != null)
        {
            RectTransform dragRect = dragVisual.GetComponent<RectTransform>();
            dragRect.position = rectTransform.position;

            //  핵심: dragVisual이 raycast를 차단하지 않도록 설정
            CanvasGroup dragVisualCanvasGroup = dragVisual.GetComponent<CanvasGroup>();
            if (dragVisualCanvasGroup == null)
            {
                dragVisualCanvasGroup = dragVisual.AddComponent<CanvasGroup>();
            }
            dragVisualCanvasGroup.blocksRaycasts = false;
            dragVisualCanvasGroup.alpha = 0.7f;
        }

        // 원본은 투명하게
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.3f;
            canvasGroup.blocksRaycasts = false;
        }

        ItemData data = currentItem.GetItemData();
        Debug.Log($"[DraggableEquipmentUI] 장비 드래그 시작: {data?.itemName}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragVisual != null)
        {
            RectTransform dragRect = dragVisual.GetComponent<RectTransform>();
            dragRect.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //  null 체크 강화
        if (currentItem == null)
        {
            Debug.LogWarning("[DraggableEquipmentUI] OnEndDrag - currentItem이 null입니다.");
            CleanupDrag();
            return;
        }

        GameObject dropTarget = eventData.pointerCurrentRaycast.gameObject;
        bool unequipped = false;

        Debug.Log($"[DraggableEquipmentUI] 드롭 대상: {(dropTarget != null ? dropTarget.name : "null")}");

        if (dropTarget != null && IsOverInventoryPanel(dropTarget))
        {
            unequipped = UnequipItem();
        }

        if (!unequipped)
        {
            Debug.Log("[DraggableEquipmentUI] 장비 해제 실패 - 인벤토리로 드래그하세요.");
        }

        CleanupDrag();
    }

    private bool UnequipItem()
    {
        //  모든 단계에서 null 체크
        if (EquipmentManager.Instance == null)
        {
            Debug.LogWarning("[DraggableEquipmentUI] EquipmentManager.Instance가 null입니다.");
            return false;
        }

        if (currentItem == null)
        {
            Debug.LogWarning("[DraggableEquipmentUI] UnequipItem - currentItem이 null입니다.");
            return false;
        }

        bool success = false;

        if (mode == SlotMode.Equipment)
        {
            //  무기 슬롯 특별 처리: 실제 장착된 슬롯을 찾아서 해제 
            EquipmentSlot actualSlot = GetActualEquippedSlot();
            success = EquipmentManager.Instance.UnequipItem(actualSlot);
        }
        else
        {
            success = EquipmentManager.Instance.UnequipCosmetic(cosmeticSlot);
        }

        if (success)
        {
            //  null 안전 연산자 사용
            ItemData data = currentItem?.GetItemData();
            string modeText = mode == SlotMode.Equipment ? "장비" : "치장";
            Debug.Log($"[DraggableEquipmentUI] {data?.itemName ?? "알 수 없는 아이템"} {modeText} 해제됨 (드래그)");

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

        return success;
    }

    /// <summary>
    ///  실제 장착된 슬롯 찾기 (무기 슬롯 특별 처리)
    /// </summary>
    private EquipmentSlot GetActualEquippedSlot()
    {
        // 무기 슬롯의 경우, 실제 데이터의 equipSlot을 사용
        if (equipmentSlot == EquipmentSlot.MeleeWeapon && currentItem != null)
        {
            ItemData data = currentItem.GetItemData();
            if (data != null)
            {
                // 현재 아이템의 실제 슬롯 타입 확인
                if (data.equipSlot == EquipmentSlot.MeleeWeapon)
                    return EquipmentSlot.MeleeWeapon;
                else if (data.equipSlot == EquipmentSlot.RangedWeapon)
                    return EquipmentSlot.RangedWeapon;
            }
        }

        // 일반 슬롯은 그대로 반환
        return equipmentSlot;
    }

    private GameObject CreateDragVisual()
    {
        if (canvas == null) return null;

        GameObject visual = Instantiate(gameObject, canvas.transform);

        var eventTriggers = visual.GetComponents<MonoBehaviour>();
        foreach (var trigger in eventTriggers)
        {
            if (trigger is IPointerClickHandler || trigger is IBeginDragHandler)
            {
                Destroy(trigger);
            }
        }

        return visual;
    }

    private bool IsOverInventoryPanel(GameObject target)
    {
        if (target == null) return false;

        Transform current = target.transform;
        while (current != null)
        {
            if (current.name.Contains("ItemList") ||
                current.name.Contains("Inventory") ||
                current.GetComponent<ItemUIManager>() != null)
            {
                return true;
            }
            current = current.parent;
        }

        return false;
    }

    private void CleanupDrag()
    {
        if (dragVisual != null)
        {
            Destroy(dragVisual);
            dragVisual = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
        //  현재 드래그 중 상태 해제
        if (currentDragging == this)
        {
            currentDragging = null;
        }
    }
    /// <summary>
    /// 오브젝트가 파괴될 때 드래그 정리
    /// </summary>
    void OnDestroy()
    {
        //  이 오브젝트가 드래그 중이었다면 정리
        if (currentDragging == this)
        {
            if (dragVisual != null)
            {
                Destroy(dragVisual);
                dragVisual = null;
            }
            currentDragging = null;
            Debug.Log("[DraggableEquipmentUI] OnDestroy - 드래그 정리됨");
        }
    }
}