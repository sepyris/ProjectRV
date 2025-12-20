using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Definitions;

/// <summary>
/// 플레이어 상태 UI 관리 스크립트
/// HP, 경험치, 골드를 표시하고 슬라이더를 업데이트합니다.
/// </summary>
public class PlayerStatusUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider expSlider;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("Optional - Menu Button")]
    [SerializeField] private Button menuButton;

    [Header("Settings")]
    [SerializeField] private bool showDetailedHP = true;
    [SerializeField] private bool showExpAsRatio = true;

    void Awake()
    {
        //  씬 로드 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        InitializeUI();
        FindAndConnectPlayerStats();
    }

    void OnDestroy()
    {
        //  이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 씬이 로드될 때 호출됨
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[PlayerStatusUI] OnSceneLoaded: {scene.name}");

        //  캐릭터 선택창으로 돌아가는 경우 
        if (scene.name == Def_Name.SCENE_NAME_CHARACTER_SELECT_SCENE)
        {
            Debug.Log("[PlayerStatusUI] 캐릭터 선택창 진입 - UI 대기 모드");
            return;
        }

        //  게임 로딩 씬으로 진입하는 경우 (캐릭터 전환 시) 
        if (scene.name == Def_Name.SCENE_NAME_GAME_LOADING_SCENE)
        {
            Debug.Log("[PlayerStatusUI] 게임 로딩 씬 진입 - 플레이어 데이터 재연결 대기");

            //  PlayerController가 활성화될 시간 확보
            Invoke(nameof(ReconnectPlayerStats), 0.5f);
            return;
        }

        //  게임 씬 간 이동 (맵 이동) - UI만 재연결 
        if (scene.name.StartsWith(Definitions.Def_Name.SCENE_NAME_START_MAP))
        {
            Debug.Log("[PlayerStatusUI] 게임 씬 간 이동 - UI 재연결");

            //  참조가 끊어진 경우에만 재연결
            if (PlayerStatsComponent.Instance == null || PlayerStatsComponent.Instance.Stats == null)
            {
                Invoke(nameof(ReconnectPlayerStats), 0.3f);
            }
            else
            {
                //  참조가 유지되고 있으면 UI만 업데이트
                Debug.Log("[PlayerStatusUI] 참조 유지됨 - UI 업데이트만 수행");
                UpdateAllUI();
            }
        }
    }

    /// <summary>
    /// PlayerStats 재연결
    /// </summary>
    private void ReconnectPlayerStats()
    {
        Debug.Log("[PlayerStatusUI] ReconnectPlayerStats 시작");
        FindAndConnectPlayerStats();
    }

    /// <summary>
    /// UI 초기 설정
    /// </summary>
    private void InitializeUI()
    {
        //  슬라이더를 읽기 전용으로 설정 (마우스로 조작 불가)
        if (hpSlider != null)
        {
            hpSlider.minValue = 0;
            hpSlider.maxValue = 1;
            hpSlider.value = 1;
            hpSlider.interactable = false;
        }

        if (expSlider != null)
        {
            expSlider.minValue = 0;
            expSlider.maxValue = 1;
            expSlider.value = 0;
            expSlider.interactable = false;
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(OnMenuButtonClicked);
        }
    }

    /// <summary>
    /// PlayerStatsComponent 찾기 및 연결
    /// </summary>
    private void FindAndConnectPlayerStats()
    {
        Debug.Log("[PlayerStatusUI] FindAndConnectPlayerStats 시작");

        //  현재 씬 확인 - 로딩 씬이면 스킵 
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == Def_Name.SCENE_NAME_GAME_LOADING_SCENE ||
            currentScene == Def_Name.SCENE_NAME_CHARACTER_SELECT_SCENE)
        {
            Debug.Log($"[PlayerStatusUI] {currentScene}에서는 PlayerController가 없음 - 대기");
            return;
        }

        // PlayerController 찾기
        PlayerController[] controllers = FindObjectsOfType<PlayerController>(false);

        if (controllers.Length == 0)
        {
            Debug.LogWarning("[PlayerStatusUI] PlayerController를 찾을 수 없습니다. 재시도 예약...");
            Invoke(nameof(FindAndConnectPlayerStats), 0.5f);
            return;
        }

        PlayerController playerController = controllers[0];

        if (controllers.Length > 1)
        {
            Debug.LogWarning($"[PlayerStatusUI] PlayerController가 {controllers.Length}개 발견됨! 첫 번째 것 사용");
        }

        // PlayerStatsComponent 가져오기
        if (PlayerStatsComponent.Instance == null)
        {
            Debug.LogError("[PlayerStatusUI] PlayerStatsComponent를 찾을 수 없습니다!");
            return;
        }

        if (PlayerStatsComponent.Instance.Stats == null)
        {
            Debug.LogError("[PlayerStatusUI] Stats가 null입니다! 재시도...");
            Invoke(nameof(FindAndConnectPlayerStats), 0.5f);
            return;
        }

        // UI 업데이트
        UpdateAllUI();
    }

    /// <summary>
    /// Stats 참조 업데이트 (PlayerStatsComponent에서 호출)
    /// </summary>
    public void RefreshStatsReference()
    {
        if (PlayerStatsComponent.Instance != null)
        {
            Debug.Log($"[PlayerStatusUI] Stats 참조 업데이트: {PlayerStatsComponent.Instance.Stats.characterName} Lv.{PlayerStatsComponent.Instance.Stats.level}");
            UpdateAllUI();
        }
    }

    /// <summary>
    /// 모든 UI 업데이트
    /// </summary>
    public void UpdateAllUI()
    {
        if (PlayerStatsComponent.Instance.Stats == null)
        {
            Debug.LogWarning("[PlayerStatusUI] UpdateAllUI - stats가 null입니다!");
            return;
        }

        UpdateHPUI();
        UpdateExpUI();
        UpdateGoldUI();
    }

    /// <summary>
    /// HP UI 업데이트
    /// </summary>
    private void UpdateHPUI()
    {
        if (PlayerStatsComponent.Instance.Stats == null) return;

        if (hpSlider != null)
        {
            float hpRatio = PlayerStatsComponent.Instance.Stats.maxHP > 0 ? (float)PlayerStatsComponent.Instance.Stats.currentHP / PlayerStatsComponent.Instance.Stats.maxHP : 0;
            hpSlider.value = hpRatio;
        }

        if (hpText != null)
        {
            if (showDetailedHP)
            {
                hpText.text = $"{PlayerStatsComponent.Instance.Stats.currentHP} / {PlayerStatsComponent.Instance.Stats.maxHP}";
            }
            else
            {
                hpText.text = $"{PlayerStatsComponent.Instance.Stats.currentHP}";
            }
        }
    }

    /// <summary>
    /// 경험치 UI 업데이트
    /// </summary>
    private void UpdateExpUI()
    {
        if (PlayerStatsComponent.Instance.Stats == null) return;

        if (expSlider != null)
        {
            float expRatio = PlayerStatsComponent.Instance.Stats.expToNextLevel > 0 ? (float)PlayerStatsComponent.Instance.Stats.currentExp / PlayerStatsComponent.Instance.Stats.expToNextLevel : 0;
            expSlider.value = expRatio;
        }

        if (expText != null)
        {
            if (showExpAsRatio)
            {
                float percentage = PlayerStatsComponent.Instance.Stats.expToNextLevel > 0 ?
                    ((float)PlayerStatsComponent.Instance.Stats.currentExp / PlayerStatsComponent.Instance.Stats.expToNextLevel) * 100f : 0f;
                expText.text = $"{percentage:F3}%";
            }
            else
            {
                expText.text = $"{PlayerStatsComponent.Instance.Stats.currentExp} / {PlayerStatsComponent.Instance.Stats.expToNextLevel}";
            }
        }
    }

    /// <summary>
    /// 골드 UI 업데이트
    /// </summary>
    private void UpdateGoldUI()
    {
        if (PlayerStatsComponent.Instance.Stats == null) return;

        if (goldText != null)
        {
            goldText.text = $"{PlayerStatsComponent.Instance.Stats.gold:N0}";
        }
    }

    private void OnMenuButtonClicked()
    {
        if (PauseMenuUIManager.Instance != null)
        {
            PauseMenuUIManager.Instance.OpenPauseMenu();
        }
    }

    public void ForceUpdateUI()
    {
        UpdateAllUI();
    }

    public void SetDetailedHPDisplay(bool detailed)
    {
        showDetailedHP = detailed;
        UpdateHPUI();
    }

    public void SetExpRatioDisplay(bool showRatio)
    {
        showExpAsRatio = showRatio;
        UpdateExpUI();
    }
}