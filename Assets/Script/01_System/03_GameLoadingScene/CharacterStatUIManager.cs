using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterStatUIManager : MonoBehaviour,IClosableUI
{
    public static CharacterStatUIManager Instance { get; private set; }

    [Header("헤더")]
    public Button closeButton;

    [Header("스탯")]
    public GameObject statUIPanel;
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI HPText;
    public TextMeshProUGUI expText;
    public TextMeshProUGUI strengthText;
    public TextMeshProUGUI dexterityText;
    public TextMeshProUGUI intelligenceText;
    public TextMeshProUGUI luckText;
    public TextMeshProUGUI techniqueText;
    public TextMeshProUGUI attackPowerText;
    public TextMeshProUGUI defenseText;
    public TextMeshProUGUI criticalChanceText;
    public TextMeshProUGUI criticalDamageText;
    public TextMeshProUGUI evasionRateText;
    public TextMeshProUGUI accuracyText;

    private bool isOpen = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseStatUI);
            RefreshStatsReference();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        if (statUIPanel != null)
            statUIPanel.SetActive(false);
        
    }

    public void OpenStatUI()
    {
        if (isOpen) return;

        // 대화 중이면 스탯 창 열지 않음
        if (DialogueUIManager.Instance != null && DialogueUIManager.Instance.IsDialogueOpen)
            return;

        isOpen = true;
        statUIPanel.SetActive(true);
        PlayerHUD.Instance?.RegisterUI(this);
        RefreshStatsReference();
        Debug.Log("[StatsUI] 스탯 창 열림");
    }

    public void CloseStatUI()
    {
        if (!isOpen) return;

        isOpen = false;
        statUIPanel.SetActive(false);
        PlayerHUD.Instance?.UnregisterUI(this);

        Debug.Log("[StatsUI] 스탯 창 닫힘");
    }

    public void RefreshStatsReference()
    {
        // UI 업데이트
        if (PlayerStatsComponent.Instance != null)
        {
            CharacterStats stats = PlayerStatsComponent.Instance.Stats;

            // 기본 정보
            characterNameText.text = stats.characterName;
            levelText.text = $"Lv. {stats.level}";
            HPText.text = $"{stats.currentHP} / {stats.maxHP}";

            // 경험치 퍼센트 계산
            float expPercent = stats.expToNextLevel > 0 ? (stats.currentExp * 100f / stats.expToNextLevel) : 100f;
            expText.text = $"{Mathf.FloorToInt(expPercent)}%";

            //  5대 기본 스탯 - 장비 보너스 포함한 최종값 표시
            strengthText.text = (stats.strength + stats.equip_strength).ToString();
            dexterityText.text = (stats.dexterity + stats.equip_dexterity).ToString();
            intelligenceText.text = (stats.intelligence + stats.equip_intelligence).ToString();
            luckText.text = (stats.luck + stats.equip_luck).ToString();
            techniqueText.text = (stats.technique + stats.equip_technique).ToString();

            // 전투 스탯 (이미 계산된 최종값)
            int min_damage = Mathf.RoundToInt(stats.attackPower * 0.8f);
            int max_damage = Mathf.RoundToInt(stats.attackPower * 1.2f);

            attackPowerText.text = $"{min_damage} - {max_damage}";
            defenseText.text = stats.defense.ToString();
            criticalChanceText.text = $"{stats.criticalChance:F1}%";
            criticalDamageText.text = $"{stats.criticalDamage:F0}%";
            evasionRateText.text = $"{stats.evasionRate:F1}%";
            accuracyText.text = $"{stats.accuracy:F1}%";
        }
    }

    public bool IsStatsUIOpen()
    {
        return isOpen;
    }

    public void Close()
    {
        CloseStatUI();
    }

    public GameObject GetUIPanel()
    {
        return statUIPanel;
    }
}