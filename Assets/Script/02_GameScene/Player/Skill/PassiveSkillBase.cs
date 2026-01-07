using UnityEngine;
using Definitions;

/// <summary>
/// 패시브 스킬 베이스 클래스
/// 항상 발동되며 캐릭터 스탯에 영향을 주는 스킬
/// </summary>
public abstract class PassiveSkillBase : SkillBase
{
    protected CharacterStats targetStats;
    private bool isApplied = false;

    public PassiveSkillBase(SkillData data, int level = 1) : base(data, level)
    {
    }

    /// <summary>
    /// 패시브 효과 적용
    /// </summary>
    public virtual void ApplyEffect(CharacterStats stats)
    {
        if (isApplied)
        {
            Debug.LogWarning($"[{SkillName}] 이미 적용된 패시브 스킬입니다.");
            return;
        }

        targetStats = stats;
        OnApply(stats);
        isApplied = true;

        Debug.Log($"[{SkillName}] 패시브 효과 적용 (Lv.{currentLevel})");
    }

    /// <summary>
    /// 패시브 효과 제거
    /// </summary>
    public virtual void RemoveEffect(CharacterStats stats)
    {
        if (!isApplied)
        {
            Debug.LogWarning($"[{SkillName}] 적용되지 않은 패시브 스킬입니다.");
            return;
        }

        OnRemove(stats);
        targetStats = null;
        isApplied = false;

        Debug.Log($"[{SkillName}] 패시브 효과 제거");
    }

    /// <summary>
    /// 레벨업 시 효과 갱신
    /// </summary>
    public override void LevelUp()
    {
        base.LevelUp();

        // 이미 적용된 경우 효과 갱신
        if (isApplied && targetStats != null)
        {
            OnRemove(targetStats);
            OnApply(targetStats);
            Debug.Log($"[{SkillName}] 레벨업으로 효과 갱신 (Lv.{currentLevel})");
        }
    }

    /// <summary>
    /// 패시브 효과 적용 구현
    /// </summary>
    protected abstract void OnApply(CharacterStats stats);

    /// <summary>
    /// 패시브 효과 제거 구현
    /// </summary>
    protected abstract void OnRemove(CharacterStats stats);

    /// <summary>
    /// 패시브 스킬은 Use를 사용하지 않음
    /// </summary>
    public override bool Use(Transform caster, Vector3 targetPosition, Transform targetTransform = null)
    {
        Debug.LogWarning($"[{SkillName}] 패시브 스킬은 직접 사용할 수 없습니다.");
        return false;
    }

    /// <summary>
    /// 패시브 스킬은 CanUse 항상 false
    /// </summary>
    public override bool CanUse()
    {
        return false;
    }

    public bool IsApplied => isApplied;
}