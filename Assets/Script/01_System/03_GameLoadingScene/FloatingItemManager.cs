using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아이템 획득 메시지 매니저
/// 아이콘 + 텍스트로 구성된 메시지를 표시
/// </summary>
public class FloatingItemManager : MonoBehaviour
{
    public static FloatingItemManager Instance { get; private set; }

    [Header("UI References")]
    public Transform messageContainer; // Vertical Layout Group이 있는 컨테이너
    public GameObject messagePrefab; // FloatingMessage 프리팹 (아이콘 포함)

    [Header("Settings")]
    public int maxMessages = 5;

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
    }

    void Start()
    {
        // Vertical Layout Group 설정
        if (messageContainer != null)
        {
            VerticalLayoutGroup layout = messageContainer.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = messageContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            }
        }
    }

    /// <summary>
    /// 아이템 획득 메시지 표시 (아이콘 없음)
    /// </summary>
    public void ShowItemMessage(string itemName, int quantity)
    {
        string message = $"{itemName} + {quantity}";
        ShowItemMessage(message, null, defaultTextColor);
    }

    /// <summary>
    /// 아이템 획득 메시지 표시 (아이콘 있음)
    /// </summary>
    public void ShowItemMessage(string itemName, int quantity, Sprite icon)
    {
        string message = $"{itemName} + {quantity}";
        ShowItemMessage(message, icon, defaultTextColor);
    }

    /// <summary>
    /// 아이템 획득 메시지 표시 (전체 설정)
    /// </summary>
    public void ShowItemMessage(string message, Sprite icon, Color textColor)
    {
        if (messagePrefab == null || messageContainer == null)
        {
            Debug.LogWarning("[FloatingItemManager] 프리팹 또는 컨테이너가 설정되지 않았습니다!");
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

            if (icon != null)
            {
                floatingMsg.Show(message, icon, textColor);
            }
            else
            {
                floatingMsg.Show(message, textColor);
            }
        }

        activeMessages.Enqueue(messageObj);
    }

    /// <summary>
    /// ItemData를 사용하여 아이템 획득 메시지 표시
    /// </summary>
    public void ShowItemAcquired(ItemData itemData, int quantity)
    {
        if (itemData == null) return;

        Sprite icon = null;
        if (!string.IsNullOrEmpty(itemData.iconPath))
        {
            icon = Resources.Load<Sprite>(itemData.iconPath);
        }

        // 아이템 타입별 색상
        Color color = GetItemTypeColor(itemData.itemType);
        ShowItemMessage(itemData.itemName, quantity, icon);
    }

    /// <summary>
    /// 아이템 타입별 색상 반환
    /// </summary>
    private Color GetItemTypeColor(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Equipment:
                return new Color(1f, 0.8f, 0.5f); // 골드색
            case ItemType.Consumable:
                return new Color(0.5f, 1f, 0.5f); // 초록색
            case ItemType.Material:
                return new Color(0.7f, 0.7f, 1f); // 파란색
            case ItemType.QuestItem:
                return new Color(1f, 0.5f, 0.8f); // 분홍색
            default:
                return Color.white;
        }
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
    }
}