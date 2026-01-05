using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 아이템과 스킬 사용 처리 통합 핸들러
/// - 더블클릭으로 아이템/스킬 사용
/// - 퀵슬롯에서 아이템/스킬 사용
/// 모든 사용 로직을 한 곳에서 관리
/// </summary>
public static class UsageHandler
{
    // ==================== 아이템 사용 ====================
    /// 소모품 아이템 사용 (공통 로직)
    public static bool UseConsumable(string itemId, bool removeFromInventory = true)
    {
        // 1. 인벤토리에 아이템이 있는지 확인
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[UsageHandler] InventoryManager가 없음");
            return false;
        }

        InventoryItem item = InventoryManager.Instance.GetItem(itemId);
        if (item == null || item.quantity <= 0)
        {
            Debug.LogWarning($"[UsageHandler] 인벤토리에 아이템이 없음: {itemId}");
            return false;
        }

        // 2. 아이템 데이터 가져오기
        ItemData itemData = item.GetItemData();
        if (itemData == null)
        {
            Debug.LogWarning($"[UsageHandler] 아이템 데이터를 찾을 수 없음: {itemId}");
            return false;
        }

        // 3. consumableEffect 확인
        if (string.IsNullOrEmpty(itemData.consumableEffect))
        {
            Debug.LogWarning($"[UsageHandler] {itemData.itemName}은(는) 사용 효과가 없습니다.");
            return false;
        }

        // 4. 사용 전 검증 (체력 가득 참 등)
        if (!ValidateConsumableUse(itemData))
        {
            return false;
        }

        // 5. 효과 적용
        ApplyConsumableEffects(itemData);

        // 6. 인벤토리에서 아이템 제거
        if (removeFromInventory)
        {
            InventoryManager.Instance.RemoveItem(itemId, 1);
        }

        Debug.Log($"[UsageHandler] {itemData.itemName} 사용 완료");
        return true;
    }

    /// 소모품 사용 전 검증
    private static bool ValidateConsumableUse(ItemData itemData)
    {
        // 체력 회복 아이템인 경우 - 체력이 가득 찼는지 확인
        if (itemData.IsHealEffect())
        {
            if (PlayerStatsComponent.Instance != null)
            {
                if (PlayerStatsComponent.Instance.Stats.currentHP >= PlayerStatsComponent.Instance.Stats.maxHP)
                {
                    if (FloatingNotificationManager.Instance != null)
                    {
                        FloatingNotificationManager.Instance.ShowNotification("체력이 가득 차 있습니다.");
                    }
                    return false;
                }
            }
        }

        // 아이템 보상이 있는 경우 - 인벤토리 공간 확인
        var rewards = itemData.GetItemRewards();
        if (rewards != null && rewards.Count > 0)
        {
            int requiredSlots = CalculateRequiredSlots(rewards);
            int availableSlots = InventoryManager.Instance.GetAvailableSlots();

            if (availableSlots < requiredSlots)
            {
                if (PopupManager.Instance != null)
                {
                    PopupManager.Instance.ShowWarningPopup(
                        $"인벤토리가 가득차서 사용할수 없습니다.\n{requiredSlots}칸을 비우고 다시 사용해주세요");
                }
                return false;
            }
        }

        return true;
    }

    /// 소모품 효과 적용
    private static void ApplyConsumableEffects(ItemData itemData)
    {
        // 1. 체력 회복 효과
        if (itemData.IsHealEffect())
        {
            int healAmount = itemData.GetHealAmount();
            if (healAmount > 0 && PlayerStatsComponent.Instance != null)
            {
                PlayerStatsComponent.Instance.Stats.Heal(healAmount);
                Debug.Log($"[UsageHandler] {itemData.itemName} 사용 - HP {healAmount} 회복");
            }
        }
        // 2. 아이템 지급 효과
        else
        {
            // 아이템 보상
            var rewards = itemData.GetItemRewards();
            if (rewards != null && rewards.Count > 0)
            {
                Debug.Log($"[UsageHandler] {itemData.itemName} 사용 - 아이템 획득");

                foreach (var reward in rewards)
                {
                    if (InventoryManager.Instance != null)
                    {
                        bool added = InventoryManager.Instance.AddItem(reward.itemId, reward.quantity);

                        if (added)
                        {
                            string rewardName = reward.GetItemName();
                            if (FloatingNotificationManager.Instance != null)
                            {
                                FloatingNotificationManager.Instance.ShowNotification($"{rewardName} :{reward.quantity} 획득!");
                            }
                        }
                    }
                }
            }

            // 스킬 획득
            string skillId = itemData.GetSkill();
            if (!string.IsNullOrEmpty(skillId))
            {
                if (SkillManager.Instance != null)
                {
                    bool added = SkillManager.Instance.AddSkill(skillId);
                    if (added)
                    {
                        string skillName = SkillDataManager.Instance.GetSkillData(skillId)?.skillName ?? skillId;
                        if (FloatingNotificationManager.Instance != null)
                        {
                            FloatingNotificationManager.Instance.ShowNotification($"{skillName} 획득!");
                        }
                    }
                }
            }
        }
    }

    /// 보상 아이템을 받기 위해 필요한 인벤토리 슬롯 수 계산
    private static int CalculateRequiredSlots(List<ItemReward> rewards)
    {
        if (InventoryManager.Instance == null || rewards == null)
            return 0;

        // 단순히 보상 아이템 종류 수를 셈
        return rewards.Count;
    }

    // ==================== 스킬 사용 ====================

    /// 스킬 사용 (공통 로직)
    public static bool UseSkill(string skillId)
    {
        // 1. 스킬 매니저 확인
        if (SkillManager.Instance == null)
        {
            Debug.LogWarning("[UsageHandler] SkillManager가 없음");
            return false;
        }

        // 2. 스킬 보유 확인
        PlayerSkillData skillData = SkillManager.Instance.GetSkill(skillId);
        if (skillData == null)
        {
            Debug.LogWarning($"[UsageHandler] 보유하지 않은 스킬: {skillId}");
            return false;
        }

        // 3. 스킬 데이터 확인
        SkillData data = skillData.GetSkillData();
        if (data == null)
        {
            Debug.LogWarning($"[UsageHandler] 스킬 데이터를 찾을 수 없음: {skillId}");
            return false;
        }

        // 4. 사용 가능 여부 확인
        if (!skillData.canUse)
        {
            Debug.LogWarning($"[UsageHandler] {data.skillName}은(는) 현재 사용할 수 없습니다.");
            return false;
        }

        // 5. TODO: 쿨타임 체크
        // if (IsSkillOnCooldown(skillId)) return false;

        // 6. TODO: 마나/리소스 체크
        // if (!HasEnoughResource(data)) return false;

        // 7. 스킬 효과 발동
        ApplySkillEffects(data);

        // 8. TODO: 쿨타임 시작
        // StartSkillCooldown(skillId, data.cooldown);

        Debug.Log($"[UsageHandler] {data.skillName} 스킬 사용");
        return true;
    }

    /// <summary>
    /// 스킬 효과 적용
    /// </summary>
    private static void ApplySkillEffects(SkillData data)
    {
        // TODO: 스킬 효과 구현
        // 현재는 로그만 출력
        Debug.Log($"[UsageHandler] 스킬 '{data.skillName}' 효과 발동 (미구현)");

        // 예시:
        // - 데미지 스킬: 타겟에게 데미지
        // - 버프 스킬: 플레이어 스탯 증가
        // - 힐 스킬: 체력 회복
        // - 투사체 스킬: 발사체 생성
    }

    // ==================== UI 갱신 ====================

    /// <summary>
    /// 아이템/스킬 사용 후 모든 관련 UI 갱신
    /// </summary>
    public static void RefreshAllRelatedUIs()
    {
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

        if (SkillUIManager.Instance != null)
        {
            SkillUIManager.Instance.RefreshUI();
        }
    }
}