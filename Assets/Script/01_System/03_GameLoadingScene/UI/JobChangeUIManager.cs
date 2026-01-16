using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class JobChangeUIManager : MonoBehaviour
{
    public static JobChangeUIManager Instance { get; private set; }

    [Header("Main Panel")]
    [SerializeField] private GameObject jobChangePanel;

    [Header("Job Display")]
    [SerializeField] private Transform jobSlotContainer;
    [SerializeField] private GameObject jobSlotPrefab;

    [Header("Confirm Button")]
    [SerializeField] private Button confirmButton;

    private List<JobChangeSlotUI> jobSlots = new List<JobChangeSlotUI>();
    private JobChangeSlotUI selectedJobSlot;

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

        if (jobChangePanel != null)
            jobChangePanel.SetActive(false);

        SetupButtons();
    }

    private void SetupButtons()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
    }

    public void OpenJobChangeUI(JobsType currentJob)
    {
        if (PlayerStatsComponent.Instance == null || PlayerStatsComponent.Instance.Stats == null)
        {
            Debug.LogWarning("[JobChangeUIManager] PlayerStatsComponent가 없습니다.");
            return;
        }

        List<JobsData> availableJobs = GetAvailableJobs(currentJob);

        jobChangePanel.SetActive(true);
        RefreshJobSlots(availableJobs);
        UpdateConfirmButton();
    }

    private List<JobsData> GetAvailableJobs(JobsType currentJob)
    {
        List<JobsData> availableJobs = new List<JobsData>();

        if (JobsDataManager.Instance == null)
            return availableJobs;

        List<JobsData> allJobs = JobsDataManager.Instance.GetAllJobs();
        CharacterStats stats = PlayerStatsComponent.Instance.Stats;

        foreach (var jobData in allJobs)
        {
            if (jobData.previousJob == currentJob)
            {
                if (stats.level >= jobData.requiredLevel)
                {
                    availableJobs.Add(jobData);
                }
            }
        }

        return availableJobs;
    }

    private void RefreshJobSlots(List<JobsData> jobs)
    {
        foreach (var slot in jobSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        jobSlots.Clear();
        selectedJobSlot = null;

        if (jobSlotContainer == null || jobSlotPrefab == null)
        {
            Debug.LogWarning("[JobChangeUIManager] jobSlotContainer 또는 jobSlotPrefab이 없습니다.");
            return;
        }

        foreach (var jobData in jobs)
        {
            GameObject slotObj = Instantiate(jobSlotPrefab, jobSlotContainer);
            JobChangeSlotUI slotUI = slotObj.GetComponent<JobChangeSlotUI>();

            if (slotUI == null)
            {
                slotUI = slotObj.AddComponent<JobChangeSlotUI>();
            }

            slotUI.Initialize(jobData);

            Button slotButton = slotObj.GetComponent<Button>();
            if (slotButton != null)
            {
                slotButton.onClick.AddListener(() => OnJobSlotSelected(slotUI));
            }

            jobSlots.Add(slotUI);
        }
    }

    public void OnJobSlotSelected(JobChangeSlotUI slot)
    {
        if (selectedJobSlot != null)
        {
            selectedJobSlot.SetSelected(false);
        }

        selectedJobSlot = slot;
        selectedJobSlot.SetSelected(true);

        UpdateConfirmButton();
    }

    private void UpdateConfirmButton()
    {
        if (confirmButton != null)
        {
            confirmButton.interactable = (selectedJobSlot != null);
        }
    }

    private void OnConfirmButtonClicked()
    {
        if (selectedJobSlot == null || selectedJobSlot.JobData == null)
        {
            Debug.LogWarning("[JobChangeUIManager] 선택된 직업이 없습니다.");
            return;
        }

        if (PlayerStatsComponent.Instance == null || PlayerStatsComponent.Instance.Stats == null)
        {
            Debug.LogWarning("[JobChangeUIManager] PlayerStatsComponent가 없습니다.");
            return;
        }

        CharacterStats stats = PlayerStatsComponent.Instance.Stats;
        JobsData selectedJob = selectedJobSlot.JobData;

        stats.AddJob(selectedJob.jobstype, true);

        if (FloatingNotificationManager.Instance != null)
        {
            FloatingNotificationManager.Instance.ShowNotification($"{selectedJob.jobName}(으)로 전직했습니다!");
        }

        Debug.Log($"[JobChangeUIManager] 전직 완료: {selectedJob.jobName}");

        CloseJobChangeUI();
    }
    public void CloseJobChangeUI()
    {
        if (jobChangePanel != null)
            jobChangePanel.SetActive(false);

        selectedJobSlot = null;
    }
}