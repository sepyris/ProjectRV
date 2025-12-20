using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Definitions;

/// <summary>
/// 진행중인 퀘스트를 화면에 고정하여 표시하는 알림창 시스템
/// 최대 5~8개의 퀘스트를 추적할 수 있음
/// </summary>
public class QuestTrackerUIManager : MonoBehaviour
{
    public static QuestTrackerUIManager Instance { get; private set; }

    [Header("트래커 패널")]
    public GameObject trackerPanel;
    public Transform trackerListContainer;
    public GameObject trackerItemPrefab;

    [Header("설정")]
    [Tooltip("최대 추적 가능한 퀘스트 수")]
    public int maxTrackedQuests = 5;

    [Header("동적 크기 설정")]
    [Tooltip("trackerPanel에 헤더가 포함되어 있는지 여부\n- true: Panel = Header + Content (기본값)\n- false: Panel = Content만 (Header 별도)")]
    public bool includeHeaderInPanel = true;
    [Tooltip("헤더 기본 높이 (includeHeaderInPanel이 true일 때만 사용)")]
    public float headerHeight = 40f;
    [Tooltip("퀘스트 아이템 기본 높이 (퀘스트 이름 + 여백)")]
    public float questItemBaseHeight = 40f;
    [Tooltip("목표 1개당 추가 높이")]
    public float objectiveLineHeight = 20f;
    [Tooltip("퀘스트 아이템 간 간격")]
    public float itemSpacing = 5f;

    // 추적 중인 퀘스트 ID 목록 (순서 유지)
    private List<string> trackedQuestIds = new List<string>();

    // 퀘스트 ID -> 트래커 아이템 UI 매핑
    private Dictionary<string, GameObject> trackerItems = new Dictionary<string, GameObject>();

    // 퀘스트 ID -> 아이템 높이 매핑 (동적 크기 계산용)
    private Dictionary<string, float> itemHeights = new Dictionary<string, float>();

    // trackerPanel의 RectTransform (캐싱)
    private RectTransform trackerPanelRect;

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

        if (trackerPanel != null)
        {
            trackerPanel.SetActive(true);
            // RectTransform 캐싱
            trackerPanelRect = trackerPanel.GetComponent<RectTransform>();
            // 초기 크기 설정
            UpdatePanelSize();
        }
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
            Instance = null;

            // 이벤트 구독 해제
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestStatusChanged -= OnQuestStatusChanged;
                QuestManager.Instance.OnQuestObjectiveUpdated -= OnQuestObjectiveUpdated;
            }
        }
    }

    // ==========================================
    // 패널 크기 관리
    // ==========================================

    /// <summary>
    /// 퀘스트 데이터를 기반으로 아이템의 높이를 계산
    /// </summary>
    private float CalculateItemHeight(QuestData quest)
    {
        if (quest == null || quest.objectives == null)
            return questItemBaseHeight;

        // 기본 높이 + (목표 개수 * 목표당 높이)
        int objectiveCount = quest.objectives.Count;
        float height = questItemBaseHeight + (objectiveCount * objectiveLineHeight);

        return height;
    }

    /// <summary>
    /// trackerPanel의 높이를 현재 퀘스트 수에 맞게 동적으로 조절
    /// </summary>
    private void UpdatePanelSize()
    {
        if (trackerPanelRect == null)
            return;

        // 전체 높이 계산: 모든 퀘스트 아이템 높이의 합
        float totalItemsHeight = 0f;
        foreach (var questId in trackedQuestIds)
        {
            if (itemHeights.TryGetValue(questId, out float height))
            {
                totalItemsHeight += height;
            }
        }

        // 아이템 간 간격 추가 (n개 아이템이면 n-1개의 간격)
        if (trackedQuestIds.Count > 1)
        {
            totalItemsHeight += (trackedQuestIds.Count - 1) * itemSpacing;
        }

        // 최종 높이 계산
        float newHeight;
        if (includeHeaderInPanel)
        {
            // Panel에 헤더가 포함된 경우: 헤더 + 아이템들의 합
            newHeight = headerHeight + totalItemsHeight;
        }
        else
        {
            // Panel이 Content만인 경우: 아이템들의 합만
            newHeight = totalItemsHeight;
        }

        // 최소 높이 보장 (비어있을 때)
        if (includeHeaderInPanel && trackedQuestIds.Count == 0)
        {
            newHeight = headerHeight; // 헤더만 보이도록
        }
        else if (!includeHeaderInPanel && trackedQuestIds.Count == 0)
        {
            newHeight = 0f; // 완전히 숨김
        }

        //  Anchor 설정에 상관없이 크기를 직접 설정 
        trackerPanelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);

        Debug.Log($"[QuestTracker] 패널 크기 업데이트: {newHeight}px (퀘스트 {trackedQuestIds.Count}개, 총 아이템 높이: {totalItemsHeight}px)");
    }

    // ==========================================
    // 퀘스트 추적 추가/제거
    // ==========================================

    /// <summary>
    /// 퀘스트를 트래커에 추가
    /// </summary>
    public bool AddTrackedQuest(string questId)
    {
        // 이미 추적 중인지 확인
        if (trackedQuestIds.Contains(questId))
        {
            Debug.LogWarning($"[QuestTracker] 이미 추적 중인 퀘스트: {questId}");
            return false;
        }

        // 최대 개수 확인
        if (trackedQuestIds.Count >= maxTrackedQuests)
        {
            Debug.LogWarning($"[QuestTracker] 최대 {maxTrackedQuests}개까지만 추적 가능합니다.");
            return false;
        }

        // 퀘스트 데이터 확인
        QuestData quest = QuestManager.Instance?.GetQuestData(questId);
        if (quest == null)
        {
            Debug.LogWarning($"[QuestTracker] 퀘스트를 찾을 수 없음: {questId}");
            return false;
        }

        // InProgress 상태인지 확인
        if (quest.status != QuestStatus.Accepted && quest.status != QuestStatus.Completed)
        {
            Debug.LogWarning($"[QuestTracker] 진행중인 퀘스트만 추적 가능: {questId}");
            return false;
        }

        // 추적 목록에 추가
        trackedQuestIds.Add(questId);
        CreateTrackerItem(quest);

        // 패널 크기 업데이트
        UpdatePanelSize();

        Debug.Log($"[QuestTracker] 퀘스트 추적 시작: {questId}");
        return true;
    }

    /// <summary>
    /// 퀘스트를 트래커에서 제거
    /// </summary>
    public bool RemoveTrackedQuest(string questId)
    {
        if (!trackedQuestIds.Contains(questId))
        {
            Debug.LogWarning($"[QuestTracker] 추적 중이지 않은 퀘스트: {questId}");
            return false;
        }

        // 추적 목록에서 제거
        trackedQuestIds.Remove(questId);

        // UI 아이템 제거
        if (trackerItems.TryGetValue(questId, out GameObject item))
        {
            Destroy(item);
            trackerItems.Remove(questId);
        }

        // 높이 정보 제거
        itemHeights.Remove(questId);

        // 패널 크기 업데이트
        UpdatePanelSize();

        Debug.Log($"[QuestTracker] 퀘스트 추적 중지: {questId}");
        return true;
    }

    /// <summary>
    /// 퀘스트가 추적 중인지 확인
    /// </summary>
    public bool IsQuestTracked(string questId)
    {
        return trackedQuestIds.Contains(questId);
    }

    /// <summary>
    /// 모든 추적 중인 퀘스트 제거
    /// </summary>
    public void ClearAllTrackedQuests()
    {
        foreach (var item in trackerItems.Values)
        {
            if (item != null)
                Destroy(item);
        }

        trackedQuestIds.Clear();
        trackerItems.Clear();
        itemHeights.Clear(); // 높이 정보도 클리어

        // 패널 크기 업데이트
        UpdatePanelSize();

        Debug.Log("[QuestTracker] 모든 추적 퀘스트 제거됨");
    }

    // ==========================================
    // UI 생성 및 업데이트
    // ==========================================

    /// <summary>
    /// 트래커 아이템 UI 생성
    /// </summary>
    private void CreateTrackerItem(QuestData quest)
    {
        if (trackerItemPrefab == null || trackerListContainer == null)
        {
            Debug.LogError("[QuestTracker] Prefab 또는 Container가 없습니다!");
            return;
        }

        // 이미 존재하는 아이템이면 제거
        if (trackerItems.ContainsKey(quest.questId))
        {
            Destroy(trackerItems[quest.questId]);
            trackerItems.Remove(quest.questId);
        }

        // 새 아이템 생성
        GameObject itemObj = Instantiate(trackerItemPrefab, trackerListContainer);
        trackerItems[quest.questId] = itemObj;

        //  아이템 높이 계산 및 저장 
        float itemHeight = CalculateItemHeight(quest);
        itemHeights[quest.questId] = itemHeight;

        //  프리팹의 RectTransform 크기 조절 
        RectTransform itemRect = itemObj.GetComponent<RectTransform>();
        if (itemRect != null)
        {
            itemRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemHeight);
        }

        // UI 요소 찾기 - 이름 대신 컴포넌트 타입과 순서로 찾기
        TextMeshProUGUI[] allTexts = itemObj.GetComponentsInChildren<TextMeshProUGUI>(true);
        Button removeButton = itemObj.GetComponentInChildren<Button>(true);

        // 첫 번째 TextMeshProUGUI는 퀘스트 이름, 두 번째는 목표
        TextMeshProUGUI nameText = allTexts.Length > 0 ? allTexts[0] : null;
        TextMeshProUGUI objectivesText = allTexts.Length > 1 ? allTexts[1] : null;

        // 퀘스트 이름 설정
        if (nameText != null)
        {
            nameText.text = quest.questName;
        }

        // 목표 설정
        if (objectivesText != null)
        {
            objectivesText.text = GetObjectivesText(quest);
        }

        // 제거 버튼 설정
        if (removeButton != null)
        {
            string capturedQuestId = quest.questId;
            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(() => OnRemoveButtonClicked(capturedQuestId));
        }

        Debug.Log($"[QuestTracker] 트래커 아이템 생성: {quest.questId} (높이: {itemHeight}px)");
    }

    /// <summary>
    /// 특정 퀘스트의 트래커 UI 업데이트
    /// </summary>
    private void UpdateTrackerItem(string questId)
    {
        if (!trackerItems.TryGetValue(questId, out GameObject itemObj) || itemObj == null)
            return;

        QuestData quest = QuestManager.Instance?.GetQuestData(questId);
        if (quest == null)
            return;

        //  높이 재계산 (목표 완료 상태 변경 시) 
        float newHeight = CalculateItemHeight(quest);
        if (itemHeights.ContainsKey(questId))
        {
            float oldHeight = itemHeights[questId];
            if (Mathf.Abs(oldHeight - newHeight) > 0.1f) // 높이가 변경되었을 때만
            {
                itemHeights[questId] = newHeight;

                // 프리팹 크기 조절
                RectTransform itemRect = itemObj.GetComponent<RectTransform>();
                if (itemRect != null)
                {
                    itemRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);
                }

                // 전체 패널 크기도 업데이트
                UpdatePanelSize();
            }
        }

        // UI 요소 찾기 - 두 번째 TextMeshProUGUI가 목표
        TextMeshProUGUI[] allTexts = itemObj.GetComponentsInChildren<TextMeshProUGUI>(true);
        TextMeshProUGUI objectivesText = allTexts.Length > 1 ? allTexts[1] : null;

        // 목표 업데이트
        if (objectivesText != null)
        {
            objectivesText.text = GetObjectivesText(quest);
        }

        Debug.Log($"[QuestTracker] 트래커 아이템 업데이트: {questId} (높이: {newHeight}px)");
    }

    /// <summary>
    /// 퀘스트 목표 텍스트 생성
    /// </summary>
    private string GetObjectivesText(QuestData quest)
    {
        if (quest.objectives == null || quest.objectives.Count == 0)
            return "목표 없음";

        List<string> objectiveTexts = new List<string>();

        foreach (var obj in quest.objectives)
        {
            string checkmark = obj.IsCompleted ? "✓" : "•";
            string typeText = GetObjectiveTypeText(obj.type);
            string progress = $" ({obj.currentCount}/{obj.requiredCount})";

            // 완료된 목표는 회색으로 표시
            if (obj.IsCompleted)
            {
                objectiveTexts.Add($"<color=#888888>{checkmark} {typeText} {obj.targetId}{progress}</color>");
            }
            else
            {
                objectiveTexts.Add($"{checkmark} {typeText} {obj.targetId}{progress}");
            }
        }

        return string.Join("\n", objectiveTexts);
    }

    /// <summary>
    /// 목표 타입을 한글로 변환
    /// </summary>
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

    // ==========================================
    // 이벤트 핸들러
    // ==========================================

    /// <summary>
    /// 제거 버튼 클릭 시 호출
    /// </summary>
    private void OnRemoveButtonClicked(string questId)
    {
        RemoveTrackedQuest(questId);

        // QuestUIManager에 핀 상태 업데이트 알림
        if (QuestUIManager.Instance != null)
        {
            QuestUIManager.Instance.RefreshUI();
        }
    }

    /// <summary>
    /// 퀘스트 상태 변경 시 호출
    /// </summary>
    private void OnQuestStatusChanged(string questId, QuestStatus newStatus)
    {
        //  QuestUIManager가 열려있으면 항상 업데이트 (퀘스트 추적 여부 무관) 
        if (QuestUIManager.Instance != null && QuestUIManager.Instance.IsQuestUIOpen())
        {
            QuestUIManager.Instance.RefreshUI();
        }

        // 추적 중인 퀘스트가 아니면 트래커 업데이트는 스킵
        if (!trackedQuestIds.Contains(questId))
            return;

        // Rewarded 상태가 되면 트래커에서 제거
        if (newStatus == QuestStatus.Rewarded)
        {
            RemoveTrackedQuest(questId);
            Debug.Log($"[QuestTracker] 퀘스트 완료로 인해 제거됨: {questId}");
        }
        // Accepted나 Completed 상태가 아니면 제거
        else if (newStatus != QuestStatus.Accepted && newStatus != QuestStatus.Completed)
        {
            RemoveTrackedQuest(questId);
            Debug.Log($"[QuestTracker] 진행중이 아닌 상태로 변경되어 제거됨: {questId}");
        }
        else
        {
            // 상태 업데이트 (목표 완료 등)
            UpdateTrackerItem(questId);
        }
    }

    /// <summary>
    /// 퀘스트 목표 업데이트 시 호출
    /// </summary>
    private void OnQuestObjectiveUpdated(string questId)
    {
        //  QuestUIManager가 열려있으면 항상 업데이트 
        if (QuestUIManager.Instance != null && QuestUIManager.Instance.IsQuestUIOpen())
        {
            QuestUIManager.Instance.RefreshUI();
        }

        // 추적 중인 퀘스트가 아니면 트래커 업데이트는 스킵
        if (!trackedQuestIds.Contains(questId))
            return;

        UpdateTrackerItem(questId);
    }

    // ==========================================
    // 공개 유틸리티 메서드
    // ==========================================

    /// <summary>
    /// 현재 추적 중인 퀘스트 수
    /// </summary>
    public int GetTrackedQuestCount()
    {
        return trackedQuestIds.Count;
    }

    /// <summary>
    /// 추적 가능 여부 확인
    /// </summary>
    public bool CanTrackMoreQuests()
    {
        return trackedQuestIds.Count < maxTrackedQuests;
    }

    /// <summary>
    /// 추적 중인 모든 퀘스트 ID 가져오기
    /// </summary>
    public List<string> GetTrackedQuestIds()
    {
        return new List<string>(trackedQuestIds);
    }
}