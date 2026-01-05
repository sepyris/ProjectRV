using Definitions;
using System;
using System.Collections.Generic;
using UnityEngine;

/// 아이템 데이터를 저장하는 ScriptableObject
public class ItemDataSO : ScriptableObject
{
    public List<ItemData> Items = new List<ItemData>();
}


/// 아이템 타입

public enum ItemType
{
    Equipment,   // 장비
    Consumable,  // 소비 아이템
    Material,    // 재료
    QuestItem    // 퀘스트 아이템
}


/// 장비 슬롯 타입 (확장됨)
///  Weapon이 MeleeWeapon과 RangedWeapon으로 분리됨

public enum EquipmentSlot
{
    None,
    Helmet,        // 모자
    Armor,         // 옷
    Shoes,         // 신발
    MeleeWeapon,   //  근거리 무기
    RangedWeapon,  //  원거리 무기
    SubWeapon,     // 보조무기
    Ring,          // 반지
    Necklace,      // 목걸이
    Bracelet       // 팔찌
}


/// 치장 슬롯 타입

public enum CosmeticSlot
{
    None,
    Helmet,      // 모자
    Armor,       // 옷
    Shoes,       // 신발
    Weapon,      // 무기 (치장은 근거리/원거리 구분 안 함)
    Hair,        // 헤어 (반지 슬롯)
    FaceAccessory, // 얼굴장식 (목걸이 슬롯)
    Cape         // 망토 (팔찌 슬롯)
}


/// 아이템 데이터 (순수 데이터 클래스)

[Serializable]
public class ItemData
{
    // 공통 데이터
    public string itemId;           // 아이템 고유 id
    public string itemName;         // 아이템 이름
    public ItemType itemType;       // 아이템 타입
    public string description;      // 설명
    public int maxStack;            // 최대 스택 수 (1 = 스택 불가)
    public int buyPrice;            // 구매 가격
    public int sellPrice;           // 판매 가격
    public string iconPath;         // 아이콘 경로
    public bool disposable;         // 드롭 가능 여부

    public string consumableEffect; //  소비 효과 (체력 회복 또는 아이템)

    // 장비 전용 데이터
    public EquipmentSlot equipSlot; // 장비 슬롯
    public int attackBonus;         // 공격력 보너스
    public int defenseBonus;        // 방어력 보너스
    public int hpBonus;             // 체력 보너스
    public int strBonus;            // 힘 보너스
    public int dexBonus;            // 민첩 보너스
    public int intBonus;            // 지능 보너스
    public int lukBonus;            // 행운 보너스
    public int tecBonus;            // 기술 보너스

    // 치장 아이템 여부
    public bool isCosmetic;         // 치장 아이템인지 여부

    
    /// 소비 효과가 체력 회복인지 확인
    
    public bool IsHealEffect()
    {
        if (string.IsNullOrEmpty(consumableEffect))
            return false;

        // 숫자면 체력 회복
        return int.TryParse(consumableEffect, out _);
    }

    
    /// 체력 회복량 가져오기
    
    public int GetHealAmount()
    {
        if (IsHealEffect() && int.TryParse(consumableEffect, out int amount))
            return amount;
        return 0;
    }

    
    /// 아이템 보상 리스트 가져오기
    
    public List<ItemReward> GetItemRewards()
    {
        if (IsHealEffect() || string.IsNullOrEmpty(consumableEffect))
            return null;

        // ItemReward 형식으로 파싱
        List<ItemReward> rewards = new List<ItemReward>();
        var items = consumableEffect.Split(';');
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.Trim()))
            {
                if(ItemDataManager.Instance.GetItemData(item) != null)
                {
                    rewards.Add(new ItemReward(item));
                }
                else
                {
                    return null;
                }
            }
        }
        return rewards;
    }
    public string GetSkill()
    {
        if (IsHealEffect() || string.IsNullOrEmpty(consumableEffect))
            return null;
        if(SkillDataManager.Instance != null)
        {
            SkillData skill = SkillDataManager.Instance.GetSkillData(consumableEffect);
            if (skill != null)
            {
                return consumableEffect;
            }
        }
        return null;
    }

    public CosmeticSlot ConvertToCosmeticSlot(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Helmet: return CosmeticSlot.Helmet;
            case EquipmentSlot.Armor: return CosmeticSlot.Armor;
            case EquipmentSlot.Shoes: return CosmeticSlot.Shoes;
            case EquipmentSlot.MeleeWeapon: return CosmeticSlot.Weapon;
            case EquipmentSlot.Ring: return CosmeticSlot.Hair;
            case EquipmentSlot.Necklace: return CosmeticSlot.FaceAccessory;
            case EquipmentSlot.Bracelet: return CosmeticSlot.Cape;
        }
        return CosmeticSlot.None;
    }
}