using Definitions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    // ==================== Inspector Settings ====================
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Attack Settings")]
    [SerializeField] private PlayerAttack.AttackType attackType = PlayerAttack.AttackType.Melee;
    [SerializeField] private float attackDelay = 0.5f;
    [SerializeField] private float meleeRange = 1f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileMaxDistance = 10f;  // 발사체 최대 거리 설정

    // ==================== Components ====================
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // ==================== Modules ====================
    private PlayerMovement movement;
    private PlayerAttack attack;
    private PlayerInteraction interaction;
    private PlayerAnimationController animationController;
    private PlayerSaveLoad saveLoad;
    private PlayerBoundaryLimiter boundaryLimiter;
    private DamageTextSpawner damageTextSpawner;

    // ==================== New Input System ====================
    private PlayerControls playerControls;
    private InputAction moveAction;

    // ==================== State Variables ====================
    private Vector2 moveInput;
    private bool controlsLocked = false;

    // ==================== Properties ====================
    public bool ControlsLocked => controlsLocked;
    public bool IsAttacking => attack != null && attack.IsAttacking;
    public bool IsGathering => interaction != null && interaction.IsGathering;
    public Vector2 MoveInput => moveInput;

    // ==================== Unity Lifecycle ====================
    void Awake()
    {
        HandleSingleton();
        InitializeInputSystem();
        
    }

    void Start()
    {
        InitializeComponents();
        InitializeModules();
        CheckInitialScene();
        
    }

    void OnEnable()
    {
        playerControls?.Enable();
    }

    void OnDisable()
    {
        playerControls?.Disable();
    }

    void OnDestroy()
    {
        CleanupInputSystem();
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void Update()
    {
        if (controlsLocked || IsLoadingActive())
            return;

        // 상호작용 대상 업데이트 (E키는 Input Actions로 처리)
        if (!IsAttacking && !IsGathering)
        {
            interaction?.UpdateNearestInteractable();
        }

        UpdateMovement();
    }

    void FixedUpdate()
    {
        if (rb == null || controlsLocked || IsLoadingActive())
        {
            if (rb != null)
                rb.velocity = Vector2.zero;
            return;
        }

        // 공격 중이거나 채집 중일 때 이동 불가
        if (IsAttacking || IsGathering)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        ApplyMovement();
    }

    // ==================== Initialization ====================
    private void HandleSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (this.gameObject != Instance.gameObject)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Destroy(gameObject);
        }
    }

    private void InitializeInputSystem()
    {
        playerControls = new PlayerControls();

        // Movement
        moveAction = playerControls.Player.Move;
        moveAction.performed += OnMovePerformed;
        moveAction.canceled += OnMoveCanceled;

        // Attack
        playerControls.Player.Attack.performed += OnAttackPerformed;

        // Interact
        playerControls.Player.Interact.performed += OnInteractPerformed;

        // UI Toggles
        playerControls.Player.ToggleQuest.performed += OnToggleQuestPerformed;
        playerControls.Player.ToggleInventory.performed += OnToggleInventoryPerformed;
        playerControls.Player.ToggleStats.performed += OnToggleStatsPerformed;
        playerControls.Player.ToggleEquipment.performed += OnToggleEquipmentPerformed;

        // Cancel (Close Top UI)
        playerControls.Player.Cancel.performed += OnCancel;
    }

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void InitializeModules()
    {
        // Movement
        movement = new PlayerMovement(rb);
        movement.MoveSpeed = moveSpeed;

        // Animation
        if (animator != null)
        {
            Debug.Log($"[PlayerController] Animator found: {animator.name}");
            Debug.Log($"[PlayerController] Animator Controller: {animator.runtimeAnimatorController?.name}");
            animationController = new PlayerAnimationController(animator);
        }
        else
        {
            Debug.LogError("[PlayerController] Animator is NULL!");
        }

        damageTextSpawner = GetComponent<DamageTextSpawner>();

        // Attack
        attack = new PlayerAttack();
        attack.attackType = attackType;
        attack.attackDelay = attackDelay;
        attack.meleeRange = meleeRange;
        attack.projectilePrefab = projectilePrefab;
        attack.projectileSpeed = projectileSpeed;
        attack.projectileMaxDistance = projectileMaxDistance;  // 발사체 최대 거리 전달
        attack.SetMovement(movement);
        attack.SetAnimationController(animationController);

        if (PlayerStatsComponent.Instance != null)
        {
            attack.attackDamage = PlayerStatsComponent.Instance.Stats.attackPower;
            attack.criticalChance = PlayerStatsComponent.Instance.Stats.criticalChance;
            attack.criticalDamage = PlayerStatsComponent.Instance.Stats.criticalDamage;
            attack.accuracy = PlayerStatsComponent.Instance.Stats.accuracy;
            if (damageTextSpawner != null)
            {
                PlayerStatsComponent.Instance.SetDamageTextSpawner(damageTextSpawner);
            }

        }

        // Interaction
        interaction = new PlayerInteraction(transform, animationController);

        // Save/Load
        saveLoad = new PlayerSaveLoad(transform);

        // Boundary
        BoxCollider2D playerCollider = GetComponent<BoxCollider2D>();
        boundaryLimiter = new PlayerBoundaryLimiter(rb, playerCollider);

        // 장비 데이터 로드
        EquipmentManager.Instance?.LoadFromSaveData(CharacterSaveManager.Instance?.CurrentCharacter.equipmentData);
    }

    private void CheckInitialScene()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.name.StartsWith(Def_Name.SCENE_NAME_START_MAP))
        {
            saveLoad?.InitializePlayerState(active.name);
        }
    }

    private void CleanupInputSystem()
    {
        if (moveAction != null)
        {
            moveAction.performed -= OnMovePerformed;
            moveAction.canceled -= OnMoveCanceled;
        }

        if (playerControls != null)
        {
            playerControls.Player.Attack.performed -= OnAttackPerformed;
            playerControls.Player.Interact.performed -= OnInteractPerformed;
            playerControls.Player.ToggleQuest.performed -= OnToggleQuestPerformed;
            playerControls.Player.ToggleInventory.performed -= OnToggleInventoryPerformed;
            playerControls.Player.ToggleStats.performed -= OnToggleStatsPerformed;
            playerControls.Player.ToggleEquipment.performed -= OnToggleEquipmentPerformed;
            playerControls.Player.Cancel.performed -= OnCancel;
        }
    }

    // ==================== Input Callbacks ====================
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        if (!controlsLocked && !IsLoadingActive())
        {
            moveInput = context.ReadValue<Vector2>();
        }
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        // Value 타입에서는 모든 키를 뗐을 때만 canceled 호출됨
        moveInput = Vector2.zero;

        if (movement != null)
        {
            movement.currentInput = Vector2.zero;
        }
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (!controlsLocked && !IsLoadingActive() && !IsAttacking && !IsGathering)
        {
            // 공격 애니메이션 재생
            if (animationController != null)
            {
                animationController.PlayAttackAnimation();
            }

            attack?.PerformAttack();
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (!IsAttacking && !IsGathering)
        {
            // 일반 상호작용 (채집, NPC)
            interaction?.TryInteract();

            // SavePoint 체크
            Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, 0.5f);
            foreach (var col in nearbyColliders)
            {
                SavePointController savePoint = col.GetComponent<SavePointController>();
                if (savePoint != null)
                {
                    savePoint.TryInteract();
                    break; // 하나만 처리
                }
            }
        }
    }

    private void OnToggleQuestPerformed(InputAction.CallbackContext context)
    {
        // 대화 중이면 퀘스트 창 열지 않음
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
            return;

        ToggleQuestUI();
    }

    private void OnToggleInventoryPerformed(InputAction.CallbackContext context)
    {
        // 대화 중이면 인벤토리 창 열지 않음
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
            return;

        ToggleInventoryUI();
    }

    private void OnToggleStatsPerformed(InputAction.CallbackContext context)
    {
        // 대화 중이면 스탯 창 열지 않음
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
            return;

        ToggleStatsUI();
    }

    private void OnToggleEquipmentPerformed(InputAction.CallbackContext context)
    {
        // 대화 중이면 장비창 열지 않음
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
            return;

        ToggleEquipmentUI();
    }
    private void OnCancel(InputAction.CallbackContext context)
    {

        if (context.performed)
        {
            // UI가 열려있으면 맨 위 UI 닫기
            if (PlayerHUD.Instance?.IsAnyUIOpen() == true)
            {
                PlayerHUD.Instance.CloseTopUI();
            }
            else
            {
                // 일시정지 토글
                PauseMenuUIManager.Instance?.TogglePause();
            }
        }
    }
    // ==================== Movement & Actions ====================
    private void UpdateMovement()
    {
        // 공격 중이거나 채집 중일 때 이동 입력 무시
        if (movement != null && !IsAttacking && !IsGathering)
        {
            movement.currentInput = moveInput;
            if (moveInput.magnitude > 0.01f)
            {
                movement.SetLastDirection(moveInput);
            }

            // 애니메이션 업데이트
            if (animationController != null)
            {
                animationController.UpdateMovementAnimation(moveInput, movement.MoveSpeed);
            }
        }
        else if (movement != null)
        {
            // 공격/채집 중에는 입력을 0으로 설정
            movement.currentInput = Vector2.zero;

            // 정지 애니메이션
            if (animationController != null)
            {
                animationController.UpdateMovementAnimation(Vector2.zero, 0);
            }
        }
    }

    private void ApplyMovement()
    {
        movement?.ApplyMovement();
        boundaryLimiter?.ApplyBoundaryLimit();
    }

    // ==================== UI Management ====================
    private void ToggleQuestUI()
    {
        if (QuestUIManager.Instance == null) return;

        if (QuestUIManager.Instance.IsQuestUIOpen())
            QuestUIManager.Instance.CloseQuestUI();
        else
            QuestUIManager.Instance.OpenQuestUI();
    }

    private void ToggleInventoryUI()
    {
        if (ItemUIManager.Instance == null) return;

        if (ItemUIManager.Instance.IsItemUIOpen())
            ItemUIManager.Instance.CloseItemUI();
        else
            ItemUIManager.Instance.OpenItemUI();
    }

    private void ToggleStatsUI()
    {
        if (CharacterStatUIManager.Instance == null) return;

        if (CharacterStatUIManager.Instance.IsStatsUIOpen())
            CharacterStatUIManager.Instance.CloseStatUI();
        else
            CharacterStatUIManager.Instance.OpenStatUI();
    }
    private void ToggleEquipmentUI()
    {
        if (EquipmentUIManager.Instance == null) return;

        if (EquipmentUIManager.Instance.IsEquipmentUIOpen())
            EquipmentUIManager.Instance.CloseEquipmentUI();
        else
            EquipmentUIManager.Instance.OpenEquipmentUI();
    }

    // ==================== Scene Management ====================
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[PlayerController] OnSceneLoaded: {scene.name}");

        // 캐릭터 선택 화면으로 돌아가는 경우
        if (scene.name == Def_Name.SCENE_NAME_CHARACTER_SELECT_SCENE)
        {
            Debug.Log("[PlayerController] 캐릭터 선택창 진입 - 플레이어 비활성화");
            this.gameObject.SetActive(false);
            return;
        }

        // 게임 씬으로 진입
        if (scene.name.StartsWith(Definitions.Def_Name.SCENE_NAME_START_MAP))
        {
            Debug.Log("[PlayerController] 게임 씬 진입 - 플레이어 컨트롤러 재활성화");

            // 게임 오브젝트 활성화 확인
            if (!this.gameObject.activeSelf)
            {
                this.gameObject.SetActive(true);
            }

            // 입력 시스템 강제 재활성화
            if (playerControls != null)
            {
                playerControls.Disable();
                playerControls.Enable();
                Debug.Log("[PlayerController] 입력 시스템 재활성화");
            }

            // 플레이어 위치 복원
            string sceneName = scene.name;

            //  물리 상태만 초기화 (입력은 나중에 복원)
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }

            // 카메라 재초기화
            CameraController cameraController = Camera.main?.GetComponent<CameraController>();
            if (cameraController != null)
            {
                cameraController.ReInitialize();
            }

            // 컨트롤 확실히 해제
            SetControlsLocked(false);

            //  씬 전환 후 현재 입력 상태 복원
            StartCoroutine(RestoreInputStateAfterSceneLoad());

            Debug.Log("[PlayerController] 컨트롤 잠금 해제 완료");
        }
    }

    private IEnumerator RestoreInputStateAfterSceneLoad()
    {
        while (IsLoadingActive())
        {
            yield return null;
        }
        // 한 프레임 대기 (씬이 완전히 로드되도록)
        yield return null;

        // 현재 눌려있는 입력을 다시 읽어옴
        if (moveAction != null && !controlsLocked)
        {
            Vector2 currentInput = moveAction.ReadValue<Vector2>();

            if (currentInput.magnitude > 0.1f)
            {
                moveInput = currentInput;
                if (movement != null)
                {
                    movement.currentInput = currentInput;
                }
                Debug.Log($"[PlayerController] 씬 전환 후 입력 복원: {currentInput}");
            }
            else
            {
                moveInput = Vector2.zero;
                if (movement != null)
                {
                    movement.currentInput = Vector2.zero;
                }
            }
        }
    }

    // ==================== Public Methods ====================
    public void SetControlsLocked(bool locked)
    {
        controlsLocked = locked;

        if (movement != null) movement.ControlsLocked = locked;
        if (attack != null) attack.ControlsLocked = locked;
        if (interaction != null) interaction.ControlsLocked = locked;

        if (locked && rb != null)
        {
            rb.velocity = Vector2.zero;
            moveInput = Vector2.zero;
        }
    }
    public void SetActionControlsLocked(bool locked)
    {
        controlsLocked = locked;

        if (movement != null) movement.ControlsLocked = locked;
        if (attack != null) attack.ControlsLocked = locked;

        if (locked && rb != null)
        {
            rb.velocity = Vector2.zero;
            moveInput = Vector2.zero;
        }
    }

    public void PlayAnimation(string triggerName)
    {
        animationController?.PlayAnimation(triggerName);
    }

    public void PlayDeathAnimation()
    {
        animationController?.PlayDeathAnimation();
    }

    public void PlayHitAnimation()
    {
        animationController?.PlayHitAnimation();
    }

    public void SetIdleAnimation()
    {
        animationController?.SetIdle();
    }

    public void UpdateStats()
    {
        if (attack != null)
        {
            // Attack
            attack.meleeRange = meleeRange;
            attack.projectilePrefab = projectilePrefab;
            attack.projectileSpeed = projectileSpeed;
            attack.projectileMaxDistance = projectileMaxDistance;  // 발사체 최대 거리 업데이트

            if (PlayerStatsComponent.Instance != null)
            {

                attack.attackDamage = PlayerStatsComponent.Instance.Stats.attackPower;
                attack.criticalChance = PlayerStatsComponent.Instance.Stats.criticalChance;
                attack.criticalDamage = PlayerStatsComponent.Instance.Stats.criticalDamage;
                attack.accuracy = PlayerStatsComponent.Instance.Stats.accuracy;
                if (damageTextSpawner != null)
                {
                    PlayerStatsComponent.Instance.SetDamageTextSpawner(damageTextSpawner);
                }
            }
        }
    }
    public void SetAttackType(EquipmentSlot slot)
    {
        if (attack != null && (slot == EquipmentSlot.MeleeWeapon || slot == EquipmentSlot.RangedWeapon))
        {
            if (slot == EquipmentSlot.MeleeWeapon)
                attack.attackType = PlayerAttack.AttackType.Melee;
            else
                attack.attackType = PlayerAttack.AttackType.Ranged;
        }
    }

    // 8방향 시스템 적용
    public void SetFacingDirection(Vector2 direction)
    {
        if (direction.magnitude < 0.01f) return;

        // 8방향으로 정규화 (45도 단위)
        Vector2 dir8 = GetDirection8Way(direction);

        if (movement != null)
            movement.SetLastDirection(dir8);

        // 스프라이트 좌우 반전 (왼쪽/오른쪽 방향일 때만)
        if (spriteRenderer != null && Mathf.Abs(dir8.x) > 0.1f)
        {
            spriteRenderer.flipX = dir8.x < 0;
        }
    }

    // 8방향으로 변환하는 헬퍼 메서드
    private Vector2 GetDirection8Way(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 8방향 각도 (0, 45, 90, 135, 180, -135, -90, -45)
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;

        // 각도를 벡터로 변환
        float rad = snappedAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    public void SaveStateBeforeDeactivation()
    {
        saveLoad?.SaveStateBeforeDeactivation();
    }

    // ==================== Helper Methods ====================
    private bool IsLoadingActive()
    {
        return LoadingScreenManager.Instance != null && LoadingScreenManager.Instance.IsLoading;
    }

    private void OnDrawGizmos()
    {
        if (interaction != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interaction.getinteractRadius());
        }
    }
}