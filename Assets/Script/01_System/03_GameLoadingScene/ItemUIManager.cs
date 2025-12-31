using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// I키로 열리는 독립적인 인벤토리 UI 관리
/// PlayerController에서 호출됨
/// 수정사항: 장착된 아이템은 인벤토리에서 숨김, 드래그 앤 드롭 지원
///  드래그 중 UI 갱신 문제 해결
/// </summary>
public class ItemUIManager : MonoBehaviour, IClosableUI
{
    public static ItemUIManager Instance { get; private set; }

    [Header("메인 패널")]
    public GameObject itemUIPanel;
    public Button closeButton;

    [Header("탭 버튼")]
    public Button equipmentTabButton;
    public Button usingitemTabButton;
    public Button etcitemTabButton;
    public Button questitemTabButton;

    [Header("아이템 리스트")]
    public Transform itemListContainer;
    public GameObject itemListPrefab;
    public TextMeshProUGUI itemCountText;

    [Header("아이템 상세 정보")]
    public GameObject itemDetailPanel;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    public TextMeshProUGUI itemStatsText;

    // 처음 열릴 때만 초기화
    private bool isInitialized = false;

    private enum ItemTab
    {
        Equipment,
        Consumable,
        Material,
        QuestItem
    }

    private ItemTab currentTab = ItemTab.Equipment;
    private InventoryItem selectedItem;
    private bool isOpen = false;

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
            return;
        }

        if (itemUIPanel != null)
            itemUIPanel.SetActive(false);

        if (itemDetailPanel != null)
            itemDetailPanel.SetActive(false);

        SetupButtons();
        SubscribeToEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    void Update()
    {
    }

    private void SetupButtons()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseItemUI);

        if (equipmentTabButton != null)
            equipmentTabButton.onClick.AddListener(() => SwitchTab(ItemTab.Equipment));

        if (usingitemTabButton != null)
            usingitemTabButton.onClick.AddListener(() => SwitchTab(ItemTab.Consumable));

        if (etcitemTabButton != null)
            etcitemTabButton.onClick.AddListener(() => SwitchTab(ItemTab.Material));

        if (questitemTabButton != null)
            questitemTabButton.onClick.AddListener(() => SwitchTab(ItemTab.QuestItem));

        // useButton과 discardButton 리스너 제거 - 더블클릭과 드래그로 대체
    }

    private void SubscribeToEvents()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += OnInventoryChanged;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= OnInventoryChanged;
        }
    }
    // ==========================================
    // 인벤토리 UI 열기/닫기
    // ==========================================
    public void OpenItemUI()
    {
        if (isOpen) return;

        // 대화 중이면 인벤토리 창 열지 않음
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
            return;

        isOpen = true;
        itemUIPanel.SetActive(true);

        if (!isInitialized)
        {
            // 기본 탭으로 초기화
            SwitchTab(ItemTab.Equipment);
            isInitialized = true;
        }
        PlayerHUD.Instance?.RegisterUI(this);
        RefreshItemList();
        Debug.Log("[ItemUI] 인벤토리 창 열림");
    }

    public void CloseItemUI()
    {
        if (!isOpen) return;

        //  인벤토리 창을 닫을 때도 드래그 취소
        if (DraggableItemUI.IsDragging())
        {
            DraggableItemUI.CancelCurrentDrag();
        }

        if (DiscardableItemUI.IsDragging())
        {
            DiscardableItemUI.CancelCurrentDrag();
        }

        isOpen = false;
        itemUIPanel.SetActive(false);
        PlayerHUD.Instance?.UnregisterUI(this);
        if (itemDetailPanel != null)
            itemDetailPanel.SetActive(false);

        Debug.Log("[ItemUI] 인벤토리 창 닫힘");
    }

    // ==========================================
    // 탭 전환
    // ==========================================
    private void SwitchTab(ItemTab tab)
    {
        currentTab = tab;
        RefreshItemList();

        // 탭 전환 시 상세정보 숨김
        if (itemDetailPanel != null)
            itemDetailPanel.SetActive(false);
    }

    // ==========================================
    // 아이템 리스트 갱신
    // ==========================================
    private void RefreshItemList()
    {
        //  UI 갱신 전에 진행 중인 드래그가 있다면 취소
        if (DraggableItemUI.IsDragging())
        {
            Debug.Log("[ItemUIManager] 장비 드래그 중 UI 갱신 감지 - 드래그 취소");
            DraggableItemUI.CancelCurrentDrag();
        }

        if (DiscardableItemUI.IsDragging())
        {
            Debug.Log("[ItemUIManager] 아이템 드래그 중 UI 갱신 감지 - 드래그 취소");
            DiscardableItemUI.CancelCurrentDrag();
        }
        //  아이템 리스트가 재생성되면 호버 상태가 사라지므로 상세 패널도 숨김
        if (itemDetailPanel != null)
        {
            itemDetailPanel.SetActive(false);
        }


        // 기존 리스트 아이템 삭제
        foreach (Transform child in itemListContainer)
            Destroy(child.gameObject);

        // 현재 탭에 맞는 아이템 가져오기
        List<InventoryItem> items = GetItemsForCurrentTab();

        // 아이템 리스트 아이템 생성
        foreach (var item in items)
        {
            CreateItemListItem(item);
        }
        if (itemCountText != null)
        {
            int itemCount = items != null ? InventoryManager.Instance.GetAllItems().Count : 0;
            itemCountText.text = $"({itemCount.ToString()}/{InventoryManager.Instance.maxSlots.ToString()})";
        }

        Debug.Log($"[ItemUI] {currentTab} 탭: {items.Count}개 아이템 표시");
    }

    private List<InventoryItem> GetItemsForCurrentTab()
    {
        if (InventoryManager.Instance == null)
            return new List<InventoryItem>();

        switch (currentTab)
        {
            case ItemTab.Equipment:
                return InventoryManager.Instance.GetItemsByType(ItemType.Equipment);

            case ItemTab.Consumable:
                return InventoryManager.Instance.GetItemsByType(ItemType.Consumable);

            case ItemTab.Material:
                return InventoryManager.Instance.GetItemsByType(ItemType.Material);

            case ItemTab.QuestItem:
                return InventoryManager.Instance.GetItemsByType(ItemType.QuestItem);

            default:
                return new List<InventoryItem>();
        }
    }

    private void CreateItemListItem(InventoryItem item)
    {
        GameObject itemObj = Instantiate(itemListPrefab, itemListContainer);
        Image itemImage = itemObj.GetComponent<Image>();
        Button itemButton = itemObj.GetComponent<Button>();
        TextMeshProUGUI itemText = itemObj.GetComponentInChildren<TextMeshProUGUI>();

        // 1. 호버 및 더블클릭 핸들러 컴포넌트를 가져오거나 추가
        ItemDetailUiManager hoverHandler = itemObj.GetComponent<ItemDetailUiManager>();
        if (hoverHandler == null)
        {
            hoverHandler = itemObj.AddComponent<ItemDetailUiManager>();
        }
        hoverHandler.Initialize(item, this);

        ItemData data = item.GetItemData();
        if (data == null) return;

        // 2.  장비 아이템: DraggableItemUI만 추가 (장착 + 버리기 모두 처리)
        if (data.itemType == ItemType.Equipment)
        {
            DraggableItemUI draggable = itemObj.GetComponent<DraggableItemUI>();
            if (draggable == null)
            {
                draggable = itemObj.AddComponent<DraggableItemUI>();
            }
            draggable.Initialize(item);
        }
        // 3.  일반 아이템: DiscardableItemUI만 추가 (버리기만)
        else if (data.disposable)
        {
            DiscardableItemUI discardable = itemObj.GetComponent<DiscardableItemUI>();
            if (discardable == null)
            {
                discardable = itemObj.AddComponent<DiscardableItemUI>();
            }
            discardable.Initialize(item);
        }

        // 4. 아이템 이름 표시
        if (itemText != null)
        {

            string displayText = "";
            if (item.quantity > 1)
            {
                displayText = $"{item.quantity}";
            }
            itemText.text = displayText;
        }
        //5. 아이템 아이콘 표시
        Sprite itemIcon = itemImage.sprite;
        if (itemIcon != null && !string.IsNullOrEmpty(data.iconPath))
        {
            Sprite icon = Resources.Load<Sprite>(data.iconPath);
            if (icon != null)
            {
                itemImage.sprite = icon;
            }
            else
            {
                Debug.LogWarning($"[ShopItemUI] 아이콘을 찾을 수 없음: {data.iconPath}");
            }
        }
    }

    public void ShowItemDetailOnHover(InventoryItem item, Transform buttonTransform)
    {
        selectedItem = item;
        ShowItemDetail(item, buttonTransform);
    }

    public void HideDetailPanelOnHoverExit()
    {
        if (itemDetailPanel == null) return;
        itemDetailPanel.SetActive(false);
    }

    /// <summary>
    /// 패널이 화면 밖으로 나가지 않도록 위치를 조정합니다
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

    private void ShowItemDetail(InventoryItem item, Transform buttonTransform = null)
    {
        if (itemDetailPanel == null) return;

        ItemData data = item.GetItemData();
        if (data == null) return;

        itemDetailPanel.SetActive(true);
        itemDetailPanel.transform.SetAsLastSibling();

        // 상세 패널 위치 조정 (호버 시)
        // 상세 패널 위치 조정 (호버 시)
        if (buttonTransform != null)
        {
            RectTransform detailRect = itemDetailPanel.GetComponent<RectTransform>();
            RectTransform slotRect = buttonTransform.GetComponent<RectTransform>();

            if (detailRect != null && slotRect != null)
            {
                // 버튼 오른쪽에 패널 배치
                Vector3 newPosition = slotRect.position;

                // 버튼의 오른쪽 끝 계산
                float buttonRightEdgeX = slotRect.position.x + slotRect.rect.width * (1 - slotRect.pivot.x);
                float detailPanelPivotCompensation = detailRect.rect.width * detailRect.pivot.x;
                newPosition.x = buttonRightEdgeX + 10f + detailPanelPivotCompensation;

                newPosition.y = slotRect.position.y - 120f;

                //  화면 밖으로 나가지 않도록 위치 조정
                newPosition = ClampToScreen(newPosition, detailRect);

                detailRect.position = newPosition;
            }
        }


        // 이름
        if (itemNameText != null)
            itemNameText.text = data.itemName;

        // 설명
        if (itemDescriptionText != null)
            itemDescriptionText.text = data.description;

        // 스탯 정보
        if (itemStatsText != null)
        {
            string statsText = $"타입: {GetItemTypeName(data.itemType)}\n";
            statsText += $"개수: {item.quantity}\n";

            if (data.itemType == ItemType.Equipment)
            {
                statsText += $"슬롯: {GetEquipSlotName(data.equipSlot)}\n";
                statsText += "\n[보너스 스탯]\n";

                if (data.attackBonus > 0)
                    statsText += $"공격력: +{data.attackBonus}\n";
                if (data.defenseBonus > 0)
                    statsText += $"방어력: +{data.defenseBonus}\n";
                if (data.hpBonus > 0)
                    statsText += $"체력: +{data.hpBonus}\n";
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
            else if (data.itemType == ItemType.Consumable && data.GetHealAmount() > 0)
            {
                statsText += $"회복량: {data.GetHealAmount()} HP\n";
            }

            itemStatsText.text = statsText;
        }

        // 버튼 표시 여부
        // useButton과 discardButton 제거됨 - 더블클릭과 드래그로 대체
    }

    private string GetItemTypeName(ItemType type)
    {
        switch (type)
        {
            case ItemType.Equipment: return "장비";
            case ItemType.Consumable: return "소비 아이템";
            case ItemType.Material: return "재료";
            case ItemType.QuestItem: return "퀘스트 아이템";
            default: return "알 수 없음";
        }
    }

    private string GetEquipSlotName(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Helmet: return "모자";
            case EquipmentSlot.Armor: return "옷";
            case EquipmentSlot.Shoes: return "신발";
            case EquipmentSlot.MeleeWeapon: return "근거리 무기";
            case EquipmentSlot.RangedWeapon: return "원거리 무기";
            case EquipmentSlot.SubWeapon: return "보조무기";
            case EquipmentSlot.Ring: return "반지";
            case EquipmentSlot.Necklace: return "목걸이";
            case EquipmentSlot.Bracelet: return "팔찌";

            default: return "없음";
        }
    }

    // ==========================================
    // 이벤트 핸들러
    // ==========================================
    private void OnInventoryChanged()
    {
        if (isOpen)
        {
            RefreshItemList();

            //  상세 패널은 RefreshItemList()에서 이미 숨겨지므로 
            // 여기서는 다시 표시하지 않음 (호버 시에만 표시)
        }
    }

    // ==========================================
    // 외부에서 호출 가능한 메서드
    // ==========================================
    public void RefreshUI()
    {
        if (isOpen)
            RefreshItemList();
    }

    public bool IsItemUIOpen()
    {
        return isOpen;
    }

    public void Close()
    {
        CloseItemUI();
    }

    public GameObject GetUIPanel()
    {
        return itemUIPanel;
    }
}