using Definitions;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemDataSO : ScriptableObject
{
    public List<ItemData> Items = new List<ItemData>();
}

/// <summary>
/// 아이템 타입
/// </summary>
public enum ItemType
{
    Equipment,   // 장비
    Consumable,  // 소비 아이템
    Material,    // 재료
    QuestItem    // 퀘스트 아이템
}

/// <summary>
/// 장비 슬롯 타입 (확장됨)
///  Weapon이 MeleeWeapon과 RangedWeapon으로 분리됨
/// </summary>
public enum EquipmentSlot
{
    None,
    Helmet,        // 모자
    Armor,         // 옷
    Shoes,         // 신발
    MeleeWeapon,   //  근거리 무기 (기존 Weapon 대체)
    RangedWeapon,  //  원거리 무기 (새로 추가)
    SubWeapon,     // 보조무기
    Ring,          // 반지
    Necklace,      // 목걸이
    Bracelet       // 팔찌
}

/// <summary>
/// 치장 슬롯 타입
/// </summary>
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

/// <summary>
/// 아이템 데이터 (순수 데이터 클래스)
/// </summary>
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

    /// <summary>
    /// 소비 효과가 체력 회복인지 확인
    /// </summary>
    public bool IsHealEffect()
    {
        if (string.IsNullOrEmpty(consumableEffect))
            return false;

        // 숫자면 체력 회복
        return int.TryParse(consumableEffect, out _);
    }

    /// <summary>
    /// 체력 회복량 가져오기
    /// </summary>
    public int GetHealAmount()
    {
        if (IsHealEffect() && int.TryParse(consumableEffect, out int amount))
            return amount;
        return 0;
    }

    /// <summary>
    /// 아이템 보상 리스트 가져오기
    /// </summary>
    public List<GameData.Common.ItemReward> GetItemRewards()
    {
        if (IsHealEffect() || string.IsNullOrEmpty(consumableEffect))
            return null;

        // ItemReward 형식으로 파싱
        List<GameData.Common.ItemReward> rewards = new List<GameData.Common.ItemReward>();
        var items = consumableEffect.Split(';');
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.Trim()))
            {
                rewards.Add(new GameData.Common.ItemReward(item));
            }
        }
        return rewards;
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