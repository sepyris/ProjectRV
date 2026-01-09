using System.Collections.Generic;
using TMPro;
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

    [Header("스킬 리스트")]
    public Transform SkillListContainer;
    public GameObject skillUIPrepabs;

    private List<SkillSlotUI> activeSkillSlots = new List<SkillSlotUI>();

    private enum SkillTab
    {
        FirstSkill,
        SecondSkill,
    }
    private SkillTab currentTab = SkillTab.FirstSkill;
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
        if(SkillManager.Instance!= null)
        {
            SkillManager.Instance.OnSkillChanged += OnSkillChanged;
        }
        
    }
    void OnDestroy()
    {
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.OnSkillChanged -= OnSkillChanged;
        }
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
        RefreshSkillList();
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
    private void RefreshSkillList()
    {
        if(DraggableSkillUi.IsDragging())
        {
            DraggableSkillUi.CancelCurrentDrag();
        }
        activeSkillSlots.Clear();


        // 기존 리스트 아이템 삭제
        foreach (Transform child in SkillListContainer)
            Destroy(child.gameObject);

        // 현재 탭에 맞는 아이템 가져오기
        List<PlayerSkillData> items = GetItemsForCurrentTab();

        // 아이템 리스트 아이템 생성
        foreach (var item in items)
        {
            CreateSkillListItem(item);
        }

        Debug.Log($"[ItemUI] {currentTab} 탭: {items.Count}개 아이템 표시");
    }
    private void Update()
    {
        if (!isOpen) return;

        // 모든 스킬 슬롯 쿨타임 업데이트
        foreach (var slot in activeSkillSlots)
        {
            if (slot != null)
            {
                slot.UpdateCooldown();
            }
        }
    }

    private List<PlayerSkillData> GetItemsForCurrentTab()
    {
        if (SkillManager.Instance == null)
            return new List<PlayerSkillData>();

        switch (currentTab)
        {
            case SkillTab.FirstSkill:
                return SkillManager.Instance.GetSkillsByType();

            case SkillTab.SecondSkill:
                return SkillManager.Instance.GetSkillsByType();

            default:
                return new List<PlayerSkillData>();
        }
    }

    private void CreateSkillListItem(PlayerSkillData skill)
    {
        GameObject itemObj = Instantiate(skillUIPrepabs, SkillListContainer);

        SkillData data = skill.GetSkillData();
        if (data == null) return;

        // ===== 이름으로 자식 찾기 =====

        Image iconImage = itemObj.transform.Find("SkillIconImage")?.GetComponent<Image>();
        TextMeshProUGUI skillLevelText = itemObj?.transform.Find("SkillLevelText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descriptionText = itemObj.transform.Find("SkillDescText")?.GetComponent<TextMeshProUGUI>();
        Transform infoPanel = itemObj.transform.Find("SkillNamePanel");
        Slider expSlider = itemObj.transform.Find("SkillExp")?.GetComponent<Slider>();
        TextMeshProUGUI exptext = expSlider?.transform.Find("SkillExpText")?.GetComponent<TextMeshProUGUI>();

        TextMeshProUGUI skillNameText = infoPanel?.Find("SkillNameText")?.GetComponent<TextMeshProUGUI>();
        

        Image cooldownOverlay = itemObj.transform.Find("SkillCooldownImage")?.GetComponent<Image>();
        TextMeshProUGUI cooldownText = itemObj.transform.Find("SkillCooldownText")?.GetComponent<TextMeshProUGUI>();

        // ===== SkillSlotUI 컴포넌트 추가 =====

        SkillSlotUI slotUI = itemObj.GetComponent<SkillSlotUI>();
        if (slotUI == null)
        {
            slotUI = itemObj.AddComponent<SkillSlotUI>();
        }
        slotUI.Initialize(iconImage, cooldownOverlay, cooldownText, skill);
        activeSkillSlots.Add(slotUI);

        // ===== 드래그 컴포넌트 =====

        DraggableSkillUi draggable = itemObj.GetComponent<DraggableSkillUi>();
        if (draggable == null)
        {
            draggable = itemObj.AddComponent<DraggableSkillUi>();
        }
        draggable.Initialize(skill);

        // ===== 아이콘 설정 =====

        if (iconImage != null && !string.IsNullOrEmpty(data.skillIconPath))
        {
            Sprite icon = Resources.Load<Sprite>(data.skillIconPath);
            if (icon != null)
            {
                iconImage.sprite = icon;
            }
        }

        // ===== 텍스트 설정 =====

        if (skillNameText != null)
            skillNameText.text = data.skillName;

        if (skillLevelText != null)
            skillLevelText.text = $"Lv.{skill.skillLevel}";

        if (descriptionText != null)
            descriptionText.text = data.description;

        // ===== 경험치 =====

        if (expSlider != null)
            expSlider.value = skill.GetExpProgress();

        if(exptext != null)
        {
            exptext.text = (skill.GetExpProgress() * 100).ToString() + "%";
            if(skill.IsMaxLevel)
            {
                exptext.text = "Max";
                skillLevelText.text = "Lv.Max";

            }
        }
            
        
    }

    private void OnSkillChanged()
    {
        if (isOpen)
        {
            RefreshSkillList();

            //  상세 패널은 RefreshItemList()에서 이미 숨겨지므로 
            // 여기서는 다시 표시하지 않음 (호버 시에만 표시)
        }
    }

    public void RefreshUI()
    {
        if (isOpen)
            RefreshSkillList();
    }
}
