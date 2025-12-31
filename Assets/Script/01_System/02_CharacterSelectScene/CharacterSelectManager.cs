using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


/// 캐릭터 선택 화면 관리

public class CharacterSelectManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CharacterSlotUI[] characterSlots;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button backButton;

    [Header("Panels")]
    [SerializeField] private GameObject characterCreatePanel;
    [SerializeField] private GameObject deleteConfirmPanel;

    [Header("Delete Confirm UI")]
    [SerializeField] private TextMeshProUGUI deleteConfirmText;
    [SerializeField] private Button deleteConfirmYesButton;
    [SerializeField] private Button deleteConfirmNoButton;

    private CharacterSlotUI selectedSlot;
    private CharacterSlotUI pendingDeleteSlot;

    void Start()
    {

        InitializeSlots();
        RefreshUI();

        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGameClicked);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        if (deleteConfirmYesButton != null)
            deleteConfirmYesButton.onClick.AddListener(OnDeleteConfirmYes);

        if (deleteConfirmNoButton != null)
            deleteConfirmNoButton.onClick.AddListener(OnDeleteConfirmNo);

        if (deleteConfirmPanel != null)
            deleteConfirmPanel.SetActive(false);

        UpdateUI();
    }

    private void InitializeSlots()
    {
        for (int i = 0; i < characterSlots.Length; i++)
        {
            characterSlots[i].Initialize(i, this);
        }
    }

    public void RefreshUI()
    {
        AllCharactersSaveData saveData = CharacterSaveManager.Instance.GetSaveData();

        for (int i = 0; i < characterSlots.Length; i++)
        {
            CharacterSlotData data = saveData.GetCharacterInSlot(i);
            characterSlots[i].SetCharacterData(data);
        }


        UpdateUI();
    }
    private void UpdateUI()
    {
        AllCharactersSaveData saveData = CharacterSaveManager.Instance.GetSaveData();
        int characterCount = saveData.CharacterCount;
        for (int i = 0; i < characterSlots.Length; i++)
        {
            if(characterSlots[i].HasCharacter)
            {
                characterSlots[i].SlotButtonEnable(true);
            }else
            {
                characterSlots[i].SlotButtonEnable(false);
            }
        }
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(selectedSlot != null && selectedSlot.HasCharacter);
        }
    }

    public void OnSlotSelected(CharacterSlotUI slot)
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }

        selectedSlot = slot;
        selectedSlot.SetSelected(true);

        UpdateUI();
    }

    public void OnSlotDoubleClicked(CharacterSlotUI slot)
    {
        if (slot == null || !slot.HasCharacter)
            return;

        OnSlotSelected(slot);
        StartGame();
    }

    public void OnCreateButtonClicked(CharacterSlotUI slot)
    {
        Debug.Log("[CharacterSelect] 캐릭터 생성 버튼 클릭됨");
        if (slot == null || slot.HasCharacter)
        {
            Debug.LogWarning("[CharacterSelect] 유효하지 않은 슬롯이거나 이미 캐릭터가 존재합니다.");
            return;
        }
            

        if (characterCreatePanel != null)
        {
            Debug.Log("[CharacterSelect] 캐릭터 생성 UI 열기");
            CharacterCreateUI createUI = characterCreatePanel.GetComponent<CharacterCreateUI>();
            if (createUI != null)
            {
                characterCreatePanel.SetActive(true);
                Debug.Log("[CharacterSelect] 캐릭터 생성 UI 열기 시도");
                createUI.OpenCreatePanel(slot.GetSlotIndex(), this);
            }
        }
    }

    public void OnDeleteButtonClicked(CharacterSlotUI slot)
    {
        if (slot == null || !slot.HasCharacter)
            return;

        pendingDeleteSlot = slot;
        ShowDeleteConfirmDialog(slot.CharacterData.stats.characterName);
    }

    private void ShowDeleteConfirmDialog(string characterName)
    {
        if (deleteConfirmPanel == null)
        {
            Debug.LogWarning("[CharacterSelect] 삭제 확인 팝업이 없습니다. 바로 삭제합니다.");
            ConfirmDelete();
            return;
        }

        deleteConfirmPanel.SetActive(true);

        if (deleteConfirmText != null)
        {
            deleteConfirmText.text = $"'{characterName}' 캐릭터를\n정말 삭제하시겠습니까?";
        }
    }

    private void OnDeleteConfirmYes()
    {
        ConfirmDelete();

        if (deleteConfirmPanel != null)
            deleteConfirmPanel.SetActive(false);
    }

    private void OnDeleteConfirmNo()
    {
        pendingDeleteSlot = null;

        if (deleteConfirmPanel != null)
            deleteConfirmPanel.SetActive(false);
    }

    private void ConfirmDelete()
    {
        if (pendingDeleteSlot == null || !pendingDeleteSlot.HasCharacter)
            return;

        string characterid = pendingDeleteSlot.CharacterData.characterid;
        CharacterSaveManager.Instance.DeleteCharacter(characterid);
        selectedSlot?.SetSelected(false);

        selectedSlot = null;
        pendingDeleteSlot = null;
        RefreshUI();

        Debug.Log("[CharacterSelect] 캐릭터 삭제 완료");
    }

    private void OnStartGameClicked()
    {
        StartGame();
    }

    private void StartGame()
    {
        Debug.Log($"[StartGame] 호출됨!");
        Debug.Log($"[StartGame] selectedSlot: {selectedSlot}");
        Debug.Log($"[StartGame] HasCharacter: {selectedSlot?.HasCharacter}");
        if (selectedSlot == null || !selectedSlot.HasCharacter)
        {
            Debug.LogError("[StartGame] 조건 실패!");
            return;
        }

        CharacterSaveManager.Instance.StartCharacterGame(selectedSlot.CharacterData.characterid);

        SceneManager.LoadScene("03_GameStartLoadingScene");

        Debug.Log($"[CharacterSelect] 게임 시작: {selectedSlot.CharacterData.stats.characterName}");
    }

    private void OnBackClicked()
    {
        SceneManager.LoadScene(Definitions.Def_Name.SCENE_NAME_MAIN_SCREEN);
    }
}