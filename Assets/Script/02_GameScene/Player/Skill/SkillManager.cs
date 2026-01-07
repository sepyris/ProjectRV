using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 스킬 시스템 관리자
/// 패시브 스킬 자동 적용/해제 포함
/// </summary>
public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    private List<PlayerSkillData> skills = new List<PlayerSkillData>();
    private Dictionary<string, SkillBase> skillInstances = new Dictionary<string, SkillBase>();

    private bool isInSkillDelay = false;
    private float skillDelayRemaining = 0f;
    private const float GLOBAL_SKILL_DELAY = 0.3f;

    public bool IsInSkillDelay => isInSkillDelay;

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

    private void Start()
    {
        // 씬 로드 시 패시브 스킬 재적용
        ReapplyAllPassiveSkills();
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
    /// 스킬 인스턴스 가져오기
    /// </summary>
    public SkillBase GetSkillInstance(string skillID)
    {
        if (skillInstances.TryGetValue(skillID, out SkillBase instance))
        {
            return instance;
        }
        return null;
    }

    /// <summary>
    /// 스킬 추가 (패시브 자동 적용)
    /// </summary>
    public bool AddSkill(string skillID)
    {
        // 이미 있는 스킬의 경우 추가 안 함
        if (HasSkill(skillID))
        {
            Debug.Log($"[SkillManager] 이미 보유 중인 스킬: {skillID}");
            return false;
        }

        // 스킬 데이터 검증
        if (SkillDataManager.Instance == null)
        {
            Debug.LogError($"[SkillManager] SkillDataManager가 없습니다.");
            return false;
        }

        SkillData skillData = SkillDataManager.Instance.GetSkillData(skillID);
        if (skillData == null)
        {
            Debug.LogWarning($"[SkillManager] 스킬을 찾을 수 없음: {skillID}");
            return false;
        }

        // 스킬 데이터 추가
        PlayerSkillData newSkill = new PlayerSkillData();
        newSkill.skillid = skillID;
        newSkill.canUse = true;
        skills.Add(newSkill);

        // 스킬 인스턴스 생성
        SkillBase skillInstance = SkillFactory.CreateSkill(skillData);
        if (skillInstance != null)
        {
            skillInstances[skillID] = skillInstance;

            // 패시브 스킬이면 즉시 적용
            if (skillInstance is PassiveSkillBase passiveSkill)
            {
                if (PlayerStatsComponent.Instance != null)
                {
                    passiveSkill.ApplyEffect(PlayerStatsComponent.Instance.Stats);
                    Debug.Log($"[SkillManager] 패시브 스킬 적용: {skillID}");
                }
                else
                {
                    Debug.LogWarning($"[SkillManager] PlayerStatsComponent가 없어 패시브 적용 실패: {skillID}");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[SkillManager] 스킬 인스턴스 생성 실패: {skillID}");
        }

        OnSkillAdded?.Invoke(newSkill);
        OnSkillChanged?.Invoke();

        Debug.Log($"[SkillManager] 스킬 추가: {skillID} ({skillData.skillType}), 총 스킬 수: {skills.Count}");
        return true;
    }

    /// <summary>
    /// 스킬 제거 (패시브 자동 해제)
    /// </summary>
    public bool RemoveSkill(string skillID)
    {
        // 패시브 효과 제거
        if (skillInstances.TryGetValue(skillID, out SkillBase skillInstance))
        {
            if (skillInstance is PassiveSkillBase passiveSkill)
            {
                if (PlayerStatsComponent.Instance != null)
                {
                    passiveSkill.RemoveEffect(PlayerStatsComponent.Instance.Stats);
                    Debug.Log($"[SkillManager] 패시브 스킬 제거: {skillID}");
                }
            }
            skillInstances.Remove(skillID);
        }

        // 스킬 데이터 제거
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
    /// 스킬 사용
    /// </summary>
    public bool UseSkill(string skillID, Transform caster, Vector3 targetPosition, Transform targetTransform = null)
    {
        if (isInSkillDelay)
        {
            if (FloatingNotificationManager.Instance != null)
            {
                FloatingNotificationManager.Instance.ShowNotification("스킬 사용 대기 중!");
            }
            return false;
        }

        if (!skillInstances.TryGetValue(skillID, out SkillBase skillInstance))
        {
            Debug.LogWarning($"[SkillManager] 스킬 인스턴스를 찾을 수 없음: {skillID}");
            return false;
        }

        bool success = skillInstance.Use(caster, targetPosition, targetTransform);

        if (success)
        {
            isInSkillDelay = true;
            skillDelayRemaining = GLOBAL_SKILL_DELAY;

            PlayerSkillData skillData = GetSkill(skillID);
            OnSkillUse?.Invoke(skillData);
        }

        return success;
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
    /// 모든 패시브 스킬 재적용 (씬 전환 시)
    /// </summary>
    public void ReapplyAllPassiveSkills()
    {
        if (PlayerStatsComponent.Instance == null)
        {
            Debug.LogWarning("[SkillManager] PlayerStatsComponent가 없어 패시브 재적용 불가");
            return;
        }

        int reappliedCount = 0;
        foreach (var kvp in skillInstances)
        {
            if (kvp.Value is PassiveSkillBase passiveSkill && !passiveSkill.IsApplied)
            {
                passiveSkill.ApplyEffect(PlayerStatsComponent.Instance.Stats);
                reappliedCount++;
                Debug.Log($"[SkillManager] 패시브 재적용: {kvp.Key}");
            }
        }

        if (reappliedCount > 0)
        {
            Debug.Log($"[SkillManager] 총 {reappliedCount}개 패시브 스킬 재적용 완료");
        }
    }

    /// <summary>
    /// 스킬 데이터 저장
    /// </summary>
    public SkillSaveData ToSaveData()
    {
        return new SkillSaveData
        {
            skills = skills.Select(s => s.ToSaveData()).ToList()
        };
    }

    /// <summary>
    /// 스킬 데이터 로드
    /// </summary>
    public void LoadFromData(SkillSaveData data)
    {
        // 기존 패시브 효과 제거
        foreach (var kvp in skillInstances)
        {
            if (kvp.Value is PassiveSkillBase passiveSkill)
            {
                if (PlayerStatsComponent.Instance != null)
                {
                    passiveSkill.RemoveEffect(PlayerStatsComponent.Instance.Stats);
                }
            }
        }

        skills.Clear();
        skillInstances.Clear();

        if (data != null && data.skills != null)
        {
            foreach (var skillData in data.skills)
            {
                // AddSkill을 사용하여 자동으로 패시브 적용
                AddSkill(skillData.skillId);
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
        // 모든 패시브 효과 제거
        foreach (var kvp in skillInstances)
        {
            if (kvp.Value is PassiveSkillBase passiveSkill)
            {
                if (PlayerStatsComponent.Instance != null)
                {
                    passiveSkill.RemoveEffect(PlayerStatsComponent.Instance.Stats);
                }
            }
        }

        Debug.Log("[SkillManager] 모든 스킬 초기화");
        skills.Clear();
        skillInstances.Clear();
        OnSkillChanged?.Invoke();
    }

    /// <summary>
    /// 쿨타임 업데이트
    /// </summary>
    private void Update()
    {
        float deltaTime = Time.deltaTime;

        // 쿨타임 업데이트
        foreach (var kvp in skillInstances)
        {
            kvp.Value.UpdateCooldown(deltaTime);
        }

        // 후딜레이 업데이트 추가
        if (isInSkillDelay)
        {
            skillDelayRemaining -= deltaTime;
            if (skillDelayRemaining <= 0f)
            {
                isInSkillDelay = false;
                skillDelayRemaining = 0f;
            }
        }
    }
}

/// <summary>
/// 스킬 저장 데이터
/// </summary>
[Serializable]
public class SkillSaveData
{
    public List<PlayerSkillSaveData> skills = new List<PlayerSkillSaveData>();
}