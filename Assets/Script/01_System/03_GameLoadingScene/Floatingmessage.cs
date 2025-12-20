using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 개별 플로팅 메시지 항목
/// 위로 올라가면서 페이드아웃되는 메시지
/// </summary>
public class FloatingMessage : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI messageText;
    public Image iconImage; // 아이템 획득용 아이콘 (선택사항)
    public CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    public float duration = 2f;
    public float moveSpeed = 50f; // 위로 올라가는 속도
    public float fadeStartTime = 1f; // 페이드 시작 시간

    private RectTransform rectTransform;
    private Vector3 startPosition;
    private float elapsedTime = 0f;
    private bool isAnimating = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    /// <summary>
    /// 텍스트만 있는 메시지 표시
    /// </summary>
    public void Show(string text, Color textColor)
    {
        if (messageText != null)
        {
            messageText.text = text;
            messageText.color = textColor;
        }

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(false);
        }

        StartAnimation();
    }

    /// <summary>
    /// 아이콘 + 텍스트 메시지 표시 (아이템 획득용)
    /// </summary>
    public void Show(string text, Sprite icon, Color textColor)
    {
        if (messageText != null)
        {
            messageText.text = text;
            messageText.color = textColor;
        }

        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
            iconImage.gameObject.SetActive(true);
        }
        else if (iconImage != null)
        {
            iconImage.gameObject.SetActive(false);
        }

        StartAnimation();
    }

    void StartAnimation()
    {
        startPosition = rectTransform.anchoredPosition;
        elapsedTime = 0f;
        isAnimating = true;
        canvasGroup.alpha = 1f;

        StartCoroutine(AnimateCoroutine());
    }

    IEnumerator AnimateCoroutine()
    {
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            // 위로 이동
            float moveAmount = moveSpeed * Time.deltaTime;
            rectTransform.anchoredPosition += Vector2.up * moveAmount;

            // 페이드아웃 (후반부)
            if (elapsedTime >= fadeStartTime)
            {
                float fadeProgress = (elapsedTime - fadeStartTime) / (duration - fadeStartTime);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeProgress);
            }

            yield return null;
        }

        // 애니메이션 완료 후 제거
        isAnimating = false;
        Destroy(gameObject);
    }

    public bool IsAnimating()
    {
        return isAnimating;
    }
}