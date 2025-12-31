using Definitions;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;


/// NPC 컨트롤러 - 플레이어 중심 상호작용
/// 여러 퀘스트 지원 + 선행 조건 체크

public class NPCController : MonoBehaviour
{
    public enum NPCType { Dialogue, Quest, Shop }

    [Header("NPC 기본 설정")]
    public NPCType npcType = NPCType.Dialogue;
    public string npcId;

    [Header("퀘스트 NPC - 여러 퀘스트 할당 가능")]
    [Tooltip("이 NPC가 관리하는 퀘스트 id 목록")]
    public List<string> questIds = new List<string>();

    [Header("NPC 전용 UI - 2D 스프라이트 오브젝트")]
    [Tooltip("NPC 머리 위 상태 아이콘 GameObject")]
    [SerializeField] private GameObject statusIconObject;
    [Tooltip("상태 아이콘의 SpriteRenderer")]
    [SerializeField] private SpriteRenderer statusIconRenderer;

    [Tooltip("NPC 옆 상호작용 프롬프트 GameObject (E키 표시)")]
    [SerializeField] private GameObject interactPromptObject;

    [Header("상태 아이콘 스프라이트")]
    [SerializeField] private Sprite questOfferSprite;       // 노란색 ! (새 퀘스트)
    [SerializeField] private Sprite questProgressSprite;    // 회색 ! (진행중)
    [SerializeField] private Sprite questCompleteSprite;    // 노란색 ? (완료 가능)
    [SerializeField] private Sprite dialogueOnlySprite;     // 말풍선
    [SerializeField] private Sprite shopOnlySprite;         // 돈/쇼핑백
    [SerializeField] private Sprite nullSprite;             // 투명/공백

    [Header("NPC UI")]
    [SerializeField] private TextMeshProUGUI npcNameText; // npc 이름 텍스트 (TMP)

    void Start()
    {
        // 상태 아이콘은 항상 활성화 상태 유지
        if (statusIconObject != null)
        {
            statusIconObject.SetActive(true);
        }

        // 상태 아이콘 스프라이트 초기 업데이트
        UpdateStatusIcon();

        // 인터렉트 프롬프트는 처음에 비활성화
        if (interactPromptObject != null)
        {
            interactPromptObject.SetActive(false);
        }

        // 이름 초기화 및 위치 설정
        UpdateNameDisplay();
    }

    
    /// NPC 상태 아이콘 업데이트 (여러 퀘스트 상태를 종합)
    
    public void UpdateStatusIcon()
    {
        if (statusIconRenderer == null) return;

        Sprite targetSprite = null;
        statusIconRenderer.enabled = false;

        switch (npcType)
        {
            case NPCType.Quest:
                targetSprite = GetQuestIconSprite();
                break;

            case NPCType.Dialogue:
                targetSprite = dialogueOnlySprite;
                break;

            case NPCType.Shop:
                targetSprite = shopOnlySprite;
                break;
        }
        // 스프라이트 적용
        if (targetSprite != null)
        {
            statusIconRenderer.sprite = targetSprite;
            statusIconRenderer.enabled = true;
        }
        else
        {
            statusIconRenderer.enabled = false;
        }
    }

    private void UpdateNameDisplay()
    {
        if (npcNameText != null)
        {
            npcNameText.text = "";
            if (!string.IsNullOrEmpty(npcId))
            {
                Npcs npcData = NPCInfoManager.Instance.GetNPCInfo(npcId);
                if (npcData != null)
                {
                    npcNameText.text += npcData.npcName;
                    if (npcData.npcTitle != "")
                    {
                        npcNameText.text += "\n";
                        npcNameText.text += "[" + npcData.npcTitle + "]";
                    }
                }
            }
        }
    }

    
    /// 여러 퀘스트 중 가장 우선순위 높은 아이콘 반환
    /// 우선순위: 완료 가능 > 진행 중 > 수락 가능
    ///  선행 조건을 만족하는 퀘스트만 표시 
    
    private Sprite GetQuestIconSprite()
    {
        if (QuestManager.Instance == null || questIds.Count == 0)
        {
            return dialogueOnlySprite;
        }

        bool hasCompletable = false;  // 완료 가능 (Completed 상태)
        bool hasOffered = false;      // 수락 가능 (None/Offered 상태)
        bool hasAccepted = false;     // 진행 중 (Accepted 상태)

        foreach (string questId in questIds)
        {
            if (string.IsNullOrEmpty(questId)) continue;

            QuestData quest = QuestManager.Instance.GetQuestData(questId);
            if (quest == null) continue;

            QuestStatus status = quest.status;

            switch (status)
            {
                case QuestStatus.Completed:
                    //  Completed = 목표 달성, NPC에게 보고 필요 → 완료 가능 아이콘 
                    if (IsQuestObjectiveForThisNPC(quest))
                    {
                        hasCompletable = true;
                    }
                    break;

                case QuestStatus.None:
                case QuestStatus.Offered:
                    //  선행 조건을 만족하는 경우만 표시 
                    if (quest.CanAccept())
                    {
                        hasOffered = true;
                    }
                    break;

                case QuestStatus.Accepted:
                    //  진행중인 퀘스트 체크 - 목표 완료 여부도 확인 
                    if (quest.IsCompleted())
                    {
                        // 목표 달성했으면 완료 가능 아이콘 표시
                        hasCompletable = true;
                    }
                    else
                    {
                        hasAccepted = true;
                    }
                    break;

                case QuestStatus.Rewarded:
                    //  Rewarded = 보상 받음, 완료된 퀘스트 → 표시 안함 
                    break;
            }
        }

        // 우선순위대로 반환
        if (hasCompletable) return questCompleteSprite;  // 노란색 ? (완료 가능)
        if (hasOffered) return questOfferSprite;         // 노란색 ! (새 퀘스트)
        if (hasAccepted) return questProgressSprite;     // 회색 ! (진행중)

        return dialogueOnlySprite;
    }

    
    /// 이 NPC가 퀘스트 목표에 포함되어 있는지 체크
    
    private bool IsQuestObjectiveForThisNPC(QuestData quest)
    {
        if (quest == null || quest.objectives == null) return false;

        foreach (var objective in quest.objectives)
        {
            if (objective.type == QuestType.Dialogue && objective.targetId == npcId)
            {
                return true;
            }
        }
        return false;
    }

    
    /// PlayerInteraction에서 호출: 프롬프트 표시
    
    public void ShowPrompt()
    {
        if (interactPromptObject != null)
        {
            interactPromptObject.SetActive(true);
        }
        UpdateStatusIcon();
    }

    
    /// PlayerInteraction에서 호출: 프롬프트 숨김
    
    public void HidePrompt()
    {
        if (interactPromptObject != null)
        {
            interactPromptObject.SetActive(false);
        }
    }

    
    /// PlayerInteraction에서 호출: NPC와 상호작용 시작
    
    public void Interact()
    {
        // 퀘스트 NPC인 경우, 대화 시작 전에 None 상태 퀘스트를 Offered로 전환
        //  단, 선행 조건을 만족하는 퀘스트만 Offered로 전환 
        if (npcType == NPCType.Quest && QuestManager.Instance != null)
        {
            foreach (string questId in questIds)
            {
                if (string.IsNullOrEmpty(questId)) continue;

                QuestData quest = QuestManager.Instance.GetQuestData(questId);
                if (quest == null) continue;

                // None 상태이고 선행 조건을 만족하는 경우에만 Offered로 전환
                if (quest.status == QuestStatus.None && quest.CanAccept())
                {
                    QuestManager.Instance.OfferQuest(questId);
                }
            }
        }
        else if (npcType == NPCType.Shop)
        {
            // 상점 NPC인 경우 상점 UI 열기
            ShopUIManager.Instance?.OpenShop(questIds.First());
        }

        // 인터렉트 프롬프트 숨김
        HidePrompt();

        if (npcType != NPCType.Shop)
        {
            // DialogueUIManager를 통해 상호작용 시작
            if (DialogueUIManager.Instance != null)
            {
                DialogueUIManager.Instance.OpenInteraction(this);
            }
            else
            {
                Debug.LogError("[NPC] DialogueUIManager.Instance가 null입니다.");
            }
        }
    }

    
    /// 대화 종료 시 DialogueUIManager에서 호출
    
    public void OnInteractionClosed()
    {
        // 상태 아이콘 업데이트
        UpdateStatusIcon();
    }

    
    /// 현재 활성화된 퀘스트 목록 반환
    ///  선행 조건을 만족하는 퀘스트만 포함 
    ///  Rewarded 상태는 제외 
    
    public List<string> GetActiveQuests()
    {
        List<string> activeQuests = new List<string>();

        if (QuestManager.Instance == null) return activeQuests;

        foreach (string questId in questIds)
        {
            if (string.IsNullOrEmpty(questId)) continue;

            QuestData quest = QuestManager.Instance.GetQuestData(questId);
            if (quest == null) continue;

            QuestStatus status = quest.status;

            switch (status)
            {
                case QuestStatus.None:
                case QuestStatus.Offered:
                    // 선행 조건을 만족하는 경우만 활성 퀘스트로 표시
                    if (quest.CanAccept())
                    {
                        activeQuests.Add(questId);
                    }
                    break;

                case QuestStatus.Accepted:
                    // 진행 중인 퀘스트는 항상 표시
                    activeQuests.Add(questId);
                    break;

                case QuestStatus.Completed:
                    // 완료 가능한 퀘스트 - 이 NPC가 목표에 포함되어 있는 경우만 표시
                    if (IsQuestObjectiveForThisNPC(quest))
                    {
                        activeQuests.Add(questId);
                    }
                    break;

                case QuestStatus.Rewarded:
                    //  Rewarded 상태는 완료된 퀘스트이므로 제외 
                    break;
            }
        }

        return activeQuests;
    }

    
    /// 특정 퀘스트가 이 NPC에게 있는지 확인
    
    public bool HasQuest(string questId)
    {
        return questIds.Contains(questId);
    }
    
    /// Gizmo로 세이브 포인트 위치 표시 (에디터에서만)
    
    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.cyan;
        //Gizmos.DrawWireSphere(transform.position, 1f);

        CircleCollider2D collider = gameObject.GetComponent<CircleCollider2D>();
        if (collider != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(collider.transform.position, collider.radius);
        }



#if UNITY_EDITOR
        
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, NPCInfoManager.Instance.GetNPCName(npcId));
#endif
    }
}