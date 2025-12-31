using Definitions;
using UnityEngine;
using UnityEngine.SceneManagement;

//씬 상태 매니저
public class SceneStateManager : MonoBehaviour
{
    public static SceneStateManager Instance { get; private set; }

    // 현재 씬 정보
    private string currentScene;
    private string previousScene;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        previousScene = currentScene;
        currentScene = scene.name;

        Debug.Log($"[SceneState] {previousScene} → {currentScene}");

        // 씬 타입별 초기화
        InitializeScene(scene.name);
    }

    private void OnSceneUnloaded(Scene scene)
    {
        Debug.Log($"[SceneState] {scene.name} 언로드됨");

        // 씬 정리
        CleanupScene(scene.name);
    }

    private void InitializeScene(string sceneName)
    {
        switch (sceneName)
        {
            case Def_Name.SCENE_NAME_INITIAL_LOADING_SCENE:
                InitializeMainLoad();
                break;

            case Def_Name.SCENE_NAME_MAIN_SCREEN:
                InitializeMain();
                break;

            case Def_Name.SCENE_NAME_CHARACTER_SELECT_SCENE:
                InitializeCharacterSelect();
                break;

            case Def_Name.SCENE_NAME_GAME_LOADING_SCENE:
                InitializeGameLoading();
                break;

            default:
                // 게임 맵 씬들 (Map_으로 시작)
                if (sceneName.StartsWith("Map_"))
                {
                    InitializeGameScene();
                }
                break;
        }
    }

    private void CleanupScene(string sceneName)
    {
        // 게임 씬에서 나갈 때
        if (sceneName.StartsWith("Map_"))
        {
            CleanupGameScene();
        }
    }

    // ===== 씬별 초기화 메서드 =====

    private void InitializeMainLoad()
    {
        // 로딩 화면 표시
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowGlobalLoading();
        }
    }

    private void InitializeMain()
    {
        // 로딩 숨김
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.HideGlobalLoading();
        }
    }

    private void InitializeCharacterSelect()
    {
        // 게임 UI 모두 비활성화
        HideAllGameUI();

        // 로딩 숨김
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.HideGlobalLoading();
        }
        

        // 게임에서 돌아온 경우 데이터 저장 확인
        if (previousScene != null && previousScene.StartsWith("Map_"))
        {
            Debug.Log("[SceneState] 게임에서 돌아옴 - 자동 저장 완료");
        }
    }

    private void InitializeGameLoading()
    {
        // 로딩 표시
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowGlobalLoading();
        }

        // 게임 UI 초기화 (아직 비활성)
        PrepareGameUI();
    }

    private void InitializeGameScene()
    {
        // 로딩 숨김
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.HideGlobalLoading();
        }

        // 게임 UI 활성화
        ShowGameUI();

        // 플레이어 초기화
        InitializePlayer();
    }

    private void CleanupGameScene()
    {

        // 게임 UI 정리
        CleanupGameUI();
    }

    // ===== 헬퍼 메서드 =====

    private void HideAllGameUI()
    {
        if (ItemUIManager.Instance != null)
            ItemUIManager.Instance.CloseItemUI();

        if (QuestUIManager.Instance != null)
            QuestUIManager.Instance.CloseQuestUI();

        if (DialogueUIManager.Instance != null)
            DialogueUIManager.Instance.CloseInteraction();
        //HUD
        if(PlayerHUD.Instance != null)
        {
            PlayerHUD.Instance.gameObject.SetActive(false);
            FloatingNotificationManager.Instance.ClearAllMessages();
            FloatingItemManager.Instance.ClearAllMessages();
        }
            
        if (MiniMapManager.Instance != null)
            MiniMapManager.Instance.gameObject.SetActive(false);
    }

    private void PrepareGameUI()
    {
        // UI 매니저들이 준비되었는지 확인
        Debug.Log("[SceneState] 게임 UI 준비 중...");
        if (PlayerHUD.Instance != null)
            PlayerHUD.Instance.gameObject.SetActive(true);
        if (MiniMapManager.Instance != null)
            MiniMapManager.Instance.gameObject.SetActive(true);
    }

    private void ShowGameUI()
    {
        // 필요한 UI만 활성화
        Debug.Log("[SceneState] 게임 UI 활성화");
        PrepareGameUI();
    }

    private void CleanupGameUI()
    {
        // 열려있는 UI 모두 닫기
        HideAllGameUI();
    }

    private void InitializePlayer()
    {
        if (PlayerController.Instance != null)
        {
            // 플레이어 컨트롤 활성화
            PlayerController.Instance.SetControlsLocked(false);
        }
    }

    // ===== 공개 메서드 =====

    public string GetCurrentScene() => currentScene;
    public string GetPreviousScene() => previousScene;

    public bool IsGameScene()
    {
        return currentScene != null && currentScene.StartsWith("Map_");
    }
}