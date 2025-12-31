using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


/// 인벤토리 아이템을 드래그할 수 있게 해주는 컴포넌트
/// 장비창으로 드래그하여 장착 가능

public class DraggableItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    //  현재 드래그 중인 인스턴스 추적 (정적 변수)
    private static DraggableItemUI currentDragging = null;

    private InventoryItem item;
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private GameObject dragVisual;

    
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
            Debug.Log("[DraggableItemUI] 드래그 강제 취소 (UI 갱신으로 인한)");
            currentDragging.CleanupDrag();
            currentDragging = null;
        }
    }

    public void Initialize(InventoryItem inventoryItem)
    {
        item = inventoryItem;
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        // CanvasGroup이 없으면 추가
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
        if (data == null || data.itemType != ItemType.Equipment) return;

        //  이미 다른 드래그가 진행 중이면 취소
        if (currentDragging != null && currentDragging != this)
        {
            currentDragging.CleanupDrag();
        }

        //  현재 드래그 중으로 표시
        currentDragging = this;

        // 드래그 비주얼 생성 (현재 오브젝트를 복제)
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

        Debug.Log($"[DraggableItemUI] 드래그 시작: {data.itemName}");
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

        // 드롭 대상 찾기
        GameObject dropTarget = eventData.pointerCurrentRaycast.gameObject;

        Debug.Log($"[DraggableItemUI] 드롭 대상: {(dropTarget != null ? dropTarget.name : "null")}");

        // 1. 먼저 장비 슬롯에 드롭되었는지 확인
        bool equipped = false;

        if (dropTarget != null)
        {
            EquipmentDropTarget dropHandler = dropTarget.GetComponent<EquipmentDropTarget>();
            if (dropHandler == null)
            {
                dropHandler = dropTarget.GetComponentInParent<EquipmentDropTarget>();
            }

            if (dropHandler != null)
            {
                equipped = dropHandler.OnItemDropped(item);

                Debug.Log($"[DraggableItemUI] 장비 슬롯 감지! 장착 {(equipped ? "성공" : "실패")}");

                CleanupDrag();
                return;  // 장비 슬롯 처리 완료
            }
        }

        // 2. 장비 슬롯이 아닌 경우 - UI 안/밖 확인
        bool droppedOnInventoryUI = IsOverInventoryUI(dropTarget);

        Debug.Log($"[DraggableItemUI] 인벤토리 UI 위: {droppedOnInventoryUI}");

        if (!droppedOnInventoryUI && data.disposable)
        {
            // UI 밖 + disposable → 버리기
            Debug.Log($"[DraggableItemUI] {data.itemName} 버리기 시도");
            CleanupDrag();
            ShowDiscardConfirmation(item, data);
            return;
        }
        else if (!droppedOnInventoryUI && !data.disposable)
        {
            Debug.Log($"[DraggableItemUI] {data.itemName}은(는) 버릴 수 없는 아이템입니다.");
        }
        else
        {
            Debug.Log("[DraggableItemUI] 인벤토리 안에 드롭됨 - 취소");
        }

        CleanupDrag();
    }

    
    /// 인벤토리 UI 위에 있는지 확인 (장비창 제외!)
    
    private bool IsOverInventoryUI(GameObject target)
    {
        if (target == null) return false;

        Transform current = target.transform;
        while (current != null)
        {
            // 인벤토리 패널만 확인 (장비창 제외)
            if (current.name.Contains("ItemList") ||
                current.name.Contains("Inventory") ||
                current.GetComponent<ItemUIManager>() != null)
            {
                return true;
            }

            // 장비창은 제외 (여기서 false 반환하지 않고 계속 검색)
            current = current.parent;
        }

        return false;
    }

    
    ///  PopupManager를 통한 버리기 팝업 표시
    
    private void ShowDiscardConfirmation(InventoryItem discardItem, ItemData data)
    {
        if (PopupManager.Instance == null)
        {
            Debug.LogWarning("[DraggableItemUI] PopupManager를 찾을 수 없습니다.");
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

    private void DiscardItem(InventoryItem discardItem, int quantity)
    {
        if (InventoryManager.Instance == null) return;

        bool removed = InventoryManager.Instance.RemoveItem(discardItem.itemid, quantity);

        if (removed)
        {
            ItemData data = discardItem.GetItemData();
            Debug.Log($"[DraggableItemUI] {data?.itemName} x{quantity} 버림");

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
            Debug.Log("[DraggableItemUI] OnDestroy - 드래그 정리됨");
        }
    }

    public InventoryItem GetItem()
    {
        return item;
    }

    


}