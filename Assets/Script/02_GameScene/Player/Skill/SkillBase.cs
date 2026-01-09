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

    public SkillBase(SkillData data, int level = 1)
    {
        skillData = data;
        currentLevel = level;
    }

    // ===== 공통 속성 =====

    public string SkillId => skillData.skillId;
    public string SkillName => skillData.skillName;
    public string Description => skillData.description;
    public SkillType SkillType => skillData.skillType;
    public int CurrentLevel => currentLevel;
    public int MaxLevel => skillData.maxLevel;

    public string IconPath => skillData.skillIconPath;

    // ===== 쿨타임 관리 =====

    protected void StartCooldown()
    {
        if (SkillCooldownManager.Instance != null)
        {
            SkillCooldownManager.Instance.StartCooldown(skillData.skillId, skillData.cooldown);
            Debug.Log($"[SkillBase] {SkillName} 쿨타임 시작: {skillData.cooldown}초");
        }
    }
    public bool IsOnCooldown
    {
        get
        {
            if (SkillCooldownManager.Instance != null)
            {
                return SkillCooldownManager.Instance.IsOnCooldown(skillData.skillId);
            }
            return false;
        }
    }
    public float CooldownRemaining
    {
        get
        {
            if (SkillCooldownManager.Instance != null)
            {
                return SkillCooldownManager.Instance.GetRemainingCooldown(skillData.skillId);
            }
            return 0f;
        }
    }
    public float CooldownProgress
    {
        get
        {
            if (SkillCooldownManager.Instance != null)
            {
                return SkillCooldownManager.Instance.GetCooldownProgress(skillData.skillId);
            }
            return 1f;
        }
    }

    public void ResetCooldown()
    {
        if (SkillCooldownManager.Instance != null)
        {
            SkillCooldownManager.Instance.ResetCooldown(skillData.skillId);
        }
    }

    // ===== 스킬 사용 검증 =====

    public virtual bool CanUse()
    {
        if (IsOnCooldown)
        {
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


    // ===== 유틸리티 =====

    /// <summary>
    /// 이펙트 생성 (좌우 반전 지원)
    /// </summary>
    protected GameObject SpawnEffect(string effectPath, Vector3 position, Quaternion rotation, bool flipY = false)
    {
        if (string.IsNullOrEmpty(effectPath))
            return null;

        GameObject prefab = Resources.Load<GameObject>(effectPath);
        if (prefab != null)
        {
            GameObject effect = Object.Instantiate(prefab, position, rotation);

            // 왼쪽 방향이면 Y축 스케일 반전
            if (flipY)
            {
                Vector3 scale = effect.transform.localScale;
                scale.y *= -1;
                effect.transform.localScale = scale;
            }

            // 자동 삭제 처리
            Animator animator = effect.GetComponent<Animator>();
            if (animator != null)
            {
                float destroyTime = GetAnimationLength(animator);
                Object.Destroy(effect, destroyTime);
            }
            else
            {
                Object.Destroy(effect, 0.5f);
            }

            return effect;
        }
        return null;
    }

    /// <summary>
    /// 애니메이션 길이 가져오기
    /// </summary>
    private float GetAnimationLength(Animator animator)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return 0.5f;

        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller.animationClips.Length > 0)
        {
            return controller.animationClips[0].length;
        }

        return 0.5f;
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