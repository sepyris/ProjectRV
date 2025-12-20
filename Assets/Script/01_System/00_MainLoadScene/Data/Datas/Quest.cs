using Definitions;
using GameData.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestDataSO : ScriptableObject
{
    // Dictionary 대신 직렬화 가능한 List를 사용합니다. (Unity는 Dictionary를 Inspector에서 표시하지 못함)
    // 데이터 접근을 빠르게 하기 위해 런타임에 List를 Dictionary로 변환할 것입니다.
    public List<QuestData> Items = new();
}

[System.Serializable]
public class QuestData
{
    public string questId;
    public string questName;
    [TextArea] public string description;

    public QuestPrerequisite prerequisite = new();

    // 퀘스트 수락 시 지급되는 보상 (예: 아이템 전달 퀘스트)
    // 형식: item:itemid;item:itemid
    public string preAcceptReward = "";

    // 퀘스트 힌트 (NPC 위치 및 아이템 수집 관련)
    // 형식: npc:npcid,item:itemid;item:itemid
    public string questHint = "";

    public List<QuestObjective> objectives = new();

    public int rewardGold;
    public int rewardExp;

    // ItemReward로 통합
    public List<ItemReward> rewards = new();

    public QuestStatus status = QuestStatus.None;

    public bool CanAccept()
    {
        if (status != QuestStatus.None && status != QuestStatus.Offered)
            return false;

        return prerequisite.IsMet();
    }

    /// <summary>
    /// 퀘스트 힌트 파싱 - NPC 위치 힌트
    /// </summary>
    public string GetNPCLocationHint()
    {
        if (string.IsNullOrEmpty(questHint))
            return "";

        List<string> hints = new();
        string[] hintParts = questHint.Split(';');

        foreach (string hint in hintParts)
        {
            string[] parts = hint.Split(':');
            if (parts.Length == 2 && parts[0].Trim().ToLower() == "npc")
            {
                string npcId = parts[1].Trim();
                if (NPCInfoManager.Instance != null)
                {
                    Npcs npcInfo = NPCInfoManager.Instance.GetNPCInfo(npcId);
                    if (npcInfo != null)
                    {
                        string npcName = npcInfo.npcName;
                        string location = npcInfo.GetLocationDescription();
                        hints.Add($"{npcName}을(를) 찾으세요 ({location})");
                    }
                }
            }
        }

        return hints.Count > 0 ? string.Join("\n", hints) : "";
    }

    /// <summary>
    /// 퀘스트 힌트 파싱 - 아이템 수집 힌트
    /// </summary>
    public string GetItemCollectionHint()
    {
        if (string.IsNullOrEmpty(questHint))
            return "";

        List<string> hints = new();
        string[] hintParts = questHint.Split(';');

        foreach (string hint in hintParts)
        {
            string[] parts = hint.Split(':');
            if (parts.Length == 2 && parts[0].Trim().ToLower() == "item")
            {
                string itemId = parts[1].Trim();
                if (ItemDataManager.Instance != null)
                {
                    ItemData itemData = ItemDataManager.Instance.GetItemData(itemId);
                    if (itemData != null)
                    {
                        hints.Add($"{itemData.itemName}을(를) 수집하세요");
                    }
                }
            }
        }

        return hints.Count > 0 ? string.Join("\n", hints) : "";
    }

    /// <summary>
    /// 전체 퀘스트 힌트 (NPC + 아이템)
    /// </summary>
    public string GetFullQuestHint()
    {
        List<string> allHints = new();

        string npcHint = GetNPCLocationHint();
        if (!string.IsNullOrEmpty(npcHint))
            allHints.Add(npcHint);

        string itemHint = GetItemCollectionHint();
        if (!string.IsNullOrEmpty(itemHint))
            allHints.Add(itemHint);

        return allHints.Count > 0 ? string.Join("\n\n", allHints) : "";
    }

    /// <summary>
    /// 기존 목표 기반 위치 힌트 (하위 호환성 유지)
    /// </summary>
    public string GetObjectiveLocationHint()
    {
        if (objectives == null || objectives.Count == 0)
            return "";

        List<string> hints = new();

        foreach (var obj in objectives)
        {
            if (obj.IsCompleted) continue;

            if (obj.type == QuestType.Dialogue && NPCInfoManager.Instance != null)
            {
                Npcs npcInfo = NPCInfoManager.Instance.GetNPCInfo(obj.targetId);
                if (npcInfo != null)
                {
                    string npcName = npcInfo.npcName;
                    string location = npcInfo.GetLocationDescription();
                    hints.Add($"{npcName}을(를) 찾으세요 ({location})");
                }
            }
        }

        return hints.Count > 0 ? string.Join("\n", hints) : "";
    }

    /// <summary>
    /// 퀘스트 목표가 모두 완료되었는지 확인 (Completed 상태 전환용)
    /// </summary>
    public bool IsCompleted()
    {
        foreach (var obj in objectives)
        {
            if (!obj.IsCompleted)
                return false;
        }
        return true;
    }
}

[Serializable]
public class QuestPrerequisite
{
    public PrerequisiteType type = PrerequisiteType.None;
    public string value = "";

    public bool IsMet()
    {
        if (type == PrerequisiteType.None || string.IsNullOrEmpty(value))
            return true;

        switch (type)
        {
            //선행조건 레벨 체크
            case PrerequisiteType.Level:
                if (int.TryParse(value, out int reqLevel))
                {
                    return PlayerStatsComponent.Instance.Stats.level >= reqLevel;
                }
                break;
            //선행조건 아이템 체크
            case PrerequisiteType.Item:
                return InventoryManager.Instance.HasItem(value);
            //선행조건 퀘스트 상태 체크 (Quest_001:Completed)
            case PrerequisiteType.QuestStatus:
                var parts = value.Split(':');
                if (parts.Length == 2)
                {
                    string questId = parts[0];
                    if (System.Enum.TryParse(parts[1], out QuestStatus status))
                    {
                        return QuestManager.Instance.GetQuestStatus(questId) == status;
                    }
                }
                break;
            //선행조건 여러 퀘스트 상태 체크
            case PrerequisiteType.MultipleQuests:
                // 여러 퀘스트를 동시에 체크 (Quest_001:Completed,Quest_002:Completed)
                var quests = value.Split(',');
                foreach (var quest in quests)
                {
                    var qParts = quest.Split(':');
                    if (qParts.Length == 2)
                    {
                        string qId = qParts[0].Trim();
                        if (System.Enum.TryParse(qParts[1], out QuestStatus qStatus))
                        {
                            if (QuestManager.Instance.GetQuestStatus(qId) != qStatus)
                                return false;
                        }
                    }
                }
                return true;
        }

        return false;
    }
}

[Serializable]
public class QuestObjective
{
    public QuestType type;
    public string targetId;
    public int requiredCount;
    public int currentCount;
    public bool IsCompleted => currentCount >= requiredCount;
}

/// <summary>
/// 퀘스트 상태
/// </summary>
public enum QuestStatus
{
    None,       // 퀘스트를 아직 받지 않음
    Offered,    // 퀘스트 제안됨
    Accepted,   // 퀘스트 수락함 (진행 중)
    Completed,  // 퀘스트 목표 달성 (완료 가능한 상태, NPC에게 보고 필요)
    Rewarded    // 퀘스트 완전히 완료됨 (보상 받음)
}

public enum QuestType
{
    Dialogue,
    Kill,
    Collect,
    Gather
}

public enum PrerequisiteType
{
    None,
    Level,
    Item,
    QuestStatus,
    MultipleQuests  // 여러 퀘스트 조건
}