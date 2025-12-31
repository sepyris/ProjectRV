using Pathfinding;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;


/// 게임 최초 실행 시 시스템 초기화
/// - CharacterSaveManager 생성
/// - 게임 데이터 로드 (CSV → SO)
/// - 각종 매니저 초기화

public class SystemInitializer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float minLoadingTime = 2f;
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("UI (Optional)")]
    [SerializeField] private UnityEngine.UI.Slider progressBar;
    [SerializeField] private TMPro.TextMeshProUGUI statusText;

    void Start()
    {
#if UNITY_EDITOR
        // 🔧 A* Pathfinding Project의 자동 업데이트 체크 비활성화
        if (EditorPrefs.GetBool("AstarCheckForUpdates", true))
        {
            EditorPrefs.SetBool("AstarCheckForUpdates", false);
            Debug.Log("[SystemInit] A* Update Checker 비활성화 완료");
        }
#endif
        StartCoroutine(InitializeSystem());
    }

    private IEnumerator InitializeSystem()
    {
        float startTime = Time.time;
        Debug.Log("[SystemInit] 시스템 초기화 시작...");

        // ===== 1단계: CharacterSaveManager 생성 =====
        UpdateStatus("캐릭터 저장 시스템 초기화...", 0.1f);
        yield return StartCoroutine(InitializeCharacterSaveManager());

        // ===== 2단계: 게임 데이터 로드 (CSV → SO) =====
        UpdateStatus("게임 데이터 로드 중...", 0.3f);
        yield return StartCoroutine(InitializeDataManagers());

        // ===== 3단계: 기타 시스템 초기화 =====
        UpdateStatus("시스템 초기화 중...", 0.7f);
        yield return StartCoroutine(InitializeOtherSystems());

        // ===== 4단계: 완료 =====
        UpdateStatus("초기화 완료!", 1.0f);

        // 최소 로딩 시간 보장
        float elapsedTime = Time.time - startTime;
        if (elapsedTime < minLoadingTime)
        {
            yield return new WaitForSeconds(minLoadingTime - elapsedTime);
        }

        Debug.Log("[SystemInit] 시스템 초기화 완료");

        // 메인 메뉴로 이동
        SceneManager.LoadScene(mainMenuSceneName);
    }

    
    /// 1단계: CharacterSaveManager 초기화
    
    private IEnumerator InitializeCharacterSaveManager()
    {
        if (CharacterSaveManager.Instance == null)
        {
            GameObject saveManagerObj = new GameObject("CharacterSaveManager");
            saveManagerObj.AddComponent<CharacterSaveManager>();
            Debug.Log("[SystemInit] CharacterSaveManager 생성 완료");
        }
        yield return null;
    }

    
    /// 2단계: 게임 데이터 매니저 초기화 (CSV → SO 로드)
    
    private IEnumerator InitializeDataManagers()
    {
        // ItemDataManager 초기화
        if (ItemDataManager.Instance != null)
        {
            Debug.Log("[SystemInit] ItemDataManager 로드 중...");
            yield return null;
        }

        // MonsterDataManager 초기화
        if (MonsterDataManager.Instance != null)
        {
            Debug.Log("[SystemInit] MonsterDataManager 로드 중...");
            yield return null;
        }

        // QuestDataManager 초기화
        if (QuestDataManager.Instance != null)
        {
            Debug.Log("[SystemInit] QuestDataManager 로드 중...");
            yield return null;
        }

        // DialogueDataManager 초기화
        if (DialogueDataManager.Instance != null)
        {
            Debug.Log("[SystemInit] DialogueDataManager 로드 중...");
            yield return null;
        }

        // GatherableDataManager 초기화
        if (GatherableDataManager.Instance != null)
        {
            Debug.Log("[SystemInit] GatherableDataManager 로드 중...");
            yield return null;
        }

        // NPCInfoManager 초기화
        if (NPCInfoManager.Instance != null)
        {
            Debug.Log("[SystemInit] NPCInfoManager 로드 중...");
            yield return null;
        }

        // MapInfoManager 초기화
        if (MapInfoManager.Instance != null)
        {
            Debug.Log("[SystemInit] MapInfoManager 로드 중...");
            yield return null;
        }

        Debug.Log("[SystemInit] 모든 데이터 매니저 로드 완료");
        yield return null;
    }

    
    /// 3단계: 기타 시스템 초기화
    
    private IEnumerator InitializeOtherSystems()
    {
        // LocalizationManager 초기화
        if (LocalizationManager.Instance != null)
        {
            Debug.Log("[SystemInit] LocalizationManager 초기화");
            yield return null;
        }

        // 기타 필요한 싱글톤 매니저들
        // 예: SoundManager, PoolManager 등

        yield return null;
    }

    
    /// UI 상태 업데이트
    
    private void UpdateStatus(string message, float progress)
    {
        Debug.Log($"[SystemInit] {message} ({progress * 100:F0}%)");

        if (statusText != null)
        {
            statusText.text = message;
        }

        if (progressBar != null)
        {
            progressBar.value = progress;
        }
    }
}