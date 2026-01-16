using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillUIManager : MonoBehaviour, IClosableUI
{
    public static SkillUIManager Instance { get; private set; }

    [Header("메인 패널")]
    public GameObject skillUIPanel;
    public Button SkillUiCloseButton;

    [Header("탭 버튼 컨테이너")]
    public Transform jobTabContainer;
    public GameObject jobTabButtonPrefab;

    [Header("스킬 리스트")]
    public Transform SkillListContainer;
    public GameObject skillUIPrepabs;

    private List<SkillSlotUI> activeSkillSlots = new List<SkillSlotUI>();
    private List<GameObject> jobTabButtons = new List<GameObject>();
    private JobsType currentJobTab = JobsType.Novice;
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
        if (SkillManager.Instance != null)
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

        if (PlayerStatsComponent.Instance != null && PlayerStatsComponent.Instance.Stats != null)
        {
            PlayerStatsComponent.Instance.Stats.OnJobChanged -= OnJobChanged;
        }
    }

    private void SetupButtons()
    {
        if (SkillUiCloseButton != null)
            SkillUiCloseButton.onClick.AddListener(CloseSkillUI);

        if (PlayerStatsComponent.Instance != null && PlayerStatsComponent.Instance.Stats != null)
        {
            PlayerStatsComponent.Instance.Stats.OnJobChanged += OnJobChanged;
        }
    }

    public void OpenSkillUI()
    {
        if (isOpen) return;

        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
            return;

        isOpen = true;
        skillUIPanel.SetActive(true);

        RefreshJobTabs();
        RefreshSkillList();

        PlayerHUD.Instance?.RegisterUI(this);
    }

    public void CloseSkillUI()
    {
        if (!isOpen) return;

        isOpen = false;
        skillUIPanel.SetActive(false);
        PlayerHUD.Instance?.UnregisterUI(this);

        // 스킬 디테일 패널 닫기
        if (SkillDetailUIManager.Instance != null)
        {
            SkillDetailUIManager.Instance.HideSkillDetail();
        }
    }

    public bool IsSkillUIOpen()
    {
        return isOpen;
    }


    private void UpdateJobTabColors()
    {
        Color selectedColor = new Color(0f, 0.392f, 1f);

        foreach (var tabBtn in jobTabButtons)
        {
            if (tabBtn == null) continue;

            Button button = tabBtn.GetComponent<Button>();
            if (button == null) continue;

            Image img = tabBtn.GetComponent<Image>();
            if (img == null) continue;

            // 버튼의 onClick 이벤트에서 JobsType 가져오기
            // 이 방법은 완벽하지 않으므로, 탭 버튼 이름으로 구분
            TextMeshProUGUI tabText = tabBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (tabText != null)
            {
                string tabName = tabText.text;
                string currentJobName = GetJobName(currentJobTab);
                img.color = (tabName == currentJobName) ? selectedColor : Color.white;
            }
        }
    }
    public void Close()
    {
        CloseSkillUI();
    }

    public GameObject GetUIPanel()
    {
        return skillUIPanel;
    }

    private void RefreshJobTabs()
    {
        if (jobTabContainer == null || jobTabButtonPrefab == null)
        {
            Debug.LogWarning("[SkillUIManager] jobTabContainer 또는 jobTabButtonPrefab이 없습니다.");
            return;
        }

        foreach (var btn in jobTabButtons)
        {
            Destroy(btn);
        }
        jobTabButtons.Clear();

        if (PlayerStatsComponent.Instance == null || PlayerStatsComponent.Instance.Stats == null)
            return;

        var allJobs = PlayerStatsComponent.Instance.Stats.GetAllJobs();

        if (allJobs.Count == 0)
            return;

        bool isFirstTab = true;
        foreach (var job in allJobs)
        {
            GameObject tabBtn = Instantiate(jobTabButtonPrefab, jobTabContainer);
            jobTabButtons.Add(tabBtn);

            TextMeshProUGUI tabText = tabBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (tabText != null)
            {
                string jobName = GetJobName(job.jobType);
                tabText.text = jobName;
            }

            Button button = tabBtn.GetComponent<Button>();
            if (button != null)
            {
                JobsType capturedJobType = job.jobType;
                button.onClick.AddListener(() => OnJobTabClicked(capturedJobType));

                if (isFirstTab)
                {
                    currentJobTab = capturedJobType;
                    isFirstTab = false;
                }
            }
        }

        UpdateJobTabColors();
    }

    private void OnJobTabClicked(JobsType jobType)
    {
        if (currentJobTab == jobType)
            return;

        currentJobTab = jobType;
        UpdateJobTabColors();
        RefreshSkillList();
    }

    private void RefreshSkillList()
    {
        if (DraggableSkillUi.IsDragging())
        {
            DraggableSkillUi.CancelCurrentDrag();
        }
        activeSkillSlots.Clear();

        foreach (Transform child in SkillListContainer)
            Destroy(child.gameObject);

        List<PlayerSkillData> skills = GetSkillsForCurrentJobTab();

        foreach (var skill in skills)
        {
            CreateSkillListItem(skill);
        }

        Debug.Log($"[SkillUIManager] {currentJobTab} 탭: {skills.Count}개 스킬 표시");
    }

    private void Update()
    {
        if (!isOpen) return;

        foreach (var slot in activeSkillSlots)
        {
            if (slot != null)
            {
                slot.UpdateCooldown();
            }
        }
    }

    private List<PlayerSkillData> GetSkillsForCurrentJobTab()
    {
        if (SkillManager.Instance == null)
            return new List<PlayerSkillData>();

        List<PlayerSkillData> allSkills = SkillManager.Instance.GetSkillsByType();

        List<PlayerSkillData> filteredSkills = new List<PlayerSkillData>();

        foreach (var playerSkill in allSkills)
        {
            SkillData skillData = playerSkill.GetSkillData();
            if (skillData != null)
            {
                if (skillData.requiredJob == currentJobTab)
                {
                    filteredSkills.Add(playerSkill);
                }
            }
        }

        return filteredSkills;
    }

    private string GetJobName(JobsType jobType)
    {
        if (JobsDataManager.Instance != null)
        {
            JobsData jobData = JobsDataManager.Instance.GetJobDataByType(jobType);
            if (jobData != null)
            {
                return jobData.jobName;
            }
        }
        return jobType.ToString();
    }

    private void CreateSkillListItem(PlayerSkillData skill)
    {
        GameObject itemObj = Instantiate(skillUIPrepabs, SkillListContainer);

        SkillData data = skill.GetSkillData();
        if (data == null) return;

        Image iconImage = itemObj.transform.Find("SkillIconImage")?.GetComponent<Image>();
        TextMeshProUGUI skillLevelText = itemObj?.transform.Find("SkillLevelText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descriptionText = itemObj.transform.Find("SkillDescText")?.GetComponent<TextMeshProUGUI>();
        Transform infoPanel = itemObj.transform.Find("SkillNamePanel");
        Slider expSlider = itemObj.transform.Find("SkillExp")?.GetComponent<Slider>();
        TextMeshProUGUI exptext = expSlider?.transform.Find("SkillExpText")?.GetComponent<TextMeshProUGUI>();

        TextMeshProUGUI skillNameText = infoPanel?.Find("SkillNameText")?.GetComponent<TextMeshProUGUI>();

        Image cooldownOverlay = itemObj.transform.Find("SkillCooldownImage")?.GetComponent<Image>();
        TextMeshProUGUI cooldownText = itemObj.transform.Find("SkillCooldownText")?.GetComponent<TextMeshProUGUI>();

        SkillSlotUI slotUI = itemObj.GetComponent<SkillSlotUI>();
        if (slotUI == null)
        {
            slotUI = itemObj.AddComponent<SkillSlotUI>();
        }
        slotUI.Initialize(iconImage, cooldownOverlay, cooldownText, skill);
        activeSkillSlots.Add(slotUI);

        DraggableSkillUi draggable = itemObj.GetComponent<DraggableSkillUi>();
        if (draggable == null)
        {
            draggable = itemObj.AddComponent<DraggableSkillUi>();
        }
        draggable.Initialize(skill);

        if (iconImage != null && !string.IsNullOrEmpty(data.skillIconPath))
        {
            Sprite icon = Resources.Load<Sprite>(data.skillIconPath);
            if (icon != null)
            {
                iconImage.sprite = icon;
            }
        }

        if (skillNameText != null)
            skillNameText.text = data.skillName;

        if (skillLevelText != null)
            skillLevelText.text = $"Lv.{skill.skillLevel}";

        if (descriptionText != null)
            descriptionText.text = data.description;

        if (expSlider != null)
            expSlider.value = skill.GetExpProgress();

        if (exptext != null)
        {
            exptext.text = (skill.GetExpProgress() * 100).ToString() + "%";
            if (skill.IsMaxLevel)
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
        }
    }

    private void OnJobChanged()
    {
        if (isOpen)
        {
            RefreshJobTabs();
            RefreshSkillList();
        }
    }

    public void RefreshUI()
    {
        if (isOpen)
        {
            RefreshJobTabs();
            RefreshSkillList();
        }
    }
}