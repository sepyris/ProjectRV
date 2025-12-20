using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 채집 오브젝트(약초, 광석 등)를 자동으로 스폰하고 관리하는 영역
/// 여러 종류의 채집물을 랜덤하게 스폰
/// </summary>
public class GatheringSpawnArea : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private List<GameObject> gatheringPrefabs = new List<GameObject>(); // 채집 오브젝트 프리팹 리스트
    [SerializeField] private int maxGatheringCount = 10; // 최대 채집 오브젝트 수
    [SerializeField] private float spawnInterval = 5f; // 스폰 체크 간격 (초)
    [SerializeField] private int objectsPerSpawn = 1; // 한 번에 스폰할 개수
    [Tooltip("부족 발생 시 한 번에 부족분을 전부 스폰할지 여부")]
    [SerializeField] private bool spawnAllMissingOnShortage = true;

    [Header("Spawn Area")]
    [SerializeField] private bool useCircleArea = false; // false: Box, true: Circle
    [SerializeField] private float circleRadius = 5f; // Circle 반지름

    [Header("Random Variation")]
    [Tooltip("스폰 위치에 랜덤 오프셋 추가 (자연스러운 배치)")]
    [SerializeField] private float randomOffset = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    private List<GameObject> spawnedGatherings = new List<GameObject>();
    private Collider2D areaCollider;

    // 루트에 생성할 컨테이너 (스케일 영향 방지)
    private Transform gatheringsContainer;

    void Awake()
    {
        EnsureAreaCollider();
        CreateGatheringsContainer();
    }

    private void OnValidate()
    {
        EnsureAreaCollider();

        // 프리팹 리스트가 비어있으면 경고
        if (gatheringPrefabs.Count == 0)
        {
            Debug.LogWarning("[GatheringSpawnArea] 채집물 프리팹 리스트가 비어있습니다!");
        }
    }

    private void Reset()
    {
        EnsureAreaCollider();
    }

    /// <summary>
    /// 콜라이더 존재 확인 및 필요 시 생성
    /// </summary>
    private void EnsureAreaCollider()
    {
        if (areaCollider != null) return;

        CircleCollider2D existingCircle = GetComponent<CircleCollider2D>();
        BoxCollider2D existingBox = GetComponent<BoxCollider2D>();

        if (useCircleArea)
        {
            CircleCollider2D circle = existingCircle ?? gameObject.AddComponent<CircleCollider2D>();
            circle.radius = circleRadius;
            circle.isTrigger = true;
            areaCollider = circle;
        }
        else
        {
            BoxCollider2D box = existingBox ?? gameObject.AddComponent<BoxCollider2D>();
            if (box.size == Vector2.zero)
            {
                box.size = Vector2.one * 10f; // 채집 영역은 넓게 기본값 설정
            }
            box.isTrigger = true;
            areaCollider = box;
        }
    }

    /// <summary>
    /// 채집 오브젝트 컨테이너 생성
    /// </summary>
    private void CreateGatheringsContainer()
    {
        if (gatheringsContainer != null) return;

        GameObject existing = GameObject.Find($"{name}_Gatherings");
        if (existing != null)
        {
            gatheringsContainer = existing.transform;
            gatheringsContainer.localScale = Vector3.one;
            return;
        }

        GameObject containerGO = new GameObject($"{name}_Gatherings");
        gatheringsContainer = containerGO.transform;
        gatheringsContainer.SetParent(null);
        gatheringsContainer.localScale = Vector3.one;
    }

    void Start()
    {
        // 채집물 프리팹 리스트 검증
        if (gatheringPrefabs.Count == 0)
        {
            Debug.LogError("[GatheringSpawnArea] 채집물 프리팹 리스트가 비어있습니다. 스폰할 수 없습니다.");
            return;
        }

        // 초기 스폰
        SpawnGatherings(maxGatheringCount);

        // 주기적 스폰 시작
        StartCoroutine(SpawnRoutine());
    }

    /// <summary>
    /// 주기적으로 채집 오브젝트 수를 확인하고 부족하면 스폰
    /// </summary>
    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // 파괴된 오브젝트 참조 정리
            spawnedGatherings.RemoveAll(obj => obj == null);

            // 현재 개수 확인
            int currentCount = spawnedGatherings.Count;
            int missing = Mathf.Max(0, maxGatheringCount - currentCount);

            int spawnCount;
            if (missing <= 0)
            {
                spawnCount = 0;
            }
            else if (spawnAllMissingOnShortage)
            {
                // 부족분을 한 번에 전부 스폰
                spawnCount = missing;
            }
            else
            {
                // objectsPerSpawn 단위로 보충
                spawnCount = Mathf.Min(objectsPerSpawn, missing);
            }

            if (spawnCount > 0)
            {
                Debug.Log($"[GatheringSpawnArea] 채집 오브젝트 부족 감지: {currentCount}/{maxGatheringCount}. {spawnCount}개 스폰.");
                SpawnGatherings(spawnCount);
            }
        }
    }

    /// <summary>
    /// 채집 오브젝트 스폰 (랜덤 프리팹 선택)
    /// </summary>
    private void SpawnGatherings(int count)
    {
        if (gatheringPrefabs.Count == 0)
        {
            Debug.LogError("[GatheringSpawnArea] 채집물 프리팹 리스트가 비어있습니다!");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            // 현재 개수 확인
            if (spawnedGatherings.Count >= maxGatheringCount)
            {
                Debug.LogWarning($"[GatheringSpawnArea] 최대 채집 오브젝트 수({maxGatheringCount}) 도달. 스폰 중단.");
                break;
            }

            // 랜덤 위치
            Vector2 spawnPosition = GetRandomPositionInArea();

            // 랜덤 프리팹 선택
            GameObject randomPrefab = gatheringPrefabs[Random.Range(0, gatheringPrefabs.Count)];

            if (randomPrefab == null)
            {
                Debug.LogError("[GatheringSpawnArea] 프리팹이 null입니다. 리스트를 확인하세요!");
                continue;
            }

            // 프리팹 인스턴스화
            GameObject gathering = Instantiate(randomPrefab, spawnPosition, Quaternion.identity, gatheringsContainer);

            // 채집 오브젝트에게 스폰 영역 알려주기
            GatheringObject gatheringObj = gathering.GetComponent<GatheringObject>();
            if (gatheringObj != null)
            {
                gatheringObj.SetSpawnArea(this);
            }
            else
            {
                Debug.LogError("[GatheringSpawnArea] GatheringObject 컴포넌트를 찾을 수 없습니다!");
            }

            spawnedGatherings.Add(gathering);
            Debug.Log($"[GatheringSpawnArea] 채집 오브젝트 스폰 완료: {randomPrefab.name} at {spawnPosition}");
        }
    }

    /// <summary>
    /// 영역 내 랜덤 위치 반환 (콜라이더.bounds 기반)
    /// </summary>
    private Vector2 GetRandomPositionInArea()
    {
        Vector2 basePosition;

        if (areaCollider == null)
        {
            // 폴백: 기존 방식
            if (useCircleArea)
            {
                Vector2 randomDirection = Random.insideUnitCircle * circleRadius;
                basePosition = (Vector2)transform.position + randomDirection;
            }
            else
            {
                float randomX = Random.Range(-transform.localScale.x / 2f, transform.localScale.x / 2f);
                float randomY = Random.Range(-transform.localScale.y / 2f, transform.localScale.y / 2f);
                basePosition = (Vector2)transform.position + new Vector2(randomX, randomY);
            }
        }
        else
        {
            Bounds b = areaCollider.bounds;
            if (useCircleArea)
            {
                Vector2 randomDirection = Random.insideUnitCircle * circleRadius;
                basePosition = (Vector2)b.center + randomDirection;
            }
            else
            {
                float randomX = Random.Range(b.min.x, b.max.x);
                float randomY = Random.Range(b.min.y, b.max.y);
                basePosition = new Vector2(randomX, randomY);
            }
        }

        //  랜덤 오프셋 추가 (자연스러운 배치) 
        if (randomOffset > 0f)
        {
            Vector2 offset = Random.insideUnitCircle * randomOffset;
            basePosition += offset;
        }

        return basePosition;
    }

    /// <summary>
    /// 채집 오브젝트가 파괴되었을 때 호출됨 (일회성 오브젝트용)
    /// </summary>
    public void OnGatheringObjectDestroyed(GameObject obj)
    {
        if (spawnedGatherings.Contains(obj))
        {
            spawnedGatherings.Remove(obj);
            Debug.Log($"[GatheringSpawnArea] 채집 오브젝트 파괴 알림 받음. 남은 개수: {spawnedGatherings.Count}/{maxGatheringCount}");
        }
    }

    /// <summary>
    /// 모든 채집 오브젝트 강제 재생성
    /// </summary>
    [ContextMenu("Respawn All Gatherings")]
    public void RespawnAllGatherings()
    {
        foreach (GameObject obj in spawnedGatherings)
        {
            if (obj != null)
            {
                GatheringObject gathering = obj.GetComponent<GatheringObject>();
                if (gathering != null)
                {
                    gathering.ForceRespawn();
                }
            }
        }
        Debug.Log("[GatheringSpawnArea] 모든 채집 오브젝트 강제 재생성 완료.");
    }

    /// <summary>
    /// 강제 스폰 (테스트용)
    /// </summary>
    [ContextMenu("Spawn One Gathering")]
    public void SpawnOneGathering()
    {
        SpawnGatherings(1);
    }

    /// <summary>
    /// 모든 채집 오브젝트 제거
    /// </summary>
    [ContextMenu("Clear All Gatherings")]
    public void ClearAllGatherings()
    {
        foreach (GameObject obj in spawnedGatherings)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedGatherings.Clear();
        Debug.Log("[GatheringSpawnArea] 모든 채집 오브젝트 제거 완료.");
    }

    // 에디터용 기즈모 (스폰 영역 표시)
    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // 반투명 초록색 (채집 영역)

        if (useCircleArea)
        {
            Gizmos.DrawWireSphere(transform.position, circleRadius);
        }
        else
        {
            if (areaCollider != null)
            {
                Bounds b = areaCollider.bounds;
                Gizmos.DrawWireCube(b.center, b.size);
            }
            else
            {
                Gizmos.DrawWireCube(transform.position, transform.localScale);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.green; // 선택 시 진한 초록색

        if (useCircleArea)
        {
            Gizmos.DrawWireSphere(transform.position, circleRadius);
        }
        else
        {
            if (areaCollider != null)
            {
                Bounds b = areaCollider.bounds;
                Gizmos.DrawWireCube(b.center, b.size);
            }
            else
            {
                Gizmos.DrawWireCube(transform.position, transform.localScale);
            }
        }
    }
}