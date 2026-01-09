using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 버프 UI 관리자
/// BuffManager의 이벤트를 받아서 UI 표시/제거
/// </summary>
public class BuffUIManager : MonoBehaviour
{
    public static BuffUIManager Instance { get; private set; }

    [Header("UI Settings")]
    public Transform buffContainer;            // 버프 아이템들이 들어갈 부모 (Panel)
    public GameObject buffItemPrefab;          // BuffUIItem 프리팹
    public float updateInterval = 0.1f;        // UI 업데이트 주기 (0.1초마다)

    private Dictionary<string, BuffUIItem> activeBuffUIs = new Dictionary<string, BuffUIItem>();
    private float updateTimer = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        foreach (Transform child in buffContainer)
        {
            Destroy(child.gameObject);
        }
        buffContainer.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // BuffManager 이벤트 구독
        if (BuffManager.Instance != null)
        {
            BuffManager.Instance.OnBuffAdded += OnBuffAdded;
            BuffManager.Instance.OnBuffRemoved += OnBuffRemoved;
            BuffManager.Instance.OnBuffRefreshed += OnBuffRefreshed;
        }
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        if (BuffManager.Instance != null)
        {
            BuffManager.Instance.OnBuffAdded -= OnBuffAdded;
            BuffManager.Instance.OnBuffRemoved -= OnBuffRemoved;
            BuffManager.Instance.OnBuffRefreshed -= OnBuffRefreshed;
        }
    }

    private void Update()
    {
        // 일정 주기마다 UI 업데이트
        updateTimer += Time.deltaTime;

        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateAllBuffUIs();
        }
    }

    /// <summary>
    /// 버프 추가 이벤트 처리
    /// </summary>
    private void OnBuffAdded(Buff buff)
    {
        if (buffItemPrefab == null || buffContainer == null)
        {
            Debug.LogWarning("[BuffUIManager] Prefab 또는 Container가 설정되지 않음");
            return;
        }

        // UI 아이템 생성
        GameObject itemObj = Instantiate(buffItemPrefab, buffContainer);
        BuffUIItem uiItem = itemObj.GetComponent<BuffUIItem>();

        if (uiItem != null)
        {
            uiItem.Initialize(buff);
            activeBuffUIs[buff.buffId] = uiItem;

            Debug.Log($"[BuffUIManager] 버프 UI 추가: {buff.buffName}");
            buffContainer.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("[BuffUIManager] BuffUIItem 컴포넌트를 찾을 수 없음");
            Destroy(itemObj);
        }
    }

    /// <summary>
    /// 버프 제거 이벤트 처리
    /// </summary>
    private void OnBuffRemoved(Buff buff)
    {
        if (activeBuffUIs.TryGetValue(buff.buffId, out BuffUIItem uiItem))
        {
            Destroy(uiItem.gameObject);
            activeBuffUIs.Remove(buff.buffId);

            Debug.Log($"[BuffUIManager] 버프 UI 제거: {buff.buffName}");
        }
        if(activeBuffUIs.Count == 0)
        {
            buffContainer.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 버프 갱신 이벤트 처리 (시간 리셋)
    /// </summary>
    private void OnBuffRefreshed(Buff buff)
    {
        if (activeBuffUIs.TryGetValue(buff.buffId, out BuffUIItem uiItem))
        {
            uiItem.Refresh(buff);
            Debug.Log($"[BuffUIManager] 버프 UI 갱신: {buff.buffName}");
        }
    }

    /// <summary>
    /// 모든 버프 UI 업데이트 (남은 시간)
    /// </summary>
    private void UpdateAllBuffUIs()
    {
        foreach (var kvp in activeBuffUIs)
        {
            kvp.Value.UpdateDisplay();
        }
    }

    /// <summary>
    /// 모든 버프 UI 제거
    /// </summary>
    public void ClearAllBuffUIs()
    {
        foreach (var kvp in activeBuffUIs)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }

        activeBuffUIs.Clear();
        Debug.Log("[BuffUIManager] 모든 버프 UI 제거");
    }
}