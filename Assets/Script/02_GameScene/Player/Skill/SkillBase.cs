using UnityEngine;
using Definitions;

/// <summary>
/// 스킬 실행 기본 클래스
/// 각 스킬이 자신의 설정을 오버라이드
/// </summary>
public abstract class SkillBase
{
    protected SkillData skillData;
    protected int currentLevel;
    protected float currentCooldown;

    public SkillBase(SkillData data, int level = 1)
    {
        skillData = data;
        currentLevel = level;
        currentCooldown = 0f;
    }

    // ===== 공통 속성 =====

    public string SkillId => skillData.skillId;
    public string SkillName => skillData.skillName;
    public string Description => skillData.description;
    public SkillType SkillType => skillData.skillType;
    public int CurrentLevel => currentLevel;
    public int MaxLevel => skillData.maxLevel;
    public bool IsOnCooldown => currentCooldown > 0f;
    public float CooldownRemaining => currentCooldown;
    public float CooldownProgress => skillData.cooldown > 0 ? 1f - (currentCooldown / skillData.cooldown) : 1f;

    // ===== 쿨타임 관리 =====

    public void UpdateCooldown(float deltaTime)
    {
        if (currentCooldown > 0f)
        {
            currentCooldown -= deltaTime;
            if (currentCooldown < 0f)
                currentCooldown = 0f;
        }
    }

    protected void StartCooldown()
    {
        currentCooldown = skillData.cooldown;
    }

    public void ResetCooldown()
    {
        currentCooldown = 0f;
    }

    // ===== 스킬 사용 검증 =====

    public virtual bool CanUse()
    {
        if (IsOnCooldown)
        {
            if (FloatingNotificationManager.Instance != null)
            {
                FloatingNotificationManager.Instance.ShowNotification(
                    $"{SkillName} 쿨타임 중 ({currentCooldown:F1}초)");
            }
            return false;
        }
        return true;
    }

    // ===== 스킬 실행 =====

    public virtual bool Use(Transform caster, Vector3 targetPosition, Transform targetTransform = null)
    {
        if (!CanUse())
            return false;

        Execute(caster, targetPosition, targetTransform);
        StartCooldown();

        return true;
    }

    /// <summary>
    /// 스킬 효과 실행 (액티브 스킬이 구현)
    /// </summary>
    protected virtual void Execute(Transform caster, Vector3 targetPosition, Transform targetTransform)
    {
        // 기본 구현 없음 (패시브는 사용 안 함)
    }

    // ===== 레벨업 =====

    public virtual void LevelUp()
    {
        if (currentLevel < skillData.maxLevel)
        {
            currentLevel++;
            Debug.Log($"[Skill] {SkillName} 레벨업! Lv.{currentLevel}");
        }
    }

    public float GetCurrentDamage()
    {
        if (currentLevel <= 1)
            return skillData.damageRate;

        return skillData.damageRate + (skillData.levelUpDamageRate * (currentLevel - 1));
    }

    public float GetCurrentModifier()
    {
        return skillData.GetModifierAtLevel(currentLevel);
    }

    // ===== 유틸리티 =====

    /// <summary>
    /// 이펙트 생성
    /// </summary>
    protected GameObject SpawnEffect(string effectPath, Vector3 position, Quaternion rotation)
    {
        if (string.IsNullOrEmpty(effectPath))
            return null;

        GameObject prefab = Resources.Load<GameObject>(effectPath);
        if (prefab != null)
        {
            return Object.Instantiate(prefab, position, rotation);
        }
        return null;
    }

    /// <summary>
    /// 사운드 재생
    /// </summary>
    protected void PlaySound(string soundPath)
    {
        if (string.IsNullOrEmpty(soundPath))
            return;

        // TODO: 사운드 매니저 연동
        Debug.Log($"[Skill] Playing sound: {soundPath}");
    }
}