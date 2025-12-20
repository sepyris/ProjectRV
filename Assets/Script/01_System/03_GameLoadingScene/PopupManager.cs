using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// 공유 팝업(수량 입력, 확인, 경고)을 중앙에서 관리하는 매니저
/// 팝업이 열릴 때 다른 모든 UI의 상호작용을 차단합니다
/// </summary>
public class PopupManager : MonoBehaviour, IClosableUI
{
    public static PopupManager Instance { get; private set; }

    [Header("팝업 패널")]
    public GameObject quantityPopupPanel;
    public GameObject confirmPopupPanel;
    public GameObject warningPopupPanel;

    [Header("수량 입력 팝업 UI")]
    public TextMeshProUGUI quantityPopupTitleText;
    public Image quantityPopupIcon;
    public TextMeshProUGUI quantityPopupPriceText;
    public Image quantityPriceIcon;
    public TMP_InputField quantityPopupInput;
    public Button quantityPopupConfirmButton;
    public Button quantityPopupCloseButton;

    [Header("수량 조절 버튼")]
    public Button quantityMinusAllButton;
    public Button quantityMinus10Button;
    public Button quantityMinus1Button;
    public Button quantityPlus1Button;
    public Button quantityPlus10Button;
    public Button quantityPlusAllButton;

    [Header("확인 팝업 UI")]
    public TextMeshProUGUI confirmPopupMessageText;
    public Button confirmPopupConfirmButton;
    public Button confirmPopupCancelButton;

    [Header("경고 팝업 UI")]
    public TextMeshProUGUI warningPopupMessageText;
    public Button warningPopupConfirmButton;

    [Header("차단할 UI 패널들")]
    [Tooltip("아이템 관련 팝업")]
    public List<GameObject> uiPanelsToBlock = new List<GameObject>();

    // 현재 팝업 상태
    private bool isQuantityPopupOpen = false;
    private bool isConfirmPopupOpen = false;
    private bool isWarningPopupOpen = false;

    // 수량 입력 팝업 콜백
    private Action<int> quantityConfirmCallback;
    private int quantityMaxValue;

    // 확인 팝업 콜백
    private Action confirmCallback;
    private Action cancelCallback;

    // 경고 팝업 콜백 (확인만 있음)
    private Action warningConfirmCallback;

    // UI 패널의 CanvasGroup 캐시
    private Dictionary<GameObject, CanvasGroup> panelCanvasGroups = new Dictionary<GameObject, CanvasGroup>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 팝업 버튼 리스너 설정
        if (quantityPopupConfirmButton != null)
            quantityPopupConfirmButton.onClick.AddListener(OnQuantityPopupConfirm);
        if (quantityPopupCloseButton != null)
            quantityPopupCloseButton.onClick.AddListener(OnQuantityPopupCancel);

        if (quantityMinusAllButton != null)
            quantityMinusAllButton.onClick.AddListener(() => AdjustQuantity(-9999));
        if (quantityMinus10Button != null)
            quantityMinus10Button.onClick.AddListener(() => AdjustQuantity(-10));
        if (quantityMinus1Button != null)
            quantityMinus1Button.onClick.AddListener(() => AdjustQuantity(-1));
        if (quantityPlus1Button != null)
            quantityPlus1Button.onClick.AddListener(() => AdjustQuantity(1));
        if (quantityPlus10Button != null)
            quantityPlus10Button.onClick.AddListener(() => AdjustQuantity(10));
        if (quantityPlusAllButton != null)
            quantityPlusAllButton.onClick.AddListener(() => AdjustQuantity(9999));

        if (confirmPopupConfirmButton != null)
            confirmPopupConfirmButton.onClick.AddListener(OnConfirmPopupConfirm);
        if (confirmPopupCancelButton != null)
            confirmPopupCancelButton.onClick.AddListener(OnConfirmPopupCancel);

        if (warningPopupConfirmButton != null)
            warningPopupConfirmButton.onClick.AddListener(OnWarningPopupConfirm);

        // 팝업에 CanvasGroup 설정
        SetupPopupCanvasGroup(quantityPopupPanel);
        SetupPopupCanvasGroup(confirmPopupPanel);
        SetupPopupCanvasGroup(warningPopupPanel);

        // UI 패널들의 CanvasGroup 캐시
        CacheUICanvasGroups();

        // 팝업 초기 상태 비활성화
        if (quantityPopupPanel != null)
            quantityPopupPanel.SetActive(false);
        if (confirmPopupPanel != null)
            confirmPopupPanel.SetActive(false);
        if (warningPopupPanel != null)
            warningPopupPanel.SetActive(false);
    }

    /// <summary>
    /// 팝업에 CanvasGroup 추가 및 설정
    /// </summary>
    private void SetupPopupCanvasGroup(GameObject popup)
    {
        if (popup == null) return;

        CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = popup.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }

    /// <summary>
    /// UI 패널들의 CanvasGroup 미리 캐시
    /// </summary>
    private void CacheUICanvasGroups()
    {
        foreach (GameObject panel in uiPanelsToBlock)
        {
            if (panel == null) continue;

            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }

            panelCanvasGroups[panel] = canvasGroup;
        }
    }

    /// <summary>
    /// 모든 UI 패널 상호작용 차단
    /// </summary>
    private void BlockAllUIPanels()
    {
        foreach (var kvp in panelCanvasGroups)
        {
            if (kvp.Key != null && kvp.Key.activeSelf && kvp.Value != null)
            {
                kvp.Value.interactable = false;
                kvp.Value.blocksRaycasts = false;
            }
        }
        Debug.Log("[PopupManager] 모든 UI 패널 차단됨");
    }

    /// <summary>
    /// 모든 UI 패널 상호작용 복원
    /// </summary>
    private void UnblockAllUIPanels()
    {
        foreach (var kvp in panelCanvasGroups)
        {
            if (kvp.Key != null && kvp.Key.activeSelf && kvp.Value != null)
            {
                kvp.Value.interactable = true;
                kvp.Value.blocksRaycasts = true;
            }
        }
        Debug.Log("[PopupManager] 모든 UI 패널 차단 해제됨");
    }

    // ==================== 수량 입력 팝업 ====================

    /// <summary>
    /// 수량 입력 팝업 표시 (상점 거래용)
    /// </summary>
    public void ShowQuantityPopup(ItemData itemData, int maxQuantity, int price, bool isBuying, Action<int> onConfirm)
    {
        if (quantityPopupPanel == null) return;

        quantityMaxValue = maxQuantity;
        quantityConfirmCallback = onConfirm;

        // 다른 모든 UI 차단
        BlockAllUIPanels();

        isQuantityPopupOpen = true;
        quantityPopupPanel.SetActive(true);

        //  HUD에 UI 등록
        PlayerHUD.Instance?.RegisterUI(this);

        // 팝업 내용 설정
        if (quantityPopupTitleText != null)
            quantityPopupTitleText.text = isBuying ? $"{itemData.itemName} 구매" : $"{itemData.itemName} 판매";

        if (quantityPopupIcon != null && !string.IsNullOrEmpty(itemData.iconPath))
        {
            Sprite icon = Resources.Load<Sprite>(itemData.iconPath);
            if (icon != null)
                quantityPopupIcon.sprite = icon;
        }

        if (quantityPriceIcon != null)
            quantityPriceIcon.enabled = true;

        if (quantityPopupInput != null)
        {
            quantityPopupInput.text = "1";
            quantityPopupInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        }

        UpdatePriceDisplay(1, price);
    }

    /// <summary>
    /// 수량 입력 팝업 표시 (아이템 버리기용)
    /// </summary>
    public void ShowDiscardQuantityPopup(ItemData itemData, int maxQuantity, Action<int> onConfirm)
    {
        if (quantityPopupPanel == null) return;

        quantityMaxValue = maxQuantity;
        quantityConfirmCallback = onConfirm;

        // 다른 모든 UI 차단
        BlockAllUIPanels();

        isQuantityPopupOpen = true;
        quantityPopupPanel.SetActive(true);

        //  HUD에 UI 등록
        PlayerHUD.Instance?.RegisterUI(this);

        // 팝업 내용 설정
        if (quantityPopupTitleText != null)
            quantityPopupTitleText.text = $"{itemData.itemName}\n버리기";

        if (quantityPopupIcon != null && !string.IsNullOrEmpty(itemData.iconPath))
        {
            Sprite icon = Resources.Load<Sprite>(itemData.iconPath);
            if (icon != null)
                quantityPopupIcon.sprite = icon;
        }

        if (quantityPopupPriceText != null)
            quantityPopupPriceText.text = "";

        if (quantityPriceIcon != null)
            quantityPriceIcon.enabled = false;

        if (quantityPopupInput != null)
        {
            quantityPopupInput.text = "1";
            quantityPopupInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        }
    }

    private void UpdatePriceDisplay(int quantity, int unitPrice)
    {
        if (quantityPopupPriceText != null)
        {
            int totalPrice = quantity * unitPrice;
            quantityPopupPriceText.text = $"{totalPrice}";
        }
    }

    private void AdjustQuantity(int delta)
    {
        if (quantityPopupInput == null) return;

        int currentValue = 1;
        if (int.TryParse(quantityPopupInput.text, out int parsed))
        {
            currentValue = parsed;
        }

        int newValue = currentValue + delta;
        newValue = Mathf.Clamp(newValue, 1, quantityMaxValue);

        quantityPopupInput.text = newValue.ToString();
    }

    private void OnQuantityPopupConfirm()
    {
        if (quantityPopupInput == null) return;

        int quantity = 1;
        if (int.TryParse(quantityPopupInput.text, out int parsed))
        {
            quantity = Mathf.Clamp(parsed, 1, quantityMaxValue);
        }

        CloseQuantityPopup();

        quantityConfirmCallback?.Invoke(quantity);
        quantityConfirmCallback = null;
    }

    private void OnQuantityPopupCancel()
    {
        CloseQuantityPopup();
        quantityConfirmCallback = null;
    }

    private void CloseQuantityPopup()
    {
        //  순서 변경: Panel 비활성화
        if (quantityPopupPanel != null)
            quantityPopupPanel.SetActive(false);

        //  UnregisterUI를 flag 변경 전에 호출
        PlayerHUD.Instance?.UnregisterUI(this);

        //  flag를 나중에 false로
        isQuantityPopupOpen = false;

        // 다른 팝업이 열려있지 않으면 UI 차단 해제
        if (!isConfirmPopupOpen && !isWarningPopupOpen)
        {
            UnblockAllUIPanels();
        }
    }

    // ==================== 확인 팝업 ====================

    /// <summary>
    /// 확인 팝업 표시 (확인/취소 버튼)
    /// </summary>
    public void ShowConfirmPopup(string message, Action onConfirm, Action onCancel = null)
    {
        if (confirmPopupPanel == null) return;

        confirmCallback = onConfirm;
        cancelCallback = onCancel;

        // 다른 모든 UI 차단 (이미 차단되어 있을 수 있음)
        if (!isQuantityPopupOpen && !isWarningPopupOpen)
        {
            BlockAllUIPanels();
        }

        isConfirmPopupOpen = true;
        confirmPopupPanel.SetActive(true);

        //  HUD에 UI 등록
        PlayerHUD.Instance?.RegisterUI(this);

        if (confirmPopupMessageText != null)
        {
            confirmPopupMessageText.text = message;
        }
    }

    private void OnConfirmPopupConfirm()
    {
        CloseConfirmPopup();
        confirmCallback?.Invoke();
        confirmCallback = null;
        cancelCallback = null;
    }

    private void OnConfirmPopupCancel()
    {
        CloseConfirmPopup();
        cancelCallback?.Invoke();
        confirmCallback = null;
        cancelCallback = null;
    }

    private void CloseConfirmPopup()
    {
        //  Panel 비활성화
        if (confirmPopupPanel != null)
            confirmPopupPanel.SetActive(false);

        //  UnregisterUI를 flag 변경 전에 호출
        PlayerHUD.Instance?.UnregisterUI(this);

        //  flag를 나중에 false로
        isConfirmPopupOpen = false;

        // 다른 팝업이 열려있지 않으면 UI 차단 해제
        if (!isQuantityPopupOpen && !isWarningPopupOpen)
        {
            UnblockAllUIPanels();
        }
    }

    // ==================== 경고 팝업 (확인 버튼만) ====================

    /// <summary>
    /// 경고 팝업 표시 (확인 버튼만)
    /// </summary>
    public void ShowWarningPopup(string message, Action onConfirm = null)
    {
        if (warningPopupPanel == null) return;

        warningConfirmCallback = onConfirm;

        // 다른 모든 UI 차단 (이미 차단되어 있을 수 있음)
        if (!isQuantityPopupOpen && !isConfirmPopupOpen)
        {
            BlockAllUIPanels();
        }

        isWarningPopupOpen = true;
        warningPopupPanel.SetActive(true);

        //  HUD에 UI 등록
        PlayerHUD.Instance?.RegisterUI(this);

        if (warningPopupMessageText != null)
        {
            warningPopupMessageText.text = message;
        }

        Debug.Log($"[PopupManager] 경고 팝업 표시: {message}");
    }

    private void OnWarningPopupConfirm()
    {
        CloseWarningPopup();
        warningConfirmCallback?.Invoke();
        warningConfirmCallback = null;
    }

    private void CloseWarningPopup()
    {
        //  Panel 비활성화
        if (warningPopupPanel != null)
            warningPopupPanel.SetActive(false);

        //  UnregisterUI를 flag 변경 전에 호출
        PlayerHUD.Instance?.UnregisterUI(this);

        //  flag를 나중에 false로
        isWarningPopupOpen = false;

        // 다른 팝업이 열려있지 않으면 UI 차단 해제
        if (!isQuantityPopupOpen && !isConfirmPopupOpen)
        {
            UnblockAllUIPanels();
        }

        Debug.Log("[PopupManager] 경고 팝업 닫힘");
    }

    // ==================== 공개 메서드 ====================

    /// <summary>
    /// 팝업이 현재 열려있는지 확인
    /// </summary>
    public bool IsAnyPopupOpen()
    {
        return isQuantityPopupOpen || isConfirmPopupOpen || isWarningPopupOpen;
    }

    /// <summary>
    /// 모든 팝업 강제 닫기
    /// </summary>
    public void CloseAllPopups()
    {
        if (isQuantityPopupOpen)
            OnQuantityPopupCancel();

        if (isConfirmPopupOpen)
            OnConfirmPopupCancel();

        if (isWarningPopupOpen)
            OnWarningPopupConfirm();
    }

    /// <summary>
    /// IClosableUI 구현 - ESC로 맨 위 팝업 닫기
    /// </summary>
    public void Close()
    {
        // 가장 최근에 열린 팝업부터 닫기
        if (isWarningPopupOpen)
            CloseWarningPopup();
        else if (isConfirmPopupOpen)
            CloseConfirmPopup();
        else if (isQuantityPopupOpen)
            CloseQuantityPopup();
    }

    /// <summary>
    /// IClosableUI 구현 - 현재 열린 팝업 Panel 반환
    /// </summary>
    public GameObject GetUIPanel()
    {
        return this.gameObject;
    }
}