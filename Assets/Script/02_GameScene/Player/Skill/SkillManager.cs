using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    private List<PlayerSkillData> skills = new List<PlayerSkillData>();
    private Dictionary<string, SkillBase> skillInstances = new Dictionary<string, SkillBase>();


    private float skillDelayRemaining = 0f;
    private const float GLOBAL_SKILL_DELAY = 0.3f;

    private bool isInSkillDelay = false;
    public bool IsInSkillDelay => isInSkillDelay;

    private bool isUsingSkill = false;
    public bool IsUsingSkill => isUsingSkill;

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
        ReapplyAllPassiveSkills();
    }

    public List<PlayerSkillData> GetSkillsByType()
    {
        return skills.ToList();
    }

    public PlayerSkillData GetSkill(string skillID)
    {
        return skills.FirstOrDefault(s => s.skillid == skillID);
    }

    public SkillBase GetSkillInstance(string skillID)
    {
        if (skillInstances.TryGetValue(skillID, out SkillBase instance))
        {
            return instance;
        }
        return null;
    }

    public bool AddSkill(string skillID)
    {
        if (HasSkill(skillID))
        {
            Debug.Log($"[SkillManager] 이미 보유 중인 스킬: {skillID}");
            return false;
        }

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

        PlayerSkillData newSkill = new PlayerSkillData();
        newSkill.skillid = skillID;
        newSkill.canUse = true;
        skills.Add(newSkill);

        SkillBase skillInstance = SkillFactory.CreateSkill(skillData);
        if (skillInstance != null)
        {
            skillInstances[skillID] = skillInstance;

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

    public bool RemoveSkill(string skillID)
    {
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

    public bool UseSkill(string skillID, Transform caster, Vector3 targetPosition, Transform targetTransform = null)
    {
        if (isInSkillDelay)
        {
            return false;
        }

        PlayerSkillData skillData = GetSkill(skillID);
        if (skillData == null)
        {
            Debug.LogWarning($"[SkillManager] 보유하지 않은 스킬: {skillID}");
            return false;
        }

        if (!skillData.canUse)
        {
            Debug.LogWarning($"[SkillManager] 스킬을 사용할 수 없음: {skillID}");
            return false;
        }

        SkillData data = skillData.GetSkillData();
        if (data != null && data.requiredJob != JobsType.None)
        {
            if (PlayerStatsComponent.Instance != null)
            {
                if (!PlayerStatsComponent.Instance.Stats.CanUseSkill(data.requiredJob))
                {
                    Debug.LogWarning($"[SkillManager] 직업 조건 미충족: {skillID} (필요 직업: {data.requiredJob})");
                    return false;
                }
            }
        }

        if (!skillInstances.TryGetValue(skillID, out SkillBase skillInstance))
        {
            Debug.LogWarning($"[SkillManager] 스킬 인스턴스를 찾을 수 없음: {skillID}");
            return false;
        }

        if (SkillCooldownManager.Instance != null &&
            SkillCooldownManager.Instance.IsOnCooldown(skillID))
        {
            float remaining = SkillCooldownManager.Instance.GetRemainingCooldown(skillID);

            Debug.Log($"[SkillManager] {data?.skillName ?? skillID} 쿨타임 중");
            return false;
        }

        isUsingSkill = true;
        Debug.Log($"[SkillManager] 스킬 사용 시작 - 이동 멈춤");

        bool success = skillInstance.Use(caster, targetPosition, targetTransform);

        if (success)
        {
            isInSkillDelay = true;
            skillDelayRemaining = GLOBAL_SKILL_DELAY;

            OnSkillUse?.Invoke(skillData);
            skillData.AddExp(5 * skillData.skillLevel);

            Debug.Log($"[SkillManager]  스킬 사용 성공: {skillInstance.SkillName}");

            StartCoroutine(ResetSkillUse(0.3f));
        }
        else
        {
            isUsingSkill = false;
            Debug.Log($"[SkillManager] 스킬 사용 실패");
        }

        return success;
    }

    private IEnumerator ResetSkillUse(float duration)
    {
        yield return new WaitForSeconds(duration);
        isUsingSkill = false;
    }

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

    public bool HasSkill(string skillID)
    {
        return skills.Any(s => s.skillid == skillID);
    }

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

    public SkillSaveData ToSaveData()
    {
        return new SkillSaveData
        {
            skills = skills.Select(s => s.ToSaveData()).ToList()
        };
    }

    public void LoadFromData(SkillSaveData data)
    {
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
            foreach (var savedSkill in data.skills)
            {
                PlayerSkillData playerSkill = PlayerSkillData.LoadData(savedSkill);
                skills.Add(playerSkill);

                SkillData skillData = SkillDataManager.Instance?.GetSkillData(savedSkill.skillId);
                if (skillData == null)
                {
                    Debug.LogWarning($"[SkillManager] 스킬 데이터를 찾을 수 없음: {savedSkill.skillId}");
                    continue;
                }

                SkillBase skillInstance = SkillFactory.CreateSkill(skillData, savedSkill.skillLevel);
                if (skillInstance != null)
                {
                    skillInstances[savedSkill.skillId] = skillInstance;

                    if (skillInstance is PassiveSkillBase passiveSkill)
                    {
                        if (PlayerStatsComponent.Instance != null)
                        {
                            passiveSkill.ApplyEffect(PlayerStatsComponent.Instance.Stats);
                        }
                    }
                }
            }
        }

        OnSkillChanged?.Invoke();
        Debug.Log($"[SkillManager] 데이터 로드 완료 ({skills.Count}개 스킬)");
    }

    public void ClearAllSkills()
    {
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

    private void Update()
    {
        if (isInSkillDelay)
        {
            skillDelayRemaining -= Time.deltaTime;
            if (skillDelayRemaining <= 0f)
            {
                isInSkillDelay = false;
                skillDelayRemaining = 0f;
            }
        }
    }
}

[Serializable]
public class SkillSaveData
{
    public List<PlayerSkillSaveData> skills = new List<PlayerSkillSaveData>();
}