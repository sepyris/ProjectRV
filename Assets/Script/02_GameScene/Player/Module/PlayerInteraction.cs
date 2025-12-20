using Definitions;
using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어의 채집 및 NPC 상호작용 관리
/// </summary>
public class PlayerInteraction
{
    private readonly Transform playerTransform;
    private readonly PlayerAnimationController animationController;

    public bool ControlsLocked = false;
    public bool IsGathering { get; private set; } = false;

    private GatheringObject currentNearestGathering = null;
    private NPCController currentNearestNPC = null;
    private readonly float detectionRadius = 0.5f;

    // 도구 보유 상태 (실제로는 인벤토리 시스템과 연동해야 함)
    private bool hasPickaxe = false;
    private bool hasSickle = false;
    private bool hasFishingRod = false;
    private bool hasAxe = false;

    public PlayerInteraction(Transform playerTransform, PlayerAnimationController animController)
    {
        this.playerTransform = playerTransform;
        this.animationController = animController;
    }

    /// <summary>
    /// New Input System용 상호작용 메서드
    /// </summary>
    public void TryInteract()
    {
        if (ControlsLocked || IsGathering) return;

        // 채집물이 선택된 경우
        if (currentNearestGathering != null)
        {
            Vector2 directionToGathering = (currentNearestGathering.transform.position - playerTransform.position).normalized;
            PlayerController.Instance?.SetFacingDirection(directionToGathering);

            PlayerController.Instance.StartCoroutine(GatherCoroutine(currentNearestGathering));
        }
        // NPC가 선택된 경우
        else if (currentNearestNPC != null)
        {
            InteractWithNPC(currentNearestNPC);
        }
    }

    /// <summary>
    /// 매 프레임 가장 가까운 상호작용 오브젝트 감지
    /// </summary>
    public void UpdateNearestInteractable()
    {
        if (ControlsLocked || IsGathering)
        {
            HideAllPrompts();
            return;
        }

        // 1. 가장 가까운 채집물 찾기
        GatheringObject closestGathering = FindNearestGathering();

        // 2. 가장 가까운 NPC 찾기
        NPCController closestNPC = FindNearestNPC();

        // 3. 둘 중 더 가까운 것 선택
        float gatheringDistance = closestGathering != null ?
            Vector2.Distance(playerTransform.position, closestGathering.transform.position) : float.MaxValue;
        float npcDistance = closestNPC != null ?
            Vector2.Distance(playerTransform.position, closestNPC.transform.position) : float.MaxValue;

        // 이전 프롬프트와 다를 경우 프롬프트 업데이트
        if (gatheringDistance < npcDistance)
        {
            if (currentNearestGathering != closestGathering)
            {
                currentNearestGathering?.HidePrompt();
                currentNearestNPC?.HidePrompt();
                currentNearestGathering = closestGathering;
                currentNearestNPC = null;
                currentNearestGathering?.ShowPrompt();
            }
        }
        else if (npcDistance < float.MaxValue)
        {
            if (currentNearestNPC != closestNPC)
            {
                currentNearestGathering?.HidePrompt();
                currentNearestNPC?.HidePrompt();
                currentNearestNPC = closestNPC;
                currentNearestGathering = null;
                currentNearestNPC?.ShowPrompt();
            }
        }
        else
        {
            HideAllPrompts();
        }
    }

    private GatheringObject FindNearestGathering()
    {
        int gatheringLayer = LayerMask.GetMask(Def_Name.LAYER_GATHERING);
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(playerTransform.position, detectionRadius, gatheringLayer);

        GatheringObject closest = null;
        float closestDistance = float.MaxValue;

        foreach (var col in nearbyColliders)
        {
            GatheringObject gatherObj = col.GetComponent<GatheringObject>();
            if (gatherObj != null && gatherObj.CanGather())
            {
                float distance = Vector2.Distance(playerTransform.position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = gatherObj;
                }
            }
        }

        return closest;
    }

    private NPCController FindNearestNPC()
    {
        int npcLayer = LayerMask.GetMask(Def_Name.LAYER_NPC);
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(playerTransform.position, detectionRadius, npcLayer);

        NPCController closest = null;
        float closestDistance = float.MaxValue;

        foreach (var col in nearbyColliders)
        {
            if (col.TryGetComponent<NPCController>(out var npc))
            {
                float distance = Vector2.Distance(playerTransform.position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = npc;
                }
            }
        }

        return closest;
    }

    private void HideAllPrompts()
    {
        currentNearestGathering?.HidePrompt();
        currentNearestGathering = null;
        currentNearestNPC?.HidePrompt();
        currentNearestNPC = null;
    }

    private void InteractWithNPC(NPCController npc)
    {
        // 버그 수정 2: NPC 방향으로 플레이어 회전 (8방향)
        Vector2 directionToNPC = (npc.transform.position - playerTransform.position).normalized;
        PlayerController.Instance?.SetFacingDirection(directionToNPC);

        // NPC 상호작용 시작
        npc.Interact();

        // 프롬프트 숨김
        npc.HidePrompt();
        currentNearestNPC = null;
    }

    private IEnumerator GatherCoroutine(GatheringObject targetObject)
    {
        IsGathering = true;
        targetObject.HidePrompt();
        // 필요한 도구 확인
        GatherToolType requiredTool = targetObject.GetRequiredTool();
        bool hasTool = HasRequiredTool(requiredTool);

        if (!hasTool)
        {
            Debug.LogWarning($"[Gathering] {GetToolName(requiredTool)}이(가) 필요합니다!");
            // TODO: UI로 메시지 표시
        }

        // 채집 애니메이션 재생
        animationController?.PlayAnimation("Gather");

        // --- 👇 키 입력으로 중지 가능한 부분 👇 ---
        float gatherTime = targetObject.GetGatherTime();
        float elapsedTime = 0f;

        while (elapsedTime < gatherTime)
        {
            // 중지: 이동 입력이 있으면 채집 중단
            if (PlayerController.Instance != null && PlayerController.Instance.MoveInput.magnitude > 0.1f)
            {
                Debug.Log("[Gathering] 이동 입력으로 채집 중단!");

                // 애니메이션 복원 (중단 시)
                animationController?.PlayAnimation("Idle");

                IsGathering = false;
                currentNearestGathering = null;
                targetObject.CancelProgress();
                targetObject.ShowPrompt();
                yield break; // 👈 코루틴 즉시 종료
            }
            targetObject.UpdateProgress(elapsedTime / gatherTime);
            elapsedTime += Time.deltaTime;
            yield return null; // 👈 다음 프레임까지 대기
        }
        // --- 👆 키 입력으로 중지 가능한 부분 👆 ---

        // 채집 완료 처리 (정상 종료 시에만 실행)
        targetObject.Gather(hasTool);

        // 애니메이션 복원
        animationController?.PlayAnimation("Idle");

        IsGathering = false;

        // 프롬프트 숨김
        currentNearestGathering = null;
    }

    // 도구 설정 메서드들 (인벤토리 시스템과 연동시 사용)
    public void SetHasTool(string toolType, bool hasIt)
    {
        switch (toolType.ToLower())
        {
            case "pickaxe": hasPickaxe = hasIt; break;
            case "sickle": hasSickle = hasIt; break;
            case "fishingrod": hasFishingRod = hasIt; break;
            case "axe": hasAxe = hasIt; break;
        }
    }

    public bool HasRequiredTool(GatherToolType type)
    {
        // 인벤토리 시스템과 연동하여 실제 도구 보유 여부 확인
        if (InventoryManager.Instance != null)
        {
            switch (type)
            {
                case GatherToolType.Pickaxe:
                    return InventoryManager.Instance.HasItem("Pickaxe") || hasPickaxe;
                case GatherToolType.Sickle:
                    return InventoryManager.Instance.HasItem("Sickle") || hasSickle;
                case GatherToolType.FishingRod:
                    return InventoryManager.Instance.HasItem("FishingRod") || hasFishingRod;
                case GatherToolType.Axe:
                    return InventoryManager.Instance.HasItem("Axe") || hasAxe;
                case GatherToolType.None:
                    return true;
                default:
                    return false;
            }
        }

        // InventoryManager가 없는 경우 로컬 변수로 확인
        return type switch
        {
            GatherToolType.Pickaxe => hasPickaxe,
            GatherToolType.Sickle => hasSickle,
            GatherToolType.FishingRod => hasFishingRod,
            GatherToolType.Axe => hasAxe,
            GatherToolType.None => true,
            _ => false
        };
    }

    private string GetToolName(GatherToolType type)
    {
        return type switch
        {
            GatherToolType.Pickaxe => "곡괭이",
            GatherToolType.Sickle => "낫",
            GatherToolType.FishingRod => "낚싯대",
            GatherToolType.Axe => "도끼",
            GatherToolType.None => "도구 없음",
            _ => "알 수 없는 도구"
        };
    }
    public float getinteractRadius()
    {
        return detectionRadius;
    }
}