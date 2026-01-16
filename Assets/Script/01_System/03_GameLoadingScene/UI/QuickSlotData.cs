using System;
using UnityEngine;


/// 퀵슬롯 아이템 타입

public enum QuickSlotType
{
    Empty,      // 빈 슬롯
    Consumable, // 소모품
    Skill       // 스킬 (추후 확장용)
}


/// 퀵슬롯에 저장되는 데이터

[Serializable]
public class QuickSlotData
{
    public QuickSlotType slotType;
    public string itemId;       // 아이템 ID (소모품의 경우)
    public string skillId;      // 스킬 ID (스킬의 경우)
    public int slotIndex;       // 슬롯 번호 (0~9)

    public QuickSlotData(int index)
    {
        slotIndex = index;
        slotType = QuickSlotType.Empty;
        itemId = null;
        skillId = null;
    }

    
    /// 소모품 아이템 설정
    
    public void SetConsumable(string id)
    {
        slotType = QuickSlotType.Consumable;
        itemId = id;
        skillId = null;
    }

    
    /// 스킬 설정
    
    public void SetSkill(string id)
    {
        slotType = QuickSlotType.Skill;
        skillId = id;
        itemId = null;
    }

    
    /// 슬롯 비우기
    
    public void Clear()
    {
        slotType = QuickSlotType.Empty;
        itemId = null;
        skillId = null;
    }

    
    /// 빈 슬롯인지 확인
    
    public bool IsEmpty()
    {
        return slotType == QuickSlotType.Empty;
    }

    
    /// 저장 데이터로 변환
    
    public QuickSlotSaveData ToSaveData()
    {
        return new QuickSlotSaveData
        {
            slotType = this.slotType,
            itemId = this.itemId,
            skillId = this.skillId,
            slotIndex = this.slotIndex
        };
    }

    
    /// 저장 데이터에서 복원
    
    public static QuickSlotData FromSaveData(QuickSlotSaveData data)
    {
        QuickSlotData slot = new QuickSlotData(data.slotIndex);
        slot.slotType = data.slotType;
        slot.itemId = data.itemId;
        slot.skillId = data.skillId;
        return slot;
    }
}


/// 퀵슬롯 저장 데이터

[Serializable]
public class QuickSlotSaveData
{
    public QuickSlotType slotType;
    public string itemId;
    public string skillId;
    public int slotIndex;
}