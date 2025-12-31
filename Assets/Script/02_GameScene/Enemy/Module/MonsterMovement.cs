using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static MonsterAI;
using Pathfinding; // A* Pathfinding Project


/// 몬스터 이동 모듈 - A* Pathfinding

public class MonsterMovement
{
    private Rigidbody2D rb;
    private Transform transform;

    public float moveSpeed = 2f;
    public float separationRadius = 1.0f;
    public float separationStrength = 1.0f;
    public LayerMask separationMask;

    // A* Pathfinding 설정
    private Seeker seeker;
    private Path currentPath;
    private int currentWaypoint = 0;
    public float nextWaypointDistance = 0.1f; // 다음 웨이포인트로 간주하는 거리
    public float pathUpdateInterval = 1.5f; // 경로 재계산 간격 (배회: 1.5초)
    public float chasePathUpdateInterval = 0.5f; // 추적 시 경로 재계산 간격 (0.5초)
    private float lastPathUpdateTime = 0f;
    private Vector2 lastTargetPosition; // 마지막 목표 위치
    private float pathUpdateIntervalVariation = 0.2f; // 재계산 시간 랜덤 편차

    public bool ignoreAreaLimit = false; // 도발 상태 등에서 true로
    private Collider2D spawnAreaCollider;
    private bool isChasing = false; // 추적 중인지 여부

    public MonsterMovement(Rigidbody2D rb, Transform transform)
    {
        this.rb = rb;
        this.transform = transform;

        // Seeker 컴포넌트 가져오기 (없으면 추가)
        seeker = transform.GetComponent<Seeker>();
        if (seeker == null)
        {
            seeker = transform.gameObject.AddComponent<Seeker>();
        }

        // 기본 레이어 마스크 설정
        separationMask = LayerMask.GetMask("Monster", "Player", "NPC");

        //  경로 재계산 시간에 랜덤 편차 추가 (뭉침 방지)
        pathUpdateInterval += Random.Range(-pathUpdateIntervalVariation, pathUpdateIntervalVariation);
    }

    
    /// 스폰 영역 설정
    
    public void SetSpawnArea(Collider2D areaCollider)
    {
        spawnAreaCollider = areaCollider;
    }

    
    /// 목표 위치로 이동 - A* Pathfinding 사용
    
    public void MoveToPosition(Vector2 targetPosition, float speedMultiplier = 1f, bool ignoreArea = false)
    {
        if (rb == null || seeker == null) return;

        // 영역 제한 적용
        if (!ignoreAreaLimit && spawnAreaCollider != null && !ignoreArea)
        {
            targetPosition = ClampToSpawnArea(targetPosition);
        }

        Vector2 currentPos = rb.position;
        float distanceToTarget = Vector2.Distance(currentPos, targetPosition);

        // 너무 가까우면 도착 처리
        if (distanceToTarget <= 0.25f)
        {
            rb.velocity = Vector2.zero;
            rb.MovePosition(targetPosition);
            currentPath = null;
            return;
        }

        //  추적 중인지 배회 중인지에 따라 다른 재계산 간격 사용
        float currentUpdateInterval = isChasing ? chasePathUpdateInterval : pathUpdateInterval;

        //  목표 변경 감지 임계값 조정 (배회: 1.0, 추적: 0.5)
        float targetChangeThreshold = isChasing ? 0.5f : 1.0f;

        // 경로가 없거나 목표가 변경되었거나 일정 시간이 지났으면 경로 재계산
        bool shouldRecalculate = currentPath == null ||
                                 Vector2.Distance(lastTargetPosition, targetPosition) > targetChangeThreshold ||
                                 Time.time - lastPathUpdateTime > currentUpdateInterval;

        if (shouldRecalculate && !seeker.IsDone())
        {
            // 이미 경로 계산 중이면 대기
            return;
        }

        if (shouldRecalculate)
        {
            lastTargetPosition = targetPosition;
            lastPathUpdateTime = Time.time;

            // 경로 계산 요청
            seeker.StartPath(rb.position, targetPosition, OnPathComplete);
        }

        // 경로를 따라 이동
        if (currentPath != null && !currentPath.error)
        {
            FollowPath(speedMultiplier);
        }
    }

    
    /// 추적 모드 설정 (추적 중일 때 더 자주 경로 재계산)
    
    public void SetChasing(bool chasing)
    {
        isChasing = chasing;
    }

    
    /// 경로 계산 완료 콜백
    
    private void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            currentPath = p;
            currentWaypoint = 0; // 웨이포인트 인덱스 초기화
        }
        else
        {
            Debug.LogWarning($"[MonsterMovement] 경로 계산 실패: {p.errorLog}");
            currentPath = null;
        }
    }

    
    /// 계산된 경로를 따라 이동
    
    private void FollowPath(float speedMultiplier)
    {
        if (currentPath == null || currentWaypoint >= currentPath.vectorPath.Count)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Vector2 currentPos = rb.position;
        Vector2 targetWaypoint = currentPath.vectorPath[currentWaypoint];

        // 현재 웨이포인트에 도달했는지 확인
        float distanceToWaypoint = Vector2.Distance(currentPos, targetWaypoint);
        if (distanceToWaypoint < nextWaypointDistance)
        {
            currentWaypoint++;
            if (currentWaypoint >= currentPath.vectorPath.Count)
            {
                rb.velocity = Vector2.zero;
                return;
            }
            targetWaypoint = currentPath.vectorPath[currentWaypoint];
        }

        // 다음 웨이포인트로 이동
        Vector2 direction = (targetWaypoint - currentPos).normalized;
        Vector2 desiredVel = direction * moveSpeed * speedMultiplier;

        // 분리 벡터 추가 (다른 몬스터와 겹침 방지)
        Vector2 separation = ComputeSeparation() * separationStrength;
        Vector2 finalVel = desiredVel + separation;

        // 속도 제한
        float maxVel = moveSpeed * speedMultiplier * 1.2f;
        if (finalVel.magnitude > maxVel)
            finalVel = finalVel.normalized * maxVel;

        // 영역 경계 보정 (비도발 상태에서만)
        if (!ignoreAreaLimit && spawnAreaCollider != null)
        {
            finalVel = ClampVelocityToArea(currentPos, finalVel);
        }

        rb.velocity = finalVel;
    }

    
    /// 이동 정지
    
    public void Stop()
    {
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        currentPath = null;
    }

    
    /// 현재 위치 반환
    
    public Vector2 GetPosition()
    {
        return rb != null ? rb.position : Vector2.zero;
    }

    
    /// 목표까지의 거리 반환
    
    public float GetDistanceTo(Vector2 targetPosition)
    {
        return Vector2.Distance(GetPosition(), targetPosition);
    }

    
    /// 주변 대상과의 분리 벡터 계산 (겹침 방지)
    
    private Vector2 ComputeSeparation()
    {
        if (rb == null || separationRadius <= 0f) return Vector2.zero;

        Collider2D[] hits = Physics2D.OverlapCircleAll(rb.position, separationRadius, separationMask);
        Vector2 separation = Vector2.zero;
        int count = 0;

        foreach (var col in hits)
        {
            if (col == null) continue;
            Rigidbody2D otherRb = col.attachedRigidbody;
            if (otherRb == null || otherRb == rb) continue;

            Vector2 diff = rb.position - (Vector2)otherRb.position;
            float dist = diff.magnitude;
            if (dist > 0.0001f)
            {
                separation += diff.normalized / dist;
                count++;
            }
        }

        if (count > 0)
        {
            separation /= count;
            if (separation.magnitude > 1f)
                separation = separation.normalized;
        }

        return separation;
    }

    
    /// 위치를 스폰 영역 내로 제한
    
    private Vector2 ClampToSpawnArea(Vector2 position)
    {
        if (spawnAreaCollider == null) return position;
        return spawnAreaCollider.ClosestPoint(position);
    }

    
    /// 속도를 영역 내로 제한
    
    private Vector2 ClampVelocityToArea(Vector2 currentPos, Vector2 velocity)
    {
        Vector2 nextPos = currentPos + velocity * Time.fixedDeltaTime;
        Vector2 clampedNextPos = spawnAreaCollider.ClosestPoint(nextPos);

        if (Vector2.Distance(nextPos, clampedNextPos) > 0.05f)
        {
            Vector2 toInside = (clampedNextPos - currentPos).normalized;
            velocity = toInside * velocity.magnitude;
        }

        return velocity;
    }

    
    /// 위치를 영역 내로 제한 (외부 호출용)
    
    public Vector2 ClampToArea(Vector2 position, Collider2D areaCollider)
    {
        if (areaCollider == null) return position;
        return areaCollider.ClosestPoint(position);
    }

    
    /// 속도 너무 느리면 멈춤 처리
    
    public void ClampVelocity()
    {
        if (rb == null) return;
        if (rb.velocity.magnitude < 0.05f)
            rb.velocity = Vector2.zero;
    }

    
    /// 부드럽게 위치 보정 (영역 복귀용)
    
    public void SmoothCorrection(Vector2 targetPosition, float lerpSpeed = 1f)
    {
        if (rb == null) return;

        Vector2 currentPos = rb.position;
        Vector2 newPos = Vector2.Lerp(currentPos, targetPosition, lerpSpeed);
        rb.MovePosition(newPos);
        rb.velocity = Vector2.zero;
        currentPath = null;
    }
}