using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 쿨타임 관리자
/// UI에서 쿨타임 정보를 조회할 수 있도록 중앙 관리
/// </summary>
public class SkillCooldownManager : MonoBehaviour
{
    public static SkillCooldownManager Instance { get; private set; }

    // 스킬 ID -> 남은 쿨타임
    private Dictionary<string, float> cooldowns = new Dictionary<string, float>();

    // 이벤트: 쿨타임 시작 시
    public event System.Action<string, float> OnCooldownStarted;
    // 이벤트: 쿨타임 완료 시
    public event System.Action<string> OnCooldownCompleted;

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

    private void Update()
    {
        // 모든 쿨타임 업데이트
        List<string> completedSkills = new List<string>();

        foreach (var kvp in cooldowns)
        {
            string skillId = kvp.Key;
            float remaining = kvp.Value;

            if (remaining > 0f)
            {
                cooldowns[skillId] = remaining - Time.deltaTime;

                // 쿨타임 완료
                if (cooldowns[skillId] <= 0f)
                {
                    cooldowns[skillId] = 0f;
                    completedSkills.Add(skillId);
                }
            }
        }

        // 완료 이벤트 발생
        foreach (string skillId in completedSkills)
        {
            OnCooldownCompleted?.Invoke(skillId);
        }
    }

    /// <summary>
    /// 쿨타임 시작
    /// </summary>
    public void StartCooldown(string skillId, float duration)
    {
        if (cooldowns.ContainsKey(skillId))
        {
            cooldowns[skillId] = duration;
        }
        else
        {
            cooldowns.Add(skillId, duration);
        }

        OnCooldownStarted?.Invoke(skillId, duration);
        Debug.Log($"[CooldownManager] {skillId} 쿨타임 시작: {duration}초");
    }

    /// <summary>
    /// 쿨타임 중인지 확인
    /// </summary>
    public bool IsOnCooldown(string skillId)
    {
        if (cooldowns.ContainsKey(skillId))
        {
            return cooldowns[skillId] > 0f;
        }
        return false;
    }

    /// <summary>
    /// 남은 쿨타임 가져오기
    /// </summary>
    public float GetRemainingCooldown(string skillId)
    {
        if (cooldowns.ContainsKey(skillId))
        {
            return Mathf.Max(0f, cooldowns[skillId]);
        }
        return 0f;
    }

    /// <summary>
    /// 쿨타임 진행도 (0~1, 1이 완료)
    /// </summary>
    public float GetCooldownProgress(string skillId)
    {
        if (!cooldowns.ContainsKey(skillId))
            return 1f;

        SkillData skillData = SkillDataManager.Instance?.GetSkillData(skillId);
        if (skillData == null)
            return 1f;

        float remaining = cooldowns[skillId];
        float total = skillData.cooldown;

        if (total <= 0f)
            return 1f;

        return 1f - (remaining / total);
    }

    /// <summary>
    /// 쿨타임 리셋 (치트/디버그용)
    /// </summary>
    public void ResetCooldown(string skillId)
    {
        if (cooldowns.ContainsKey(skillId))
        {
            cooldowns[skillId] = 0f;
            OnCooldownCompleted?.Invoke(skillId);
            Debug.Log($"[CooldownManager] {skillId} 쿨타임 리셋");
        }
    }

    /// <summary>
    /// 모든 쿨타임 리셋
    /// </summary>
    public void ResetAllCooldowns()
    {
        List<string> allSkills = new List<string>(cooldowns.Keys);

        foreach (string skillId in allSkills)
        {
            cooldowns[skillId] = 0f;
            OnCooldownCompleted?.Invoke(skillId);
        }

        Debug.Log($"[CooldownManager] 모든 쿨타임 리셋");
    }
}