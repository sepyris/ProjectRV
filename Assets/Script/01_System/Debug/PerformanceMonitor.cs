using UnityEngine;
using TMPro;


/// 게임 성능을 실시간으로 표시하는 간단한 모니터
/// FPS, 메모리 사용량, CPU 프레임 시간 등을 표시

public class PerformanceMonitor : MonoBehaviour
{
    [Header("UI 설정")]
    public TextMeshProUGUI fpsText;
    public bool showDetailedInfo = true;

    [Header("업데이트 설정")]
    [Range(0.1f, 2f)]
    public float updateInterval = 0.5f; // 업데이트 주기 (초)

    [Header("색상 설정")]
    public Color goodFpsColor = Color.green;     // 60 FPS 이상
    public Color okayFpsColor = Color.yellow;    // 30-60 FPS
    public Color badFpsColor = Color.red;        // 30 FPS 미만

    [Header("단축키 설정")]
    public KeyCode toggleKey = KeyCode.F3; // 표시/숨김 토글

    // 내부 변수
    private float deltaTime = 0f;
    private float updateTimer = 0f;
    private int frameCount = 0;
    private float fps = 0f;

    // 메모리 정보
    private float totalMemoryMB = 0f;
    private float usedMemoryMB = 0f;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (fpsText == null)
        {
            Debug.LogWarning("[PerformanceMonitor] FPS Text가 설정되지 않았습니다!");
        }
    }

    void Update()
    {
        // 토글 키 입력
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleDisplay();
        }

        if (fpsText == null || !fpsText.gameObject.activeSelf) return;

        // 프레임 시간 누적
        deltaTime += Time.unscaledDeltaTime;
        frameCount++;
        updateTimer += Time.unscaledDeltaTime;

        // 업데이트 주기마다 FPS 계산 및 표시
        if (updateTimer >= updateInterval)
        {
            fps = frameCount / deltaTime;

            // 메모리 정보 업데이트
            UpdateMemoryInfo();

            // UI 업데이트
            UpdateDisplay();

            // 리셋
            deltaTime = 0f;
            frameCount = 0;
            updateTimer = 0f;
        }
    }

    void UpdateMemoryInfo()
    {
        // Unity가 할당한 총 메모리
        totalMemoryMB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576f;

        // Unity가 예약한 메모리
        usedMemoryMB = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / 1048576f;
    }

    void UpdateDisplay()
    {
        if (fpsText == null) return;

        string displayText = "";

        // FPS 표시
        displayText += $"FPS: {Mathf.RoundToInt(fps)}";

        if (showDetailedInfo)
        {
            // 프레임 시간 (ms)
            float frameTime = (1000f / fps);
            displayText += $"\nFrame Time: {frameTime:F1}ms";

            // 메모리 정보
            displayText += $"\nMemory: {totalMemoryMB:F1}MB";
            displayText += $"\nReserved: {usedMemoryMB:F1}MB";

            // V-Sync 정보
            displayText += $"\nV-Sync: {(QualitySettings.vSyncCount > 0 ? "ON" : "OFF")}";

            // 타겟 FPS
            if (Application.targetFrameRate > 0)
            {
                displayText += $"\nTarget FPS: {Application.targetFrameRate}";
            }
        }

        fpsText.text = displayText;

        // FPS에 따른 색상 변경
        if (fps >= 60f)
            fpsText.color = goodFpsColor;
        else if (fps >= 30f)
            fpsText.color = okayFpsColor;
        else
            fpsText.color = badFpsColor;
    }

    public void ToggleDisplay()
    {
        if (fpsText != null)
        {
            fpsText.gameObject.SetActive(!fpsText.gameObject.activeSelf);
        }
    }

    public void ShowDisplay()
    {
        if (fpsText != null)
        {
            fpsText.gameObject.SetActive(true);
        }
    }

    public void HideDisplay()
    {
        if (fpsText != null)
        {
            fpsText.gameObject.SetActive(false);
        }
    }

    // 현재 FPS 가져오기
    public float GetCurrentFPS()
    {
        return fps;
    }
}