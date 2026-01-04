using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// QuestManager (Collect와 Gather 구분)
/// - Collect: 현재 인벤토리 포함, 이미 가지고 있으면 바로 완료
/// - Gather: 퀘스트 수락 후 새로 획득한 아이템만 카운트
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    // 모든 퀘스트을 id -> QuestData로 보관
    private Dictionary<string, QuestData> questDictionary = new Dictionary<string, QuestData>();

    // ===== 이벤트 추가 =====
    /// <summary>
    /// 퀘스트 상태가 변경될 때 발생하는 이벤트
    /// </summary>
    public event System.Action<string, QuestStatus> OnQuestStatusChanged;

    /// <summary>
    /// 퀘스트 목표가 업데이트될 때 발생하는 이벤트
    /// </summary>
    public event System.Action<string> OnQuestObjectiveUpdated;


    // =======================

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
    }

    public void RegisterQuest(QuestData quest)
    {
        if (quest == null || string.IsNullOrEmpty(quest.questId)) return;

        if (!questDictionary.ContainsKey(quest.questId))
        {
            questDictionary.Add(quest.questId, quest);
            Debug.Log($"[QuestManager] 퀘스트 등록: {quest.questId}");
        }
        else
        {
            Debug.LogWarning($"[QuestManager] 이미 등록된 퀘스트입니다: {quest.questId}");
        }
    }

    public QuestData GetQuestData(string questId)
    {
        questDictionary.TryGetValue(questId, out QuestData quest);
        return quest;
    }

    public List<QuestData> GetAllQuests()
    {
        return new List<QuestData>(questDictionary.Values);
    }

    public QuestStatus GetQuestStatus(string questId)
    {
        var q = GetQuestData(questId);
        return q != null ? q.status : QuestStatus.None;
    }

    public void OfferQuest(string questId)
    {
        var q = GetQuestData(questId);
        if (q == null) { Debug.LogWarning($"[QuestManager] OfferQuest: 퀘스트 없음 {questId}"); return; }

        if (q.status == QuestStatus.None)
        {
            q.status = QuestStatus.Offered;
            Debug.Log($"[QuestManager] Quest Offered: {questId}");

            // 이벤트 발생
            OnQuestStatusChanged?.Invoke(questId, q.status);
        }
    }

    public void AcceptQuest(string questId)
    {
        var q = GetQuestData(questId);
        if (q == null) { Debug.LogWarning($"[QuestManager] AcceptQuest: 퀘스트 없음 {questId}"); return; }

        if (q.status == QuestStatus.Offered || q.status == QuestStatus.None)
        {
            q.status = QuestStatus.Accepted;

            //퀘스트 수락 보상 지급
            foreach (var rewardPart in q.preAcceptReward.Split(';'))
            {
                var parts = rewardPart.Split(':');
                // item:itemid:갯수; 형식
                if (parts.Length == 3 && parts[0].Trim().ToLower() == "item")
                {
                    string itemId = parts[1].Trim();
                    int quantity = int.Parse(parts[2].Trim());
                    if (InventoryManager.Instance != null)
                    {
                        InventoryManager.Instance.AddItem(itemId, quantity);
                        FloatingItemManager.Instance?.ShowItemAcquired(ItemDataManager.Instance.GetItemData(itemId), quantity);
                        Debug.Log($"[QuestManager] 수락 보상 지급: {itemId} x{quantity}");
                    }
                }
            }

            //  Collect 타입 목표는 현재 인벤토리 아이템으로 초기화 
            InitializeCollectObjectives(q);

            Debug.Log($"[QuestManager] Quest Accepted: {questId}");
            TutorialManager.Instance.ShowNarrationUi(questId);
            // 이벤트 발생
            OnQuestStatusChanged?.Invoke(questId, q.status);
        }
    }

    /// <summary>
    /// Collect 타입 목표는 퀘스트 수락 시 현재 인벤토리를 확인하여 초기값 설정
    /// </summary>
    private void InitializeCollectObjectives(QuestData quest)
    {
        if (InventoryManager.Instance == null) return;

        foreach (var obj in quest.objectives)
        {
            if (obj.type == QuestType.Collect)
            {
                // 현재 인벤토리에서 해당 아이템 개수 확인
                int currentAmount = InventoryManager.Instance.GetItemQuantity(obj.targetId);

                // 필요한 개수만큼만 카운트 (초과분은 무시)
                obj.currentCount = Mathf.Min(currentAmount, obj.requiredCount);

                Debug.Log($"[QuestManager] Collect 초기화: {obj.targetId} {obj.currentCount}/{obj.requiredCount} (인벤토리에 {currentAmount}개 보유)");
            }
            // Gather 타입은 0부터 시작 (기본값)
        }

        //  초기화 후 완료 조건 체크 (상태는 Accepted 유지, 내부적으로만 완료 여부 확인) 
        CheckQuestCompletion(quest);
    }

    /// <summary>
    /// 퀘스트 완료 조건 체크 (목표는 완료되지만 상태는 Accepted 유지)
    /// </summary>
    private void CheckQuestCompletion(QuestData quest)
    {
        if (quest.status != QuestStatus.Accepted) return;

        if (quest.IsCompleted())
        {
            Debug.Log($"[QuestManager] 퀘스트 목표 달성: {quest.questId} (NPC에게 보고 필요)");
            //  상태는 Accepted 유지 - UI에서 "완료 가능" 표시만 함

            //  모든 NPC의 상태 아이콘 업데이트 
            UpdateAllNPCIcons();
        }
    }
    public void ConfirmedQuestCompletion(string questId)
    {
        var quest = GetQuestData(questId);
        if (quest.status != QuestStatus.Accepted) return;

        if (quest.IsCompleted())
        {
            Debug.Log($"[QuestManager] 퀘스트 완료 확정: {quest.questId}");
            quest.status = QuestStatus.Completed;

            // 이벤트 발생
            OnQuestStatusChanged?.Invoke(questId, quest.status);
        }
    }

    public void FinalizeQuest(string questId)
    {
        var q = GetQuestData(questId);
        if (q == null) { Debug.LogWarning($"[QuestManager] FinalizeQuest: 퀘스트 없음 {questId}"); return; }

        if (q.status == QuestStatus.Completed)
        {
            //  수집/채집 퀘스트 아이템 소모 
            ConsumeQuestItems(q);

            // 보상 지급
            if (InventoryManager.Instance != null)
            {
                foreach (var r in q.rewards)
                {
                    InventoryManager.Instance.AddItem(r.itemId, r.quantity);
                    FloatingItemManager.Instance?.ShowItemAcquired(ItemDataManager.Instance.GetItemData(r.itemId), r.quantity);
                    Debug.Log($"[QuestManager] 보상 지급: {r.itemId} x{r.quantity}");
                }
            }
            // 경험치, 골드 지급 (실제 시스템과 연동)
            if (q.rewardExp > 0)
            {
                Debug.Log($"[QuestManager] 경험치 보상: {q.rewardExp}");
                ExperienceManager.Instance?.AddExp(q.rewardExp);
                FloatingItemManager.Instance?.ShowItemMessage("EXP", q.rewardExp);
            }

            if (q.rewardGold > 0)
            {
                Debug.Log($"[QuestManager] 골드 보상: {q.rewardGold}");
                ExperienceManager.Instance?.AddGold(q.rewardGold);
                FloatingItemManager.Instance?.ShowItemMessage("Gold", q.rewardGold);
            }

            q.status = QuestStatus.Rewarded;
            Debug.Log($"[QuestManager] Quest Rewarded: {questId}");
            TutorialManager.Instance.CloseNarrationUi();
            // 이벤트 발생
            OnQuestStatusChanged?.Invoke(questId, q.status);
        }
        else
        {
            Debug.Log($"[QuestManager] FinalizeQuest 호출되었지만 상태가 Completed 아님: {questId} 상태={q.status}");
        }
    }
    /// <summary>
    /// 지정된 퀘스트의 특정 타겟 목표를 업데이트한다.
    /// </summary>
    public void UpdateObjective(string questId, string targetId, int count)
    {
        var q = GetQuestData(questId);
        if (q == null) return;
        if (q.status != QuestStatus.Accepted) return;

        bool anyChanged = false;
        foreach (var obj in q.objectives)
        {
            if (obj.targetId == targetId && obj.currentCount < obj.requiredCount)
            {
                obj.currentCount = Mathf.Min(obj.currentCount + count, obj.requiredCount);
                anyChanged = true;
                Debug.Log($"[QuestManager] [{questId}] 목표 업데이트: {obj.targetId} {obj.currentCount}/{obj.requiredCount}");
            }
        }

        if (anyChanged)
        {
            // 이벤트 발생
            OnQuestObjectiveUpdated?.Invoke(questId);

            CheckQuestCompletion(q);
        }
    }

    /// <summary>
    /// 몬스터 처치 업데이트
    /// </summary>
    public void UpdateKillProgress(string monsterId)
    {
        if (string.IsNullOrEmpty(monsterId)) return;

        foreach (var q in questDictionary.Values)
        {
            if (q.status != QuestStatus.Accepted) continue;

            foreach (var obj in q.objectives)
            {
                if (obj.type == QuestType.Kill && obj.targetId == monsterId && obj.currentCount < obj.requiredCount)
                {
                    UpdateObjective(q.questId, monsterId, 1);
                }
            }
        }
    }

    /// <summary>
    /// 아이템 획득 업데이트 (Collect와 Gather 모두 처리)
    /// </summary>
    public void UpdateItemProgress(string itemId, int amount = 1)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return;

        foreach (var q in questDictionary.Values)
        {
            if (q.status != QuestStatus.Accepted) continue;

            foreach (var obj in q.objectives)
            {
                // Collect: 현재 인벤토리 개수로 업데이트
                if (obj.type == QuestType.Collect && obj.targetId == itemId)
                {
                    UpdateCollectProgress(q.questId, itemId);
                }
                // Gather: 획득한 만큼만 증가
                else if (obj.type == QuestType.Gather && obj.targetId == itemId && obj.currentCount < obj.requiredCount)
                {
                    UpdateObjective(q.questId, itemId, amount);
                }
            }
        }
    }

    /// <summary>
    /// Collect 타입 목표 업데이트 - 현재 인벤토리 개수로 동기화
    /// </summary>
    private void UpdateCollectProgress(string questId, string itemId)
    {
        if (InventoryManager.Instance == null) return;

        var q = GetQuestData(questId);
        if (q == null || q.status != QuestStatus.Accepted) return;

        bool anyChanged = false;

        foreach (var obj in q.objectives)
        {
            if (obj.type == QuestType.Collect && obj.targetId == itemId)
            {
                // 현재 인벤토리 개수 확인
                int currentAmount = InventoryManager.Instance.GetItemQuantity(itemId);
                int newCount = Mathf.Min(currentAmount, obj.requiredCount);

                if (newCount != obj.currentCount)
                {
                    obj.currentCount = newCount;
                    anyChanged = true;
                    Debug.Log($"[QuestManager] [{questId}] Collect 목표 업데이트: {itemId} {obj.currentCount}/{obj.requiredCount}");
                }
            }
        }

        if (anyChanged)
        {
            // 이벤트 발생
            OnQuestObjectiveUpdated?.Invoke(questId);

            CheckQuestCompletion(q);
        }
    }

    /// <summary>
    /// NPC 대화로 인한 진행 업데이트
    /// </summary>
    public void UpdateDialogueProgress(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return;

        foreach (var q in questDictionary.Values)
        {
            if (q.status != QuestStatus.Accepted) continue;

            foreach (var obj in q.objectives)
            {
                if (obj.type == QuestType.Dialogue && obj.targetId == npcId && obj.currentCount < obj.requiredCount)
                {
                    UpdateObjective(q.questId, npcId, 1);
                }
            }
        }
    }

    /// <summary>
    /// 모든 진행중인 퀘스트의 Collect 목표를 인벤토리 상태로 갱신
    /// (아이템을 버렸을 때 호출)
    /// </summary>
    public void RefreshAllCollectObjectives()
    {
        if (InventoryManager.Instance == null) return;

        foreach (var q in questDictionary.Values)
        {
            if (q.status != QuestStatus.Accepted) continue;

            foreach (var obj in q.objectives)
            {
                if (obj.type == QuestType.Collect)
                {
                    int currentAmount = InventoryManager.Instance.GetItemQuantity(obj.targetId);
                    obj.currentCount = Mathf.Min(currentAmount, obj.requiredCount);
                }
            }
        }
    }

    /// <summary>
    /// 모든 퀘스트 초기화
    /// </summary>
    public void ResetAllQuests()
    {
        foreach (var quest in questDictionary.Values)
        {
            quest.status = QuestStatus.None;
            foreach (var obj in quest.objectives)
            {
                obj.currentCount = 0;
            }
        }
        Debug.Log("[QuestManager] 모든 퀘스트 초기화");
    }

    /// <summary>
    /// 퀘스트 데이터를 저장 형식으로 변환
    /// </summary>
    public AllQuestsSaveData ToSaveData()
    {
        AllQuestsSaveData saveData = new AllQuestsSaveData();

        foreach (var quest in questDictionary.Values)
        {
            // None 상태는 저장하지 않음
            if (quest.status == QuestStatus.None)
                continue;

            QuestSaveData questSave = new QuestSaveData
            {
                questId = quest.questId,
                status = quest.status,
                objectives = new List<QuestObjectiveSaveData>()
            };

            foreach (var obj in quest.objectives)
            {
                questSave.objectives.Add(new QuestObjectiveSaveData
                {
                    currentCount = obj.currentCount
                });
            }

            saveData.quests.Add(questSave);
        }

        Debug.Log($"[QuestManager] 저장: {saveData.quests.Count}개 퀘스트");
        return saveData;
    }

    /// <summary>
    /// 저장된 퀘스트 데이터 로드
    /// </summary>
    public void LoadFromData(AllQuestsSaveData saveData)
    {
        if (saveData == null || saveData.quests == null)
        {
            Debug.Log("[QuestManager] 저장된 퀘스트 데이터가 없습니다.");
            return;
        }

        int loadedCount = 0;
        foreach (var questSave in saveData.quests)
        {
            if (!questDictionary.TryGetValue(questSave.questId, out QuestData quest))
            {
                Debug.LogWarning($"[QuestManager] 퀘스트를 찾을 수 없음: {questSave.questId}");
                continue;
            }

            quest.status = questSave.status;

            for (int i = 0; i < quest.objectives.Count && i < questSave.objectives.Count; i++)
            {
                quest.objectives[i].currentCount = questSave.objectives[i].currentCount;
            }

            loadedCount++;
            Debug.Log($"[QuestManager] 퀘스트 로드: {quest.questId} ({quest.status})");
        }

        Debug.Log($"[QuestManager] 로드 완료: {loadedCount}개 퀘스트");
    }

    /// <summary>
    /// 모든 NPC의 상태 아이콘을 업데이트합니다
    /// </summary>
    private void UpdateAllNPCIcons()
    {
        NPCController[] allNPCs = FindObjectsOfType<NPCController>();
        foreach (NPCController npc in allNPCs)
        {
            npc.UpdateStatusIcon();
        }
    }

    /// <summary>
    /// 수집/채집 퀘스트의 아이템을 인벤토리에서 소모합니다
    /// </summary>
    private void ConsumeQuestItems(QuestData quest)
    {
        if (InventoryManager.Instance == null) return;

        foreach (var objective in quest.objectives)
        {
            // Collect와 Gather 타입만 아이템 소모
            if (objective.type == QuestType.Collect || objective.type == QuestType.Gather)
            {
                string itemId = objective.targetId;
                int requiredCount = objective.requiredCount;

                // 인벤토리에서 아이템 제거
                bool removed = InventoryManager.Instance.RemoveItem(itemId, requiredCount);

                if (removed)
                {
                    Debug.Log($"[QuestManager] 퀘스트 아이템 소모: {itemId} x{requiredCount}");
                }
                else
                {
                    Debug.LogWarning($"[QuestManager] 아이템 소모 실패: {itemId} x{requiredCount} (인벤토리에 부족)");
                }
            }
        }
    }
}