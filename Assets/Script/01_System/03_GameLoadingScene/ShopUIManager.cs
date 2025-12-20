using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUIManager : MonoBehaviour, IClosableUI
{
    public static ShopUIManager Instance { get; private set; }

    [Header("메인 패널")]
    public GameObject shopUIPanel;
    public Button closeButton;

    [Header("플레이어 골드")]
    public TextMeshProUGUI playerGoldText;

    [Header("탭 버튼")]
    public Button buyTabButton;
    public Button rebuyTabButton;
    public Button equipmentTabButton;
    public Button usingitemTabButton;
    public Button etcitemTabButton;

    [Header("아이템 리스트")]
    public Transform buyitemListContainer;
    public Transform sellitemListContainer;
    public GameObject itemListPrefab;

    [Header("아이템 툴팁 (마우스 호버)")]
    public GameObject itemTooltipPanel;
    public TextMeshProUGUI tooltipNameText;
    public TextMeshProUGUI tooltipDescriptionText;
    public TextMeshProUGUI tooltipStatsText;

    // 상점 관련 변수
    private string currentShopid;
    private ShopData currentShopData;
    private ItemData pendingItemData;
    private ShopItemData pendingShopItemData;
    private int pendingQuantity;

    //  실제 거래 모드 저장
    private bool isPendingTransactionBuy = false;
    private bool isPendingTransactionRebuy = false;

    private bool isTooltipActive = false;

    private enum ShopMode { Buy, Rebuy, SellEquipment, SellConsumable, SellMaterial }
    private ShopMode currentBuyMode = ShopMode.Buy;
    private ShopMode currentSellMode = ShopMode.SellEquipment;

    private bool isShopActive = false;
    public bool IsShopOpen => isShopActive;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        closeButton.onClick.AddListener(CloseShop);

        buyTabButton.onClick.AddListener(() => SwitchBuyMode(ShopMode.Buy));
        rebuyTabButton.onClick.AddListener(() => SwitchBuyMode(ShopMode.Rebuy));
        equipmentTabButton.onClick.AddListener(() => SwitchSellMode(ShopMode.SellEquipment));
        usingitemTabButton.onClick.AddListener(() => SwitchSellMode(ShopMode.SellConsumable));
        etcitemTabButton.onClick.AddListener(() => SwitchSellMode(ShopMode.SellMaterial));

        shopUIPanel.SetActive(false);
        if (itemTooltipPanel != null)
        {
            itemTooltipPanel.SetActive(false);
            Graphic[] graphics = itemTooltipPanel.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
            {
                graphic.raycastTarget = false;
            }
        }

        LoadShopData();
    }

    void Update()
    {
        if (isTooltipActive && itemTooltipPanel != null && itemTooltipPanel.activeSelf)
        {
            UpdateTooltipPosition();
        }
        UpdatePlayerGoldDisplay();
    }

    private ShopStockSaveData GetShopStockData()
    {
        if (CharacterSaveManager.Instance == null || CharacterSaveManager.Instance.CurrentCharacter == null)
        {
            Debug.LogWarning("[ShopUI] 캐릭터 데이터를 찾을 수 없습니다!");
            return null;
        }

        return CharacterSaveManager.Instance.CurrentCharacter.shopStockData;
    }

    public void OpenShop(string shopid)
    {
        PlayerController.Instance?.SetControlsLocked(true);
        currentShopid = shopid;
        currentShopData = ShopDataManager.Instance.GetShopData(shopid);

        if (currentShopData == null)
        {
            Debug.LogError($"상점 '{shopid}'를 찾을 수 없습니다!");
            return;
        }

        isShopActive = true;
        shopUIPanel.SetActive(true);
        PlayerHUD.Instance?.RegisterUI(this);
        if (ItemUIManager.Instance?.IsItemUIOpen() == true)
        {
            ItemUIManager.Instance?.CloseItemUI();
        }
        if (EquipmentUIManager.Instance?.IsEquipmentUIOpen() == true)
        {
            EquipmentUIManager.Instance?.CloseEquipmentUI();
        }

        currentBuyMode = ShopMode.Buy;
        currentSellMode = ShopMode.SellEquipment;
        RefreshAllUI();
    }

    public void CloseShop()
    {
        HideItemTooltip();

        if (PopupManager.Instance != null)
        {
            PopupManager.Instance.CloseAllPopups();
        }

        pendingItemData = null;
        pendingShopItemData = null;
        isShopActive = false;
        shopUIPanel.SetActive(false);
        PlayerController.Instance?.SetControlsLocked(false);
        PlayerHUD.Instance?.UnregisterUI(this);
    }

    void SwitchBuyMode(ShopMode mode)
    {
        currentBuyMode = mode;

        HideItemTooltip();

        if (PopupManager.Instance != null)
        {
            PopupManager.Instance.CloseAllPopups();
        }

        pendingItemData = null;
        pendingShopItemData = null;
        isPendingTransactionRebuy = false;

        //UpdateBuyTabColors();
        RefreshBuyList();
    }

    void SwitchSellMode(ShopMode mode)
    {
        currentSellMode = mode;

        HideItemTooltip();

        if (PopupManager.Instance != null)
        {
            PopupManager.Instance.CloseAllPopups();
        }

        pendingItemData = null;
        pendingShopItemData = null;

        //UpdateSellTabColors();
        RefreshSellList();
    }

    void RefreshAllUI()
    {
        HideItemTooltip();

        if (PopupManager.Instance != null)
        {
            PopupManager.Instance.CloseAllPopups();
        }

        pendingItemData = null;
        pendingShopItemData = null;
        isPendingTransactionRebuy = false;

      //UpdateBuyTabColors();
      //UpdateSellTabColors();

        RefreshBuyList();
        RefreshSellList();
    }

    void RefreshBuyList()
    {
        ClearBuyItemList();

        switch (currentBuyMode)
        {
            case ShopMode.Buy:
                DisplayBuyItems();
                break;
            case ShopMode.Rebuy:
                DisplayRebuyItems();
                break;
        }
    }

    void RefreshSellList()
    {
        ClearSellItemList();

        switch (currentSellMode)
        {
            case ShopMode.SellEquipment:
                DisplaySellItems(ItemType.Equipment);
                break;
            case ShopMode.SellConsumable:
                DisplaySellItems(ItemType.Consumable);
                break;
            case ShopMode.SellMaterial:
                DisplaySellItems(ItemType.Material);
                break;
        }
    }

    void UpdateBuyTabColors()
    {
        buyTabButton.GetComponent<Image>().color = (currentBuyMode == ShopMode.Buy) ? Color.yellow : Color.white;
        rebuyTabButton.GetComponent<Image>().color = (currentBuyMode == ShopMode.Rebuy) ? Color.yellow : Color.white;
    }

    void UpdateSellTabColors()
    {
        equipmentTabButton.GetComponent<Image>().color = (currentSellMode == ShopMode.SellEquipment) ? Color.yellow : Color.white;
        usingitemTabButton.GetComponent<Image>().color = (currentSellMode == ShopMode.SellConsumable) ? Color.yellow : Color.white;
        etcitemTabButton.GetComponent<Image>().color = (currentSellMode == ShopMode.SellMaterial) ? Color.yellow : Color.white;
    }

    void DisplayBuyItems()
    {
        if (currentShopData == null) return;

        ShopStockSaveData stockData = GetShopStockData();
        if (stockData == null) return;

        foreach (ShopItemData shopItem in currentShopData.items)
        {
            ItemData itemData = ItemDataManager.Instance.GetItemData(shopItem.itemid);
            if (itemData == null) continue;

            int remainingStock = shopItem.limitedStock;
            if (shopItem.limitedStock >= 0)
            {
                int purchased = stockData.GetPurchasedQuantity(currentShopid, shopItem.itemid);
                remainingStock = shopItem.limitedStock - purchased;

                if (remainingStock <= 0)
                    continue;
            }

            CreateItemListEntry(itemData, buyitemListContainer, shopItem, remainingStock);
        }
    }

    void DisplayRebuyItems()
    {
        ShopStockSaveData stockData = GetShopStockData();
        if (stockData == null) return;

        List<string> itemsToRemove = new List<string>();

        foreach (var kvp in stockData.rebuyItems)
        {
            string itemId = kvp.itemId;
            int quantity = kvp.quantity;

            if (quantity <= 0)
            {
                itemsToRemove.Add(itemId);
                continue;
            }

            ItemData itemData = ItemDataManager.Instance.GetItemData(itemId);
            if (itemData == null) continue;

            CreateItemListEntry(itemData, buyitemListContainer, null, quantity, isRebuy: true);
        }

        foreach (string itemId in itemsToRemove)
        {
            stockData.rebuyItems.RemoveAll(r => itemsToRemove.Contains(r.itemId));
        }

        if (itemsToRemove.Count > 0)
        {
            SaveShopData();
        }
    }

    void DisplaySellItems(ItemType itemType)
    {
        if (InventoryManager.Instance == null) return;

        List<InventoryItem> inventoryItems = InventoryManager.Instance.GetItemsByType(itemType);

        foreach (InventoryItem item in inventoryItems)
        {
            ItemData itemData = item.GetItemData();
            if (itemData != null && item.quantity > 0)
            {
                CreateItemListEntry(itemData, sellitemListContainer, null, item.quantity);
            }
        }
    }

    void CreateItemListEntry(ItemData itemData, Transform container, ShopItemData shopItem = null, int stockOrQuantity = 0, bool isRebuy = false)
    {
        GameObject itemEntry = Instantiate(itemListPrefab, container);
        ShopItemUI shopItemUI = itemEntry.GetComponent<ShopItemUI>();

        if (shopItemUI != null)
        {
            bool isBuyMode = (container == buyitemListContainer);
            int displayPrice = isBuyMode ? itemData.buyPrice : itemData.sellPrice;

            string stockInfo = "";
            if (shopItem != null && shopItem.limitedStock >= 0)
            {
                stockInfo = $"재고: {stockOrQuantity}";
            }
            else if (!isBuyMode && stockOrQuantity > 0)
            {
                if (itemData.itemType != ItemType.Equipment)
                {
                    stockInfo = $"보유: {stockOrQuantity}";
                }
            }
            else if (isBuyMode && isRebuy && stockOrQuantity > 0)
            {
                stockInfo = $"재고: {stockOrQuantity}";
            }

            shopItemUI.SetItemInfo(itemData, shopItem, displayPrice, isBuyMode, stockInfo);
            shopItemUI.SetTransactionButtonCallback(() => OnTransactionButtonClicked(itemData, shopItem, isBuyMode, isRebuy));
        }
    }

    void OnTransactionButtonClicked(ItemData itemData, ShopItemData shopItem, bool isBuyMode, bool isRebuy = false)
    {
        pendingItemData = itemData;
        pendingShopItemData = shopItem;

        isPendingTransactionBuy = isBuyMode;
        isPendingTransactionRebuy = isRebuy;

        if (itemData.itemType == ItemType.Equipment)
        {
            pendingQuantity = 1;

            //  구매 시 골드 확인 (1개만 구매하므로 바로 확인)
            if (isBuyMode)
            {
                if (!CheckGoldBeforePurchase())
                    return;
            }

            ShowFinalConfirmPopup();
        }
        else
        {
            if (isBuyMode)
            {
                //  1개 가격이라도 살 수 없으면 바로 경고
                if (itemData.buyPrice > 0 && !PlayerStatsComponent.Instance.Stats.HasGold(itemData.buyPrice))
                {
                    ShowInsufficientGoldWarning(itemData.buyPrice, 1);
                    return;
                }

                ShopStockSaveData stockData = GetShopStockData();
                if (stockData == null) return;

                int availableStock = -1;
                if (isRebuy)
                {
                    availableStock = stockData.GetRebuyQuantity(itemData.itemId);
                }
                else if (shopItem != null && shopItem.limitedStock >= 0)
                {
                    int purchased = stockData.GetPurchasedQuantity(currentShopid, shopItem.itemid);
                    availableStock = shopItem.limitedStock - purchased;
                }
                ShowQuantityPopupForBuy(itemData, shopItem, availableStock);
            }
            else
            {
                int ownedQuantity = InventoryManager.Instance.GetItemQuantity(itemData.itemId);
                ShowQuantityPopupForSell(itemData, ownedQuantity);
            }
        }
    }

    /// <summary>
    /// 골드 부족 경고 팝업 표시
    /// </summary>
    void ShowInsufficientGoldWarning(int requiredGold, int quantity)
    {
        if (PopupManager.Instance == null) return;

        int currentGold = PlayerStatsComponent.Instance.Stats.gold;
        string message = $"골드가 부족합니다!\n\n필요: {requiredGold * quantity}\n보유: {currentGold}";

        PopupManager.Instance.ShowWarningPopup(message);
    }

    /// <summary>
    /// 구매 전 골드 확인
    /// </summary>
    bool CheckGoldBeforePurchase()
    {
        if (PlayerStatsComponent.Instance == null) return false;

        int totalCost = pendingItemData.buyPrice * pendingQuantity;

        if (totalCost > 0 && !PlayerStatsComponent.Instance.Stats.HasGold(totalCost))
        {
            ShowInsufficientGoldWarning(pendingItemData.buyPrice, pendingQuantity);
            return false;
        }

        return true;
    }

    void ShowQuantityPopupForBuy(ItemData itemData, ShopItemData shopItem, int stockOrQuantity)
    {
        if (PopupManager.Instance == null)
        {
            Debug.LogError("[ShopUIManager] PopupManager를 찾을 수 없습니다!");
            return;
        }

        int maxQuantity = GetMaxQuantityForBuy(itemData, shopItem, stockOrQuantity);
        int price = itemData.buyPrice;

        PopupManager.Instance.ShowQuantityPopup(
            itemData,
            maxQuantity,
            price,
            true,
            OnQuantitySelected
        );
    }

    void ShowQuantityPopupForSell(ItemData itemData, int ownedQuantity)
    {
        if (PopupManager.Instance == null)
        {
            Debug.LogError("[ShopUIManager] PopupManager를 찾을 수 없습니다!");
            return;
        }

        int price = itemData.sellPrice;

        PopupManager.Instance.ShowQuantityPopup(
            itemData,
            ownedQuantity,
            price,
            false,
            OnQuantitySelected
        );
    }

    void OnQuantitySelected(int quantity)
    {
        pendingQuantity = quantity;

        //  구매 시 선택한 수량으로 골드 확인
        if (isPendingTransactionBuy)
        {
            if (!CheckGoldBeforePurchase())
                return;
        }

        ShowFinalConfirmPopup();
    }

    void ShowFinalConfirmPopup()
    {
        if (PopupManager.Instance == null)
        {
            Debug.LogError("[ShopUIManager] PopupManager를 찾을 수 없습니다!");
            return;
        }

        if (pendingItemData == null) return;

        string message;
        if (isPendingTransactionBuy)
        {
            int totalCost = pendingItemData.buyPrice * pendingQuantity;
            message = $"{pendingItemData.itemName} {pendingQuantity}개\n구매하시겠습니까?\n총 비용: {totalCost}";
        }
        else
        {
            int totalEarn = pendingItemData.sellPrice * pendingQuantity;
            message = $"{pendingItemData.itemName} {pendingQuantity}개\n판매하시겠습니까?\n획득 골드: {totalEarn}";
        }

        PopupManager.Instance.ShowConfirmPopup(
            message,
            OnTransactionConfirmed,
            OnTransactionCancelled
        );
    }

    void OnTransactionConfirmed()
    {
        if (isPendingTransactionBuy)
        {
            ProcessBuy();
        }
        else
        {
            ProcessSell();
        }
    }

    void OnTransactionCancelled()
    {
        pendingItemData = null;
        pendingShopItemData = null;
    }

    int GetMaxQuantityForBuy(ItemData itemData, ShopItemData shopItem, int stockOrQuantity)
    {
        if (PlayerStatsComponent.Instance == null) return 1;

        ShopStockSaveData stockData = GetShopStockData();
        if (stockData == null) return 1;

        int affordableQuantity;
        if (itemData.buyPrice <= 0)
        {
            affordableQuantity = int.MaxValue;
        }
        else
        {
            affordableQuantity = PlayerStatsComponent.Instance.Stats.gold / itemData.buyPrice;
        }

        if (isPendingTransactionRebuy)
        {
            int rebuyStock = stockData.GetRebuyQuantity(itemData.itemId);
            if (rebuyStock > 0)
            {
                affordableQuantity = Mathf.Min(affordableQuantity, rebuyStock);
            }
            else
            {
                return 0;
            }
        }
        else if (shopItem != null && shopItem.limitedStock >= 0)
        {
            int purchased = stockData.GetPurchasedQuantity(currentShopid, shopItem.itemid);
            int remaining = shopItem.limitedStock - purchased;
            affordableQuantity = Mathf.Min(affordableQuantity, remaining);
        }

        return Mathf.Max(1, affordableQuantity);
    }

    void ProcessBuy()
    {
        if (pendingItemData == null) return;

        if (PlayerStatsComponent.Instance == null)
        {
            Debug.LogError("[ShopUIManager] PlayerStatsComponent를 찾을 수 없습니다!");
            return;
        }

        int totalCost = pendingItemData.buyPrice * pendingQuantity;

        if (!PlayerStatsComponent.Instance.Stats.HasGold(totalCost))
        {
            ShowInsufficientGoldWarning(pendingItemData.buyPrice, pendingQuantity);
            return;
        }

        ShopStockSaveData stockData = GetShopStockData();
        if (stockData == null) return;

        if (isPendingTransactionRebuy)
        {
            int currentStock = stockData.GetRebuyQuantity(pendingItemData.itemId);
            if (pendingQuantity > currentStock)
            {
                if (PopupManager.Instance != null)
                {
                    PopupManager.Instance.ShowWarningPopup("재고가 부족합니다!");
                }
                return;
            }

            stockData.ReduceRebuyItem(pendingItemData.itemId, pendingQuantity);
        }
        else if (pendingShopItemData != null && pendingShopItemData.limitedStock >= 0)
        {
            int purchased = stockData.GetPurchasedQuantity(currentShopid, pendingShopItemData.itemid);
            int remaining = pendingShopItemData.limitedStock - purchased;

            if (pendingQuantity > remaining)
            {
                if (PopupManager.Instance != null)
                {
                    PopupManager.Instance.ShowWarningPopup("재고가 부족합니다!");
                }
                return;
            }

            stockData.RecordPurchase(currentShopid, pendingShopItemData.itemid, pendingQuantity);
        }

        PlayerStatsComponent.Instance.Stats.SpendGold(totalCost);
        InventoryManager.Instance.AddItem(pendingItemData.itemId, pendingQuantity);
        FloatingItemManager.Instance?.ShowItemAcquired(ItemDataManager.Instance.GetItemData(pendingItemData.itemId), pendingQuantity);

        Debug.Log($"{pendingItemData.itemName} {pendingQuantity}개 구매 완료!");

        SaveShopData();
        RefreshAllUI();
    }

    void ProcessSell()
    {
        if (pendingItemData == null) return;

        if (PlayerStatsComponent.Instance == null)
        {
            Debug.LogError("[ShopUIManager] PlayerStatsComponent를 찾을 수 없습니다!");
            return;
        }

        int ownedQuantity = InventoryManager.Instance.GetItemQuantity(pendingItemData.itemId);
        if (pendingQuantity > ownedQuantity)
        {
            if (PopupManager.Instance != null)
            {
                PopupManager.Instance.ShowWarningPopup("판매할 아이템이 부족합니다!");
            }
            return;
        }

        int totalEarn = pendingItemData.sellPrice * pendingQuantity;
        InventoryManager.Instance.RemoveItem(pendingItemData.itemId, pendingQuantity);
        PlayerStatsComponent.Instance.Stats.AddGold(totalEarn);

        ShopStockSaveData stockData = GetShopStockData();
        if (stockData != null)
        {
            stockData.AddRebuyItem(pendingItemData.itemId, pendingQuantity);
            SaveShopData();
        }

        Debug.Log($"{pendingItemData.itemName} {pendingQuantity}개 판매 완료! ({totalEarn}G 획득)");

        currentBuyMode = ShopMode.Rebuy;
        RefreshAllUI();
    }

    public void ShowDiscardQuantityPopup(ItemData itemData, int ownedQuantity, System.Action<int> onConfirm)
    {
        if (PopupManager.Instance == null)
        {
            Debug.LogWarning("[ShopUIManager] PopupManager를 찾을 수 없습니다.");
            onConfirm?.Invoke(1);
            return;
        }

        PopupManager.Instance.ShowDiscardQuantityPopup(itemData, ownedQuantity, onConfirm);
    }

    public void ShowDiscardConfirmPopup(string itemName, int quantity, System.Action onConfirm)
    {
        if (PopupManager.Instance == null)
        {
            Debug.LogWarning("[ShopUIManager] PopupManager를 찾을 수 없습니다.");
            onConfirm?.Invoke();
            return;
        }

        string message = $"{itemName} {quantity}개를\n정말 버리시겠습니까?";
        PopupManager.Instance.ShowConfirmPopup(message, onConfirm);
    }

    public void ShowItemTooltip(ItemData itemData, ShopItemData shopItem = null)
    {
        if (itemTooltipPanel == null) return;

        itemTooltipPanel.SetActive(true);
        itemTooltipPanel.transform.SetAsLastSibling();
        isTooltipActive = true;

        if (tooltipNameText != null)
            tooltipNameText.text = itemData.itemName;

        if (tooltipDescriptionText != null)
            tooltipDescriptionText.text = itemData.description;

        string statsInfo = "";
        if (itemData.attackBonus > 0)
            statsInfo += $"공격력: +{itemData.attackBonus}\n";
        if (itemData.defenseBonus > 0)
            statsInfo += $"방어력: +{itemData.defenseBonus}\n";
        if (itemData.GetHealAmount() > 0)
            statsInfo += $"체력 회복: +{itemData.GetHealAmount()}\n";
        if (itemData.strBonus > 0)
            statsInfo += $"힘: +{itemData.strBonus}\n";
        if (itemData.dexBonus > 0)
            statsInfo += $"민첩: +{itemData.dexBonus}\n";
        if (itemData.intBonus > 0)
            statsInfo += $"지능: +{itemData.intBonus}\n";

        if (tooltipStatsText != null)
            tooltipStatsText.text = statsInfo;

        UpdateTooltipPosition();
    }

    void UpdateTooltipPosition()
    {
        if (itemTooltipPanel == null) return;

        Vector2 mousePosition = Input.mousePosition;
        RectTransform tooltipRect = itemTooltipPanel.GetComponent<RectTransform>();

        if (tooltipRect != null)
        {
            Vector2 offset = new Vector2(15f, -15f);
            Vector2 newPosition = mousePosition + offset;

            float tooltipPivotCompensationX = tooltipRect.rect.width * tooltipRect.pivot.x;
            float tooltipPivotCompensationY = tooltipRect.rect.height * tooltipRect.pivot.y;

            newPosition.x += tooltipPivotCompensationX;
            newPosition.y -= tooltipPivotCompensationY;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            if (newPosition.x + tooltipRect.rect.width > screenWidth)
            {
                newPosition.x = mousePosition.x - tooltipRect.rect.width - 15f;
            }

            if (newPosition.y - tooltipRect.rect.height < 0)
            {
                newPosition.y = mousePosition.y + tooltipRect.rect.height + 15f;
            }

            tooltipRect.position = newPosition;
        }
    }

    public void UpdatePlayerGoldDisplay()
    {
        if (playerGoldText == null) return;

        if (PlayerStatsComponent.Instance != null)
        {
            playerGoldText.text = $"{PlayerStatsComponent.Instance.Stats.gold}";
        }
        else
        {
            playerGoldText.text = "0";
        }
    }

    public void HideItemTooltip()
    {
        isTooltipActive = false;
        if (itemTooltipPanel != null)
            itemTooltipPanel.SetActive(false);
    }

    void ClearBuyItemList()
    {
        foreach (Transform child in buyitemListContainer)
        {
            Destroy(child.gameObject);
        }
    }

    void ClearSellItemList()
    {
        foreach (Transform child in sellitemListContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void LoadShopData()
    {
        Debug.Log("[ShopUI] 상점 재고 로드 완료!");
    }

    public void SaveShopData()
    {
        if (CharacterSaveManager.Instance == null || CharacterSaveManager.Instance.CurrentCharacter == null)
        {
            Debug.LogWarning("[ShopUI] CharacterSaveManager가 없어 상점 데이터를 저장할 수 없습니다!");
            return;
        }

        ShopStockSaveData stockData = GetShopStockData();
        if (stockData != null)
        {
            stockData.CommitTempData();
            Debug.Log("[ShopUI] 상점 임시 데이터 커밋 완료");
        }

        bool success = CharacterSaveManager.Instance.SaveCurrentCharacterGameData();
        if (success)
        {
            Debug.Log("[ShopUI] 상점 거래 후 자동 저장 완료!");
        }
        else
        {
            Debug.LogWarning("[ShopUI] 상점 거래 후 자동 저장 실패!");
        }
    }

    public void Close()
    {
        CloseShop();
    }

    public GameObject GetUIPanel()
    {
        return shopUIPanel;
    }
}