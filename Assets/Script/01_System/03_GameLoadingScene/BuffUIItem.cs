using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 개별 버프 UI 아이템
/// 버프 아이콘, 이름, 남은 시간 표시
/// </summary>
public class BuffUIItem : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;                    // 버프 아이콘
    public TextMeshProUGUI nameText;           // 버프 이름
    public TextMeshProUGUI timeText;           // 남은 시간
    public Image fillImage;                    // 남은 시간 fill (선택 사항)

    private Buff currentBuff;

    /// <summary>
    /// 버프 UI 초기화
    /// </summary>
    public void Initialize(Buff buff)
    {
        currentBuff = buff;

        if (nameText != null)
        {
            nameText.text = buff.buffName;
        }

        // 버프 아이콘은 나중에 SkillData에서 가져올 수 있음
        if (iconImage != null)
        {
            Sprite icon = Resources.Load<Sprite>(SkillManager.Instance.GetSkillInstance(buff.buffId).IconPath);
            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.color = Color.white;
            }
        }

        UpdateDisplay();
    }

    /// <summary>
    /// 매 프레임 UI 업데이트
    /// </summary>
    public void UpdateDisplay()
    {
        if (currentBuff == null) return;

        // 남은 시간 표시
        if (timeText != null)
        {
            float remaining = Mathf.Max(0, currentBuff.remainingTime);

            if (remaining >= 1f)
            {
                // 1초 이상이면 정수 표시
                timeText.text = Mathf.CeilToInt(remaining).ToString();
            }
            else
            {
                // 1초 미만이면 소수점 1자리
                timeText.text = remaining.ToString("F1");
            }
        }
    }

    /// <summary>
    /// 버프 갱신 (시간 리셋)
    /// </summary>
    public void Refresh(Buff buff)
    {
        currentBuff = buff;
        UpdateDisplay();
    }

    /// <summary>
    /// 버프가 만료되었는지 확인
    /// </summary>
    public bool IsExpired()
    {
        return currentBuff == null || currentBuff.IsExpired();
    }

    public string GetBuffId()
    {
        return currentBuff?.buffId;
    }
}