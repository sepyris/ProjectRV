using GameData.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 퀵슬롯 시스템 관리자
/// 1~0번 키로 10개의 퀵슬롯 사용 (넘버패드 제외)
///  수정: 퀵슬롯 간 위치 교환 기능 추가
/// </summary>
public class QuickSlotManager : MonoBehaviour
{
    public static QuickSlotManager Instance { get; private set; }

    [Header("퀵슬롯 설정")]
    [SerializeField] private int maxSlots = 10; // 1~0 키 (10개)

    // 퀵슬롯 데이터 (0~9 인덱스)
    private QuickSlotData[] quickSlots;

    // Input Actions
    private PlayerControls playerControls;

    // 이벤트
    public event Action<int, QuickSlotData> OnQuickSlotChanged;
    public event Action<int> OnQuickSlotUsed;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeSlots();
            InitializeInputActions();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        playerControls?.Enable();
    }

    void OnDisable()
    {
        playerControls?.Disable();
    }

    void OnDestroy()
    {
        if (playerControls != null)
        {
            // 모든 QuickSlot 액션 구독 해제
            playerControls.Player.QuickSlot1.performed -= ctx => UseQuickSlot(0);
            playerControls.Player.QuickSlot2.performed -= ctx => UseQuickSlot(1);
            playerControls.Player.QuickSlot3.performed -= ctx => UseQuickSlot(2);
            playerControls.Player.QuickSlot4.performed -= ctx => UseQuickSlot(3);
            playerControls.Player.QuickSlot5.performed -= ctx => UseQuickSlot(4);
            playerControls.Player.QuickSlot6.performed -= ctx => UseQuickSlot(5);
            playerControls.Player.QuickSlot7.performed -= ctx => UseQuickSlot(6);
            playerControls.Player.QuickSlot8.performed -= ctx => UseQuickSlot(7);
            playerControls.Player.QuickSlot9.performed -= ctx => UseQuickSlot(8);
            playerControls.Player.QuickSlot0.performed -= ctx => UseQuickSlot(9);

            playerControls.Dispose();
            playerControls = null;
        }
    }

    /// <summary>
    /// 퀵슬롯 초기화
    /// </summary>
    private void InitializeSlots()
    {
        quickSlots = new QuickSlotData[maxSlots];
        for (int i = 0; i < maxSlots; i++)
        {
            quickSlots[i] = new QuickSlotData(i);
        }
    }

    /// <summary>
    /// Input Actions 초기화
    /// </summary>
    private void InitializeInputActions()
    {
        playerControls = new PlayerControls();

        // QuickSlot 액션 구독 (1~0키)
        playerControls.Player.QuickSlot1.performed += ctx => UseQuickSlot(0);
        playerControls.Player.QuickSlot2.performed += ctx => UseQuickSlot(1);
        playerControls.Player.QuickSlot3.performed += ctx => UseQuickSlot(2);
        playerControls.Player.QuickSlot4.performed += ctx => UseQuickSlot(3);
        playerControls.Player.QuickSlot5.performed += ctx => UseQuickSlot(4);
        playerControls.Player.QuickSlot6.performed += ctx => UseQuickSlot(5);
        playerControls.Player.QuickSlot7.performed += ctx => UseQuickSlot(6);
        playerControls.Player.QuickSlot8.performed += ctx => UseQuickSlot(7);
        playerControls.Player.QuickSlot9.performed += ctx => UseQuickSlot(8);
        playerControls.Player.QuickSlot0.performed += ctx => UseQuickSlot(9);

        playerControls.Enable();
    }

    /// <summary>
    /// 퀵슬롯에 소모품 등록
    /// </summary>
    public bool RegisterConsumable(int slotIndex, string itemId)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            Debug.LogWarning($"[QuickSlotManager] 잘못된 슬롯 인덱스: {slotIndex}");
            return false;
        }

        // 아이템이 소모품인지 확인
        if (ItemDataManager.Instance != null)
        {
            ItemData itemData = ItemDataManager.Instance.GetItemData(itemId);
            if (itemData == null)
            {
                Debug.LogWarning($"[QuickSlotManager] 아이템을 찾을 수 없음: {itemId}");
                return false;
            }

            if (itemData.itemType != ItemType.Consumable)
            {
                Debug.LogWarning($"[QuickSlotManager] 소모품이 아닌 아이템: {itemData.itemName}");
                return false;
            }
        }

        quickSlots[slotIndex].SetConsumable(itemId);
        OnQuickSlotChanged?.Invoke(slotIndex, quickSlots[slotIndex]);
        Debug.Log($"[QuickSlotManager] 슬롯 {slotIndex + 1}에 아이템 등록: {itemId}");
        return true;
    }

    /// <summary>
    /// 퀵슬롯에 스킬 등록 (추후 확장용)
    /// </summary>
    public bool RegisterSkill(int slotIndex, string skillId)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            Debug.LogWarning($"[QuickSlotManager] 잘못된 슬롯 인덱스: {slotIndex}");
            return false;
        }

        // TODO: 스킬 데이터 검증 (스킬 시스템 구현 후)

        quickSlots[slotIndex].SetSkill(skillId);
        OnQuickSlotChanged?.Invoke(slotIndex, quickSlots[slotIndex]);
        Debug.Log($"[QuickSlotManager] 슬롯 {slotIndex + 1}에 스킬 등록: {skillId}");
        return true;
    }

    /// <summary>
    /// 퀵슬롯 비우기
    /// </summary>
    public void ClearSlot(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
            return;

        quickSlots[slotIndex].Clear();
        OnQuickSlotChanged?.Invoke(slotIndex, quickSlots[slotIndex]);
        Debug.Log($"[QuickSlotManager] 슬롯 {slotIndex + 1} 비움");
    }

    /// <summary>
    ///  두 퀵슬롯의 위치를 서로 교환
    /// </summary>
    public void SwapQuickSlots(int sourceIndex, int targetIndex)
    {
        if (!IsValidSlotIndex(sourceIndex) || !IsValidSlotIndex(targetIndex))
        {
            Debug.LogWarning($"[QuickSlotManager] 잘못된 슬롯 인덱스: {sourceIndex} <-> {targetIndex}");
            return;
        }

        if (sourceIndex == targetIndex)
        {
            Debug.Log("[QuickSlotManager] 같은 슬롯으로 이동 시도 - 무시");
            return;
        }

        // 슬롯 데이터 교환
        QuickSlotData temp = quickSlots[sourceIndex];
        quickSlots[sourceIndex] = quickSlots[targetIndex];
        quickSlots[targetIndex] = temp;

        // 슬롯 인덱스도 업데이트
        quickSlots[sourceIndex].slotIndex = sourceIndex;
        quickSlots[targetIndex].slotIndex = targetIndex;

        // 두 슬롯 모두 변경 이벤트 발생
        OnQuickSlotChanged?.Invoke(sourceIndex, quickSlots[sourceIndex]);
        OnQuickSlotChanged?.Invoke(targetIndex, quickSlots[targetIndex]);

        Debug.Log($"[QuickSlotManager] 퀵슬롯 교환: {sourceIndex + 1} <-> {targetIndex + 1}");
    }

    /// <summary>
    /// 퀵슬롯 사용
    /// </summary>
    public void UseQuickSlot(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
            return;

        QuickSlotData slot = quickSlots[slotIndex];

        if (slot.IsEmpty())
        {
            Debug.Log($"[QuickSlotManager] 슬롯 {slotIndex + 1}이 비어있음");
            return;
        }

        switch (slot.slotType)
        {
            case QuickSlotType.Consumable:
                UseConsumable(slotIndex, slot.itemId);
                break;

            case QuickSlotType.Skill:
                UseSkill(slotIndex, slot.skillId);
                break;
        }

        OnQuickSlotUsed?.Invoke(slotIndex);
    }

    /// <summary>
    /// 소모품 사용 (수정됨 - 체력 회복 OR 아이템 지급)
    /// </summary>
    private void UseConsumable(int slotIndex, string itemId)
    {
        // 인벤토리에 아이템이 있는지 확인
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[QuickSlotManager] InventoryManager가 없음");
            return;
        }

        InventoryItem item = InventoryManager.Instance.GetItem(itemId);
        if (item == null || item.quantity <= 0)
        {
            Debug.LogWarning($"[QuickSlotManager] 인벤토리에 아이템이 없음: {itemId}");
            // 슬롯 비우기
            ClearSlot(slotIndex);
            return;
        }

        // 아이템 데이터 가져오기
        ItemData itemData = item.GetItemData();
        if (itemData == null)
        {
            Debug.LogWarning($"[QuickSlotManager] 아이템 데이터를 찾을 수 없음: {itemId}");
            return;
        }

        //  consumableEffect 처리
        if (string.IsNullOrEmpty(itemData.consumableEffect))
        {
            Debug.LogWarning($"[QuickSlotManager] {itemData.itemName}은(는) 사용 효과가 없습니다.");
            return;
        }

        //  체력 회복 효과인지 확인
        if (itemData.IsHealEffect())
        {
            int healAmount = itemData.GetHealAmount();
            if (healAmount > 0 && PlayerStatsComponent.Instance != null)
            {
                if (PlayerStatsComponent.Instance.Stats.currentHP == PlayerStatsComponent.Instance.Stats.maxHP)
                {

                    FloatingNotificationManager.Instance.ShowNotification("체력이 가득 차 있습니다.");
                    return;
                }
                else
                {
                    PlayerStatsComponent.Instance.Stats.Heal(healAmount);
                    Debug.Log($"[QuickSlotManager] {itemData.itemName} 사용 - HP {healAmount} 회복");
                }
            }
        }
        //  아이템 지급 효과
        else
        {
            var rewards = itemData.GetItemRewards();
            if (rewards != null && rewards.Count > 0)
            {
                //  사전 검증: 모든 보상을 받을 수 있는 공간이 있는지 확인
                if (InventoryManager.Instance != null)
                {
                    int requiredSlots = CalculateRequiredSlots(rewards);
                    int availableSlots = InventoryManager.Instance.GetAvailableSlots();

                    if (availableSlots < requiredSlots)
                    {
                        PopupManager.Instance.ShowWarningPopup($"인벤토리가 가득차서 사용할수 없습니다.\n{requiredSlots}칸을 비우고 다시 사용해주세요");
                        return; // 사용 취소
                    }
                }

                Debug.Log($"[QuickSlotManager] {itemData.itemName} 사용 - 아이템 획득");

                foreach (var reward in rewards)
                {
                    if (InventoryManager.Instance != null)
                    {
                        bool added = InventoryManager.Instance.AddItem(reward.itemId, reward.quantity);

                        if (added)
                        {
                            // 획득한 아이템 정보 표시
                            ItemData rewardItemData = ItemDataManager.Instance?.GetItemData(reward.itemId);
                            string rewardName = rewardItemData != null ? rewardItemData.itemName : reward.itemId;
                            Debug.Log($"  - {rewardName}:{reward.quantity} 획득");

                            if (FloatingNotificationManager.Instance != null)
                            {
                                FloatingNotificationManager.Instance.ShowNotification($"{rewardName} :{reward.quantity} 획득!");
                            }
                        }
                    }
                }
            }
        }

        // 인벤토리에서 아이템 1개 제거
        InventoryManager.Instance.RemoveItem(itemId, 1);

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


        // 아이템이 모두 소진되면 슬롯 비우기
        InventoryItem remainingItem = InventoryManager.Instance.GetItem(itemId);
        if (remainingItem == null || remainingItem.quantity <= 0)
        {
            ClearSlot(slotIndex);
        }
    }

    /// <summary>
    /// 스킬 사용 (추후 확장용)
    /// </summary>
    private void UseSkill(int slotIndex, string skillId)
    {
        // TODO: 스킬 시스템 구현
        Debug.Log($"[QuickSlotManager] 스킬 사용: {skillId} (미구현)");
    }

    /// <summary>
    /// 특정 아이템이 퀵슬롯에 등록되어 있는지 확인
    /// </summary>
    public bool IsItemInQuickSlot(string itemId)
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (quickSlots[i].slotType == QuickSlotType.Consumable &&
                quickSlots[i].itemId == itemId)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 특정 아이템이 등록된 슬롯 인덱스 찾기
    /// </summary>
    public int FindItemSlotIndex(string itemId)
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (quickSlots[i].slotType == QuickSlotType.Consumable &&
                quickSlots[i].itemId == itemId)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 퀵슬롯 데이터 가져오기
    /// </summary>
    public QuickSlotData GetSlotData(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
            return null;

        return quickSlots[slotIndex];
    }

    /// <summary>
    /// 모든 퀵슬롯 데이터 가져오기
    /// </summary>
    public QuickSlotData[] GetAllSlots()
    {
        return quickSlots;
    }

    /// <summary>
    /// 유효한 슬롯 인덱스인지 확인
    /// </summary>
    private bool IsValidSlotIndex(int index)
    {
        return index >= 0 && index < maxSlots;
    }

    /// <summary>
    /// 저장 데이터로 변환
    /// </summary>
    public List<QuickSlotSaveData> GetSaveData()
    {
        List<QuickSlotSaveData> saveData = new List<QuickSlotSaveData>();
        for (int i = 0; i < maxSlots; i++)
        {
            if (!quickSlots[i].IsEmpty())
            {
                saveData.Add(quickSlots[i].ToSaveData());
            }
        }
        return saveData;
    }

    /// <summary>
    /// 저장 데이터에서 복원
    /// </summary>
    public void LoadFromSaveData(List<QuickSlotSaveData> saveData)
    {
        // 모든 슬롯 초기화
        InitializeSlots();

        // 저장된 데이터 복원
        if (saveData != null)
        {
            foreach (var data in saveData)
            {
                if (IsValidSlotIndex(data.slotIndex))
                {
                    quickSlots[data.slotIndex] = QuickSlotData.FromSaveData(data);
                    OnQuickSlotChanged?.Invoke(data.slotIndex, quickSlots[data.slotIndex]);
                }
            }
        }

        Debug.Log($"[QuickSlotManager] 퀵슬롯 로드 완료: {saveData?.Count ?? 0}개");
    }

    /// <summary>
    /// 모든 슬롯 초기화
    /// </summary>
    public void ClearAllSlots()
    {
        for (int i = 0; i < maxSlots; i++)
        {
            ClearSlot(i);
        }
        Debug.Log("[QuickSlotManager] 모든 퀵슬롯 초기화");
    }

    /// <summary>
    /// 보상 아이템들을 받기 위해 필요한 인벤토리 슬롯 수 계산
    /// </summary>
    private int CalculateRequiredSlots(List<ItemReward> rewards)
    {
        if (InventoryManager.Instance == null)
            return 0;

        int requiredSlots = 0;

        //단순히 보상 아이템 종류 수를 셈
        requiredSlots = rewards.Count;

        return requiredSlots;
    }
}