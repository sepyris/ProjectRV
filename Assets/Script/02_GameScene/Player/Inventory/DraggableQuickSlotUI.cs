using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 퀵슬롯에서 아이템을 드래그하여 제거할 수 있는 컴포넌트
/// QuickSlotUI에 추가로 붙여서 사용
///  수정: 퀵슬롯 간 이동 기능 추가
/// </summary>
public class DraggableQuickSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private QuickSlotUI quickSlotUI;
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private GameObject dragVisual;
    private Vector3 startPosition;

    [Header("드래그 설정")]
    [SerializeField] private float dragOutDistance = 50f; // 이 거리 이상 드래그하면 제거

    void Awake()
    {
        quickSlotUI = GetComponent<QuickSlotUI>();
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
        // 빈 슬롯은 드래그 불가
        if (QuickSlotManager.Instance == null)
            return;

        int slotIndex = GetSlotIndex();
        QuickSlotData slotData = QuickSlotManager.Instance.GetSlotData(slotIndex);

        if (slotData == null || slotData.IsEmpty())
            return;

        // 시작 위치 저장
        startPosition = rectTransform.position;

        // 드래그 비주얼 생성
        dragVisual = Instantiate(gameObject, canvas.transform);
        RectTransform dragRect = dragVisual.GetComponent<RectTransform>();
        dragRect.position = rectTransform.position;
        dragRect.sizeDelta = rectTransform.sizeDelta;

        // 드래그 비주얼 설정
        CanvasGroup dragVisualCanvasGroup = dragVisual.GetComponent<CanvasGroup>();
        if (dragVisualCanvasGroup == null)
        {
            dragVisualCanvasGroup = dragVisual.AddComponent<CanvasGroup>();
        }
        dragVisualCanvasGroup.blocksRaycasts = false;
        dragVisualCanvasGroup.alpha = 0.7f;

        // DraggableQuickSlotUI 컴포넌트 제거 (복제본에서)
        DraggableQuickSlotUI dragScript = dragVisual.GetComponent<DraggableQuickSlotUI>();
        if (dragScript != null)
        {
            Destroy(dragScript);
        }

        // 원본은 약간 투명하게
        canvasGroup.alpha = 0.5f;

        Debug.Log("[DraggableQuickSlotUI] 퀵슬롯 드래그 시작");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragVisual != null)
        {
            dragVisual.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 드래그 비주얼 제거
        if (dragVisual != null)
        {
            Destroy(dragVisual);
        }

        // 원본 투명도 복원
        canvasGroup.alpha = 1f;

        //  드롭된 위치에 QuickSlotUI가 있는지 확인
        bool droppedOnQuickSlot = false;
        int sourceSlotIndex = GetSlotIndex();

        // Raycast로 드롭 대상 확인
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            QuickSlotUI targetSlot = result.gameObject.GetComponent<QuickSlotUI>();
            if (targetSlot != null)
            {
                // 같은 슬롯이 아닌 경우에만 처리
                int targetSlotIndex = targetSlot.transform.GetSiblingIndex();
                if (targetSlotIndex != sourceSlotIndex)
                {
                    //  퀵슬롯 간 이동 처리
                    if (QuickSlotManager.Instance != null)
                    {
                        QuickSlotManager.Instance.SwapQuickSlots(sourceSlotIndex, targetSlotIndex);
                        Debug.Log($"[DraggableQuickSlotUI] 퀵슬롯 {sourceSlotIndex + 1} → {targetSlotIndex + 1} 이동");
                    }
                    droppedOnQuickSlot = true;
                    break;
                }
            }
        }

        //  퀵슬롯에 드롭되지 않은 경우, 거리 확인하여 제거 여부 결정
        if (!droppedOnQuickSlot)
        {
            float dragDistance = Vector3.Distance(startPosition, eventData.position);

            if (dragDistance > dragOutDistance)
            {
                // 충분히 멀리 드래그했으면 슬롯 비우기
                if (QuickSlotManager.Instance != null)
                {
                    QuickSlotManager.Instance.ClearSlot(sourceSlotIndex);
                    Debug.Log($"[DraggableQuickSlotUI] 퀵슬롯 {sourceSlotIndex + 1} 해제 (드래그 거리: {dragDistance})");
                }
            }
            else
            {
                Debug.Log("[DraggableQuickSlotUI] 드래그 거리 부족 - 슬롯 유지");
            }
        }
    }

    /// <summary>
    /// QuickSlotUI로부터 슬롯 인덱스 가져오기
    /// </summary>
    private int GetSlotIndex()
    {
        if (quickSlotUI != null)
        {
            // Reflection을 사용하거나, QuickSlotUI에 public getter 추가 필요
            // 여기서는 간단하게 부모 계층에서 인덱스 추출
            int siblingIndex = transform.GetSiblingIndex();
            return siblingIndex;
        }
        return 0;
    }
}