using System;
using System.Collections.Generic;
using UnityEngine;


/// 캐릭터 슬롯 정보 (기존 CharacterStats 재사용)

[System.Serializable]
public class CharacterSlotData
{
    public string characterid;           // 고유 id (GUid)
    public int slotIndex;                // 슬롯 번호 (0, 1, 2)

    // 기존 CharacterStatsData 재사용
    public CharacterStatsData stats;

    // 위치 정보
    public string currentScene;
    public Vector3 position;

    // 인벤토리 정보
    public InventorySaveData inventoryData;

    // 커스터마이징 (추후 확장)
    public int hairStyle;
    public int hairColor;
    public int skinColor;
    public int eyeColor;

    // 생성 시간
    public string createdDate;
    public string lastPlayedDate;

    // 플레이 시간
    public float totalPlayTime;

    // 퀵슬롯 정보
    public List<QuickSlotSaveData> quickSlots = new List<QuickSlotSaveData>();

    //  장비 슬롯 정보 추가
    public EquipmentSaveData equipmentData = new EquipmentSaveData();

    //  상점 재고 정보 추가 (캐릭터별로 따로 관리)
    public ShopStockSaveData shopStockData = new ShopStockSaveData();

    
    /// 새 캐릭터 생성
    
    public static CharacterSlotData CreateNew(string name, int slot)
    {
        // 기존 CharacterStats 사용
        CharacterStats newStats = new CharacterStats();
        newStats.Initialize(name, 1);

        return new CharacterSlotData
        {
            characterid = Guid.NewGuid().ToString(),
            slotIndex = slot,

            // 기존 ToSaveData() 메서드 사용
            stats = newStats.ToSaveData(),

            currentScene = Definitions.Def_Name.SCENE_NAME_DEFAULT_MAP,
            position = Vector3.zero,

            // 빈 인벤토리
            inventoryData = new InventorySaveData(),

            //  빈 장비 데이터 초기화
            equipmentData = new EquipmentSaveData(),

            //  빈 상점 재고 데이터 초기화
            shopStockData = new ShopStockSaveData(),

            hairStyle = 0,
            hairColor = 0,
            skinColor = 0,
            eyeColor = 0,

            createdDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            lastPlayedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),

            totalPlayTime = 0f
        };
    }

    
    /// 마지막 플레이 시간 업데이트
    
    public void UpdateLastPlayed()
    {
        lastPlayedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    
    /// 플레이 시간 추가
    
    public void AddPlayTime(float seconds)
    {
        totalPlayTime += seconds;
    }

    
    /// 플레이 시간을 "XX시간 YY분" 형식으로 반환
    
    public string GetFormattedPlayTime()
    {
        int hours = Mathf.FloorToInt(totalPlayTime / 3600f);
        int minutes = Mathf.FloorToInt((totalPlayTime % 3600f) / 60f);
        return $"{hours}시간 {minutes}분";
    }

    
    /// CharacterStats 객체로 변환
    
    public CharacterStats ToCharacterStats()
    {
        CharacterStats stats = new CharacterStats();
        stats.LoadFromData(this.stats);
        return stats;
    }
}