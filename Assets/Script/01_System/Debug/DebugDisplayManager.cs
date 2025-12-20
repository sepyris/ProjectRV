using UnityEngine;
using TMPro;

public class DebugDisplayManager : MonoBehaviour
{
    private static DebugDisplayManager _instance;

    public static DebugDisplayManager Instance
    {
        get
        {
            // 아직 인스턴스가 없으면 자동으로 씬에서 찾기 시도
            if (_instance == null)
            {
                _instance = FindObjectOfType<DebugDisplayManager>();

                if (_instance == null)
                {
                    Debug.LogWarning("[DebugDisplayManager] Instance not found. Ensure one exists in the initial scene.");
                }
            }
            return _instance;
        }
    }

    [Header("UI Text Reference")]
    public TMP_Text statusText;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (statusText == null)
        {
            Debug.LogWarning("[DebugDisplayManager] TMP_Text (statusText) is not assigned!");
        }
    }

    // 내부 메서드 (메시지 표시)
    private void DisplayStatusInternal(string message, Color color)
    {
        if (statusText == null)
        {
            Debug.LogWarning("[DebugDisplayManager] TMP_Text is missing. Message: " + message);
            return;
        }

        statusText.text = message;
        statusText.color = color;

        // TODO: 나중에 코루틴으로 일정 시간 뒤 메시지 숨김 구현
    }

    // 외부 호출용 (Instance를 통해)
    public void DisplayStatus(string message, Color color)
    {
        DisplayStatusInternal(message, color);
    }

    void Update()
    {
        
    }

    // --- Static 외부 호출 API ---
    public static void DisplayError(string localizationKey)
    {
        if (Instance == null) return;
        if (LocalizationManager.Instance == null)
        {
            Debug.LogWarning("[DebugDisplayManager] LocalizationManager is missing.");
            return;
        }

        string message = LocalizationManager.Instance.GetLocalizedValue(localizationKey);
        Instance.DisplayStatusInternal(message, Color.red);
    }

    public static void DisplaySuccess(string localizationKey)
    {
        if (Instance == null) return;
        if (LocalizationManager.Instance == null)
        {
            Debug.LogWarning("[DebugDisplayManager] LocalizationManager is missing.");
            return;
        }

        string message = LocalizationManager.Instance.GetLocalizedValue(localizationKey);
        Instance.DisplayStatusInternal(message, Color.green);
    }
}
