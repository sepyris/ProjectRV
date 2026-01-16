using System.Collections.Generic;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    public static PlayerHUD Instance { get; private set; }

    //  UI 레이어 관리
    private List<IClosableUI> openedUIs = new List<IClosableUI>();

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

    // ==================== UI 레이어 관리 ====================


    /// UI를 열 때 호출 - 맨 위로 올림

    public void RegisterUI(IClosableUI ui)
    {
        if (ui == null) return;

        // 이미 열려있으면 제거 후 다시 추가 (맨 위로)
        if (openedUIs.Contains(ui))
        {
            openedUIs.Remove(ui);
        }

        openedUIs.Add(ui);
        ui.GetUIPanel().transform.SetAsLastSibling();

        Debug.Log($"[PlayerHUD] UI 등록: {ui.GetUIPanel().name}, 총 {openedUIs.Count}개");
    }


    /// UI를 닫을 때 호출

    public void UnregisterUI(IClosableUI ui)
    {
        if (openedUIs.Contains(ui))
        {
            openedUIs.Remove(ui);
            Debug.Log($"[PlayerHUD] UI 해제: {ui.GetUIPanel().name}, 남은 {openedUIs.Count}개");
        }
    }


    /// UiDragger에서 사용
    /// 드래그시에 선택된 UI를 맨위로 설정

    public void SetTopUI(GameObject ui_panel)
    {
        IClosableUI current_ui = null;
        foreach (var ui in openedUIs)
        {
            if (ui.GetUIPanel() == ui_panel)
            {
                current_ui = ui;
                break;
            }
        }
        if (current_ui != null)
        {
            RegisterUI(current_ui);
        }
    }


    /// PlayerController의 Cancel 입력에서 호출
    /// 맨 위 UI 닫기

    public void CloseTopUI()
    {
        if (openedUIs.Count == 0)
        {
            Debug.Log("[PlayerHUD] 열린 UI가 없습니다.");
            return;
        }

        IClosableUI topUI = openedUIs[openedUIs.Count - 1];
        Debug.Log($"[PlayerHUD] 맨 위 UI 닫기: {topUI.GetUIPanel().name}");

        topUI.Close();
    }


    /// 모든 UI 닫기

    public void CloseAllUIs()
    {
        while (openedUIs.Count > 0)
        {
            CloseTopUI();
        }
    }


    /// 열린 UI 개수

    public int GetOpenUICount()
    {
        return openedUIs.Count;
    }


    /// UI가 하나라도 열려있는지

    public bool IsAnyUIOpen()
    {
        return openedUIs.Count > 0;
    }


    /// 맨 위 UI 가져오기

    public GameObject GetTopUIPanel()
    {
        if (openedUIs.Count > 0)
            return openedUIs[openedUIs.Count - 1].GetUIPanel();
        return null;
    }

    // ==================== UI Management ====================
    public void ToggleQuestUI()
    {
        if (QuestUIManager.Instance == null) return;

        if (QuestUIManager.Instance.IsQuestUIOpen())
            QuestUIManager.Instance.CloseQuestUI();
        else
            QuestUIManager.Instance.OpenQuestUI();
    }

    public void ToggleInventoryUI()
    {
        if (ItemUIManager.Instance == null) return;

        if (ItemUIManager.Instance.IsItemUIOpen())
            ItemUIManager.Instance.CloseItemUI();
        else
            ItemUIManager.Instance.OpenItemUI();
    }

    public void ToggleStatsUI()
    {
        if (CharacterStatUIManager.Instance == null) return;

        if (CharacterStatUIManager.Instance.IsStatsUIOpen())
            CharacterStatUIManager.Instance.CloseStatUI();
        else
            CharacterStatUIManager.Instance.OpenStatUI();
    }
    public void ToggleEquipmentUI()
    {
        if (EquipmentUIManager.Instance == null) return;

        if (EquipmentUIManager.Instance.IsEquipmentUIOpen())
            EquipmentUIManager.Instance.CloseEquipmentUI();
        else
            EquipmentUIManager.Instance.OpenEquipmentUI();
    }

    public void ToggleSkillUI()
    {
        if (SkillUIManager.Instance == null) return;

        if (SkillUIManager.Instance.IsSkillUIOpen())
            SkillUIManager.Instance.CloseSkillUI();
        else
            SkillUIManager.Instance.OpenSkillUI();

    }
}