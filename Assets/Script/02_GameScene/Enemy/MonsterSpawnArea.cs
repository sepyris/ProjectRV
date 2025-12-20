using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 몬스터 스폰 영역 (여러 종류의 몬스터 지원)
/// </summary>
public class MonsterSpawnArea : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private List<GameObject> monsterPrefabs = new List<GameObject>(); // 몬스터 프리팹 리스트
    [SerializeField] private int maxMonsterCount = 5; // 최대 몬스터 수
    [SerializeField] private float spawnInterval = 10f; // 스폰 체크 간격 (초)
    [SerializeField] private int monstersPerSpawn = 1; // 한 번에 스폰할 몬스터 수
    private int[] spawnCounts;
    [Tooltip("부족 발생 시 한 번에 부족분을 전부 스폰할지 여부")]
    [SerializeField] private bool spawnAllMissingOnShortage = true;

    [Header("Spawn Area")]
    [SerializeField] private bool useCircleArea = false; // false: Box, true: Circle
    [SerializeField] private float circleRadius = 5f; // Circle 반지름

    [Header("Random Variation")]
    [Tooltip("스폰 위치에 랜덤 오프셋 추가")]
    [SerializeField] private float randomOffset = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    private List<GameObject> spawnedMonsters = new List<GameObject>();
    private Collider2D areaCollider;

    // 루트에 생성할 컨테이너 (스케일 영향 방지)
    private Transform monstersContainer;

    void Awake()
    {
        EnsureAreaCollider();
        CreateMonstersContainer();
    }

    private void OnValidate()
    {
        EnsureAreaCollider();

        // 프리팹 리스트가 비어있으면 경고
        if (monsterPrefabs.Count == 0)
        {
            Debug.LogWarning("[MonsterSpawnArea] 몬스터 프리팹 리스트가 비어있습니다!");
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
                box.size = Vector2.one * 10f;
            }
            box.isTrigger = true;
            areaCollider = box;
        }
    }

    /// <summary>
    /// 몬스터 컨테이너 생성
    /// </summary>
    private void CreateMonstersContainer()
    {
        if (monstersContainer != null) return;

        GameObject existing = GameObject.Find($"{name}_Monsters");
        if (existing != null)
        {
            monstersContainer = existing.transform;
            monstersContainer.localScale = Vector3.one;
            return;
        }

        GameObject containerGO = new GameObject($"{name}_Monsters");
        monstersContainer = containerGO.transform;
        monstersContainer.SetParent(null);
        monstersContainer.localScale = Vector3.one;
    }

    void Start()
    {
        // 몬스터 프리팹 리스트 검증
        if (monsterPrefabs.Count == 0)
        {
            Debug.LogError("[MonsterSpawnArea] 몬스터 프리팹 리스트가 비어있습니다. 스폰할 수 없습니다.");
            return;
        }
        spawnCounts = new int[monsterPrefabs.Count];
        // 초기 스폰
        SpawnMonsters(maxMonsterCount);

        // 주기적 스폰 시작
        StartCoroutine(SpawnRoutine());
    }

    /// <summary>
    /// 주기적으로 몬스터 수를 확인하고 부족하면 스폰
    /// </summary>
    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // 죽은 몬스터 참조 정리
            spawnedMonsters.RemoveAll(monster => monster == null);

            // 현재 개수 확인
            int currentCount = spawnedMonsters.Count;
            int missing = Mathf.Max(0, maxMonsterCount - currentCount);

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
                // monstersPerSpawn 단위로 보충
                spawnCount = Mathf.Min(monstersPerSpawn, missing);
            }

            if (spawnCount > 0)
            {
                Debug.Log($"[MonsterSpawnArea] 몬스터 부족 감지: {currentCount}/{maxMonsterCount}. {spawnCount}마리 스폰.");
                SpawnMonsters(spawnCount);
            }
        }
    }

    private void SpawnMonsters(int count)
    {
        if (monsterPrefabs.Count == 0)
        {
            Debug.LogError("[MonsterSpawnArea] 몬스터 프리팹 리스트가 비어있습니다!");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            if (spawnedMonsters.Count >= maxMonsterCount)
            {
                Debug.LogWarning($"[MonsterSpawnArea] 최대 몬스터 수({maxMonsterCount}) 도달. 스폰 중단.");
                break;
            }

            float totalWeight = 0f;
            float[] weights = new float[monsterPrefabs.Count];
            for (int j = 0; j < monsterPrefabs.Count; j++)
            {
                weights[j] = 1f / (1 + spawnCounts[j]);
                totalWeight += weights[j];
            }

            float rand = Random.value * totalWeight;
            int chosenIndex = 0;
            for (int j = 0; j < monsterPrefabs.Count; j++)
            {
                rand -= weights[j];
                if (rand <= 0f)
                {
                    chosenIndex = j;
                    break;
                }
            }

            GameObject prefab = monsterPrefabs[chosenIndex];
            if (prefab == null)
            {
                Debug.LogError("[MonsterSpawnArea] 프리팹이 null입니다. 리스트를 확인하세요!");
                continue;
            }

            Vector2 spawnPosition = GetRandomPositionInArea();
            GameObject monster = Instantiate(prefab, spawnPosition, Quaternion.identity, monstersContainer);

            MonsterController mc = monster.GetComponent<MonsterController>();
            if (mc != null)
                mc.SetSpawnArea(this);
            else
                Debug.LogError("[MonsterSpawnArea] MonsterController 컴포넌트를 찾을 수 없습니다!");

            spawnedMonsters.Add(monster);
            spawnCounts[chosenIndex]++;

            Debug.Log($"[MonsterSpawnArea] 스폰 완료: {prefab.name} (현재 분포: {string.Join(",", spawnCounts)})");
        }
    }



    /// <summary>
    /// 영역 내 랜덤 위치 반환
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

        // 랜덤 오프셋 추가
        if (randomOffset > 0f)
        {
            Vector2 offset = Random.insideUnitCircle * randomOffset;
            basePosition += offset;
        }

        return basePosition;
    }

    /// <summary>
    /// 몬스터가 죽었을 때 호출됨
    /// </summary>
    public void OnMonsterDied(GameObject monster)
    {
        if (spawnedMonsters.Contains(monster))
        {
            spawnedMonsters.Remove(monster);
            Debug.Log($"[MonsterSpawnArea] 몬스터 사망 알림 받음. 남은 몬스터: {spawnedMonsters.Count}/{maxMonsterCount}");
        }
    }

    /// <summary>
    /// 강제 스폰 (테스트용)
    /// </summary>
    [ContextMenu("Spawn One Monster")]
    public void SpawnOneMonster()
    {
        SpawnMonsters(1);
    }

    /// <summary>
    /// 모든 몬스터 제거
    /// </summary>
    [ContextMenu("Clear All Monsters")]
    public void ClearAllMonsters()
    {
        foreach (GameObject monster in spawnedMonsters)
        {
            if (monster != null)
            {
                Destroy(monster);
            }
        }
        spawnedMonsters.Clear();
        Debug.Log("[MonsterSpawnArea] 모든 몬스터 제거 완료.");
    }

    // 에디터용 기즈모 (스폰 영역 표시)
    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // 반투명 빨강

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

        Gizmos.color = Color.red;

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