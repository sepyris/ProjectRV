using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class SkillUIManager : MonoBehaviour,IClosableUI
{
    // Singleton instance
    public static SkillUIManager Instance { get; private set; }

    [Header("메인 패널")]
    public GameObject skillUIPanel;
    public Button SkillUiCloseButton;

    [Header("탭버튼")]
    public Button ActiveSkillTabButton;

    [Header("스킬 리스트")]
    public Transform SkillListContainer;
    public GameObject skillUIPrepabs;

    private enum SkillTab
    {
        FirstSkill,
        SecondSkill,
    }
    private SkillTab currentTab = SkillTab.FirstSkill;
    private bool isOpen = false;

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
        }
        skillUIPanel.SetActive(false);
        SetupButtons();
        if(SkillManager.Instance!= null)
        {
            SkillManager.Instance.OnSkillChanged += OnSkillChanged;
        }
        
    }
    void OnDestroy()
    {
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.OnSkillChanged -= OnSkillChanged;
        }
    }

    private void SetupButtons()
    {
        if (SkillUiCloseButton != null)
            SkillUiCloseButton.onClick.AddListener(CloseSkillUI);
    }

    public void OpenSkillUI()
    {
        if (isOpen) return;

        // 대화 중이면 열지 않음
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
            return;

        isOpen = true;
        skillUIPanel.SetActive(true);
        RefreshSkillList();
        PlayerHUD.Instance?.RegisterUI(this);
    }

    public void CloseSkillUI()
    {
        if (!isOpen) return;

        isOpen = false;
        skillUIPanel.SetActive(false);
        PlayerHUD.Instance?.UnregisterUI(this);
    }
    public bool IsSkillUIOpen()
    {
        return isOpen;
    }
    public void Close()
    {
        CloseSkillUI();
    }

    public GameObject GetUIPanel()
    {
        return skillUIPanel;
    }
    private void RefreshSkillList()
    {
        if(DraggableSkillUi.IsDragging())
        {
            DraggableSkillUi.CancelCurrentDrag();
        }


        // 기존 리스트 아이템 삭제
        foreach (Transform child in SkillListContainer)
            Destroy(child.gameObject);

        // 현재 탭에 맞는 아이템 가져오기
        List<PlayerSkillData> items = GetItemsForCurrentTab();

        // 아이템 리스트 아이템 생성
        foreach (var item in items)
        {
            CreateSkillListItem(item);
        }

        Debug.Log($"[ItemUI] {currentTab} 탭: {items.Count}개 아이템 표시");
    }
    private List<PlayerSkillData> GetItemsForCurrentTab()
    {
        if (SkillManager.Instance == null)
            return new List<PlayerSkillData>();

        switch (currentTab)
        {
            case SkillTab.FirstSkill:
                return SkillManager.Instance.GetSkillsByType();

            case SkillTab.SecondSkill:
                return SkillManager.Instance.GetSkillsByType();

            default:
                return new List<PlayerSkillData>();
        }
    }

    private void CreateSkillListItem(PlayerSkillData skill)
    {
        GameObject itemObj = Instantiate(skillUIPrepabs, SkillListContainer);
        Image itemImage = itemObj.GetComponent<Image>();
        Button itemButton = itemObj.GetComponent<Button>();
        TextMeshProUGUI itemText = itemObj.GetComponentInChildren<TextMeshProUGUI>();

        // 1. 호버 및 더블클릭 핸들러 컴포넌트를 가져오거나 추가
        /*
        ItemDetailUiManager hoverHandler = itemObj.GetComponent<ItemDetailUiManager>();
        if (hoverHandler == null)
        {
            hoverHandler = itemObj.AddComponent<ItemDetailUiManager>();
        }
        hoverHandler.Initialize(skill, this);
        */

        SkillData data = skill.GetSkillData();
        if (data == null) return;

        DraggableSkillUi draggable = itemObj.GetComponent<DraggableSkillUi>();
        if (draggable == null)
        {
            draggable = itemObj.AddComponent<DraggableSkillUi>();
        }
        draggable.Initialize(skill);

        // 4. 아이템 이름 표시
        if (itemText != null)
        {
            string displayText = "";
            itemText.text = displayText;
        }
        //5. 아이템 아이콘 표시
        Sprite itemIcon = itemImage.sprite;
        if (itemIcon != null && !string.IsNullOrEmpty(data.skillIconPath))
        {
            Sprite icon = Resources.Load<Sprite>(data.skillIconPath);
            if (icon != null)
            {
                itemImage.sprite = icon;
            }
            else
            {
                Debug.LogWarning($"[ShopItemUI] 아이콘을 찾을 수 없음: {data.skillIconPath}");
            }
        }
    }

    private void OnSkillChanged()
    {
        if (isOpen)
        {
            RefreshSkillList();

            //  상세 패널은 RefreshItemList()에서 이미 숨겨지므로 
            // 여기서는 다시 표시하지 않음 (호버 시에만 표시)
        }
    }

    public void RefreshUI()
    {
        if (isOpen)
            RefreshSkillList();
    }
}
