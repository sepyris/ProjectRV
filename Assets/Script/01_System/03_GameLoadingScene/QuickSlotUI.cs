using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


/// 개별 퀵슬롯 UI
/// 드래그로 아이템/스킬 등록, 드래그로 밖으로 빼면 해제
///  호버 기능 추가: 아이템 상세 정보 표시
///  화면 밖으로 나가지 않도록 위치 자동 조정

public class QuickSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI 참조")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TextMeshProUGUI keyBindText;
    [SerializeField] private Image cooldownOverlay; // 쿨다운 표시용 (선택사항)
    [SerializeField] private TextMeshProUGUI cooldownText;

    [Header("설정")]
    [SerializeField] private int slotIndex; // 0~9
    [SerializeField] private Sprite emptySlotSprite;

    private QuickSlotData slotData;

    private SkillBase currentSkill;
    private bool isOnCooldown = false;

    void Start()
    {
        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = 0f;
            cooldownOverlay.enabled = false;
        }

        if (cooldownText != null)
        {
            cooldownText.enabled = false;
        }

        // 기존 코드
        // QuickSlotManager 이벤트 구독
        if (QuickSlotManager.Instance != null)
        {
            QuickSlotManager.Instance.OnQuickSlotChanged += OnSlotChanged;
            QuickSlotManager.Instance.OnQuickSlotUsed += OnSlotUsed;
        }

        // 초기 슬롯 데이터 가져오기
        if (QuickSlotManager.Instance != null)
        {
            slotData = QuickSlotManager.Instance.GetSlotData(slotIndex);
            UpdateUI();
        }

        // 키 바인딩 표시 (1~0)
        UpdateKeyBindText();
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (QuickSlotManager.Instance != null)
        {
            QuickSlotManager.Instance.OnQuickSlotChanged -= OnSlotChanged;
            QuickSlotManager.Instance.OnQuickSlotUsed -= OnSlotUsed;
        }
    }
    void Update()
    {
        UpdateCooldown();
    }

    /// <summary>
    /// 쿨타임 업데이트
    /// </summary>
    private void UpdateCooldown()
    {
        if (currentSkill == null)
            return;

        if (currentSkill.IsOnCooldown)
        {
            // 쿨타임 시작
            if (!isOnCooldown)
            {
                isOnCooldown = true;
                if (cooldownOverlay != null)
                    cooldownOverlay.enabled = true;
                if (cooldownText != null)
                    cooldownText.enabled = true;
            }

            // 쿨타임 진행도
            float progress = currentSkill.CooldownProgress;  // 0 → 1
            float fillAmount = 1f - progress;  // 1 → 0

            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = fillAmount;
            }

            // 남은 시간
            if (cooldownText != null)
            {
                float remaining = currentSkill.CooldownRemaining;

                if (remaining >= 1f)
                {
                    cooldownText.text = Mathf.Ceil(remaining).ToString();
                }
                else
                {
                    cooldownText.text = remaining.ToString("F1");
                }
            }
        }
        else
        {
            // 쿨타임 종료
            if (isOnCooldown)
            {
                isOnCooldown = false;

                if (cooldownOverlay != null)
                {
                    cooldownOverlay.fillAmount = 0f;
                    cooldownOverlay.enabled = false;
                }

                if (cooldownText != null)
                {
                    cooldownText.enabled = false;
                }
            }
        }
    }

    /// 슬롯 인덱스 설정

    public void SetSlotIndex(int index)
    {
        slotIndex = index;
        UpdateKeyBindText();
    }

    
    /// 키 바인딩 텍스트 업데이트
    
    private void UpdateKeyBindText()
    {
        if (keyBindText != null)
        {
            // 0~8 -> 1~9, 9 -> 0
            int displayKey = (slotIndex + 1) % 10;
            keyBindText.text = displayKey.ToString();
        }
    }


    /// 슬롯 변경 이벤트 핸들러

    private void OnSlotChanged(int index, QuickSlotData data)
    {
        if (index == slotIndex)
        {
            slotData = data;

            if (data == null || data.IsEmpty() || data.slotType != QuickSlotType.Skill)
            {
                currentSkill = null;

                // 쿨타임 UI 초기화
                if (cooldownOverlay != null)
                {
                    cooldownOverlay.fillAmount = 0f;
                    cooldownOverlay.enabled = false;
                }

                if (cooldownText != null)
                {
                    cooldownText.enabled = false;
                }
            }

            UpdateUI();
        }
    }


    /// 슬롯 사용 이벤트 핸들러 (애니메이션 등)

    private void OnSlotUsed(int index)
    {
        if (index == slotIndex)
        {
            // UI 갱신 (수량 업데이트)
            UpdateUI();

            // TODO: 사용 애니메이션, 쿨다운 시작 등
            Debug.Log($"[QuickSlotUI] 슬롯 {slotIndex + 1} 사용됨");
        }
    }

    
    /// UI 업데이트
    
    private void UpdateUI()
    {
        //  슬롯 내용이 변경되면 ItemUIManager의 상세 패널 숨김 (호버 상태 리셋)
        if (ItemUIManager.Instance != null)
        {
            ItemUIManager.Instance.HideDetailPanelOnHoverExit();
        }

        if (slotData == null || slotData.IsEmpty())
        {
            currentSkill = null;
            isOnCooldown = false;

            // 쿨타임 UI 초기화
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = 0f;
                cooldownOverlay.enabled = false;
            }
            // 빈 슬롯
            if (iconImage != null)
            {
                iconImage.sprite = emptySlotSprite;
                iconImage.color = new Color(1, 1, 1, 0.3f); // 반투명
            }
            if (quantityText != null)
            {
                quantityText.gameObject.SetActive(false);
            }
            return;
        }

        // 등록된 아이템/스킬 표시
        switch (slotData.slotType)
        {
            case QuickSlotType.Consumable:
                currentSkill = null;
                UpdateConsumableUI(slotData.itemId);
                break;

            case QuickSlotType.Skill:
                UpdateSkillUI(slotData.skillId);
                break;
        }
    }

    
    /// 소모품 UI 업데이트
    
    private void UpdateConsumableUI(string itemId)
    {
        currentSkill = null;

        if (ItemDataManager.Instance == null)
            return;

        ItemData itemData = ItemDataManager.Instance.GetItemData(itemId);
        if (itemData == null)
            return;

        // 아이콘 표시
        if (iconImage != null)
        {
            Sprite icon = Resources.Load<Sprite>(itemData.iconPath);
            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.color = Color.white;
            }
        }

        // 수량 표시
        if (quantityText != null)
        {
            if (InventoryManager.Instance != null)
            {
                InventoryItem item = InventoryManager.Instance.GetItem(itemId);
                if (item != null && item.quantity > 0)
                {
                    quantityText.gameObject.SetActive(true);
                    quantityText.text = item.quantity.ToString();
                }
                else
                {
                    // 아이템이 없으면 슬롯 비우기
                    quantityText.gameObject.SetActive(false);
                    if (QuickSlotManager.Instance != null)
                    {
                        QuickSlotManager.Instance.ClearSlot(slotIndex);
                    }
                }
            }
        }
    }

    /// 스킬 UI 업데이트
    // QuickSlotUI.cs - UpdateSkillUI() 수정

    /// <summary>
    /// 스킬 UI 업데이트
    /// </summary>
    private void UpdateSkillUI(string skillId)
    {
        if (SkillDataManager.Instance == null)
            return;

        SkillData skillData = SkillDataManager.Instance.GetSkillData(skillId);
        if (skillData == null)
        {
            Debug.LogWarning($"[QuickSlotUI] 스킬 데이터를 찾을 수 없음: {skillId}");
            currentSkill = null;
            return;
        }

        // ===== 스킬 인스턴스 가져오기 =====

        if (SkillManager.Instance != null)
        {
            currentSkill = SkillManager.Instance.GetSkillInstance(skillId);


            if (currentSkill != null && !currentSkill.IsOnCooldown)
            {
                // 쿨타임이 아닐 때만 UI 초기화
                if (cooldownOverlay != null)
                {
                    cooldownOverlay.fillAmount = 0f;
                    cooldownOverlay.enabled = false;
                }

                if (cooldownText != null)
                {
                    cooldownText.enabled = false;
                }
            }
        }
        else
        {
            currentSkill = null;
        }

        // ===== 아이콘 설정 =====

        if (iconImage != null)
        {
            if (!string.IsNullOrEmpty(skillData.skillIconPath))
            {
                Sprite icon = Resources.Load<Sprite>(skillData.skillIconPath);
                if (icon != null)
                {
                    iconImage.sprite = icon;
                    iconImage.color = Color.white;
                }
                else
                {
                    Debug.LogWarning($"[QuickSlotUI] 스킬 아이콘을 찾을 수 없음: {skillData.skillIconPath}");
                    iconImage.color = Color.cyan;
                }
            }
            else
            {
                iconImage.color = Color.cyan;
            }
        }

        // 스킬은 수량 표시 안 함
        if (quantityText != null)
        {
            quantityText.gameObject.SetActive(false);
        }
    }

    // ==========================================
    //  호버 기능 추가
    // ==========================================


    /// 마우스 호버 시작 - 아이템 상세 정보 표시

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 슬롯이 비어있으면 무시
        if (slotData == null || slotData.IsEmpty())
            return;

        // 아이템중 소모품일 경우만 상세 정보 표시
        if (slotData.slotType == QuickSlotType.Consumable)
        {
            // InventoryManager에서 아이템 가져오기
            if (InventoryManager.Instance != null)
            {
                InventoryItem item = InventoryManager.Instance.GetItem(slotData.itemId);
                if (item != null && ItemUIManager.Instance != null)
                {
                    // ItemUIManager의 호버 표시 함수 호출
                    ItemUIManager.Instance.ShowItemDetailOnHover(item, this.transform);
                }
            }
        }
        //스킬일 경우 상세 정보 표시
        if (slotData.slotType == QuickSlotType.Skill)
        {

        }

    }

    
    /// 마우스 호버 종료 - 상세 정보 숨기기
    
    public void OnPointerExit(PointerEventData eventData)
    {
        // 슬롯이 비어있으면 무시
        if (slotData == null || slotData.IsEmpty())
            return;

        // 소모품일 경우만 상세 정보 숨기기
        if (slotData.slotType == QuickSlotType.Consumable)
        {
            if (ItemUIManager.Instance != null)
            {
                ItemUIManager.Instance.HideDetailPanelOnHoverExit();
            }
        }
        //
        if(slotData.slotType == QuickSlotType.Skill)
        {

        }
    }

    // ==========================================
    // 드롭 및 클릭 처리
    // ==========================================

    
    /// 드롭 처리 - 아이템을 퀵슬롯에 등록
    
    public void OnDrop(PointerEventData eventData)
    {
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null)
            return;

        // DraggableItemUI에서 드롭된 경우
        DraggableItemUI draggableItem = draggedObject.GetComponent<DraggableItemUI>();
        if (draggableItem != null)
        {
            InventoryItem item = draggableItem.GetItem();
            if (item != null)
            {
                ItemData itemData = item.GetItemData();
                if (itemData != null && itemData.itemType == ItemType.Consumable)
                {
                    // 소모품만 퀵슬롯에 등록 가능
                    if (QuickSlotManager.Instance != null)
                    {
                        QuickSlotManager.Instance.RegisterConsumable(slotIndex, item.itemid);
                    }
                }
                else
                {
                    Debug.Log("[QuickSlotUI] 소모품만 퀵슬롯에 등록 가능합니다.");
                }
            }
            return;
        }
        DraggableSkillUi draggableSkill = draggedObject.GetComponent<DraggableSkillUi>();
        if (draggableSkill != null)
        {
            PlayerSkillData skill = draggableSkill.GetSkillData();
            if (skill != null)
            {
                SkillData skilldata = skill.GetSkillData();
                if (skilldata != null)
                {
                    if (skilldata.skillType == SkillType.Passive)
                    {
                        return;
                    }
                    if (QuickSlotManager.Instance != null)
                    {
                        QuickSlotManager.Instance.RegisterSkill(slotIndex, skilldata.skillId);
                    }
                }
            }
            return;
        }
    }

    
    /// 클릭 처리 - 퀵슬롯 사용
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // 좌클릭: 아이템 사용
            if (QuickSlotManager.Instance != null)
            {
                QuickSlotManager.Instance.UseQuickSlot(slotIndex);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // 우클릭: 슬롯 비우기
            if (QuickSlotManager.Instance != null)
            {
                QuickSlotManager.Instance.ClearSlot(slotIndex);
            }
        }
    }

    
    /// 공개 메서드: 외부에서 UI 강제 업데이트
    
    public void RefreshUI()
    {
        if (QuickSlotManager.Instance != null)
        {
            slotData = QuickSlotManager.Instance.GetSlotData(slotIndex);
            UpdateUI();
        }
    }
}