using System;
using UnityEngine;

/// <summary>
/// 플레이어가 실제로 소유한 스킬
/// </summary>
[Serializable]
public class PlayerSkillData
{
    public string skillid;
    public bool canUse;
    public int skillLevel = 1;              // 스킬 레벨 (기본 1)
    public int currentExp = 0;              // 현재 경험치

    [NonSerialized]
    private SkillData cachedData;

    public string SkillName
    {
        get
        {
            SkillData data = GetSkillData();
            return data != null ? data.skillName : skillid;
        }
    }

    /// <summary>
    /// 최대 레벨 도달 여부
    /// </summary>
    public bool IsMaxLevel
    {
        get
        {
            SkillData data = GetSkillData();
            return data != null && skillLevel >= data.maxLevel;
        }
    }

    /// <summary>
    /// 다음 레벨까지 필요한 경험치
    /// </summary>
    public int GetRequiredExpForNextLevel()
    {
        // 레벨당 필요 경험치
        return 50 + (skillLevel * 70);
    }

    /// <summary>
    /// 경험치 획득
    /// </summary>
    public bool AddExp(int exp)
    {
        if (IsMaxLevel)
        {
            Debug.Log($"[PlayerSkillData] {SkillName}은(는) 이미 최대 레벨입니다.");
            return false;
        }

        currentExp += exp;

        // 레벨업 체크
        bool leveledUp = false;
        while (currentExp >= GetRequiredExpForNextLevel() && !IsMaxLevel)
        {
            currentExp -= GetRequiredExpForNextLevel();
            skillLevel++;
            leveledUp = true;

            Debug.Log($"[PlayerSkillData] {SkillName} 레벨업! Lv.{skillLevel}");
        }

        // 최대 레벨 도달 시 경험치 초기화
        if (IsMaxLevel)
        {
            currentExp = 0;
        }

        return leveledUp;
    }

    /// <summary>
    /// 경험치 진행도 (0~1)
    /// </summary>
    public float GetExpProgress()
    {
        if (IsMaxLevel)
            return 1f;

        int required = GetRequiredExpForNextLevel();
        if (required <= 0)
            return 1f;

        return (float)currentExp / required;
    }

    /// <summary>
    /// 현재 레벨의 데미지
    /// </summary>
    public float GetCurrentDamage()
    {
        SkillData data = GetSkillData();
        if (data == null)
            return 0f;

        return data.GetDamageAtLevel(skillLevel);
    }

    public SkillData GetSkillData()
    {
        if (cachedData == null)
        {
            if (SkillDataManager.Instance != null)
            {
                cachedData = SkillDataManager.Instance.GetSkillData(skillid);
            }
        }
        return cachedData;
    }

    /// <summary>
    /// 저장용 데이터로 변환
    /// </summary>
    public PlayerSkillSaveData ToSaveData()
    {
        return new PlayerSkillSaveData
        {
            skillId = this.skillid,
            canUse = this.canUse,
            skillLevel = this.skillLevel,
            currentExp = this.currentExp
        };
    }

    /// <summary>
    /// 저장 데이터에서 복원
    /// </summary>
    public static PlayerSkillData LoadData(PlayerSkillSaveData data)
    {
        return new PlayerSkillData
        {
            skillid = data.skillId,
            canUse = data.canUse,
            skillLevel = data.skillLevel,
            currentExp = data.currentExp
        };
    }
}

/// <summary>
/// 플레이어 스킬 저장 데이터
/// </summary>
[Serializable]
public class PlayerSkillSaveData
{
    public string skillId;
    public bool canUse;
    public int skillLevel = 1;
    public int currentExp = 0;
}