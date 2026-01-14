using UnityEngine;
using UnityEngine.EventSystems;

public class JobChangeSkillIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private SkillData skillData;

    public void Initialize(SkillData data)
    {
        skillData = data;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (skillData == null) return;

        if (SkillDetailUIManager.Instance != null)
        {
            ShowSkillTooltip();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (SkillDetailUIManager.Instance != null)
        {
            SkillDetailUIManager.Instance.HideSkillDetail();
        }
    }

    private void ShowSkillTooltip()
    {
        if (SkillDetailUIManager.Instance == null || skillData == null)
            return;

        if (SkillDetailUIManager.Instance.skillDetailPanel != null)
        {
            SkillDetailUIManager.Instance.skillDetailPanel.SetActive(true);
            SkillDetailUIManager.Instance.skillDetailPanel.transform.SetAsLastSibling();
        }

        if (SkillDetailUIManager.Instance.skillNameText != null)
        {
            SkillDetailUIManager.Instance.skillNameText.text = skillData.skillName;
        }

        if (SkillDetailUIManager.Instance.skillDescriptionText != null)
        {
            SkillDetailUIManager.Instance.skillDescriptionText.text = skillData.description;
        }

        UpdateTooltipPosition();
    }

    private void UpdateTooltipPosition()
    {
        if (SkillDetailUIManager.Instance == null ||
            SkillDetailUIManager.Instance.skillDetailPanel == null)
            return;

        Vector2 mousePosition = Input.mousePosition;
        RectTransform tooltipRect = SkillDetailUIManager.Instance.skillDetailPanel.GetComponent<RectTransform>();

        if (tooltipRect != null)
        {
            Vector2 offset = new Vector2(15f, -15f);
            Vector2 newPosition = mousePosition + offset;

            float tooltipPivotCompensationX = tooltipRect.rect.width * tooltipRect.pivot.x;
            float tooltipPivotCompensationY = tooltipRect.rect.height * tooltipRect.pivot.y;

            newPosition.x += tooltipPivotCompensationX;
            newPosition.y -= tooltipPivotCompensationY;

            newPosition = ClampToScreen(newPosition, tooltipRect);

            tooltipRect.position = newPosition;
        }
    }

    private Vector3 ClampToScreen(Vector3 position, RectTransform panelRect)
    {
        if (panelRect == null) return position;

        float panelWidth = panelRect.rect.width * panelRect.lossyScale.x;
        float panelHeight = panelRect.rect.height * panelRect.lossyScale.y;

        float leftEdge = position.x - panelWidth * panelRect.pivot.x;
        float rightEdge = position.x + panelWidth * (1 - panelRect.pivot.x);
        float bottomEdge = position.y - panelHeight * panelRect.pivot.y;
        float topEdge = position.y + panelHeight * (1 - panelRect.pivot.y);

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float padding = 10f;

        if (rightEdge > screenWidth - padding)
        {
            position.x -= (rightEdge - (screenWidth - padding));
        }
        else if (leftEdge < padding)
        {
            position.x += (padding - leftEdge);
        }

        if (topEdge > screenHeight - padding)
        {
            position.y -= (topEdge - (screenHeight - padding));
        }
        else if (bottomEdge < padding)
        {
            position.y += (padding - bottomEdge);
        }

        return position;
    }
}