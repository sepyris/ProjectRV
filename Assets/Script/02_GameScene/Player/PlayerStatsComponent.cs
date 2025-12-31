using Definitions;
using System.Data;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.SceneManagement;


/// 플레이어 스탯 컴포넌트
/// PlayerController에 붙여서 사용
/// UI 업데이트를 직접 호출하는 방식으로 변경됨

public class PlayerStatsComponent : MonoBehaviour
{
    public static PlayerStatsComponent Instance { get; private set; }

    [Header("스탯 시스템")]
    public CharacterStats Stats = new CharacterStats();

    [Header("초기 설정")]
    [SerializeField] private string playerName = "Hero";
    [SerializeField] private int startLevel = 1;

    [Header("UI 연결")]
    [SerializeField] private PlayerStatusUIManager statusUI;
    DamageTextSpawner damageTextSpawner;

    private bool isFirstLoad = true; //  첫 로드 여부 플래그
    private string lastLoadedCharacterId = ""; //  마지막 로드한 캐릭터 ID

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (this.gameObject != Instance.gameObject)
        {
            Destroy(gameObject);
        }

        //  씬 로드 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (Stats == null)
        {
            Stats = new CharacterStats();
            Stats.SetDamageTextSpawner(damageTextSpawner);
        }

        Debug.Log("[PlayerStats] Awake - Stats 객체 생성");
    }

    void Start()
    {
        Debug.Log("[PlayerStats] Start - 스텟 로드 시작");

        //  최초 한 번만 로드 또는 캐릭터가 변경된 경우
        if (ShouldReloadStats())
        {
            LoadCharacterStats();
            UpdateLastLoadedCharacter();
            isFirstLoad = false;
        }

        SubscribeToEvents();

        if (statusUI == null)
        {
            statusUI = FindObjectOfType<PlayerStatusUIManager>();
        }

        if (statusUI != null)
        {
            statusUI.RefreshStatsReference();
            statusUI.UpdateAllUI();
        }
        if (CharacterStatUIManager.Instance != null)
        {
            CharacterStatUIManager.Instance.RefreshStatsReference();
        }

        Debug.Log($"[PlayerStats] Start 완료 - {Stats.characterName} Lv.{Stats.level}");
    }

    void OnDestroy()
    {
        //  이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeFromEvents();
    }

    
    /// 씬 로드 이벤트 핸들러
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[PlayerStats] OnSceneLoaded: {scene.name}");

        //  캐릭터 선택창으로 돌아가는 경우 
        if (scene.name == Def_Name.SCENE_NAME_CHARACTER_SELECT_SCENE)
        {
            Debug.Log("[PlayerStats] 캐릭터 선택창 진입 - 다음 게임 로딩 시 재로드 준비");
            //  다음 게임 씬 진입 시 데이터를 다시 로드하도록 플래그 설정
            isFirstLoad = true;
            return;
        }

        //  게임 씬으로 진입하는 경우 (캐릭터 전환 확인) 
        if (scene.name.StartsWith(Definitions.Def_Name.SCENE_NAME_START_MAP))
        {
            //  캐릭터가 변경되었는지 확인
            if (ShouldReloadStats())
            {
                Debug.Log("[PlayerStats] 캐릭터 전환 감지 - 데이터 재로드");
                Invoke(nameof(ReloadCharacterStats), 0.3f);
            }
            else
            {
                Debug.Log("[PlayerStats] 게임 씬 간 이동 - 데이터 유지 (재로드 안 함)");

                //  UI만 재연결 (데이터는 그대로 유지)
                if (statusUI == null)
                {
                    statusUI = FindObjectOfType<PlayerStatusUIManager>();
                }

                if (statusUI != null)
                {
                    statusUI.RefreshStatsReference();
                    statusUI.UpdateAllUI();
                }

                if (CharacterStatUIManager.Instance != null)
                {
                    CharacterStatUIManager.Instance.RefreshStatsReference();
                }
            }
        }
    }

    
    /// 데이터를 재로드해야 하는지 확인
    
    private bool ShouldReloadStats()
    {
        //  첫 로드인 경우
        if (isFirstLoad)
        {
            Debug.Log("[PlayerStats] ShouldReloadStats: 첫 로드 = true");
            return true;
        }

        //  CharacterSaveManager가 없으면 재로드 안 함
        if (CharacterSaveManager.Instance == null)
        {
            Debug.Log("[PlayerStats] ShouldReloadStats: SaveManager 없음 = false");
            return false;
        }

        //  CurrentCharacter가 없으면 재로드 안 함
        if (CharacterSaveManager.Instance.CurrentCharacter == null)
        {
            Debug.Log("[PlayerStats] ShouldReloadStats: CurrentCharacter 없음 = false");
            return false;
        }

        //  캐릭터 ID가 변경되었는지 확인
        string currentCharacterId = CharacterSaveManager.Instance.CurrentCharacter.characterid;
        bool shouldReload = (currentCharacterId != lastLoadedCharacterId);

        Debug.Log($"[PlayerStats] ShouldReloadStats: 캐릭터 변경 확인");
        Debug.Log($"  - 현재 ID: {currentCharacterId}");
        Debug.Log($"  - 마지막 로드 ID: {lastLoadedCharacterId}");
        Debug.Log($"  - 재로드 필요: {shouldReload}");

        return shouldReload;
    }

    
    /// 마지막 로드한 캐릭터 ID 업데이트
    
    private void UpdateLastLoadedCharacter()
    {
        if (CharacterSaveManager.Instance != null &&
            CharacterSaveManager.Instance.CurrentCharacter != null)
        {
            lastLoadedCharacterId = CharacterSaveManager.Instance.CurrentCharacter.characterid;
            Debug.Log($"[PlayerStats] 마지막 로드 캐릭터 ID 업데이트: {lastLoadedCharacterId}");
        }
    }

    
    /// 캐릭터 스텟 재로드 (캐릭터 전환 시에만 호출)
    
    private void ReloadCharacterStats()
    {
        Debug.Log("[PlayerStats] ========================================");
        Debug.Log("[PlayerStats] ReloadCharacterStats 시작 (캐릭터 전환)");
        Debug.Log("[PlayerStats] ========================================");

        // 기존 데이터 완전 초기화
        LoadCharacterStats();
        UpdateLastLoadedCharacter();
        isFirstLoad = false;

        // 이벤트 재구독 (새로운 Stats 객체이므로)
        SubscribeToEvents();

        // UI 재연결
        if (statusUI == null)
        {
            statusUI = FindObjectOfType<PlayerStatusUIManager>();
        }

        if (statusUI != null)
        {
            statusUI.RefreshStatsReference();
            statusUI.UpdateAllUI();
            Debug.Log("[PlayerStats] UI 재연결 완료");
        }
        if (CharacterStatUIManager.Instance != null)
        {
            CharacterStatUIManager.Instance.RefreshStatsReference();
            Debug.Log("[PlayerStats] CharacterStatUI 재연결 완료");
        }

        Debug.Log("[PlayerStats] ========================================");
        Debug.Log("[PlayerStats] ReloadCharacterStats 완료");
        Debug.Log("[PlayerStats] ========================================");
    }

    
    /// 캐릭터 스텟 로드 (중요!)
    
    private void LoadCharacterStats()
    {
        Debug.Log("[PlayerStats] LoadCharacterStats 시작");

        if (CharacterSaveManager.Instance != null &&
            CharacterSaveManager.Instance.CurrentCharacter != null)
        {
            CharacterStatsData savedStats = CharacterSaveManager.Instance.CurrentCharacter.stats;

            Debug.Log($"[PlayerStats] 저장된 스텟 발견: {savedStats.characterName} Lv.{savedStats.level}");

            //  새로운 Stats 객체를 생성하여 완전히 초기화 
            Stats = new CharacterStats();
            Stats.SetDamageTextSpawner(damageTextSpawner);
            Stats.LoadFromData(savedStats);

            Debug.Log($"[PlayerStats] 스텟 로드 완료:");
            Debug.Log($"  - 캐릭터 ID: {CharacterSaveManager.Instance.CurrentCharacter.characterid}");
            Debug.Log($"  - 이름: {Stats.characterName}");
            Debug.Log($"  - 레벨: {Stats.level}");
            Debug.Log($"  - HP: {Stats.currentHP}/{Stats.maxHP}");
            Debug.Log($"  - Gold: {Stats.gold}");
            Debug.Log($"  - Exp: {Stats.currentExp}/{Stats.expToNextLevel}");
        }
        else
        {
            Debug.LogWarning("[PlayerStats] CurrentCharacter가 없음 - 기본 스텟 초기화");
            Stats = new CharacterStats();
            Stats.SetDamageTextSpawner(damageTextSpawner);
            Stats.Initialize(playerName, startLevel);
            lastLoadedCharacterId = "";
            Debug.Log($"[PlayerStats] 기본 스텟으로 초기화: {Stats.characterName} Lv.{Stats.level}");
        }
        //  장비 스탯 재적용 (Stats 객체가 새로 생성된 후)
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.RecalculateAndApplyAllEquipmentStats();
            Debug.Log("[PlayerStats] 장비 스탯 재적용 완료");
        }
    }

    
    /// 이벤트 구독
    
    private void SubscribeToEvents()
    {
        //  기존 구독 먼저 해제 (중복 방지)
        UnsubscribeFromEvents();

        //  새로 구독
        Stats.OnStatsChanged += OnStatsChanged;
        Stats.OnLevelUp += OnLevelUp;
        Stats.OnDeath += OnDeath;
        Stats.OnExpGained += OnExpGained;

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemAdded += OnItemAdded;
            InventoryManager.Instance.OnItemRemoved += OnItemRemoved;
            InventoryManager.Instance.OnItemUsed += OnItemUsed;
        }

        Debug.Log("[PlayerStats] 이벤트 구독 완료");
    }

    
    /// 이벤트 구독 해제
    
    private void UnsubscribeFromEvents()
    {
        if (Stats != null)
        {
            Stats.OnStatsChanged -= OnStatsChanged;
            Stats.OnLevelUp -= OnLevelUp;
            Stats.OnDeath -= OnDeath;
            Stats.OnExpGained -= OnExpGained;
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemAdded -= OnItemAdded;
            InventoryManager.Instance.OnItemRemoved -= OnItemRemoved;
            InventoryManager.Instance.OnItemUsed -= OnItemUsed;
        }
    }

    // ===== 이벤트 핸들러 =====

    private void OnStatsChanged()
    {
        Debug.Log($"[PlayerStats] OnStatsChanged - HP: {Stats.currentHP}/{Stats.maxHP}");
        UpdateUI();
    }

    private void OnLevelUp()
    {
        Debug.Log($"[PlayerStats] 레벨업! Lv.{Stats.level}");
        //업데이트에 따른 스텟 변화 적용
        PlayerController.Instance.UpdateStats();
        UpdateUI();
    }

    private void OnDeath()
    {
        Debug.Log($"[PlayerStats] 플레이어 사망!");
        HandlePlayerDeath();
    }

    private void OnExpGained(int amount)
    {
        Debug.Log($"[PlayerStats] 경험치 획득 +{amount}");
        UpdateUI();
    }

    private void OnItemAdded(InventoryItem item)
    {
        Debug.Log($"[PlayerStats] 아이템 획득: {item.itemName} x{item.quantity}");
    }

    private void OnItemRemoved(InventoryItem item)
    {
        Debug.Log($"[PlayerStats] 아이템 사용/제거: {item.itemName}");
    }

    private void OnItemUsed(InventoryItem item)
    {
        Debug.Log($"[PlayerStats] 아이템 사용: {item.itemName}");
    }

    private void UpdateUI()
    {
        if (statusUI != null)
        {
            statusUI.UpdateAllUI();
        }
        if (CharacterStatUIManager.Instance != null)
        {
            CharacterStatUIManager.Instance.RefreshStatsReference();
        }
    }

    // ===== 게임플레이 메서드 =====

    private void HandlePlayerDeath()
    {
        Debug.Log("[PlayerStats] 플레이어 사망");
        FloatingNotificationManager.Instance.ShowNotification("사망했습니다. 안전 지역으로 리스폰됩니다.");
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetControlsLocked(true);
            PlayerController.Instance.PlayDeathAnimation();
        }

        // 2초 대기 후 리스폰
        Invoke(nameof(RespawnAtSafeZone), 5f);
    }

    private void RespawnAtSafeZone()
    {
        Debug.Log("[PlayerStats] 안전 지역 리스폰 시작");

        // HP 완전 회복
        Stats.FullRecover();

        if (CharacterSaveManager.Instance == null)
        {
            Debug.LogError("[PlayerStats] CharacterSaveManager 없음");
            return;
        }

        var globalData = CharacterSaveManager.Instance.CurrentGlobalData;

        if (string.IsNullOrEmpty(globalData.lastSafeZoneScene))
        {
            Debug.LogWarning("[PlayerStats] 안전 지역 미등록");

            // ===== 추가: 강제 컨트롤 해제 =====
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetControlsLocked(false);
                PlayerController.Instance.PlayAnimation("Idle");
            }
            // =================================

            UpdateUI();
            return;
        }

        // ===== 씬 전환 전 컨트롤 해제 =====
        if (PlayerController.Instance != null)
        {
            Debug.Log("[PlayerStats] 리스폰 전 컨트롤 해제");
            PlayerController.Instance.SetControlsLocked(false);
        }
        // ================================
        FloatingNotificationManager.Instance.ShowNotification("");

        CharacterSaveManager.Instance.LoadSceneByName(
            globalData.lastSafeZoneScene,
            globalData.lastSafeZoneSpawnPoint
        );

        Debug.Log($"[PlayerStats] 마을 귀환: {globalData.lastSafeZoneScene}");
        
        UpdateUI();
        PlayerController.Instance.SetIdleAnimation();
    }

    public void UseItemById(string itemId)
    {
        if (InventoryManager.Instance != null)
        {
            Stats.SetDamageTextSpawner(damageTextSpawner);
            InventoryManager.Instance.UseItem(itemId, Stats);
        }
    }

    public void UseHealthPotion()
    {
        UseItemById("potion_health");
    }

    // ===== 저장/로드 =====

    public PlayerDataSave GetSaveData()
    {
        return new PlayerDataSave
        {
            statsData = Stats.ToSaveData(),
            inventoryData = InventoryManager.Instance?.ToSaveData()
        };
    }

    public void LoadData(PlayerDataSave data)
    {
        if (data == null)
        {
            Debug.LogWarning("[PlayerStats] 저장 데이터가 없습니다. 기본값 사용.");
            return;
        }

        if (data.statsData != null)
        {
            Stats.LoadFromData(data.statsData);
        }

        if (data.inventoryData != null && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.LoadFromData(data.inventoryData);
        }

        Debug.Log("[PlayerStats] 데이터 로드 완료!");
        UpdateUI();
    }

    public void SetDamageTextSpawner(DamageTextSpawner damageTextSpawner)
    {
        this.damageTextSpawner = damageTextSpawner;
        Stats.SetDamageTextSpawner(damageTextSpawner);
    }
}


[System.Serializable]
public class PlayerDataSave
{
    public CharacterStatsData statsData;
    public InventorySaveData inventoryData;
}