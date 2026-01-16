using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuickUiManager : MonoBehaviour
{
    public static QuickUiManager instance { get; private set; }

    public Button QuickUiItemButton;
    public Button QuickUiEquipButton;
    public Button QuickUiQuestButton;
    public Button QuickUiStatButton;
    public Button QuickUiSkillButton;
    //public Button QuickUiPartyButton;


    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
        SetButton();
    }

    private void SetButton()
    {
        if (QuickUiItemButton != null)
            QuickUiItemButton.onClick.AddListener(() => OpenItemUi());
        if (QuickUiQuestButton != null)
            QuickUiQuestButton.onClick.AddListener(() => OpenQuestUi());
        if (QuickUiStatButton != null)
            QuickUiStatButton.onClick.AddListener(() => OpenStatUi());
        if (QuickUiEquipButton != null)
            QuickUiEquipButton.onClick.AddListener(() => OpenEquipUi());
        if (QuickUiSkillButton != null)
            QuickUiSkillButton.onClick.AddListener(() => OpenSkillUi());
    }

    private void OpenItemUi()
    {
        PlayerHUD.Instance.ToggleInventoryUI();
    }
    
    private void OpenQuestUi()
    {
        PlayerHUD.Instance.ToggleQuestUI();
    }

    private void OpenStatUi()
    {
        PlayerHUD.Instance.ToggleStatsUI();
    }
    private void OpenEquipUi()
    {
        PlayerHUD.Instance.ToggleEquipmentUI();
    }
    private void OpenSkillUi()
    {
        PlayerHUD.Instance.ToggleSkillUI();
    }
}
