using UnityEngine;
using System;

public class Buff
{
    public string buffId;
    public string buffName;
    public float duration;
    public float remainingTime;
    public Action<CharacterStats> onApply;
    public Action<CharacterStats> onRemove;
    public bool isActive;

    public Buff(string id, string name, float duration, Action<CharacterStats> apply, Action<CharacterStats> remove)
    {
        buffId = id;
        buffName = name;
        this.duration = duration;
        remainingTime = duration;
        onApply = apply;
        onRemove = remove;
        isActive = false;
    }

    public void Update(float deltaTime)
    {
        if (isActive)
        {
            remainingTime -= deltaTime;
        }
    }

    public bool IsExpired()
    {
        return remainingTime <= 0f;
    }
}