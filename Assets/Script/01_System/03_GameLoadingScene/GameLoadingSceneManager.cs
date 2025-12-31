using Definitions;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;


/// 게임 로딩 씬 매니저
/// 캐릭터 데이터를 로드하고 게임 씬으로 전환하는 역할

public class GameLoadingSceneManager : MonoBehaviour
{
    [Header("로딩 설정")]
    [SerializeField] private float minimumLoadingTime = 1f; // 최소 로딩 시간

    private bool isLoading = false;

    void Awake()
    {
        Debug.Log("[GameLoading] Awake 호출됨!");
        if (!isLoading)
        {
            StartCoroutine(LoadGameData());
        }
    }

    void Start()
    {
        Debug.Log("[GameLoading] Start 호출됨!");
        if (!isLoading)
        {
            StartCoroutine(LoadGameData());
        }
    }

    
    /// 게임 데이터 로드 및 씬 전환
    
    private IEnumerator LoadGameData()
    {
        isLoading = true;
        float startTime = Time.time;

        // 로딩 화면 표시
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowGlobalLoading();
        }

        Debug.Log("[GameLoading] 캐릭터 데이터 로딩 시작...");

        // 1. 캐릭터 선택 확인
        if (CharacterSaveManager.Instance == null)
        {
            Debug.LogError("[GameLoading] CharacterSaveManager가 없습니다!");
            yield break;
        }

        if (CharacterSaveManager.Instance.CurrentCharacter == null)
        {
            Debug.LogError("[GameLoading] 선택된 캐릭터가 없습니다!");
            SceneManager.LoadScene("CharacterSelectScene");
            yield break;
        }

        // 2. 캐릭터 데이터 로드 (수정!)
        string characterName = CharacterSaveManager.Instance.CurrentCharacter.stats.characterName;
        Debug.Log($"[GameLoading] '{characterName}' 캐릭터 데이터 로딩 중...");

        //  여기가 핵심! LoadGame 대신 LoadCurrentCharacterGameData 호출
        CharacterSaveManager.Instance.LoadCurrentCharacterGameData();

        // 3. 로드할 씬 결정
        string targetScene = GetTargetScene();
        Debug.Log($"[GameLoading] 목표 씬: {targetScene}");

        // 최소 로딩 시간 대기
        float elapsedTime = Time.time - startTime;
        if (elapsedTime < minimumLoadingTime)
        {
            yield return new WaitForSeconds(minimumLoadingTime - elapsedTime);
        }

        // 4. 게임 씬으로 전환
        Debug.Log($"[GameLoading] '{targetScene}' 씬으로 전환 시작!");
        Debug.Log($"[GameLoading] CurrentCharacter: {CharacterSaveManager.Instance.CurrentCharacter?.stats.characterName}");

        SceneManager.LoadScene(targetScene);

        Debug.Log($"[GameLoading] LoadScene 호출 완료"); // ← 이건 안 찍힐 수도 있음

        isLoading = false;
    }

    
    /// 로드할 씬 결정
    
    private string GetTargetScene()
    {
        if (CharacterSaveManager.Instance == null)
        {
            Debug.LogError("[GameLoading] CharacterSaveManager가 null!");
            return Def_Name.SCENE_NAME_DEFAULT_MAP;
        }

        string savedScene = CharacterSaveManager.Instance.CurrentGlobalData.currentSceneName;
        Debug.Log($"[GameLoading] savedScene: '{savedScene}'");

        if (!string.IsNullOrEmpty(savedScene) && savedScene.StartsWith(Def_Name.SCENE_NAME_START_MAP))
        {
            Debug.Log($"[GameLoading] 저장된 씬 사용: {savedScene}");
            return savedScene;
        }

        Debug.Log($"[GameLoading] 기본 맵 사용: {Def_Name.SCENE_NAME_DEFAULT_MAP}");
        return Def_Name.SCENE_NAME_DEFAULT_MAP;
    }
}