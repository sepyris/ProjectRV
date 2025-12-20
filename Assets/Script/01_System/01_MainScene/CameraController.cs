// CameraController.cs
using Definitions;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    private Transform target;
    public float smoothSpeed = 0.125f;

    private Bounds worldBounds;
    private Vector2 minCameraBoundary;
    private Vector2 maxCameraBoundary;
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();

        // 싱글턴/영속화 처리
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            if (this.gameObject != Instance.gameObject)
            {
                Destroy(this.gameObject);
                return;
            }
        }

        FindPlayerTarget();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.name.StartsWith(Def_Name.SCENE_NAME_START_MAP))
        {
            // 게임 콘텐츠 씬이 아니면 무시
            return;
        }

        // 씬 로드 시 자동 재초기화
        ReInitialize();
    }

    void Start()
    {
        SetupWorldBounds();

        if (target != null)
        {
            transform.position = new Vector3(target.position.x, target.position.y, transform.position.z);
        }
    }

    /// <summary>
    /// 씬 로드 후 카메라를 재초기화합니다.
    /// GameDataManager에서 호출됩니다.
    /// </summary>
    public void ReInitialize()
    {
        // 1. 플레이어 타겟 다시 찾기
        FindPlayerTarget();

        // 2. 월드 경계 다시 설정
        SetupWorldBounds();

        // 3. 카메라 위치 즉시 설정
        if (target != null)
        {
            Vector3 initialPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
            transform.position = initialPosition;

            // 4. 경계 제한 적용
            if (minCameraBoundary != Vector2.zero || maxCameraBoundary != Vector2.zero)
            {
                float clampedX = Mathf.Clamp(transform.position.x, minCameraBoundary.x, maxCameraBoundary.x);
                float clampedY = Mathf.Clamp(transform.position.y, minCameraBoundary.y, maxCameraBoundary.y);
                transform.position = new Vector3(clampedX, clampedY, transform.position.z);
            }

            Debug.Log("[CameraController] 씬 로드 후 카메라 초기 위치 재설정 및 바운드 적용 완료.");
        }
        else
        {
            Debug.LogWarning("[CameraController] ReInitialize 시 플레이어 타겟을 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 플레이어를 찾아 타겟으로 설정합니다.
    /// </summary>
    private void FindPlayerTarget()
    {
        //플레이어 오브젝트 찾기
        GameObject playerObject = GameObject.FindGameObjectWithTag(Def_Name.PLAYER_TAG);
        //플레이어가 존재하고, 현재 씬이 게임 맵 씬인 경우에만 타겟 설정
        if (playerObject != null && SceneManager.GetActiveScene().name.StartsWith(Def_Name.SCENE_NAME_START_MAP))
        {
            target = playerObject.transform;
            Debug.Log("[CameraController] 플레이어 타겟 설정 완료.");
        }
        else
        {
            target = null;
        }
    }

    /// <summary>
    /// 맵 경계를 설정합니다.
    /// WorldBorder 태그를 가진 오브젝트들을 찾아 카메라 이동 범위를 제한합니다.
    /// </summary>
    private void SetupWorldBounds()
    {
        GameObject[] borderObjects = GameObject.FindGameObjectsWithTag(Def_Name.WORLD_BORDER_TAG);

        if (borderObjects.Length > 0)
        {
            Collider2D firstCollider = borderObjects[0].GetComponent<Collider2D>();
            if (firstCollider == null)
            {
                Debug.LogError("월드 보더 오브젝트에 Collider2D 컴포넌트가 없습니다!");
                return;
            }

            worldBounds = firstCollider.bounds;
            for (int i = 1; i < borderObjects.Length; i++)
            {
                Collider2D otherCollider = borderObjects[i].GetComponent<Collider2D>();
                if (otherCollider != null)
                {
                    worldBounds.Encapsulate(otherCollider.bounds);
                }
            }

            float camHalfHeight = cam.orthographicSize;
            float camHalfWidth = cam.aspect * camHalfHeight;

            minCameraBoundary.x = worldBounds.min.x + camHalfWidth;
            maxCameraBoundary.x = worldBounds.max.x - camHalfWidth;
            minCameraBoundary.y = worldBounds.min.y + camHalfHeight;
            maxCameraBoundary.y = worldBounds.max.y - camHalfHeight;

            Debug.Log($"[CameraController] 월드 경계 설정 완료: Min({minCameraBoundary}), Max({maxCameraBoundary})");
        }
        else
        {
            minCameraBoundary = Vector2.zero;
            maxCameraBoundary = Vector2.zero;
        }
    }

    void LateUpdate()
    {
        if (target == null)
        {
            // 타겟이 없으면 ReInitialize()에 의존
            return;
        }

        Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // 경계 제한 적용
        if (minCameraBoundary != Vector2.zero || maxCameraBoundary != Vector2.zero)
        {
            float clampedX = Mathf.Clamp(transform.position.x, minCameraBoundary.x, maxCameraBoundary.x);
            float clampedY = Mathf.Clamp(transform.position.y, minCameraBoundary.y, maxCameraBoundary.y);
            transform.position = new Vector3(clampedX, clampedY, transform.position.z);
        }
    }
}