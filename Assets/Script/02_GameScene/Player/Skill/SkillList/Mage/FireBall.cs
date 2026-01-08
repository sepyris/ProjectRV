using UnityEngine;

public class FireBall : ActiveSkillBase
{
    private const string PROJECTILE_PATH = "SkillsPrefabs/FireBallProjectile";
    private const float PROJECTILE_SPEED = 12f;
    private const float PROJECTILE_DISTANCE = 10f;

    public FireBall(SkillData data, int level = 1) : base(data, level)
    {
    }

    protected override void Execute(Transform caster, Vector3 targetPosition, Transform targetTransform)
    {
        PlayerStatsComponent playerStats = caster.GetComponent<PlayerStatsComponent>();
        PlayerMovement movement = caster.GetComponent<PlayerController>()?.movement;

        if (playerStats == null)
        {
            Debug.LogError("[FireBall] PlayerStatsComponent 없음");
            return;
        }

        // 바라보는 방향으로 발사
        Vector2 direction;
        if (movement != null)
        {
            direction = movement.LastMoveDirection;
        }
        else
        {
            direction = Vector2.right;  // fallback
        }

        // 데미지 계산
        float skillDamageRate = GetCurrentDamage();
        int damage = Mathf.FloorToInt(playerStats.Stats.attackPower * skillDamageRate / 100f);

        // 직진 발사체 생성
        SpawnProjectile(caster.position, direction, damage, playerStats.Stats);

        Debug.Log($"[FireBall] 파이어볼 발사! 방향: {direction}");
    }

    /// <summary>
    /// 발사체 생성
    /// </summary>
    private void SpawnProjectile(Vector3 position, Vector2 direction, int damage, CharacterStats stats)
    {
        GameObject prefab = Resources.Load<GameObject>(PROJECTILE_PATH);
        if (prefab == null)
        {
            Debug.LogError($"[FireBall] 프리팹을 찾을 수 없음: {PROJECTILE_PATH}");
            return;
        }

        GameObject projectileObj = Object.Instantiate(prefab, position, Quaternion.identity);
        PlayerProjectile projectile = projectileObj.GetComponent<PlayerProjectile>();

        if (projectile != null)
        {
            projectile.Initialize(
                damage,
                stats.criticalChance,
                stats.criticalDamage,
                stats.accuracy,
                PROJECTILE_DISTANCE
            );

            projectile.SetVelocity(direction * PROJECTILE_SPEED);
            projectile.SetIgnoreWalls(true);
        }
        else
        {
            Debug.LogError("[FireBall] PlayerProjectile 컴포넌트 없음");
            Object.Destroy(projectileObj);
        }
    }
}