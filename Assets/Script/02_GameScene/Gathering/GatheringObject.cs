using GameData.Common;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

/// <summary>
/// 채집 가능한 오브젝트 (약초, 광석 등)
/// - CSV 데이터 기반으로 초기화
/// - 입력 처리는 PlayerGathering에서 담당
/// </summary>
public class GatheringObject : MonoBehaviour
{
    [Header("Gathering id")]
    [SerializeField] private string gatherableid; // 채집물 id (Inspector에서 설정)

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator; // 애니메이터 (선택사항)
    [SerializeField] private GameObject interactPrompt; // "E키" 표시

    private GatherableData gatherableData; // CSV에서 로드한 데이터
    private bool isGathered = false;
    private GatheringSpawnArea parentSpawnArea; // 스폰 영역 참조

    [Header("Gathering UI")]
    [SerializeField] private TextMeshProUGUI gatheringNameText; // 채집물 이름 텍스트 (TMP)
    [SerializeField] private GameObject SliderContainer; // 게이지와 게이지 백그라운드를 포함하는 부모 오브젝트
    [SerializeField] private Slider secondsSlider; // 채집 진행률 게이지 (Slider 권장)

    void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        // CSV 데이터 로드
        LoadGatherableData();

        UpdateNameDisplay();
        SliderContainer.SetActive(false);
    }

    /// <summary>
    /// 채집물 id 설정 (외부에서 호출 가능, 주로 프리팹에 직접 설정됨)
    /// </summary>
    public void SetGatherableid(string id)
    {
        gatherableid = id;
        LoadGatherableData();
    }

    /// <summary>
    /// CSV에서 채집물 데이터 로드
    /// </summary>
    private void LoadGatherableData()
    {
        if (string.IsNullOrEmpty(gatherableid))
        {
            Debug.LogWarning("[GatheringObject] 채집물 id가 설정되지 않았습니다.");
            return;
        }

        if (GatherableDataManager.Instance == null)
        {
            Debug.LogError("[GatheringObject] GatherableDataManager가 없습니다!");
            return;
        }

        gatherableData = GatherableDataManager.Instance.GetGatherableData(gatherableid);

        if (gatherableData == null)
        {
            Debug.LogError($"[GatheringObject] 채집물 데이터를 찾을 수 없음: {gatherableid}");
        }
        else
        {
            Debug.Log($"[GatheringObject] 채집물 데이터 로드 완료: {gatherableData.gatherableName}");
        }
    }

    private void UpdateNameDisplay()
    {
        if (gatheringNameText != null)
        {
            gatheringNameText.text = gatherableData.gatherableName;
        }
    }
    public void UpdateProgress(float progress)
    {
        // 게이지 활성화/비활성화
        bool shouldShowGauge = progress > 0.0f && progress < 1.0f;
        if (SliderContainer != null && SliderContainer.activeSelf != shouldShowGauge)
        {
            SliderContainer.SetActive(shouldShowGauge);
        }

        // 게이지 값 업데이트
        if (secondsSlider != null && shouldShowGauge)
        {
            secondsSlider.value = progress;
        }
    }
    public void CancelProgress()
    {
        SliderContainer.SetActive(false);
    }
    /// <summary>
    /// 채집 가능 여부 확인
    /// </summary>
    public bool CanGather()
    {
        return !isGathered && gatherableData != null;
    }

    /// <summary>
    /// 프롬프트 표시 (PlayerGathering에서 호출)
    /// </summary>
    public void ShowPrompt()
    {
        if (interactPrompt != null && !isGathered)
            interactPrompt.SetActive(true);
    }

    /// <summary>
    /// 프롬프트 숨김 (PlayerGathering에서 호출)
    /// </summary>
    public void HidePrompt()
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    /// <summary>
    /// 채집물 이름 가져오기
    /// </summary>
    public string GetGatherableName()
    {
        return gatherableData != null ? gatherableData.gatherableName : gatherableid;
    }

    /// <summary>
    /// 필요한 도구 타입 가져오기
    /// </summary>
    public GatherToolType GetRequiredTool()
    {
        return gatherableData != null ? gatherableData.requiredTool : GatherToolType.None;
    }

    /// <summary>
    /// 채집 시간 가져오기
    /// </summary>
    public float GetGatherTime()
    {
        return gatherableData != null ? gatherableData.gatherTime : 1.0f;
    }

    /// <summary>
    /// 실제 채집 처리 (PlayerGathering에서 호출)
    /// </summary>
    /// <param name="hasRequiredTool">필요한 도구를 가지고 있는지 여부</param>
    public void Gather(bool hasRequiredTool = false)
    {
        if (isGathered || gatherableData == null) return;

        isGathered = true;

        string toolStatus = hasRequiredTool ? " (도구 사용)" : " (도구 없음)";
        Debug.Log($"[Gathering] {gatherableData.gatherableName} 채집 완료!{toolStatus}");

        // 드롭 테이블에서 아이템 획득 (도구 유무에 따라 확률 변경)
        ProcessDropTable(hasRequiredTool);

        // 프롬프트 숨김
        HidePrompt();

        // 스폰 영역에 알림 (일회성 오브젝트)
        if (parentSpawnArea != null)
        {
            parentSpawnArea.OnGatheringObjectDestroyed(this.gameObject);
        }

        // 오브젝트 제거
        Destroy(gameObject, 0.5f);
    }

    /// <summary>
    /// 드롭 테이블 처리 - 확률에 따라 아이템 드롭
    /// </summary>
    private void ProcessDropTable(bool hasRequiredTool)
    {
        if (gatherableData.dropItems == null || gatherableData.dropItems.Count == 0)
        {
            Debug.LogWarning($"[Gathering] {gatherableData.gatherableName}의 드롭 테이블이 비어있습니다.");
            return;
        }
        Dictionary<string, int> itemIdToQuantity = new Dictionary<string, int>();

        foreach (var dropItem in gatherableData.dropItems)
        {

            bool dropSuccess = false;

            // 필요한 도구를 가지고 있으면 100% 드롭
            if (hasRequiredTool)
            {
                dropSuccess = true;
            }
            else
            {
                // 도구가 없으면 원래 확률로 드롭
                dropSuccess = dropItem.RollDrop();
            }

            if (dropSuccess)
            {
                if (itemIdToQuantity.ContainsKey(dropItem.itemId))
                {
                    itemIdToQuantity[dropItem.itemId] += dropItem.quantity;
                }
                else
                {
                    itemIdToQuantity[dropItem.itemId] = dropItem.quantity;
                }
            }
            else
            {
                Debug.Log($"[Gathering] 드롭 실패: {dropItem.itemId} (확률: {dropItem.dropRate}%)");
            }
        }
        foreach (var dropItem in itemIdToQuantity)
        {
            // 아이템 획득
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(dropItem.Key, dropItem.Value);
                FloatingItemManager.Instance?.ShowItemAcquired(ItemDataManager.Instance.GetItemData(dropItem.Key), dropItem.Value);
            }

            // 퀘스트 매니저에 아이템 획득 알림
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.UpdateItemProgress(dropItem.Key, dropItem.Value);
            }
        }
        itemIdToQuantity.Clear();
    }

    /// <summary>
    /// 스폰 영역 설정 (GatheringSpawnArea에서 호출)
    /// </summary>
    public void SetSpawnArea(GatheringSpawnArea spawnArea)
    {
        parentSpawnArea = spawnArea;
    }

    /// <summary>
    /// 강제 재생성 (외부에서 호출 가능)
    /// </summary>
    public void ForceRespawn()
    {
        if (isGathered)
        {
            isGathered = false;

            // 시각적 복구
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = 1f;
                spriteRenderer.color = color;
            }

            Debug.Log($"[Gathering] {gatherableData.gatherableName} 재생성 완료");
        }
    }
    private void OnDrawGizmos()
    {
        CircleCollider2D collider = gameObject.GetComponent<CircleCollider2D>();
        if (collider != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(collider.transform.position, collider.radius);
        }
    }
}