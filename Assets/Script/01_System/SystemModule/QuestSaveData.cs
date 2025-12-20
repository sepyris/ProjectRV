using System.Collections.Generic;

/// <summary>
/// 퀘스트 저장 데이터
/// </summary>
[System.Serializable]
public class QuestSaveData
{
    public string questId;
    public QuestStatus status;
    public List<QuestObjectiveSaveData> objectives = new List<QuestObjectiveSaveData>();
}

[System.Serializable]
public class QuestObjectiveSaveData
{
    public int currentCount;
}

/// <summary>
/// 전체 퀘스트 상태 저장
/// </summary>
[System.Serializable]
public class AllQuestsSaveData
{
    public List<QuestSaveData> quests = new List<QuestSaveData>();
}