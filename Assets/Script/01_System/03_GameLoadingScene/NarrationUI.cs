using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 나레이션 UI 표시 담당
/// </summary>
public class NarrationUI : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private GameObject narrationPanel;
    [SerializeField] private TextMeshProUGUI narrationText;

    [Header("스킵 진행바 (F키 홀드)")]
    [SerializeField] private GameObject skipProgressPanel;
    [SerializeField] private Slider skipProgressBar;
    [SerializeField] private TextMeshProUGUI skipHintText;

    [Header("진행 표시 (선택사항)")]
    [SerializeField] private TextMeshProUGUI waitingText;  // "이동해보세요..." 같은 안내

    private NarrationConfig currentConfig;
    private Coroutine typingCoroutine;
    private bool isTypingComplete = false;

    private void Awake()
    {
        HidePanel(instant: true);

        narrationPanel.SetActive(false);
        if (skipProgressPanel != null)
            skipProgressPanel.SetActive(false);
    }

    /// <summary>
    /// 나레이션 표시 (타이핑 효과 포함)
    /// </summary>
    public void Show(string text, NarrationConfig config)
    {
        currentConfig = config;
        isTypingComplete = false;

        // 스킵 힌트 표시
        if (skipHintText != null && config.canSkip)
        {
            skipHintText.text = $"F키 {config.skipHoldDuration}초 홀드\n건너뛰기";
        }

        // 대기 중 표시 (Conditional 모드일 때만)
        if (waitingText != null)
        {
            waitingText.gameObject.SetActive(config.mode == NarrationMode.Conditional);
            if (config.mode == NarrationMode.Conditional)
            {
                waitingText.text = GetConditionHintText(config.conditionType);
            }
        }

        narrationPanel.SetActive(true);
        StartTyping(text);
    }

    /// <summary>
    /// 타이핑 효과 시작
    /// </summary>
    private void StartTyping(string text)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (currentConfig.useTypingEffect)
        {
            typingCoroutine = StartCoroutine(TypeText(text));
        }
        else
        {
            narrationText.text = text;
            isTypingComplete = true;
        }
    }

    /// <summary>
    /// 타이핑 효과 코루틴
    /// </summary>
    private IEnumerator TypeText(string fullText)
    {
        narrationText.text = "";

        foreach (char c in fullText)
        {
            narrationText.text += c;
            yield return new WaitForSeconds(currentConfig.typingSpeed);
        }

        isTypingComplete = true;
        typingCoroutine = null;
    }

    /// <summary>
    /// 타이핑이 완료되었는지 여부
    /// </summary>
    public bool IsTypingComplete()
    {
        return isTypingComplete;
    }

    /// <summary>
    /// 타이핑 즉시 완료 (스킵용)
    /// </summary>
    public void CompleteTypingImmediately()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        isTypingComplete = true;
    }

    /// <summary>
    /// 나레이션 숨기기
    /// </summary>
    public void HidePanel(bool instant = false)
    {
        // 스킵 진행바 숨김
        if (skipProgressPanel != null)
            skipProgressPanel.SetActive(false);

        narrationPanel.SetActive(false);
    }
    /// <summary>
    /// 나레이션 숨기기
    /// </summary>
    public void HideProgress(bool instant = false)
    {
        // 스킵 진행바 숨김
        if (skipProgressPanel != null)
            skipProgressPanel.SetActive(false);
    }

    /// <summary>
    /// 스킵 진행도 업데이트
    /// </summary>
    public void UpdateSkipProgress(float progress)
    {
        if (skipProgressPanel != null)
        {
            skipProgressPanel.SetActive(progress > 0f);

            // 게이지 값 업데이트
            if (skipProgressBar != null)
            {
                skipProgressBar.value = progress;
            }
        }
    }

    /// <summary>
    /// 조건에 따른 힌트 텍스트 반환
    /// </summary>
    private string GetConditionHintText(NarrationConditionType conditionType)
    {
        return conditionType switch
        {
            NarrationConditionType.Move => "방향키로 이동해보세요",
            NarrationConditionType.OpenInventory => "I(i)키로 아이템창을 열어보세요",
            NarrationConditionType.OpenEquipment => "E(e)장비창을 열어보세요",
            NarrationConditionType.OpenQuest => "Q(q)퀘스트창을 열어보세요",
            NarrationConditionType.OpenStat => "S(s)스텟창을 열어보세요",
            _ => ""
        };
    }
}