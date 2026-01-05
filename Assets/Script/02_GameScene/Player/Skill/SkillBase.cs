using UnityEngine;

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

    // ===== 각 스킬이 오버라이드할 설정들 =====

    protected virtual SkillType Type => SkillType.Damage;
    protected virtual SkillTargetType TargetType => SkillTargetType.Enemy;
    protected virtual float Range => 2f;
    protected virtual float Radius => 0f;
    protected virtual float CastTime => 0f;
    protected virtual int ProjectileCount => 0;
    protected virtual float ProjectileSpeed => 10f;
    protected virtual string EffectPrefabPath => "";
    protected virtual string ProjectilePrefabPath => "";

    // ===== 공통 속성 =====

    public string SkillId => skillData.skillId;
    public string SkillName => skillData.skillName;
    public bool IsOnCooldown => currentCooldown > 0f;
    public float CooldownRemaining => currentCooldown;
    public float CooldownProgress => 1f - (currentCooldown / skillData.cooldown);

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

    public bool Use(Transform caster, Vector3 targetPosition, Transform targetTransform = null)
    {
        if (!CanUse())
            return false;

        Execute(caster, targetPosition, targetTransform);
        StartCooldown();

        return true;
    }

    /// <summary>
    /// 스킬 효과 실행 (각 스킬이 구현)
    /// </summary>
    protected abstract void Execute(Transform caster, Vector3 targetPosition, Transform targetTransform);

    // ===== 레벨업 =====

    public void LevelUp()
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

    // ===== 유틸리티 =====

    /// <summary>
    /// 이펙트 생성
    /// </summary>
    protected GameObject SpawnEffect(Vector3 position, Quaternion rotation)
    {
        if (string.IsNullOrEmpty(EffectPrefabPath))
            return null;

        GameObject prefab = Resources.Load<GameObject>(EffectPrefabPath);
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

// ===== Enum 정의 =====

public enum SkillType
{
    Damage,
    Heal,
    Projectile,
    Area,
    Buff,
    Dash,
    Summon
}

public enum SkillTargetType
{
    Self,
    Enemy,
    Direction,
    Ground,
    Area
}