using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 캐릭터 슬롯 UI
/// </summary>
public class CharacterSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Button slotButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button createButton;
    [SerializeField] private GameObject characterInfoPanel;
    [SerializeField] private GameObject emptySlotPanel;

    [Header("Character Info")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI levelJobText;
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("Selection")]
    [SerializeField] private GameObject selectionHighlight;

    private CharacterSlotData characterData;
    public int slotIndex;
    private CharacterSelectManager selectManager;
    private float lastClickTime;
    private const float DOUBLE_CLICK_TIME = 0.3f;

    public bool HasCharacter => characterData != null;
    public CharacterSlotData CharacterData => characterData;

    public void Initialize(int index, CharacterSelectManager manager)
    {
        slotIndex = index;
        selectManager = manager;

        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClicked);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(OnDeleteClicked);
        }

        if (createButton != null)
        {
            createButton.onClick.AddListener(OnCreateClicked);
        }

        SetSelected(false);
    }

    public void SetCharacterData(CharacterSlotData data)
    {
        characterData = data;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (characterData != null)
        {
            if (characterInfoPanel != null)
                characterInfoPanel.SetActive(true);

            if (emptySlotPanel != null)
                emptySlotPanel.SetActive(false);

            if (levelJobText != null)
                levelJobText.text = $"Lv.{characterData.stats.level} 초보자";

            if (nameText != null)
                nameText.text = characterData.stats.characterName;

            if (deleteButton != null)
                deleteButton.gameObject.SetActive(true);

            if (slotButton != null)
                slotButton.interactable = true;

        }
        else
        {
            if (characterInfoPanel != null)
                characterInfoPanel.SetActive(false);

            if (emptySlotPanel != null)
                emptySlotPanel.SetActive(true);

            if (deleteButton != null)
                deleteButton.gameObject.SetActive(false);
        }

    }

    public void SetSlotEnabled(bool enabled)
    {
        if (slotButton != null)
            slotButton.interactable = enabled;

        if (createButton != null)
            createButton.interactable = enabled;

        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = enabled ? 1f : 0f;
    }

    private void OnSlotClicked()
    {
        if (!HasCharacter)
            return;

        selectManager?.OnSlotSelected(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!HasCharacter)
            return;

        float timeSinceLastClick = Time.time - lastClickTime;

        if (timeSinceLastClick <= DOUBLE_CLICK_TIME)
        {
            selectManager?.OnSlotDoubleClicked(this);
        }

        lastClickTime = Time.time;
    }

    private void OnDeleteClicked()
    {
        if (!HasCharacter)
            return;

        selectManager?.OnDeleteButtonClicked(this);
    }

    private void OnCreateClicked()
    {
        if (HasCharacter)
            return;

        selectManager?.OnCreateButtonClicked(this);
    }

    public void SetSelected(bool selected)
    {
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(selected);
        }
    }

    public int GetSlotIndex()
    {
        return slotIndex;
    }
    public bool IsEmpty()
    {
        return characterData == null;
    }
    public void SlotButtonEnable(bool is_enable)
    {
        slotButton.interactable = is_enable;
    }
}