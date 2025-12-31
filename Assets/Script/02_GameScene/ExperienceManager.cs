using UnityEngine;


/// 경험치 관리 싱글톤 매니저
/// 다른 스크립트에서 쉽게 경험치를 지급할 수 있도록 함

public class ExperienceManager : MonoBehaviour
{
    public static ExperienceManager Instance { get; private set; }

    [Header("경험치 배율 설정")]
    [SerializeField] private float expMultiplier = 1.0f; // 경험치 배율 (이벤트 등에 사용)

    [Header("골드 배율 설정")]
    [SerializeField] private float goldMultiplier = 1.0f; // 골드 배율 (이벤트 등에 사용)
    [SerializeField] private float goldRandomRange = 0.2f; // 골드 랜덤 범위 (±20%)

    [Header("레벨별 경험치 테이블 (옵션)")]
    [SerializeField] private bool useCustomExpTable = false;
    [SerializeField] private int[] customExpTable; // 레벨별 필요 경험치

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
    }
   
    
    /// 플레이어에게 골드 지급
    
    public void AddGold(int baseAmount, bool is_multiple = false)
    {
        if (PlayerController.Instance == null)
        {
            Debug.LogWarning("[ExpManager] PlayerController를 찾을 수 없습니다.");
            return;
        }

        var playerStats = PlayerController.Instance.GetComponent<PlayerStatsComponent>();
        if (playerStats != null)
        {
            int finalAmount = baseAmount;
            //골드 비율 조정,퀘스트는 비율을 정하지 않고 그대로 들어오도록함
            if (is_multiple)
            {
                float randomFactor = Random.Range(1f - goldRandomRange, 1f + goldRandomRange);
                finalAmount = Mathf.RoundToInt(baseAmount * randomFactor * goldMultiplier);
            }
            playerStats.Stats.AddGold(finalAmount);
        }
        else
        {
            Debug.LogWarning("[ExpManager] 플레이어에게 스탯 컴포넌트가 없습니다.");
        }
    }

    
    /// 플레이어에게 경험치 지급
    
    public void AddExp(int baseAmount, bool is_multiple = false)
    {
        if (PlayerController.Instance == null)
        {
            Debug.LogWarning("[ExpManager] PlayerController를 찾을 수 없습니다.");
            return;
        }

        var playerStats = PlayerController.Instance.GetComponent<PlayerStatsComponent>();
        if (playerStats != null)
        {
            int finalAmount = baseAmount;
            //경험치 비율 조정,퀘스트는 비율을 정하지 않고 그대로 들어오도록함
            if (is_multiple)
            {
                finalAmount = Mathf.RoundToInt(baseAmount * expMultiplier);
            }

            playerStats.Stats.GainExperience(finalAmount);
        }
        else
        {
            Debug.LogWarning("[ExpManager] 플레이어에게 스탯 컴포넌트가 없습니다.");
        }
    }

    
    /// 퀘스트 완료 시 경험치 지급
    
    public void GiveQuestReward(int rewardExp)
    {
        AddExp(rewardExp);
        Debug.Log($"[ExpManager] 퀘스트 보상 경험치 지급: {rewardExp}");
    }

    
    /// 경험치 배율 설정 (이벤트, 버프 등에 사용)
    
    public void SetExpMultiplier(float multiplier)
    {
        expMultiplier = Mathf.Max(0.1f, multiplier);
        Debug.Log($"[ExpManager] 경험치 배율 변경: x{expMultiplier}");
    }

    
    /// 골드 배율 설정 (이벤트, 버프 등에 사용)
    
    public void SetGoldMultiplier(float multiplier)
    {
        goldMultiplier = Mathf.Max(0.1f, multiplier);
        Debug.Log($"[ExpManager] 골드 배율 변경: x{goldMultiplier}");
    }

    
    /// 현재 경험치 배율 반환
    
    public float GetExpMultiplier()
    {
        return expMultiplier;
    }

    
    /// 현재 골드 배율 반환
    
    public float GetGoldMultiplier()
    {
        return goldMultiplier;
    }

    
    /// 레벨별 필요 경험치 계산 (커스텀 테이블 사용)
    
    public int GetRequiredExpForLevel(int level)
    {
        if (useCustomExpTable && customExpTable != null && level <= customExpTable.Length)
        {
            return customExpTable[level - 1];
        }
        else
        {
            // 기본 공식: 100 * 1.2^(level-1)
            return Mathf.RoundToInt(100 * Mathf.Pow(1.2f, level - 1));
        }
    }
}