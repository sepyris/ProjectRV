using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;


/// 상점 아이템 UI 항목 (마우스 호버 지원 + 구매/판매 버튼)

public class ShopItemUI : MonoBehaviour
{
    [Header("UI 요소")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI stockText; // 보유 수량 (판매 시, 장비 제외)
    public Image itemIcon;

    [Header("구매/판매 버튼")]
    public Button transactionButton;
    public TextMeshProUGUI transactionButtonText;

    private ItemData itemData;
    private ShopItemData shopItemData;
    private Action transactionCallback;

    void Start()
    {
        // 아이콘에 EventTrigger 자동 추가
        SetupIconTooltip();
    }

    
    /// 아이템 정보를 UI에 설정합니다.
    
    public void SetItemInfo(ItemData itemData, ShopItemData shopItemData, int price, bool is_buy = true ,string stockInfo = "")
    {
        this.itemData = itemData;
        this.shopItemData = shopItemData;

        if (itemNameText != null)
            itemNameText.text = itemData.itemName;

        if (priceText != null)
            priceText.text = $"{price}G";

        // 보유 수량 표시 (판매 시, 장비 제외)
        if (stockText != null)
        {
            if (!string.IsNullOrEmpty(stockInfo))
            {
                stockText.text = stockInfo;
                stockText.gameObject.SetActive(true);
            }
            else
            {
                stockText.text = "";
                stockText.gameObject.SetActive(false);
            }
        }

        // iconPath를 사용해 아이콘 로드
        if (itemIcon != null && !string.IsNullOrEmpty(itemData.iconPath))
        {
            Sprite icon = Resources.Load<Sprite>(itemData.iconPath);
            if (icon != null)
            {
                itemIcon.sprite = icon;
            }
            else
            {
                Debug.LogWarning($"[ShopItemUI] 아이콘을 찾을 수 없음: {itemData.iconPath}");
            }
        }
        //구매리스트,판매리스트에 따라 버튼 텍스트 변경
        if (transactionButtonText != null)
        {
            transactionButtonText.text = is_buy ? "구매" : "판매";
        }
    }

    
    /// 구매/판매 버튼 콜백 설정
    
    public void SetTransactionButtonCallback(Action callback)
    {
        transactionCallback = callback;

        if (transactionButton != null)
        {
            transactionButton.onClick.RemoveAllListeners();
            transactionButton.onClick.AddListener(() => transactionCallback?.Invoke());
        }
    }

    
    /// 아이콘에 툴팁 이벤트 설정
    
    void SetupIconTooltip()
    {
        if (itemIcon == null) return;

        EventTrigger trigger = itemIcon.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = itemIcon.gameObject.AddComponent<EventTrigger>();
        }

        // PointerEnter 이벤트
        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => { OnIconPointerEnter((PointerEventData)data); });
        trigger.triggers.Add(entryEnter);

        // PointerExit 이벤트
        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { OnIconPointerExit((PointerEventData)data); });
        trigger.triggers.Add(entryExit);
    }

    
    /// 아이콘에 마우스가 올라갔을 때
    
    void OnIconPointerEnter(PointerEventData eventData)
    {
        if (itemData != null)
        {
            ShopUIManager.Instance?.ShowItemTooltip(itemData, shopItemData);
        }
    }

    
    /// 아이콘에서 마우스가 벗어날 때
    
    void OnIconPointerExit(PointerEventData eventData)
    {
        ShopUIManager.Instance?.HideItemTooltip();
    }
}