using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class UsageHandler
{
    // ==================== 아이템 사용 ====================
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

        // 4. 사용 전 검증 (체력 가득 찬 등)
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

    private static bool ValidateConsumableUse(ItemData itemData)
    {
        // 체력 회복 아이템인 경우 - 체력이 가득 찬지 확인
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

        // 스킬 스크롤인 경우 - 직업 조건 확인
        string skillId = itemData.GetSkill();
        if (!string.IsNullOrEmpty(skillId))
        {
            if (SkillDataManager.Instance != null && PlayerStatsComponent.Instance != null)
            {
                SkillData skillData = SkillDataManager.Instance.GetSkillData(skillId);
                if (skillData != null)
                {
                    // 이미 보유한 스킬인지 확인
                    if (SkillManager.Instance != null && SkillManager.Instance.HasSkill(skillId))
                    {
                        if (PopupManager.Instance != null)
                        {
                            PopupManager.Instance.ShowWarningPopup(
                                "이미 보유한 스킬 입니다."
                                );
                        }
                        return false;
                    }

                    // 직업 조건 확인
                    if (skillData.requiredJob != JobsType.None)
                    {
                        CharacterStats stats = PlayerStatsComponent.Instance.Stats;
                        if (!stats.CanUseSkill(skillData.requiredJob))
                        {
                            // 직업 조건 미충족
                            string jobName = GetJobName(skillData.requiredJob);
                            if (PopupManager.Instance != null)
                            {
                                PopupManager.Instance.ShowWarningPopup(
                                    $"{jobName} 스킬 입니다.\n{stats.GetCurrentJob().GetJobName()} 은(는) 배울수 없습니다."
                                    );
                            }

                            Debug.Log($"[UsageHandler] 직업 조건 미충족 - 필요: {skillData.requiredJob}, 보유 직업: {string.Join(", ", stats.GetAllJobs().Select(j => j.jobType))}");
                            return false;
                        }
                    }
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
                        InventoryManager.Instance.AddItem(reward.itemId, reward.quantity);
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

    private static int CalculateRequiredSlots(List<ItemReward> rewards)
    {
        if (InventoryManager.Instance == null || rewards == null)
            return 0;

        return rewards.Count;
    }

    private static string GetJobName(JobsType jobType)
    {
        if (JobsDataManager.Instance != null)
        {
            JobsData jobData = JobsDataManager.Instance.GetJobDataByType(jobType);
            if (jobData != null)
            {
                return jobData.jobName;
            }
        }

        return jobType.ToString();
    }

    // ==================== 스킬 사용 ====================

    public static bool UseSkill(string skillId)
    {
        // 1. SkillManager 확인
        if (SkillManager.Instance == null)
        {
            Debug.LogWarning("[UsageHandler] SkillManager가 없음");
            return false;
        }

        // 2. PlayerController 확인
        Transform playerTransform = PlayerController.Instance?.transform;
        if (playerTransform == null)
        {
            Debug.LogWarning("[UsageHandler] PlayerController가 없음");
            return false;
        }

        // 3. 타겟 위치 계산
        Vector3 targetPosition = playerTransform.position + playerTransform.forward * 5f;

        bool used = SkillManager.Instance.UseSkill(skillId, playerTransform, targetPosition);

        if (used)
        {
            Debug.Log($"[UsageHandler] 스킬 사용 성공");
        }

        return used;
    }

    // ==================== UI 갱신 ====================

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