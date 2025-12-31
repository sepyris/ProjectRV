using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraPersistenceHelper : MonoBehaviour
{
    public static CameraPersistenceHelper Instance { get; private set; }

    void Awake()
    {
        //  싱글톤 중복 방지
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 전역 CameraController 인스턴스가 있으면 기준 카메라로 사용
        var cameraController = CameraController.Instance;
        Camera persistentCam = cameraController != null ? cameraController.GetComponent<Camera>() : Camera.main;

        if (persistentCam == null)
        {
            Debug.LogWarning("[CameraPersistenceHelper] 기준 카메라를 찾을 수 없습니다.");
            return;
        }

        // 1️ 씬의 카메라 정리
        Camera[] allCams = FindObjectsOfType<Camera>(true);
        foreach (var cam in allCams)
        {
            if (cam == persistentCam) continue;
            if (cam.gameObject.CompareTag("KeepCamera")) continue;

            var al = cam.GetComponent<AudioListener>();
            if (al != null) al.enabled = false;
            cam.enabled = false;

            Debug.Log($"[CameraPersistenceHelper] Disabled scene camera: {cam.gameObject.name}");
        }

        // 2️ Canvas 재연결
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
        foreach (var cv in allCanvases)
        {
            if (cv.renderMode == RenderMode.ScreenSpaceCamera)
            {
                cv.worldCamera = persistentCam;
                Debug.Log($"[CameraPersistenceHelper] Canvas '{cv.gameObject.name}' assigned to persistent camera.");
            }
        }
    }
}
