using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JobChangeSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI jobNameText;
    [SerializeField] private TextMeshProUGUI jobDescriptionText;
    [SerializeField] private Transform skillIconContainer;
    [SerializeField] private GameObject skillIconPrefab;
    [SerializeField] private GameObject selectionBackground;

    private JobsData jobData;
    private bool isSelected = false;

    public JobsData JobData => jobData;
    public bool IsSelected => isSelected;

    public void Initialize(JobsData data)
    {
        jobData = data;
        RefreshUI();
        SetSelected(false);
    }

    private void RefreshUI()
    {
        if (jobData == null) return;

        if (jobNameText != null)
            jobNameText.text = jobData.jobName;

        if (jobDescriptionText != null)
            jobDescriptionText.text = jobData.description;

        CreateSkillIcons();
    }

    private void CreateSkillIcons()
    {
        if (skillIconContainer == null || skillIconPrefab == null)
            return;

        foreach (Transform child in skillIconContainer)
        {
            Destroy(child.gameObject);
        }

        if (SkillDataManager.Instance == null)
            return;

        var jobSkills = SkillDataManager.Instance.GetJobSkills(jobData.jobstype);

        foreach (var skillEntry in jobSkills)
        {
            SkillData skillData = skillEntry.Value;

            GameObject iconObj = Instantiate(skillIconPrefab, skillIconContainer);
            Image iconImage = iconObj.GetComponent<Image>();

            if (iconImage != null && !string.IsNullOrEmpty(skillData.skillIconPath))
            {
                Sprite icon = Resources.Load<Sprite>(skillData.skillIconPath);
                if (icon != null)
                {
                    iconImage.sprite = icon;
                }
            }

            JobChangeSkillIconUI iconUI = iconObj.GetComponent<JobChangeSkillIconUI>();
            if (iconUI == null)
            {
                iconUI = iconObj.AddComponent<JobChangeSkillIconUI>();
            }
            iconUI.Initialize(skillData);
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectionBackground != null)
        {
            selectionBackground.SetActive(selected);
        }
    }

    public void OnSlotClicked()
    {
        JobChangeUIManager manager = FindObjectOfType<JobChangeUIManager>();
        if (manager != null)
        {
            manager.OnJobSlotSelected(this);
        }
    }
}