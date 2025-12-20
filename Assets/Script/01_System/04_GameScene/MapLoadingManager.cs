using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using Definitions;

/// <summary>
/// MapLoadingScene에서 사용
/// 맵 간 이동 시 로딩 화면 표시 (데이터 로드 없음)
/// </summary>
public class MapLoadingManager : MonoBehaviour
{
    public static string TargetSceneName { get; set; } = "";
    public static string TargetSpawnPointid { get; set; } = "";

    [Header("UI Components")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private TextMeshProUGUI tipsText;
    [SerializeField] private GameObject loadingIcon;

    [Header("Settings")]
    [SerializeField] private float minLoadingTime = 2.0f;
    [SerializeField] private float iconRotationSpeed = 180f;

    [Header("Loading Tips")]
    [SerializeField]
    private string[] loadingTips = {
        "몬스터를 처치하면 경험치를 얻을 수 있습니다.",
        "채집 지점은 일정 시간이 지나면 랜덤위치에 다시 생성됩니다.",
        "세이브 포인트에서 E키를 눌러 저장할수 있습니다.",
        "인벤토리는 I키로 열 수 있습니다.",
        "NPC와 대화하면 퀘스트를 받을 수 있습니다.",
        "퀘스트를 완료하면 보상을 받을 수 있습니다.",
        "맵을 탐험하며 숨겨진 아이템을 찾아보세요.",
        "파티를 구성하여 강력한 몬스터에 도전하세요.",
        "상점에서 아이템을 구매하고 판매할 수 있습니다.",
        "스킬을 업그레이드하여 전투 능력을 향상시키세요.",
        "장비 없이 싸우면 위험합니다.",
        "자동저장이 없습니다.세이브를 잊지마세요.",
    };

    void Start()
    {
        // 랜덤 팁 표시
        if (tipsText != null && loadingTips.Length > 0)
        {
            string randomTip = loadingTips[Random.Range(0, loadingTips.Length)];
            tipsText.text = $"팁: {randomTip}";
        }

        // 로딩 시작
        StartCoroutine(LoadMapRoutine());
    }

    void Update()
    {
        // 로딩 아이콘 회전
        if (loadingIcon != null)
        {
            loadingIcon.transform.Rotate(0, 0, -iconRotationSpeed * Time.deltaTime);
        }
    }

    private IEnumerator LoadMapRoutine()
    {
        float startTime = Time.time;

        // 목표 씬 확인
        if (string.IsNullOrEmpty(TargetSceneName))
        {
            Debug.LogError("[MapLoading] 목표 씬 이름이 설정되지 않았습니다!");
            yield break;
        }

        UpdateLoadingText("맵 로딩 중...");
        UpdateProgress(0f);

        yield return new WaitForSeconds(0.2f);

        //  스폰 포인트 설정 (CharacterSaveManager 사용)
        if (!string.IsNullOrEmpty(TargetSpawnPointid) && CharacterSaveManager.Instance != null)
        {
            CharacterSaveManager.Instance.NextSceneSpawnPointid = TargetSpawnPointid;
            Debug.Log($"[MapLoading] 스폰 포인트 설정: {TargetSpawnPointid}");
        }
        else if (string.IsNullOrEmpty(TargetSpawnPointid))
        {
            Debug.LogWarning("[MapLoading] 스폰 포인트 id가 비어있습니다!");
        }

        UpdateLoadingText($"{TargetSceneName} 로딩 중...");
        UpdateProgress(0.3f);

        yield return new WaitForSeconds(0.3f);

        // 씬 비동기 로드
        //AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(TargetSceneName);
        CharacterSaveManager.Instance.CurrentGlobalData.currentSceneName = TargetSceneName;
        CharacterSaveManager.Instance.NextSceneSpawnPointid = TargetSpawnPointid;
        CharacterSaveManager.Instance.LoadSceneByName(TargetSceneName, TargetSpawnPointid);
        UpdateProgress(0.8f);
        /*
        if (asyncLoad == null)
        {
            Debug.LogError($"[MapLoading] 씬 '{TargetSceneName}'를 로드할 수 없습니다!");
            yield break;
        }

        // 로딩 진행률 업데이트
        while (!asyncLoad.isDone)
        {
            // AsyncOperation.progress는 0.0 ~ 0.9 범위
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            UpdateProgress(progress);
            yield return null;
        }
        */
        UpdateLoadingText("로딩 완료!");
        UpdateProgress(1f);

        // 최소 로딩 시간 보장
        float elapsedTime = Time.time - startTime;
        if (elapsedTime < minLoadingTime)
        {
            yield return new WaitForSeconds(minLoadingTime - elapsedTime);
        }

        Debug.Log($"[MapLoading] 맵 로딩 완료: {TargetSceneName}");

        // 정적 변수 초기화
        TargetSceneName = "";
        TargetSpawnPointid = "";
    }

    /// <summary>
    /// 로딩 텍스트 업데이트
    /// </summary>
    private void UpdateLoadingText(string text)
    {
        if (loadingText != null)
        {
            loadingText.text = text;
        }
    }

    /// <summary>
    /// 진행률 바 업데이트
    /// </summary>
    private void UpdateProgress(float progress)
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
        }
    }

    /// <summary>
    /// 외부에서 맵 로딩 시작 (정적 메서드)
    /// </summary>
    public static void LoadMap(string sceneName, string spawnPointid = "")
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[MapLoading] 씬 이름이 비어있습니다!");
            return;
        }

        TargetSceneName = sceneName;
        TargetSpawnPointid = spawnPointid;

        SceneManager.LoadScene(Def_Name.SCENE_NAME_MAP_LOADING_SCENE);
        Debug.Log($"[MapLoading] 맵 전환 시작: {sceneName}");
    }
}