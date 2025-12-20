using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MapController : MonoBehaviour
{
    [Header("Map Info")]
    [SerializeField] private string mapId;
    [SerializeField] private bool showDebugLogs = true;

    [Header("Player Spawn")]
    [SerializeField] private Transform defaultSpawnPoint;
    [SerializeField] private bool restorePlayerPosition = true;

    [Header("Loading Settings")]
    [SerializeField] private float minDisplayTime = 0.5f;
    [SerializeField] private float fadeOutDelay = 0.2f;

    public string SceneName = "";

    private bool isInitialized = false;

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == gameObject.scene.name && !isInitialized)
        {
            StartCoroutine(InitializeMapRoutine());
        }
    }

    private IEnumerator InitializeMapRoutine()
    {
        float startTime = Time.time;

        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowGlobalLoading();
            LogDebug("[Map] 로딩 화면 표시");
        }

        LogDebug($"[Map] 맵 초기화 시작: {mapId}");

        yield return null;
        yield return null;

        if (restorePlayerPosition)
        {
            RestorePlayerPosition();
        }

        CheckAndRegisterSafeZone();

        InitializeCamera();

        if (MiniMapManager.Instance != null)
        {
            MiniMapManager.Instance.ReInitialize();
            if (MapInfoManager.Instance != null)
            {
                MiniMapManager.Instance.SetMapName(MapInfoManager.Instance.GetMapName(mapId));
            }

            LogDebug("[Map] 미니맵 재초기화 완료");
        }

        float elapsedTime = Time.time - startTime;
        if (elapsedTime < minDisplayTime)
        {
            yield return new WaitForSeconds(minDisplayTime - elapsedTime);
        }

        yield return new WaitForSeconds(fadeOutDelay);

        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.HideGlobalLoading();
            LogDebug("[Map] 로딩 화면 숨김");
        }

        // ===== 추가: 로딩 화면 숨긴 후 한 프레임 대기 =====
        yield return null;
        // =============================================

        // ===== 강화: 플레이어 컨트롤 확실히 해제 =====
        if (PlayerController.Instance != null)
        {
            Debug.Log("[Map] 플레이어 컨트롤 강제 활성화");
            PlayerController.Instance.SetControlsLocked(false);

            // 입력 시스템 재활성화
            yield return null;
            Debug.Log("[Map] 플레이어 게임오브젝트 재활성화 완료");
        }
        // ===========================================

        SceneName = SceneManager.GetActiveScene().name;

        isInitialized = true;
        LogDebug($"[Map] 맵 초기화 완료: {mapId}");
        // 미니맵 초기화
        MiniMapManager.Instance.SetInitialize();
    }

    // ===== 추가: 안전 지역 체크 메서드 =====
    /// <summary>
    /// 현재 맵이 안전 지역(Town)이면 리스폰 포인트로 등록
    /// </summary>
    private void CheckAndRegisterSafeZone()
    {
        if (MapInfoManager.Instance == null) return;

        var mapData = MapInfoManager.Instance.GetMapInfo(mapId);
        if (mapData == null) return;

        // mapType이 "Town"인지 확인
        if (mapData.mapType.ToLower() == "town")
        {
            RegisterAsRespawnPoint();
        }
    }

    /// <summary>
    /// 현재 맵을 리스폰 포인트로 등록
    /// </summary>
    private void RegisterAsRespawnPoint()
    {
        if (CharacterSaveManager.Instance == null) return;

        string currentScene = gameObject.scene.name;
        Vector3 respawnPosition = defaultSpawnPoint != null ? defaultSpawnPoint.position : Vector3.zero;
        string spawnPointId = defaultSpawnPoint != null ? "town_spawn" : "default";

        // GlobalSaveData에 저장
        var globalData = CharacterSaveManager.Instance.CurrentGlobalData;
        globalData.lastSafeZoneScene = currentScene;
        globalData.lastSafeZoneSpawnPoint = spawnPointId;
        globalData.lastSafeZonePosition = respawnPosition;

        LogDebug($"[Map]  안전 지역 등록: {currentScene} (위치: {respawnPosition})");
    }
    // =====================================

    private void RestorePlayerPosition()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        player.SetIdleAnimation();

        if (player == null)
        {
            Debug.LogWarning("[Map] PlayerController를 찾을 수 없습니다!");
            return;
        }

        if (CharacterSaveManager.Instance != null &&
            !string.IsNullOrEmpty(CharacterSaveManager.Instance.NextSceneSpawnPointid))
        {
            string spawnPointid = CharacterSaveManager.Instance.NextSceneSpawnPointid;
            MapSpawnPoint targetSpawn = FindSpawnPoint(spawnPointid);

            if (targetSpawn != null)
            {
                player.transform.position = targetSpawn.transform.position;
                Debug.Log($"[Map] 플레이어를 스폰 포인트 '{spawnPointid}'로 이동: {targetSpawn.transform.position}");
                CharacterSaveManager.Instance.NextSceneSpawnPointid = "";
                return;
            }
        }

        if (CharacterSaveManager.Instance != null &&
            CharacterSaveManager.Instance.CurrentCharacter != null)
        {
            Vector3 savedPosition = CharacterSaveManager.Instance.CurrentCharacter.position;

            if (savedPosition != Vector3.zero)
            {
                player.transform.position = savedPosition;
                LogDebug($"[Map] 저장된 위치로 복원: {savedPosition}");
                return;
            }
        }

        if (defaultSpawnPoint != null)
        {
            player.transform.position = defaultSpawnPoint.position;
            LogDebug($"[Map] 기본 스폰 포인트로 이동: {defaultSpawnPoint.position}");
        }
        else
        {
            player.transform.position = new Vector3(-999,-999,0);
            Debug.LogWarning("[Map] 기본 스폰 포인트가 설정되지 않았습니다!");
        }
    }

    private MapSpawnPoint FindSpawnPoint(string spawnPointid)
    {
        MapSpawnPoint[] allSpawnPoints = FindObjectsOfType<MapSpawnPoint>();

        foreach (var spawnPoint in allSpawnPoints)
        {
            if (spawnPoint.spawnPointid == spawnPointid)
            {
                return spawnPoint;
            }
        }

        return null;
    }

    private void InitializeCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.CompareTag("TmpCamera"))
        {
            Destroy(mainCam.gameObject);
            LogDebug("[Map] 임시 카메라 제거");
        }

        CameraController cameraCtrl = FindGameCameraController();
        if (cameraCtrl != null)
        {
            cameraCtrl.ReInitialize();
            LogDebug("[Map] 카메라 재초기화 완료");
        }
    }

    private CameraController FindGameCameraController()
    {
        Camera[] allCameras = FindObjectsOfType<Camera>(true);

        foreach (var cam in allCameras)
        {
            if (cam.CompareTag("GameCamera"))
            {
                return cam.GetComponent<CameraController>();
            }
        }

        return null;
    }

    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log(message);
        }
    }

    private void OnDrawGizmos()
    {
        if (defaultSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(defaultSpawnPoint.position, 0.5f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(defaultSpawnPoint.position + Vector3.up, "Default Spawn");
#endif
        }
    }
}