using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using Definitions;
using TMPro;


/// 일시정지 메뉴 UI 관리
/// ESC 또는 버튼으로 열고, 게임을 일시정지하며, 캐릭터 선택으로 돌아갈 수 있음
/// 캐릭터 선택 이동 및 게임 종료 시 경고 팝업 표시

public class PauseMenuUIManager : MonoBehaviour, IClosableUI
{
    public static PauseMenuUIManager Instance { get; private set; }

    [Header("UI References - Main Menu")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button returnToCharacterSelectButton;
    [SerializeField] private Button closeGameButton;

    [Header("UI References - Warning Popup")]
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI warning_message;

    [Header("Settings")]
    [SerializeField] private bool pauseTimeOnOpen = true; // Time.timeScale = 0
    private bool isPaused = false;

    
    /// 경고 팝업에서 어떤 액션을 실행할지 저장하는 열거형
    
    private enum WarningAction
    {
        None,
        ReturnToCharacterSelect,
        CloseGame
    }

    private WarningAction pendingAction = WarningAction.None;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        InitializeUI();

        // 씬 로드 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    
    /// UI 초기화
    
    private void InitializeUI()
    {
        // 메인 메뉴 버튼 이벤트 연결
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(OnResumeButtonClicked);
        }

        if (returnToCharacterSelectButton != null)
        {
            returnToCharacterSelectButton.onClick.RemoveAllListeners();
            returnToCharacterSelectButton.onClick.AddListener(OnReturnToCharacterSelectClicked);
        }

        if (closeGameButton != null)
        {
            closeGameButton.onClick.RemoveAllListeners();
            closeGameButton.onClick.AddListener(OnCloseGameClicked);
        }

        // 경고 팝업 버튼 이벤트 연결
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnWarningConfirmClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnWarningCancelClicked);
        }

        // 초기 상태: 닫힘
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
        }

        isPaused = false;
        pendingAction = WarningAction.None;
    }

    
    /// 씬 로드 시 초기화
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 게임 씬이 아니면 일시정지 메뉴 숨기기
        if (!IsGameScene(scene.name))
        {
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }

            if (warningPanel != null)
            {
                warningPanel.SetActive(false);
            }

            isPaused = false;
            pendingAction = WarningAction.None;

            // Time.timeScale 복원
            if (pauseTimeOnOpen)
            {
                Time.timeScale = 1f;
            }
        }
    }

    
    /// 일시정지 토글
    
    public void TogglePause()
    {
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    
    /// 일시정지 (메뉴 열기)
    
    public void Pause()
    {
        if (isPaused) return;

        // 대화 중이거나 다른 UI가 열려있으면 일시정지 불가
        if (IsOtherUIOpen())
        {
            Debug.Log("[PauseMenu] 다른 UI가 열려있어 일시정지할 수 없습니다.");
            return;
        }

        isPaused = true;

        // 패널 활성화
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
        PlayerHUD.Instance.RegisterUI(this);
        // 시간 정지
        if (pauseTimeOnOpen)
        {
            Time.timeScale = 0f;
        }

        // 플레이어 조작 잠금
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetControlsLocked(true);
        }

        Debug.Log("[PauseMenu] 게임 일시정지");
    }

    
    /// 재개 (메뉴 닫기)
    
    public void Resume()
    {
        if (!isPaused) return;

        isPaused = false;

        // 패널 비활성화
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        // 경고 팝업도 닫기
        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
        }
        PlayerHUD.Instance.UnregisterUI(this);
        // 시간 재개
        if (pauseTimeOnOpen)
        {
            Time.timeScale = 1f;
        }

        // 플레이어 조작 해제
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetControlsLocked(false);
        }

        pendingAction = WarningAction.None;

        Debug.Log("[PauseMenu] 게임 재개");
    }

    // ==========================================
    // 메인 메뉴 버튼 이벤트
    // ==========================================

    
    /// 재개 버튼 클릭
    
    private void OnResumeButtonClicked()
    {
        Resume();
    }

    
    /// 캐릭터 선택으로 돌아가기 버튼 클릭
    
    private void OnReturnToCharacterSelectClicked()
    {
        // 경고 팝업 표시
        warning_message.text = "다른 캐릭터를\n보러 가시겠습니까?";
        ShowWarningPopup(WarningAction.ReturnToCharacterSelect);
    }

    
    /// 게임 종료 버튼 클릭
    
    private void OnCloseGameClicked()
    {
        // 경고 팝업 표시
        warning_message.text = "여정을 기록 하고\n종료 하시겠습니까?";
        ShowWarningPopup(WarningAction.CloseGame);
    }

    // ==========================================
    // 경고 팝업 관련
    // ==========================================

    
    /// 경고 팝업 표시
    
    /// <param name="action">확인 버튼 클릭 시 실행할 액션</param>
    private void ShowWarningPopup(WarningAction action)
    {
        pendingAction = action;

        confirmButton.gameObject.SetActive(true);
        cancelButton.gameObject.SetActive(true);

        // 메인 메뉴 패널 숨기기
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        // 경고 팝업 표시
        if (warningPanel != null)
        {
            warningPanel.SetActive(true);
            warningPanel.transform.SetAsLastSibling();
        }

        Debug.Log($"[PauseMenu] 경고 팝업 표시: {action}");
    }

    
    /// 경고 팝업 - 확인 버튼 클릭
    
    private void OnWarningConfirmClicked()
    {
        Debug.Log($"[PauseMenu] 경고 확인: {pendingAction}");

        // 대기 중인 액션 실행
        switch (pendingAction)
        {
            case WarningAction.ReturnToCharacterSelect:
                ReturnToCharacterSelect();
                break;

            case WarningAction.CloseGame:
                CloseGame();
                break;

            case WarningAction.None:
            default:
                // 아무것도 하지 않고 팝업만 닫기
                HideWarningPopup();
                break;
        }

        pendingAction = WarningAction.None;
    }

    
    /// 경고 팝업 - 취소 버튼 클릭
    
    private void OnWarningCancelClicked()
    {
        Debug.Log("[PauseMenu] 경고 취소");

        // 경고 팝업 닫고 메인 메뉴로 돌아가기
        HideWarningPopup();

        pendingAction = WarningAction.None;
    }

    
    /// 경고 팝업 숨기기
    
    private void HideWarningPopup()
    {
        // 경고 팝업 숨기기
        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
        }

        // 메인 메뉴 패널 다시 표시
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
    }

    // ==========================================
    // 실제 액션 실행
    // ==========================================

    
    /// 게임 종료 하기
    
    private void CloseGame()
    {
        Debug.Log("[PauseMenu] 게임 종료 시작");

        //  코루틴으로 저장 후 종료
        StartCoroutine(CloseGameRoutine());
    }

    
    /// 게임 종료 코루틴 - 저장 완료 후 종료
    
    private IEnumerator CloseGameRoutine()
    {
        // ===== 1. 버튼 비활성화 =====
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(false);
        if (cancelButton != null)
            cancelButton.gameObject.SetActive(false);

        // ===== 2. 저장 메시지 표시 =====
        if (warning_message != null)
            warning_message.text = "기록 중...";
        yield return new WaitForSecondsRealtime(0.3f);

        // ===== 3. 실제 저장 수행 =====
        bool saveSuccess = false;

        if (CharacterSaveManager.Instance != null)
        {
            //  상점 데이터 먼저 커밋
            if (CharacterSaveManager.Instance.CurrentCharacter != null &&
                CharacterSaveManager.Instance.CurrentCharacter.shopStockData != null)
            {
                CharacterSaveManager.Instance.CurrentCharacter.shopStockData.CommitTempData();
                Debug.Log("[PauseMenu] 상점 임시 데이터 커밋 완료");
            }

            //  저장 실행
            saveSuccess = CharacterSaveManager.Instance.SaveCurrentCharacterGameData();
            Debug.Log($"[PauseMenu] 저장 결과: {(saveSuccess ? "성공" : "실패")}");
        }

        // ===== 4. 저장 완료 메시지 표시 =====
        if (warning_message != null)
            warning_message.text = saveSuccess ? "기록 완료!" : "기록 실패";
        yield return new WaitForSecondsRealtime(0.5f);

        // ===== 5. 게임 종료 =====
        Debug.Log("[PauseMenu] 게임 종료 실행");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    
    /// 캐릭터 선택 화면으로 돌아가기
    
    private void ReturnToCharacterSelect()
    {
        Debug.Log("[PauseMenu] 캐릭터 선택 화면으로 돌아갑니다...");

        // 1. 일시정지 해제
        isPaused = false;
        if (pauseTimeOnOpen)
        {
            Time.timeScale = 1f;
        }

        // 2. 플레이어 상태 저장
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SaveStateBeforeDeactivation();
            PlayerController.Instance.SetControlsLocked(false);
            CharacterSaveManager.Instance.SaveCurrentCharacterGameData();
        }

        // 3. 열려있는 UI 모두 닫기
        CloseAllGameUI();

        // 4. 로딩 화면 표시
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowGlobalLoading();
        }

        // 5. 씬 전환
        SceneManager.LoadScene(Def_Name.SCENE_NAME_CHARACTER_SELECT_SCENE);
    }

    
    /// 모든 게임 UI 닫기
    
    private void CloseAllGameUI()
    {
        if (ItemUIManager.Instance != null)
            ItemUIManager.Instance.CloseItemUI();

        if (QuestUIManager.Instance != null)
            QuestUIManager.Instance.CloseQuestUI();

        if (DialogueUIManager.Instance != null)
            DialogueUIManager.Instance.CloseInteraction();

        if (ShopUIManager.Instance != null)
            ShopUIManager.Instance.CloseShop();

        if (EquipmentUIManager.Instance != null)
            EquipmentUIManager.Instance.CloseEquipmentUI();

        if(SkillUIManager.Instance != null)
            SkillUIManager.Instance.CloseSkillUI();

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (warningPanel != null)
            warningPanel.SetActive(false);
    }

    
    /// 다른 UI가 열려있는지 확인
    
    private bool IsOtherUIOpen()
    {
        // 대화 중
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
            return true;

        // 상점 열림
        if (ShopUIManager.Instance != null && ShopUIManager.Instance.IsShopOpen)
            return true;

        // 로딩 중
        if (LoadingScreenManager.Instance != null && LoadingScreenManager.Instance.IsLoading)
            return true;

        return false;
    }

    
    /// 게임 씬인지 확인
    
    private bool IsGameScene(string sceneName)
    {
        return sceneName.StartsWith("Map_");
    }

    // ===== 공개 메서드 =====

    
    /// 일시정지 상태 확인
    
    public bool IsPaused()
    {
        return isPaused;
    }

    
    /// 일시정지 메뉴 열기 (외부에서 호출)
    
    public void OpenPauseMenu()
    {
        Pause();
    }

    
    /// 일시정지 메뉴 닫기 (외부에서 호출)
    
    public void ClosePauseMenu()
    {
        Resume();
    }

    public void Close()
    {
        ClosePauseMenu();
    }

    public GameObject GetUIPanel()
    {
        return pauseMenuPanel;
    }
}