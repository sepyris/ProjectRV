using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 장비창 UI 관리
/// 장비 탭과 치장 탭을 제공하며 더블클릭으로 장착/해제
/// 
///  수정사항: weaponSlot이 MeleeWeapon과 RangedWeapon 둘 다 처리
/// </summary>
public class EquipmentUIManager : MonoBehaviour, IClosableUI
{
    public static EquipmentUIManager Instance { get; private set; }

    [Header("메인 패널")]
    public GameObject equipmentUIPanel;
    public Button closeButton;

    [Header("탭 버튼")]
    public Button equipmentTabButton;  // 장비 탭
    public Button cosmeticTabButton;   // 치장 탭

    [Header("장비 슬롯들 - 장비 탭")]
    public EquipmentSlotUI helmetSlot;
    public EquipmentSlotUI armorSlot;
    public EquipmentSlotUI shoesSlot;
    public EquipmentSlotUI weaponSlot;      //  근거리/원거리 무기 모두 표시
    public EquipmentSlotUI subWeaponSlot;
    public EquipmentSlotUI ringSlot;
    public EquipmentSlotUI necklaceSlot;
    public EquipmentSlotUI braceletSlot;

    [Header("치장 슬롯들 - 치장 탭")]
    public EquipmentSlotUI cosmeticHelmetSlot;
    public EquipmentSlotUI cosmeticArmorSlot;
    public EquipmentSlotUI cosmeticShoesSlot;
    public EquipmentSlotUI cosmeticWeaponSlot;
    public EquipmentSlotUI cosmetichairSlot;
    public EquipmentSlotUI cosmeticfaceSlot;
    public EquipmentSlotUI cosmeticcapeSlot;

    private enum EquipmentTab
    {
        Equipment,
        Cosmetic
    }

    private EquipmentTab currentTab = EquipmentTab.Equipment;
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
            return;
        }
        equipmentUIPanel.SetActive(false);
        SetupButtons();
        InitializeSlots();
        SubscribeToEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SetupButtons()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseEquipmentUI);

        if (equipmentTabButton != null)
            equipmentTabButton.onClick.AddListener(() => SwitchTab(EquipmentTab.Equipment));

        if (cosmeticTabButton != null)
            cosmeticTabButton.onClick.AddListener(() => SwitchTab(EquipmentTab.Cosmetic));
    }

    private void InitializeSlots()
    {
        // 장비 슬롯 초기화 (장비 모드)
        if (helmetSlot != null) helmetSlot.InitializeAsEquipment(EquipmentSlot.Helmet, "모자");
        if (armorSlot != null) armorSlot.InitializeAsEquipment(EquipmentSlot.Armor, "옷");
        if (shoesSlot != null) shoesSlot.InitializeAsEquipment(EquipmentSlot.Shoes, "신발");
        if (weaponSlot != null) weaponSlot.InitializeAsEquipment(EquipmentSlot.MeleeWeapon, "무기");  //  MeleeWeapon으로 초기화 (나중에 둘 다 처리)
        if (subWeaponSlot != null) subWeaponSlot.InitializeAsEquipment(EquipmentSlot.SubWeapon, "보조무기");
        if (ringSlot != null) ringSlot.InitializeAsEquipment(EquipmentSlot.Ring, "반지");
        if (necklaceSlot != null) necklaceSlot.InitializeAsEquipment(EquipmentSlot.Necklace, "목걸이");
        if (braceletSlot != null) braceletSlot.InitializeAsEquipment(EquipmentSlot.Bracelet, "팔찌");

        // 치장 슬롯 초기화 (치장 모드)
        if (cosmeticHelmetSlot != null) cosmeticHelmetSlot.InitializeAsCosmetic(CosmeticSlot.Helmet, "모자");
        if (cosmeticArmorSlot != null) cosmeticArmorSlot.InitializeAsCosmetic(CosmeticSlot.Armor, "옷");
        if (cosmeticShoesSlot != null) cosmeticShoesSlot.InitializeAsCosmetic(CosmeticSlot.Shoes, "신발");
        if (cosmeticWeaponSlot != null) cosmeticWeaponSlot.InitializeAsCosmetic(CosmeticSlot.Weapon, "무기");
        if (cosmetichairSlot != null) cosmetichairSlot.InitializeAsCosmetic(CosmeticSlot.Hair, "헤어");
        if (cosmeticfaceSlot != null) cosmeticfaceSlot.InitializeAsCosmetic(CosmeticSlot.FaceAccessory, "얼굴장식");
        if (cosmeticcapeSlot != null) cosmeticcapeSlot.InitializeAsCosmetic(CosmeticSlot.Cape, "망토");
    }

    private void SubscribeToEvents()
    {
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged += OnEquipmentChanged;
            EquipmentManager.Instance.OnCosmeticChanged += OnCosmeticChanged;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged -= OnEquipmentChanged;
            EquipmentManager.Instance.OnCosmeticChanged -= OnCosmeticChanged;
        }
    }

    // ==================== UI 열기/닫기 ====================

    public void OpenEquipmentUI()
    {
        if (isOpen) return;

        // 대화 중이면 열지 않음
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
            return;

        isOpen = true;
        equipmentUIPanel.SetActive(true);
        PlayerHUD.Instance?.RegisterUI(this);
        SwitchTab(currentTab);
        RefreshAllSlots();

        Debug.Log("[EquipmentUI] 장비창 열림");
    }

    public void CloseEquipmentUI()
    {
        if (!isOpen) return;

        isOpen = false;
        equipmentUIPanel.SetActive(false);
        PlayerHUD.Instance?.UnregisterUI(this);

        //상세 패널 닫기
        HideAllDetailPanels();

        //  드래그 중인 장비가 있다면 취소
        if (DraggableEquipmentUI.IsDragging())
        {
            Debug.Log("[EquipmentUIManager] 장비 드래그 중 UI 갱신 감지 - 드래그 취소");
            DraggableEquipmentUI.CancelCurrentDrag();
        }
        Debug.Log("[EquipmentUI] 장비창 닫힘");
    }

    public bool IsEquipmentUIOpen()
    {
        return isOpen;
    }

    // ==================== 탭 전환 ====================

    private void SwitchTab(EquipmentTab tab)
    {
        currentTab = tab;
        //UpdateTabButtons();
        if (tab == EquipmentTab.Equipment)
        {
            helmetSlot.gameObject.SetActive(true);
            armorSlot.gameObject.SetActive(true);
            shoesSlot.gameObject.SetActive(true);
            weaponSlot.gameObject.SetActive(true);
            subWeaponSlot.gameObject.SetActive(true);
            ringSlot.gameObject.SetActive(true);
            necklaceSlot.gameObject.SetActive(true);
            braceletSlot.gameObject.SetActive(true);
            cosmeticHelmetSlot.gameObject.SetActive(false);
            cosmeticArmorSlot.gameObject.SetActive(false);
            cosmeticShoesSlot.gameObject.SetActive(false);
            cosmeticWeaponSlot.gameObject.SetActive(false);
            cosmetichairSlot.gameObject.SetActive(false);
            cosmeticfaceSlot.gameObject.SetActive(false);
            cosmeticcapeSlot.gameObject.SetActive(false);
        }
        else if (tab == EquipmentTab.Cosmetic)
        {
            cosmeticHelmetSlot.gameObject.SetActive(true);
            cosmeticArmorSlot.gameObject.SetActive(true);
            cosmeticShoesSlot.gameObject.SetActive(true);
            cosmeticWeaponSlot.gameObject.SetActive(true);
            cosmetichairSlot.gameObject.SetActive(true);
            cosmeticfaceSlot.gameObject.SetActive(true);
            cosmeticcapeSlot.gameObject.SetActive(true);
            helmetSlot.gameObject.SetActive(false);
            armorSlot.gameObject.SetActive(false);
            shoesSlot.gameObject.SetActive(false);
            weaponSlot.gameObject.SetActive(false);
            subWeaponSlot.gameObject.SetActive(false);
            ringSlot.gameObject.SetActive(false);
            necklaceSlot.gameObject.SetActive(false);
            braceletSlot.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"[EquipmentUI] 알 수 없는 탭: {tab}");
            return;
        }

        Debug.Log($"[EquipmentUI] {tab} 탭으로 전환");
    }

    private void UpdateTabButtons()
    {
        UpdateTabButtonColor(equipmentTabButton, currentTab == EquipmentTab.Equipment, new Color(1f, 0.8f, 0.5f));
        UpdateTabButtonColor(cosmeticTabButton, currentTab == EquipmentTab.Cosmetic, new Color(0.8f, 0.5f, 1f));


    }

    private void UpdateTabButtonColor(Button button, bool isActive, Color activeColor)
    {
        if (button == null) return;

        var colors = button.colors;
        colors.normalColor = isActive ? activeColor : Color.white;
        button.colors = colors;
    }

    // ==================== 슬롯 갱신 ====================

    public void RefreshAllSlots()
    {
        //  모든 슬롯 갱신 전에 모든 상세 패널 숨김
        HideAllDetailPanels();
        //  드래그 중인 장비가 있다면 취소
        if (DraggableEquipmentUI.IsDragging())
        {
            Debug.Log("[EquipmentUIManager] 장비 드래그 중 UI 갱신 감지 - 드래그 취소");
            DraggableEquipmentUI.CancelCurrentDrag();
        }


        // 장비 슬롯 갱신
        RefreshEquipmentSlot(helmetSlot, EquipmentSlot.Helmet);
        RefreshEquipmentSlot(armorSlot, EquipmentSlot.Armor);
        RefreshEquipmentSlot(shoesSlot, EquipmentSlot.Shoes);
        RefreshWeaponSlot(); //  무기 슬롯은 근거리/원거리 모두 체크
        RefreshEquipmentSlot(subWeaponSlot, EquipmentSlot.SubWeapon);
        RefreshEquipmentSlot(ringSlot, EquipmentSlot.Ring);
        RefreshEquipmentSlot(necklaceSlot, EquipmentSlot.Necklace);
        RefreshEquipmentSlot(braceletSlot, EquipmentSlot.Bracelet);

        // 치장 슬롯 갱신
        RefreshCosmeticSlot(cosmeticHelmetSlot, CosmeticSlot.Helmet);
        RefreshCosmeticSlot(cosmeticArmorSlot, CosmeticSlot.Armor);
        RefreshCosmeticSlot(cosmeticShoesSlot, CosmeticSlot.Shoes);
        RefreshCosmeticSlot(cosmeticWeaponSlot, CosmeticSlot.Weapon);
        RefreshCosmeticSlot(cosmetichairSlot, CosmeticSlot.Hair);
        RefreshCosmeticSlot(cosmeticfaceSlot, CosmeticSlot.FaceAccessory);
        RefreshCosmeticSlot(cosmeticcapeSlot, CosmeticSlot.Cape);
    }

    /// <summary>
    ///  무기 슬롯 특별 처리: MeleeWeapon과 RangedWeapon 둘 다 체크
    /// </summary>
    private void RefreshWeaponSlot()
    {
        if (weaponSlot == null) return;

        // 1순위: 근거리 무기 확인
        InventoryItem meleeItem = EquipmentManager.Instance?.GetEquippedItem(EquipmentSlot.MeleeWeapon);
        if (meleeItem != null)
        {
            weaponSlot.UpdateSlot(meleeItem);
            return;
        }

        // 2순위: 원거리 무기 확인
        InventoryItem rangedItem = EquipmentManager.Instance?.GetEquippedItem(EquipmentSlot.RangedWeapon);
        if (rangedItem != null)
        {
            weaponSlot.UpdateSlot(rangedItem);
            return;
        }

        // 둘 다 없으면 빈 슬롯
        weaponSlot.UpdateSlot(null);
    }

    private void RefreshEquipmentSlot(EquipmentSlotUI slotUI, EquipmentSlot slot)
    {
        if (slotUI == null) return;

        InventoryItem item = EquipmentManager.Instance?.GetEquippedItem(slot);
        slotUI.UpdateSlot(item);
    }

    private void RefreshCosmeticSlot(EquipmentSlotUI slotUI, CosmeticSlot slot)
    {
        if (slotUI == null) return;

        InventoryItem item = EquipmentManager.Instance?.GetCosmeticItem(slot);
        slotUI.UpdateSlot(item);
    }

    /// <summary>
    ///  모든 슬롯의 상세 패널 숨김
    /// </summary>
    private void HideAllDetailPanels()
    {
        // 장비 슬롯들의 상세 패널 숨김
        if (helmetSlot != null) helmetSlot.HideDetailPanel();
        if (armorSlot != null) armorSlot.HideDetailPanel();
        if (shoesSlot != null) shoesSlot.HideDetailPanel();
        if (weaponSlot != null) weaponSlot.HideDetailPanel();
        if (subWeaponSlot != null) subWeaponSlot.HideDetailPanel();
        if (ringSlot != null) ringSlot.HideDetailPanel();
        if (necklaceSlot != null) necklaceSlot.HideDetailPanel();
        if (braceletSlot != null) braceletSlot.HideDetailPanel();

        // 치장 슬롯들의 상세 패널 숨김
        if (cosmeticHelmetSlot != null) cosmeticHelmetSlot.HideDetailPanel();
        if (cosmeticArmorSlot != null) cosmeticArmorSlot.HideDetailPanel();
        if (cosmeticShoesSlot != null) cosmeticShoesSlot.HideDetailPanel();
        if (cosmeticWeaponSlot != null) cosmeticWeaponSlot.HideDetailPanel();
        if (cosmetichairSlot != null) cosmetichairSlot.HideDetailPanel();
        if (cosmeticfaceSlot != null) cosmeticfaceSlot.HideDetailPanel();
        if (cosmeticcapeSlot != null) cosmeticcapeSlot.HideDetailPanel();
    }

    // ==================== 이벤트 핸들러 ====================

    private void OnEquipmentChanged(EquipmentSlot slot, InventoryItem item)
    {
        if (!isOpen) return;
        //  슬롯 갱신 전에 모든 상세 패널 숨김
        HideAllDetailPanels();


        // 해당 슬롯만 갱신
        switch (slot)
        {
            case EquipmentSlot.Helmet:
                RefreshEquipmentSlot(helmetSlot, slot);
                break;
            case EquipmentSlot.Armor:
                RefreshEquipmentSlot(armorSlot, slot);
                break;
            case EquipmentSlot.Shoes:
                RefreshEquipmentSlot(shoesSlot, slot);
                break;
            case EquipmentSlot.MeleeWeapon:      //  근거리 무기
                RefreshWeaponSlot();
                break;
            case EquipmentSlot.RangedWeapon:     //  원거리 무기
                RefreshWeaponSlot();
                break;
            case EquipmentSlot.SubWeapon:
                RefreshEquipmentSlot(subWeaponSlot, slot);
                break;
            case EquipmentSlot.Ring:
                RefreshEquipmentSlot(ringSlot, slot);
                break;
            case EquipmentSlot.Necklace:
                RefreshEquipmentSlot(necklaceSlot, slot);
                break;
            case EquipmentSlot.Bracelet:
                RefreshEquipmentSlot(braceletSlot, slot);
                break;
        }
    }

    private void OnCosmeticChanged(CosmeticSlot slot, InventoryItem item)
    {
        if (!isOpen) return;
        //  슬롯 갱신 전에 모든 상세 패널 숨김
        HideAllDetailPanels();


        // 해당 슬롯만 갱신
        switch (slot)
        {
            case CosmeticSlot.Helmet:
                RefreshCosmeticSlot(cosmeticHelmetSlot, slot);
                break;
            case CosmeticSlot.Armor:
                RefreshCosmeticSlot(cosmeticArmorSlot, slot);
                break;
            case CosmeticSlot.Shoes:
                RefreshCosmeticSlot(cosmeticShoesSlot, slot);
                break;
            case CosmeticSlot.Weapon:
                RefreshCosmeticSlot(cosmeticWeaponSlot, slot);
                break;
            case CosmeticSlot.Hair:
                RefreshCosmeticSlot(cosmetichairSlot, slot);
                break;
            case CosmeticSlot.FaceAccessory:
                RefreshCosmeticSlot(cosmeticfaceSlot, slot);
                break;
            case CosmeticSlot.Cape:
                RefreshCosmeticSlot(cosmeticcapeSlot, slot);
                break;
        }
    }

    // ==================== 외부 호출 메서드 ====================

    public void RefreshUI()
    {
        if (isOpen)
            RefreshAllSlots();
    }

    public void Close()
    {
        CloseEquipmentUI();
    }

    public GameObject GetUIPanel()
    {
        return equipmentUIPanel;
    }
}