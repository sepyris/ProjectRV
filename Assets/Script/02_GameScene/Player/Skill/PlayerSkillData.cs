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
            canUse = this.canUse
        };
    }

    /// <summary>
    /// 저장 데이터에서 복원
    /// </summary>
    public static PlayerSkillData FromSaveData(PlayerSkillSaveData data)
    {
        return new PlayerSkillData
        {
            skillid = data.skillId,
            canUse = data.canUse
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
}