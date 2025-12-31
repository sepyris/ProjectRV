using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UI;

public class SkillUIManager : MonoBehaviour,IClosableUI
{
    // Singleton instance
    public static SkillUIManager Instance { get; private set; }

    [Header("메인 패널")]
    public GameObject skillUIPanel;
    public Button SkillUiCloseButton;

    [Header("탭버튼")]
    public Button ActiveSkillTabButton;
    public GameObject skillUIPrepabs;

    private bool isOpen = false;

    void Awake()
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
        skillUIPanel.SetActive(false);
        SetupButtons();

    }
    private void SetupButtons()
    {
        if (SkillUiCloseButton != null)
            SkillUiCloseButton.onClick.AddListener(CloseSkillUI);
    }

    public void OpenSkillUI()
    {
        if (isOpen) return;

        // 대화 중이면 열지 않음
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
            return;

        isOpen = true;
        skillUIPanel.SetActive(true);
        PlayerHUD.Instance?.RegisterUI(this);
    }

    public void CloseSkillUI()
    {
        if (!isOpen) return;

        isOpen = false;
        skillUIPanel.SetActive(false);
        PlayerHUD.Instance?.UnregisterUI(this);
    }
    public bool IsSkillUIOpen()
    {
        return isOpen;
    }
    public void Close()
    {
        CloseSkillUI();
    }

    public GameObject GetUIPanel()
    {
        return skillUIPanel;
    }


}
