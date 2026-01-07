using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance { get; private set; }

    private Dictionary<string, Buff> activeBuffs = new Dictionary<string, Buff>();

    // 이벤트
    public event Action<Buff> OnBuffAdded;
    public event Action<Buff> OnBuffRemoved;
    public event Action<Buff> OnBuffRefreshed;

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
        UpdateBuffs(Time.deltaTime);
    }

    /// <summary>
    /// 버프 적용
    /// </summary>
    public void ApplyBuff(string buffId, string buffName, float duration,
                         Action<CharacterStats> onApply, Action<CharacterStats> onRemove)
    {
        // PlayerStatsComponent 가져오기
        if (PlayerStatsComponent.Instance == null)
        {
            Debug.LogError("[BuffManager] PlayerStatsComponent를 찾을 수 없음");
            return;
        }

        CharacterStats stats = PlayerStatsComponent.Instance.Stats;

        // 이미 버프가 있으면?
        if (activeBuffs.ContainsKey(buffId))
        {
            // 버프 갱신 (시간 리셋)
            Buff existingBuff = activeBuffs[buffId];
            existingBuff.remainingTime = duration;

            Debug.Log($"[BuffManager] 버프 갱신: {buffName} ({duration}초)");
            OnBuffRefreshed?.Invoke(existingBuff);
        }
        else
        {
            // 새 버프 추가
            Buff newBuff = new Buff(buffId, buffName, duration, onApply, onRemove);

            // 버프 효과 적용
            newBuff.onApply?.Invoke(stats);
            newBuff.isActive = true;

            activeBuffs[buffId] = newBuff;

            Debug.Log($"[BuffManager] 버프 적용: {buffName} ({duration}초)");
            OnBuffAdded?.Invoke(newBuff);
        }
    }

    /// <summary>
    /// 버프 수동 제거
    /// </summary>
    public void RemoveBuff(string buffId)
    {
        if (activeBuffs.TryGetValue(buffId, out Buff buff))
        {
            // 버프 효과 제거
            if (PlayerStatsComponent.Instance != null)
            {
                buff.onRemove?.Invoke(PlayerStatsComponent.Instance.Stats);
            }

            activeBuffs.Remove(buffId);

            Debug.Log($"[BuffManager] 버프 제거: {buff.buffName}");
            OnBuffRemoved?.Invoke(buff);
        }
    }

    /// <summary>
    /// 모든 버프 업데이트
    /// </summary>
    private void UpdateBuffs(float deltaTime)
    {
        if (PlayerStatsComponent.Instance == null) return;

        List<string> expiredBuffs = new List<string>();

        // 버프 시간 감소
        foreach (var kvp in activeBuffs)
        {
            kvp.Value.Update(deltaTime);

            if (kvp.Value.IsExpired())
            {
                expiredBuffs.Add(kvp.Key);
            }
        }

        // 만료된 버프 제거
        foreach (string buffId in expiredBuffs)
        {
            RemoveBuff(buffId);
        }
    }

    /// <summary>
    /// 버프가 활성화되어 있는지 확인
    /// </summary>
    public bool HasBuff(string buffId)
    {
        return activeBuffs.ContainsKey(buffId);
    }

    /// <summary>
    /// 버프 정보 가져오기
    /// </summary>
    public Buff GetBuff(string buffId)
    {
        if (activeBuffs.TryGetValue(buffId, out Buff buff))
        {
            return buff;
        }
        return null;
    }

    /// <summary>
    /// 모든 활성 버프 가져오기
    /// </summary>
    public List<Buff> GetActiveBuffs()
    {
        return activeBuffs.Values.ToList();
    }

    /// <summary>
    /// 모든 버프 제거
    /// </summary>
    public void ClearAllBuffs()
    {
        List<string> buffIds = activeBuffs.Keys.ToList();

        foreach (string buffId in buffIds)
        {
            RemoveBuff(buffId);
        }

        Debug.Log("[BuffManager] 모든 버프 제거");
    }
}