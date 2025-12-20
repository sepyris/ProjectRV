using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 통합 장비/치장 슬롯 UI
/// 장비 탭과 치장 탭 모드에서 사용 가능
/// 더블클릭으로 장착 해제
/// 드래그하여 인벤토리로 해제 가능
/// 
///  수정사항: 
/// 1. 무기 슬롯 더블클릭 시 MeleeWeapon과 RangedWeapon을 올바르게 판단
/// 2. 상세 패널 위치를 명확하게 슬롯 기준으로 설정
/// 3. 화면 밖으로 나가지 않도록 위치 자동 조정
/// </summary>
public class EquipmentSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI 컴포넌트")]
    public Image itemIconImage;
    public GameObject emptySlotIndicator;

    [Header("상세 정보 패널")]
    public GameObject detailPanel;
    public TextMeshProUGUI detailItemNameText;
    public TextMeshProUGUI detailDescriptionText;
    public TextMeshProUGUI detailStatsText;

    private enum SlotMode
    {
        Equipment,  // 장비 모드
        Cosmetic    // 치장 모드
    }

    private SlotMode mode;
    private EquipmentSlot equipmentSlot;
    private CosmeticSlot cosmeticSlot;
    private InventoryItem currentItem;
    private float lastClickTime = 0f;
    private const float doubleClickThreshold = 0.3f;

    //  드래그 앤 드롭 컴포넌트 
    private EquipmentDropTarget dropTarget;
    private DraggableEquipmentUI draggableEquipment;

    // ==================== 초기화 (장비 모드) ====================
    public void InitializeAsEquipment(EquipmentSlot slot, string slotName)
    {
        mode = SlotMode.Equipment;
        equipmentSlot = slot;

        //  드롭 타겟 초기화 
        dropTarget = gameObject.GetComponent<EquipmentDropTarget>();
        if (dropTarget == null)
        {
            dropTarget = gameObject.AddComponent<EquipmentDropTarget>();
        }
        dropTarget.InitializeAsEquipment(slot);

        //  드래그 가능 컴포넌트 초기화 
        draggableEquipment = gameObject.GetComponent<DraggableEquipmentUI>();
        if (draggableEquipment == null)
        {
            draggableEquipment = gameObject.AddComponent<DraggableEquipmentUI>();
        }
        draggableEquipment.InitializeAsEquipment(slot);

        UpdateSlot(null);
    }

    // ==================== 초기화 (치장 모드) ====================
    public void InitializeAsCosmetic(CosmeticSlot slot, string slotName)
    {
        mode = SlotMode.Cosmetic;
        cosmeticSlot = slot;

        //  드롭 타겟 초기화 
        dropTarget = gameObject.GetComponent<EquipmentDropTarget>();
        if (dropTarget == null)
        {
            dropTarget = gameObject.AddComponent<EquipmentDropTarget>();
        }
        dropTarget.InitializeAsCosmetic(slot);

        //  드래그 가능 컴포넌트 초기화 
        draggableEquipment = gameObject.GetComponent<DraggableEquipmentUI>();
        if (draggableEquipment == null)
        {
            draggableEquipment = gameObject.AddComponent<DraggableEquipmentUI>();
        }
        draggableEquipment.InitializeAsCosmetic(slot);

        UpdateSlot(null);
    }

    // ==================== 슬롯 업데이트 ====================
    public void UpdateSlot(InventoryItem item)
    {
        currentItem = item;
        //  슬롯 내용이 변경되면 상세 패널 숨김 (호버 상태 리셋)
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
        //  이 슬롯이 드래그 중이었다면 취소
        if (draggableEquipment != null && DraggableEquipmentUI.IsDragging())
        {
            // 현재 드래그 중인 것이 이 슬롯인지 체크는 DraggableEquipmentUI 내부에서 처리
            DraggableEquipmentUI.CancelCurrentDrag();
        }



        //  드래그 가능 컴포넌트에 현재 아이템 설정 
        if (draggableEquipment != null)
        {
            draggableEquipment.SetCurrentItem(item);
        }

        if (item == null)
        {
            // 빈 슬롯

            if (itemIconImage != null)
                itemIconImage.enabled = false;

            if (emptySlotIndicator != null)
                emptySlotIndicator.SetActive(true);
        }
        else
        {
            // 장착된 아이템 표시
            ItemData data = item.GetItemData();
            if (data != null)
            {
                if (itemIconImage != null)
                {
                    itemIconImage.enabled = true;
                    itemIconImage.sprite = Resources.Load<Sprite>(data.iconPath);
                }

                if (emptySlotIndicator != null)
                    emptySlotIndicator.SetActive(false);
            }
        }
    }

    // ==================== 클릭 이벤트 ====================
    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem == null) return;

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

    private void OnDoubleClick()
    {
        if (currentItem == null) return;

        ItemData data = currentItem.GetItemData();
        if (data == null) return;

        bool success = false;

        // 모드에 따라 해제 처리
        if (mode == SlotMode.Equipment)
        {
            //  무기 슬롯 특별 처리: 실제 장착된 슬롯을 찾아서 해제 
            EquipmentSlot actualSlot = GetActualEquippedSlot(data);
            success = EquipmentManager.Instance?.UnequipItem(actualSlot) ?? false;
        }
        else // SlotMode.Cosmetic
        {
            success = EquipmentManager.Instance?.UnequipCosmetic(cosmeticSlot) ?? false;
        }

        if (success)
        {
            string modeText = mode == SlotMode.Equipment ? "장비" : "치장";
            Debug.Log($"[EquipmentSlotUI] {data.itemName} {modeText} 해제됨 (더블클릭)");

            //  상세 패널 숨김 (더블클릭으로 해제했으므로)
            HideDetailPanel();

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

    /// <summary>
    ///  실제 장착된 슬롯 찾기 (무기 슬롯 특별 처리)
    /// </summary>
    private EquipmentSlot GetActualEquippedSlot(ItemData data)
    {
        // 무기 슬롯의 경우, 실제 데이터의 equipSlot을 사용
        if (equipmentSlot == EquipmentSlot.MeleeWeapon)
        {
            // 현재 아이템의 실제 슬롯 타입 확인
            if (data.equipSlot == EquipmentSlot.MeleeWeapon)
                return EquipmentSlot.MeleeWeapon;
            else if (data.equipSlot == EquipmentSlot.RangedWeapon)
                return EquipmentSlot.RangedWeapon;
        }

        // 일반 슬롯은 그대로 반환
        return equipmentSlot;
    }

    // ==================== 호버 이벤트 ====================
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem == null || detailPanel == null) return;

        ShowDetailPanel();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideDetailPanel();
    }

    /// <summary>
    ///  상세 패널 숨김 (공개 메서드로 변경하여 외부에서도 호출 가능)
    /// </summary>
    public void HideDetailPanel()
    {
        if (detailPanel != null)
            detailPanel.SetActive(false);
    }

    /// <summary>

    /// <summary>
    ///  패널이 화면 밖으로 나가지 않도록 위치를 조정합니다
    /// </summary>
    private Vector3 ClampToScreen(Vector3 position, RectTransform panelRect)
    {
        if (panelRect == null) return position;

        Canvas canvas = panelRect.GetComponentInParent<Canvas>();
        if (canvas == null) return position;

        // 패널의 크기 (스케일 적용)
        float panelWidth = panelRect.rect.width * panelRect.lossyScale.x;
        float panelHeight = panelRect.rect.height * panelRect.lossyScale.y;

        // 패널의 pivot 고려한 실제 경계 계산
        float leftEdge = position.x - panelWidth * panelRect.pivot.x;
        float rightEdge = position.x + panelWidth * (1 - panelRect.pivot.x);
        float bottomEdge = position.y - panelHeight * panelRect.pivot.y;
        float topEdge = position.y + panelHeight * (1 - panelRect.pivot.y);

        // 화면 경계
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // 여백 (픽셀)
        float padding = 10f;

        // X축 조정
        if (rightEdge > screenWidth - padding)
        {
            // 오른쪽으로 나갔으면 왼쪽으로 이동
            position.x -= (rightEdge - (screenWidth - padding));
        }
        else if (leftEdge < padding)
        {
            // 왼쪽으로 나갔으면 오른쪽으로 이동
            position.x += (padding - leftEdge);
        }

        // Y축 조정
        if (topEdge > screenHeight - padding)
        {
            // 위로 나갔으면 아래로 이동
            position.y -= (topEdge - (screenHeight - padding));
        }
        else if (bottomEdge < padding)
        {
            // 아래로 나갔으면 위로 이동
            position.y += (padding - bottomEdge);
        }

        return position;
    }

    private void ShowDetailPanel()
    {
        if (currentItem == null || detailPanel == null) return;

        ItemData data = currentItem.GetItemData();
        if (data == null) return;

        detailPanel.SetActive(true);
        detailPanel.transform.SetAsLastSibling();

        //  상세 패널 위치를 슬롯 기준으로 명확하게 설정 
        RectTransform detailRect = detailPanel.GetComponent<RectTransform>();
        RectTransform slotRect = GetComponent<RectTransform>();

        if (detailRect != null && slotRect != null)
        {
            // 슬롯 오른쪽에 패널 배치
            Vector3 newPosition = slotRect.position;

            // 슬롯의 오른쪽 끝 계산 (pivot 고려)
            float buttonRightEdgeX = slotRect.position.x + slotRect.rect.width * (1 - slotRect.pivot.x);
            float detailPanelPivotCompensation = detailRect.rect.width * detailRect.pivot.x;
            newPosition.x = buttonRightEdgeX + 10f + detailPanelPivotCompensation;

            // Y 위치는 슬롯보다 아래로
            newPosition.y = slotRect.position.y - 120f;

            //  화면 밖으로 나가지 않도록 위치 조정
            newPosition = ClampToScreen(newPosition, detailRect);

            detailRect.position = newPosition;
        }

        // 아이템 이름
        if (detailItemNameText != null)
            detailItemNameText.text = data.itemName;

        // 설명
        if (detailDescriptionText != null)
            detailDescriptionText.text = data.description;

        // 스탯 정보 (장비 모드일 때만)
        if (detailStatsText != null)
        {
            string statsText = "";

            if (mode == SlotMode.Equipment)
            {
                if (data.attackBonus > 0)
                    statsText += $"공격력: +{data.attackBonus}\n";
                if (data.defenseBonus > 0)
                    statsText += $"방어력: +{data.defenseBonus}\n";
                if (data.strBonus > 0)
                    statsText += $"힘: +{data.strBonus}\n";
                if (data.dexBonus > 0)
                    statsText += $"민첩: +{data.dexBonus}\n";
                if (data.intBonus > 0)
                    statsText += $"지능: +{data.intBonus}\n";
                if (data.lukBonus > 0)
                    statsText += $"행운: +{data.lukBonus}\n";
                if (data.tecBonus > 0)
                    statsText += $"기술: +{data.tecBonus}\n";
            }
            else
            {
                statsText = "외형 전용 아이템";
            }

            detailStatsText.text = statsText;
        }
    }
}