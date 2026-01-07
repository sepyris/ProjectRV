using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicMissile : ActiveSkillBase
{
    public MagicMissile(SkillData data, int level = 1) : base(data, level)
    {
    }

    protected override void Execute(Transform caster, Vector3 targetPosition, Transform targetTransform)
    {
        throw new System.NotImplementedException();
    }
}
