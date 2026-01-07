using UnityEngine;

/// <summary>
/// 액티브 스킬 베이스 클래스
/// 사용자가 직접 발동하는 스킬
/// </summary>
public abstract class ActiveSkillBase : SkillBase
{
    public ActiveSkillBase(SkillData data, int level = 1) : base(data, level)
    {
    }

    /// <summary>
    /// 액티브 스킬 실행 (자식 클래스에서 구현)
    /// </summary>
    protected abstract override void Execute(Transform caster, Vector3 targetPosition, Transform targetTransform);
}