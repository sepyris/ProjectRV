using UnityEngine;
using TMPro;
using System.Collections;


/// 캐릭터/몬스터 위에 표시되는 데미지 텍스트
/// 월드 스페이스에서 위로 떠오르며 페이드아웃

public class FloatingDamageText : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI damageText;
    public CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    public float duration = 1.5f;
    public float moveSpeed = 2f; // 월드 스페이스 이동 속도
    public float fadeStartTime = 0.8f;
    public Vector3 randomOffset = new Vector3(0.5f, 0.5f, 0f); // 랜덤 오프셋 범위

    private float elapsedTime = 0f;
    private Vector3 velocity;

    void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    
    /// 데미지 텍스트 표시
    
    public void Show(int damage, Color textColor, bool isCritical = false)
    {
        if (damageText != null)
        {
            damageText.text = damage.ToString();
            damageText.color = textColor;

            // 크리티컬이면 크기 증가
            if (isCritical)
            {
                damageText.fontSize *= 1.3f;
                damageText.text = damageText.text;
            }
        }

        // 랜덤한 방향으로 초기 속도 설정
        float randomX = Random.Range(-randomOffset.x, randomOffset.x);
        float randomY = Random.Range(0f, randomOffset.y);
        velocity = new Vector3(randomX, moveSpeed + randomY, 0f);

        StartCoroutine(AnimateCoroutine());
    }

    
    /// 텍스트만 표시 (힐, 버프 등)
    
    public void Show(string text, Color textColor)
    {
        if (damageText != null)
        {
            damageText.text = text;
            damageText.color = textColor;
        }

        // 랜덤한 방향으로 초기 속도 설정
        float randomX = Random.Range(-randomOffset.x, randomOffset.x);
        float randomY = Random.Range(0f, randomOffset.y);
        velocity = new Vector3(randomX, moveSpeed + randomY, 0f);

        StartCoroutine(AnimateCoroutine());
    }

    IEnumerator AnimateCoroutine()
    {
        canvasGroup.alpha = 1f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            // 위로 이동 (감속)
            transform.position += velocity * Time.deltaTime;
            velocity.y *= 0.95f; // 감속

            // 페이드아웃
            if (elapsedTime >= fadeStartTime)
            {
                float fadeProgress = (elapsedTime - fadeStartTime) / (duration - fadeStartTime);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeProgress);
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}