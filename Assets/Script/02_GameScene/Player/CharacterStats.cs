using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class CharacterStats
{
    [Header("기본 정보")]
    public string characterName = "Unknown";
    public int level = 1;
    public int currentExp = 0;
    public int expToNextLevel = 100;
    public int gold = 0;

    [Header("직업 정보")]
    public List<PlayerJobData> jobHistory = new List<PlayerJobData>();

    [Header("기본 스탯")]
    public int strength = 10;
    public int dexterity = 10;
    public int intelligence = 10;
    public int luck = 10;
    public int technique = 10;

    [Header("전투 스탯")]
    public int maxHP = 100;
    public int currentHP = 100;
    public int attackPower = 15;
    public int defense = 5;
    public float criticalChance = 5f;
    public float criticalDamage = 150f;
    public float evasionRate = 5f;
    public float accuracy = 95f;

    [Header("장비 스탯")]
    public int equip_strength = 0;
    public int equip_dexterity = 0;
    public int equip_intelligence = 0;
    public int equip_luck = 0;
    public int equip_technique = 0;
    public int equip_attackBonus = 0;
    public int equip_defenseBonus = 0;
    public int equip_HPBonus = 0;

    [Header("스킬스텟")]
    public float skill_attackBonus = 0f;
    public float skill_defenseBonus = 0f;
    public float skill_HPBonus = 0f;
    public float skill_criticalChance = 0f;
    public float skill_criticalDamage = 0f;
    public float skill_evasionRate = 0f;
    public float skill_accuracy = 0f;

    [Header("이동 관련")]
    public float baseMoveSpeed = 5f;
    public float equip_moveSpeedBonus = 0f;
    public float skill_moveSpeedBonus = 0f;

    public float moveSpeed { get; private set; }

    public event Action OnStatsChanged;
    public event Action OnLevelUp;
    public event Action OnDeath;
    public event Action<int> OnExpGained;
    public event Action<int> OnGoldChanged;
    public event Action OnJobChanged;

    private bool is_monster_stat = false;
    private DamageTextSpawner damageTextSpawner;

    public void SetDamageTextSpawner(DamageTextSpawner spawner)
    {
        this.damageTextSpawner = spawner;
    }

    public void Initialize(string name = "Character", int startLevel = 1, bool is_monster = false)
    {
        characterName = name;
        level = startLevel;
        currentExp = 0;
        is_monster_stat = is_monster;

        strength = 10 + (level - 1) * 2;
        dexterity = 10 + (level - 1) * 2;
        intelligence = 10 + (level - 1) * 2;
        luck = 10 + (level - 1) * 2;
        technique = 10 + (level - 1) * 2;

        if (!is_monster)
        {
            AddJob(JobsType.Novice, true);
        }

        RecalculateStats();
        currentHP = maxHP;
    }

    public void AddJob(JobsType jobType, bool setAsCurrent = true)
    {
        if (HasJob(jobType))
        {
            Debug.LogWarning($"[{characterName}] 이미 보유한 직업입니다: {jobType}");
            return;
        }

        if (setAsCurrent)
        {
            foreach (var job in jobHistory)
            {
                job.isCurrentJob = false;
            }
        }

        PlayerJobData newJob = new PlayerJobData(jobType, setAsCurrent);
        jobHistory.Add(newJob);

        Debug.Log($"[{characterName}] 새 직업 획득: {newJob.GetJobName()} (현재 직업: {setAsCurrent})");

        OnJobChanged?.Invoke();
    }

    public bool ChangeJob(JobsType jobType)
    {
        PlayerJobData targetJob = jobHistory.FirstOrDefault(j => j.jobType == jobType);

        if (targetJob == null)
        {
            Debug.LogWarning($"[{characterName}] 보유하지 않은 직업입니다: {jobType}");
            return false;
        }

        foreach (var job in jobHistory)
        {
            job.isCurrentJob = false;
        }

        targetJob.isCurrentJob = true;

        Debug.Log($"[{characterName}] 직업 변경: {targetJob.GetJobName()}");

        OnJobChanged?.Invoke();
        return true;
    }

    public bool HasJob(JobsType jobType)
    {
        return jobHistory.Any(j => j.jobType == jobType);
    }

    public PlayerJobData GetCurrentJob()
    {
        return jobHistory.FirstOrDefault(j => j.isCurrentJob);
    }

    public List<PlayerJobData> GetAllJobs()
    {
        return new List<PlayerJobData>(jobHistory);
    }

    public bool CanUseSkill(JobsType requiredJob)
    {
        if (requiredJob == JobsType.None)
            return true;

        return jobHistory.Any(j => j.jobType == requiredJob);
    }

    public void RecalculateStats()
    {
        if (is_monster_stat)
        {
            return;
        }

        int oldMaxHP = maxHP;

        float base_attackPower = (15 + (Mathf.FloorToInt((strength + equip_strength) * 0.35f) + Mathf.FloorToInt((intelligence + equip_intelligence) * 0.2f)) + equip_attackBonus);
        float base_defense = (5 + (Mathf.FloorToInt((strength + equip_strength) * 0.15f) + Mathf.FloorToInt((intelligence + equip_intelligence) * 0.1f)) + equip_defenseBonus);
        float base_maxHP = (100 + (((level - 1) * 10) + Mathf.FloorToInt((strength + equip_strength) * 1.2f)) + equip_HPBonus);

        float base_accuracy = (60 + Mathf.FloorToInt((technique + equip_technique) * 0.25f) + Mathf.FloorToInt((luck + equip_luck) * 0.15f)) + skill_accuracy;
        float base_evasionRate = (2 + Mathf.FloorToInt((dexterity + equip_dexterity) * 0.2f) + Mathf.FloorToInt((luck + equip_luck) * 0.12f)) + skill_evasionRate;
        float base_criticalChance = (2 + Mathf.FloorToInt((luck + equip_luck) * 0.2f) + Mathf.FloorToInt((technique + equip_technique) * 0.15f) + Mathf.FloorToInt((intelligence + equip_intelligence) * 0.08f)) + skill_criticalChance;
        float base_criticalDamage = (150 + Mathf.FloorToInt(((strength + equip_strength) + (intelligence + equip_intelligence) + (dexterity + equip_dexterity) + (luck + equip_luck) + (technique + equip_technique)) * 0.075f)) + skill_criticalDamage;

        attackPower = Mathf.FloorToInt(base_attackPower + (base_attackPower * (skill_attackBonus / 100)));
        defense = Mathf.FloorToInt(base_defense + (base_defense * (skill_defenseBonus / 100)));
        maxHP = Mathf.FloorToInt(base_maxHP + (base_maxHP * (skill_HPBonus / 100)));

        float basePluEquip = baseMoveSpeed + equip_moveSpeedBonus;
        moveSpeed = basePluEquip * (1 + skill_moveSpeedBonus);
        moveSpeed = Mathf.Clamp(moveSpeed, 1f, 15f);

        AdjustCurrentHP(oldMaxHP, maxHP);

        OnStatsChanged?.Invoke();
    }

    private void AdjustCurrentHP(int oldMaxHP, int newMaxHP)
    {
        if (oldMaxHP == newMaxHP)
            return;

        int hpDifference = newMaxHP - oldMaxHP;

        if (currentHP == oldMaxHP)
        {
            currentHP = newMaxHP;
            Debug.Log($"[{characterName}] HP 풀 상태 - currentHP를 maxHP에 맞춤: {currentHP}/{newMaxHP}");
        }
        else if (currentHP < oldMaxHP)
        {
            if (hpDifference > 0)
            {
                currentHP += hpDifference;
                currentHP = Mathf.Min(currentHP, newMaxHP);
                Debug.Log($"[{characterName}] maxHP 증가 (+{hpDifference}) - currentHP도 증가: {currentHP}/{newMaxHP}");
            }
            else
            {
                currentHP = Mathf.Min(currentHP, newMaxHP);
                Debug.Log($"[{characterName}] maxHP 감소 ({hpDifference}) - currentHP 조정: {currentHP}/{newMaxHP}");
            }
        }
        else if (currentHP > oldMaxHP)
        {
            currentHP = newMaxHP;
            Debug.LogWarning($"[{characterName}] 비정상 HP 감지! currentHP를 maxHP로 조정: {currentHP}/{newMaxHP}");
        }
    }

    public void GainExperience(int amount)
    {
        if (amount <= 0) return;

        currentExp += amount;
        OnExpGained?.Invoke(amount);

        Debug.Log($"[{characterName}] 경험치 +{amount} (현재: {currentExp}/{expToNextLevel})");

        while (currentExp >= expToNextLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentExp -= expToNextLevel;
        level++;

        expToNextLevel = Mathf.RoundToInt(100 * Mathf.Pow(1.2f, level - 1));

        strength += 2;
        dexterity += 2;
        intelligence += 2;
        luck += 2;
        technique += 2;
        RecalculateStats();

        currentHP = maxHP;

        Debug.Log($"[{characterName}] 레벨 업! Lv.{level}");

        OnLevelUp?.Invoke();
    }

    public int TakeDamage(int rawDamage, bool is_critical, float attackerAccuracy = 100f)
    {
        if (currentHP <= 0) return 0;

        float hitChance = attackerAccuracy - evasionRate;
        hitChance = Mathf.Clamp(hitChance, 10f, 95f);

        if (UnityEngine.Random.Range(0f, 100f) > hitChance)
        {
            Debug.Log($"[{characterName}] 회피!");

            if (damageTextSpawner != null)
            {
                damageTextSpawner.ShowMiss();
            }

            return 0;
        }

        float damageReduction = defense / (defense + 100f);
        int actualDamage = Mathf.RoundToInt(rawDamage * (1f - damageReduction));

        actualDamage = Mathf.Max(1, actualDamage);

        currentHP -= actualDamage;
        currentHP = Mathf.Max(0, currentHP);

        Debug.Log($"[{characterName}] 데미지 -{actualDamage} (방어 {defense}, 감소 {damageReduction * 100f:F1}%, HP: {currentHP}/{maxHP})");

        if (damageTextSpawner != null)
        {
            damageTextSpawner.ShowDamage(actualDamage, is_critical);
        }

        if (PlayerController.Instance != null && !is_monster_stat)
        {
            PlayerController.Instance.PlayHitAnimation();
        }

        OnStatsChanged?.Invoke();

        if (currentHP <= 0)
        {
            Die();
        }

        return actualDamage;
    }

    public void Heal(int amount)
    {
        if (currentHP <= 0) return;

        int actualHealAmount = Mathf.Min(amount, maxHP - currentHP);

        currentHP += actualHealAmount;

        Debug.Log($"[{characterName}] 체력 회복 +{actualHealAmount} (HP: {currentHP}/{maxHP})");

        if (damageTextSpawner != null)
        {
            damageTextSpawner.ShowHeal(actualHealAmount);
        }

        OnStatsChanged?.Invoke();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        gold += amount;
        Debug.Log($"[{characterName}] 골드 +{amount} (현재: {gold})");
        OnGoldChanged?.Invoke(gold);
        OnStatsChanged?.Invoke();
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0) return false;

        if (gold < amount)
        {
            Debug.LogWarning($"[{characterName}] 골드 부족! (보유: {gold}, 필요: {amount})");
            return false;
        }

        gold -= amount;
        Debug.Log($"[{characterName}] 골드 -{amount} (현재: {gold})");
        OnGoldChanged?.Invoke(gold);
        OnStatsChanged?.Invoke();
        return true;
    }

    public bool HasGold(int amount)
    {
        return gold >= amount;
    }

    public int GetCurrentGold()
    {
        return gold;
    }

    private void Die()
    {
        Debug.Log($"[{characterName}] 사망!");
        OnDeath?.Invoke();
    }

    public void FullRecover()
    {
        currentHP = maxHP;
        OnStatsChanged?.Invoke();
    }

    public CharacterStats Clone()
    {
        CharacterStats clone = new CharacterStats();
        clone.characterName = this.characterName;
        clone.level = this.level;
        clone.currentExp = this.currentExp;
        clone.expToNextLevel = this.expToNextLevel;
        clone.gold = this.gold;
        clone.strength = this.strength;
        clone.dexterity = this.dexterity;
        clone.intelligence = this.intelligence;
        clone.luck = this.luck;
        clone.technique = this.technique;
        clone.maxHP = this.maxHP;
        clone.currentHP = this.currentHP;
        clone.attackPower = this.attackPower;
        clone.defense = this.defense;
        clone.criticalChance = this.criticalChance;
        clone.criticalDamage = this.criticalDamage;
        clone.evasionRate = this.evasionRate;
        clone.accuracy = this.accuracy;
        clone.is_monster_stat = this.is_monster_stat;

        clone.jobHistory = new List<PlayerJobData>();
        foreach (var job in this.jobHistory)
        {
            clone.jobHistory.Add(new PlayerJobData(job.jobType, job.isCurrentJob));
        }

        return clone;
    }

    public CharacterStatsData ToSaveData()
    {
        List<PlayerJobSaveData> jobSaveData = new List<PlayerJobSaveData>();
        foreach (var job in jobHistory)
        {
            jobSaveData.Add(new PlayerJobSaveData
            {
                jobType = (int)job.jobType,
                isCurrentJob = job.isCurrentJob
            });
        }

        return new CharacterStatsData
        {
            characterName = this.characterName,
            level = this.level,
            currentExp = this.currentExp,
            expToNextLevel = this.expToNextLevel,
            gold = this.gold,
            strength = this.strength,
            dexterity = this.dexterity,
            intelligence = this.intelligence,
            luck = this.luck,
            technique = this.technique,
            currentHP = this.currentHP,
            maxHP = this.maxHP,
            jobHistoryData = jobSaveData
        };
    }

    public void LoadFromData(CharacterStatsData data)
    {
        characterName = data.characterName;
        level = data.level;
        currentExp = data.currentExp;
        expToNextLevel = data.expToNextLevel;
        gold = data.gold;
        strength = data.strength;
        dexterity = data.dexterity;
        intelligence = data.intelligence;
        luck = data.luck;
        technique = data.technique;

        int savedHpDeficit = data.maxHP - data.currentHP;

        Debug.Log($"[{characterName}] 저장된 HP: {data.currentHP}/{data.maxHP} (부족량: {savedHpDeficit})");

        jobHistory.Clear();
        if (data.jobHistoryData != null && data.jobHistoryData.Count > 0)
        {
            foreach (var jobSaveData in data.jobHistoryData)
            {
                JobsType jobType = (JobsType)jobSaveData.jobType;
                PlayerJobData job = new PlayerJobData(jobType, jobSaveData.isCurrentJob);
                jobHistory.Add(job);
            }
        }
        else
        {
            AddJob(JobsType.Novice, true);
        }

        RecalculateStats();

        currentHP = maxHP - savedHpDeficit;
        currentHP = Mathf.Clamp(currentHP, 1, maxHP);

        Debug.Log($"[{characterName}] HP 복원 완료: {currentHP}/{maxHP} (부족량: {maxHP - currentHP})");
    }

    public void ModifyStat(StatType stattype, int value)
    {
        switch (stattype)
        {
            case StatType.Strength:
                equip_strength = value;
                break;
            case StatType.Dexterity:
                equip_dexterity = value;
                break;
            case StatType.Intelligence:
                equip_intelligence = value;
                break;
            case StatType.Luck:
                equip_luck = value;
                break;
            case StatType.Technique:
                equip_technique = value;
                break;
            case StatType.AttackPower:
                equip_attackBonus = value;
                break;
            case StatType.Defense:
                equip_defenseBonus = value;
                break;
            case StatType.MaxHP:
                equip_HPBonus = value;
                break;
            default:
                Debug.LogWarning($"[{characterName}] 알 수 없는 스탯 타입: {stattype}");
                return;
        }
        RecalculateStats();
    }

}

public enum StatType
{
    Strength,
    Dexterity,
    MaxHP,
    Intelligence,
    Luck,
    Technique,
    AttackPower,
    Defense
}

[System.Serializable]
public class CharacterStatsData
{
    public string characterName;
    public int level;
    public int currentExp;
    public int expToNextLevel;
    public int gold;
    public int strength;
    public int dexterity;
    public int intelligence;
    public int luck;
    public int technique;
    public int currentHP;
    public int maxHP;
    public int attackbonus;
    public int defencebonus;

    public List<PlayerJobSaveData> jobHistoryData = new List<PlayerJobSaveData>();
}