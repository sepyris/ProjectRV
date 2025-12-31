using UnityEngine;
using UnityEngine.UI;
using Definitions;
using TMPro;

public class MiniMapManager : MonoBehaviour, IClosableUI
{
    public static MiniMapManager Instance { get; private set; }

    [Header("References")]
    public Camera minimapCamera;
    public RawImage minimapDisplay;
    public RenderTexture minimapTexture;
    public TextMeshProUGUI Map_Name;

    [Header("Maximize_References")]
    public GameObject maximize_Minimap_Panel;
    public Button maximize_Open_Button;
    public Button maximize_Close_Button;
    public TextMeshProUGUI maximize_Map_Name;
    public RawImage maximize_Display;

    [Header("Player Minimap Object")]
    [Tooltip("플레이어의 미니맵 표시 오브젝트 (서클 등)")]
    public GameObject playerMinimapObject;

    [Header("Settings")]
    public float padding = 2f;
    public float followSmooth = 8f;
    public Color backgroundColor = Color.black;

    [Header("Player Object Scale Settings")]
    [Tooltip("기준 맵 크기 (50x50일 때)")]
    public float referenceMapSize = 50f;
    [Tooltip("기준 카메라 orthographicSize (27일 때)")]
    public float referenceCameraSize = 27f;
    [Tooltip("기준 플레이어 오브젝트 크기 (1x1일 때)")]
    public float referencePlayerScale = 1f;

    [Header("Game Scene Detection")]
    [Tooltip("게임 씬이 로드될 때까지 대기")]
    public bool waitForGameScene = true;
    public float updateInterval = 1f;  // 플레이어 재검색 간격

    [Header("Default Size (WorldBorder가 없을 때)")]
    [Tooltip("WorldBorder를 찾지 못했을 때 사용할 기본 orthographicSize")]
    public float defaultOrthographicSize = 50f;

    private Transform player;
    private float nextUpdateTime = 0f;
    private bool isSetupComplete = false;

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
            return;
        }
    }

    void Start()
    {
        SetupMinimapCamera();
        SetupRenderTextureIfNeeded();
        SetupMaximizePanel();

        Debug.Log("[MiniMap] 초기 설정 완료. 게임 씬 대기 중...");
    }

    void Update()
    {
        // 주기적으로 게임씬 확인 및 플레이어 재검색
        if (!isSetupComplete || player == null)
        {
            if (Time.time >= nextUpdateTime)
            {
                nextUpdateTime = Time.time + updateInterval;
                TryFindGameScene();
            }
        }
    }

    // ===== 새로 추가: Maximize Panel 설정 =====
    private void SetupMaximizePanel()
    {
        // 초기에는 패널 숨기기
        if (maximize_Minimap_Panel != null)
        {
            maximize_Minimap_Panel.SetActive(false);
        }

        // Open 버튼 이벤트 등록
        if (maximize_Open_Button != null)
        {
            maximize_Open_Button.onClick.RemoveAllListeners();
            maximize_Open_Button.onClick.AddListener(OpenMaximizeMap);
        }

        // Close 버튼 이벤트 등록
        if (maximize_Close_Button != null)
        {
            maximize_Close_Button.onClick.RemoveAllListeners();
            maximize_Close_Button.onClick.AddListener(CloseMaximizeMap);
        }

        // maximize_Display에도 동일한 RenderTexture 연결
        if (maximize_Display != null && minimapTexture != null)
        {
            maximize_Display.texture = minimapTexture;
        }

        Debug.Log("[MiniMap] Maximize Panel 설정 완료");
    }

    
    /// 확대 맵 열기
    
    private void OpenMaximizeMap()
    {
        if (maximize_Minimap_Panel != null)
        {
            if (!maximize_Minimap_Panel.activeSelf)
            {
                maximize_Minimap_Panel.SetActive(true);
                PlayerHUD.Instance?.RegisterUI(this);

                PlayerController.Instance.SetControlsLocked(true);
                Debug.Log("[MiniMap] 확대 맵 열림");
            }
        }
    }

    
    /// 확대 맵 닫기
    
    private void CloseMaximizeMap()
    {
        if (maximize_Minimap_Panel != null)
        {
            maximize_Minimap_Panel.SetActive(false);
            PlayerHUD.Instance?.UnregisterUI(this);
            PlayerController.Instance.SetControlsLocked(false);
            Debug.Log("[MiniMap] 확대 맵 닫힘");
        }
    }
    // =========================================

    public void ReInitialize()
    {
        SetupMinimapCamera();
        SetupRenderTextureIfNeeded();
        CenterCameraOnWorldBounds();
        UpdatePlayerMinimapObjectScale(); // 플레이어 오브젝트 크기 업데이트
    }

    private void CenterCameraOnWorldBounds()
    {
        GameObject[] borders = GameObject.FindGameObjectsWithTag(Def_Name.WORLD_BORDER_TAG);

        if (borders.Length == 0)
        {
            minimapCamera.transform.position = new Vector3(0, 0, -20f);
            return;
        }

        Collider2D first = borders[0].GetComponent<Collider2D>();
        if (first == null)
        {
            minimapCamera.transform.position = new Vector3(0, 0, -20f);
            return;
        }

        Bounds worldBounds = first.bounds;

        for (int i = 1; i < borders.Length; i++)
        {
            Collider2D c = borders[i].GetComponent<Collider2D>();
            if (c != null)
                worldBounds.Encapsulate(c.bounds);
        }

        Vector3 worldCenter = worldBounds.center;
        worldCenter.z = -20f;

        minimapCamera.transform.position = worldCenter;
    }

    void LateUpdate()
    {
        if (!isSetupComplete || minimapCamera == null) return;

        CenterCameraOnWorldBounds();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;

            // 버튼 이벤트 제거
            if (maximize_Open_Button != null)
                maximize_Open_Button.onClick.RemoveAllListeners();

            if (maximize_Close_Button != null)
                maximize_Close_Button.onClick.RemoveAllListeners();

            // RenderTexture 정리
            if (minimapTexture != null)
            {
                if (minimapCamera != null && minimapCamera.targetTexture == minimapTexture)
                    minimapCamera.targetTexture = null;

                minimapTexture.Release();
                Destroy(minimapTexture);
                minimapTexture = null;
            }
        }
    }

    private void SetupMinimapCamera()
    {
        if (minimapCamera == null)
        {
            GameObject camObj = new GameObject("MiniMap_Camera");
            camObj.tag = "KeepCamera";
            minimapCamera = camObj.AddComponent<Camera>();
            DontDestroyOnLoad(camObj);

            Debug.Log("[MiniMap] 카메라 생성됨");
        }

        minimapCamera.orthographic = true;

        Bounds worldBounds = CalculateWorldBounds();
        if (worldBounds.size.magnitude > 0.1f)
        {
            UpdateCameraSizeFromBounds(worldBounds);
        }
        else
        {
            minimapCamera.orthographicSize = defaultOrthographicSize;
            Debug.Log($"[MiniMap] WorldBorder 없음. 기본 크기 사용: {defaultOrthographicSize}");
        }

        minimapCamera.cullingMask = (Def_Layer_Mask_Values.LAYER_MASK_DEFAULT) | (Def_Layer_Mask_Values.LAYER_MASK_MINIMAP_OBJECT);
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = backgroundColor;
        minimapCamera.depth = -100;
        minimapCamera.transform.position = new Vector3(0, 0, -20f);
        minimapCamera.transform.rotation = Quaternion.identity;
    }

    private void SetupRenderTextureIfNeeded()
    {
        if (minimapCamera == null || minimapDisplay == null)
        {
            Debug.LogWarning("[MiniMap] Camera 또는 RawImage가 없습니다!");
            return;
        }

        if (minimapTexture != null)
        {
            if (minimapCamera.targetTexture == minimapTexture)
                minimapCamera.targetTexture = null;

            if (minimapDisplay.texture == minimapTexture)
                minimapDisplay.texture = null;

            minimapTexture.Release();
            Destroy(minimapTexture);
            minimapTexture = null;

            Debug.Log("[MiniMap] 기존 RenderTexture 해제");
        }

        minimapTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
        minimapTexture.name = "Minimap_RT";
        minimapTexture.filterMode = FilterMode.Bilinear;
        minimapTexture.anisoLevel = 0;
        minimapTexture.useMipMap = false;
        minimapTexture.autoGenerateMips = false;

        minimapTexture.Create();

        Debug.Log("[MiniMap] RenderTexture 생성 및 Create() 호출");

        minimapCamera.targetTexture = minimapTexture;

        minimapDisplay.texture = minimapTexture;
        minimapDisplay.enabled = true;
        minimapDisplay.gameObject.SetActive(true);

        // ===== maximize_Display에도 동일한 텍스처 할당 =====
        if (maximize_Display != null)
        {
            maximize_Display.texture = minimapTexture;
        }
        // ==============================================

        Canvas parentCanvas = minimapDisplay.GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            Debug.Log($"[MiniMap] Canvas: {parentCanvas.name}, RenderMode: {parentCanvas.renderMode}");
            Canvas.ForceUpdateCanvases();
        }

        Debug.Log($"[MiniMap] RenderTexture 연결 완료");
    }

    private void TryFindGameScene()
    {
        if (PlayerController.Instance != null)
        {
            player = PlayerController.Instance.transform;

            // 플레이어 미니맵 오브젝트 자동 검색
            if (playerMinimapObject == null)
            {
                FindPlayerMinimapObject();
            }

            Bounds bounds = CalculateWorldBounds();
            UpdateCameraSizeAndPosition(bounds);

            isSetupComplete = true;
            return;
        }

        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc != null)
        {
            player = pc.transform;

            // 플레이어 미니맵 오브젝트 자동 검색
            if (playerMinimapObject == null)
            {
                FindPlayerMinimapObject();
            }

            Bounds bounds = CalculateWorldBounds();
            UpdateCameraSizeAndPosition(bounds);

            isSetupComplete = true;
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag(Def_Name.PLAYER_TAG);
        if (playerObj != null)
        {
            player = playerObj.transform;

            // 플레이어 미니맵 오브젝트 자동 검색
            if (playerMinimapObject == null)
            {
                FindPlayerMinimapObject();
            }

            Bounds bounds = CalculateWorldBounds();
            UpdateCameraSizeAndPosition(bounds);

            isSetupComplete = true;
            return;
        }

        if (!isSetupComplete)
        {
            Debug.LogWarning("[MiniMap] 플레이어를 찾을 수 없습니다. 대기 중...");
        }
    }

    
    /// 플레이어의 자식 오브젝트 중에서 미니맵 오브젝트를 자동으로 찾습니다.
    /// MinimapObject 레이어를 가진 오브젝트를 찾습니다.
    
    private void FindPlayerMinimapObject()
    {
        if (player == null) return;

        // 플레이어의 모든 자식 중에서 MinimapObject 레이어를 가진 오브젝트 찾기
        Transform[] children = player.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.gameObject.layer == LayerMask.NameToLayer("MinimapObject"))
            {
                playerMinimapObject = child.gameObject;
                Debug.Log($"[MiniMap] 플레이어 미니맵 오브젝트 자동 발견: {child.name}");
                return;
            }
        }

        Debug.LogWarning("[MiniMap] 플레이어의 미니맵 오브젝트를 찾지 못했습니다. Inspector에서 수동으로 할당해주세요.");
    }

    
    /// 현재 카메라 크기에 따라 플레이어 미니맵 오브젝트의 크기를 조정합니다.
    /// 기준: 맵 50x50, 카메라 사이즈 27, 플레이어 크기 1x1
    
    private void UpdatePlayerMinimapObjectScale()
    {
        if (playerMinimapObject == null || minimapCamera == null)
        {
            return;
        }

        // 현재 카메라 사이즈 대비 기준 카메라 사이즈의 비율 계산
        float scaleRatio = minimapCamera.orthographicSize / referenceCameraSize;

        // 새로운 스케일 계산 (기준 스케일 * 비율)
        float newScale = referencePlayerScale * scaleRatio;

        // 스케일 적용
        playerMinimapObject.transform.localScale = new Vector3(newScale, newScale, 1f);

        Debug.Log($"[MiniMap] 플레이어 오브젝트 크기 조정: " +
                  $"카메라사이즈={minimapCamera.orthographicSize:F2}, " +
                  $"비율={scaleRatio:F3}, " +
                  $"새크기={newScale:F3}");
    }

    private Bounds CalculateWorldBounds()
    {
        GameObject[] borders = GameObject.FindGameObjectsWithTag(Def_Name.WORLD_BORDER_TAG);

        if (borders.Length == 0)
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        Collider2D first = borders[0].GetComponent<Collider2D>();
        if (first == null)
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        Bounds world = first.bounds;
        for (int i = 1; i < borders.Length; i++)
        {
            Collider2D c = borders[i].GetComponent<Collider2D>();
            if (c != null) world.Encapsulate(c.bounds);
        }

        Debug.Log($"[MiniMap] 월드 바운드: center={world.center}, size={world.size}");
        return world;
    }

    private void UpdateCameraSizeFromBounds(Bounds worldBounds)
    {
        if (minimapCamera == null) return;

        float halfHeight = worldBounds.extents.y + padding;
        float halfWidth = worldBounds.extents.x + padding;

        float sizeByWidth = halfWidth / minimapCamera.aspect;
        float targetSize = Mathf.Max(halfHeight, sizeByWidth);

        minimapCamera.orthographicSize = targetSize;

        // 카메라 크기가 변경되었으므로 플레이어 오브젝트 크기도 업데이트
        UpdatePlayerMinimapObjectScale();

        Debug.Log($"[MiniMap] 카메라 크기 설정: orthographicSize={targetSize:F2} | " +
                  $"맵크기=({worldBounds.size.x:F1}{worldBounds.size.y:F1}), " +
                  $"halfWidth={halfWidth:F1}, halfHeight={halfHeight:F1}, " +
                  $"sizeByWidth={sizeByWidth:F1}, 카메라비율={minimapCamera.aspect:F2}");
    }

    private void UpdateCameraSizeAndPosition(Bounds worldBounds)
    {
        if (minimapCamera == null) return;

        float halfHeight = worldBounds.extents.y + padding;
        float halfWidth = worldBounds.extents.x + padding;
        float sizeByWidth = halfWidth / minimapCamera.aspect;
        float targetSize = Mathf.Max(halfHeight, sizeByWidth);

        minimapCamera.orthographicSize = targetSize;

        // 카메라 크기가 변경되었으므로 플레이어 오브젝트 크기도 업데이트
        UpdatePlayerMinimapObjectScale();

        if (player != null)
        {
            Vector3 initialPos = player.position;
            initialPos.z = -20f;
            minimapCamera.transform.position = initialPos;
        }

        Debug.Log($"[MiniMap] 카메라 업데이트: orthographicSize={targetSize:F2}, position={minimapCamera.transform.position}");
    }

    public void SetMapName(string Map_Name)
    {
        string tmp = Map_Name;
        this.Map_Name.text = tmp.Replace(" - ", "\n");

        // maximize 맵 이름도 함께 업데이트
        if (maximize_Map_Name != null)
        {
            maximize_Map_Name.text = Map_Name;
        }
    }

    public void Close()
    {
        CloseMaximizeMap();
    }

    public GameObject GetUIPanel()
    {
        return maximize_Minimap_Panel;
    }

    //씬 전환시 초기화를 위한 함수
    public void SetInitialize()
    {
        //Update에서 조건을 확인하여 초기화 하기 때문에 플래그만 변경
        isSetupComplete = false;
        playerMinimapObject = null; // 씬 전환 시 플레이어 오브젝트 참조도 초기화
    }

    
    /// 외부에서 플레이어 미니맵 오브젝트를 수동으로 설정할 때 사용
    
    public void SetPlayerMinimapObject(GameObject obj)
    {
        playerMinimapObject = obj;
        UpdatePlayerMinimapObjectScale();
        Debug.Log($"[MiniMap] 플레이어 미니맵 오브젝트 수동 설정: {obj.name}");
    }
}