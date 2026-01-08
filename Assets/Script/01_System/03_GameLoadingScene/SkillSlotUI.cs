using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스킬 슬롯 쿨타임 관리
/// </summary>
public class SkillSlotUI : MonoBehaviour
{
    private Image skillIcon;
    private Image cooldownOverlay;
    private TextMeshProUGUI cooldownText;

    private PlayerSkillData playerSkill;
    private SkillBase skillInstance;
    private bool isOnCooldown = false;

    public void Initialize(Image icon, Image overlay, TextMeshProUGUI text, PlayerSkillData skill)
    {
        skillIcon = icon;
        cooldownOverlay = overlay;
        cooldownText = text;
        playerSkill = skill;

        // 스킬 인스턴스 가져오기
        if (skill != null)
        {
            skillInstance = SkillManager.Instance?.GetSkillInstance(skill.skillid);
        }

        // 쿨타임 UI 초기화
        ResetCooldownUI();
    }

    public void UpdateCooldown()
    {
        if (skillInstance == null)
            return;

        if (skillInstance.IsOnCooldown)
        {
            // 쿨타임 시작
            if (!isOnCooldown)
            {
                isOnCooldown = true;
                if (cooldownOverlay != null)
                    cooldownOverlay.enabled = true;
                if (cooldownText != null)
                    cooldownText.enabled = true;
            }

            // 쿨타임 진행도
            float progress = skillInstance.CooldownProgress;  // 0 → 1
            float fillAmount = 1f - progress;  // 1 → 0

            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = fillAmount;
            }

            // 남은 시간
            if (cooldownText != null)
            {
                float remaining = skillInstance.CooldownRemaining;

                if (remaining >= 1f)
                {
                    cooldownText.text = Mathf.Ceil(remaining).ToString();
                }
                else
                {
                    cooldownText.text = remaining.ToString("F1");
                }
            }
        }
        else
        {
            // 쿨타임 종료
            if (isOnCooldown)
            {
                isOnCooldown = false;
                ResetCooldownUI();
            }
        }
    }

    private void ResetCooldownUI()
    {
        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = 0f;
            cooldownOverlay.enabled = false;
        }

        if (cooldownText != null)
        {
            cooldownText.enabled = false;
        }
    }

    public string GetSkillId()
    {
        return playerSkill?.skillid;
    }
}