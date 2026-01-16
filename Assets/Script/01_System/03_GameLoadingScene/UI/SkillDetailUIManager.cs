using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillDetailUIManager : MonoBehaviour
{
    public static SkillDetailUIManager Instance { get; private set; }

    [Header("디테일 패널")]
    public GameObject skillDetailPanel;
    public TextMeshProUGUI skillNameText;
    public TextMeshProUGUI skillDescriptionText;

    private bool isTooltipActive = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (skillDetailPanel != null)
        {
            skillDetailPanel.SetActive(false);

            // 패널이 raycast를 받지 않도록 설정
            Graphic[] graphics = skillDetailPanel.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
            {
                graphic.raycastTarget = false;
            }
        }
    }

    void Update()
    {
        if (isTooltipActive && skillDetailPanel != null && skillDetailPanel.activeSelf)
        {
            UpdateTooltipPosition();
        }
    }

    public void ShowSkillDetail(string skillId, Transform anchorTransform = null)
    {
        if (skillDetailPanel == null) return;

        // 스킬 인스턴스에서 DetailDescription 가져오기
        SkillBase skillInstance = SkillManager.Instance?.GetSkillInstance(skillId);
        if (skillInstance == null)
        {
            Debug.LogWarning($"[SkillDetailUI] 스킬 인스턴스를 찾을 수 없음: {skillId}");
            return;
        }

        SkillData skillData = skillInstance.SkillData;
        if (skillData == null)
        {
            Debug.LogWarning($"[SkillDetailUI] 스킬 데이터를 찾을 수 없음: {skillId}");
            return;
        }

        PlayerSkillData playerSkill = SkillManager.Instance?.GetSkill(skillId);

        skillDetailPanel.SetActive(true);
        skillDetailPanel.transform.SetAsLastSibling();
        isTooltipActive = true;

        if (skillNameText != null)
        {
            skillNameText.text = skillData.skillName;
        }

        if (skillDescriptionText != null)
        {
            skillDescriptionText.text = skillInstance.DetailDescription;
        }

        if (anchorTransform != null)
        {
            PositionTooltip(anchorTransform);
        }
        else
        {
            UpdateTooltipPosition();
        }
    }

    public void ShowBuffDetail(Buff buff, Transform anchorTransform = null)
    {
        if (skillDetailPanel == null || buff == null) return;

        SkillBase skillInstance = SkillManager.Instance?.GetSkillInstance(buff.buffId);
        if (skillInstance == null)
        {
            Debug.LogWarning($"[SkillDetailUI] 버프의 스킬 인스턴스를 찾을 수 없음: {buff.buffId}");
            return;
        }

        SkillData skillData = skillInstance.SkillData;
        if (skillData == null)
        {
            Debug.LogWarning($"[SkillDetailUI] 스킬 데이터를 찾을 수 없음: {buff.buffId}");
            return;
        }

        skillDetailPanel.SetActive(true);
        skillDetailPanel.transform.SetAsLastSibling();
        isTooltipActive = true;

        if (skillNameText != null)
        {
            skillNameText.text = buff.buffName;
        }

        if (skillDescriptionText != null)
        {
            // 스킬 인스턴스의 DetailDescription 사용
            skillDescriptionText.text = skillInstance.DetailDescription;
        }

        if (anchorTransform != null)
        {
            PositionTooltip(anchorTransform);
        }
        else
        {
            UpdateTooltipPosition();
        }
    }

    private void PositionTooltip(Transform anchorTransform)
    {
        if (skillDetailPanel == null || anchorTransform == null) return;

        RectTransform detailRect = skillDetailPanel.GetComponent<RectTransform>();
        RectTransform anchorRect = anchorTransform.GetComponent<RectTransform>();

        if (detailRect != null && anchorRect != null)
        {
            Vector3 newPosition = anchorRect.position;

            float anchorRightEdgeX = anchorRect.position.x + anchorRect.rect.width * (1 - anchorRect.pivot.x);
            float detailPanelPivotCompensation = detailRect.rect.width * detailRect.pivot.x;
            newPosition.x = anchorRightEdgeX + 10f + detailPanelPivotCompensation;

            newPosition.y = anchorRect.position.y;

            newPosition = ClampToScreen(newPosition, detailRect);

            detailRect.position = newPosition;
        }
    }

    private void UpdateTooltipPosition()
    {
        if (skillDetailPanel == null) return;

        Vector2 mousePosition = Input.mousePosition;
        RectTransform tooltipRect = skillDetailPanel.GetComponent<RectTransform>();

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

    public void HideSkillDetail()
    {
        isTooltipActive = false;
        if (skillDetailPanel != null)
        {
            skillDetailPanel.SetActive(false);
        }
    }

    private string GetSkillTypeName(SkillType type)
    {
        switch (type)
        {
            case SkillType.Active: return "액티브";
            case SkillType.Passive: return "패시브";
            default: return "알 수 없음";
        }
    }

    private string GetJobName(JobsType jobType)
    {
        if (JobsDataManager.Instance != null)
        {
            JobsData jobData = JobsDataManager.Instance.GetJobDataByType(jobType);
            if (jobData != null)
            {
                return jobData.jobName;
            }
        }
        return jobType.ToString();
    }
}