using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 스킬 UI를 드래그할 수 있게 해주는 컴포넌트
/// 퀵슬롯으로 드래그하여 등록 가능
/// DraggableItemUI 패턴을 따름
/// </summary>
public class DraggableSkillUi : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // 현재 드래그 중인 인스턴스 추적 (정적 변수)
    private static DraggableSkillUi currentDragging = null;

    private PlayerSkillData skillData;
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private GameObject dragVisual;

    /// <summary>
    /// 드래그 중인 스킬이 있는지 확인
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
            Debug.Log("[DraggableSkillUI] 드래그 강제 취소 (UI 갱신으로 인한)");
            currentDragging.CleanupDrag();
            currentDragging = null;
        }
    }

    /// <summary>
    /// 스킬 데이터 가져오기 (QuickSlotUI에서 사용)
    /// </summary>
    public PlayerSkillData GetSkillData()
    {
        return skillData;
    }

    public void Initialize(PlayerSkillData skill)
    {
        skillData = skill;

        // 이미지 컴포넌트의 RectTransform 사용
        Image iconImage = GetComponent<Image>();
        if (iconImage != null)
        {
            rectTransform = iconImage.rectTransform;
        }
        else
        {
            rectTransform = GetComponent<RectTransform>();
        }

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
        if (skillData == null) return;

        SkillData data = skillData.GetSkillData();
        if (data == null) return;
        if (data.skillType == SkillType.Passive) return; // 패시브스킬은 드래그 안됨

        // 이미 다른 드래그가 진행 중이면 취소
        if (currentDragging != null && currentDragging != this)
        {
            currentDragging.CleanupDrag();
        }

        // 현재 드래그 중으로 표시
        currentDragging = this;

        // 드래그 비주얼 생성 - 작은 아이콘만
        CreateDragVisual(data);

        // 원본은 투명하게
        canvasGroup.alpha = 0.3f;
        canvasGroup.blocksRaycasts = false;

        Debug.Log($"[DraggableSkillUI] 드래그 시작: {data.skillName}");
    }

    /// <summary>
    /// 드래그 비주얼 생성 (아이콘만 표시)
    /// </summary>
    private void CreateDragVisual(SkillData data)
    {
        // 드래그 비주얼용 작은 GameObject 생성
        dragVisual = new GameObject("SkillDragVisual");
        dragVisual.transform.SetParent(canvas.transform, false);

        // RectTransform 설정
        RectTransform dragRect = dragVisual.AddComponent<RectTransform>();
        dragRect.sizeDelta = new Vector2(64, 64); // 아이콘 크기
        dragRect.position = rectTransform.position;

        // Image 컴포넌트 추가
        Image dragImage = dragVisual.AddComponent<Image>();

        // 아이콘 스프라이트 로드
        if (!string.IsNullOrEmpty(data.skillIconPath))
        {
            Sprite icon = Resources.Load<Sprite>(data.skillIconPath);
            if (icon != null)
            {
                dragImage.sprite = icon;
            }
            else
            {
                Debug.LogWarning($"[DraggableSkillUI] 아이콘을 찾을 수 없음: {data.skillIconPath}");
            }
        }

        // CanvasGroup 설정 (투명도 및 레이캐스트 차단)
        CanvasGroup dragVisualCanvasGroup = dragVisual.AddComponent<CanvasGroup>();
        dragVisualCanvasGroup.blocksRaycasts = false;
        dragVisualCanvasGroup.alpha = 0.7f;
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
        if (skillData == null)
        {
            CleanupDrag();
            return;
        }

        SkillData data = skillData.GetSkillData();
        if (data == null)
        {
            CleanupDrag();
            return;
        }

        // 드롭 대상 찾기
        GameObject dropTarget = eventData.pointerCurrentRaycast.gameObject;

        Debug.Log($"[DraggableSkillUI] 드롭 대상: {(dropTarget != null ? dropTarget.name : "null")}");

        // 퀵슬롯에 드롭되었는지 확인
        bool droppedOnQuickSlot = false;

        if (dropTarget != null)
        {
            QuickSlotUI quickSlot = dropTarget.GetComponent<QuickSlotUI>();
            if (quickSlot == null)
            {
                quickSlot = dropTarget.GetComponentInParent<QuickSlotUI>();
            }

            if (quickSlot != null)
            {
                // QuickSlotUI의 OnDrop이 자동으로 호출됨
                droppedOnQuickSlot = true;
                Debug.Log($"[DraggableSkillUI] 퀵슬롯에 드롭됨");
            }
        }

        if (!droppedOnQuickSlot)
        {
            Debug.Log("[DraggableSkillUI] 퀵슬롯 외부에 드롭 - 아무 일도 일어나지 않음");
        }

        CleanupDrag();
    }

    /// <summary>
    /// 드래그 정리
    /// </summary>
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

        // 현재 드래그 중 상태 해제
        if (currentDragging == this)
        {
            currentDragging = null;
        }
    }
}