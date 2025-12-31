using UnityEngine;


/// 몬스터 스폰 영역 관리 모듈 - 배회 개선 버전

public class MonsterSpawnManager
{
    private Transform transform;
    public Vector2 spawnPosition { get; private set; }
    public Collider2D spawnAreaCollider { get; private set; }

    //  배회 거리 설정 (현재 위치 기준)
    public float minWanderDistance = 2f; // 최소 배회 거리
    public float maxWanderDistance = 10f; // 최대 배회 거리

    public MonsterSpawnManager(Transform transform)
    {
        this.transform = transform;
        spawnPosition = transform.position;
    }

    
    /// 스폰 영역 설정
    
    public void SetSpawnArea(Collider2D areaCollider)
    {
        spawnAreaCollider = areaCollider;
        spawnPosition = transform.position;
    }

    
    /// 랜덤 이동 목표 생성 (현재 위치 기준, 가까운 거리로 생성)
    
    public Vector2 GetRandomMoveTarget()
    {
        Vector2 currentPos = transform.position;
        Vector2 targetPosition;

        //  현재 위치에서 가까운 거리로 목표 생성 (뭉침 방지)
        float randomDistance = Random.Range(minWanderDistance, maxWanderDistance);
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        targetPosition = currentPos + (randomDirection * randomDistance);

        // 스폰 영역이 설정되어 있으면 영역 내로 제한
        if (spawnAreaCollider != null)
        {
            targetPosition = ClampToSpawnArea(targetPosition);

            // 제한된 위치가 너무 가까우면 재생성
            float distanceToTarget = Vector2.Distance(currentPos, targetPosition);
            if (distanceToTarget < minWanderDistance * 0.5f)
            {
                // 다른 방향으로 재시도
                randomDirection = -randomDirection; // 반대 방향
                targetPosition = currentPos + (randomDirection * randomDistance);
                targetPosition = ClampToSpawnArea(targetPosition);
            }
        }

        return targetPosition;
    }

    
    /// 위치를 스폰 영역 내로 제한
    
    public Vector2 ClampToSpawnArea(Vector2 position)
    {
        if (spawnAreaCollider == null) return position;

        Vector2 clamped = spawnAreaCollider.ClosestPoint(position);
        return clamped;
    }

    
    /// 스폰 영역 내에 있는지 확인
    
    public bool IsInsideSpawnArea(Vector2 position)
    {
        if (spawnAreaCollider == null) return true;

        Vector2 closest = spawnAreaCollider.ClosestPoint(position);
        float distance = Vector2.Distance(position, closest);

        return distance < 0.05f; // 허용 오차
    }

    
    /// 스폰 위치로 복귀가 필요한지 확인
    
    public bool ShouldReturnToSpawn(Vector2 currentPosition)
    {
        if (spawnAreaCollider == null) return false;

        return !IsInsideSpawnArea(currentPosition);
    }

    
    /// 스폰 위치까지의 거리 반환
    
    public float GetDistanceFromSpawn(Vector2 currentPosition)
    {
        return Vector2.Distance(currentPosition, spawnPosition);
    }

    
    /// 스폰 영역의 가장 가까운 지점 반환
    
    public Vector2 GetClosestPointInSpawnArea(Vector2 position)
    {
        if (spawnAreaCollider == null) return spawnPosition;

        return spawnAreaCollider.ClosestPoint(position);
    }
}