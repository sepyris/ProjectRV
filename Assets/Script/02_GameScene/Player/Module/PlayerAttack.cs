using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack
{
    public enum AttackType { Melee, Ranged }
    public AttackType attackType = AttackType.Melee;
    public float attackDelay = 0.5f;
    public int attackDamage = 10;
    public float criticalChance = 5f;
    public float criticalDamage = 150f;
    public float accuracy = 10f;

    //  근접 공격 설정 (부채꼴 범위)
    public float meleeRange = 1f;           // 공격 거리(거리는 플레이어 컨트롤러에서 설정, 없을시 기본값)
    public float meleeAngle = 90;          // 공격 각도 (좌우 ±50도씩 총 100도)
    public int meleeRayCount = 5;           // 범위 감지용 레이 개수 (많을수록 정밀)


    //  검 휘두르기 효과 설정
    public bool useSwingEffect = false;      // 검 휘두르기 효과 사용 여부
    public float swingDuration = 0.5f;      // 휘두르는 시간
    public AnimationCurve swingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 휘두르기 속도 곡선

    //  원거리 공격 설정
    public GameObject projectilePrefab;
    public float projectileSpeed = 8f;
    public float projectileMaxDistance = 10f;  // 발사체 최대 거리 (PlayerController에서 설정)

    public bool ControlsLocked = false;

    private bool isAttacking = false;
    public bool IsAttacking => isAttacking;
    private float lastAttackTime = -999f;

    private HashSet<MonsterController> hitMonstersThisAttack = new HashSet<MonsterController>(); // 이번 공격에 맞은 몬스터

    private PlayerMovement movement;
    private PlayerAnimationController animationController;

    public void SetMovement(PlayerMovement movement) => this.movement = movement;
    public void SetAnimationController(PlayerAnimationController anim) => this.animationController = anim;


    private const string MELEE_EFFECT_PATH = "SkillsPrefabs/NormalAttack";

    // New Input System
    public void PerformAttack()
    {
        if (ControlsLocked || isAttacking) return;

        if (Time.time - lastAttackTime >= attackDelay)
        {
            movement?.ApplyMovement();
            animationController?.PlayAnimation("Attack");

            hitMonstersThisAttack.Clear(); // 공격 시작 시 초기화

            // 즉시 공격 판정
            Attack();

            lastAttackTime = Time.time;
            isAttacking = true;
            PlayerController.Instance.StartCoroutine(ResetAttack());
        }
    }

    private void Attack()
    {
        if (attackType == AttackType.Melee)
            MeleeAttack();
        else
            RangedAttack();
    }

    private void MeleeAttack()
    {
        Vector2 attackDirection = movement.LastMoveDirection.normalized;
        Vector2 playerPos = PlayerController.Instance.transform.position;

        // ===== 이펙트를 먼저 표시 (공격 시도 시 항상) =====

        Vector3 effectPosition = playerPos + (Vector2)attackDirection * 0.7f;
        float effectAngle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;

        bool isFacingLeft = attackDirection.x < 0;

        SpawnMeleeEffect(effectPosition, effectAngle, isFacingLeft);

        // ===== 오버랩 방식으로 몬스터 탐지 =====

        Collider2D[] hits = Physics2D.OverlapCircleAll(playerPos, meleeRange, LayerMask.GetMask("Monster"));

        MonsterController closestMonster = null;
        float closestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            MonsterController monster = hit.GetComponent<MonsterController>();
            if (monster != null && !monster.IsDead())
            {
                // 각도 체크 (부채꼴 범위)
                Vector2 toMonster = (hit.transform.position - (Vector3)playerPos).normalized;
                float angle = Vector2.Angle(attackDirection, toMonster);

                if (angle <= meleeAngle / 2f)
                {
                    float distance = Vector2.Distance(playerPos, hit.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestMonster = monster;
                    }
                }
            }
        }

        // ===== 가장 가까운 몬스터 공격 =====

        if (closestMonster != null && !hitMonstersThisAttack.Contains(closestMonster))
        {
            // 데미지 계산
            float damageVariance = Random.Range(0.8f, 1.2f);
            int damage = Mathf.RoundToInt(attackDamage * damageVariance);

            // 크리티컬 체크
            bool isCritical = false;
            if (Random.Range(0f, 100f) <= criticalChance)
            {
                damage = (int)(damage * (criticalDamage / 100f));
                isCritical = true;
            }

            // 데미지 적용
            closestMonster.TakeDamage(damage, isCritical);
            hitMonstersThisAttack.Add(closestMonster);

            Debug.Log($"[PlayerAttack] {closestMonster.GetMonsterName()} 공격 성공! (거리: {closestDistance:F2})");
        }
        else
        {
            Debug.Log("[PlayerAttack] 빗나감!");
        }
    }

    private void SpawnMeleeEffect(Vector3 position, float angle, bool flipY = false)
    {
        GameObject prefab = Resources.Load<GameObject>(MELEE_EFFECT_PATH);
        if (prefab != null)
        {
            GameObject effect = Object.Instantiate(prefab, position, Quaternion.Euler(0, 0, angle));
            if (flipY)
            {
                Vector3 scale = effect.transform.localScale;
                scale.y *= -1;
                effect.transform.localScale = scale;
            }
            // 자동 삭제 처리
            Animator animator = effect.GetComponent<Animator>();
            if (animator != null)
            {
                // 애니메이션 길이만큼 대기 후 삭제
                float destroyTime = GetAnimationLength(animator);
                Object.Destroy(effect, destroyTime);
            }
            else
            {
                // 애니메이션 없으면 0.5초 후 삭제
                Object.Destroy(effect, 0.5f);
            }
        }
        else
        {
            Debug.LogWarning($"[PlayerAttack] 이펙트 프리팹을 찾을 수 없음: {MELEE_EFFECT_PATH}");
        }
    }

    /// <summary>
    /// 애니메이션 길이 가져오기
    /// </summary>
    private float GetAnimationLength(Animator animator)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return 0.5f;

        // 첫 번째 애니메이션 클립의 길이 반환
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller.animationClips.Length > 0)
        {
            return controller.animationClips[0].length;
        }

        return 0.5f;
    }


    /// 벡터 회전 유틸리티

    private Vector2 RotateVector(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

    
    /// 부채꼴 범위 시각화 (디버그용)
    
    private void DrawArcGizmo(Vector2 origin, Vector2 direction, float range, float angle)
    {
        float halfAngle = angle / 2f;
        Vector2 leftBound = RotateVector(direction, -halfAngle);
        Vector2 centerBound = RotateVector(direction, 0);
        Vector2 rightBound = RotateVector(direction, halfAngle);

        Debug.DrawRay(origin, leftBound * range, Color.green, 0.3f);
        Debug.DrawRay(origin, centerBound * range, Color.green, 0.3f);
        Debug.DrawRay(origin, rightBound * range, Color.green, 0.3f);
    }

    private void RangedAttack()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[PlayerAttack] projectilePrefab이 할당되지 않았습니다!");
            return;
        }

        // 발사 위치 계산 (플레이어 위치 + 방향 * 오프셋)
        Vector2 playerPos = (Vector2)PlayerController.Instance.transform.position;
        Vector2 direction = movement.LastMoveDirection.normalized;
        Vector2 spawnPosition = playerPos + direction * 0.5f;

        // 발사체 생성
        GameObject projectileObj = GameObject.Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

        // PlayerProjectile 컴포넌트 가져오기
        PlayerProjectile projectile = projectileObj.GetComponent<PlayerProjectile>();

        if (projectile != null)
        {
            // 발사체 초기화 (데미지, 크리티컬 정보, 최대 거리 전달)
            projectile.Initialize(Mathf.FloorToInt(attackDamage * 1.2f), criticalChance, criticalDamage, accuracy, projectileMaxDistance);

            // 속도 설정
            projectile.SetVelocity(direction * projectileSpeed);

            Debug.Log($"[PlayerAttack] 발사체 발사! 방향: {direction}, 속도: {projectileSpeed}, 최대 거리: {projectileMaxDistance}");
        }
        else
        {
            // PlayerProjectile 컴포넌트가 없는 경우 기본 동작
            Debug.LogWarning("[PlayerAttack] projectilePrefab에 PlayerProjectile 컴포넌트가 없습니다. 기본 발사체로 작동합니다.");

            Rigidbody2D rb = projectileObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = direction * projectileSpeed;
            }

            // 일정 시간 후 자동 파괴 (PlayerProjectile이 없을 경우 대비)
            GameObject.Destroy(projectileObj, 1f);
        }
    }

    private IEnumerator ResetAttack()
    {
        yield return new WaitForSeconds(attackDelay);
        animationController?.PlayAnimation("Idle");
        isAttacking = false;
    }

}