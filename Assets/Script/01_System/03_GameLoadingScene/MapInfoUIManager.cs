using TMPro;
using UnityEngine;

public class MapInfoUIManager : MonoBehaviour
{
    private static MapInfoUIManager instance;
    public static MapInfoUIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<MapInfoUIManager>();
            }
            return instance;
        }
    }

    [Header("UI References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform mapInfoPanel;
    [SerializeField] private TextMeshProUGUI mapNameText;
    [SerializeField] private TextMeshProUGUI mapLevelText;

    [Header("Settings")]
    [SerializeField] private Vector2 screenOffset = new Vector2(0, 100);

    private Camera mainCamera;
    private Vector3 currentWorldPosition; // 현재 추적 중인 월드 위치
    private bool isActive = false; // UI가 활성화 상태인지

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (canvas == null)
        {
            canvas = GetComponentInChildren<Canvas>();
        }

        if (mapInfoPanel != null)
        {
            mapInfoPanel.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        // UI가 활성화되어 있을 때만 위치 업데이트
        if (isActive && mapInfoPanel != null && mapInfoPanel.gameObject.activeSelf)
        {
            UpdatePosition();
        }
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        mainCamera = Camera.main;
        ClearMapInfo();
    }

    
    /// 현재 월드 위치를 기준으로 UI 위치 업데이트
    
    private void UpdatePosition()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null) return;

        // 월드 좌표를 스크린 좌표로 변환
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(currentWorldPosition);

        // 화면 밖이면 숨기기
        if (screenPosition.z < 0 || screenPosition.x < 0 || screenPosition.x > Screen.width ||
            screenPosition.y < 0 || screenPosition.y > Screen.height)
        {
            if (mapInfoPanel.gameObject.activeSelf)
            {
                mapInfoPanel.gameObject.SetActive(false);
            }
            return;
        }
        else
        {
            if (!mapInfoPanel.gameObject.activeSelf)
            {
                mapInfoPanel.gameObject.SetActive(true);
            }
        }

        // 스크린 좌표에 오프셋 추가
        screenPosition += (Vector3)screenOffset;

        // RectTransform 위치 설정
        mapInfoPanel.position = screenPosition;
    }

    
    /// 월드 위치 설정
    
    public void SetWorldPosition(Vector3 worldPosition)
    {
        currentWorldPosition = worldPosition;
        UpdatePosition();
    }

    
    /// 맵 정보 표시
    
    public void SetMapInfo(string mapid, Vector3 worldPosition)
    {
        if (mapInfoPanel == null || MapInfoManager.Instance == null) return;

        if (string.IsNullOrEmpty(mapid)) return;

        // 맵 정보 가져오기
        var mapInfo = MapInfoManager.Instance.GetMapInfo(mapid);
        if (mapInfo == null) return;

        // 맵 이름 설정
        string displayName = mapInfo.mapName;
        if (displayName.Contains(" - "))
        {
            displayName = displayName.Replace(" - ", "\n");
        }

        if (mapNameText != null)
        {
            mapNameText.text = displayName;
        }

        // 추천 레벨 설정
        if (mapLevelText != null)
        {
            mapLevelText.text = mapInfo.mapRecommendedLevel;
        }

        // 위치 설정 및 활성화
        currentWorldPosition = worldPosition;
        isActive = true;
        mapInfoPanel.gameObject.SetActive(true);
        UpdatePosition();
    }

    
    /// 맵 정보 숨기기
    
    public void ClearMapInfo()
    {
        isActive = false;

        if (mapNameText != null)
        {
            mapNameText.text = string.Empty;
        }

        if (mapLevelText != null)
        {
            mapLevelText.text = string.Empty;
        }

        if (mapInfoPanel != null)
        {
            mapInfoPanel.gameObject.SetActive(false);
        }
    }
}