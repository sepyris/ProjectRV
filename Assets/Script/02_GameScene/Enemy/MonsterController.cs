using Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonsterController : MonoBehaviour
{
    // ==================== Inspector Settings ====================
    [Header("Monster id")]
    [SerializeField] private string monsterid;

    // ==================== Components ====================
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private GameObject rangeAttackProjectile;
    private TextMeshProUGUI monsterNameText;
    private GameObject SliderContainer;
    private Slider hpSlider;

    // ==================== Modules ====================
    private MonsterMovement movement;
    private MonsterAI ai;
    private MonsterCombat combat;
    private MonsterSpawnManager spawnManager;
    private DamageTextSpawner damageTextSpawner;

    // ==================== State Variables ====================
    private CharacterStats stats;
    private bool isDead = false;
    // ==================== Data ====================
    private MonsterData monsterData;
    // ==================== Other ====================
    private Transform playerTransform;
    private MonsterSpawnArea parentSpawnArea;
    private Collider2D cachedSpawnAreaCollider;

    

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Start()
    {
        LoadMonsterData();
        InitializeModules();

        if (cachedSpawnAreaCollider != null)
        {
            ApplySpawnArea(cachedSpawnAreaCollider);
        }

        FindPlayer();
        SetupIgnorePlayerCollision();
        UpdateNameDisplay();
    }

    void Update()
    {
        if (isDead) return;

        ai?.UpdateAI(playerTransform);

        if (stats != null)
        {
            float hp_percent = (float)stats.currentHP / stats.maxHP;
            if (hp_percent >= 0.99f)
            {
                SliderContainer.SetActive(false);
            }
            else
            {
                SliderContainer.SetActive(true);
            }
            UpdateHPProgress(hp_percent);
        }
    }

    void FixedUpdate()
    {
        if (isDead)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        ai?.ExecuteCurrentState();
    }

    public void SetMonsterid(string id)
    {
        monsterid = id;
        LoadMonsterData();
        InitializeModules();
    }

    private void LoadMonsterData()
    {
        if (string.IsNullOrEmpty(monsterid))
        {
            Debug.LogWarning("[MonsterController] 몬스터 id가 설정되지 않았습니다.");
            return;
        }

        if (MonsterDataManager.Instance == null)
        {
            Debug.LogError("[MonsterController] MonsterDataManager가 없습니다!");
            return;
        }

        monsterData = MonsterDataManager.Instance.GetMonsterData(monsterid);

        if (monsterData == null)
        {
            Debug.LogError($"[MonsterController] 몬스터 데이터를 찾을 수 없음: {monsterid}");
            return;
        }
        stats = MonsterBalanceCalculator.CalculateMonsterStats(monsterData);

        Debug.Log($"[Monster] 생성: {stats.characterName} Lv.{stats.level} ({monsterData.monsterType}) HP:{stats.maxHP} ATK:{stats.attackPower} DEF:{stats.defense}");
    }

    private void InitializeModules()
    {
        if (monsterData == null || rb == null || stats == null) return;

        damageTextSpawner = GetComponent<DamageTextSpawner>();
        if (damageTextSpawner != null)
        {
            stats.SetDamageTextSpawner(damageTextSpawner);
        }

        movement = new MonsterMovement(rb, transform);
        movement.moveSpeed = monsterData.moveSpeed;

        combat = new MonsterCombat(transform, this);
        combat.attackCooldown = 1f / monsterData.attackSpeed;
        combat.rangedAttackRange = monsterData.detectionRange; // 감지범위를 원거리 공격 범위로 사용
        combat.canRangedAttack = monsterData.isRanged;
        combat.projectilePrefab = rangeAttackProjectile;


        spawnManager = new MonsterSpawnManager(transform);

        ai = new MonsterAI(transform, movement, combat, spawnManager, monsterData.isAggressive);
        ai.detectionRange = monsterData.detectionRange;

        Debug.Log($"[Monster] 모듈 초기화: {stats.characterName}");
    }

    public void SetSpawnArea(MonsterSpawnArea spawnArea)
    {
        parentSpawnArea = spawnArea;

        if (spawnArea != null)
        {
            Collider2D spawnCollider = spawnArea.GetComponent<Collider2D>();
            if (spawnCollider != null)
            {
                cachedSpawnAreaCollider = spawnCollider;
            }
        }
    }

    private void ApplySpawnArea(Collider2D spawnCollider)
    {
        if (spawnManager != null && spawnCollider != null)
        {
            spawnManager.SetSpawnArea(spawnCollider);
        }
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag(Def_Name.PLAYER_TAG);
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void SetupIgnorePlayerCollision()
    {
        if (playerTransform == null) return;

        Collider2D playerCol = playerTransform.GetComponent<Collider2D>();
        if (playerCol == null) return;

        Collider2D[] myCols = GetComponentsInChildren<Collider2D>();
        foreach (var c in myCols)
        {
            if (c != null)
            {
                Physics2D.IgnoreCollision(c, playerCol, true);
            }
        }
    }

    public int TakeDamage(int damage,bool is_critical, float attackerAccuracy = 100f)
    {
        if (isDead || stats == null) return 0;
        int actualDamage = stats.TakeDamage(damage, is_critical, attackerAccuracy);

        if (ai != null && stats.currentHP > 0)
        {
            ai.SetProvoked();
        }

        if (stats.currentHP <= 0)
        {
            Die();
        }
        return actualDamage;
    }

    public void RegenerateHealth()
    {
        if (stats == null || isDead) return;

        int oldHP = stats.currentHP;
        stats.FullRecover();

        Debug.Log($"[Monster] {stats.characterName} 체력 회복: {oldHP} → {stats.currentHP}");
    }

    public int GetAttackPower(ref bool is_critical)
    {
        if (stats == null || isDead) return 0;

        float damageVariance = Random.Range(0.8f, 1.2f); // 80% ~ 120%
        int damage = Mathf.RoundToInt(stats.attackPower * damageVariance);

        if (Random.Range(0f, 100f) <= stats.criticalChance)
        {
            damage = Mathf.RoundToInt(damage * (stats.criticalDamage / 100f));
            Debug.Log($"[Monster] {stats.characterName} 크리티컬!");
            is_critical = true;
        }

        return damage;
    }

    public float GetAccuracy()
    {
        if (stats == null) return 70f;
        return stats.accuracy;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[Monster] {stats.characterName} 사망!");
        DropItems();

        if (playerTransform != null)
        {
            var playerStatsComponent = playerTransform.GetComponent<PlayerStatsComponent>();
            if (playerStatsComponent != null)
            {
                playerStatsComponent.Stats.GainExperience(monsterData.dropExp);
                if (monsterData.dropExp > 0)
                {
                    FloatingItemManager.Instance?.ShowItemMessage("EXP", monsterData.dropExp);
                }
                playerStatsComponent.Stats.AddGold(monsterData.dropGold);
                if (monsterData.dropGold > 0)
                {
                    FloatingItemManager.Instance?.ShowItemMessage("Gold", monsterData.dropGold);
                }
            }
        }

        

        // ===== 수정: this.gameObject 전달 =====
        if (parentSpawnArea != null)
        {
            parentSpawnArea.OnMonsterDied(this.gameObject);
        }
        // ====================================

        Destroy(gameObject, 0.5f);
    }

    private void DropItems()
    {
        if (monsterData.dropItems == null || monsterData.dropItems.Count == 0) return;

        foreach (var reward in monsterData.dropItems)
        {
            float roll = Random.Range(0f, 100f);
            if (roll <= reward.dropRate)
            {
                InventoryManager.Instance?.AddItem(reward.itemId, reward.quantity);
                FloatingItemManager.Instance?.ShowItemAcquired(ItemDataManager.Instance.GetItemData(reward.itemId), reward.quantity);
                Debug.Log($"[Monster] 아이템 드롭: {reward.itemId} x{reward.quantity}");
            }
        }
    }

    private void UpdateNameDisplay()
    {
        if (monsterNameText != null && stats != null)
        {
            string colorCode = "#FFFFFF";
            switch (monsterData.monsterType)
            {
                case MonsterType.Normal:
                    colorCode = "#FFFFFF";
                    break;
                case MonsterType.Elite:
                    colorCode = "#FFD700";
                    break;
                case MonsterType.Boss:
                    colorCode = "#FF4500";
                    break;
            }

            monsterNameText.text = $"<color={colorCode}>{stats.characterName} Lv.{stats.level}</color>";
        }
    }

    private void UpdateHPProgress(float percent)
    {
        if (hpSlider != null)
        {
            hpSlider.value = percent;
        }
    }

    public MonsterData GetMonsterData() => monsterData;
    public bool IsDead() => isDead;
    public CharacterStats GetStats() => stats;
    public MonsterAI GetAI() => ai;
    public MonsterCombat GetCombat() => combat;
    public MonsterMovement GetMovement() => movement;
    public string GetMonsterName() => stats?.characterName ?? "Unknown";

    void OnDrawGizmosSelected()
    {
        if (monsterData == null) return;

        if (monsterData.isAggressive)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, monsterData.detectionRange);
        }

        if (combat != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, combat.attackRange);
        }
    }
}