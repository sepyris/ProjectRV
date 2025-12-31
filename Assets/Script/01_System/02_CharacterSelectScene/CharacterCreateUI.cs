using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// 캐릭터 생성 UI

public class CharacterCreateUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject createPanel;
    [SerializeField] private TMP_InputField characterNameInput;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Validation")]
    [SerializeField] private TextMeshProUGUI errorMessageText;
    [SerializeField] private int minNameLength = 2;
    [SerializeField] private int maxNameLength = 10;

    private int targetSlotIndex;
    private CharacterSelectManager selectManager;

    void Awake()
    {
        if (createPanel != null)
            createPanel.SetActive(false);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);

        if (characterNameInput != null)
            characterNameInput.onValueChanged.AddListener(OnNameInputChanged);
    }

    public void OpenCreatePanel(int slotIndex, CharacterSelectManager manager)
    {
        targetSlotIndex = slotIndex;
        selectManager = manager;

        if (createPanel != null)
        {
            createPanel.SetActive(true);
            Debug.Log("[CharacterCreate] 캐릭터 생성 패널 열림");
        }
        else
        {
            Debug.LogWarning("[CharacterCreate] 캐릭터 생성 패널이 할당되지 않음");
        }


        if (characterNameInput != null)
        {
            Debug.Log("[CharacterCreate] 이름 입력 필드 초기화");
            characterNameInput.text = "";
            characterNameInput.Select();
            characterNameInput.ActivateInputField();
        }

        ClearErrorMessage();
        UpdateConfirmButton();
    }

    public void CloseCreatePanel()
    {
        if (createPanel != null)
            createPanel.SetActive(false);
    }

    private void OnConfirmClicked()
    {
        string characterName = characterNameInput.text.Trim();
        
        if (!ValidateCharacterName(characterName, out string errorMessage))
        {
            ShowErrorMessage(errorMessage);
            return;
        }

        CharacterSlotData newCharacter = CharacterSaveManager.Instance.CreateCharacter(characterName, targetSlotIndex);
        ShowErrorMessage("캐릭터 생성 누름");
        if (newCharacter != null)
        {
            Debug.Log($"[CharacterCreate] 캐릭터 생성 성공: {characterName}");
            CloseCreatePanel();
            selectManager?.RefreshUI();
        }
        else
        {
            ShowErrorMessage("캐릭터 생성에 실패했습니다.");
        }
    }

    private void OnCancelClicked()
    {
        CloseCreatePanel();
    }

    private void OnNameInputChanged(string value)
    {
        ClearErrorMessage();
        UpdateConfirmButton();
    }

    private bool ValidateCharacterName(string name, out string errorMessage)
    {
        errorMessage = "";

        if (string.IsNullOrWhiteSpace(name))
        {
            errorMessage = "이름을 입력해주세요.";
            return false;
        }

        if (name.Length < minNameLength)
        {
            errorMessage = $"이름은 최소 {minNameLength}자 이상이어야 합니다.";
            return false;
        }

        if (name.Length > maxNameLength)
        {
            errorMessage = $"이름은 최대 {maxNameLength}자까지 가능합니다.";
            return false;
        }

        AllCharactersSaveData saveData = CharacterSaveManager.Instance.GetSaveData();
        foreach (var character in saveData.characterSlots)
        {
            if (character.stats.characterName == name)
            {
                errorMessage = "이미 사용 중인 이름입니다.";
                return false;
            }
        }

        return true;
    }

    private void UpdateConfirmButton()
    {
        if (confirmButton != null && characterNameInput != null)
        {
            string name = characterNameInput.text.Trim();
            confirmButton.interactable = name.Length >= minNameLength && name.Length <= maxNameLength;
        }
    }

    private void ShowErrorMessage(string message)
    {
        if (errorMessageText != null)
        {
            errorMessageText.text = message;
            errorMessageText.gameObject.SetActive(true);
        }
    }

    private void ClearErrorMessage()
    {
        if (errorMessageText != null)
        {
            errorMessageText.text = "";
            errorMessageText.gameObject.SetActive(false);
        }
    }
}