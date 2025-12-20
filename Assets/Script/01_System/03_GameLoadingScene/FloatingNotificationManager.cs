using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 고정 위치에 표시되는 알림 메시지 매니저
/// 여러 메시지가 동시에 표시되며 Vertical Layout으로 정렬됨
/// </summary>
public class FloatingNotificationManager : MonoBehaviour
{
    public static FloatingNotificationManager Instance { get; private set; }

    [Header("UI References")]
    public Transform messageContainer; // Vertical Layout Group이 있는 컨테이너
    public GameObject messagePrefab; // FloatingMessage 프리팹

    [Header("Settings")]
    public int maxMessages = 5; // 최대 표시 메시지 수
    public float messageSpacing = 10f; // 메시지 간격

    [Header("Default Settings")]
    public float defaultDuration = 2f;
    public float defaultMoveSpeed = 50f;
    public Color defaultTextColor = Color.white;

    private Queue<GameObject> activeMessages = new Queue<GameObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        ClearAllMessages();
    }

    void Start()
    {
        // Vertical Layout Group 설정 확인
        if (messageContainer != null)
        {
            VerticalLayoutGroup layout = messageContainer.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = messageContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.spacing = messageSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }
    }

    /// <summary>
    /// 일반 알림 메시지 표시
    /// </summary>
    public void ShowNotification(string message)
    {
        ShowNotification(message, defaultTextColor);
    }

    /// <summary>
    /// 색상 지정 알림 메시지 표시
    /// </summary>
    public void ShowNotification(string message, Color textColor)
    {
        if (messagePrefab == null || messageContainer == null)
        {
            Debug.LogWarning("[FloatingNotificationManager] 프리팹 또는 컨테이너가 설정되지 않았습니다!");
            return;
        }

        // 최대 개수 초과 시 가장 오래된 메시지 제거
        while (activeMessages.Count >= maxMessages)
        {
            GameObject oldest = activeMessages.Dequeue();
            if (oldest != null)
            {
                Destroy(oldest);
            }
        }

        // 새 메시지 생성
        GameObject messageObj = Instantiate(messagePrefab, messageContainer);
        FloatingMessage floatingMsg = messageObj.GetComponent<FloatingMessage>();

        if (floatingMsg != null)
        {
            floatingMsg.duration = defaultDuration;
            floatingMsg.moveSpeed = defaultMoveSpeed;
            floatingMsg.Show(message, textColor);
        }

        activeMessages.Enqueue(messageObj);
    }

    /// <summary>
    /// 모든 메시지 즉시 제거
    /// </summary>
    public void ClearAllMessages()
    {
        while (activeMessages.Count > 0)
        {
            GameObject msg = activeMessages.Dequeue();
            if (msg != null)
            {
                Destroy(msg);
            }
        }
        if (messageContainer.childCount > 0)
        {
            for (int i = 0; i < messageContainer.childCount; i++)
            {
                Destroy(messageContainer.GetChild(i).gameObject);
            }
        }
    }
}