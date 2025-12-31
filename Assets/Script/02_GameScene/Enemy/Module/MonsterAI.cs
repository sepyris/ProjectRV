using System.Collections;
using System.Xml.Linq;
using UnityEngine;


/// 몬스터 AI 모듈

public class MonsterAI
{
    private Transform transform;
    private MonsterMovement movement;
    private MonsterCombat combat;
    private MonsterSpawnManager spawnManager;

    public bool isAggressive;  // 선공 여부
    public float detectionRange = 5f;
    private float originalDetectionRange;

    public float minWaitTime = 2f;//최소 대기 시간
    public float maxWaitTime = 7f;//최대 대기 시간
    public float idleProbability = 0.15f; // 가만히 있을 확률 (15%)

    public float provokedDuration = 8f;
    private float provokedTimeRemaining = 0f;
    private bool isProvoked = false;
    public bool originalIsAggressive;

    public float returnStopDistance = 0.15f;
    public float returnLerpSpeed = 0.2f;
    private bool isReturning = false;

    private MonsterState currentState = MonsterState.Idle;
    private Vector2 moveTargetPosition;
    private MonoBehaviour coroutineRunner;

    public enum MonsterState { Idle, Wandering, Chasing, Attacking, Returning }

    public MonsterAI(Transform transform, MonsterMovement movement, MonsterCombat combat, MonsterSpawnManager spawnManager, bool isAggressive)
    {
        this.transform = transform;
        this.movement = movement;
        this.combat = combat;
        this.spawnManager = spawnManager;
        this.isAggressive = isAggressive;
        this.originalIsAggressive = isAggressive;
        this.originalDetectionRange = detectionRange;

        coroutineRunner = transform.GetComponent<MonsterController>();
        if (coroutineRunner != null)
            coroutineRunner.StartCoroutine(AIRoutine());
    }

    public void UpdateAI(Transform playerTransform)
    {
        if (isProvoked)
        {
            provokedTimeRemaining -= Time.deltaTime;
            if (provokedTimeRemaining <= 0f)
            {
                isProvoked = false;
                provokedTimeRemaining = 0f;
                isAggressive = originalIsAggressive;
                detectionRange = originalDetectionRange;
                movement.ignoreAreaLimit = false;

                StartReturnToSpawn();
            }
        }

        if (playerTransform != null)
            UpdateStateByPlayer(playerTransform);
    }

    public void ExecuteCurrentState()
    {
        if (isReturning)
        {
            PerformReturn();
            return;
        }

        switch (currentState)
        {
            case MonsterState.Idle:
                movement.SetChasing(false); //  배회 모드
                movement.Stop();
                break;

            case MonsterState.Wandering:
                movement.SetChasing(false); //  배회 모드
                movement.MoveToPosition(moveTargetPosition, 1f);

                if (spawnManager.spawnAreaCollider != null)
                {
                    Vector2 currentPos = movement.GetPosition();
                    if (!spawnManager.IsInsideSpawnArea(currentPos))
                    {
                        moveTargetPosition = spawnManager.spawnPosition;
                    }
                }

                if (movement.GetDistanceTo(moveTargetPosition) <= 0.2f)
                    currentState = MonsterState.Idle;
                break;

            case MonsterState.Chasing:
                break;

            case MonsterState.Attacking:
                movement.Stop();
                break;
        }

        if (!isProvoked && !isReturning && currentState != MonsterState.Chasing)
            EnsureInsideSpawnArea();
    }

    private void UpdateStateByPlayer(Transform playerTransform)
    {
        float distanceToPlayer = combat.GetDistanceTo(playerTransform);

        //  공격/도발 몬스터만 처리
        if (isAggressive || isProvoked)
        {
            // ==========================
            //  현재 상태별 처리
            // ==========================
            switch (currentState)
            {
                case MonsterState.Idle:
                case MonsterState.Wandering:
                    if (distanceToPlayer <= detectionRange)
                    {
                        currentState = MonsterState.Chasing;
                        Debug.Log("[AI] 플레이어 감지 → 추적 시작");
                    }
                    break;

                case MonsterState.Chasing:
                    if (combat.IsInAttackRange(playerTransform))
                    {
                        //  공격 범위 진입 시 공격 상태로 전환
                        currentState = MonsterState.Attacking;
                        movement.Stop(); // 이동 완전 정지
                        if(originalIsAggressive)
                        {
                            SetProvoked();
                        }
                        Debug.Log("[AI] 공격 범위 진입 → 공격 상태 전환");
                    }
                    else
                    {
                        ChasePlayer(playerTransform);
                    }
                    break;

                case MonsterState.Attacking:
                    if (!combat.IsInAttackRange(playerTransform))
                    {
                        //  공격 범위 벗어남 → 다시 추적
                        currentState = MonsterState.Chasing;
                        Debug.Log("[AI] 공격 범위 이탈 → 추적 상태 전환");
                    }
                    else
                    {
                        if (combat.TryAttackPlayer(playerTransform))
                        {
                            Debug.Log($"[AI] {transform.name} → 플레이어 공격 실행");

                            //  공격 시 도발 시간 초기화
                            if (isProvoked)
                                provokedTimeRemaining = provokedDuration;
                        }
                    }
                    break;
                case MonsterState.Returning:
                    //귀환중일때
                    if(originalIsAggressive)
                    {
                        // 선공 몬스터는 귀환 중에도 플레이어 감지 시 추적 시작
                        if (combat.IsInAttackRange(playerTransform))
                        {
                            //  공격 범위 진입 시 공격 상태로 전환
                            currentState = MonsterState.Attacking;
                            movement.Stop(); // 이동 완전 정지
                            if (originalIsAggressive)
                            {
                                SetProvoked();
                            }
                            Debug.Log("[AI] 공격 범위 진입 → 공격 상태 전환");
                        }
                    }
                    //비선공 몬스터는 귀환 중 플레이어 감지 안 함
                    break;
            }
        }
        else
        {
            //  비공격형 몬스터 (도발도 안 됨)
            if (currentState == MonsterState.Attacking && !combat.IsInAttackRange(playerTransform))
                currentState = MonsterState.Wandering;
        }
    }


    private void ChasePlayer(Transform playerTransform)
    {
        Vector2 targetPosition = playerTransform.position;
        bool ignoreArea = isAggressive || isProvoked;

        //  추적 중임을 movement에 알림
        movement.SetChasing(true);

        float distanceToTarget = movement.GetDistanceTo(targetPosition);
        float attackRange = combat.preferredAttackDistance; // 몬스터 타입에 따라 다름
        float stopDistance = attackRange + 0.1f;            // 공격 개시 거리 (약간 여유)
        float resumeChaseDistance = attackRange + 0.3f;     // 공격 중 이 거리 넘으면 추적 재개

        //  공격 중일 때 — 일정 거리 이내면 공격 유지
        if (currentState == MonsterState.Attacking)
        {
            if (distanceToTarget > resumeChaseDistance)
            {
                // 너무 멀어지면 다시 추적 시작
                currentState = MonsterState.Chasing;
                Debug.Log($"[AI] {transform.name} → 공격 중 거리 {distanceToTarget:F2}, 추적으로 복귀");
            }
            else
            {
                // 아직 충분히 가까우면 공격 유지
                movement.Stop();

                //  공격 시 도발 시간 갱신 (비선공이 맞은 상태 유지)
                if (isProvoked)
                    provokedTimeRemaining = provokedDuration;

                // 플레이어가 공격 범위 안이면 계속 공격 시도
                if (combat.IsInAttackRange(playerTransform))
                {
                    if (combat.TryAttackPlayer(playerTransform))
                        Debug.Log($"[AI] {transform.name} → 공격 유지 중 플레이어 타격");
                }

                return;
            }
        }

        //  공격 거리 밖 — 추적 중
        if (distanceToTarget > stopDistance)
        {
            movement.MoveToPosition(targetPosition, 1.5f, ignoreArea);
            currentState = MonsterState.Chasing;
        }
        else
        {
            //  공격 범위 도달 — 이동 멈추고 공격 전환
            movement.Stop();
            currentState = MonsterState.Attacking;

            // 공격 직전 도발 시간 갱신 (비선공이 맞은 상태라면 유지)
            if (isProvoked)
                provokedTimeRemaining = provokedDuration;

            if (combat.IsInAttackRange(playerTransform))
            {
                if (combat.TryAttackPlayer(playerTransform))
                    Debug.Log($"[AI] {transform.name} → 플레이어 공격 시작");
            }
        }
    }


    private void EnsureInsideSpawnArea()
    {
        if (isProvoked)
            return;
        if (spawnManager.spawnAreaCollider == null) return;

        Vector2 currentPos = movement.GetPosition();

        if (!spawnManager.IsInsideSpawnArea(currentPos))
        {
            moveTargetPosition = spawnManager.spawnPosition;
            currentState = MonsterState.Wandering;

            Vector2 closest = spawnManager.GetClosestPointInSpawnArea(currentPos);
            movement.SmoothCorrection(closest, 1f);
        }
    }

    private void PerformReturn()
    {
        Vector2 currentPos = movement.GetPosition();
        Vector2 target = spawnManager.spawnPosition;

        movement.ignoreAreaLimit = true;

        if (spawnManager.spawnAreaCollider != null)
        {
            target = spawnManager.ClampToSpawnArea(target);
        }

        float dist = Vector2.Distance(currentPos, target);

        if (dist <= returnStopDistance)
        {
            isReturning = false;

            movement.Stop();
            movement.SmoothCorrection(target, 1f);

            // 상태 초기화
            isProvoked = false;
            movement.ignoreAreaLimit = false;

            // 비선공이면 공격/체이싱 상태 초기화
            if (!originalIsAggressive)
            {
                isAggressive = false;
                currentState = MonsterState.Idle;
            }
            else
            {
                // 선공은 기존 공격 상태 유지 가능
                isAggressive = true;
                // 필요시 currentState는 Chasing 혹은 Idle 판단
            }

            // 체력 회복
            var controller = transform.GetComponent<MonsterController>();
            controller?.RegenerateHealth();

            Debug.Log($"[AI] 귀환 완료 → Aggressive: {isAggressive}, Provoked: {isProvoked}");
            return;
        }

        movement.MoveToPosition(target, 1.0f, ignoreArea: true);
    }

    private void SetRandomMoveTarget()
    {
        moveTargetPosition = spawnManager.GetRandomMoveTarget();
    }

    public void SetProvoked()
    {
        isProvoked = true;
        movement.ignoreAreaLimit = true;
        provokedTimeRemaining = provokedDuration;

        if (isReturning)
            isReturning = false;

        isAggressive = true;

        if (detectionRange < 5f)
            detectionRange = 5f;

        if (currentState != MonsterState.Chasing && currentState != MonsterState.Attacking)
            currentState = MonsterState.Chasing;
    }

    private void StartReturnToSpawn()
    {
        if (isReturning) return;
        isReturning = true;
        currentState = MonsterState.Returning;

        Debug.Log("[MonsterAI] 스폰 위치로 귀환 시작");
        if(!originalIsAggressive)
        {
            isAggressive = false; // 비선공 몬스터는 도발 끝나면 공격 false
        }
        
        isProvoked = false;
        movement.ignoreAreaLimit = false;
    }

    private IEnumerator AIRoutine()
    {
        while (true)
        {
            if (isProvoked ||
                currentState == MonsterState.Chasing ||
                currentState == MonsterState.Attacking ||
                currentState == MonsterState.Returning)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            float randomDuration = Random.Range(minWaitTime, maxWaitTime);

            if (Random.value < idleProbability)
                currentState = MonsterState.Idle;
            else
            {
                SetRandomMoveTarget();
                currentState = MonsterState.Wandering;
            }

            yield return new WaitForSeconds(randomDuration);
        }
    }
}