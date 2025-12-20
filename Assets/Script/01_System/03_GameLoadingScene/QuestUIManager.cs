using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Definitions;

/// <summary>
/// Q키로 열리는 독립적인 퀘스트 창 UI 관리
/// PlayerController에서 호출됨
/// </summary>
public class QuestUIManager : MonoBehaviour,IClosableUI
{
    public static QuestUIManager Instance { get; private set; }

    [Header("메인 패널")]
    public GameObject questUIPanel;
    public Button closeButton;

    [Header("탭 버튼")]
    public Button availableTabButton;
    public Button inProgressTabButton;
    public Button completedTabButton;

    [Header("퀘스트 리스트")]
    public Transform questListContainer;
    public GameObject questListItemPrefab;

    [Header("퀘스트 상세 정보")]
    public GameObject questDetailPanel;
    public TextMeshProUGUI questNameText;
    public TextMeshProUGUI questDescriptionText;
    public TextMeshProUGUI questObjectivesText;
    public TextMeshProUGUI questRewardsText;
    public Image questStatusImage;
    public Sprite availableSprite;
    public Sprite inProgressSprite;
    public Sprite completedSprite;

    [Header("빈 상태 표시")]
    public GameObject emptyStatePanel;
    public TextMeshProUGUI emptyStateText;

    [Header("퀘스트 트래커 (핀 기능)")]
    [Tooltip("핀이 눌렸을 때 사용할 스프라이트")]
    public Sprite pinOnSprite;
    [Tooltip("핀이 눌리지 않았을 때 사용할 스프라이트")]
    public Sprite pinOffSprite;

    //처음 열릴때만 초기화
    private bool is_initialize = true;

    private enum QuestTab
    {
        Available,
        InProgress,
        Completed
    }

    private QuestTab currentTab = QuestTab.Available;
    private string selectedQuestId;
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
            return;
        }

        if (questUIPanel != null)
            questUIPanel.SetActive(false);

        SetupButtons();
    }

    void Update()
    {

    }

    void Start()
    {
        // 퀘스트 매니저의 이벤트 구독
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStatusChanged += OnQuestStatusChanged;
            QuestManager.Instance.OnQuestObjectiveUpdated += OnQuestObjectiveUpdated;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            // 이벤트 구독 해제
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestStatusChanged -= OnQuestStatusChanged;
                QuestManager.Instance.OnQuestObjectiveUpdated -= OnQuestObjectiveUpdated;
            }
        }
    }


    private void SetupButtons()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseQuestUI);

        if (availableTabButton != null)
            availableTabButton.onClick.AddListener(() => SwitchTab(QuestTab.Available));

        if (inProgressTabButton != null)
            inProgressTabButton.onClick.AddListener(() => SwitchTab(QuestTab.InProgress));

        if (completedTabButton != null)
            completedTabButton.onClick.AddListener(() => SwitchTab(QuestTab.Completed));
    }

    // ==========================================
    // 퀘스트 UI 열기/닫기
    // ==========================================
    public void OpenQuestUI()
    {
        if (isOpen) return;

        // 대화 중이면 퀘스트 창 열지 않음
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
            return;

        isOpen = true;
        questUIPanel.SetActive(true);
        PlayerHUD.Instance?.RegisterUI(this);
        if (is_initialize)
        {
            // 기본 탭으로 초기화
            SwitchTab(QuestTab.Available);
            is_initialize = false;
        }

        //UpdateTabButtons();
        RefreshQuestList();
        Debug.Log("[QuestUI] 퀘스트 창 열림");
    }

    public void CloseQuestUI()
    {
        if (!isOpen) return;

        isOpen = false;
        questUIPanel.SetActive(false);
        PlayerHUD.Instance?.UnregisterUI(this);
        Debug.Log("[QuestUI] 퀘스트 창 닫힘");
    }

    // ==========================================
    // 탭 전환
    // ==========================================
    private void SwitchTab(QuestTab tab)
    {
        currentTab = tab;
        //UpdateTabButtons();
        RefreshQuestList();
    }

    private void UpdateTabButtons()
    {
        // 탭 버튼 활성화 상태 업데이트
        if (availableTabButton != null)
        {
            var colors = availableTabButton.colors;
            colors.normalColor = currentTab == QuestTab.Available ? new Color(1f, 1f, 0.5f) : Color.white;
            availableTabButton.colors = colors;
        }

        if (inProgressTabButton != null)
        {
            var colors = inProgressTabButton.colors;
            colors.normalColor = currentTab == QuestTab.InProgress ? new Color(0.5f, 1f, 0.5f) : Color.white;
            inProgressTabButton.colors = colors;
        }

        if (completedTabButton != null)
        {
            var colors = completedTabButton.colors;
            colors.normalColor = currentTab == QuestTab.Completed ? new Color(0.5f, 0.5f, 1f) : Color.white;
            completedTabButton.colors = colors;
        }
    }

    // ==========================================
    // 퀘스트 리스트 갱신
    // ==========================================
    private void RefreshQuestList()
    {
        // 기존 리스트 아이템 삭제
        foreach (Transform child in questListContainer)
            Destroy(child.gameObject);



        // 현재 탭에 맞는 퀘스트 가져오기
        List<QuestData> quests = GetQuestsForCurrentTab();

        if (quests == null || quests.Count == 0)
        {
            ShowEmptyState();

            return;
        }

        if (emptyStatePanel != null)
            emptyStatePanel.SetActive(false);

        // 퀘스트 리스트 아이템 생성
        bool is_selected = false;
        foreach (var quest in quests)
        {
            CreateQuestListItem(quest);

            //퀘스트가 완료된 상태가 아니라면 목표 갱신
            if(quest.status != QuestStatus.Rewarded)
            {
                string objectives = GetQuestObjectives(quest);
                questObjectivesText.text = $"<b>목표</b>\n{objectives}";
            }
            

            if (quest.questId == selectedQuestId)
            {
                is_selected = true;
            }
        }
        if (!is_selected)
        {
            selectedQuestId = null;
            if (questDetailPanel != null)
                questDetailPanel.SetActive(false);
        }

        Debug.Log($"[QuestUI] {currentTab} 탭: {quests.Count}개 퀘스트 표시");
    }

    private List<QuestData> GetQuestsForCurrentTab()
    {
        if (QuestManager.Instance == null)
            return new List<QuestData>();

        var allQuests = QuestManager.Instance.GetAllQuests();

        switch (currentTab)
        {
            case QuestTab.Available:
                return allQuests.Where(q => q.status == QuestStatus.None || q.status == QuestStatus.Offered).ToList();

            case QuestTab.InProgress:
                //  InProgress 탭: Accepted와 Completed 상태 모두 표시 
                return allQuests.Where(q => q.status == QuestStatus.Accepted || q.status == QuestStatus.Completed).ToList();

            case QuestTab.Completed:
                //  Completed 탭: Rewarded 상태만 표시 (완전히 완료된 퀘스트) 
                return allQuests.Where(q => q.status == QuestStatus.Rewarded).ToList();

            default:
                return new List<QuestData>();
        }
    }

    private void CreateQuestListItem(QuestData quest)
    {
        GameObject itemObj = Instantiate(questListItemPrefab, questListContainer);

        // ===== 메인 버튼 (itemObj 자신의 Button) =====
        Button itemButton = itemObj.GetComponent<Button>();

        // ===== 핀 버튼 (자식 중에서 찾기, 자기 자신 제외) =====
        Button pinButton = null;
        foreach (Transform child in itemObj.transform)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null)
            {
                pinButton = btn;
                break;
            }
        }

        TextMeshProUGUI itemText = itemObj.GetComponentInChildren<TextMeshProUGUI>();

        if (itemText != null)
        {
            string statusIcon = GetStatusIcon(quest);
            itemText.text = $"{statusIcon} {quest.questName}";
        }

        string capturedQuestId = quest.questId;
        if (itemButton != null)
        {
            itemButton.onClick.AddListener(() => ShowQuestDetail(capturedQuestId));
        }

        // ===== 핀 버튼 기능 추가 =====
        // InProgress 탭에서만 핀 버튼 표시
        if (currentTab == QuestTab.InProgress && pinButton != null)
        {
            // 핀 버튼 활성화
            pinButton.gameObject.SetActive(true);

            // 현재 핀 상태에 따라 스프라이트 설정
            bool isPinned = QuestTrackerUIManager.Instance != null &&
                            QuestTrackerUIManager.Instance.IsQuestTracked(quest.questId);

            Image pinButtonImage = pinButton.GetComponent<Image>();
            if (pinButtonImage != null)
            {
                pinButtonImage.sprite = isPinned ? pinOnSprite : pinOffSprite;
            }

            // 핀 버튼 클릭 이벤트 등록
            pinButton.onClick.RemoveAllListeners();
            pinButton.onClick.AddListener(() => OnPinButtonClicked(capturedQuestId, pinButton));
        }
        else if (pinButton != null)
        {
            // InProgress 탭이 아니면 핀 버튼 숨기기
            pinButton.gameObject.SetActive(false);
        }
        // ================================
    }

    /// <summary>
    /// 상태 아이콘 가져오기
    ///  새로운 상태 흐름에 맞춰 수정 
    /// </summary>
    private string GetStatusIcon(QuestData quest)
    {
        switch (quest.status)
        {
            case QuestStatus.None:
            case QuestStatus.Offered:
                return "[시작 가능]";

            case QuestStatus.Accepted:
                //  Accepted 상태에서도 목표가 모두 완료되었는지 확인 
                if (quest.IsCompleted())
                {
                    return "[완료 가능]";
                }
                return "[진행중]";

            case QuestStatus.Completed:
                //  Completed = 목표 달성, NPC에게 보고 필요 
                return "[완료 가능]";

            case QuestStatus.Rewarded:
                //  Rewarded = 보상 받음, 완전히 완료됨 
                return "[완료]";

            default:
                return "";
        }
    }

    /// <summary>
    /// 퀘스트 목표 텍스트 생성 (완료 가능 상태 강조)
    /// </summary>
    private string GetQuestObjectives(QuestData quest)
    {
        if (quest.objectives == null || quest.objectives.Count == 0)
            return "목표 정보 없음";

        List<string> objectiveTexts = new List<string>();
        bool allCompleted = true;
        bool firstObjective = false;
        //퀘스트를 아직 받지 않은 상태라면 스테이터스 메세지 변경을 위해 추가 처리;
        if (quest.status == QuestStatus.None)
        {
            firstObjective = true;
        }
        foreach (var obj in quest.objectives)
        {
            string status = obj.IsCompleted ? "[완료]" : "[진행중]";
            if (firstObjective)
            {
                status = "";
                firstObjective = false;
            }
            if (!obj.IsCompleted) allCompleted = false;

            string typeText = GetObjectiveTypeText(obj.type);
            string progress = $" ({obj.currentCount}/{obj.requiredCount})";

            string objectiveText = $"{status} {typeText} {obj.targetId}{progress}";

            // Dialogue 목표인 경우 NPC 위치 정보 추가
            if (obj.type == QuestType.Dialogue && !obj.IsCompleted && NPCInfoManager.Instance != null)
            {
                Npcs npcInfo = NPCInfoManager.Instance.GetNPCInfo(obj.targetId);
                if (npcInfo != null)
                {
                    objectiveText += $"\n  {npcInfo.npcName}: {npcInfo.GetLocationDescription()}";
                }
            }

            objectiveTexts.Add(objectiveText);
        }

        //  모든 목표 완료 시 안내 메시지 추가 
        if (allCompleted && (quest.status == QuestStatus.Accepted || quest.status == QuestStatus.Completed))
        {
            objectiveTexts.Add("\n<color=#FFD700>✓ 모든 목표 달성! NPC에게 돌아가세요.</color>");
        }

        // 추가 힌트 표시 (questHint 활용)
        string fullHint = quest.GetFullQuestHint();
        if (!string.IsNullOrEmpty(fullHint) && quest.status == QuestStatus.Accepted && !allCompleted)
        {
            objectiveTexts.Add($"\n<color=#87CEEB>힌트:\n{fullHint}</color>");
        }

        // 기존 힌트 (하위 호환)
        string locationHint = quest.GetObjectiveLocationHint();
        if (!string.IsNullOrEmpty(locationHint) && string.IsNullOrEmpty(fullHint))
        {
            objectiveTexts.Add($"\n<color=#FFD700>힌트: {locationHint}</color>");
        }

        return string.Join("\n", objectiveTexts);
    }

    private void ShowEmptyState()
    {
        if (emptyStatePanel != null)
            emptyStatePanel.SetActive(true);

        string message = currentTab switch
        {
            QuestTab.Available => "시작 가능한 퀘스트가 없습니다.",
            QuestTab.InProgress => "진행중인 퀘스트가 없습니다.",
            QuestTab.Completed => "완료한 퀘스트가 없습니다.",
            _ => ""
        };

        if (emptyStateText != null)
            emptyStateText.text = message;

        // 상세 정보 패널 숨기기
        selectedQuestId = null;
        if (questDetailPanel != null)
            questDetailPanel.SetActive(false);
    }

    // ==========================================
    // 퀘스트 상세 정보 표시
    // ==========================================
    private void ShowQuestDetail(string questId)
    {
        selectedQuestId = questId;
        QuestData quest = QuestManager.Instance.GetQuestData(questId);

        if (quest == null)
        {
            Debug.LogWarning($"[QuestUI] 퀘스트를 찾을 수 없음: {questId}");
            return;
        }

        if (questDetailPanel != null)
            questDetailPanel.SetActive(true);

        // 퀘스트 이름
        if (questNameText != null)
            questNameText.text = quest.questName;

        // 퀘스트 설명
        if (questDescriptionText != null)
        {
            // description에 \n이 있으면 실제 줄바꿈으로 변환
            string desc = quest.description.Replace("\\n", "\n");
            questDescriptionText.text = $"<b>퀘스트 내용</b>\n{desc}";
        }

        // 퀘스트 목표
        //  완료된 퀘스트(Rewarded)는 목표와 보상 숨김 
        bool isCompleted = quest.status == QuestStatus.Rewarded;

        if (questObjectivesText != null)
        {
            if (isCompleted)
            {
                questObjectivesText.text = "";
                //questObjectivesText.text = "<b>목표</b>\n<color=#FFD700>✓ 완료됨</color>";
            }
            else
            {
                string objectives = GetQuestObjectives(quest);
                questObjectivesText.text = $"<b>목표</b>\n{objectives}";
            }
        }
        if (questRewardsText != null)
        {
            if (isCompleted)
            {
                questRewardsText.text = "";
                //questRewardsText.text = "<b>보상</b>\n<color=#FFD700>✓ 받음</color>";
            }
            else
            {
                string rewards = GetQuestRewards(quest);
                questRewardsText.text = $"<b>보상</b>\n{rewards}";
            }
        }


        // 상태 아이콘
        if (questStatusImage != null)
        {
            questStatusImage.sprite = quest.status switch
            {
                QuestStatus.None or QuestStatus.Offered => availableSprite,
                //  Accepted 상태에서도 목표 완료 시 완료 이미지 표시 
                QuestStatus.Accepted => quest.IsCompleted() ? completedSprite : inProgressSprite,
                QuestStatus.Rewarded or QuestStatus.Completed => completedSprite,
                _ => null
            };
        }

        Debug.Log($"[QuestUI] 퀘스트 상세 정보 표시: {questId}");
    }

    private string GetObjectiveTypeText(QuestType type)
    {
        switch (type)
        {
            case QuestType.Dialogue:
                return "[대화]";
            case QuestType.Kill:
                return "[처치]";
            case QuestType.Collect:
                return "[수집]";
            case QuestType.Gather:
                return "[채집]";
            default:
                return "목표";
        }
    }

    private string GetQuestRewards(QuestData quest)
    {
        List<string> rewardTexts = new List<string>();

        // 경험치 보상
        if (quest.rewardExp > 0)
            rewardTexts.Add($"경험치 +{quest.rewardExp}");

        // 골드 보상
        if (quest.rewardGold > 0)
            rewardTexts.Add($"골드 +{quest.rewardGold}");

        // 아이템 보상
        if (quest.rewards != null && quest.rewards.Count > 0)
        {
            foreach (var reward in quest.rewards)
            {
                rewardTexts.Add($"{reward.itemId}:{reward.quantity}");
            }
        }

        return rewardTexts.Count > 0 ? string.Join("\n", rewardTexts) : "보상 없음";
    }


    // ==========================================
    // 외부에서 호출 가능한 갱신 메서드
    // ==========================================
    public void RefreshUI()
    {
        if (isOpen)
            RefreshQuestList();
    }

    public bool IsQuestUIOpen()
    {
        return isOpen;
    }

    // ==========================================
    // 퀘스트 트래커 (핀 기능)
    // ==========================================

    /// <summary>
    /// 핀 버튼 클릭 시 호출
    /// </summary>
    private void OnPinButtonClicked(string questId, Button pinButton)
    {
        if (QuestTrackerUIManager.Instance == null)
        {
            Debug.LogWarning("[QuestUI] QuestTrackerUIManager가 없습니다!");
            return;
        }

        bool isPinned = QuestTrackerUIManager.Instance.IsQuestTracked(questId);

        if (isPinned)
        {
            // 핀 해제
            QuestTrackerUIManager.Instance.RemoveTrackedQuest(questId);
        }
        else
        {
            // 핀 추가
            if (QuestTrackerUIManager.Instance.CanTrackMoreQuests())
            {
                QuestTrackerUIManager.Instance.AddTrackedQuest(questId);
            }
            else
            {
                int maxCount = QuestTrackerUIManager.Instance.maxTrackedQuests;
                Debug.LogWarning($"[QuestUI] 최대 {maxCount}개까지만 추적 가능합니다!");

                // 사용자에게 알림 (선택사항)
                if (FloatingNotificationManager.Instance != null)
                {
                    FloatingNotificationManager.Instance.ShowNotification($"최대 {maxCount}개까지만 추적 가능합니다!");
                }
                return;
            }
        }

        // 핀 버튼 스프라이트 업데이트
        Image pinButtonImage = pinButton.GetComponent<Image>();
        if (pinButtonImage != null)
        {
            bool newPinState = QuestTrackerUIManager.Instance.IsQuestTracked(questId);
            pinButtonImage.sprite = newPinState ? pinOnSprite : pinOffSprite;
        }

        Debug.Log($"[QuestUI] 핀 상태 변경: {questId} -> {!isPinned}");
    }

    // ==========================================
    // 퀘스트 매니저 이벤트 핸들러
    // ==========================================

    /// <summary>
    /// 퀘스트 상태 변경 시 호출 (QuestManager 이벤트)
    /// </summary>
    private void OnQuestStatusChanged(string questId, QuestStatus newStatus)
    {
        // 퀘스트 창이 열려있을 때만 자동 업데이트
        if (isOpen)
        {
            RefreshUI();
            Debug.Log($"[QuestUI] 퀘스트 상태 변경 감지: {questId} -> {newStatus}");
        }
    }

    /// <summary>
    /// 퀘스트 목표 업데이트 시 호출 (QuestManager 이벤트)
    /// </summary>
    private void OnQuestObjectiveUpdated(string questId)
    {
        // 퀘스트 창이 열려있을 때만 자동 업데이트
        if (isOpen)
        {
            RefreshUI();
            Debug.Log($"[QuestUI] 퀘스트 목표 업데이트 감지: {questId}");
        }
    }

    public void Close()
    {
        CloseQuestUI();
    }

    public GameObject GetUIPanel()
    {
        return questUIPanel;
    }
}