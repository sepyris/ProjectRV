using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


/// 모든 아이템을 드래그하여 버릴 수 있게 해주는 컴포넌트
/// UI 밖으로 드래그하면 아이템 버리기 팝업 표시
/// 장비 아이템은 DraggableItemUI와 함께 사용됨

public class DiscardableItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    //  현재 드래그 중인 인스턴스 추적 (정적 변수)
    private static DiscardableItemUI currentDragging = null;

    private InventoryItem item;
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private GameObject dragVisual;
    private bool isDraggingToDiscard = false;

    
    /// 드래그 중인 아이템이 있는지 확인
    
    public static bool IsDragging()
    {
        return currentDragging != null;
    }

    
    /// 진행 중인 드래그를 강제로 취소
    
    public static void CancelCurrentDrag()
    {
        if (currentDragging != null)
        {
            Debug.Log("[DiscardableItemUI] 드래그 강제 취소 (UI 갱신으로 인한)");
            currentDragging.CleanupDrag();
            currentDragging = null;
        }
    }

    public void Initialize(InventoryItem inventoryItem)
    {
        item = inventoryItem;
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null) return;

        ItemData data = item.GetItemData();
        if (data == null) return;

        // 퀘스트 아이템은 버릴 수 없음
        if (data.itemType == ItemType.QuestItem)
        {
            Debug.Log("[DiscardableItemUI] 퀘스트 아이템은 버릴 수 없습니다.");
            return;
        }

        //  이미 다른 드래그가 진행 중이면 취소
        if (currentDragging != null && currentDragging != this)
        {
            currentDragging.CleanupDrag();
        }

        //  현재 드래그 중으로 표시
        currentDragging = this;
        isDraggingToDiscard = true;

        // 드래그 비주얼 생성
        dragVisual = Instantiate(gameObject, canvas.transform);
        RectTransform dragRect = dragVisual.GetComponent<RectTransform>();
        dragRect.position = rectTransform.position;
        dragRect.sizeDelta = rectTransform.sizeDelta;

        CanvasGroup dragVisualCanvasGroup = dragVisual.GetComponent<CanvasGroup>();
        if (dragVisualCanvasGroup == null)
        {
            dragVisualCanvasGroup = dragVisual.AddComponent<CanvasGroup>();
        }
        dragVisualCanvasGroup.blocksRaycasts = false;
        dragVisualCanvasGroup.alpha = 0.7f;  // 약간 투명하게

        // 원본은 투명하게
        canvasGroup.alpha = 0.3f;
        canvasGroup.blocksRaycasts = false;

        Debug.Log($"[DiscardableItemUI] 드래그 시작: {data.itemName}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggingToDiscard || dragVisual == null) return;

        RectTransform dragRect = dragVisual.GetComponent<RectTransform>();
        dragRect.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragVisual != null)
        {
            Destroy(dragVisual);
            dragVisual = null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        //  퀵슬롯 드롭 체크 (최우선)
        if (TryDropToQuickSlot(eventData))
        {
            Debug.Log("[DraggableItemUI] 퀵슬롯에 등록됨");
            currentDragging = null;
            return;
        }

        if (!isDraggingToDiscard)
        {
            CleanupDrag();
            return;
        }

        if (item == null)
        {
            CleanupDrag();
            return;
        }

        ItemData data = item.GetItemData();
        if (data == null)
        {
            CleanupDrag();
            return;
        }

        // 드롭 대상 확인
        GameObject dropTarget = eventData.pointerCurrentRaycast.gameObject;
        bool droppedOnUI = IsOverUI(dropTarget);

        Debug.Log($"[DiscardableItemUI] 드롭 대상: {(dropTarget != null ? dropTarget.name : "null")}, UI 위: {droppedOnUI}");

        // 장비 아이템의 경우 장비창에 드롭했는지 확인
        bool droppedOnEquipmentSlot = false;
        if (data.itemType == ItemType.Equipment && dropTarget != null)
        {
            EquipmentDropTarget equipDropTarget = dropTarget.GetComponent<EquipmentDropTarget>();
            if (equipDropTarget == null)
            {
                equipDropTarget = dropTarget.GetComponentInParent<EquipmentDropTarget>();
            }
            droppedOnEquipmentSlot = (equipDropTarget != null);
        }

        // UI 밖으로 드래그했고, 장비 슬롯도 아닌 경우 버리기
        if (!droppedOnUI && !droppedOnEquipmentSlot)
        {
            ShowDiscardConfirmation(item, data);
        }
        else if (droppedOnUI && !droppedOnEquipmentSlot)
        {
            Debug.Log($"[DiscardableItemUI] {data.itemName} - 인벤토리 안에 드롭됨");
        }

        CleanupDrag();
    }

    private bool IsOverUI(GameObject target)
    {
        if (target == null) return false;

        // 인벤토리 패널 위에 있는지 확인 (장비창은 일반 아이템 드래그 대상이 아니므로 제외)
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

    
    ///  PopupManager를 통한 버리기 팝업 표시
    
    private void ShowDiscardConfirmation(InventoryItem discardItem, ItemData data)
    {
        // disposable 체크
        if (!data.disposable)
        {
            Debug.LogWarning($"[DiscardableItemUI] {data.itemName}은(는) 버릴 수 없는 아이템입니다.");
            return;
        }

        if (PopupManager.Instance == null)
        {
            Debug.LogWarning("[DiscardableItemUI] PopupManager를 찾을 수 없습니다.");
            return;
        }

        // 수량이 1개면 바로 확인 팝업
        if (discardItem.quantity == 1)
        {
            string message = $"{data.itemName} 1개를\n정말 버리시겠습니까?";
            PopupManager.Instance.ShowConfirmPopup(
                message,
                () => DiscardItem(discardItem, 1)
            );
        }
        // 수량이 여러 개면 개수 입력 팝업
        else
        {
            PopupManager.Instance.ShowDiscardQuantityPopup(
                data,
                discardItem.quantity,
                (quantity) => {
                    // 수량 입력 후 확인 팝업 표시
                    string message = $"{data.itemName} {quantity}개를\n정말 버리시겠습니까?";
                    PopupManager.Instance.ShowConfirmPopup(
                        message,
                        () => DiscardItem(discardItem, quantity)
                    );
                }
            );
        }
    }

    
    /// 실제 아이템 버리기 실행
    
    private void DiscardItem(InventoryItem discardItem, int quantity)
    {
        if (InventoryManager.Instance == null) return;

        bool removed = InventoryManager.Instance.RemoveItem(discardItem.itemid, quantity);

        if (removed)
        {
            ItemData data = discardItem.GetItemData();
            Debug.Log($"[DiscardableItemUI] {data?.itemName} x{quantity} 버림");

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

    private void CleanupDrag()
    {
        isDraggingToDiscard = false;

        // 드래그 비주얼 제거
        if (dragVisual != null)
        {
            Destroy(dragVisual);
            dragVisual = null;
        }

        // 원본 복원
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

    
    /// 오브젝트가 파괴될 때 드래그 정리
    
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
            Debug.Log("[DiscardableItemUI] OnDestroy - 드래그 정리됨");
        }
    }
    
    /// 드롭 위치가 퀵슬롯인지 확인하고 처리
    
    
    /// 드롭 위치가 퀵슬롯인지 확인하고 처리
    
    private bool TryDropToQuickSlot(PointerEventData eventData)
    {
        if (item == null)
            return false;

        // 소모품이 아니면 퀵슬롯에 등록 불가
        ItemData itemData = item.GetItemData();
        if (itemData == null || itemData.itemType != ItemType.Consumable)
            return false;

        // 마우스 위치에 있는 모든 UI 확인
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        foreach (var result in raycastResults)
        {
            // QuickSlotUI를 찾음
            QuickSlotUI quickSlot = result.gameObject.GetComponent<QuickSlotUI>();
            if (quickSlot == null)
            {
                // 부모에서도 찾아보기
                quickSlot = result.gameObject.GetComponentInParent<QuickSlotUI>();
            }

            if (quickSlot != null)
            {
                //  QuickSlotUI의 OnDrop에 의존하지 않고 직접 등록
                // QuickSlotUI에서 슬롯 인덱스 가져오기
                int slotIndex = quickSlot.transform.GetSiblingIndex();

                if (QuickSlotManager.Instance != null)
                {
                    bool registered = QuickSlotManager.Instance.RegisterConsumable(slotIndex, item.itemid);

                    if (registered)
                    {
                        Debug.Log($"[DiscardableItemUI] 퀵슬롯 {slotIndex + 1}에 {itemData.itemName} 등록 완료");
                    }
                    else
                    {
                        Debug.LogWarning($"[DiscardableItemUI] 퀵슬롯 등록 실패: {itemData.itemName}");
                    }
                }

                return true;
            }
        }

        return false;
    }
    public InventoryItem GetItem()
    {
        return item;
    }
}