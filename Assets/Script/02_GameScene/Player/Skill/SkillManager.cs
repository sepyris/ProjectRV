using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 스킬 시스템 관리자
/// InventoryManager 패턴을 정확히 따름
/// </summary>
public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    private List<PlayerSkillData> skills = new List<PlayerSkillData>();

    // 이벤트
    public event Action<PlayerSkillData> OnSkillAdded;
    public event Action<PlayerSkillData> OnSkillRemoved;
    public event Action<PlayerSkillData> OnSkillUse;
    public Action OnSkillChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 모든 스킬 목록 가져오기
    /// </summary>
    public List<PlayerSkillData> GetSkillsByType()
    {
        return skills.ToList();
    }

    /// <summary>
    /// 특정 스킬 ID로 스킬 데이터 가져오기
    /// </summary>
    public PlayerSkillData GetSkill(string skillID)
    {
        return skills.FirstOrDefault(s => s.skillid == skillID);
    }

    /// <summary>
    /// 스킬 추가
    /// </summary>
    public bool AddSkill(string skillID)
    {
        // 이미 있는 스킬의 경우 사용 취소
        foreach (var s in skills)
        {
            if (s.skillid == skillID)
            {
                Debug.Log($"[SkillManager] 이미 보유 중인 스킬: {skillID}");
                return false;
            }
        }

        // 스킬 데이터 검증
        if (SkillDataManager.Instance != null)
        {
            SkillData skillData = SkillDataManager.Instance.GetSkillData(skillID);
            if (skillData == null)
            {
                Debug.LogWarning($"[SkillManager] 스킬을 찾을 수 없음: {skillID}");
                return false;
            }
        }

        // 그렇지 않으면 스킬 추가
        PlayerSkillData newSkill = new PlayerSkillData();
        newSkill.skillid = skillID;
        newSkill.canUse = true; // 기본값: 사용 가능
        skills.Add(newSkill);

        OnSkillAdded?.Invoke(newSkill);
        OnSkillChanged?.Invoke();

        Debug.Log($"[SkillManager] 스킬 추가: {skillID}, 총 스킬 수: {skills.Count}");
        return true;
    }

    /// <summary>
    /// 스킬 제거
    /// </summary>
    public bool RemoveSkill(string skillID)
    {
        PlayerSkillData skill = skills.FirstOrDefault(s => s.skillid == skillID);
        if (skill != null)
        {
            skills.Remove(skill);
            OnSkillRemoved?.Invoke(skill);
            OnSkillChanged?.Invoke();
            Debug.Log($"[SkillManager] 스킬 제거: {skillID}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 스킬 사용 가능 여부 설정
    /// </summary>
    public void SetSkillUsable(string skillID, bool canUse)
    {
        PlayerSkillData skill = GetSkill(skillID);
        if (skill != null)
        {
            skill.canUse = canUse;
            OnSkillChanged?.Invoke();
            Debug.Log($"[SkillManager] 스킬 사용 가능 여부 변경: {skillID} -> {canUse}");
        }
    }

    /// <summary>
    /// 스킬이 존재하는지 확인
    /// </summary>
    public bool HasSkill(string skillID)
    {
        return skills.Any(s => s.skillid == skillID);
    }

    /// <summary>
    /// 스킬 데이터 저장 (InventoryManager.ToSaveData() 패턴)
    /// </summary>
    public SkillSaveData ToSaveData()
    {
        return new SkillSaveData
        {
            skills = skills.Select(s => s.ToSaveData()).ToList()
        };
    }

    /// <summary>
    /// 스킬 데이터 로드 (InventoryManager.LoadFromData() 패턴)
    /// </summary>
    public void LoadFromData(SkillSaveData data)
    {
        skills.Clear();

        if (data != null && data.skills != null)
        {
            foreach (var skillData in data.skills)
            {
                skills.Add(PlayerSkillData.FromSaveData(skillData));
            }
        }

        OnSkillChanged?.Invoke();

        Debug.Log($"[SkillManager] 데이터 로드 완료 ({skills.Count}개 스킬)");
    }

    /// <summary>
    /// 모든 스킬 초기화 (새 캐릭터 생성 시)
    /// </summary>
    public void ClearAllSkills()
    {
        Debug.Log("[SkillManager] 모든 스킬 초기화");
        skills.Clear();
        OnSkillChanged?.Invoke();
    }
}

/// <summary>
/// 스킬 저장 데이터 (InventorySaveData 패턴)
/// </summary>
[Serializable]
public class SkillSaveData
{
    public List<PlayerSkillSaveData> skills = new List<PlayerSkillSaveData>();
}