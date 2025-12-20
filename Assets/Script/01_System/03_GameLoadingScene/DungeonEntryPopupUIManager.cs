using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 던전 입장 팝업을 관리하는 매니저
/// </summary>
public class DungeonEntryPopupManager : MonoBehaviour, IClosableUI
{
    [Header("UI References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button enterButton;

    [Header("Dungeon Info Display")]
    [SerializeField] private Image dungeonImage;
    [SerializeField] private TextMeshProUGUI dungeonNameText;

    [Header("Item Reward List")]
    [SerializeField] private Transform itemListContainer;  // Horizontal Layout Group
    [SerializeField] private GameObject itemIconPrefab;

    [Header("Party Member List (Future Feature)")]
    [SerializeField] private Transform partyMemberContainer;  // Vertical Layout Group
    [SerializeField] private GameObject partyMemberPrefab;

    private DungeonData currentDungeonData;

    void Start()
    {
        // 버튼 이벤트 등록
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePopup);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(ClosePopup);

        if (enterButton != null)
            enterButton.onClick.AddListener(OnEnterDungeon);

        // 팝업 초기 상태는 비활성화
        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    /// <summary>
    /// 던전 입장 팝업 열기
    /// </summary>
    /// <param name="dungeonId">던전 ID</param>
    public void OpenDungeonEntryPopup(string dungeonId)
    {
        // 던전 데이터 가져오기
        if (DungeonsDataManager.Instance == null)
        {
            Debug.LogError("[DungeonEntryPopupManager] DungeonsDataManager 인스턴스를 찾을 수 없습니다.");
            return;
        }

        currentDungeonData = DungeonsDataManager.Instance.GetDungeonData(dungeonId);
        if (currentDungeonData == null)
        {
            Debug.LogError($"[DungeonEntryPopupManager] 던전 데이터를 찾을 수 없습니다: {dungeonId}");
            return;
        }

        // 팝업 활성화
        if (popupPanel != null)
            popupPanel.SetActive(true);

        PlayerHUD.Instance?.RegisterUI(this);

        // UI 업데이트
        UpdateDungeonInfo();
        UpdateItemRewardList();
        // UpdatePartyMemberList(); // 파티 기능 추가 시 구현
    }

    /// <summary>
    /// 던전 정보 UI 업데이트
    /// </summary>
    private void UpdateDungeonInfo()
    {
        if (currentDungeonData == null) return;

        // 던전 이미지 설정
        if (dungeonImage != null)
        {
            if (!string.IsNullOrEmpty(currentDungeonData.dungeonImagePath))
            {
                // Resources 폴더에서 이미지 로드
                Sprite loadedSprite = Resources.Load<Sprite>(currentDungeonData.dungeonImagePath);
                if (loadedSprite != null)
                {
                    dungeonImage.sprite = loadedSprite;
                }
            }
        }

        // 던전 이름
        if (dungeonNameText != null)
            dungeonNameText.text = currentDungeonData.dungeonName;
    }

    /// <summary>
    /// 예상 획득 아이템 리스트 업데이트
    /// ItemDetailUiManager의 호버 기능을 활용
    /// </summary>
    private void UpdateItemRewardList()
    {
        if (currentDungeonData == null || itemListContainer == null) return;

        // 기존 아이템 UI 제거
        ClearItemRewardList();

        // 아이템 리스트가 없으면 종료
        if (currentDungeonData.clearRewardItems == null || currentDungeonData.clearRewardItems.Count == 0)
        {
            Debug.Log("[DungeonEntryPopupManager] 보상 아이템이 없습니다.");
            return;
        }

        // 아이템 프리팹이 없으면 종료
        if (itemIconPrefab == null)
        {
            Debug.LogError("[DungeonEntryPopupManager] itemIconPrefab이 할당되지 않았습니다.");
            return;
        }

        // ItemDataManager 확인
        if (ItemDataManager.Instance == null)
        {
            Debug.LogError("[DungeonEntryPopupManager] ItemDataManager 인스턴스를 찾을 수 없습니다.");
            return;
        }

        // ItemUIManager 확인 (호버 툴팁용)
        if (ItemUIManager.Instance == null)
        {
            Debug.LogWarning("[DungeonEntryPopupManager] ItemUIManager 인스턴스를 찾을 수 없습니다. 아이템 호버 기능이 작동하지 않습니다.");
        }

        // 아이템 UI 생성
        foreach (var reward in currentDungeonData.clearRewardItems)
        {
            // 아이템 데이터 가져오기
            ItemData itemData = ItemDataManager.Instance.GetItemData(reward.itemId);
            if (itemData == null)
            {
                Debug.LogWarning($"[DungeonEntryPopupManager] 아이템 데이터를 찾을 수 없습니다: {reward.itemId}");
                continue;
            }

            // 아이템 아이콘 UI 생성
            GameObject itemIconObj = Instantiate(itemIconPrefab, itemListContainer);

            // 아이콘 이미지 설정
            Image iconImage = itemIconObj.GetComponent<Image>();
            if (iconImage == null)
            {
                iconImage = itemIconObj.GetComponentInChildren<Image>();
            }

            if (iconImage != null && !string.IsNullOrEmpty(itemData.iconPath))
            {
                Sprite iconSprite = Resources.Load<Sprite>(itemData.iconPath);
                if (iconSprite != null)
                {
                    iconImage.sprite = iconSprite;
                }
            }

            // ItemDetailUiManager 추가하여 호버 기능 활성화
            if (ItemUIManager.Instance != null)
            {
                // InventoryItem 임시 생성 (호버용)
                InventoryItem tempItem = new InventoryItem(reward.itemId, reward.quantity);

                // ItemDetailUiManager 컴포넌트 추가
                ItemDetailUiManager hoverHandler = itemIconObj.GetComponent<ItemDetailUiManager>();
                if (hoverHandler == null)
                {
                    hoverHandler = itemIconObj.AddComponent<ItemDetailUiManager>();
                }

                // 호버 핸들러 초기화
                hoverHandler.Initialize(tempItem, ItemUIManager.Instance);
            }
        }
    }

    /// <summary>
    /// 아이템 리스트 초기화
    /// </summary>
    private void ClearItemRewardList()
    {
        if (itemListContainer == null) return;

        foreach (Transform child in itemListContainer)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 파티원 리스트 업데이트 (향후 파티 기능 추가 시 구현)
    /// </summary>
    private void UpdatePartyMemberList()
    {
        // TODO: 파티 시스템 구현 시 작성
        // 현재는 구현하지 않음

        if (partyMemberContainer == null) return;

        // 기존 파티원 UI 제거
        ClearPartyMemberList();

        // 파티 시스템이 구현되면 여기에 파티원 리스트 생성 로직 추가
        // 예시:
        // foreach (var member in PartyManager.Instance.GetPartyMembers())
        // {
        //     GameObject memberUI = Instantiate(partyMemberPrefab, partyMemberContainer);
        //     // 파티원 정보 설정
        // }
    }

    /// <summary>
    /// 파티원 리스트 초기화
    /// </summary>
    private void ClearPartyMemberList()
    {
        if (partyMemberContainer == null) return;

        foreach (Transform child in partyMemberContainer)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 던전 입장 버튼 클릭 시
    /// </summary>
    private void OnEnterDungeon()
    {
        if (currentDungeonData == null)
        {
            Debug.LogError("[DungeonEntryPopupManager] 현재 던전 데이터가 없습니다.");
            return;
        }

        Debug.Log($"[DungeonEntryPopupManager] 던전 입장: {currentDungeonData.dungeonName} (MapID: {currentDungeonData.entryMapId})");

        // TODO: 실제 던전 입장 로직 구현
        // 예시:
        // SceneManager.LoadScene(currentDungeonData.entryMapId);
        // 또는
        // MapLoadingManager.Instance.LoadMap(currentDungeonData.entryMapId);

        ClosePopup();
    }

    /// <summary>
    /// 팝업 닫기
    /// </summary>
    public void ClosePopup()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);

        currentDungeonData = null;
        PlayerHUD.Instance?.UnregisterUI(this);
        // 리스트 초기화
        ClearItemRewardList();
        ClearPartyMemberList();
    }

    void OnDestroy()
    {
        // 버튼 이벤트 해제
        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePopup);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(ClosePopup);

        if (enterButton != null)
            enterButton.onClick.RemoveListener(OnEnterDungeon);
    }

    public void Close()
    {
        ClosePopup();
    }

    public GameObject GetUIPanel()
    {
        return popupPanel;
    }
}