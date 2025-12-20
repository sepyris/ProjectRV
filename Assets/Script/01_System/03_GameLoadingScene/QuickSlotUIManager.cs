using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 퀵슬롯 UI 전체 관리자
/// 10개의 퀵슬롯 UI를 관리하고 업데이트
/// </summary>
public class QuickSlotUIManager : MonoBehaviour
{
    public static QuickSlotUIManager Instance { get; private set; }

    [Header("UI 참조")]
    [SerializeField] private GameObject quickSlotPanel;
    [SerializeField] private QuickSlotUI[] quickSlotUIs; // 10개의 퀵슬롯 UI

    private bool isPanelVisible = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeSlotUIs();
    }

    void Start()
    {
        RefreshAllSlots();
    }

    void Update()
    {
    }

    void OnDestroy()
    {
    }

    /// <summary>
    /// 슬롯 UI 초기화
    /// </summary>
    private void InitializeSlotUIs()
    {
        if (quickSlotUIs == null || quickSlotUIs.Length == 0)
        {
            // 자식에서 자동으로 찾기
            quickSlotUIs = GetComponentsInChildren<QuickSlotUI>(true);
        }

        // 각 슬롯에 인덱스 설정
        for (int i = 0; i < quickSlotUIs.Length && i < 10; i++)
        {
            if (quickSlotUIs[i] != null)
            {
                quickSlotUIs[i].SetSlotIndex(i);
            }
        }

        Debug.Log($"[QuickSlotUIManager] {quickSlotUIs.Length}개의 퀵슬롯 UI 초기화");
    }

    /// <summary>
    /// 모든 슬롯 UI 새로고침
    /// </summary>
    public void RefreshAllSlots()
    {
        foreach (var slotUI in quickSlotUIs)
        {
            if (slotUI != null)
            {
                slotUI.RefreshUI();
            }
        }
    }

    /// <summary>
    /// 특정 슬롯 UI 새로고침
    /// </summary>
    public void RefreshSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < quickSlotUIs.Length && quickSlotUIs[slotIndex] != null)
        {
            quickSlotUIs[slotIndex].RefreshUI();
        }
    }

    /// <summary>
    /// 패널 가시성 토글
    /// </summary>
    public void ToggleQuickSlotPanel()
    {
        isPanelVisible = !isPanelVisible;
        if (quickSlotPanel != null)
        {
            quickSlotPanel.SetActive(isPanelVisible);
        }
    }

    /// <summary>
    /// 패널 표시/숨기기
    /// </summary>
    public void SetPanelVisible(bool visible)
    {
        isPanelVisible = visible;
        if (quickSlotPanel != null)
        {
            quickSlotPanel.SetActive(visible);
        }
    }

    /// <summary>
    /// 특정 아이템이 등록된 슬롯 하이라이트 (선택사항)
    /// </summary>
    public void HighlightItemSlots(string itemId)
    {
        if (QuickSlotManager.Instance == null)
            return;

        for (int i = 0; i < quickSlotUIs.Length; i++)
        {
            QuickSlotData slotData = QuickSlotManager.Instance.GetSlotData(i);
            if (slotData != null &&
                slotData.slotType == QuickSlotType.Consumable &&
                slotData.itemId == itemId)
            {
                // TODO: 하이라이트 효과 추가
                Debug.Log($"[QuickSlotUIManager] 슬롯 {i + 1} 하이라이트: {itemId}");
            }
        }
    }

}