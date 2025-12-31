using UnityEngine;
using UnityEngine.UI;
using TMPro;


/// 퀘스트 네비게이션 시스템
/// 현재는 기본 구조만 - 나중에 미니맵/화살표 등을 추가

public class QuestNavigationSystem : MonoBehaviour
{
    public static QuestNavigationSystem Instance { get; private set; }

    [Header("UI 요소")]
    public GameObject navigationPanel;
    public TextMeshProUGUI targetNameText;
    public TextMeshProUGUI distanceText;
    public Image directionArrow;

    [Header("추적 대상")]
    private string currentTargetNPCId;
    private NPCController currentTargetNPC;

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

        if (navigationPanel != null)
            navigationPanel.SetActive(false);
    }

    void Update()
    {
        if (currentTargetNPC != null)
        {
            UpdateNavigation();
        }
    }

    
    /// 특정 NPC를 추적 대상으로 설정
    
    public void SetNavigationTarget(string npcId)
    {
        if (NPCInfoManager.Instance == null) return;

        Npcs npcInfo = NPCInfoManager.Instance.GetNPCInfo(npcId);
        if (npcInfo == null)
        {
            Debug.LogWarning($"[Navigation] NPC 정보를 찾을 수 없음: {npcId}");
            return;
        }

        // 같은 맵에 있는지 확인
        if (MapInfoManager.Instance != null)
        {
            if (npcInfo.mapId != MapInfoManager.Instance.currentMapId)
            {
                // 다른 맵에 있음 - 경로 안내
                ShowMapPathToTarget(npcInfo);
                return;
            }
        }

        // 같은 맵에 있음 - 실제 NPC 찾기
        NPCController[] npcs = FindObjectsOfType<NPCController>();
        foreach (var npc in npcs)
        {
            if (npc.npcId == npcId)
            {
                currentTargetNPC = npc;
                currentTargetNPCId = npcId;

                if (navigationPanel != null)
                    navigationPanel.SetActive(true);

                if (targetNameText != null)
                    targetNameText.text = $"목표: {npcInfo.npcName}";

                Debug.Log($"[Navigation] 추적 시작: {npcInfo.npcName}");
                return;
            }
        }

        Debug.LogWarning($"[Navigation] 씬에서 NPC를 찾을 수 없음: {npcId}");
    }

    
    /// 다른 맵에 있는 NPC로 가는 경로 표시
    
    private void ShowMapPathToTarget(Npcs npcInfo)
    {
        if (MapInfoManager.Instance == null) return;

        string currentMap = MapInfoManager.Instance.currentMapId;
        string targetMap = npcInfo.mapId;

        var path = MapInfoManager.Instance.FindPathBetweenMaps(currentMap, targetMap);

        if (path.Count > 1)
        {
            // 다음에 가야 할 맵 표시
            string nextMapId = path[1];
            string nextMapName = MapInfoManager.Instance.GetMapName(nextMapId);

            if (navigationPanel != null)
                navigationPanel.SetActive(true);

            if (targetNameText != null)
                targetNameText.text = $"목표: {npcInfo.npcName}";

            if (distanceText != null)
                distanceText.text = $"{nextMapName}(으)로 이동하세요";

            Debug.Log($"[Navigation] 경로: {string.Join(" → ", path)}");
        }
    }

    
    /// 네비게이션 업데이트 (거리, 방향 계산)
    
    private void UpdateNavigation()
    {
        if (PlayerController.Instance == null || currentTargetNPC == null)
            return;

        Vector3 playerPos = PlayerController.Instance.transform.position;
        Vector3 targetPos = currentTargetNPC.transform.position;

        // 거리 계산
        float distance = Vector3.Distance(playerPos, targetPos);

        if (distanceText != null)
        {
            if (distance < 2f)
            {
                distanceText.text = "도착!";
                distanceText.color = Color.green;
            }
            else
            {
                distanceText.text = $"{distance:F1}m";
                distanceText.color = Color.white;
            }
        }

        // 방향 화살표 회전
        if (directionArrow != null)
        {
            Vector3 direction = (targetPos - playerPos).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            directionArrow.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }
    }

    
    /// 네비게이션 종료
    
    public void ClearNavigation()
    {
        currentTargetNPC = null;
        currentTargetNPCId = null;

        if (navigationPanel != null)
            navigationPanel.SetActive(false);

        Debug.Log("[Navigation] 추적 종료");
    }

    
    /// 현재 진행 중인 퀘스트의 첫 번째 목표를 추적
    
    public void TrackCurrentQuest(string questId)
    {
        if (QuestManager.Instance == null) return;

        QuestData quest = QuestManager.Instance.GetQuestData(questId);
        if (quest == null || quest.status != QuestStatus.Accepted)
            return;

        // 첫 번째 미완료 Dialogue 목표 찾기
        foreach (var obj in quest.objectives)
        {
            if (!obj.IsCompleted && obj.type == QuestType.Dialogue)
            {
                SetNavigationTarget(obj.targetId);
                return;
            }
        }

        Debug.Log($"[Navigation] 추적 가능한 목표 없음: {questId}");
    }
}